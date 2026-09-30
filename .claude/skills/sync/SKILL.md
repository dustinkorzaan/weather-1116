---
name: sync
description: Bring the current story branch up to date with main after other PRs merged - merge origin/main (never rebase), adapt to overlapping changes, verify, re-review, update spec and PR, push and follow CI. Use when the user says /sync, "update/refresh/rebase the branch", or "main moved". Args - optional PR number or branch name; --light for merge + verify + push only.
---

# /sync: update a story branch with main and re-check it

Other PRs have merged. Bring this branch onto the new `main` so it is correct against what's there now, not just so it compiles.

**Rules:** these come from `.claude/skills/steward/SKILL.md`.
- **Merge** `origin/main`. Never rebase, amend or force-push. (A user saying "rebase" means "update"; merge.)
- Never merge the PR.
- Never skip or loosen a test.
- Every fix loop is capped at **3** rounds; after that, stop and report.

## 0. Locate

- **Branch:** if the args name a PR (`#123`) or a branch, switch to that branch (`git fetch origin <branch>`, `git switch <branch>`). Otherwise use the current branch. Refuse to run on `main`.
- **Spec:** `git diff --name-only --diff-filter=A origin/main...HEAD -- docs/specs/`. There may be none, for a non-`/ship` branch.
- **PR:** find the open PR for this branch.
- **Clean tree:** `git status` must be clean. Commit finished work first, or report and stop if it's half-done.
- **Old base:** record `OLD_BASE=$(git merge-base HEAD origin/main)` *before* fetching.

## 1. Merge

1. `git fetch origin main`, then `git merge origin/main`.
2. If it's already up to date, say so, run `scripts/verify.sh`, and stop.
3. On conflicts:
   - Resolve each one so **both sides' intent** survives.
   - For `package-lock.json`: take main's version, then run `npm install --prefix <dir>`. Never hand-edit a lockfile.
   - Generated files: regenerate them, don't hand-merge them.
   - Ask the user only when both sides changed the same logic and either choice loses behaviour. Explain the trade-off and recommend one.
4. Commit the merge. Keep the default message and add a short conflict note.

## 2. Overlap scan: adapt, don't just compile

1. `git diff --name-only $OLD_BASE origin/main` lists what main changed.
2. Intersect that with this branch's files (`git diff --name-only origin/main...HEAD` from before the merge) **and** the areas this story depends on:
   - endpoints and handlers it calls;
   - MCP tool names and their hosts;
   - Core/CQMediator contracts;
   - parity counterparts in the other UIs;
   - config and env vars.
3. Read the overlapping changes on main. Adapt this branch to them: renamed symbols, changed signatures, moved files, new parity requirements, a behaviour this story assumed that has since changed.
4. Keep a list of `overlap → adaptation` for the report and the spec.

## 3. Verify

- Run `scripts/verify.sh --all`.
- If it's red: fix the root cause and re-run, up to 3 rounds. After that, stop, add `## Open issues` to the spec, and report.

## 4. Re-review

Skip this step with `--light`, or when there were no conflicts **and** step 2 found no overlap.

1. Spawn `peer-reviewer` with the spec path. Tell it to focus on the conflict files and the overlap list.
2. Fix BLOCKING and SHOULD findings, verify again (≤ 3 rounds).
3. Spawn `final-reviewer` against the spec. REWORK → fix → verify → final review again (≤ 3 rounds).

## 5. Record and push

1. **Spec**, if present: append a Review log row: `| n | sync with main (<short sha>) | <conflicts / overlaps> | <resolution, commits> |`. Update Status if it changed.
2. Commit, then `git push`.
3. **PR body:** refresh the Verify table, and add a line under Review notes, e.g. "Synced with main at `<sha>`: <one-line summary>".
4. **Follow-through:**
   - Subscribe to PR activity.
   - Schedule a `send_later` check-in about 60 minutes out: "Re-check PR #N per the steward skill".
   - Follow the steward skill until CI is green on the new head.

## 6. Report (one short message)

- **Merged:** `main` at `<sha>` (N commits / PRs brought in).
- **Conflicts:** file → how it was resolved; or "none".
- **Adaptations:** overlap → change; or "none needed".
- **Verify:** the summary table.
- **Reviews:** peer and final verdicts, or "skipped (no overlap)".
- **CI:** current status, and the PR link.

## Several PRs waiting on each other

Sync and merge them **one at a time**. After each merge, run `/sync` on the next one. Syncing them all in parallel means each one conflicts again as soon as the one before it merges.
