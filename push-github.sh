#!/bin/bash
set -euo pipefail

# Run from your regular terminal while run-gost is active.
project_root="$(cd -- "$(dirname -- "$0")" && pwd)"
cd "$project_root"
proxy_url="${GIT_PROXY_URL:-socks5h://127.0.0.1:51080}"
remote_url="https://github.com/SaidimM/XTrial.git"

printf 'Checking GitHub through the local GOST proxy...\n'
# Test before changing the repository. TLS certificate verification stays enabled.
git -c http.proxy="$proxy_url" -c http.connectTimeout=15 ls-remote "$remote_url" HEAD >/dev/null

if ! git rev-parse --show-toplevel >/dev/null 2>&1; then
    git init -b main
fi
repo_root="$(git rev-parse --show-toplevel)"
if [[ "$repo_root" != "$project_root" ]]; then
    printf 'This folder is inside another Git repository. Run from a standalone XTrial checkout.\n' >&2
    exit 1
fi

if git remote get-url origin >/dev/null 2>&1; then
    if [[ "$(git remote get-url origin)" != "$remote_url" ]]; then
        printf 'The existing origin points elsewhere. Inspect git remote -v before continuing.\n' >&2
        exit 1
    fi
else
    git remote add origin "$remote_url"
fi

# Keep the setting local to this repository and scoped to GitHub.
git config 'http.https://github.com.proxy' "$proxy_url"

git add -- .gitignore README.md push-github.sh Network \
    'New Tuanjie Project/Assets' \
    'New Tuanjie Project/Packages' \
    'New Tuanjie Project/ProjectSettings' \
    'New Tuanjie Project/Tools' \
    'New Tuanjie Project/README.md'
if ! git diff --cached --quiet; then
    git commit -m 'Add Trial X five-track game with progression and refined handling'
fi

# Ordinary push preserves any existing remote history; never force-push.
git -c http.proxy="$proxy_url" push -u origin HEAD:main
printf 'Push completed. This repository will keep using the GOST proxy for GitHub.\n'
