# Chat2a right sidebar with map state synchronization

- **Status:** shipped (merged as #377)
- **Branch / PR:** `claude/affectionate-cray-6sli9u` / https://github.com/dustinkorzaan/weather-1116/pull/377
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

Revision 2 (PR #377 feedback, comparing it with PR #376) changes AC1, AC2 and AC4 and adds AC6. The originals are in git history.

1. **AC1 (header button + order, map page only):** Given the Home page (`/`),
   when the top bar renders, then the header actions in DOM order are:
   Add location (plus) → **Open chat** → Open user menu (avatar). On every other
   route (`/hello-world`, `/current-ai-weather`, `/chat-clients`) there is no
   "Open chat" button.
2. **AC2 (docked beside the map):** Given `/` with the sidebar closed, when the
   user clicks "Open chat", then a panel with role `complementary` labelled
   **"Chat2a"** becomes visible, and the button reports `aria-expanded="true"`.
   The panel sits **in the layout next to the map, not on top of it**: at
   widths ≥ 640px it is to the right of the map in the same row, and the map
   gets narrower. Below 640px it stacks under the map, and the map keeps part of
   the height. No `position: fixed` overlay covers the map. Clicking the button
   again, the panel's **"Close chat"** button, or pressing Escape hides it
   (`aria-expanded="false"`). Leaving `/` closes the panel; on returning it
   is closed, and (React/Blazor) the conversation history is still there.
3. **AC3 (uses Chat2a):** unchanged. Given the sidebar is open, when the user
   submits a message, then exactly one `POST` goes to `/Chat2a/messages` with
   body `{ sessionId, message }`. `sessionId` is `null` on the first send, then
   the id from the stream's `session` event. Streamed `token` events render as
   the assistant reply, `tool_start`/`tool_end` render as tool lines, and
   `error` events render as an error message.
4. **AC4 (refetch on every completion, through the map's own path):** Given the
   sidebar is open, when a Chat2a send completes (the stream ended normally, or
   the request failed), then the UI re-reads `GET /User` exactly once for that
   send, through the same path the map renders from:
   - React: the map-pins context `refreshCities` (RTK `refetch`);
   - Blazor and MVC: `weatherMap.refreshCities()`.

   No other `/User` request is made. The MVC fallback `fetch('/User')` is gone.
5. **AC5 (map reflects changes):** unchanged. After a completed send, the map's
   pin set equals the cities in the refetched `/User` (added pin present,
   removed pin gone) without a reload or navigation.
6. **AC6 (renders like the existing chat panel):** In every UI, sidebar
   messages render the way the `/chat-clients` Chat2a tab renders them, using
   **the same shared rendering code** (no second copy of the formatting logic):
   - finished assistant replies are sanitized GFM markdown (the same renderer
     as `/chat-clients`);
   - a finished reply that has `usage` shows the usage chip (same text, for
     example `1.2s · 345 tok`) with the same hover/focus details;
   - tool lines show the same hover/focus card with `Arguments` and `Result`
     sections (`Waiting for tool output…` while running).

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
- "Every chat completion" includes failed sends: the agent may have mutated
  cities before an error, so the refetch always runs in a `finally`.
- The sidebar keeps its own Chat2a session, independent of the `/chat-clients` Chat2a tab.
- Revision 2: the chat button exists only on `/`. Map sync is the sidebar's
  purpose, and the plus button already follows that rule. In React and Blazor
  the sidebar stays mounted while hidden, so its conversation survives SPA
  navigation away from `/` and back. MVC reloads the page on navigation, so its
  history resets.
- Revision 2: the sidebar docks beside the map (the map shrinks) instead of
  overlaying it, so the pins being changed stay visible. Below 640px it stacks
  under the map. Google Maps re-lays itself out on container resize.
- Blazor and MVC refetch through `weatherMap.refreshCities()` (exported in revision 1).
- `blocked` events are not rendered: only Chat5a/Chat5b emit them
  (`core-dotnet/core/Chat/Chat5b/Chat5bService.cs`), never Chat2a.

## Plan

| # | Task | Files (create/modify) | Tests to add/update | Depends on | Parallel-safe |
|---|------|------------------------|---------------------|------------|---------------|
| 1 | React: header "Open chat" button + Chat2a sidebar + refetch `/User` on every completion | create `ui-react/src/components/chat/Chat2aSidebar.jsx`; modify `ui-react/src/App.jsx`, `ui-react/src/map/mapPinsContext.jsx` (expose `refreshCities`), optionally `ui-react/src/components/chat/ChatPanel.jsx` (export `ToolChip`/`messageClasses` only) | create `ui-react/src/components/chat/Chat2aSidebar.test.jsx` (unit); update `ui-react/src/App.test.jsx` only if an existing header assertion breaks | - | yes |
| 2 | Blazor: header "Open chat" button + `Chat2aSidebar` component + `weatherMap.refreshCities()` export | create `ui-blazor/blazor/Shared/Chat2aSidebar.razor`; modify `ui-blazor/blazor/Shared/MainLayout.razor`, `ui-blazor/blazor/wwwroot/js/weatherMap.js` (add `refreshCities` to the returned object), `ui-blazor/blazor/wwwroot/css/site.css` | update `ui-blazor/blazor.tests/LayoutCssTests.cs` (register `ChatApiClient` in MainLayout render setups); unit tests in new `ui-blazor/blazor.tests/Chat2aSidebarTests.cs` | - | yes |
| 3 | MVC: header "Open chat" button + sidebar markup + `chatSidebar.js` + `weatherMap.refreshCities()` export | create `mvc-dotnet/mvc/Views/Shared/_Chat2aSidebar.cshtml`, `mvc-dotnet/mvc/wwwroot/js/chatSidebar.js`; modify `mvc-dotnet/mvc/Views/Shared/_Layout.cshtml`, `mvc-dotnet/mvc/wwwroot/js/weatherMap.js`, `mvc-dotnet/mvc/wwwroot/css/site.css` | update `mvc-dotnet/mvc.tests/HomeControllerTests.cs` only if header assertions break; unit/source tests in new `mvc-dotnet/mvc.tests/Chat2aSidebarLayoutTests.cs` | - | yes |
| 4 | Docs: top-bar contract + sidebar | modify `docs/architecture.md` (*Pages and routes* intro + *Responsive Design Contract* top-bar bullet), `docs/5-chat-clients/5-chat-clients.md` (*React / Blazor vs MVC* section: sidebar reuses Chat2a) | - | - | yes |

No serialized paths are touched (no Core, CQMediator, sln, infra, workflows, `.claude/`, AGENTS.md, MCP registration). No backend change: `POST /Chat2a/messages` already exists on the API and on MVC (`mvc-dotnet/mvc/Controllers/Chat2aController.cs`); `GET /User` exists on the API, on Blazor (`ui-blazor/blazor/Program.cs` proxy `app.MapGet("/User", ...)`) and on MVC (`mvc-dotnet/mvc/Controllers/UserController.cs`).

### Task notes
1. **React.**
   - `mapPinsContext.jsx`: add `refreshCities` (wraps the existing RTK `refetch` from `useGetUserQuery`, returns its promise) to the context value. `WeatherMap.jsx` already re-renders markers from `cities`, so AC5 needs no map change.
   - `Chat2aSidebar.jsx`: props `open`, `onClose`. Render `<aside role="complementary" aria-label="Chat2a" id="chat2a-sidebar">` (keep it mounted, toggle `hidden`, so history survives open/close and route changes). Position `fixed right-0` below the header (`top` = header height, `bottom-0`), `w-full sm:w-96`, `z-40`, border-l, `bg-background`. Header text "Chat2a" and a `Button aria-label="Close chat"`. Escape key calls `onClose`.
   - Send loop: copy the single-tab subset of `ChatPanel.sendMessage` (`ui-react/src/components/chat/ChatPanel.jsx`). Call `streamChatMessage({ endpoint: '/Chat2a/messages', sessionId: sessionRef.current, message, onEvent })` from `ui-react/src/utils/chatStream.js`. Do not pass `gates`, so the body is exactly `{ sessionId, message }`. Handle `session`, `token`, `tool_start`, `tool_end`, `error`, `done`. In `finally`, call `refreshCities()` from `useMapPins()` once.
   - To reuse `ToolChip`/`messageClasses`, export them from `ChatPanel.jsx` with no behaviour change. Use a distinct textarea id (`chat2a-sidebar-input`) so it does not collide with `chat-input` on `/chat-clients`.
   - `App.jsx`: in `.flex items-center gap-2`, render `{isMapVisible && <AddLocationControl />}`, then `<Button aria-label="Open chat" aria-expanded={open} aria-controls="chat2a-sidebar">` with a lucide `MessageSquare` icon (outline icon button like the avatar), then the avatar `DropdownMenu`. Render `<Chat2aSidebar>` inside `AppShell`, which is already under `MapPinsProvider`.
2. **Blazor.**
   - `weatherMap.js`: add `refreshCities: refreshCities` to the returned object. `refreshCities` already fetches `/User` and calls `renderCities`, which clears and recreates markers on every mounted map and is a no-op when no map is mounted.
   - `Chat2aSidebar.razor`: inject `ChatApiClient` and `IJSRuntime`; parameters `Open`, `OnClose`. Render `<aside role="complementary" aria-label="Chat2a" id="chat2a-sidebar" hidden=@(!Open)>` with a "Close chat" button and `@onkeydown` Escape handling. Send with `ChatClient.StreamMessageAsync("Chat2a", new ChatSendMessageRequest { SessionId = _sessionId, Message = msg }, ct)` (`ui-blazor/blazor/Data/ChatApiClient.cs`), and mirror the event handling from `ui-blazor/blazor/Shared/ChatPanel.razor` (around lines 319-420). In `finally`: `try { await JS.InvokeVoidAsync("weatherMap.refreshCities"); } catch (JSException) { }`, also catching `InvalidOperationException`/`JSDisconnectedException` because prerender/tests may have no JS. Flow a `CancellationTokenSource` and dispose it in `Dispose`.
   - `MainLayout.razor`: inside `.header-actions`, add the chat `<button class="chat-toggle-button" aria-label="Open chat" title="Open chat" aria-expanded aria-controls="chat2a-sidebar">` after the `@if (IsMapPage)` add-location block and before `#user-menu-button`. Render `<Chat2aSidebar>` after `</FluentLayout>`.
   - CSS: `position: fixed; top: 48px; right: 0; bottom: 0; width: 24rem; max-width: 100%` and `@media (max-width: 639.98px) { width: 100% }`, using the existing theme variables.
   - `LayoutCssTests` renders `MainLayout` without `ChatApiClient` in DI, so add `context.Services.AddSingleton(new ChatApiClient(...))` to those setups. That changes an existing test's setup but loosens no assertion.
3. **MVC.**
   - `weatherMap.js`: add `refreshCities: refreshCities` to the returned object.
   - `_Chat2aSidebar.cshtml`: render `<aside id="chat2a-sidebar" class="chat-sidebar" role="complementary" aria-label="Chat2a" hidden>` with a "Close chat" button, a messages list, and a form (textarea `id="chat2a-sidebar-input"`, which must not reuse `chat-input`/`chat-messages` because `chatClient.js` binds to those ids on `/chat-clients`).
   - `_Layout.cshtml`: add `<button id="chatSidebarButton" class="chat-toggle-button" aria-label="Open chat" aria-expanded="false" aria-controls="chat2a-sidebar">` between the `@if (isMapPage)` add-location block and `.avatar-wrap`. Render the partial after `</main>` and add `<script src="~/js/chatSidebar.js" asp-append-version="true">` next to `addLocation.js`.
   - `chatSidebar.js` (IIFE, vanilla):
     - Toggle `hidden` and `aria-expanded` from the button and the Close button; Escape closes.
     - Send with `fetch('/Chat2a/messages', { method: 'POST', body: JSON.stringify({ sessionId, message }) })` and parse SSE the same way as `streamChat` in `mvc-dotnet/mvc/wwwroot/js/chatClient.js` (~line 266). Render `token`, `tool_start`/`tool_end` and `error`.
     - In `finally`: `if (window.weatherMap && typeof window.weatherMap.refreshCities === 'function') window.weatherMap.refreshCities().catch(() => {}); else fetch('/User', { headers: { Accept: 'application/json' } }).catch(() => {});`. `weatherMap.js` is loaded only by `Views/Home/Index.cshtml`, so the fallback keeps AC4 (a GET per completion) on the other routes.
     - Assistant text renders as plain text. Markdown is not required.
   - CSS in `site.css`: the same fixed-right layout, the `<640px` full width, and `:root`/`html.dark` variables.
4. **Docs.**
   - `docs/architecture.md`: in *Pages and routes*, say the top bar has, on every route, Add location (only on `/`), then Open chat (Chat2a sidebar, right edge, full width below 640px), then the avatar, and that each completion re-reads `/User` and re-renders pins. Update the *Responsive Design Contract* top-bar bullet the same way.
   - `docs/5-chat-clients/5-chat-clients.md`: add a short note that the header sidebar reuses `POST /Chat2a/messages` with its own session.

### Acceptance test files (test-author only)
- `ui-react/src/chat2aSidebar.acceptance.test.jsx`: AC1, AC2, AC3, AC4, AC5. Render `App` with the `createTestStore`/`MemoryRouter` pattern from `ui-react/src/App.test.jsx`. Mock `fetch` with an SSE `ReadableStream` for `/Chat2a/messages` and count `/User` calls as in the "navigating to Home refetches" test. For AC5, assert the `useMapPins().cities` set through a probe consumer, or assert the Google marker set with a stubbed `loadGoogleMaps`.
- `ui-blazor/blazor.tests/Chat2aSidebarAcceptanceTests.cs`: AC1, AC2 (bUnit `MainLayout` markup order and `aria-expanded` toggle), AC3 (stub `HttpMessageHandler` behind `ChatApiClient` that captures the `Chat2a/messages` body and returns SSE), AC4 (`JSInterop` `VerifyInvoke("weatherMap.refreshCities", N)`, including a failed send), AC5 (`weatherMap.js` source exports `refreshCities`, and `renderCities` clears markers with `setMap(null)`).
- `mvc-dotnet/mvc.tests/Chat2aSidebarAcceptanceTests.cs`: AC1, AC2 (`WeatherMvcWebApplicationFactory` HTML for `/` and `/hello-world`: button order, `aria-label`, `aria-controls`, `role`/`aria-label="Chat2a"`, Close button), AC3, AC4, AC5 (source assertions on `chatSidebar.js`/`weatherMap.js`: `/Chat2a/messages`, `{ sessionId, message }`, `refreshCities` in `finally`, `/User` fallback, `refreshCities` exported).

### Verification
- `scripts/verify.sh --all` must be green.
- Manual runtime check, since MVC has no JS test runner and the Google map cannot run under jsdom/bUnit: run the API (8080) with `DB_CONNECTION_STRING` and AI config. On each UI's `/`, open chat and ask "add Nashville to my cities" and then "remove Nashville". Confirm the pin appears and disappears with no reload, the panel is full-width at 375px, and theme toggling still styles the panel.

### Risks
- **Parity:** Tasks 1-3 must match on labels ("Open chat", "Close chat", "Chat2a"), button order, the Escape behaviour and the `<640px` rule. Reviewers should diff the three against AC1/AC2.
- **MVC test depth:** AC3-AC5 are covered only by source-string assertions, which follows the existing pattern in `HomeControllerTests`. The manual check above is the real coverage.
- **Blazor DI:** adding the sidebar to `MainLayout` makes `ChatApiClient` required wherever `MainLayout` renders in tests. Update the setups; do not delete the tests.
- **ID collisions:** on `/chat-clients` both the page chat and the sidebar are present. Sidebar ids must be unique (`chat2a-sidebar-*`), and MVC's `chatClient.js` must not select sidebar elements.
- **Z-order:** the sidebar must sit above the map and pin hover card but below the About modal and the weather modal. Check each UI's existing z-index values.
- No config, env vars, infra, ports or migrations change.

### Revision 2 plan (PR feedback)

| # | Task | Files | Parallel-safe |
|---|---|---|---|
| R1 | React: button only on `/`; sidebar in the map page's flex row (not fixed); usage chip via ChatPanel's existing helpers; drop header measurement | `ui-react/src/App.jsx`, `ui-react/src/pages/MapPage.jsx` (if the row lives there), `ui-react/src/components/chat/Chat2aSidebar.jsx`, `ChatPanel.jsx` (exports only), `Chat2aSidebar.test.jsx` | yes |
| R2 | Blazor: button only on map page; sidebar docked beside `@Body` on `/`; extract a shared `ChatMessageList` (entries, markdown, usage chip, tool hover attrs) used by both `ChatPanel` and `Chat2aSidebar`; remove the header z-index workaround if the overlay is gone | `ui-blazor/blazor/Shared/*.razor`, `wwwroot/css/site.css`, `blazor.tests/Chat2aSidebarTests.cs`, `LayoutCssTests.cs`, `ChatPanelTests.cs` (must stay green) | yes |
| R3 | MVC: button only on map page; sidebar partial + scripts only on `Views/Home/Index.cshtml`, docked beside the map; extract `wwwroot/js/chatRender.js` (renderEntry, usage/tool formatters, hover card) used by both `chatClient.js` and `chatSidebar.js`; remove `/User` fallback and the `.site-header` z-index workaround if no longer needed; **keep CRLF in `_Layout.cshtml` and `Index.cshtml`** | `mvc-dotnet/mvc/**`, `mvc.tests/Chat2aSidebarLayoutTests.cs` | yes |
| R4 | Acceptance tests (test-author): update the three `*acceptance*` files for revised AC1/AC2/AC4 and new AC6 | the 3 acceptance files | yes |
| R5 | Docs: architecture.md + 5-chat-clients.md for map-only button, docked layout, rendering parity | docs | after R1-R3 (orchestrator) |

Test size: implementer unit tests keep only cases the acceptance files do not
cover (for example Blazor focus-on-open, cancellation on dispose, CSS rules).
Duplicated cases are deleted, not skipped.

## Review log

| Round | Gate | Findings | Resolution (commit / reason) |
|---|---|---|---|
| 1 | peer | BLOCKING: Blazor header stacking context (z 10) let sidebar cover avatar menu / add-location panel | 64ccf3c (header `position: relative; z-index: 36` + CSS test) |
| 1 | peer | SHOULD: Blazor Escape only worked with focus inside the aside | 64ccf3c (textarea focused on open + test); Escape after clicking outside the panel still requires focus in the panel |
| 1 | peer | SHOULD: MVC `_Layout.cshtml` CRLF→LF churn | e64c46b (CRLF restored; diff is +15/-0) |
| 1 | peer | NIT: stale Blazor z-index comment | 64ccf3c |
| 2 | peer | clean. SHOULD: React sidebar `top` not re-measured when Home header wraps after navigation (~320px) | 6a97751 (ResizeObserver on header, re-run on route change; jsdom has no layout, so manual check at 320px) |
| 2 | peer | NIT: Blazor composer stayed disabled until `/User` refresh finished | 6a97751 (StateHasChanged before refresh) |
| 1 | final | SHIP — all five ACs evidenced in React/Blazor/MVC; verify --all green | d5f45ab |

## Open issues

