#!/usr/bin/env bash
# The one check command for humans and agents. Maps changed paths to the same
# checks .github/workflows/build-test.yml runs, installs missing deps on demand
# (so fresh git worktrees just work), and prints a PASS/FAIL/SKIPPED summary.
#
# Usage:
#   scripts/verify.sh              # checks for everything changed vs origin/main (default)
#   scripts/verify.sh --changed    # same as above
#   scripts/verify.sh --all        # every check CI runs
#   scripts/verify.sh --list [...] # print the checks that would run, run nothing
#   scripts/verify.sh <path>...    # checks for the given paths only
#   scripts/verify.sh --stamp      # print the working-tree stamp (used by hooks)
#
# Exit code: 0 when nothing FAILed, 1 otherwise.
# On a green --changed/--all run it records a stamp of the tree in the git dir
# so .claude/hooks/stop-verify.sh can skip re-running on an unchanged tree.

set -uo pipefail

ROOT="$(git rev-parse --show-toplevel)"
cd "$ROOT"

VENV="${WEATHER_VENV:-$HOME/.venvs/weather}"
LOG_DIR="$(git rev-parse --git-dir)/claude-verify-logs"
STAMP_FILE="$(git rev-parse --git-dir)/claude-verify-green"

mode="changed"
list_only=false
paths=()
for arg in "$@"; do
  case "$arg" in
    --all) mode="all" ;;
    --changed) mode="changed" ;;
    --list) list_only=true ;;
    --stamp) mode="stamp" ;;
    -h|--help) sed -n '2,16p' "$0"; exit 0 ;;
    -*) echo "unknown option: $arg" >&2; exit 2 ;;
    *) mode="paths"; paths+=("$arg") ;;
  esac
done

# ---------------------------------------------------------------------------
# Changed files and tree stamp
# ---------------------------------------------------------------------------

base_ref() {
  git merge-base HEAD origin/main 2>/dev/null \
    || git merge-base HEAD main 2>/dev/null \
    || git rev-parse HEAD
}

changed_files() {
  local base
  base="$(base_ref)"
  {
    git diff --name-only "$base"
    git ls-files --others --exclude-standard
  } | sort -u
}

# Git tree id of the whole working tree (tracked + untracked, minus ignored).
# Content-only, so committing green work keeps the same stamp.
tree_stamp() {
  local idx
  idx="$(mktemp)"
  rm -f "$idx"
  GIT_INDEX_FILE="$idx" git add -A . >/dev/null 2>&1
  GIT_INDEX_FILE="$idx" git write-tree
  rm -f "$idx"
}

if [[ "$mode" == "stamp" ]]; then
  tree_stamp
  exit 0
fi

# ---------------------------------------------------------------------------
# Path -> check mapping
# ---------------------------------------------------------------------------

declare -A CHECKS=()
add() { CHECKS["$1"]=1; }

# Stacks with a .NET test project: dir -> test csproj.
declare -A DOTNET_TESTS=(
  [api-dotnet]=api-dotnet/api.tests/WeatherAPI.Tests.csproj
  [mvc-dotnet]=mvc-dotnet/mvc.tests/WeatherMVC.Tests.csproj
  [worker-dotnet]=worker-dotnet/worker.tests/WeatherWorkerDotNet.Tests.csproj
  [ui-blazor]=ui-blazor/blazor.tests/WeatherBlazor.Tests.csproj
  [mcp-srv-app-service]=mcp-srv-app-service/mcp.tests/WeatherMcpSrvAppService.Tests.csproj
  [mcp-srv-func-app]=mcp-srv-func-app/mcp.tests/WeatherMcpSrvFuncApp.Tests.csproj
)

# Images CI builds in its docker-build job.
DOCKERFILES=(
  api-dotnet/api/Dockerfile
  mvc-dotnet/mvc/Dockerfile
  ui-blazor/blazor/Dockerfile
  worker-dotnet/worker/Dockerfile
  mcp-srv-app-service/mcp/Dockerfile
  mcp-srv-func-app/mcp/Dockerfile
  mcp-srv-python/mcp/Dockerfile
  mcp-srv-node/mcp/Dockerfile
)

