License
GNU General Public License v3.0

# Audiobook-Auto-play
This is an auto advance to the next chapter of audio book plugin for Jellyfin //// server version:10.11.5


# Audiobook Auto-Play — Jellyfin Plugin

Automatically advances to the next chapter (or audio file) when an audiobook track finishes playing in Jellyfin.

---

## Features

| Feature | Description |
|---|---|
| Auto-advance | Moves to the next sibling audio file when the current one ends |
| Configurable delay | Optional pause (0–60 s) before the next chapter starts |
| Position-based detection | Detects track completion even when clients don't report it correctly |
| Works with Books libraries | Supports both `Audio` and `AudioBook` item types |

---

## Requirements

- **Jellyfin** 10.10+ (tested on 10.11.5)
- **.NET SDK 9.0** (for building)
- Jellyfin server DLLs for compilation (copy from your server's install directory)

---

## Build

```bash
cd AudiobookAutoPlay
dotnet clean -c Release
dotnet build -c Release
```

The compiled DLL will be at:

```
AudiobookAutoPlay/bin/Release/net9.0/AudiobookAutoPlay.dll
```

---

## Install (Linux)

### 1. Create the plugin folder

```bash
sudo mkdir -p /var/lib/jellyfin/plugins/AudiobookAutoPlay/
```

### 2. Copy ONLY the DLL

Copy `AudiobookAutoPlay.dll` into the plugin folder. Do **not** copy `.pdb`, `.deps.json`, `.xml`, or any Jellyfin/Microsoft DLLs.

```bash
sudo cp AudiobookAutoPlay.dll /var/lib/jellyfin/plugins/AudiobookAutoPlay/
```

### 3. Fix ownership (CRITICAL)

Jellyfin runs as the `jellyfin` user. If the plugin folder or its contents are owned by `root`, the server **will fail to start**. Always run:

```bash
sudo chown -R jellyfin:jellyfin /var/lib/jellyfin/plugins/AudiobookAutoPlay/
```

### 4. Restart Jellyfin

```bash
sudo systemctl restart jellyfin
```

### 5. Verify

Check the plugin's own log file:

```bash
cat /var/lib/jellyfin/plugins/configurations/AudiobookAutoPlay.log
```

You should see:

```
2026-03-13 12:00:00 Plugin loaded
2026-03-13 12:00:01 Playback listener active
```

---

## Important File Locations (Linux)

| Path | Description |
|---|---|
| `/var/lib/jellyfin/plugins/AudiobookAutoPlay/` | Plugin folder — contains `AudiobookAutoPlay.dll` and auto-generated `meta.json` |
| `/var/lib/jellyfin/plugins/configurations/AudiobookAutoPlay.xml` | Configuration file — edit this to change settings |
| `/var/lib/jellyfin/plugins/configurations/AudiobookAutoPlay.log` | Plugin log file — shows startup status and chapter advances |

### Ownership reminder

Every file under `/var/lib/jellyfin/plugins/` must be owned by `jellyfin:jellyfin`. After copying or editing any files, always run:

```bash
sudo chown -R jellyfin:jellyfin /var/lib/jellyfin/plugins/
```

If you forget this, Jellyfin will fail to start with `System.UnauthorizedAccessException: Access to the path 'meta.json' is denied.` in the logs.

---

## Configuration

Jellyfin 10.11+ uses a React-based dashboard that does not reliably load legacy plugin configuration pages. **Edit the XML file directly** to change settings:

```bash
sudo nano /var/lib/jellyfin/plugins/configurations/AudiobookAutoPlay.xml
sudo systemctl restart jellyfin
```

### Configuration options

```xml
<?xml version="1.0" encoding="utf-8"?>
<PluginConfiguration xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"
                     xmlns:xsd="http://www.w3.org/2001/XMLSchema">
  <EnableAutoPlay>true</EnableAutoPlay>           <!-- Master on/off switch -->
  <DelaySeconds>2</DelaySeconds>                  <!-- Seconds to wait before next chapter (0-60) -->
  <ShowCountdownNotification>true</ShowCountdownNotification>  <!-- Reserved for future use -->
  <StopAtEndOfBook>true</StopAtEndOfBook>         <!-- Stop after last chapter -->
  <SavePositionBeforeAdvance>true</SavePositionBeforeAdvance>  <!-- Reserved for future use -->
</PluginConfiguration>
```

| Setting | Default | Description |
|---|---|---|
| EnableAutoPlay | true | Master on/off switch |
| DelaySeconds | 2 | Pause between chapters (0 = immediate) |
| StopAtEndOfBook | true | Don't loop after last chapter |

---

## How It Works

```
PlaybackStopped event fires
        │
        ▼
Is it an Audio/AudioBook item?  ──No──▶ ignore
        │ Yes
        ▼
Did it finish? (PlayedToCompletion OR within last 15s)  ──No──▶ ignore
        │ Yes
        ▼
Find next sibling audio file in same parent folder
        │
        ├── Not found ──▶ end of book, stop
        │
        ▼
Wait [DelaySeconds]
        │
        ▼
Send PlayNow command to the same session
```

The plugin uses **position-based completion detection** as a fallback. Many Jellyfin clients (including the Windows desktop app) report `PlayedToCompletion=false` even when a track plays to the end. The plugin checks whether playback stopped within the last 15 seconds of the track's duration and treats that as a completed play.

The plugin searches for both `Audio` and `AudioBook` item types, so it works whether your audiobooks are in a Music library or a Books library.

---

## Project Structure

```
AudiobookAutoPlay/
├── AudiobookAutoPlay.sln
└── AudiobookAutoPlay/
    ├── AudiobookAutoPlay.csproj       # Project / references
    ├── Plugin.cs                      # Plugin class, event handling, auto-advance logic
    ├── PlaybackListener.cs            # IHostedService that wires up the event hook
    ├── PluginServiceRegistrator.cs    # DI registration for PlaybackListener
    └── Configuration/
        ├── PluginConfiguration.cs     # Settings model
        └── configPage.html            # Dashboard settings UI (limited on 10.11+)
```

---

## Troubleshooting

**Server won't start after installing the plugin**

- File permissions. Run `sudo chown -R jellyfin:jellyfin /var/lib/jellyfin/plugins/AudiobookAutoPlay/` and restart.
- If the server previously crashed with this plugin, Jellyfin may have written `"status": "Disabled"` into `meta.json`. Delete `meta.json` and restart — Jellyfin will recreate it.

**Plugin loads but nothing happens when a chapter ends**

- Check `/var/lib/jellyfin/plugins/configurations/AudiobookAutoPlay.log` for the "Playback listener active" line. If it's missing, the hosted service didn't start — try deleting `meta.json` and restarting.
- Make sure `EnableAutoPlay` is `true` in the XML config.
- Make sure your audiobook library is set to **Books** type in Jellyfin. The plugin searches for `AudioBook` items.

**Plugin log says no siblings found**

- Your audiobook files need to be in the same parent folder. The plugin looks for sibling items under the same parent container.

**Dashboard settings page is blank**

- This is a known limitation of Jellyfin 10.11's React-based dashboard. Edit the XML config file directly instead (see Configuration section above).

---

## License

GNU General Public License v3.0
