#!/usr/bin/env bash
# Client for the Windows Unity CLI runner (Tools/UnityCli/unity-cli.ps1 watch).
# Use it from a shell that can see the repository but cannot start Windows processes (WSL, a Linux VM, Git Bash).
#
#   unity-cli.sh status                         Is the runner alive? What is it doing?
#   unity-cli.sh run <command> [testFilter]     Queue compile|editmode|playmode|smoke|all|sync. Prints the request id.
#   unity-cli.sh wait <id> [timeoutSeconds]     Wait for a result and print its summary (exit 0 ok, 1 failed, 2 timeout).
#   unity-cli.sh exec <command> [testFilter]    run + wait (default timeout 170 s; re-run 'wait <id>' if it times out).
#   unity-cli.sh show <id>                      Print a finished summary again.
#   unity-cli.sh cancel                         Kill the Unity process of the run in progress.
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
CLI="$ROOT/.unity-cli"
REQ="$CLI/requests"
RES="$CLI/results"

summary() {
  local file="$RES/$1.json"
  if command -v python3 >/dev/null 2>&1; then
    python3 - "$file" <<'PY'
import json, sys
s = json.load(open(sys.argv[1], encoding="utf-8-sig"))
print(("OK" if s.get("success") else "FAILED"), s.get("command", ""), s.get("id", ""))
if s.get("error"):
    print("  runner error:", s["error"])
for st in s.get("steps") or []:
    print(f"  {st['step']}: exit {st['exitCode']}, {st['seconds']}s, {len(st.get('errors') or [])} errors, {len(st.get('warnings') or [])} warnings")
    for e in st.get("errors") or []:
        print("    ERROR", e)
    for p in st.get("problems") or []:
        print("    NOTE ", p)
    t = st.get("tests")
    if t:
        print(f"    tests: {t['total']} total, {t['passed']} passed, {t['failed']} failed, {t['skipped']} skipped")
        for f in t.get("failures") or []:
            print("    FAIL", f["name"], "-", (f.get("message") or "").replace("\n", " ")[:400])
    print("    log:", st.get("log"))
PY
  else
    cat "$file"
  fi
}

passed() {
  grep -q '"success":  *true' "$RES/$1.json" 2>/dev/null || grep -q '"success": true' "$RES/$1.json"
}

cmd="${1:-status}"
case "$cmd" in
  status)
    if [[ -f "$CLI/runner.json" ]]; then
      cat "$CLI/runner.json"; echo
      age=$(( $(date +%s) - $(stat -c %Y "$CLI/runner.json") ))
      echo "heartbeat age: ${age}s $([[ $age -gt 30 ]] && echo '(runner may be stopped or busy with a long step)')"
    else
      echo "No runner heartbeat. Start it on Windows: powershell -ExecutionPolicy Bypass -File Tools\\UnityCli\\unity-cli.ps1 watch"
    fi
    ls -1 "$REQ" 2>/dev/null | sed 's/^/queued: /' || true
    ;;
  run|exec)
    what="${2:?command required}"
    filter="${3:-}"
    mkdir -p "$REQ"
    id="$(date +%Y%m%d-%H%M%S)-$RANDOM"
    printf '{"command":"%s","filter":"%s"}\n' "$what" "$filter" > "$REQ/$id.tmp"
    mv "$REQ/$id.tmp" "$REQ/$id.json"
    echo "$id"
    if [[ "$cmd" == exec ]]; then exec "$0" wait "$id" "${UNITY_CLI_WAIT:-170}"; fi
    ;;
  wait)
    id="${2:?id required}"
    timeout="${3:-170}"
    end=$(( $(date +%s) + timeout ))
    while [[ ! -f "$RES/$id.json" ]]; do
      if (( $(date +%s) >= end )); then echo "still running: $id (call: wait $id)"; exit 2; fi
      sleep 3
    done
    summary "$id"
    passed "$id"
    ;;
  show)
    summary "${2:?id required}"
    ;;
  cancel)
    mkdir -p "$CLI"; touch "$CLI/cancel"; echo "cancel requested"
    ;;
  *)
    echo "unknown command: $cmd" >&2; exit 64 ;;
esac
