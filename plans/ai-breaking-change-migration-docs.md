# Proposal: Automated AI Migration Guide for Breaking Changes

## Problem

Terminal.Gui is an actively developed library with breaking API changes between releases.
AI coding agents (GitHub Copilot, Claude Code, Cursor, and others) assist users in building
apps with Terminal.Gui. However, AI agents have a training data cutoff — they are unaware
of breaking changes that happened after they were trained.

This gap causes real problems:

- Agents suggest deprecated or removed APIs
- Agents cannot guide users through non-obvious migrations (e.g., a renamed method,
  a restructured architecture, a changed event signature)
- Users waste time debugging compile errors or runtime failures caused by stale AI advice

Breaking changes are already labeled `breaking-change` on all relevant PRs. The information
needed for migration guidance (what changed, why, and how to update) is typically written
in the PR description at merge time. **The bottleneck is making that information available
to AI agents in a structured, discoverable way.**

---

## Proposed Solution

### 1. A Dedicated Migration Document for AI Agents

Create `docfx/docs/breaking-changes.md` — a structured, continuously-updated markdown
file that lists every breaking change merged into the library, along with step-by-step
migration guidance and before/after code examples.

This file is the single source of truth for breaking change migration. It is:
- Written in plain markdown (universally readable)
- Referenced from all AI agent instruction files in the repo
- Kept up to date automatically by the workflow below

### 2. A GitHub Actions Workflow

Create `.github/workflows/breaking-changes.yml` that triggers automatically when a PR
labeled `breaking-change` is merged into `v2_develop` or `v2_release`.

The workflow:
1. Reads the merged PR's title, number, author, and body
2. Extracts the `## Migration Steps` section from the PR body
3. Builds a formatted, timestamped entry
4. **Prepends** the entry to `breaking-changes.md` (newest entries first)
5. Commits and pushes the update with a bot identity

If the `## Migration Steps` section is missing from the PR body, the workflow:
- Creates a minimal stub entry with PR metadata
- Posts a comment on the PR asking the author to fill in the migration details

No PAT or elevated permissions are required — `GITHUB_TOKEN` with `contents: write` is sufficient.

### 3. Updated PR Template

Update `pull_request_template.md` to include a `## Migration Steps` section with
instructional placeholder text. This prompts authors of breaking-change PRs to provide:
- A description of what changed and why
- Before/after code examples
- Any affected APIs or files to be aware of

### 4. Updated AI Agent Instruction Files

Add a prominent reference to `breaking-changes.md` in all AI agent instruction files:

| File | Purpose |
|------|---------|
| `AGENTS.md` | Read by all agents (universal) |
| `CLAUDE.md` | Claude Code |
| `.github/copilot-instructions.md` | GitHub Copilot |
| `llms.txt` | Machine-readable LLM context |

Each file will include a note directing the agent to consult `breaking-changes.md`
before suggesting code that uses Terminal.Gui APIs.

---

## Document Format

Each breaking change entry will follow this structure:

```markdown
---

## Breaking Change: [PR Title] — PR #1234

**Merged:** 2025-06-15 | **Author:** @username | [View PR](https://github.com/...)

**Changed Files:**
- `Terminal.Gui/Views/Button.cs`
- `Terminal.Gui/ViewBase/View.cs`

### What Changed

[Narrative description from PR body]

### Migration Steps

**Before:**
\`\`\`csharp
// Old API
button.Clicked += () => { ... };
\`\`\`

**After:**
\`\`\`csharp
// New API
button.Accepting += (sender, e) => { ... };
\`\`\`

### Notes

[Any additional context, edge cases, or related changes]
```

---

## Implementation Steps

| # | Task | File(s) |
|---|------|---------|
| 1 | Create initial `breaking-changes.md` with header and intro section | `docfx/docs/breaking-changes.md` |
| 2 | Write Python parser/formatter script | `.github/scripts/update-breaking-changes.py` |
| 3 | Write GitHub Actions workflow | `.github/workflows/breaking-changes.yml` |
| 4 | Add `## Migration Steps` section to PR template | `pull_request_template.md` |
| 5 | Add reference to `breaking-changes.md` in agent instruction files | `AGENTS.md`, `CLAUDE.md`, `.github/copilot-instructions.md`, `llms.txt` |

---

## Why This Approach

| Concern | Decision |
|---------|----------|
| **Universality** | Plain markdown — readable by any AI agent, no agent-specific tooling |
| **Discoverability** | Referenced from all AI instruction files already read at context init |
| **Automation** | GitHub Actions — no manual steps after merging a PR |
| **PR author burden** | Low — a single template section to fill in |
| **Maintenance** | Zero — the workflow maintains the document automatically |
| **Rollback** | Document is in version control — bad entries can be reverted |

### Alternatives Considered

**Post to GitHub Discussions/Wiki instead:** AI agents don't automatically read those.
The agent instruction files (AGENTS.md, etc.) are the established integration point.

**Use GitHub API at agent query time:** Would require agents to make API calls dynamically.
Most agents don't support this without custom plugins.

**Require no PR body section:** Auto-generating useful migration steps from a diff alone
is unreliable. The PR author has unique context; asking them to write one section is the
right tradeoff.

---

## Rollout

1. Merge this implementation to `v2_develop`
2. Backfill past `breaking-change` PRs manually (or via script) as a one-time effort
3. The workflow handles all future PRs automatically

Backfilling can be done by running the Python script locally against a list of historical
breaking-change PRs using the GitHub CLI.

---

## Open Questions for the Team

1. **Document location:** `docfx/docs/breaking-changes.md` puts it in the published docs site.
   Is that the right home, or should it be in a separate agent-only location (e.g., `.tg-docs/`)?

2. **Backfill:** Do we want to backfill historical breaking changes, and if so, how far back?

3. **PR template enforcement:** Should the `## Migration Steps` section be required (CI check)
   for PRs labeled `breaking-change`, or remain advisory?

4. **Scope:** Should this cover `v2_develop` only, or also `v2_release`?
