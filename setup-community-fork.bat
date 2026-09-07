@echo off
setlocal EnableExtensions

REM
REM Configures a fresh clone of this fork to match the branch model described in
REM README.md. Git configuration lives in .git\config and is never cloned or
REM pushed, so it has to be recreated by hand on every machine - this does that.
REM
REM     git clone https://github.com/CSEliot/sbox-public.git
REM     cd sbox-public
REM     git checkout community          :: this script only exists on community
REM     setup-community-fork.bat
REM     Bootstrap.bat
REM
REM Windows twin of setup-community-fork.sh - keep the two in step.
REM
REM Safe to re-run.

if not defined UPSTREAM_URL set "UPSTREAM_URL=https://github.com/Facepunch/sbox-public.git"
set "NO_PUSH_URL=no-push://facepunch-is-read-only"

cd /d "%~dp0"

if not exist ".git\" (
    echo error: %CD% is not a git repository 1>&2
    exit /b 1
)

REM upstream is fetched from, never pushed to - point its push url at a dead
REM scheme so a stray 'git push upstream' fails instead of reaching Facepunch
git remote get-url upstream >nul 2>&1
if errorlevel 1 (
    git remote add upstream "%UPSTREAM_URL%"
) else (
    git remote set-url upstream "%UPSTREAM_URL%"
)
if errorlevel 1 goto :failed

git remote set-url --push upstream "%NO_PUSH_URL%"
if errorlevel 1 goto :failed

git fetch upstream
if errorlevel 1 goto :failed
git fetch origin
if errorlevel 1 goto :failed

REM master: pulls from Facepunch, pushes to the fork, rebases rather than
REM merging so it can never grow a merge commit Facepunch doesn't have
git config branch.master.remote upstream
if errorlevel 1 goto :failed
git config branch.master.merge refs/heads/master
if errorlevel 1 goto :failed
git config branch.master.pushRemote origin
if errorlevel 1 goto :failed
git config branch.master.rebase true
if errorlevel 1 goto :failed

REM ignore changes to sample projects when opening locally
for /f "delims=" %%F in ('git ls-files "game/samples/*/.sbproj"') do (
    git update-index --skip-worktree "%%F"
    if errorlevel 1 goto :failed
)

REM make sure master exists and matches Facepunch. git refuses to fetch into a
REM branch that is checked out, so fast-forward it in place when we're on it
set "CURRENT_BRANCH="
for /f "delims=" %%B in ('git branch --show-current') do set "CURRENT_BRANCH=%%B"

if "%CURRENT_BRANCH%"=="master" (
    git merge --ff-only upstream/master
) else (
    git fetch upstream master:master
)
if errorlevel 1 goto :failed

REM a bare 'git push' targets the fork from any branch, and deleted remote
REM branches stop lingering as stale remote-tracking refs
git config remote.pushDefault origin
if errorlevel 1 goto :failed
git config fetch.prune true
if errorlevel 1 goto :failed

REM community: personal workspace, lives on the fork, pulls with rebase
git show-ref --verify --quiet refs/heads/community
if errorlevel 1 (
    git show-ref --verify --quiet refs/remotes/origin/community
    if errorlevel 1 (
        echo error: neither refs/heads/community nor origin/community exists 1>&2
        exit /b 1
    )
    git branch community origin/community
    if errorlevel 1 goto :failed
)
git config branch.community.remote origin
if errorlevel 1 goto :failed
git config branch.community.merge refs/heads/community
if errorlevel 1 goto :failed
git config branch.community.rebase true
if errorlevel 1 goto :failed

echo.
git remote -v
echo.
git branch -vv
exit /b 0

:failed
set "RC=%ERRORLEVEL%"
echo error: git command failed with exit code %RC% 1>&2
exit /b %RC%
