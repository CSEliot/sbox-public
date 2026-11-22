## Status: STARTED? YES! CLARIFIED? NO! HISTORY? YES! REVIEWED? YES! DONE? YES! WORKER: sa-0-cccf2481 OTHER: review clean, commit in progress

## Task
Fixing max path support in sbox-editor

### Description
Documentation regarding max path can be found here: https://learn.microsoft.com/en-us/windows/win32/fileio/maximum-file-path-limitation?tabs=powershell
sbox-editor currently doesn't support such.
A branch has been made in the /Dev/ editor project, work in that.

### Work Review

2026-11-22 14:08 (UTC+03:00) - Verdict: no further changes

Findings: none (no [High], [Medium] or [Low] defect found in the 8 owner files).

Area checks (each risk in the assigned order, result only):
- A manifest namespaces: no defect. `<trustInfo xmlns="urn:schemas-microsoft-com:asm.v3">` with `requestedPrivileges`/`requestedExecutionLevel` inheriting asm.v3 is a Microsoft-documented canonical form (learn.microsoft.com "Running with Administrator Privileges"); the SDK default's v2-trustInfo/v3-requestedPrivileges form and ours put `requestedExecutionLevel level="asInvoker" uiAccess="false"` in the same asm.v3 namespace, so UAC behaviour is unchanged. `http://schemas.microsoft.com/SMI/2016/WindowsSettings` is the correct longPathAware namespace; it sits under `asmv3:application/asmv3:windowsSettings`. No fix needed.
- B LongPath.cs: no defect. IsTooLong is null-safe; GetFullPath does not emit a `\\?\` prefix for the common path (PathHelper.Normalize -> GetFullPathNameW; the short-name path strips its temporary prefix), so no double-prefix; dot segments rejected before normalization; UNC, drive-relative, already-prefixed, `/`->`\` and GetFullPath cases handled; RemoveExtendedPrefix maps `\\?\UNC\` -> `\\`; Windows guards present. `MaxPathLength { get; set; } = 32767` is an exposed, settable property satisfying the stylistic note.
- C NativeFileSystem.cs: no defect. Null/zero-size query returns required size including NUL (documented GetFinalPathNameByHandleW behaviour), so `new char[requiredLength]` plus `len >= buffer.Length` is correct; dwFlags=0 on both passes matches the settled decision; handle still closed in finally; `\\?\` input works with FILE_FLAG_BACKUP_SEMANTICS.
- D HammerTools.Edit.cs: no defect. `LongPath` resolves via `global using Sandbox;` (AssemblyInitialize.cs:2) and the gate compiled clean; raising the heuristic cap 260 -> MaxPathLength is the intended change, with no observed side effect on `.png`-style extension detection.
- E csproj wiring: no defect. `..\..\LongPathAware.manifest` resolves to `engine/LongPathAware.manifest` from all three project directories; `'$(OS)' == 'Windows_NT'` correctly skips the Windows manifest on non-Windows builds; NativeAOT EditorLauncher embedding is confirmed by the on-disk byte probe.
- F scope/adherence: no defect. Exactly the pinned 8 files changed; no registry writes, generated-file edits or new test files; no out-of-scope owner edits. Exposed property, OS guards and one shared helper honour the stylistic notes.

Tree:
154904f8cd472aac7a0d4c55c42a878cea1448c8  .gitignore  (git hash-object --path=.gitignore .gitignore)
f4ad409ff49586aa8a27ad08a04a9027c704080e  engine/Launcher/Sbox/Sbox.csproj  (git hash-object --path=engine/Launcher/Sbox/Sbox.csproj engine/Launcher/Sbox/Sbox.csproj)
70adc2772cb67779548f3335f950e6a3cfe03265  engine/Launcher/SboxDev/Sbox-Dev.csproj  (git hash-object --path=engine/Launcher/SboxDev/Sbox-Dev.csproj engine/Launcher/SboxDev/Sbox-Dev.csproj)
191bc944e9ce050d4fa5388a006983f018b4d815  engine/Sandbox.SolutionGenerator/NativeFileSystem.cs  (git hash-object --path=engine/Sandbox.SolutionGenerator/NativeFileSystem.cs engine/Sandbox.SolutionGenerator/NativeFileSystem.cs)
537c650c8fd1afb98840fce51ed00068a9991c5a  engine/Sandbox.Tools/MapEditor/Mcp/HammerTools.Edit.cs  (git hash-object --path=engine/Sandbox.Tools/MapEditor/Mcp/HammerTools.Edit.cs engine/Sandbox.Tools/MapEditor/Mcp/HammerTools.Edit.cs)
32f5a63f5ba9628d966c2acde1b8667c8b0205a8  engine/Tools/EditorLauncher/EditorLauncher.csproj  (git hash-object --path=engine/Tools/EditorLauncher/EditorLauncher.csproj engine/Tools/EditorLauncher/EditorLauncher.csproj)
589c0a1f1da8ad691f5ef5a66fe753f6f6d62f49  engine/LongPathAware.manifest  (git hash-object engine/LongPathAware.manifest)
c9b90f7c316d787245abd70ab85afee6f16ae196  engine/Sandbox.System/LongPath.cs  (git hash-object engine/Sandbox.System/LongPath.cs)

## Notes

### Implementation Plan (plan gate — awaiting PERMISSION; no code edits this turn)

Commit ordering: implement -> run gates -> write evidence to scratch -> report. NO commit in this phase. The Review Critic reviews the tree first; commit exactly the reviewed tree only after a "no further changes" verdict and the DONE? write.

Pre-edit safety: before editing, copy each touched file into `C:/Users/Eliot/AppData/Local/hermes/profiles/hermes-sbox/cache/scratch/max-path-task/pre-edit/` and restore from that copy if an edit must be undone. No `git checkout/restore/stash/reset`. Grep call sites before deleting any member. LEAVE IT COMPILING at every point.

Owner files and exact changes (8, pinned):
1. `engine/LongPathAware.manifest` (NEW) - shared Win32 manifest: `assembly manifestVersion="1.0"` with asm.v1/asmv3 namespaces, `assemblyIdentity`, the standard `trustInfo`/`requestedPrivileges`/`requestedExecutionLevel level="asInvoker" uiAccess="false"` block (keep it so UAC/DPI behaviour is unchanged), and `asmv3:application > asmv3:windowsSettings xmlns:ws2` containing `<ws2:longPathAware>true</ws2:longPathAware>`.
2. `.gitignore` (EDIT) - add exactly ONE line, `!*.manifest`, to the whitelist section so the new manifest is committable (without it `.gitignore:2 *` ignores the file).
3. `engine/Launcher/SboxDev/Sbox-Dev.csproj` (EDIT) - add `<ApplicationManifest>..\..\LongPathAware.manifest</ApplicationManifest>` in a PropertyGroup with `Condition="'$(OS)' == 'Windows_NT'"`.
4. `engine/Launcher/Sbox/Sbox.csproj` (EDIT) - same Windows-conditioned `ApplicationManifest`.
5. `engine/Tools/EditorLauncher/EditorLauncher.csproj` (EDIT) - same Windows-conditioned `ApplicationManifest` (already win-x64; condition added for consistency).
6. `engine/Sandbox.System/LongPath.cs` (NEW) - `namespace Sandbox; public static class LongPath`: `public static int MaxPathLength { get; set; } = 32767;` (exposed property per stylistic note); `public static bool IsTooLong( string path )` (null-safe, `> MaxPathLength`); `public static string EnsureExtendedPrefix( string path )` (Windows-only via `OperatingSystem.IsWindows()`, no-op on Unix/relative/already-prefixed, otherwise `Path.GetFullPath`, `/` -> `\`, prepend `\\?\` for drive or `\\?\UNC\` for UNC, reject `.`/`..` segments since the prefix disables normalization); `public static string RemoveExtendedPrefix( string path )` (strip `\\?\UNC\` -> `\\` and `\\?\` -> `""`). Tab indent, XML doc comments, follow `.editorconfig`. Place in Sandbox.System because Sandbox.Tools and Sandbox.SolutionGenerator can reference it; do NOT put it in `Sandbox.Filesystem/SafePath.cs` (internal, invisible to SolutionGenerator).
7. `engine/Sandbox.Tools/MapEditor/Mcp/HammerTools.Edit.cs` (EDIT) - in `LooksLikeAssetPath` (~:389) replace `value.Length > 260` with `LongPath.IsTooLong( value )`; add nothing else to this file.
8. `engine/Sandbox.SolutionGenerator/NativeFileSystem.cs` (EDIT) - pass `LongPath.EnsureExtendedPrefix( path )` to `CreateFileW`; replace the manual `StartsWith`/`Substring` 4-char strip with `LongPath.RemoveExtendedPrefix( finalPath )`; replace the fixed `new char[512]` with a two-pass `GetFinalPathNameByHandleW` (first call with a null/0 buffer to learn the required length, then allocate and call again) so long canonical paths are not silently dropped; keep the `OperatingSystem.IsWindows()` guard (~:38) and the existing try/catch.

Explicitly OUT of scope: no registry writes; no native C++ core changes; no removal of the short staging-path workarounds in `ContentRoots.cs`/`ContentBuilder.cs`; no generated Steam interop edits; no new test files; no `current_goals.md` edits; nothing under `.humans-only/` or `.obsidian/`; no git commands this turn.

Verification (all evidence on disk in `.../scratch/max-path-task/`, paths reported in the return):
- Offline gate from project root: `SBOX_SKIP_HOOKS=1 dotnet run --project engine/Tools/SboxBuild/SboxBuild.csproj -- build --config Developer`; save the full log; require "Build completed successfully!" and "Build managed code: succeeded"; grep the log for `error CS` (must be empty).
- Byte probe (ASCII + UTF-16LE) of `game/sbox-dev.exe` and `game/sbox.exe` for `longPathAware` (absent before, present after); save the probe output.
- EditorLauncher: `dotnet build engine/Tools/EditorLauncher/EditorLauncher.csproj -c Release`, then probe its produced exe; if that build cannot run here, state so plainly rather than claiming verification.
- `git status --short`: the new `.manifest` must appear as untracked-but-not-ignored (proves `!*.manifest` works) and no unintended files staged.
- Keep the build log and grep output on disk and name each evidence path in the report.

Design forks for the Master (no answers written by me):
Q: AGENTS.md says exposing a new .NET type to game code requires an access allow-list entry under `engine/Sandbox.Compiling/Compiler/Access/Rules/`. The pinned 8-file owner list contains no allow-list file. Confirm none is needed because `Sandbox.LongPath` is engine-internal and never referenced from game scripts.
Q: For the two-pass `GetFinalPathNameByHandleW`, confirm the flags: retain the current flags, or pass `FILE_NAME_NORMALIZED` (0)? Recommendation: retain current behaviour so the returned path text does not change.

Master decisions on the two design forks (settled from source 2026-11-22; these are NOT Director questions):
1. Allow-list: NO new file under `engine/Sandbox.Compiling/Compiler/Access/Rules/`. `BaseAccess.cs:8` already carries `"Sandbox.System/*"`, a blanket allow for the whole assembly, and `AccessRules.cs:97-103` allows a symbol only when no deny rule matches and an allow rule does. A public `Sandbox.LongPath` in `Sandbox.System` is therefore already allowed; we are not bypassing the allow-list. Accept the consequence: the helper becomes visible to game code as a small engine utility.
2. `GetFinalPathNameByHandleW` flags: keep `dwFlags = 0` on BOTH passes. `FILE_NAME_NORMALIZED` is 0x0, so "retain current flags" and "pass FILE_NAME_NORMALIZED" are the same value; do not change the returned path text. Pin 0 in both P/Invoke calls.
3. Namespace/using: `HammerTools.Edit.cs` is in namespace `Editor.Mcp`, but `engine/Sandbox.Tools/AssemblyInitialize.cs:2` has `global using Sandbox;`, so `LongPath` resolves without a new using. If the gate disagrees, fully qualify `Sandbox.LongPath` rather than adding a using.

Master note on the manifest namespace (2026-11-22, after the review): the Master flagged that our `trustInfo` uses `xmlns="urn:schemas-microsoft-com:asm.v3"` while the SDK default (extracted from `game/sbox-dev.exe`) and the Microsoft "Application manifests" doc example use `asm.v2` trustInfo + `asm.v3` requestedPrivileges. Checks: `mt.exe -validate_manifest` parses both forms successfully; the all-asmv3 trustInfo form is used by shipping Windows applications (e.g. Firefox's firefox.exe.manifest) and puts `requestedExecutionLevel level="asInvoker" uiAccess="false"` in the SAME asm.v3 namespace as the documented form. Both forms therefore preserve UAC asInvoker; no defect. Accepted the Review Critic's "no further changes" verdict. Evidence in scratch: `corrected.manifest`, `app-manifests.html`, `extract_manifest.py`.

## Clarification Q&A

Q: (Master, standing decisions; change if wrong) Scope and process for "max path support".
- Parent decision: work in the live tree on branch max-path-fix (the Director created it); no scratch clone, no branch surgery.
- Parent decision: no upfront clarification (current_goals.md carries SESSION START? YES!, the split layout has no begin_flag). Mid-task blocking questions go to the Director.
- Parent decision: offline verification only, via the SboxBuild Developer build gate; no live editor launch, no playtest.
- Parent decision: the commit stages explicit code files plus this task file; plain message, no AI attribution; always push to origin (CSEliot fork) as branch max-path-fix. current_goals.md is NOT part of this task commit.
- Parent decision: stylistic notes apply verbatim (X/Y-style constants become exposed properties for developer tweaking; portability and DRY are priorities).
- Parent decision: Skills To Know in current_goals.md are stale ("box-agent-playtest" -> sbox-agent-playtest; "sbox-editor-mcp-driving" -> sbox-cli-driving). Informational only for this task.

## Relevant History 

Windows MAX_PATH is 260 chars; Win32 Unicode APIs accept an extended-length `\\?\` form (~32,767 UTF-16 chars) that bypasses it, and Windows 10 1607+ also offers a per-app opt-in requiring both the machine `LongPathsEnabled` registry value and a `<ws2:longPathAware>true</ws2:longPathAware>` application manifest. .NET 10 managed `System.IO` already handles long paths internally, so a residual editor failure lives in Win32 P/Invoke, spawned native tools, or managed heuristics rather than `System.IO`. Verified on this checkout (2026-11-22): every built launcher embeds a default manifest with `requestedExecutionLevel` but none declares `longPathAware`, and a scratch `dotnet build` probe proved `<ApplicationManifest>` embeds it into both the managed DLL and the apphost `.exe`.

Concrete blockers found: `engine/Sandbox.Tools/MapEditor/Mcp/HammerTools.Edit.cs:389` rejects any asset-path string longer than 260 chars; `engine/Sandbox.SolutionGenerator/NativeFileSystem.cs` P/Invokes `CreateFileW` with a raw path, uses a fixed 512-char buffer for `GetFinalPathNameByHandleW`, and strips `\\?\` with a manual 4-char substring. `engine/Sandbox.ContentBuild/ContentRoots.cs:44` and `ContentBuilder.cs:873` keep staging paths short specifically because of native MAX_PATH. Baseline SboxBuild Developer build is green. Full research lives in the pinned scratch SPEC.md, research.md, context.md and ms-doc.html.

## Stylistic Task Notes:
References to variables like X and Y automatically imply that these are to be exposed properties for developer tweaking.
When programming, portability and DRY are a priority.

## Post-Task Follow Ups 
