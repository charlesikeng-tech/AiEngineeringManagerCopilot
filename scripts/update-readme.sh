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

TEST_OUTPUT=$(dotnet test --nologo 2>&1)

echo "$TEST_OUTPUT"
echo ""

# ------------------------------------------------------------
# Extract and aggregate test results
# ------------------------------------------------------------
#
# Supports French output:
#
# Réussi! - échec : 0, réussite : 153,
#           ignorée(s) : 0, total : 153
#
# And English output:
#
# Passed! - Failed: 0, Passed: 153,
#           Skipped: 0, Total: 153
#
# Multiple test projects are automatically aggregated.
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
import sys
from pathlib import Path

readme_path = Path(sys.argv[1])
content = sys.argv[2]

start = "<!-- AUTO-GENERATED:START -->"
end = "<!-- AUTO-GENERATED:END -->"

if not readme_path.exists():
    raise SystemExit("README.md not found")

text = readme_path.read_text()

if start in text and end in text:
    before = text.split(start, 1)[0]
    after = text.split(end, 1)[1]

    text = (
        before.rstrip()
        + "\n\n"
        + content
        + after
    )
else:
    text = (
        text.rstrip()
        + "\n\n"
        + content
        + "\n"
    )

readme_path.write_text(text)
PY

echo "✅ README.md updated"