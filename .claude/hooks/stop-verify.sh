#!/bin/bash
# Stop / SubagentStop gate: an agent may not finish while scripts/verify.sh is
# red for what it changed. Fast path when nothing verifiable changed or the tree
# already has a green stamp. Blocks at most 3 times in a row per session/agent,
# then lets the stop through with a loud warning so a hard failure can't loop.
#
# Opt out for a session with CLAUDE_VERIFY_HOOK=off.

set -uo pipefail

MAX_BLOCKS=3
# Read-only roles cannot fix anything, and test-author's acceptance tests are
# expected red until the implementation is merged, so never block these.
EXEMPT_AGENTS=" planner peer-reviewer final-reviewer test-author Explore Plan claude-code-guide statusline-setup "

[[ "${CLAUDE_VERIFY_HOOK:-on}" == "off" ]] && exit 0

input="$(cat)"
field() { jq -r "$1 // empty" <<< "$input" 2>/dev/null; }

agent_type="$(field .agent_type)"
if [[ -n "$agent_type" && "$EXEMPT_AGENTS" == *" $agent_type "* ]]; then
  exit 0
fi

# An implementer that reports STATUS: blocked has already reverted its partial
# edits and is handing the problem back to the orchestrator; let it stop.
if grep -q 'STATUS: blocked' <<< "$(field .last_assistant_message)"; then
  exit 0
fi

cwd="$(field .cwd)"
cd "${cwd:-${CLAUDE_PROJECT_DIR:-.}}" 2>/dev/null || exit 0
git rev-parse --show-toplevel >/dev/null 2>&1 || exit 0
cd "$(git rev-parse --show-toplevel)" || exit 0
[[ -x scripts/verify.sh ]] || exit 0

git_dir="$(git rev-parse --git-dir)"
key="$(field .session_id)-$(field .agent_id)"
counter="$git_dir/claude-verify-blocks-${key//[^A-Za-z0-9_-]/_}"

# Nothing verifiable changed -> done.
if [[ -z "$(scripts/verify.sh --list --changed 2>/dev/null | grep -v '^verify:')" ]]; then
  rm -f "$counter"
  exit 0
fi

# Unchanged since the last green run -> done.
stamp_now="$(scripts/verify.sh --stamp)"
if [[ -f "$git_dir/claude-verify-green" && "$(cat "$git_dir/claude-verify-green")" == "$stamp_now" ]]; then
  rm -f "$counter"
  exit 0
fi

output="$(scripts/verify.sh --changed 2>&1)"
if [[ $? -eq 0 ]]; then
  rm -f "$counter"
  exit 0
fi

blocks=$(( $(cat "$counter" 2>/dev/null || echo 0) + 1 ))
echo "$blocks" > "$counter"

if (( blocks > MAX_BLOCKS )); then
  rm -f "$counter"
  jq -n --arg m "VERIFY STILL RED after $MAX_BLOCKS fix attempts - stopping anyway. Report the failing checks to the user; do not claim the work is done." \
    '{systemMessage: $m}'
  exit 0
fi

{
  echo "scripts/verify.sh is RED for your changes (block $blocks of $MAX_BLOCKS). Fix the failures below, re-run scripts/verify.sh, then finish."
  echo "If the failure is genuinely outside your task, say so explicitly in your final report instead of claiming success."
  echo
  echo "$output" | tail -n 120
} >&2
exit 2
