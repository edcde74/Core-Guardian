#!/usr/bin/env bash
# Build a GitHub upload commit from local HEAD's tree on top of github-main (no push).
# Keeps the remote LFS pointer files and .gitattributes, drops new .rgba/.r32 dumps.
# Usage (Git Bash, project root): bash docs/development/git_publish/cg_build_publish.sh
set -euo pipefail
cd "$(git rev-parse --show-toplevel)"
export GIT_INDEX_FILE="$(git rev-parse --git-dir)/cg_publish_index"
rm -f "$GIT_INDEX_FILE"
git read-tree HEAD
copy_from_remote() { # path
  local e; e=$(git ls-tree github-main -- "$1")
  if [ -n "$e" ]; then
    git update-index --add --cacheinfo "$(echo "$e" | awk '{print $1}'),$(echo "$e" | awk '{print $3}'),$1"
  else
    git update-index --force-remove -- "$1"
  fi
}
copy_from_remote .gitattributes
git show github-main:.gitattributes | grep 'filter=lfs' | awk '{print $1}' | sed 's#^/##' | while IFS= read -r p; do copy_from_remote "$p"; done
git ls-files | grep -E '\.(rgba|r32)$' | while IFS= read -r p; do copy_from_remote "$p"; done || true
tree=$(git write-tree)
git log -1 --format=%B HEAD > "$(git rev-parse --git-dir)/cg_publish_msg"
sha=$(git commit-tree "$tree" -p github-main -F "$(git rev-parse --git-dir)/cg_publish_msg")
rm -f "$GIT_INDEX_FILE"
unset GIT_INDEX_FILE
echo "PUBLISH_SHA=$sha"
echo "--- diff github-main..$sha ---"
git -c core.quotepath=false diff --stat github-main "$sha" | tail -40
echo "--- rgba/r32/lfs check ---"
git -c core.quotepath=false diff --name-only github-main "$sha" | grep -E '\.(rgba|r32|data|zip)$' || echo "none"
echo "DONE_BUILD"
