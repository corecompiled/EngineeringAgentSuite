# Repo Contract: <REPO NAME>

> Purpose: what this repo promises to the outside world. This is the doc to check before changing anything public. Target: 1–2 pages.

## What this repo is
One paragraph. Deliverables (libraries? executables? services?).

## Exposed surface (the contract)
What other repos/clients may legitimately consume:
- Public API summary — the important namespaces/types (full compiler-verified list: `docs/api-surface/projects/*.md` for this repo's projects — a workspace path, cited as plain text, not a link)
- Native/external API exposed to non-workspace clients: protocols, versioning, discovery — and WHERE it's implemented
- Any files/DB schemas/queues that act as de-facto contracts

## Known consumers
| Consumer | What they use | Breakage risk notes |
|---|---|---|
| | | |

## What this repo depends on
Other repos in the workspace (from api-surface index graph), external packages that matter, external services.

## Versioning & release
How this repo ships; compatibility rules; deprecation process for public members.

## Change safety checklist
Before changing anything in the exposed surface: (1) find all internal callers via Serena, (2) check consumers table above, (3) ...
