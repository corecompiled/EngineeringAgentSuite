# Prompt 05 — Save This Finding (end of an investigation)

Paste everything below the line into Kiro **in the same conversation** where a question,
ADO item, or ServiceNow item was just resolved. Takes ~1 minute; pays off every time the
topic comes back. (Retrieval is automatic: the steering index tells the agent to search
the vault's `knowledge/` folder before starting new investigations.)

---

Save what we just established as a reusable knowledge note.

TASK
Write `<YYYY-MM-DD>-<short-slug>.md` in the knowledge vault's `knowledge/` folder
(`~/Documents/Engineering Knowledge Base/knowledge/`) following
`docs/templates/knowledge-note-template.md` exactly, based on THIS conversation.

METHOD — follow strictly:
1. Front matter is mandatory. Determine `repo` (the umbrella subfolder the evidence lives in)
   and run `git -C <repo> rev-parse --short HEAD` for `source-commit`. `watch-paths` = the
   folders containing the files cited in the Evidence section (keep it tight — the freshness
   checker watches exactly these). Use today's date for `generated`, set `status: verified`.
2. Answer (short): only what we actually confirmed. If parts remained unresolved, list them
   under Related as open questions — do not present them as answered.
3. Evidence/trace: only paths and symbols that were actually inspected or traced in this
   conversation. No reconstructed-from-memory paths.
4. Check the vault's `knowledge/` folder for an existing note on the same topic: if found, either update it
   (refresh `source-commit`, note what changed) or mark it `status: superseded` linking to the
   new note — never leave two "verified" notes that contradict each other.
5. If this investigation revealed that a doc in the vault's `architecture/` folder is wrong or incomplete,
   also propose that exact doc edit (show a diff-style suggestion; apply it if I confirm).

RULES
- Max 1 page. The note is a pointer to truth, not a transcript.
- No secrets, credentials, ticket customer data, or personal names in the note.
- Finish by outputting: the note's file path, its watch-paths, and (if any) the proposed
  architecture-doc correction.
