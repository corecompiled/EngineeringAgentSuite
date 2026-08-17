# Prompt 02 — Generate a Repo Contract Doc

Paste everything below the line into Kiro. Replace the placeholder. Run once per repo in the umbrella workspace. Review before committing — the "Known consumers" table especially needs your human knowledge.

---

You are documenting the external contract of one repository: what it exposes, and who depends on it. This doc is what everyone checks before changing anything public.

TARGET REPO FOLDER: <e.g. C:/work/everything/main-repo or ./main-repo>

TASK
Produce `contracts/<repo-name>.md` in the knowledge vault's `architecture/` folder (`~/Documents/Engineering Knowledge Base/architecture/contracts/`) following `docs/templates/repo-contract-template.md` exactly (keep all section headings).

METHOD — follow strictly:
1. From `docs/api-surface/index.md`, list this repo's projects and their dependency edges (both directions: what they use, and which other projects in the workspace reference them).
2. From `docs/api-surface/projects/*.md` for this repo's projects, summarize the public surface: the important namespaces and headline types — do NOT copy the full member lists, link to the dump files instead.
3. Identify externally-exposed surface: anything served to clients outside this workspace (native API, COM visibility, HTTP endpoints, exported interop, files/DB schemas acting as contracts). Use Serena and targeted source inspection of likely hosting/bootstrap code to locate WHERE the exposure is implemented, and cite paths.
4. For the "Known consumers" table: fill what is provable from workspace references; add a row `EXTERNAL — humans must complete this` for consumers outside the workspace.

RULES
- Compiler-verified facts (from api-surface) vs. inferences must be distinguishable — prefix inferences with "Likely:".
- Anything not determinable: `UNKNOWN — needs human input`.
- Max ~2 pages. Cite file paths for all exposure claims.
- Start the file with YAML front matter for the freshness checker: `generated:` (today), `repo:` (this repo's folder name), `source-commit:` (run `git -C <repo> rev-parse --short HEAD`), `watch-paths:` (the folders that define this repo's exposed surface).
- Write the file, then list the top 3 risks you noticed in this repo's exposed surface (e.g., very widely referenced types, undocumented public members).
