<div align="center">
  <img src="https://sbox.game/img/sbox-logo-square.svg" width="80px" alt="s&box logo">

  [Website] | [Getting Started] | [Forums] | [Documentation] | [Contributing]
</div>

[Website]: https://sbox.game/
[Getting Started]: https://sbox.game/dev/doc/about/getting-started/first-steps/
[Forums]: https://sbox.game/f/
[Documentation]: https://sbox.game/dev/doc/
[Contributing]: CONTRIBUTING.md

---

# S&Box Community Fork

---

## A Personal Fork for the S&Box Linux Community - Details

### First Steps

```bash
./setup-community-fork.sh
```
This repository has unique configurations to setup.

### Why Like This?

1. So Master Branch is always 1-1 with FacePunch master (github.com/Facepunch/sbox-public)
2. So Community Branch can offer a lower barrier-to-entry. (With things like a build.sh and features we prefer not to PR, Master is Upstream and Master changes are rebased into Community Branch)
3. So we create feature/foo type branches that have the best of both worlds: dev-friendly tools in-repo PLUS!! easy PR submissions containing ONLY necessary changes.

### Okay, What Do I Do?

Work on your feature/foo fork (off Community Branch) normally, with Community's tools available the whole time. When you're ready to make a PR, do the following:

```bash
git checkout -b feature/foo-pr feature/foo 		# create the pr branch
git rebase --onto master community feature/foo-pr	# update your pr with master, clears the changes/additions from Community Branch
git diff --name-only master...feature/foo-pr   		# sanity check: only the feature's own files
git push -u origin feature/foo-pr			# push and open github to create your online pull request submission
```

Feature/foo itself is never rewritten, so it keeps build.sh and stays workable. To update your PR, if needed: `git branch -f feature/foo-pr feature/foo`, rebase again, force-push.

## Updating from Upstream

```bash
git fetch upstream master:master   		# no checkout; if this fails, someone has touched master inappropriately, contact authorities
git push origin master             		# making sure remote copy is also updated

git checkout community && git rebase master	# update community with the master changes
git push --force-with-lease        		# Rebasing changes ancestor history, so force required. Lease protects us in the case where there's more than 1 meddler per branch.
```
This is assuming you're never standing in the master branch. And, if you follow these steps correctly, you shouldn't need to.

If you have any feature/foo branches in flight, rebase them onto the new Community tip too:

```bash
git rebase community feature/foo
```

Otherwise their base is a Community tip that no longer exists, and the `--onto` above can't tell which commits came from Community anymore - build.sh and friends leak into your PR diff.

---


# s&box

s&box is a modern game engine, built on Valve's Source 2 and the latest .NET technology, it provides a modern intuitive editor for creating games.

![s&box editor](https://files.facepunch.com/matt/1b2211b1/sbox-dev_FoZ5NNZQTi.jpg)

If your goal is to create games using s&box, please start with the [getting started guide](https://sbox.game/dev/doc/about/getting-started/first-steps/).
This repository is for building the engine from source for those who want to contribute to the development of the engine.

## Getting the Engine

### Steam

You can download and install the s&box editor directly from [Steam](https://store.steampowered.com/app/590830/sbox/).

### Compiling from Source

If you want to build from source, this repository includes all the necessary files to compile the engine yourself.

#### Prerequisites

* [Git](https://git-scm.com/install/windows)
* [Visual Studio 2026](https://visualstudio.microsoft.com/)
* [.NET 10 SDK](https://dotnet.microsoft.com/en-us/download)

#### Building

```bash
# Clone the repo
git clone https://github.com/Facepunch/sbox-public.git
```

Once you've cloned the repo simply run `Bootstrap.bat` which will download dependencies and build the engine.

The game and editor can be run from the binaries in the game folder.

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
