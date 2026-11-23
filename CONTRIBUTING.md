# Contributing

Thanks for interest in LCARS x32.

## Ways to help

- **Bug reports** — what you did, what you expected, what happened, OS/version, and whether you run as app or shell.
- **Screenshots / repro steps** — especially for Start Menu launches, Engineering, Comms, and Browser/WebView2.
- **Pull requests** — keep changes focused; prefer restoring original chrome behavior over layout hacks when fixing shell UI.
- **Self-hosted updates** — you may run your own update server on your LAN; this fork does not provide a public update host.

## Development notes

- Build on Windows with MSBuild / .NET Framework tooling. Start from `tools/Build-LCARS.ps1`.
- Do not commit secrets, LAN update URLs, or `Build/update-server.local.ps1`.
- Do not commit `Build/Deploy/` packages.
- Match existing VB.NET / WinForms style in the area you touch.

## Conduct

Be civil. This is a fan freeware project around a trademarked interface aesthetic — keep discussions technical and respectful of upstream authors and rights holders.
