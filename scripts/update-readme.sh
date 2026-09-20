#!/bin/bash

set -e

README="README.md"

START="<!-- AUTO-GENERATED:START -->"
END="<!-- AUTO-GENERATED:END -->"

CONTENT=$(cat <<'EOF'
<!-- AUTO-GENERATED:START -->

## 🚀 Current capabilities

### Integrations

- GitHub
- Jira

### Engineering Metrics

- Cycle Time
- PR Review Time
- Deployment Frequency
- Change Failure Rate
- Lead Time
- Open Pull Requests
- Merged Pull Requests
- Blocked Items

### Data sources

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

<!-- AUTO-GENERATED:END -->
EOF
)

python3 - "$README" "$CONTENT" <<'PY'
import sys
from pathlib import Path

readme_path = Path(sys.argv[1])
content = sys.argv[2]

start = "<!-- AUTO-GENERATED:START -->"
end = "<!-- AUTO-GENERATED:END -->"

text = readme_path.read_text()

if start in text and end in text:
    before = text.split(start)[0]
    after = text.split(end, 1)[1]
    text = before + content + after
else:
    text = text.rstrip() + "\n\n" + content + "\n"

readme_path.write_text(text)
PY