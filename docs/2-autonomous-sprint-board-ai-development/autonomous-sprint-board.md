# Autonomous Sprint Board AI Development

_Context for the journey — content to be fleshed out._

How autonomous agents and sprint-board workflows fit into day-to-day engineering:
planning, execution, and review without a traditional "you type every line" loop.

## Topics

- Sprint board as the coordination surface for agent work
- Handoffs between human intent and autonomous execution
- What worked, what did not (Mid June → late July)

## Reflection / Out of Scope / V2 Notes

- Not intended for production, only designed for learning
- Too synchronous - GitHub Actions runs the full length of the request
- Use **GitHub Copilot Agent** instead of Copilot CLI

## Trello Rule Details

**Link to Rule:**  
https://trello.com/b/7BmzvGVI/weather-1116/butler/rules

**Trigger:**  
Card added to "Ready for AI"

**Post URL:**  
https://api.github.com/repos/dustinkorzaan/weather-1116/dispatches

**Headers (use w/o \n):**

```json
{
    "Authorization": "Bearer <YOUR_GITHUB_PAT>",
    "Accept": "application/vnd.github+json",
    "User-Agent": "dustinkorzaan",
    "Content-Type": "application/json"
}
```

**Payload (use w/o \n):**

```json
{
    "event_type": "start_story_with_v1_ai",
    "client_payload": {
        "story_source": "Trello",
        "story_name": "{cardname}",
        "story_number": "{cardnumber}",
        "story_id": "{cardid}",
        "story_description": "{carddescription}",
        "story_checklists": "{cardchecklists}"
    }
}
```
