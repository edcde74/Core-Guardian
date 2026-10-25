#!/usr/bin/env bash
# Push a commit made by cg_build_publish.sh to origin main, then move github-main to it.
# Usage: bash docs/development/git_publish/cg_push.sh <PUBLISH_SHA>
set -euo pipefail
cd "$(git rev-parse --show-toplevel)"
sha="$1"
test "$(git rev-parse "$sha^")" = "$(git rev-parse github-main)"
git push origin "$sha:refs/heads/main"
git branch -f github-main "$sha"
git fetch origin main
echo "origin/main=$(git rev-parse origin/main) github-main=$(git rev-parse github-main)"
echo "DONE_PUSH"
