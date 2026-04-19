# Postmortem: Why the Last Fix Was Expensive and How to Make Future Work Cheaper

## Summary
The time and token cost came from three compounding problems, not one bug:

- A small lint-driven refactor changed behavior because it touched React 19 `useEffectEvent` semantics in bootstrap effects.
- The e2e runner was serving stale SPA assets from backend `wwwroot`, so some debugging was happening against old code instead of current source.
- The feedback loops were noisy and slow: verbose backend logs, retry-heavy Playwright runs, and a shared helper that encoded assumptions about "page ready" too aggressively.

The biggest lesson is that the repo currently makes it too easy to confuse:
1. source behavior vs served bundle behavior,
2. lint compliance vs runtime-safe hook semantics,
3. route shell readiness vs data-loaded readiness.

## What Consumed Time and Tokens
- **Behavior changed during a "safe" lint fix**
  - `useContinuationCollection.reset` is implemented with `useEffectEvent`.
  - Treating it like a normal callback in `useCallback` / effect dependencies created bootstrap loops on timeline/profile/direct-post pages.
  - That turned one lint warning into a broad regression across unit tests and e2e.
- **Debugging happened against stale frontend assets**
  - Backend-served Playwright runs read from `backend/src/Postly.Api/wwwroot`.
  - Source files were fixed, but e2e still hit old `index-*.js` assets until the frontend was rebuilt and mirrored into `wwwroot`.
  - This caused false conclusions and repeated hypothesis changes.
- **Signal-to-noise ratio was poor**
  - EF/backend logs flooded the terminal and obscured the actual Playwright assertions.
  - Full-suite reruns happened before the current hypothesis was fully validated in the served app.
  - Some errors were expected noise, like the notifications test's intentional rejected mock, but they still looked like active failures.
- **Shared helpers and tests were too coupled to optimistic readiness assumptions**
  - `signIn()` assumed a fully ready home surface, which became brittle when the route shell loaded before data.
  - The continuation tests initially waited on structure in ways that did not distinguish "route mounted" from "data loaded".
- **Tooling friction added waste**
  - ESLint raced Playwright's transient `test-results` directory.
  - Playwright local config still carried some expensive diagnostics that are valuable in CI but costly during repeated local debug loops.

## Why This Happened
- **Missing React 19 hook guidance in the codebase**
  - There is no strong local convention for when `useEffectEvent` values must be excluded from dependency chains.
  - So a linter-driven fix looked mechanically correct but was semantically wrong.
- **No hard guardrail for stale SPA bundles**
  - E2E depends on a built frontend mirrored into backend `wwwroot`, but that dependency is implicit.
  - There is no automatic "frontend source changed -> served assets rebuilt" guarantee before backend-hosted e2e runs.
- **Insufficient separation of test layers**
  - Route-shell presence, data fetch completion, continuation behavior, and full user flow were not clearly staged in the debug process.
  - That made failures appear broader than they were.
- **Operational defaults favored observability over iteration speed**
  - Good for CI, expensive for local debugging.

## Improvements to Implement
- **Codify React 19 effect-event rules**
  - Add a short repo guideline for `useEffectEvent` usage:
    - do not add effect-event functions to bootstrap effect dependencies;
    - prefer one-shot bootstrap effects with an inline comment when needed;
    - treat effect-event values as special, not ordinary callbacks.
  - Add a targeted code comment near `useContinuationCollection.reset` documenting this expectation.
- **Make e2e always run against fresh frontend assets**
  - Best fix: ensure the frontend build and backend asset sync happen automatically before local backend-hosted Playwright runs.
  - Practical options:
    - add a wrapper script for `test:e2e` that runs `npm run build` and `dotnet build backend/src/Postly.Api/Postly.Api.csproj` first;
    - or make the Playwright web server command call a repo-level script that refreshes the SPA bundle before starting the backend.
  - This is the single biggest process improvement.
- **Separate page-shell readiness from data readiness in tests**
  - Keep route-level `data-testid` hooks available for loading/error/content shells.
  - In helpers, wait for route shell after navigation/sign-in, not for unrelated fully-loaded content unless the helper is specifically about that content.
  - In continuation tests, explicitly poll for card counts before asserting pagination behavior.
- **Reduce local debug overhead by default**
  - Keep local Playwright trace off unless explicitly requested.
  - Keep backend logs at `Warning` in local e2e.
  - Preserve richer diagnostics in CI only.
- **Stabilize lint/test interaction**
  - Ignore transient e2e artifact directories such as `test-results` in ESLint.
  - Avoid running lint concurrently with tools that create/delete ignored directories unless the ignore config fully covers them.
- **Use a stricter debug sequence**
  - For regressions that span unit + e2e:
    1. fix and run the smallest failing unit test;
    2. run the smallest related component/integration set;
    3. rebuild served assets if e2e depends on backend-hosted SPA;
    4. run the smallest affected e2e spec;
    5. only then run full suites.
  - This would have cut a large amount of repeated runtime and token usage.
- **Add one or two cheap guardrail checks**
  - A tiny e2e smoke check or helper assertion that verifies the expected built asset hash changed after rebuild would catch "stale bundle" earlier.
  - A small unit test or documented pattern around bootstrap effects using `reset()` from `useContinuationCollection` would catch future dependency regressions earlier.

## Test and Maintenance Practices
- **When touching continuation/bootstrap code**
  - Always run:
    - the continuation hook test,
    - the three continuation UI tests,
    - the direct-post/timeline tests that touch reload behavior.
- **When touching backend-hosted frontend behavior**
  - Rebuild frontend assets before concluding anything from Playwright.
- **When fixing lint warnings in hook-heavy files**
  - Treat linter suggestions as proposals, not truth.
  - Verify whether the symbol is a stable setter, ordinary callback, or effect event before changing dependencies.

## Assumptions and Defaults
- Assume backend-served e2e remains the project model.
- Assume React 19 `useEffectEvent` remains in use and should be treated as a special-case API in local conventions.
- Assume local iteration speed matters more than local trace/log exhaustiveness, while CI should retain stronger diagnostics.
