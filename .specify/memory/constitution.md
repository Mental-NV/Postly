<!--
Sync Impact Report
Version change: unversioned template -> 1.0.0
Modified principles:
- Principle 1 placeholder -> I. Modular Boundaries
- Principle 2 placeholder -> II. Explicit Contracts, Validation, and Predictable Errors
- Principle 3 placeholder -> III. Automated Testing Is Mandatory
- Principle 4 placeholder -> IV. UX Consistency by Default
- Principle 5 placeholder -> V. Readable Simplicity and Safe Evolution
Added sections:
- Definition of Done
- Review Workflow
Removed sections:
- None
Templates requiring updates:
- ✅ .specify/templates/plan-template.md
- ✅ .specify/templates/spec-template.md
- ✅ .specify/templates/tasks-template.md
Follow-up TODOs:
- None
-->
# Postly Constitution

## Core Principles

### I. Modular Boundaries
- Frontend, backend, and shared code MUST live behind clear module boundaries with
  a single stated responsibility.
- Cross-module usage MUST go through documented public interfaces; reviews MUST
  reject reach-through imports, cross-layer data access, and shared mutable state
  that bypasses the owning module.
- New shared abstractions SHOULD be introduced only when at least two consumers
  need the same behavior or when they remove duplicated business rules behind a
  stable contract.

Rationale: explicit boundaries reduce change blast radius and keep ownership
reviewable.

### II. Explicit Contracts, Validation, and Predictable Errors
- Every API, job, event, persistence boundary, and reusable module MUST define
  explicit input and output contracts.
- Untrusted input MUST be validated at the boundary where it enters the system;
  invalid input MUST fail fast with deterministic, documented error behavior.
- Error handling MUST surface user-safe messages and machine-actionable details
  appropriate to the layer; silent fallbacks, swallowed exceptions, and
  ambiguous null-style failures MUST NOT be merged.
- Contract changes SHOULD include compatibility notes, migration steps, or both
  when an existing consumer may be affected.

Rationale: explicit contracts keep failures understandable and prevent broken
assumptions from spreading.

### III. Automated Testing Is Mandatory
- Every production change MUST add or update automated tests at the lowest useful
  level and at any affected integration or contract boundary.
- Bug fixes MUST include a regression test that demonstrates the failure mode
  when feasible.
- Pull requests MUST NOT merge while required automated tests are missing,
  failing, or intentionally skipped without maintainer approval recorded in the
  review.
- Test suites SHOULD remain deterministic, readable, and fast enough to run
  during normal development.

Rationale: required automation keeps behavior stable as Postly evolves.

### IV. UX Consistency by Default
- User-visible changes MUST follow existing Postly patterns for interaction,
  layout, copy, and accessibility unless the pull request explicitly introduces
  a reviewed pattern update.
- Each affected surface MUST define loading, empty, success, and error states
  where those states can occur.
- New UI primitives, divergent wording, or novel flows SHOULD be introduced only
  with a documented reason and a clear reuse plan.

Rationale: a consistent interface lowers user friction and reduces accidental
complexity across the product.

### V. Readable Simplicity and Safe Evolution
- Code, schema, and API changes MUST prefer the simplest design that satisfies
  the current requirement and leaves intent obvious in review.
- Reviews MUST reject speculative abstractions, unnecessary indirection, and
  hidden side effects.
- Breaking changes to persisted data, public APIs, or shared contracts MUST
  include a migration, compatibility, or rollback plan before merge.
- Refactors SHOULD improve names, structure, or safety without changing
  behavior unless the behavior change is explicitly specified and tested.

Rationale: readable systems are easier to review, safer to change, and cheaper
to maintain.

## Definition of Done

- Requirements, acceptance scenarios, and affected module boundaries MUST be
  documented in the feature artifacts before merge.
- Contracts, validation rules, and predictable error behavior MUST be
  implemented for every affected boundary.
- Required automated tests MUST be added or updated and MUST pass for the
  changed behavior.
- User-visible work MUST match established Postly patterns and cover relevant
  loading, empty, success, and error states.
- Data, API, and shared-contract changes MUST include compatibility notes plus
  migration or rollback steps when applicable.
- Code submitted for review SHOULD remove dead branches, obsolete TODOs, and
  incidental complexity introduced during delivery.

## Review Workflow

- Plans MUST name the modules being changed, the dependency direction between
  them, and any new public interfaces.
- Specifications MUST describe validation rules, failure modes, and
  user-visible state changes for affected flows.
- Tasks MUST include work for automated tests, validation and error handling,
  and UX verification for each user story.
- Pull requests SHOULD be small enough for a reviewer to evaluate boundary,
  contract, test, and UX impact in one pass; larger changes MUST be split or
  explicitly justified.
- Any intentional exception to this constitution MUST be documented in the pull
  request and approved by at least one maintainer before merge.

## Governance

- This constitution is the default authority for engineering decisions in
  Postly and supersedes conflicting local habits or undocumented review
  preferences.
- Every pull request review MUST verify compliance with the five core
  principles and the Definition of Done.
- Amendments MUST be proposed in a pull request that updates this file and any
  affected templates or guidance artifacts in the same change.
- Amendments MUST include a short rationale and receive approval from at least
  one maintainer before merge.
- Constitution versioning MUST follow semantic versioning.
- A MAJOR version MUST be used when a principle is removed, a mandatory
  requirement is weakened, or governance is redefined in a backward-incompatible
  way.
- A MINOR version MUST be used when a principle or section is added, or when
  required guidance or review gates are materially expanded.
- A PATCH version MUST be used for clarifications, wording improvements, and
  non-semantic refinements.
- The ratification date MUST remain the original adoption date; the last
  amended date MUST be updated whenever the constitution changes.

**Version**: 1.0.0 | **Ratified**: 2026-04-09 | **Last Amended**: 2026-04-09
