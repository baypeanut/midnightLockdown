#!/bin/bash
# run a unity editor method in batch mode
# usage: tools/unity.sh <method> [extra args]
cd "$(dirname "$0")/.." || exit 1
UNITY="${UNITY:-/Applications/Unity/Hub/Editor/6000.5.9f1/Unity.app/Contents/MacOS/Unity}"
method="$1"; shift
mkdir -p Logs
log="Logs/batch-${method##*.}.log"
quit="-quit"; [ -n "$NOQUIT" ] && quit=""
"$UNITY" -batchmode $quit -projectPath "$PWD" -executeMethod "$method" -logFile "$log" "$@"
code=$?
grep -E "error CS|\[setup\]|\[build\]|\[check\]|\[capture\]|Exception|executeMethod method .* threw" "$log" | grep -v "^UnityEngine.Debug" | head -80
echo "exit=$code log=$log"
exit $code
