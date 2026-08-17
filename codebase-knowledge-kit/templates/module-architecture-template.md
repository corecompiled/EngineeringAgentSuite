# Module: <NAME>

> Target length: <= 2 pages. Ground every claim in code (cite paths) or api-surface dumps. Mark unknowns as UNKNOWN rather than guessing.

## Purpose
What this module is responsible for, in 2–4 sentences. What it deliberately does NOT do.

## Where it lives
Repo(s), project(s), root folder(s). Cite the api dump as a plain-text workspace path (`docs/api-surface/projects/<Project>.md`, relative to the umbrella root — not a link; api-surface lives in the workspace, not the vault).

## Key types and entry points
| Type / member | Role | Defined in |
|---|---|---|
| | | |

Entry points = the handful of methods/classes through which the outside world reaches this module (UI handlers, public API methods, message handlers, scheduled jobs).

## Who uses it (callers)
- Internal: which modules/projects call into this one (from api-surface dependency graph + Serena references).
- External: is any of this exposed via the public/native API? (cross-check the repo contract doc)

## What it depends on
Downstream modules, database tables/objects, third-party services — and through which types.

## Core flows
1–3 mermaid sequence or flow diagrams of the most important operations inside this module.

## Data & state
Main entities touched; anything stateful, cached, or transactional worth knowing.

## Invariants & gotchas
Rules that must hold; historical traps; concurrency/threading notes (WinForms UI-thread rules if relevant).

## Test coverage & how to verify changes
Where the tests are; how to exercise this module manually.

## Open questions
Things the generator could not determine — a human should resolve these over time.
