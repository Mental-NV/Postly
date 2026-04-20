#!/usr/bin/env bash
set -euo pipefail

README_PATH="${1:-README.md}"
BACKEND_COVERAGE_XML="${2:-artifacts/coverage/backend/coverage.cobertura.xml}"
FRONTEND_COVERAGE_XML="${3:-frontend/coverage/cobertura-coverage.xml}"

if [[ ! -f "$README_PATH" ]]; then
  echo "README file not found at '$README_PATH'." >&2
  exit 1
fi

if [[ ! -f "$BACKEND_COVERAGE_XML" ]]; then
  echo "Backend coverage file not found at '$BACKEND_COVERAGE_XML'." >&2
  exit 1
fi

if [[ ! -f "$FRONTEND_COVERAGE_XML" ]]; then
  echo "Frontend coverage file not found at '$FRONTEND_COVERAGE_XML'." >&2
  exit 1
fi

coverage_percent() {
  local xml_path="$1"

  python3 - "$xml_path" <<'PY'
import sys
import xml.etree.ElementTree as ET

xml_path = sys.argv[1]
root = ET.parse(xml_path).getroot()
line_rate = root.attrib.get("line-rate")

if line_rate is None:
    raise SystemExit(f"line-rate is missing in {xml_path}")

value = float(line_rate) * 100
print(f"{value:.2f}")
PY
}

badge_color() {
  local pct="$1"

  python3 - "$pct" <<'PY'
import sys
pct = float(sys.argv[1])
if pct >= 90:
    print("brightgreen")
elif pct >= 80:
    print("green")
elif pct >= 70:
    print("yellow")
elif pct >= 60:
    print("orange")
else:
    print("red")
PY
}

backend_pct="$(coverage_percent "$BACKEND_COVERAGE_XML")"
frontend_pct="$(coverage_percent "$FRONTEND_COVERAGE_XML")"

backend_color="$(badge_color "$backend_pct")"
frontend_color="$(badge_color "$frontend_pct")"

backend_value="${backend_pct//%/%25}%25"
frontend_value="${frontend_pct//%/%25}%25"

backend_badge="https://img.shields.io/badge/backend%20coverage-${backend_value}-${backend_color}"
frontend_badge="https://img.shields.io/badge/frontend%20coverage-${frontend_value}-${frontend_color}"

backend_markdown="![Backend Coverage](${backend_badge})"
frontend_markdown="![Frontend Coverage](${frontend_badge})"

perl -0pi -e "s|<!-- backend-coverage-badge:start -->.*?<!-- backend-coverage-badge:end -->|<!-- backend-coverage-badge:start -->${backend_markdown}<!-- backend-coverage-badge:end -->|s" "$README_PATH"
perl -0pi -e "s|<!-- frontend-coverage-badge:start -->.*?<!-- frontend-coverage-badge:end -->|<!-- frontend-coverage-badge:start -->${frontend_markdown}<!-- frontend-coverage-badge:end -->|s" "$README_PATH"

echo "Updated coverage badges in ${README_PATH}"
echo "Backend unit test coverage: ${backend_pct}%"
echo "Frontend unit test coverage: ${frontend_pct}%"
