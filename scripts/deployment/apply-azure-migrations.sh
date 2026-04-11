#!/usr/bin/env bash
set -euo pipefail

if [[ -z "${AZURE_WEBAPP_PUBLISH_PROFILE:-}" ]]; then
  echo "AZURE_WEBAPP_PUBLISH_PROFILE is required" >&2
  exit 1
fi

if [[ -z "${MIGRATION_SCRIPT_PATH:-}" ]]; then
  echo "MIGRATION_SCRIPT_PATH is required" >&2
  exit 1
fi

temp_dir="$(mktemp -d)"
trap 'rm -rf "$temp_dir"' EXIT

publish_profile_path="$temp_dir/publish-profile.xml"
printf '%s' "$AZURE_WEBAPP_PUBLISH_PROFILE" > "$publish_profile_path"

mapfile -t publish_profile_parts < <(
  python3 - "$publish_profile_path" <<'PY'
import sys
import xml.etree.ElementTree as ET

root = ET.parse(sys.argv[1]).getroot()
profiles = root.findall(".//publishProfile")

selected = None
for method in ("ZipDeploy", "MSDeploy"):
    for profile in profiles:
        if profile.attrib.get("publishMethod") == method:
            selected = profile
            break
    if selected is not None:
        break

if selected is None and profiles:
    selected = profiles[0]

if selected is None:
    raise SystemExit("No publish profile entries were found.")

publish_url = selected.attrib.get("publishUrl", "")
username = selected.attrib.get("userName", "")
password = selected.attrib.get("userPWD", "")

if not publish_url or not username or not password:
    raise SystemExit("Publish profile is missing publishUrl, userName, or userPWD.")

print(publish_url.split(":")[0])
print(username)
print(password)
PY
)

scm_host="${publish_profile_parts[0]}"
deploy_username="${publish_profile_parts[1]}"
deploy_password="${publish_profile_parts[2]}"
kudu_base_url="https://${scm_host}"
migration_dir="$(dirname "$MIGRATION_SCRIPT_PATH")"

echo "Applying database migrations through Kudu at ${kudu_base_url}"
echo "Running remote migration script ${MIGRATION_SCRIPT_PATH}"

export KUDU_COMMAND="/bin/bash $MIGRATION_SCRIPT_PATH"
export KUDU_WORKING_DIRECTORY="$migration_dir"

response_path="$temp_dir/kudu-response.json"

python3 <<'PY' > "$temp_dir/kudu-command-payload.json"
import json
import os

print(json.dumps({
    "command": os.environ["KUDU_COMMAND"],
    "dir": os.environ["KUDU_WORKING_DIRECTORY"],
}))
PY

curl --fail-with-body --silent --show-error \
  --user "${deploy_username}:${deploy_password}" \
  --header "Content-Type: application/json" \
  --data @"$temp_dir/kudu-command-payload.json" \
  "${kudu_base_url}/api/command" \
  > "$response_path"

python3 - "$response_path" <<'PY'
import json
import pathlib
import sys

response = json.loads(pathlib.Path(sys.argv[1]).read_text())
stdout = response.get("Output", "")
stderr = response.get("Error", "")
exit_code = response.get("ExitCode", 0)

if stdout:
    print(stdout, end="" if stdout.endswith("\n") else "\n")

if stderr:
    print(stderr, file=sys.stderr, end="" if stderr.endswith("\n") else "\n")

if exit_code != 0:
    raise SystemExit(exit_code)
PY