map_path() {
  local f="$1" top="${1%%/*}"
  case "$f" in
    cqmediator-dotnet/*|core-dotnet/*|Weather.sln|Directory.*|global.json|NuGet.config|nuget.config)
      add dotnet-all ;;
    FoundryConsoleV[1-5]/*)
      add "dotnet-build:$top" ;;
    FoundryConsoleV[2-4]python/*)
      add "py:$top" ;;
    mcp-srv-python/*)
      add py:mcp-srv-python ;;
    mcp-srv-node/*)
      add node:mcp-srv-node ;;
    ui-react/*)
      add react ;;
    infra/*)
      add infra ;;
    .github/scripts/*|.github/foundry-agents/*)
      add gh-scripts ;;
    *)
      if [[ -n "${DOTNET_TESTS[$top]:-}" ]]; then add "dotnet-test:$top"; fi ;;
  esac
  case "$f" in
    *.sh) [[ -f "$f" ]] && add "shell:$f" ;;
    */Dockerfile) [[ -f "$f" ]] && add "docker:$f" ;;
    .dockerignore) for df in "${DOCKERFILES[@]}"; do add "docker:$df"; done ;;
  esac
  case "$f" in
    *.json) [[ -f "$f" && "$f" != */node_modules/* ]] && add "json:$f" ;;
    *.yml|*.yaml) [[ -f "$f" && "$f" != */node_modules/* ]] && add "yaml:$f" ;;
  esac
}

if [[ "$mode" == "all" ]]; then
  add dotnet-all
  add react
  add node:mcp-srv-node
  add py:mcp-srv-python
  for d in FoundryConsoleV2python FoundryConsoleV3python FoundryConsoleV4python; do add "py:$d"; done
  add infra
  add gh-scripts
  for df in "${DOCKERFILES[@]}"; do add "docker:$df"; done
  for f in .github/workflows/*.yml; do add "yaml:$f"; done
else
  if [[ "$mode" == "paths" ]]; then
    files="$(printf '%s\n' "${paths[@]}")"
  else
    files="$(changed_files)"
  fi
  while IFS= read -r f; do
    [[ -n "$f" ]] && map_path "$f"
  done <<< "$files"
fi

# dotnet-all subsumes every per-stack .NET check.
if [[ -n "${CHECKS[dotnet-all]:-}" ]]; then
  for k in "${!CHECKS[@]}"; do
    [[ "$k" == dotnet-test:* || "$k" == dotnet-build:* ]] && unset "CHECKS[$k]"
  done
fi

ORDER=()
if [[ ${#CHECKS[@]} -gt 0 ]]; then
  mapfile -t ORDER < <(printf '%s\n' "${!CHECKS[@]}" | sort)
fi

if [[ ${#ORDER[@]} -eq 0 ]]; then
  echo "verify: no checks apply to the changed files (docs/config only)."
  if [[ "$mode" != "paths" && "$list_only" == false ]]; then
    echo "$(tree_stamp)" > "$STAMP_FILE"
  fi
  exit 0
fi

if [[ "$list_only" == true ]]; then
  printf '%s\n' "${ORDER[@]}"
  exit 0
fi

# ---------------------------------------------------------------------------
# Dependency helpers
# ---------------------------------------------------------------------------

ensure_node_modules() {
  local dir="$1"
  if [[ ! -d "$dir/node_modules" ]]; then
    echo "  installing npm deps in $dir"
    npm ci --prefix "$dir" --no-audit --no-fund --loglevel=error
  fi
}

ensure_venv() {
  if [[ ! -x "$VENV/bin/python" ]]; then
    echo "  creating python venv at $VENV"
    local py
    py="$(command -v python3.12 || command -v python3)"
    "$py" -m venv "$VENV" || return 1
  fi
  # Installs deps once; PYTHONPATH below makes pytest import this checkout's
  # sources even when the editable install points at another worktree.
  if ! "$VENV/bin/python" -c "import pytest, pytest_asyncio, mcp, openai" 2>/dev/null; then
    echo "  installing python deps into $VENV"
    "$VENV/bin/python" -m pip install -q \
      -e "./mcp-srv-python/mcp[dev]" \
      -e "./FoundryConsoleV2python[dev]" \
      -e "./FoundryConsoleV3python[dev]" \
      -e "./FoundryConsoleV4python[dev]"
  fi
}

# First interpreter that can import PyYAML: system python3, then the weather
# venv. Installs only PyYAML (never the full venv) when neither has it.
yaml_python() {
  local py
  for py in python3 "$VENV/bin/python"; do
    if "$py" -c 'import yaml' 2>/dev/null; then echo "$py"; return 0; fi
  done
  for py in "$VENV/bin/python" python3; do
    "$py" -m pip --version >/dev/null 2>&1 || continue
    echo "  installing PyYAML for $py" >&2
    if "$py" -m pip install -q pyyaml >&2 2>&1 \
        || "$py" -m pip install -q --user pyyaml >&2 2>&1; then
      "$py" -c 'import yaml' 2>/dev/null && { echo "$py"; return 0; }
    fi
  done
  echo "no Python with PyYAML, and pip could not install it" >&2
  return 1
}

# ---------------------------------------------------------------------------
# Check runners (each returns 0 pass, 1 fail, 3 skipped)
# ---------------------------------------------------------------------------

run_check() {
  local id="$1"
  case "$id" in
    dotnet-all)
      dotnet build Weather.sln -c Release --nologo -v q -clp:ErrorsOnly \
        && dotnet test Weather.sln -c Release --no-build --nologo -v q ;;
    dotnet-test:*)
      dotnet test "${DOTNET_TESTS[${id#dotnet-test:}]}" -c Release --nologo -v q ;;
    dotnet-build:*)
      local dir="${id#dotnet-build:}"
      dotnet build "$dir"/*.csproj -c Release --nologo -v q -clp:ErrorsOnly ;;
    react)
      ensure_node_modules ui-react \
        && npm --prefix ui-react run build \
        && npm --prefix ui-react test -- --run ;;
    node:mcp-srv-node)
      ensure_node_modules mcp-srv-node \
        && npm --prefix mcp-srv-node run typecheck \
        && npm --prefix mcp-srv-node test -- --run ;;
    py:mcp-srv-python)
      ensure_venv \
        && (cd mcp-srv-python && PYTHONPATH="$ROOT/mcp-srv-python/mcp${PYTHONPATH:+:$PYTHONPATH}" \
              "$VENV/bin/python" -m pytest -q mcp.tests) ;;
    py:*)
      local dir="${id#py:}"
      ensure_venv \
        && (cd "$dir" && PYTHONPATH="$ROOT/$dir${PYTHONPATH:+:$PYTHONPATH}" \
              "$VENV/bin/python" -m pytest -q) ;;
    infra)
      jq empty infra/main.parameters.json || return 1
      if command -v bicep >/dev/null 2>&1; then
        local f
        # Same file set and command as CI's validate-bicep job.
        for f in $(find infra -name '*.bicep' | sort); do
          bicep build "$f" --stdout >/dev/null || return 1
        done
      else
        echo "bicep CLI not installed; Bicep build left to CI"
        return 3
      fi ;;
    docker:*)
      local df="${id#docker:}"
      if ! docker info >/dev/null 2>&1; then
        echo "no Docker daemon; image build for $df left to CI"
        return 3
      fi
      docker build -q -f "$df" . >/dev/null ;;
    gh-scripts)
      bash .github/scripts/deploy-foundry-toolbox.test.sh \
        && bash .github/scripts/deploy-foundry-agent.test.sh ;;
    shell:*)
      local f="${id#shell:}"
      bash -n "$f" || return 1
      if command -v shellcheck >/dev/null 2>&1; then shellcheck -S warning "$f"; fi ;;
    json:*)
      local f="${id#json:}"
      # tsconfig/launchSettings-style files may carry comments; only strict-check the rest.
      case "$f" in
        */tsconfig*.json|*/launchSettings.json|.vscode/*|.devcontainer/*) return 0 ;;
      esac
      jq empty "$f" ;;
    yaml:*)
      # Parse only (no workflow schema), but strict about duplicate keys:
      # PyYAML lets the last key win, while Actions rejects the file.
      local f="${id#yaml:}" py
      py="$(yaml_python)" || return 1
      "$py" - "$f" <<'PY' ;;
import sys, yaml

class StrictLoader(yaml.SafeLoader):
    pass

def construct_mapping(loader, node, deep=False):
    seen = set()
    for key_node, _ in node.value:
        if key_node.tag == "tag:yaml.org,2002:merge":
            continue  # "<<" merge keys may repeat and may be overridden
        key = loader.construct_object(key_node, deep=deep)
        if key in seen:
            raise yaml.constructor.ConstructorError(
                None, None, f"duplicate key {key!r}", key_node.start_mark)
        seen.add(key)
    return yaml.SafeLoader.construct_mapping(loader, node, deep)

StrictLoader.add_constructor(
    yaml.resolver.BaseResolver.DEFAULT_MAPPING_TAG, construct_mapping)
with open(sys.argv[1]) as fh:
    list(yaml.load_all(fh, Loader=StrictLoader))
PY
    *)
      echo "unknown check $id"; return 1 ;;
  esac
}

mkdir -p "$LOG_DIR"
declare -A RESULT=()
failed=0
echo "verify ($mode): ${ORDER[*]}"
for id in "${ORDER[@]}"; do
  log="$LOG_DIR/$(echo "$id" | tr '/:' '__').log"
  start=$SECONDS
  printf '  %-40s ' "$id"
  run_check "$id" > "$log" 2>&1
  rc=$?
  dur=$((SECONDS - start))
  case $rc in
    0) RESULT[$id]="PASS"; echo "PASS (${dur}s)" ;;
    3) RESULT[$id]="SKIPPED"; echo "SKIPPED (${dur}s): $(tail -n1 "$log")" ;;
    *) RESULT[$id]="FAIL"; failed=1; echo "FAIL (${dur}s) -> $log" ;;
  esac
done

if [[ $failed -ne 0 ]]; then
  echo
  for id in "${ORDER[@]}"; do
    [[ "${RESULT[$id]}" == "FAIL" ]] || continue
    log="$LOG_DIR/$(echo "$id" | tr '/:' '__').log"
    echo "===== FAIL: $id (last 60 lines of $log) ====="
    tail -n 60 "$log"
    echo
  done
fi

echo
echo "| check | result |"
echo "|---|---|"
for id in "${ORDER[@]}"; do echo "| $id | ${RESULT[$id]} |"; done
if [[ -n "${CHECKS[dotnet-all]:-}" && -z "${CORE_TESTS_SQL_CONNECTION_STRING:-}" ]]; then
  echo
  echo "note: Core [SqlServerFact] tests self-skip without CORE_TESTS_SQL_CONNECTION_STRING; CI runs them against SQL Server."
fi

if [[ $failed -eq 0 && "$mode" != "paths" ]]; then
  tree_stamp > "$STAMP_FILE"
fi
exit $failed
