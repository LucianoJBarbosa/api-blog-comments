#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"

dotnet run --project "$repo_root/tools/api-blog-comments-dev.Tools/api-blog-comments-dev.Tools.csproj" -- rebuild-demo-db