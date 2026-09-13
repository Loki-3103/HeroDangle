# HeroDangle

A Windows desktop overlay that hangs a charm from a physically simulated thread. The charm sways with simulated wind, can be grabbed, tossed, and flicked, and settles in as a light, always-on-top companion on your screen. The window is click-through everywhere except the charm itself, so it never blocks your work.

The word **"dangle"** in the name is the point — everything in the app hangs, swings, and droops with soft rope physics.

## Features

- **Always-on-top charm** pinned to the screen with a simulated thread (Verlet / rope physics).
- **Grab, drag & flick** — the charm trails the cursor with momentum and snaps back.
- **Wind sway** — a subtle animated breeze keeps the charm gently moving.
- **Click-through overlay** — mouse events pass straight through the window outside the charm.
- **7 built-in charms** (vector-rendered): Nazar 🧿, Lemon 🍋, Chili 🌶️, Horseshoe 🐴, Clover 🍀, Hamsa 🪬, Batman 🦇 (image charm).
- **Custom emoji charm** — use any emoji you want as the charm.
- **Right-click charm menu** — hover the charm, right-click, and pick a different charm instantly (drawn directly on the overlay, no popup windows).
- **Reposition mode** — move the charm's hanging point anywhere on screen.
- **Summon / dismiss animation** — springy, underdamped drop-in when summoned.
- **System tray control** — charm gallery, custom emoji, reposition, hotkey display, launch on startup, and quit from the tray icon.
- **Per-monitor DPI aware** rendering (SkiaSharp).
- **Persistent configuration** — settings survive restarts.

## Installation

### Prerequisites

- **Windows 10 or 11**
- **[.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)** (only needed to build from source)

### Build from source

```powershell
git clone https://github.com/Loki-3103/HeroDangle.git
cd HeroDangle
dotnet restore HeroDangle\HeroDangle.sln
dotnet build HeroDangle\HeroDangle.sln -c Release
```

### Run (from source)

```powershell
dotnet run --project HeroDangle\HeroDangle.csproj -c Release
```

or launch the built executable:

```powershell
HeroDangle\bin\Release\net8.0-windows\HeroDangle.exe
```

### Ready-to-run (no build needed)

Download the latest `HeroDangle.zip` from the [Releases](https://github.com/Loki-3103/HeroDangle/releases) page — it is a self-contained Windows 64-bit build (no .NET install required). Unzip anywhere, then:

- Double-click `HeroDangle.exe` to start.
- Optional: run `Create Shortcut.bat` once to place a "HeroDangle" shortcut on your desktop.

## Usage

After launch the app lives in your **system tray** (notification area). Use `Ctrl+Alt+D` (default, rebindable) to summon and dismiss the dangle.

| Input | Behavior |
|---|---|
| `Ctrl+Alt+D` (default) | Summon / dismiss the charm with a spring animation |
| Left-drag the charm | Rope trails the cursor |
| Release | Flicks with the drag velocity |
| Drag near the top edge | Re-pins the nail (anchor) horizontally |
| Right-click the charm | Open the charm switcher menu; pick a new charm instantly |
| Click anywhere else | Passes through to the desktop |
| Tray icon menu | Charm gallery, custom emoji, reposition mode, hotkey, launch-on-startup, quit |

### Configuration

Settings are stored in `%AppData%\HeroDangle\config.json`:

| Key | Meaning |
|---|---|
| `charmId` | Active charm id (`nazar`, `lemon`, `chili`, `horseshoe`, `clover`, `hamsa`, `batman`, or `custom`) |
| `customEmoji` | Emoji used when `charmId` is `custom` |
| `anchorXNormalized` | Horizontal anchor position as a fraction of screen width (0–1) |
| `hotkeyModifiers` | Hotkey modifier flags (e.g. `3` = `Ctrl+Alt`, `4` = shutdown disabled reserved) |
| `hotkeyVirtualKey` | Hotkey virtual-key code (e.g. `68` = `D`) |
| `launchOnStartup` | Whether the app auto-starts with Windows |
| `summoned` | Whether the dangle was visible on last exit |

You can also set the charm, emoji, anchor, and startup behavior through the tray menu — no manual file editing required.

## Project Structure

```
HeroDangle/
├── HeroDangle/               # Main WPF application
│   ├── App.xaml(.cs)         # Entry point, app wiring
│   ├── MainWindow.xaml(.cs)  # Overlay window, rendering loop, drag & menu logic
│   ├── CharmRenderer.cs      # SkiaSharp drawing (rope, nail, charms, image charm)
│   ├── CharmCatalog.cs       # Built-in charm definitions + charmed resolution
│   ├── RopeSolver.cs         # Verlet rope simulation
│   ├── OverlayWindow.cs      # Click-through / topmost window chrome
│   ├── HotkeyManager.cs      # Global hotkey registration
│   ├── TrayManager.cs        # System tray icon & menu
│   ├── ConfigService.cs      # JSON config load/save (%AppData%\HeroDangle)
│   ├── StartupManager.cs     # Launch-on-startup registry handling
│   ├── SettingsWindow.xaml   # Hotkey/settings UI
│   ├── EmojiPromptWindow.xaml# Custom emoji picker
│   ├── SimplexNoise.cs       # Wind noise generator
│   ├── Vec2.cs, NativeMethods.cs
│   └── Assets/batman.png     # Batman charm image asset
├── tools/
│   └── Create Shortcut.bat   # One-time desktop shortcut installer for releases
└── .github/workflows/
    └── release.yml           # Auto-builds & attaches HeroDangle.zip to releases
```

## Contributing

New charms are welcome and easy to add.

1. **Add the charm to the catalog** — open `HeroDangle\CharmCatalog.cs` and append an entry, e.g. `new("myid", "My Charm", "✨", CharmKind.Vector)`. Charm ids must be lowercase `-`/`_`-free slugs, unique within the catalog.
2. **Draw it** — vector charms are rendered in `CharmRenderer.cs` inside `DrawVectorCharm(...)`. Use the `r` radius unit and existing style (bright colors, subtle highlights). For image charms, add `CharmKind.Image`, place the asset under `HeroDangle\Assets\`, and include it in the `.csproj`.
3. **Code style** — follow the existing conventions: `.editorconfig`-clean, TS-friendly style, no public-native dependencies beyond SkiaSharp/WPF, colors as `SKColor(r, g, b[, a])`.
4. **Test** — build with `dotnet build HeroDangle\HeroDangle.sln -c Release`, run, and confirm the charm renders correctly at 100% and 150% DPI.
5. **Open a PR** with a short description and, if applicable, a screenshot.

### Pull request process

- Branch from `main`, keep commits small and focused.
- Preserve the naming conventions (lowercase ids).
- No changes to built binaries or `bin/`/`obj/` folders.
- CI (`release.yml`) will verify the project builds.

## Future Roadmap

- **More charms** — additional vector-rendered charms following the existing catalog pattern.
- **Configurable charm scale** — per-charm size control.
- **Multiple dangles** — more than one charm on screen at once.
- **Settings window polish** — inline hotkey recorder and preview.
- **Better wind / collision** — richer physics options (collision with screen edges, stronger gusts).

## License

Licensing is currently undecided — pending a license choice (MIT is the likely default). No license yet means all rights reserved by default; do not re-distribute until a license is added.