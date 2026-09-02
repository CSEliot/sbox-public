#!/usr/bin/env dash
#
# Configures a fresh clone of this fork to match the branch model described in
# README.md. Git configuration lives in .git/config and is never cloned or
# pushed, so it has to be recreated by hand on every machine - this does that.
#
#	git clone https://github.com/CSEliot/sbox-public.git
#	cd sbox-public
#	git checkout community		# this script only exists on community
#	./setup-community-fork.sh
#	./build.sh
#
# Safe to re-run.

set -e

UPSTREAM_URL="${UPSTREAM_URL:-https://github.com/Facepunch/sbox-public.git}"
NO_PUSH_URL="no-push://facepunch-is-read-only"

cd "$(dirname "$0")"

if [ ! -d .git ]; then
	echo "error: $(pwd) is not a git repository" >&2
	exit 1
fi

# upstream is fetched from, never pushed to - point its push url at a dead
# scheme so a stray 'git push upstream' fails instead of reaching Facepunch
if git remote get-url upstream >/dev/null 2>&1; then
	git remote set-url upstream "$UPSTREAM_URL"
else
	git remote add upstream "$UPSTREAM_URL"
fi
git remote set-url --push upstream "$NO_PUSH_URL"

git fetch upstream
git fetch origin

# master: pulls from Facepunch, pushes to the fork, rebases rather than
# merging so it can never grow a merge commit Facepunch doesn't have
git config branch.master.remote upstream
git config branch.master.merge refs/heads/master
git config branch.master.pushRemote origin
git config branch.master.rebase true

# make sure master exists and matches Facepunch. git refuses to fetch into a
# branch that is checked out, so fast-forward it in place when we're on it
if [ "$(git branch --show-current)" = "master" ]; then
	git merge --ff-only upstream/master
else
	git fetch upstream master:master
fi

# a bare 'git push' targets the fork from any branch, and deleted remote
# branches stop lingering as stale remote-tracking refs
git config remote.pushDefault origin
git config fetch.prune true

# community: personal workspace, lives on the fork, pulls with rebase
if ! git show-ref --verify --quiet refs/heads/community; then
	if ! git show-ref --verify --quiet refs/remotes/origin/community; then
		echo "error: neither refs/heads/community nor origin/community exists" >&2
		exit 1
	fi
	git branch community origin/community
fi
git config branch.community.remote origin
git config branch.community.merge refs/heads/community
git config branch.community.rebase true

echo
git remote -v
echo
git branch -vv
