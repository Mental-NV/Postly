# Postly

Unit test coverage badges are refreshed by the scheduled **Coverage Badges** workflow.

- Backend: <!-- backend-coverage-badge:start -->![Backend Coverage](https://img.shields.io/badge/backend%20coverage-pending-lightgrey)<!-- backend-coverage-badge:end -->
- Frontend: <!-- frontend-coverage-badge:start -->![Frontend Coverage](https://img.shields.io/badge/frontend%20coverage-pending-lightgrey)<!-- frontend-coverage-badge:end -->

## Coverage badge automation

The workflow in `.github/workflows/coverage-badges.yml` runs daily at 07:00 UTC and can also be run manually. It:

1. Runs backend unit tests with XPlat Code Coverage and writes Cobertura XML.
2. Runs frontend unit tests with Vitest coverage output in Cobertura format.
3. Updates the coverage badge URLs in this README using `scripts/coverage/update-readme-badges.sh`.
4. Commits and pushes the README update when coverage numbers change.

## Manual setup required (one-time)

Yes — most repos need a one-time permission setup for the bot commit to work:

1. In **Settings → Actions → General → Workflow permissions**, set **Read and write permissions**.
2. If `master` is branch-protected and blocks direct pushes from `GITHUB_TOKEN`, create a bot PAT with `repo` scope and save it as repository secret `COVERAGE_BADGE_TOKEN`.
3. If branch protection requires pull requests, switch the final step to a PR-based action (for example `peter-evans/create-pull-request`) instead of direct `git push`.
