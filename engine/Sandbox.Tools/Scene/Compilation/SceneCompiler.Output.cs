using System;
using System.Globalization;
using System.IO;
using System.Text.Json;

namespace Editor;

internal static partial class SceneCompiler
{
	/// <summary>
	/// Invalidates the saved bake on the first geometry or collision write. An interrupted
	/// compile can then leave files on disk without making them eligible for publishing.
	/// </summary>
	internal sealed class GeometryOutputFolder( SceneFolder folder, string sourcePath ) : SceneFolder
	{
		bool started;

		/// <summary>
		/// Writes a numbered output after retiring the previous bake header.
		/// </summary>
		public override string WriteFile( string filename, byte[] data )
		{
			if ( !started )
			{
				File.Delete( SceneResourceCompiler.GetBakePath( sourcePath ) );
				started = true;
			}

			return folder.WriteFile( filename, data );
		}
	}

	readonly record struct SourceRevision( long Length, DateTime LastWriteTimeUtc, byte[] Hash );

	static SourceRevision CaptureSourceRevision( string sourcePath )
	{
		var info = new FileInfo( sourcePath );
		return new SourceRevision( info.Length, info.LastWriteTimeUtc, SceneResourceCompiler.GetSourceHash( sourcePath ) );
	}

	static bool HasSourceRevision( string sourcePath, SourceRevision revision )
	{
		var before = new FileInfo( sourcePath );
		if ( !before.Exists || before.Length != revision.Length || before.LastWriteTimeUtc != revision.LastWriteTimeUtc )
			return false;

		var hash = SceneResourceCompiler.GetSourceHash( sourcePath );
		var after = new FileInfo( sourcePath );
		return after.Exists && after.Length == revision.Length && after.LastWriteTimeUtc == revision.LastWriteTimeUtc
			&& System.Security.Cryptography.CryptographicOperations.FixedTimeEquals( revision.Hash, hash );
	}

	// Geometry owns the numbered files it writes, including those in older GUID folders.
	static IEnumerable<(string Path, int Index)> NumberedFiles( string folder, string prefix, string extension )
	{
		var options = new EnumerationOptions { AttributesToSkip = FileAttributes.ReparsePoint, IgnoreInaccessible = false };
		foreach ( var file in Directory.EnumerateFiles( folder, $"{prefix}*{extension}", options ) )
		{
			if ( int.TryParse( Path.GetFileNameWithoutExtension( file )[prefix.Length..], NumberStyles.None, CultureInfo.InvariantCulture, out var index ) )
				yield return (file, index);
		}
	}

	internal static IEnumerable<string> GetOutputFiles( string sourcePath )
	{
		var bake = new FileInfo( SceneResourceCompiler.GetBakePath( sourcePath ) );
		if ( !bake.Directory.Exists || (bake.Directory.Attributes & FileAttributes.ReparsePoint) != 0 )
			yield break;

		yield return bake.Name;
		var folder = new DirectoryInfo( SceneResourceCompiler.GetCompiledFolder( sourcePath ) );
		if ( !folder.Exists || (folder.Attributes & FileAttributes.ReparsePoint) != 0 )
			yield break;

		var legacy = folder.EnumerateDirectories().Where( d => (d.Attributes & FileAttributes.ReparsePoint) == 0 && Guid.TryParseExact( d.Name, "N", out _ ) );
		foreach ( var directory in legacy.Prepend( folder ) )
		{
			foreach ( var (file, _) in NumberedFiles( directory.FullName, "aggregate_", ".vmdl_c" )
				.Concat( NumberedFiles( directory.FullName, "mesh_", ".vmdl_c" ) )
				.Concat( NumberedFiles( directory.FullName, "collision_", ".vphys_c" ) ) )
				yield return Path.GetRelativePath( bake.DirectoryName, file );
		}
	}

	internal static void CleanupCompiledFiles( string folder, int aggregates, int meshes, int collisions )
	{
		if ( !Directory.Exists( folder ) )
			return;

		try
		{
			DeleteUnused( "aggregate_", ".vmdl_c", aggregates );
			DeleteUnused( "mesh_", ".vmdl_c", meshes );
			DeleteUnused( "collision_", ".vphys_c", collisions );

			foreach ( var directory in Directory.EnumerateDirectories( folder ) )
			{
				if ( Guid.TryParseExact( Path.GetFileName( directory ), "N", out _ ) )
					Directory.Delete( directory, recursive: true );
			}
		}
		catch ( Exception e ) when ( e is IOException or UnauthorizedAccessException )
		{
			Log.Warning( $"Could not clean up old compiled scene resources in '{folder}': {e.Message}" );
		}

		// Each writer numbers its outputs consecutively from zero.
		void DeleteUnused( string prefix, string extension, int count )
		{
			foreach ( var (file, index) in NumberedFiles( folder, prefix, extension ) )
			{
				if ( index >= count )
					File.Delete( file );
			}
		}
	}

