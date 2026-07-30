# Laptop-Free Development

Ways to work on this repo (or similar .NET + React stacks) without treating a
personal laptop as the primary development machine.

## GitHub Codespaces

Browser-based or VS Code–connected dev environments backed by a cloud VM.

- This repo includes [`.devcontainer/devcontainer.json`](../../.devcontainer/devcontainer.json)
  with .NET 10, Node 24, Docker, GitHub CLI, and Azure Developer CLI (`azd`).
- Ports for API, UIs, worker, and MCP hosts are forwarded automatically.
- Open the repo in Codespaces from GitHub (**Code** → **Codespaces** → **Create
  codespace on main**) or use the **Run All** launch profile in VS Code once
  connected.

Best when you want a standardized, repo-defined environment with minimal local
setup.

## Microsoft VPC

Microsoft cloud PC / virtual desktop options (often called **VPC** or **cloud PC**
in enterprise setups).

- **Windows 365 Cloud PC** — persistent personal cloud desktop.
- **Azure Virtual Desktop (AVD)** — pooled or personal desktops in Azure.
- **Microsoft Dev Box** — developer-focused VMs provisioned from a dev center.

Best when IT provisions a managed Windows desktop with corporate identity, VPN,
and pre-installed tooling instead of shipping a physical laptop.

## Citrix

Citrix Virtual Apps and Desktops (or Citrix DaaS) deliver a hosted desktop or
published IDE/session from on-premises or cloud infrastructure.

- Developers connect via Citrix Workspace; the session runs on a datacenter VM.
- Useful when code, secrets, and build agents must stay inside a corporate
  network boundary.

Best when policy requires all development inside a Citrix-hosted session rather
than on a local or public-cloud dev box.
