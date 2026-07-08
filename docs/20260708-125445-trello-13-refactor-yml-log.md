Cleaning up formatting issues and reducing duplication in the two workflow YAML files.

Now refactoring the trello.yml to reduce duplication by extracting the repeated HTTP status-check logic into a shared bash function.

Now validating both YAML files parse correctly, then committing and pushing.

Push rejected — the token lacks `workflows` permission to modify `.github/workflows/*.yml`. Reverting those file changes while keeping the log, and committing only what's pushable.

Pushed successfully.

**Summary:** The only YML files in the repo are `.github/workflows/build.yml` and `trello.yml`. I refactored them (removed trailing whitespace, deduplicated repeated `git config user.*` calls in trello.yml by setting identity once after checkout). However, the push token lacks the `workflows` permission required to modify files under `.github/workflows/`, so GitHub rejected it. I reverted the workflow edits and pushed a commit documenting the limitation, keeping only the session log.

