# Presentation — AI Development Journey

**Mid June → late July 2026**

This repo's `docs/` folders mirror four sections of a summary journey. The arc is
intentional: each step sets up the next, even when a section is only introduced in
the talk.

## Why "journey"?

The timeline is real. Over roughly six weeks the work moved from *where* development
happens, through *how* autonomous agents fit a sprint rhythm, into the broader *tooling
landscape*, and finally into a concrete *Foundry + MCP* implementation in this
Weather sample.

Sections **1–3** are framing — name them, show the folder, move on. The material is
sketched, not presentation-ready. **Section 4** is the focus: live demos, architecture,
and the Foundry console learning path (V1 → V4).

## Sections

| # | Title | Doc | Role in the talk |
| --- | --- | --- | --- |
| 1 | Laptop-free Engineering | [`1-laptop-free-engineering/laptop-free.md`](1-laptop-free-engineering/laptop-free.md) | Introduce — context for cloud / Codespaces / VPC |
| 2 | Autonomous Sprint Board AI Development | [`2-autonomous-sprint-board-ai-development/autonomous-sprint-board.md`](2-autonomous-sprint-board-ai-development/autonomous-sprint-board.md) | Introduce — placeholder for sprint-board + agents story |
| 3 | AI Development Ecosystem | [`3-ai-development-ecosystem/ai-development-ecosystem.md`](3-ai-development-ecosystem/ai-development-ecosystem.md) | Introduce — landscape of tools (wireframes, rapid app gen, IDEs) |
| 4 | Demystifying Microsoft Foundry Agents and MCP | [`4-demystifying-foundry-agents-mcp/demystifying-foundry-agents-mcp.md`](4-demystifying-foundry-agents-mcp/demystifying-foundry-agents-mcp.md) | **Focus** — Foundry consoles, agents, MCP hosts in this repo |

## Supporting material

- [`architecture.md`](architecture.md) — runtime diagrams, ports, production settings (pair with section 4)
- [`../README.md`](../README.md) — how to run the Weather sample locally or in Codespaces
- [`../AGENTS.md`](../AGENTS.md) — Cursor Cloud / agent environment notes

## Suggested flow (section 4)

1. **Problem** — AI weather needs real lat/long and public weather data, not hallucination.
2. **V1 → V2** — model-direct (legacy vs unified endpoint); when the console still owns data prep.
3. **V3** — in-process function tools; model chooses tools locally.
4. **V4 + production** — hosted Foundry agent + MCP (`mcp-function`, `mcp-dotnet`); same path as API/MVC.
5. **Demos** — React / Blazor / API `GetCurrentAIWeather`; optional MCP inspector on 8110 / 8120.

Adjust depth on 1–3 to taste; keep most clock time on 4 and hands-on paths in the repo.