	static void WriteBake( string path, byte[] data )
	{
		Directory.CreateDirectory( Path.GetDirectoryName( path ) );
		var temporaryPath = $"{path}.{Guid.NewGuid():N}.tmp";
		try
		{
			File.WriteAllBytes( temporaryPath, data );
			File.Move( temporaryPath, path, overwrite: true );
		}
		finally
		{
			File.Delete( temporaryPath );
		}
	}

	static void WriteScene( Asset asset, string sourcePath, SceneEditorSession editor, SourceRevision sourceRevision, SceneFile file, SceneCompilerSettings settings,
		int aggregates, int meshes, int collisions )
	{
		if ( asset.IsDeleted || !File.Exists( sourcePath ) || sourcePath != asset.GetSourceFile( true ) )
			throw new InvalidOperationException( "The source scene was moved or deleted during compilation." );

		if ( editor.HasUnsavedChanges || !HasSourceRevision( sourcePath, sourceRevision ) )
			throw new InvalidOperationException( "The source scene changed during compilation. Save it and compile again." );

		var json = file.Serialize();
		json["__blobdata"] = System.Convert.ToBase64String( file.BinaryData ?? [] );
		var jsonText = json.ToJsonString( new JsonSerializerOptions( JsonSerializerOptions.Default ) { MaxDepth = 512 } );
		var bakePath = SceneResourceCompiler.GetBakePath( sourcePath );
		var previousBake = File.Exists( bakePath ) ? File.ReadAllBytes( bakePath ) : null;
		var previousOutput = asset.GetCompiledFile( true );
		var previousBytes = !string.IsNullOrEmpty( previousOutput ) && File.Exists( previousOutput ) ? File.ReadAllBytes( previousOutput ) : null;

		try
		{
			// Outputs derive from this path string, so compile through the lowercase
			// identity: same asset (lookups are case-insensitive), lowercase _c/_d.
			var compilePath = AssetSystem.LowercaseAssetFileName( sourcePath );

			if ( !IResourceCompilerSystem.GenerateResourceFileForced( compilePath, jsonText ) )
				throw new InvalidOperationException( $"Could not write compiled scene '{asset.Path}'." );

			var outputPath = asset.GetCompiledFile( true );
			if ( string.IsNullOrEmpty( outputPath ) )
			{
				// After a clear, the file watcher hasn't registered the newly written scene yet.
				var written = FileSystem.Mounted.GetFullPath( asset.Path + "_c" );
				if ( File.Exists( written ) )
					outputPath = AssetSystem.RegisterFile( written )?.GetCompiledFile( true );
			}

			if ( string.IsNullOrEmpty( outputPath ) )
				throw new InvalidOperationException( $"Could not find the compiled output path for '{asset.Path}'." );

			var bytes = File.ReadAllBytes( outputPath );
			using var compiledJson = JsonDocument.Parse( Game.Resources.ReadCompiledResourceJson( bytes ), new JsonDocumentOptions { MaxDepth = 512 } );
			if ( compiledJson.RootElement.TryGetProperty( "__blobdata", out _ ) )
				throw new InvalidOperationException( $"The scene resource compiler did not process '{asset.Path}'." );

			var header = FormattableString.Invariant( $"{System.Convert.ToHexString( sourceRevision.Hash )} {aggregates} {meshes} {collisions}" );
			WriteBake( bakePath, System.Text.Encoding.UTF8.GetBytes( $"{header}\n{jsonText}" ) );

			// Native reopens the source by its lowercase identity, which cannot open a
			// mixed-case source on a case-sensitive filesystem. The forced write above
			// already produced and verified the output, and ReloadResource below picks it
			// up - so only refresh through native when it can actually open the source.
			if ( !AssetSystem.SourceSpellingDiffersFromIdentity( asset ) && !asset.Compile( false ) )
				throw new InvalidOperationException( $"Could not refresh compiled scene '{asset.Path}'." );

			if ( editor.HasUnsavedChanges || !HasSourceRevision( sourcePath, sourceRevision ) )
				throw new InvalidOperationException( "The source scene changed during compilation. Save it and compile again." );

			NativeEngine.g_pResourceSystem.ReloadResource( asset.Path );
			asset.MetaData.Set( SceneCompilerSettings.MetadataProperty, settings );
		}
		catch
		{
			try
			{
				if ( previousBake is not null )
					WriteBake( bakePath, previousBake );
				else
					File.Delete( bakePath );

				var outputPath = asset.GetCompiledFile( true );
				if ( string.IsNullOrEmpty( outputPath ) )
					outputPath = previousOutput;
				if ( !string.IsNullOrEmpty( outputPath ) )
				{
					if ( previousBytes is not null )
						File.WriteAllBytes( outputPath, previousBytes );
					else if ( File.Exists( outputPath ) )
						File.Delete( outputPath );

					NativeEngine.g_pResourceSystem.ReloadResource( asset.Path );
				}
			}
			catch ( Exception e )
			{
				Log.Warning( $"Could not restore the last compiled scene '{asset.Path}': {e.Message}" );
			}

			throw;
		}
	}

