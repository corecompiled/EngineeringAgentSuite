# System Overview

> Purpose: the map of the whole system. Every codebase question starts here. Target length: 2–3 pages.

## Elevator pitch
One paragraph: what this system does, for whom, in business terms.

## Repo map
| Repo | Role | Key projects | Consumed by |
|---|---|---|---|
| | | | |

## High-level architecture
```mermaid
graph TD
  %% Boxes = modules/repos, arrows = "depends on / calls"
  %% Keep to <= 15 nodes; detail belongs in module docs
```

## The 5–10 modules that matter most
For each: one line — name, responsibility, doc link (`architecture/<module>.md`, vault-relative).

## How a typical operation flows
Pick the 1–2 most representative operations (e.g., "user saves an order") and narrate the path across modules in ~10 steps.

## External touchpoints
- Third-party systems called (which module owns each)
- Public/native API exposed to other clients (link to contracts docs)

## Cross-cutting concerns
Auth, logging, error handling, configuration, transactions — where each is implemented, one line apiece.

## Glossary
Domain terms and internal jargon a newcomer (or an AI agent) must know.

## Known sharp edges
Top gotchas that repeatedly bite people, with links to the module doc that explains each.
