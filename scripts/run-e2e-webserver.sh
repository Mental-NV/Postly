#!/usr/bin/env bash

set -euo pipefail

script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
repo_root="$(cd "${script_dir}/.." && pwd)"
api_project="${repo_root}/backend/src/Postly.Api/Postly.Api.csproj"

cd "${repo_root}/frontend"
npm run build

cd "${repo_root}"
dotnet build "${api_project}"

exec dotnet run --no-build --project "${api_project}"
