#!/bin/bash

set -euo pipefail

README="README.md"

START="<!-- AUTO-GENERATED:START -->"
END="<!-- AUTO-GENERATED:END -->"

# ------------------------------------------------------------
# Run tests
# ------------------------------------------------------------

echo "🧪 Running tests..."
echo ""

set +e

TEST_OUTPUT=$(dotnet test --nologo 2>&1)
TEST_EXIT_CODE=$?

set -e

echo "$TEST_OUTPUT"
echo ""

if [[ $TEST_EXIT_CODE -ne 0 ]]; then
    echo "❌ Tests failed with exit code $TEST_EXIT_CODE"
    exit $TEST_EXIT_CODE
fi

# ------------------------------------------------------------
# Extract and aggregate test results
# ------------------------------------------------------------

TOTAL=0
PASSED=0
FAILED=0
SKIPPED=0

while IFS= read -r line; do

    # Total
    if [[ "$line" =~ [Tt]otal[[:space:]]*:[[:space:]]*([0-9]+) ]]; then
        TOTAL=$((TOTAL + ${BASH_REMATCH[1]}))
    fi

    # Passed - French
    if [[ "$line" =~ [Rr]éussite[[:space:]]*:[[:space:]]*([0-9]+) ]]; then
        PASSED=$((PASSED + ${BASH_REMATCH[1]}))

    # Passed - English
    elif [[ "$line" =~ [Pp]assed[[:space:]]*:[[:space:]]*([0-9]+) ]]; then
        PASSED=$((PASSED + ${BASH_REMATCH[1]}))
    fi

    # Failed - French
    if [[ "$line" =~ [Éé]chec[[:space:]]*:[[:space:]]*([0-9]+) ]]; then
        FAILED=$((FAILED + ${BASH_REMATCH[1]}))

    # Failed - English
    elif [[ "$line" =~ [Ff]ailed[[:space:]]*:[[:space:]]*([0-9]+) ]]; then
        FAILED=$((FAILED + ${BASH_REMATCH[1]}))
    fi

    # Skipped - French
    if [[ "$line" =~ ignorée\(s\)[[:space:]]*:[[:space:]]*([0-9]+) ]]; then
        SKIPPED=$((SKIPPED + ${BASH_REMATCH[1]}))

    # Skipped - English
    elif [[ "$line" =~ [Ss]kipped[[:space:]]*:[[:space:]]*([0-9]+) ]]; then
        SKIPPED=$((SKIPPED + ${BASH_REMATCH[1]}))
    fi

done <<< "$TEST_OUTPUT"

# ------------------------------------------------------------
# Validate extraction
# ------------------------------------------------------------

if [[ "$TOTAL" -eq 0 ]]; then
    echo "❌ Unable to extract test results."
    exit 1
fi

echo "📊 Tests: $PASSED/$TOTAL passed"
echo ""

# ------------------------------------------------------------
# Generate README content
# ------------------------------------------------------------

CONTENT=$(cat <<EOF
$START

## 🚀 Current implementation status

### Integrations

- ✅ GitHub
- ✅ Jira
- ✅ OpenAI
- 🧪 Fake LLM provider

### Engineering intelligence

- ✅ 8 engineering metrics
- ✅ Engineering health
- ✅ Engineering reports
- ✅ Risk detection
- ✅ Recommended actions
- ✅ AI analysis
- ✅ Metric Trends v1
- ✅ Early Warning v1

### Synchronization resilience

- ✅ GitHub pagination
- ✅ GitHub retry / rate-limit handling
- ✅ Jira retry / rate-limit handling
- ✅ GitHub background synchronization
- ✅ Jira background synchronization

### Authentication & Team Isolation

- ✅ Current-user abstraction
- ✅ HTTP current-user resolution
- ✅ Owner-based Team isolation
- ✅ GitHub / Jira isolation
- ✅ JWT authentication infrastructure
- 🚧 Endpoint-level authorization rollout

### Engineering Metrics

| Metric | Source |
|---|---|
| Cycle Time | GitHub |
| PR Review Time | GitHub |
| Deployment Frequency | GitHub |
| Change Failure Rate | GitHub |
| Lead Time | Jira |
| Open PRs | GitHub |
| Merged PRs | GitHub |
| Blocked Items | Jira |

### Automated tests

\`\`\`text
$TOTAL tests
$PASSED passed
$FAILED failed
$SKIPPED skipped
\`\`\`

$END
EOF
)

# ------------------------------------------------------------
# Update README
# ------------------------------------------------------------

python3 - "$README" "$CONTENT" <<'PY'
import re
import sys
from pathlib import Path

readme_path = Path(sys.argv[1])
content = sys.argv[2]

start = "<!-- AUTO-GENERATED:START -->"
end = "<!-- AUTO-GENERATED:END -->"

if not readme_path.exists():
    raise SystemExit("README.md not found")

text = readme_path.read_text(encoding="utf-8")

pattern = re.compile(
    re.escape(start) + r".*?" + re.escape(end),
    re.DOTALL
)

matches = list(pattern.finditer(text))

if matches:
    # Replace the first generated block.
    text = pattern.sub(content, text, count=1)

    # Remove any stale duplicate generated blocks.
    text = pattern.sub("", text)
    
    # Reinsert our canonical block at the position of the first one.
    first_start = matches[0].start()

    # Because the previous substitutions changed the string,
    # reconstruct cleanly from the README without generated blocks.
    original = readme_path.read_text(encoding="utf-8")
    clean = pattern.sub("", original)

    insertion_point = min(first_start, len(clean))

    text = (
        clean[:insertion_point].rstrip()
        + "\n\n"
        + content
        + "\n\n"
        + clean[insertion_point:].lstrip()
    )
else:
    text = (
        text.rstrip()
        + "\n\n"
        + content
        + "\n"
    )

readme_path.write_text(
    text,
    encoding="utf-8"
)
PY

echo ""
echo "✅ README.md updated"
echo "📊 $PASSED/$TOTAL tests passed"