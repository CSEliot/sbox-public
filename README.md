<div align="center">
  <img src="https://sbox.game/img/sbox-logo-square.svg" width="80px" alt="s&box logo">

  [Website] | [Getting Started] | [Forums] | [Documentation] | [Contributing]
</div>

[Website]: https://sbox.game/
[Getting Started]: https://sbox.game/learn/getting-started
[Forums]: https://sbox.game/f/
[Documentation]: https://sbox.game/dev/doc/
[Contributing]: CONTRIBUTING.md

---

# CS Eliot Personal Fork

---

## CS Eliot Personal Fork Details

master branch is always 1-1 with FacePunch master (github.com/Facepunch/sbox-public)
community branch is my personal workspace with things like my own build.sh and features I prefer not to PR. master is its upstream and master changes are rebased into community branch.
feature/foo type branches are always forked from community, then PR'd to master.

Work on feature/foo normally, with community's build.sh available the whole time. Because community carries personal commits (build.sh, this README section, .gitignore additions) that must never reach FacePunch, the feature is replayed off master onto a disposable PR branch right before opening the PR:

```bash
git checkout -b feature/foo-pr feature/foo
git rebase --onto master community feature/foo-pr
git diff --name-only master...feature/foo-pr   # sanity check: only the feature's own files
git push -u origin feature/foo-pr
```

That drops the community base and leaves only the feature's own commits. feature/foo itself is never rewritten, so it keeps build.sh and stays workable. To update the PR after more work: `git branch -f feature/foo-pr feature/foo`, rebase again, force-push.

The sanity check matters: git normally drops the community base commits by patch-id even if community was rebased in the meantime, but it can't when a community commit's *content* was rewritten (a conflict resolved during a community rebase) - then build.sh leaks into the PR diff. Editing build.sh or .gitignore inside a feature commit leaks too.

---


# s&box

[s&box](https://sbox.game) is a modern game engine, built on Valve's Source 2 and the latest .NET technology, it provides a modern intuitive editor for creating games.

![s&box editor](https://cdn.sbox.game/about/editor.webp)

If your goal is to create games using s&box, please start with [getting started guide](https://sbox.game/learn/getting-started).
This repository is for building the engine from source for those who want to contribute to the development of the engine.

## Getting the engine

### Steam

You can download and install the s&box editor directly from [Steam](https://store.steampowered.com/app/590830/sbox/).

### Compiling from source

This repository contains the C# engine, editor, tooling and game content. The native Source 2 core is
distributed as prebuilt binaries that setup downloads for your platform, so no C++ toolchain is needed.

| Platform | Setup | Notes |
|----------|-------|-------|
| Windows 10 / 11 (x64) | `Setup.bat` | |
| Linux (x64) | `./Setup.sh` | Binaries target the Steam Linux Runtime, most distros should work. |
| macOS (Apple Silicon) | `./Setup.sh` | Intel Macs are not supported. |

#### Prerequisites

* [Git](https://git-scm.com/downloads)
* [.NET 10 SDK](https://dotnet.microsoft.com/en-us/download)
* An IDE for the C# code is recommended: [Visual Studio 2026](https://visualstudio.microsoft.com/) or
  [Rider](https://www.jetbrains.com/rider/) on Windows, Rider or [VS Code](https://code.visualstudio.com/) on Linux and macOS.

#### Setup

```bash
# Clone the repo
git clone https://github.com/Facepunch/sbox-public.git
cd sbox-public

# Windows
Setup.bat

# Linux / macOS
./Setup.sh
```

Once setup completes, the game (sbox) and editor (sbox-dev.exe) run from the `game` folder.

#### Staying up to date

Pulling on Git will run a hook that fetches any new native binaries or content that changed and regenerates the interop bindings. Build the C# code from your IDE as usual, or rerun `Setup.bat` for a full incremental rebuild including shaders and content.

## Contributing

If you would like to contribute to the engine, please see the [contributing guide](CONTRIBUTING.md).

If you want to report bugs or request new features, see [sbox-issues](https://github.com/Facepunch/sbox-public/issues/).

## Documentation

Full documentation, tutorials, and API references are available at [sbox.game/dev/](https://sbox.game/dev/).

## License

The s&box engine source code is licensed under the [MIT License](LICENSE.md).

Certain native binaries in `game/bin` are not covered by the MIT license. These binaries are distributed under the s&box EULA. You must agree to the terms of the EULA to use them.

This project includes third-party components that are separately licensed.
Those components are not covered by the MIT license above and remain subject
to their original licenses as indicated in `game/thirdpartylegalnotices`.
