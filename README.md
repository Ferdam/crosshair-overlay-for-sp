# Crosshair Overlay for SP

A lightweight Windows desktop overlay that renders a customizable crosshair on top of any game or application. Designed primarily for **single-player and co-op games** that lack a built-in crosshair option.

## Features

- Import a custom **PNG image** as your crosshair
- Build a crosshair from scratch using **layered vector primitives** (lines, circles, dots, etc.)
- Composable layer system for complex crosshair designs
- Transparent, always-on-top overlay window — no game file modification required
- Per-monitor high-DPI aware (`PerMonitorV2`)
- Single-file publish — no installer needed

## Requirements

- Windows 10 or later
- [.NET 9 Runtime](https://dotnet.microsoft.com/en-us/download/dotnet/9.0) (desktop)

## Getting Started

1. Download the latest release (single `.exe`, no installer)
2. Run `CrosshairOverlay.exe`
3. Position and configure your crosshair via the UI
4. Launch your game — the overlay will stay on top

> For best results, run your game in **Borderless Windowed** mode. See the anti-cheat section below for important notes on exclusive fullscreen.

## Project Structure

```
CrosshairOverlay/
├── Interop/       # Native Windows API interop (overlay, transparency, window flags)
├── Models/        # Crosshair data models and layer definitions
├── Rendering/     # Drawing logic for vector primitives and PNG compositing
├── Services/      # App-level services (settings persistence, hotkeys, etc.)
└── Views/         # WPF UI (main overlay window and configuration panel)
```

## Building from Source

```bash
git clone https://github.com/Ferdam/crosshair-overlay-for-sp.git
cd crosshair-overlay-for-sp
dotnet build
```

Or open `crosshair-overlay-for-sp.sln` in Visual Studio 2022+.

---

## ⚠️ Anti-Cheat Warning

This tool is an **external overlay** — it draws on top of the screen using a transparent window and does not read or write to game memory. That said, there are important considerations before using it in any game with anti-cheat software.

### How it works

The overlay is implemented as a transparent, click-through, always-on-top WPF window rendered entirely by the OS compositor. It does not inject code into any game process, hook game APIs, or interact with game memory in any way.

### Compatibility

| Context | Risk level | Notes |
|---|---|---|
| Single-player games (no AC) | ✅ None | Intended use case |
| Co-op games (no AC) | ✅ None | Intended use case |
| Multiplayer with user-mode anti-cheat | ⚠️ Low–Medium | Varies by AC; use at your own risk |
| Multiplayer with kernel-level anti-cheat | 🚫 High | **Strongly discouraged** |

### Kernel-level anti-cheat

Games protected by kernel-level anti-cheat systems (such as **Vanguard**, **EasyAntiCheat**, or **VAC** with certain configurations) may flag or detect external overlay windows even when those overlays perform no memory access. Some anti-cheat systems enumerate all top-level windows and flag those with specific extended window styles (e.g., `WS_EX_TRANSPARENT`, `WS_EX_LAYERED`) that overlays require to function.

**Do not use this tool in competitive or ranked multiplayer matches.** The risk of a ban, even if unlikely, is real and entirely your responsibility.

### Exclusive Fullscreen

Exclusive fullscreen (FSE) mode prevents any other application from rendering on top of the game. The overlay **will not display** in true exclusive fullscreen. Run your game in **Borderless Windowed** or **Fullscreen Borderless** mode for the overlay to work. If you require exclusive fullscreen (e.g., for DSR/DLDSR downscaling or minimal input latency), this tool is not the right solution in its current form.

### Disclaimer

> **Use at your own risk.** The author takes no responsibility for account bans, suspensions, or any other consequences resulting from the use of this software in multiplayer environments. Always check the terms of service of the game you are playing before using any third-party overlay tool.
