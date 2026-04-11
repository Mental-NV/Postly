#!/usr/bin/env bash
set -euo pipefail

script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
database_path="${AZURE_SQLITE_DB_PATH:-/home/data/postly.db}"
migration_bundle_name="${MIGRATION_BUNDLE_NAME:-postly-efbundle}"
migration_bundle_path="${script_dir}/${migration_bundle_name}"
connection_string="Data Source=${database_path}"

mkdir -p "$(dirname "$database_path")"
chmod +x "$migration_bundle_path"

"$migration_bundle_path" --connection "$connection_string" --verbose
