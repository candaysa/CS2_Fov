# CS2 FOV (Anatolia) for SwiftlyS2

A Swiftly S2 plugin for Counter-Strike 2 that lets players choose their Field of View within server-defined limits. Player FOV settings persist across sessions using SteamID-based storage.

## Features

- Player-set FOV with configurable min/max clamps
- Persists per-player FOV to `addons/swiftlys2/data/CS2_FOV/player_fov.json`
- FOV stays intact on weapon switches (uses `DesiredFOVUpdated`)
- Simple chat/console command
- Toggle the plugin on/off via config

## Commands

- `!fov <value>` — set your FOV within the allowed range
- `!fov` — reset your FOV to the default 90

## Installation

1) Install [SwiftlyS2](https://github.com/swiftly-solution/swiftlys2) on your server.
2) Build or download the plugin release.
3) Copy `Anatolia_Fov.dll` (and its `.deps.json` if needed) into `addons/swiftlys2/plugins/`.
4) Create or edit `configs/plugins/CS2_FOV/config.jsonc` (sample below).
5) Reload SwiftlyS2 or restart the server.

## Configuration

Place this in `configs/plugins/CS2_FOV/config.jsonc`:

```jsonc
{
  "Anatolia_Fov": {
    "PluginEnabled": true,
    "FOVMin": 80,
    "FOVMax": 130
  }
}
```

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `PluginEnabled` | bool | true | Enable or disable the plugin |
| `FOVMin` | int | 80 | Minimum allowed FOV value |
| `FOVMax` | int | 130 | Maximum allowed FOV value |

## Data storage

- Per-player FOV values are saved automatically to `addons/swiftlys2/data/CS2_FOV/player_fov.json` (created on first use).

## Building

From the repository root:

```bash
dotnet build CS2_FOV.sln -c Release
```

Output: `source/CS2_FOV/bin/Release/net10.0/Anatolia_Fov.dll`

## Version history

### v1.0.2 (Current)
- Added SteamID-based persistence (`player_fov.json`) and automatic reload on connect
- Default FOV falls back to 90 when no saved value exists
- Cleanup of in-memory session cache on disconnect

### v1.0.1
- FOV no longer resets when switching weapons
- Continuous application hook to keep FOV stable

### v1.0.0
- Initial release with basic FOV setting
