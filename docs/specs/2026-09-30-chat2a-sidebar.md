# Chat2a right sidebar with map state synchronization

- **Status:** in-progress
- **Branch / PR:** `claude/affectionate-cray-6sli9u` / (pending)
- **Mode:** interactive (no questions needed: story shipped with testable criteria)

## Problem

Managing saved cities through chat currently means leaving the map for
`/chat-clients`, chatting, and navigating back to Home so the map re-reads
`/User`. Users want to chat with the Chat2a agent (Agent Framework, in-process
tools including `AddUserCity`/`DeleteUserCity`) next to the map, and see pins
appear or disappear as soon as the agent finishes answering.

## Goals

- A chat button in the top bar of all three UIs (React, Blazor, MVC) that
  opens a right-hand Chat2a sidebar.
- After every Chat2a completion in the sidebar, the UI re-reads the user
  (`GET /User`) and the map re-renders its pins from that response.

## Non-goals

- No backend change: the sidebar uses the existing `POST /Chat2a/messages`
  SSE endpoint (Weather API for React/Blazor, MVC's own `Chat2aController`).
- No tab switcher in the sidebar (Chat2a only); `/chat-clients` is unchanged.
- No persistence of the sidebar conversation across full page loads (MVC
  navigations reload the page; the sidebar history resets, as `/chat-clients` does).
- No live push of pins during a streaming response; the refetch happens on completion.

## Acceptance criteria

1. **AC1 (header button + order):** Given the Home page (`/`), when the top bar
   renders, then it contains a button with accessible name **"Open chat"**, and
   in DOM order the header actions are: Add location (plus) → Open chat →
   Open user menu (avatar). On other routes (no plus button) the chat button
   still renders immediately before the avatar button.
2. **AC2 (opens on the right):** Given the sidebar is closed, when the user
   clicks "Open chat", then a panel with role `complementary` (or `dialog`)
   labelled **"Chat2a"** becomes visible, anchored to the right edge of the
   viewport below the top bar, and the button reports `aria-expanded="true"`.
   Clicking the button again, or the panel's **"Close chat"** button, hides it
   (`aria-expanded="false"`). At widths < 640px the panel spans the full width.
3. **AC3 (uses Chat2a):** Given the sidebar is open, when the user submits a
   message, then exactly one `POST` goes to `/Chat2a/messages` with body
   `{ sessionId, message }` (sessionId `null` on the first send, then the id
   from the stream's `session` event on later sends), and streamed `token`
   events render as the assistant reply; `tool_start`/`tool_end` render as tool
   lines and `error` events render as an error message.
4. **AC4 (refetch on every completion):** Given the sidebar is open, when a
   Chat2a send completes (stream ended normally, or the request failed), then
   the UI issues a new `GET /User` — once per completed send, i.e. N sends → N
   refetches.
5. **AC5 (map reflects changes):** Given the map on `/` shows the pins from
   `/User`, when a sidebar send completes and the refetched `/User` contains an
   added city (or lacks a removed one), then the map's pin set equals the cities
   in the refetched response (added pin present, removed pin gone) without a
   page reload or navigation.

## Affected stacks

- [ ] core-dotnet  - [ ] cqmediator-dotnet
- [ ] api-dotnet  - [x] mvc-dotnet  - [ ] worker-dotnet
- [x] ui-react  - [x] ui-blazor
- [ ] mcp-srv-app-service  - [ ] mcp-srv-func-app  - [ ] mcp-srv-python  - [ ] mcp-srv-node
- [ ] FoundryConsole*  - [ ] infra / workflows  - [x] docs

Parity (see `REVIEW.md`): UI change → all three UIs (React, Blazor, MVC), same
behavior, each styled with its own library.

## Assumptions

- "The existing chat 2A API service" is the Chat2a endpoint `POST /Chat2a/messages`
  already used by the `/chat-clients` Chat2a tab (Agent Framework, in-process tools).
- All three UIs get the feature, per the Feature Parity Contract in `docs/architecture.md`.
- The chat button is shown on every route (it sits between plus and avatar on
  `/`, where the plus exists). On routes without a map the refetch is harmless
  (React updates its RTK Query cache; Blazor/MVC skip the pin re-render when
  `weatherMap` has no map mounted).
- "Every chat completion" includes failed sends: the agent may have mutated
  cities before an error, so the refetch always runs in a `finally`.
- The sidebar keeps its own Chat2a session, independent of the `/chat-clients` Chat2a tab.
- The sidebar overlays the map (the map is not resized); it closes with its
  Close button or by toggling the header button. Escape also closes it.
- Blazor/MVC refetch by calling a newly exported `weatherMap.refreshCities()`
  (the same function the maps already use after add/delete).

## Plan

<Filled by the `planner` agent.>

## Review log

| Round | Gate | Findings | Resolution (commit / reason) |
|---|---|---|---|

## Open issues

