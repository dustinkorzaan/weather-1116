---
name: ship
description: End-to-end multi-agent delivery pipeline for this repo - interview, spec, plan, parallel implementation in worktrees, acceptance tests, verify, peer review, final review, rework (max 3 rounds each), draft PR, then PR follow-through. Use when the user says /ship, "build/implement/fix <story>", or hands over a feature/bug to do hands-off. Args - the story text; optional flags --hands-off (no questions), --quick (small single-stack change).
---

# /ship: orchestrated delivery

You are the **orchestrator**. You own the branch, the spec, the merges, the push and the PR. Workers do the focused work.
Stay in the main thread; never hand the orchestration itself to a subagent.

**Hard limits:**
- **N = 3** rework rounds per gate (test, peer review, final review). Rounds are counted per gate for the whole run: a test-gate pass triggered by a REWORK in phase 6 uses up a test-gate round, it doesn't start a fresh count.
- Never merge a PR (see `AGENTS.md`).
- Never skip or disable a test.
- Never claim done while `scripts/verify.sh --all` is red.

Keep a live checklist with TaskCreate/TaskUpdate, one task per phase. At every phase boundary, post a one-line status (for example, "Plan ready: 4 tasks, 3 parallel. Implementing.").

## Flags

- `--hands-off`: no questions. Make reasonable assumptions and record every one in the spec's **Assumptions**.
- `--quick`: for small, single-stack changes (roughly under 150 lines, no serialized paths). Skip the planner and test-author; you or a single implementer do the work. Verify, one peer review, and the final review still run.
- No flag: the interview is allowed (phase 1), but ask only what you can't infer.

## Phase 0: Setup

1. `git status`. The tree must be clean, or already contain only this story's work.
2. Branch: use the session's designated branch if one was given; otherwise stay on the current non-`main` branch; otherwise `git switch -c claude/<slug>`.
3. `git fetch -q origin main`. If `main` moved, merge it in now (not rebase).

## Phase 1: Interview → spec

1. Read the code the story touches first (Explore agents are fine for broad sweeps). Most questions answer themselves.
2. If a decision genuinely belongs to the user and changes the design, ask **one round** of up to 4 questions with AskUserQuestion. Put your recommended option first. Skip this under `--hands-off`, or when the story already has testable criteria.
3. Write `docs/specs/YYYY-MM-DD-<slug>.md` from `docs/specs/_template.md`. Every acceptance criterion must be **testable** (observable input → output). Commit it: `Add spec: <title>`.

## Phase 2: Plan

- Spawn `planner` with the spec path.
- Read its `## Plan`. Sanity-check the parallel-safe marks against *Parallel work* in `AGENTS.md`.
- If it lists blocking questions: answer them from the code if you can, otherwise ask the user (unless `--hands-off`, in which case pick one and record the assumption).
- Commit the plan.

## Phase 3: Execute (parallel)

1. **Serialized tasks** (parallel-safe: no), in dependency order: spawn one `implementer` at a time in the main tree with `run_in_background: false`, since the next task builds on it. Confirm its commit landed.
2. **Parallel batch**: in a **single message**, spawn:
   - one `implementer` per parallel-safe task with `isolation: "worktree"`;
   - one `test-author` (also `isolation: "worktree"`).

   Give each worker the spec path, its task number, and "commit your work, don't push".

   Before spawning, make sure the spec, the plan and any serialized-task merges are **committed**. Worktrees branch from HEAD (`worktree.baseRef: "head"` in `.claude/settings.json`), so uncommitted work is invisible to them. Put `git branch --show-current` and `git rev-parse HEAD` in every worker prompt, so a worker on a stale base can fast-forward.
3. When the workers return:
   - `git merge --no-ff <worker-branch>` each into your branch, in plan order.
   - Resolve conflicts yourself if trivial; otherwise send the task back to that implementer.
   - Workers reporting `STATUS: blocked`: fix the plan or ask the user, then re-spawn.
4. **Clean up** each merged worker: `git worktree remove --force <path>` and `git branch -D <worker-branch>`. This applies after every worker merge in phases 3-6, not only here. Each worktree carries its own `node_modules`, `bin/` and `obj/` (hundreds of MB), and the session's disk is a fixed allowance, so leftover worktrees from several rework rounds can fill it. Keep a blocked worker's worktree until it is re-spawned or abandoned.

## Phase 4: Test gate (≤ 3 rounds)

- Run `scripts/verify.sh --all`.
- On FAIL: send the failure tail to an implementer ("fix verify failures: …"), merge, re-run.
- Test-author tests that still fail mean the implementation is incomplete; that is an implementer fix, not a test edit. Edit a test only if the test itself contradicts the spec, and log that in the spec.
- After 3 red rounds: stop, write `## Open issues` in the spec, commit, and report to the user. Put the line `STATUS: blocked` in that report: the Stop hook lets a blocked report through instead of re-running verify three more times.

## Phase 5: Peer review (≤ 3 rounds)

1. Spawn `peer-reviewer` with the spec path.
2. For every BLOCKING and SHOULD finding: group the findings by file or area, and send each group to an implementer (in parallel worktrees if the groups don't overlap). Merge, then run verify.
3. Apply NITs only if trivial. Otherwise list them as follow-ups.
4. Append the round to the spec's **Review log** (findings → fix commit or reason).
5. Re-review until the verdict is `clean` or 3 rounds are used. After 3 rounds, commit the remaining BLOCKING items under `## Open issues` and report them to the user with the line `STATUS: blocked`, as in phase 4.
6. **Post the review on the PR** so other agents can reply in thread. A review that stays in chat is not delivered.
   - If the PR is not open yet, post it in phase 7 as soon as the PR exists.
   - One summary comment: verdict, verify table, criteria coverage.
   - One line comment per finding, on the changed line, with the severity, the scenario, and the fix.

## Phase 6: Final review (≤ 3 rounds)

- Spawn `final-reviewer`.
- On `REWORK`: send the rework items to implementers, go through the test gate, then run the final review again.
- After 3 REWORK rounds: stop and report (with `STATUS: blocked`), with no PR marked ready.

## Phase 7: PR and follow-through

1. Make sure the spec's Review log and Open issues are current and committed. Then `git push -u origin <branch>`, retrying on network errors only.
2. Open a **draft** PR:
   - title: imperative, under 70 characters;
   - body: fill `.github/pull_request_template.md` from the spec and the final-reviewer's output (criteria → evidence table, verify table, PR notes).
   - Post the latest peer review if it is not already on the PR: one summary comment, plus one line comment per finding (severity, scenario, fix).
3. Record the PR in the spec: set **Branch / PR** to the PR link and **Status** to `in-review`, commit (`Spec: link PR #N`), and push. The spec is otherwise left with a placeholder link and a stale status once the PR merges.
4. Subscribe to PR activity, then follow `.claude/skills/steward/SKILL.md` for CI and review events.
   - Webhooks can arrive late or not at all, so also schedule a `send_later` check-in about 60 minutes out: "Re-check PR #N per the steward skill".
   - Re-arm it on each check-in until the PR is green with no open threads, or is merged or closed.
5. Final chat message:
   - PR link
   - one-paragraph summary
   - criteria evidence (short)
   - assumptions made
   - open issues and follow-ups

## Worker prompts: keep them self-contained

Workers start cold. Every spawn prompt includes:
- the spec path;
- the exact task or findings;
- which branch or worktree they're on;
- the expected report shape: the one in their agent file.

Never paste the whole conversation. Point them at files instead.
