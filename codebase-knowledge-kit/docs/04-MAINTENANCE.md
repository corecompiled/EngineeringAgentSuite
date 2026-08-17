# Maintenance — Keeping the Knowledge Base Fresh

A stale knowledge base is worse than none: the agent will confidently cite outdated docs. The good news: maintenance is cheap if it's habitual.

## The three freshness rules

1. **API surface = regenerate, never edit.** `docs/api-surface/` is machine output. Re-run the dumper:
   - after any large merge/release,
   - on a schedule (e.g., every Monday),
   - whenever an answer contradicts it.

   ```powershell
   cd C:\work\everything
   git pull --all   # or update each repo
   dotnet run --project tools\ApiSurfaceDumper -- Everything.sln docs\api-surface
   ```

2. **Architecture docs = edit like code.** Add one line to your PR/definition-of-done checklist:
   > "Did this change how a module behaves or what it exposes? If yes, update its doc in the knowledge vault's `architecture/` folder (or ask Kiro to)."

   A 3-sentence edit is enough; these docs should stay ~2 pages each.

3. **Let the agent report drift.** Recipe 1 in `03-DAILY-USAGE.md` ends with "list anything the architecture doc got wrong" — when it flags something, fix the doc immediately:
   ```
   Update the vault's architecture/<module>.md to reflect what we just confirmed: <finding>. Keep the edit minimal.
   ```

## Quarterly review (30–60 min)

- Skim `system-overview.md` — still accurate at the map level?
- Delete or merge module docs for retired code.
- Re-run the dumper and diff `index.md` against last quarter — the dependency graph diff shows architectural drift at a glance.
- Prune steering: if a steering file hasn't proven useful, remove it. Thin steering is healthy steering.

## Automation (some now shipped, some optional)

- **Shipped hooks** (`.kiro/hooks/knowledge-base-hooks.json`, walkthrough in `05-USING-IN-KIRO.md`): a `.csproj`-save nudge to refresh api-surface, an auto-guard that completes freshness stamps on new knowledge notes, and an **opt-in** session-start freshness check — enable that one in the Agent Hooks panel once you're comfortable with the small per-session cost. Deliberately no hooks on `*.cs` saves: they'd fire constantly and burn attention.
- **Scheduled dump + check** (recommended next step): a weekly scheduled task or ADO pipeline that pulls all repos, re-runs the dumper, runs `Check-DocFreshness.ps1` (its exit code 1 on STALE makes it a usable gate), and opens a PR with the api-surface diff — the diff itself is a great review artifact.
- **Doc lint prompt**: monthly, ask Kiro: "Compare the vault's architecture/<module>.md against the current code with Serena; list inaccuracies." Fix what it finds.

## Signs of rot (act when you see them)

- Agent answers contradict the code more than rarely.
- `docs/api-surface/index.md` timestamp is more than a few weeks old during active development.
- Module docs reference types that no longer exist (the dumper diff will reveal this).

## Staleness detection — deterministic, not vibes

Two guarantees stack up:

- **Soft guarantee (every answer):** steering rule 9 orders trust as live code > api-surface > architecture docs > knowledge notes, and Serena always reads the *current* working tree — so even a stale doc mostly costs a slower start, not a wrong answer, and rule 5 makes the agent flag the doc drift it finds.
- **Hard guarantee (scheduled):** every generated doc and knowledge note carries front matter (`repo`, `source-commit`, `watch-paths` — the prompts add it automatically). Run:

  ```powershell
  pwsh tools/Check-DocFreshness.ps1
  ```

  (from the umbrella root — by default it reads the stamped docs from the vault's `architecture/` and `knowledge/` folders). It asks git which watched files changed since each doc's stamp and rates every doc **FRESH / DRIFTING / STALE** (plus flags unstamped ones). Exit code 1 on any STALE, so it drops straight into a weekly scheduled task or ADO pipeline. Act on it: STALE module doc → re-run prompt 01 for that module; STALE knowledge note → re-verify with Serena, update its stamp or mark it `superseded`.

Combined with the api-surface diff (rule 1 above), staleness is now *measured*, not guessed.

## Knowledge notes (the vault's `knowledge/` folder) hygiene

Resolved investigations are captured with `prompts/05-save-finding.md` (1 minute at the end of a session) and retrieved automatically via steering rule 8. Keep the pile healthy:

- **Re-verify before reuse** — the note's `Reuse guard` section and the freshness checker enforce this; never trust a note whose watch-paths moved.
- **Promote, don't hoard**: when the same note gets reused 2–3 times, move its content into the proper `architecture/<module>.md` in the vault and mark the note `superseded`. Notes are a staging area for durable knowledge, not its final home.
- **Supersede, don't contradict**: one `verified` note per topic; older takes get `status: superseded` with a link forward.
- **Prune quarterly**: delete superseded notes older than ~6 months (keep the vault under version control — see below — so history survives pruning).
- **Browsing them in Obsidian** works out of the box: the vault *is* `~/Documents/Engineering Knowledge Base` — the same Obsidian vault that holds the personal agent suite's `investigations/` notes; the kit's `architecture/` and `knowledge/` folders appear right beside them. Obsidian is a viewer/editor over the same files, not a second store. In Obsidian settings, set links to standard markdown format (not `[[wikilinks]]`) so agents, ADO, and GitHub can still resolve them; the YAML front matter doubles as Obsidian properties for filtering by tag/status. The vault sits outside the code repos — give it its own backup/sync (making the vault folder its own git repo works well); avoid third-party sync services for work content.

## Ownership

Name one owner (probably you, initially). The mechanism survives handover well — everything is markdown + one small tool in the repo — but only if someone owns the habit.
