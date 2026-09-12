# HeroDangle

A Windows always-on-top overlay that hangs a lucky charm from a physically simulated thread. Grab it, flick it, or let it sway in the breeze. Clicks everywhere else pass through to the desktop.

## Requirements

- Windows 10/11
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

## Build and run

```powershell
dotnet restore HeroDangle.sln
dotnet run --project HeroDangle\HeroDangle.csproj -c Release
```

## Usage

| Input | Behavior |
|---|---|
| `Ctrl+Alt+D` (default, rebindable) | Summon / dismiss with a spring animation |
| Left-drag the charm | Rope trails the cursor |
| Release | Flicks with the drag velocity |
| Drag near the top edge | Re-pins the nail horizontally |
| Click anywhere else | Fully click-through |
| Tray icon | Charm gallery, reposition, hotkey, launch on startup, quit |

Settings live in `%AppData%\HeroDangle\config.json`.
