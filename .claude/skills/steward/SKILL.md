---
name: steward
description: Repo rules for driving an open Weather PR to green after it is opened - CI failures, review comments, merge conflicts. Read on every PR activity event or check-in for a PR this session opened or drives. Use also when the user says "babysit/watch/fix CI on the PR".
---

# PR steward: Weather repo

## Never

- Merge, squash, auto-merge or close a PR. The user merges manually (`AGENTS.md`).
- Mark a draft ready for review unless the user asks.
- Rebase, amend, or force-push. Bring `main` in with `git merge origin/main`.
- Skip, disable, or loosen a test to get green.
- Push without running `scripts/verify.sh` (changed mode, or `--all` if Core, CQMediator or the solution changed).

## CI red

1. Read the failing job's log (`mcp__github__get_job_logs`) **before** touching code.
2. Map the job to a local check. Jobs mirror `.github/workflows/build-test.yml`, so `scripts/verify.sh <path>` reproduces the same check.
   - Two things can't be reproduced here:
     - **Core `[SqlServerFact]` tests** need SQL Server (they skip locally without `CORE_TESTS_SQL_CONNECTION_STRING`);
     - **Bicep build** runs in CI only.
   - For those, reason from the log and the diff, and say in the PR comment that the fix was not locally reproducible.
3. Fix the root cause, verify, commit, push.
4. At most **3** fix pushes per distinct failure. After that, comment on the PR once with: the failing check, the root-cause hypothesis, what was tried, and the proposed patch. Then tell the user.
5. If the same check is red on `main`: port an existing fix if there is one. Otherwise comment once saying it isn't this PR's failure.

## Review comments

- Small, local asks (nits, renames, an added test, a one-function refactor): implement, verify, push, reply in-thread with the commit, resolve.
- Larger asks (multi-file refactor, API or schema change, design change): reply with a concrete proposal and ask the user. Don't push them.
- For bot findings, verify each finding first; fix it if real, otherwise reply with the reason.
- Before a push that fixes review threads, reply on each thread naming the change.

## Merge conflicts

Run the `/sync` procedure (`.claude/skills/sync/SKILL.md`). It is also the procedure for a "base branch recovered" notice. In short:

- Merge `origin/main` into the branch and resolve.
- For lockfiles (`package-lock.json`): take `main`'s version, then run `npm install --prefix <dir>` to regenerate. Never hand-edit.
- Run `scripts/verify.sh --all` after any conflict resolution.

## Done means

- CI green on the head commit;
- no conflicts;
- no unanswered review threads.

Then say once that the PR is waiting on the user's review and merge.
