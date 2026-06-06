<h3 align="center">Screen capture, file sharing and productivity tool for macOS</h3>
<br>
<div align="center">
  <a href="https://github.com/austinhal/SnapX/actions"><img src="https://img.shields.io/github/actions/workflow/status/austinhal/SnapX/build.yml?branch=master&label=Build&cacheSeconds=3600" alt="Build Status"/></a>
  <a href="./LICENSE"><img src="https://img.shields.io/github/license/austinhal/SnapX?label=License&color=brightgreen&cacheSeconds=3600" alt="License"/></a>
  <a href="https://github.com/austinhal/SnapX/releases/latest"><img src="https://img.shields.io/github/v/release/austinhal/SnapX?label=Release&color=brightgreen&cacheSeconds=3600" alt="Release"/></a>
  <a href="https://github.com/austinhal/SnapX/releases"><img src="https://img.shields.io/github/downloads/austinhal/SnapX/total?label=Downloads&cacheSeconds=3600" alt="Downloads"/></a>
</div>
<br>

**SnapX** is a free and open-source macOS application that lets you capture or record any area of your screen, copy results to your clipboard, and share files — all from the menu bar.

A native macOS port of [ShareX](https://github.com/ShareX/ShareX), built with .NET 10 and Avalonia.

## Features

### Capture
- **Region capture** — drag to select any area of the screen
- **Window capture** — click to capture a specific window
- **Fullscreen capture** — silent capture of the entire display
- **Screen recording** — record to MP4 via FFmpeg (avfoundation)
- **GIF recording** — record directly to animated GIF

### After Capture
- **Post-capture toolbar** — floating thumbnail with one-click Copy Image, Copy Path, Open in Finder, or Dismiss
- **Auto-copy to clipboard** — image or file path copied automatically after every capture
- **History window** — searchable list of all past captures with timestamps and upload URLs

### Tools
- **OCR (Text recognition)** — extract text from any screenshot using Apple's Vision framework
- **Clipboard** — copy images and text to NSPasteboard

### Settings
- Configurable save path
- Toggle post-capture toolbar and clipboard behavior
- Adjustable toolbar auto-dismiss timeout

## Requirements

- macOS 13 Ventura or later (arm64 or x86_64)
- [.NET 10 Runtime](https://dotnet.microsoft.com/download/dotnet/10.0)
- [FFmpeg](https://ffmpeg.org) (optional — required for screen recording; install via `brew install ffmpeg`)
- **Accessibility permission** — required for global hotkeys (System Settings → Privacy & Security → Accessibility)

## Installation

### From Releases

1. Download the latest `.dmg` from the [Releases page](https://github.com/austinhal/SnapX/releases)
2. Open the `.dmg` and drag **SnapX** to `/Applications`
3. Launch SnapX — it appears in the menu bar
4. Grant Accessibility access when prompted (required for hotkeys)

### Build from Source

```bash
git clone https://github.com/austinhal/SnapX.git
cd SnapX
dotnet build ShareXMac.sln
dotnet run --project ShareXMac/ShareXMac.csproj
```

Run tests:
```bash
dotnet test ShareXMac.Tests/ShareXMac.Tests.csproj
```

## Tech Stack

| Component | Technology |
|-----------|-----------|
| UI framework | [Avalonia 11.2](https://avaloniaui.net) |
| Runtime | .NET 10 |
| MVVM | CommunityToolkit.Mvvm 8.4 |
| ObjC interop | P/Invoke via `libobjc.dylib` |
| Screen capture | `screencapture` CLI |
| Screen recording | FFmpeg avfoundation |
| OCR | Apple Vision framework (`VNRecognizeTextRequest`) |
| Clipboard | NSPasteboard |
| Notifications | UNUserNotificationCenter |
| Hotkeys | CGEventTap |
| History | JSON via Newtonsoft.Json |
| Settings | JSON (`~/Library/Application Support/SnapX/`) |

## Project Structure

```
ShareXMac/                  — Main app (Avalonia, tray, windows, ViewModels)
ShareXMac.ScreenCaptureLib/ — macOS capture, recording, OCR, clipboard, hotkeys
ShareXMac.HelpersLib/       — Shared interfaces (IScreenCapture, IHotkeyManager, …)
ShareXMac.HistoryLib/       — JSON-based capture history
ShareXMac.UploadersLib/     — Upload destinations (in progress)
ShareXMac.ImageEffectsLib/  — Image effects pipeline (in progress)
ShareXMac.Tests/            — xUnit test suite
```

## Links

- GitHub: https://github.com/austinhal/SnapX
- Original ShareX: https://github.com/ShareX/ShareX
- License: [GPL-3.0](./LICENSE)

## Acknowledgements

SnapX is a macOS port of [ShareX](https://github.com/ShareX/ShareX) by Jaex and tobya. The core HistoryLib, UploadersLib, ImageEffectsLib, and HelpersLib are adapted from the original ShareX codebase under GPL-3.0.