	/// <summary>
	/// Whether a scene recompile must bypass native <c>RecompileAsset</c>. Native file IO
	/// uses the lowercase identity spelling, which cannot open a source that keeps a
	/// mixed-case disk spelling on a case-sensitive filesystem. The native scene compiler
	/// stays the compiler - it is fed from memory instead (see <see cref="CompileCaseMismatchedScene"/>).
	/// </summary>
	internal static bool ShouldBypassNativeSceneCompile( Asset asset )
	{
		if ( asset is null || asset.IsDeleted ) return false;
		if ( !string.Equals( asset.AssetType?.FileExtension, "scene", StringComparison.OrdinalIgnoreCase ) ) return false;
		return AssetSystem.SourceSpellingDiffersFromIdentity( asset );
	}

	/// <summary>
	/// Recompile a mixed-case scene source without native source file IO. Mirrors the
	/// <see cref="WriteScene"/> feed: load from the physical spelling (source + _d blobs),
	/// hand the native compiler its input text addressed to the lowercase identity, verify
	/// it processed the output, then reload the resource.
	/// </summary>
	internal static bool CompileCaseMismatchedScene( Asset asset )
	{
		if ( !ShouldBypassNativeSceneCompile( asset ) ) return false;

		var sourcePath = asset.GetSourceFile( true );

		SceneFile file;
		try
		{
			file = SceneSource.LoadForEditing( asset );
		}
		catch ( Exception e ) when ( e is IOException or InvalidDataException or UnauthorizedAccessException or JsonException )
		{
			Log.Error( e, $"Could not load scene source '{asset.Path}'." );
			return false;
		}

		if ( file is null )
		{
			Log.Error( $"Could not load scene source '{asset.Path}'." );
			return false;
		}

		var json = file.Serialize();
		json["__blobdata"] = System.Convert.ToBase64String( file.BinaryData ?? [] );
		var previousOutput = asset.GetCompiledFile( true );
		var previousBytes = !string.IsNullOrEmpty( previousOutput ) && File.Exists( previousOutput ) ? File.ReadAllBytes( previousOutput ) : null;

		try
		{
			// Outputs derive from this path string, so compile through the lowercase
			// identity: same asset (lookups are case-insensitive), lowercase _c.
			var compilePath = AssetSystem.LowercaseAssetFileName( sourcePath );

			if ( !IResourceCompilerSystem.GenerateResourceFileForced( compilePath, json.ToJsonString( new JsonSerializerOptions( JsonSerializerOptions.Default ) { MaxDepth = 512 } ) ) )
				throw new InvalidOperationException( $"Could not write compiled scene '{asset.Path}'." );

			var outputPath = asset.GetCompiledFile( true );
			if ( string.IsNullOrEmpty( outputPath ) )
				throw new InvalidOperationException( $"Could not find the compiled output path for '{asset.Path}'." );

			var bytes = File.ReadAllBytes( outputPath );
			using var compiledJson = JsonDocument.Parse( Game.Resources.ReadCompiledResourceJson( bytes ), new JsonDocumentOptions { MaxDepth = 512 } );
			if ( compiledJson.RootElement.TryGetProperty( "__blobdata", out _ ) )
				throw new InvalidOperationException( $"The scene resource compiler did not process '{asset.Path}'." );

			NativeEngine.g_pResourceSystem.ReloadResource( asset.Path );
			return true;
		}
		catch ( Exception e )
		{
			Log.Error( e, $"Could not compile scene '{asset.Path}'." );

			try
			{
				var outputPath = asset.GetCompiledFile( true );
				if ( string.IsNullOrEmpty( outputPath ) )
					outputPath = previousOutput;
				if ( !string.IsNullOrEmpty( outputPath ) )
				{
					if ( previousBytes is not null )
						File.WriteAllBytes( outputPath, previousBytes );
					else if ( File.Exists( outputPath ) )
						File.Delete( outputPath );

					NativeEngine.g_pResourceSystem.ReloadResource( asset.Path );
				}
			}
			catch ( Exception restoreException )
			{
				Log.Warning( $"Could not restore the last compiled scene '{asset.Path}': {restoreException.Message}" );
			}

			return false;
		}
	}
}
