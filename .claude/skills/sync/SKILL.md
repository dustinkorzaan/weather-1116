---
name: sync
description: Bring the current story branch up to date with main after other PRs merged - merge origin/main (never rebase), adapt to overlapping changes, verify, re-review, update spec and PR, push and follow CI. Use when the user says /sync, "update/refresh/rebase the branch", or "main moved". Args - optional PR number (#N) or branch name; --light skips the overlap scan, and skips review unless the merge conflicted or verify needed fixes.
---

# /sync: update a story branch with main and re-check it

Other PRs have merged. Bring this branch onto the new `main` so it is correct against what's there now, not just so it compiles.

**Rules:** these come from `.claude/skills/steward/SKILL.md`.
- **Merge** `origin/main`. Never rebase, amend or force-push. (A user saying "rebase" means "update"; merge.)
- Never merge a PR. The user merges.
- Never skip or loosen a test.
- Every fix loop is capped at **3** rounds; after that, stop and report.

## 0. Locate

1. **Branch.**
   - An arg `#N`: read PR N with the GitHub MCP tool (`pull_request_read`, method `get`) and take its head ref.
   - An arg naming a branch: use that branch.
   - No arg: use the current branch.
   - For a named branch: `git fetch origin <branch>`, `git switch <branch>` (it creates a tracking branch if needed), then `git merge --ff-only origin/<branch>`, so you build on the PR's real tip.
   - If `--ff-only` fails, the local branch has diverged from the PR. Stop and report.
   - Refuse to run on `main`.
2. **Clean tree:** `git status` must be clean. Commit finished work first, or report and stop if it's half-done.
3. **Spec:** `git diff --name-only --diff-filter=A origin/main...HEAD -- docs/specs/`. Branches not made by `/ship` have none; that's fine, see *No spec* below.
4. **PR:** find the open PR for this branch.

## 1. Merge

1. `git fetch origin main`.
2. **After** the fetch, record `OLD_BASE=$(git merge-base HEAD origin/main)`: the branch point on the real `main`.
3. `git merge origin/main`.
   - **Already up to date:** skip to step 3 (verify), then report. There's no push if nothing changed.
   - **Clean merge:** git commits the merge itself. Don't amend it.
   - **Conflicts:** the merge stops. Resolve each conflict so **both sides' intent** survives.
     - For `package-lock.json`: take main's version, then run `npm install --prefix <dir>`. Never hand-edit a lockfile.
     - Generated files: regenerate them, don't hand-merge them.
     - Ask the user only when both sides changed the same logic and either choice loses behaviour. Explain the trade-off and recommend one.
     - Then `git add` the files and `git commit`, adding a short conflict note to the merge message (this is the first commit of the merge, not an amend).

## 2. Overlap scan: adapt, don't just compile

Skip this step under `--light`.

1. `git diff --name-only $OLD_BASE origin/main` lists what main changed since the branch point.
2. Intersect that with this branch's files (`git diff --name-only $OLD_BASE HEAD^1`) **and** the areas this story depends on:
   - endpoints and handlers it calls;
   - MCP tool names and their hosts;
   - Core/CQMediator contracts;
   - parity counterparts in the other UIs;
   - config and env vars.
3. Read the overlapping changes on main. Adapt this branch to them: renamed symbols, changed signatures, moved files, new parity requirements, a behaviour this story assumed that has since changed.
4. Keep a list of `overlap → adaptation` for the report.

## 3. Verify

- Run `scripts/verify.sh --all`.
- If it's red: fix the root cause and re-run, up to 3 rounds. After that, stop and record open issues (see step 5), then report.
- This applies in the already-up-to-date case too.

## 4. Re-review

Runs when **any** of these happened:
- the merge had conflicts;
- step 2 changed files;
- step 3 needed fixes.

Under `--light`, it runs only for conflicts or verify fixes, since step 2 is skipped. Otherwise skip it and say why in the report.

1. Spawn `peer-reviewer`. Pass the spec path if there is one, and tell it to focus on the conflict files, the adaptations and any verify fixes.
2. Fix BLOCKING and SHOULD findings, verify again (≤ 3 rounds).
3. **With a spec only:** spawn `final-reviewer` against the spec. REWORK → fix → verify → final review again (≤ 3 rounds). Without a spec, skip `final-reviewer`: it needs acceptance criteria to judge against.
4. **Post the peer review on the PR** as `.claude/agents/peer-reviewer.md` *Delivery* describes, once the fixes are pushed (step 5). A review that stays in chat is not delivered.

## 5. Record and push

1. **Record.**
   - With a spec: append a Review log row: `| n | sync with main (<short sha>) | <conflicts / overlaps> | <resolution, commits> |`. Put anything left open under `## Open issues`.
   - Without a spec: put the same summary and any open issues in the PR body's *Review notes* section instead.
2. **Commit** only if the tree is dirty (spec edits, fixes): `git status --porcelain` is empty means nothing to commit.
3. **Push:** `git push`, or `git push -u origin <branch>` if it has no upstream yet. Retry only on network errors. If the push is rejected as non-fast-forward, someone pushed meanwhile: `git fetch origin <branch>`, `git merge origin/<branch>`, verify, push again. Never force.
4. **PR body:** refresh the Verify table, and add "Synced with main at `<sha>`: <one-line summary>".
5. **Follow-through:**
   - Subscribe to PR activity.
   - Schedule a `send_later` check-in about 50 minutes out: "Re-check PR #N per the steward skill". Re-arm and stop it as `.claude/skills/ship/SKILL.md` phase 7 says.
   - Follow the steward skill until CI is green on the new head.

## 6. Report (one short message)

- **Merged:** `main` at `<sha>` (N commits / PRs brought in), or "already up to date".
- **Conflicts:** file → how it was resolved; or "none".
- **Adaptations:** overlap → change; or "none needed" / "skipped (--light)".
- **Verify:** the summary table.
- **Reviews:** verdicts, or why they were skipped.
- **CI:** current status, and the PR link.

## No spec

For branches not made by `/ship`:
- the peer review uses the diff and `REVIEW.md` only;
- the final review is skipped;
- the record and open issues go in the PR body.

## Several PRs waiting on each other

Handle them **one at a time**:
1. The user merges one PR.
2. Then run `/sync` on the next one.
3. Repeat.

The agent never merges PRs. Syncing them all in parallel means each one conflicts again as soon as the one before it merges.
