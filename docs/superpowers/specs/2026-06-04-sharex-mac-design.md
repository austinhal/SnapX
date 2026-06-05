# ShareX Mac — Design Spec
**Date:** 2026-06-04
**Source project:** ShareX-arm64 (GPL-3.0)
**Approach:** Surgical port — reuse platform-agnostic C# libraries, replace Windows UI and platform APIs with macOS equivalents

---

## Overview

ShareX-Mac is a standalone open-source macOS port of ShareX, the Windows screen capture and file sharing tool. It targets feature parity with all Mac-compatible ShareX features, distributed as a notarized `.app` via GitHub Releases and Homebrew Cask.

---

## Project Structure

New standalone repo: `ShareX-Mac`. .NET 9 solution with the following projects:

| Project | Source | Notes |
|---|---|---|
| `ShareXMac` | New | Avalonia MVVM app shell |
| `ShareXMac.HelpersLib` | Ported from `ShareX.HelpersLib` | Remove Win32 P/Invoke, registry calls, Windows paths |
| `ShareXMac.UploadersLib` | Ported from `ShareX.UploadersLib` | Mostly HTTP — minimal Windows surface area |
| `ShareXMac.ImageEffectsLib` | Ported from `ShareX.ImageEffectsLib` | Image processing logic, no Windows deps |
| `ShareXMac.HistoryLib` | Ported from `ShareX.HistoryLib` | Pure data/serialization — trivial port |
| `ShareXMac.IndexerLib` | Ported from `ShareX.IndexerLib` | File indexing — replace Windows path APIs |
| `ShareXMac.ScreenCaptureLib` | New | macOS-native via ScreenCaptureKit + AVFoundation |
| `ShareXMac.MediaLib` | Ported from `ShareX.MediaLib` | GIF/video via FFmpeg — cross-platform already |

**Porting work per library:** remove `[SupportedOSPlatform("windows")]` guards, delete or stub Win32 P/Invoke calls, replace `Environment.GetFolderPath` Windows-specific values with macOS equivalents (`~/Library/Application Support/ShareX-Mac`).

---

## UI Layer

**Menu bar app** — the app lives in the macOS status bar (no persistent Dock icon), consistent with other Mac capture tools (CleanShot X, Snagit, Xnapper).

**Components:**
- **Menu bar icon** — dropdown with all capture actions: region, window, fullscreen, record video, record GIF, OCR, color picker
- **Settings / History window** — full window for managing uploaders, image effects, hotkeys, and capture history. Opened from menu bar dropdown.
- **Post-capture toolbar** — small floating window after each capture with actions: annotate, copy to clipboard, upload, save to disk, open in editor

**Technology:** Avalonia UI with `CommunityToolkit.Mvvm` for ViewModels. MVVM pattern throughout — ViewModels are platform-agnostic, Views are Avalonia XAML.

**Minimum macOS version:** macOS 13 Ventura (covers ~90%+ of active Mac users; required for stable ScreenCaptureKit).

---

## macOS Platform Layer

All platform-specific implementations live in `ShareXMac.ScreenCaptureLib` and the app shell. Platform code is accessed via interfaces defined in `ShareXMac.HelpersLib` so ViewModels stay testable.

### Screen Capture
- Framework: `ScreenCaptureKit` via C# P/Invoke bindings
- Modes: region selection, window picker, fullscreen
- Permission: Screen Recording — requested lazily on first capture attempt, not at launch

### Screen Recording
- Framework: `ScreenCaptureKit` for frames + `AVFoundation` for MP4 encoding
- GIF: FFmpeg via existing `MediaLib` logic
- Permission: reuses Screen Recording — no additional permission

### OCR
- Framework: Apple `Vision` (VNRecognizeTextRequest)
- Operates on in-memory images from capture — no additional permission required

### Global Hotkeys
- Framework: `CGEvent` tap via P/Invoke
- Permission: Accessibility — requested lazily and **only** if the user enables a global hotkey in Settings. If the user never configures hotkeys, this permission is never requested.

### Notifications
- Framework: macOS `UserNotifications`
- Used for: upload complete, link copied to clipboard
- Standard one-time notification consent prompt

### Annotations
- Reuses `ShareXMac.ImageEffectsLib` directly
- Avalonia renders the annotation canvas — no new platform code

---

## Permission Strategy

| Permission | When requested | Required? |
|---|---|---|
| Screen Recording | First capture attempt | Yes (core feature) |
| Accessibility | Only when user enables a global hotkey | No (optional) |
| Notifications | Standard system prompt on first notification | No (can dismiss) |

Goal: a user who launches the app and takes their first screenshot sees exactly **one** permission prompt.

---

## Distribution

- **GitHub Releases** — primary. Each release ships a signed, notarized `.dmg` with the `.app` bundle inside.
- **Homebrew Cask** — `brew install --cask sharex-mac`. Maintained as a one-file tap recipe pointing to GitHub releases.
- **CI/CD** — GitHub Actions workflow: build → codesign → notarize → staple → attach `.dmg` to release.
- **Apple Developer account** required for signing/notarization ($99/year).
- **License:** GPL-3.0 (inherited from ShareX source). App Store distribution not applicable.

---

## Mac-Compatible Features

All features from ShareX that have macOS equivalents:

- Region / window / fullscreen screenshot
- Screen recording (MP4 + GIF)
- OCR (text extraction)
- Image annotation and effects
- File uploading (80+ services via UploadersLib)
- Post-capture clipboard copy / URL shortening
- Capture history
- Global hotkeys (optional, Accessibility permission)
- Color picker
- File indexer

**Phase 2 (deferred — complex on macOS):**
- Scrolling screenshot — requires simulating scroll + stitching frames; no direct macOS API equivalent

**Excluded (Windows-only, no Mac equivalent):**
- Shell extension / right-click context menu integration
- Windows toast notification style (replaced with macOS UserNotifications)
- Windows startup registry entry (replaced with macOS Login Items)
