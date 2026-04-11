# Custom Satellites Feature Proposal

## Overview

This document outlines a proposed enhancement to WorldMapWallpaper that would allow users to track multiple satellites beyond just the International Space Station (ISS). Users would be able to add custom satellites using NORAD catalog numbers, specify custom icons for each, and have fine-grained control over visibility.

---

## 1. Custom Satellite Support

### 1.1 Satellite Configuration

Users should be able to add satellites by specifying:

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `Name` | string | Yes | Display name (e.g., "Hubble Space Telescope") |
| `NoradId` | int | Yes | NORAD catalog number (e.g., 20580 for Hubble) |
| `Enabled` | bool | Yes | Whether to display this satellite |
| `IconPath` | string | No | Path to custom icon (null = use default) |
| `Color` | Color | No | Orbit trail color (null = auto-assign from palette) |
| `ShowOrbit` | bool | No | Whether to show orbital path (default: true) |
| `Priority` | int | No | Display priority when limit is reached (lower = higher priority) |

### 1.2 Data Storage

Satellites should be stored in a JSON configuration file:

**Location:** `%APPDATA%\WorldMapWallpaper\satellites.json`

```json
{
  "version": 1,
  "maxVisibleSatellites": 5,
  "defaultOrbitPoints": 50,
  "satellites": [
    {
      "name": "International Space Station",
      "noradId": 25544,
      "enabled": true,
      "iconPath": null,
      "color": "#FF6B35",
      "showOrbit": true,
      "priority": 1
    },
    {
      "name": "Hubble Space Telescope",
      "noradId": 20580,
      "enabled": true,
      "iconPath": "C:\\Users\\user\\icons\\hubble.png",
      "color": "#4ECDC4",
      "showOrbit": true,
      "priority": 2
    },
    {
      "name": "Tiangong Space Station",
      "noradId": 48274,
      "enabled": true,
      "iconPath": null,
      "color": "#FFE66D",
      "showOrbit": true,
      "priority": 3
    }
  ]
}
```

### 1.3 Lagrange Point Detection

Satellites at Lagrange points (L1, L2, L3, L4, L5) cannot be accurately projected onto an Earth-centered map. The application must:

1. **Detect Lagrange Point Satellites:** Maintain a list of known Lagrange point satellites:
   | NORAD ID | Name | Location |
   |----------|------|----------|
   | 50463 | James Webb Space Telescope | L2 |
   | 43435 | DSCOVR | L1 |
   | 39479 | Gaia | L2 |
   | 28928 | SOHO | L1 |
   | 52195 | Euclid | L2 |

2. **Orbital Detection:** If semi-major axis > 500,000 km, flag as potential Lagrange point

3. **User Warning:** When user attempts to add a Lagrange point satellite:
   ```
   +--------------------------------------------------+
   |  Warning                                    [X]  |
   +--------------------------------------------------+
   |                                                   |
   |  [!] James Webb Space Telescope is located at    |
   |      Lagrange Point L2, approximately 1.5        |
   |      million km from Earth.                      |
   |                                                   |
   |      Satellites at Lagrange points cannot be     |
   |      correctly projected onto the Earth map.     |
   |      The position shown will not be accurate.    |
   |                                                   |
   |      Do you still want to add this satellite?    |
   |                                                   |
   |              [Cancel]  [Add Anyway]              |
   +--------------------------------------------------+
   ```

4. **Visual Indicator:** If added anyway, show with a distinct indicator (e.g., "~" prefix or different icon border) to remind user it's not accurately positioned.

### 1.4 Preset Satellites

Include a set of well-known Earth-orbiting satellites as presets:

| Name | NORAD ID | Category | Orbit |
|------|----------|----------|-------|
| International Space Station | 25544 | Space Station | LEO |
| Tiangong Space Station | 48274 | Space Station | LEO |
| Hubble Space Telescope | 20580 | Science | LEO |
| Landsat 9 | 49260 | Earth Observation | LEO |
| GOES-18 | 51850 | Weather | GEO |
| NOAA-20 | 43013 | Weather | LEO |
| Terra | 25994 | Earth Science | LEO |
| Aqua | 27424 | Earth Science | LEO |
| GPS IIF-12 | 41019 | Navigation | MEO |
| Sentinel-2A | 40697 | Earth Observation | LEO |

**Note:** Satellite constellations (Starlink, OneWeb, etc.) are excluded because individual satellites change frequently and cannot be reliably tracked over time.

### 1.5 TLE Data Management

#### Fetching Strategy

```
Maximum Satellites: 50 (hard limit)
Request Timeout: 5 seconds per request
```

**Batch Request (Preferred):**
```
GET https://celestrak.org/NORAD/elements/gp.php?CATNR=25544,20580,48274&FORMAT=TLE
```

If batch requests are supported, fetch all enabled satellites in a single request.

**Parallel Async Fallback:**
If batch is not supported or fails, make parallel async requests:

```csharp
public async Task<Dictionary<int, TleData>> FetchMultipleTleAsync(
    IEnumerable<int> noradIds,
    CancellationToken cancellationToken)
{
    var results = new ConcurrentDictionary<int, TleData>();
    var tasks = noradIds.Select(async id =>
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(TimeSpan.FromSeconds(5)); // 5s timeout per request

        try
        {
            var tle = await FetchSingleTleAsync(id, cts.Token);
            results.TryAdd(id, tle);
        }
        catch (OperationCanceledException)
        {
            // Timeout - will use cache fallback
        }
        catch (Exception)
        {
            // Network error - will use cache fallback
        }
    });

    await Task.WhenAll(tasks);
    return results.ToDictionary(kv => kv.Key, kv => kv.Value);
}
```

#### Fallback Strategy

```
1. Try batch API request (5s timeout)
   |
   +-- Success --> Cache results, return fresh TLE data
   |
   +-- Timeout/Failure --> Try parallel individual requests
                           |
                           +-- For each satellite:
                               |
                               +-- Success --> Cache, use fresh
                               |
                               +-- Timeout --> Check cache
                                               |
                                               +-- Cache valid --> Use cached TLE for orbital calculation
                                               |
                                               +-- Cache expired/missing --> Skip satellite (don't display)
```

#### Cache Configuration

```json
{
  "tleCacheDirectory": "%APPDATA%\\WorldMapWallpaper\\tle_cache\\",
  "cacheExpirationHours": 168,  // 7 days
  "staleDataMaxAgeDays": 30     // Use stale data up to 30 days if no network
}
```

---

## 2. Custom Icons

### 2.1 Icon Specifications

| Property | Requirement |
|----------|-------------|
| Format | PNG with transparency (preferred), ICO, BMP |
| Size | 32x32 pixels (recommended), will be scaled if different |
| Color Mode | RGBA (32-bit with alpha channel) |
| File Size | Max 256 KB |

### 2.2 Default Icons

Provide a set of built-in icons for common satellite types:

```
Resources/
  Icons/
    satellite-default.png    # Generic satellite
    satellite-station.png    # Space stations (ISS, Tiangong)
    satellite-telescope.png  # Telescopes (Hubble)
    satellite-weather.png    # Weather satellites (GOES, NOAA)
    satellite-earth-obs.png  # Earth observation (Landsat, Sentinel)
    satellite-science.png    # Science satellites (Terra, Aqua)
    satellite-gps.png        # Navigation satellites
```

### 2.3 Icon Assignment Logic

```
1. If custom IconPath is specified and file exists --> use custom icon
2. Else if satellite category is known --> use category-specific icon
3. Else --> use satellite-default.png
```

### 2.4 Icon Scaling

- Icons are scaled to fit within a 24x24 to 48x48 pixel bounding box
- Use high-quality bicubic interpolation for scaling
- Maintain aspect ratio (no stretching)

---

## 3. Orbit Trail Colors

### 3.1 Auto-Assignment with User Override

The application provides suggested colors from a visually distinct palette, but users can customize:

**Default Color Palette:**
```csharp
private static readonly string[] DefaultOrbitColors =
{
    "#FF6B35",  // Orange (ISS default)
    "#4ECDC4",  // Teal
    "#FFE66D",  // Yellow
    "#95E1D3",  // Mint
    "#F38181",  // Coral
    "#AA96DA",  // Lavender
    "#81B214",  // Green
    "#2C786C",  // Dark Teal
    "#F9ED69",  // Light Yellow
    "#B83B5E",  // Magenta
};
```

**Assignment Logic:**
1. New satellite gets next unused color from palette
2. If all colors used, cycle back with slight variation
3. User can override via color picker in Edit dialog
4. Custom colors saved to satellite configuration

**Color Picker in UI:**
```
Color: [#FF6B35] [Pick...] [Reset to Suggested]
       [Preview swatch showing orbit trail sample]
```

---

## 4. Satellite Visibility Limits

### 4.1 Performance Limits

| Setting | Min | Default | Max | Notes |
|---------|-----|---------|-----|-------|
| Max Visible Satellites | 1 | 5 | 15 | UI enforced |
| Max Configurable Satellites | - | - | 50 | Total in config file |
| Orbit Trail Points | 20 | 50 | 100 | Points before + after |
| Orbit Trail Width | 1 | 2 | 5 | Pixels |

### 4.2 Priority System

When more satellites are enabled than the visibility limit:

1. Sort enabled satellites by `Priority` (ascending, lower = higher priority)
2. Take the first N satellites where N = `maxVisibleSatellites`
3. Satellites with failed TLE data are skipped (don't count against limit)

**Priority UI:**
- Drag-and-drop reordering in satellite list
- Or explicit priority number field in Edit dialog
- Priority auto-assigned on add (next available number)

### 4.3 Visibility Modes

| Mode | Description |
|------|-------------|
| `All` | Show all enabled satellites (up to limit) |
| `DaylightOnly` | Only show satellites currently in sunlight |

**Future consideration:** `VisibleFromLocation` mode requiring user's geographic coordinates.

---

## 5. Settings UI Redesign

### 5.1 Design Principles

- **Tabbed Interface:** Organize related settings into logical groups
- **Live Preview:** Real-time thumbnail showing wallpaper with current settings
- **Consistent Theme:** Leverage existing dark/light mode support
- **Responsive:** Accommodate new satellite management features

### 5.2 Main Window Layout

```
+----------------------------------------------------------+
|  World Map Wallpaper Settings                        [X]  |
+----------------------------------------------------------+
|  [Display]  [Satellites]  [Schedule]  [Advanced]         |
+----------------------------------------------------------+
|                                                           |
|  +-----------------------------------------------------+ |
|  |                                                     | |
|  |                  (Tab content area)                 | |
|  |                                                     | |
|  |                                                     | |
|  +-----------------------------------------------------+ |
|                                                           |
|  LIVE PREVIEW                                             |
|  +-----------------------------------------------------+ |
|  |                                                     | |
|  |              [Live Thumbnail 320x180]               | |
|  |                                                     | |
|  +-----------------------------------------------------+ |
|  Last updated: 2 seconds ago          [Refresh Preview]  |
|                                                           |
+----------------------------------------------------------+
|  [Update Wallpaper Now]              [Reset All] [Close] |
+----------------------------------------------------------+
```

### 5.3 Tab: Display

```
+----------------------------------------------------------+
| VISUAL ELEMENTS                                           |
| +------------------------------------------------------+ |
| | [x] Show time zone clocks around the world           | |
| | [x] Show political boundaries and country borders    | |
| +------------------------------------------------------+ |
|                                                           |
| RESOLUTION                                                |
| +------------------------------------------------------+ |
| | Mode: [Fit to Screen          v]                     | |
| | Detected: 2560 x 1440                                | |
| |                                                       | |
| | Custom: [    0    ] x [    0    ]                    | |
| | (Set both to 0 to use detected resolution)           | |
| +------------------------------------------------------+ |
+----------------------------------------------------------+
```

### 5.4 Tab: Satellites

```
+----------------------------------------------------------+
| SATELLITE TRACKING                                        |
| +------------------------------------------------------+ |
| | [x] Enable satellite tracking                         | |
| |                                                       | |
| | Max visible satellites: [  5  v]  (1-15)             | |
| | Visibility mode: [All satellites  v]                 | |
| +------------------------------------------------------+ |
|                                                           |
| TRACKED SATELLITES                          [Import]      |
| +------------------------------------------------------+ |
| | # | On |      Name                 | NORAD | Actions | |
| |---|----|-----------------------------|-------|---------|
| | 1 | [x]| [O] International Space... | 25544 | [Edit]  | |
| | 2 | [x]| [O] Hubble Space Telescope | 20580 | [Edit]  | |
| | 3 | [ ]| [O] Tiangong Space Station | 48274 | [Edit]  | |
| |   |    |                             |       |         | |
| +------------------------------------------------------+ |
| | Drag rows to reorder priority. Lower = higher priority |
| +------------------------------------------------------+ |
|                                                           |
| [+ Add Satellite]  [+ Add from Presets]  [Export]        |
|                                        [Remove Selected] |
+----------------------------------------------------------+
```

**Legend:**
- `[O]` = Colored circle showing orbit trail color
- `#` = Priority number
- Rows are draggable for reordering

### 5.5 Tab: Schedule

```
+----------------------------------------------------------+
| UPDATE SCHEDULE                                           |
| +------------------------------------------------------+ |
| | Update Frequency: [Every 15 minutes  v]              | |
| |                                                       | |
| | More frequent updates ensure accurate day/night      | |
| | cycles and satellite positions.                      | |
| +------------------------------------------------------+ |
|                                                           |
| TASK STATUS                                               |
| +------------------------------------------------------+ |
| | Status: [*] Active                                   | |
| | Next Run: Jan 22, 2026 at 3:45 PM                    | |
| | Triggers: 3 (Interval, Logon, Wake)                  | |
| |                                                       | |
| | [Disable Task]  [Run Now]  [View in Task Scheduler]  | |
| +------------------------------------------------------+ |
|                                                           |
| STARTUP OPTIONS                                           |
| +------------------------------------------------------+ |
| | [x] Run at Windows startup                           | |
| | [x] Run when resuming from sleep                     | |
| | [x] Minimize to system tray on close                 | |
| +------------------------------------------------------+ |
+----------------------------------------------------------+
```

### 5.6 Tab: Advanced

```
+----------------------------------------------------------+
| DATA MANAGEMENT                                           |
| +------------------------------------------------------+ |
| | TLE Cache: %APPDATA%\WorldMapWallpaper\tle_cache\    | |
| | Cache Status: 3 satellites cached, oldest 2h ago     | |
| |                                                       | |
| | [Refresh All TLE Data]  [Clear Cache]                | |
| +------------------------------------------------------+ |
|                                                           |
| WALLPAPER OUTPUT                                          |
| +------------------------------------------------------+ |
| | Location: %USERPROFILE%\Pictures\                    | |
| | Current: WorldMap01.jpg (2.4 MB)                     | |
| |                                                       | |
| | [Open Folder]  [Change Location...]                  | |
| +------------------------------------------------------+ |
|                                                           |
| LOGGING                                                   |
| +------------------------------------------------------+ |
| | Log Level: [Info  v]  (Debug, Info, Warning, Error)  | |
| | Log File: ...\WorldMapWallpaper\log\                 | |
| |                                                       | |
| | [View Log File]  [Clear Log]                         | |
| +------------------------------------------------------+ |
|                                                           |
| ABOUT                                                     |
| +------------------------------------------------------+ |
| | WorldMapWallpaper v1.5.26.0122                       | |
| | Copyright (c) 2023-2026                              | |
| |                                                       | |
| | [GitHub Repository]  [Report Issue]                  | |
| +------------------------------------------------------+ |
+----------------------------------------------------------+
```

### 5.7 Dialog: Edit Satellite

```
+--------------------------------------------+
|  Edit Satellite                       [X]  |
+--------------------------------------------+
|                                            |
|  Name:     [International Space Station  ] |
|  NORAD ID: [25544                        ] |
|                                            |
|  DISPLAY OPTIONS                           |
|  +----------------------------------------+|
|  | [x] Enabled                            ||
|  | [x] Show orbital path                  ||
|  +----------------------------------------+|
|                                            |
|  APPEARANCE                                |
|  +----------------------------------------+|
|  | Priority: [  1  ] (lower = higher)     ||
|  |                                        ||
|  | Orbit Color:                           ||
|  | [#FF6B35] [Pick...] [Suggest]          ||
|  | [====== Color preview bar ======]      ||
|  +----------------------------------------+|
|                                            |
|  ICON                                      |
|  +----------------------------------------+|
|  | (*) Use default icon for category      ||
|  |     Category: [Space Station  v]       ||
|  |                                        ||
|  | ( ) Use custom icon:                   ||
|  |     [                      ] [Browse]  ||
|  |                                        ||
|  | Preview: [32x32 icon preview]          ||
|  +----------------------------------------+|
|                                            |
|             [Cancel]  [Save]               |
+--------------------------------------------+
```

### 5.8 Dialog: Add from Presets

```
+----------------------------------------------------+
|  Add Satellites from Presets                  [X]  |
+----------------------------------------------------+
|                                                     |
|  Filter: [All Categories  v]  Search: [________]   |
|                                                     |
|  +-----------------------------------------------+ |
|  | [ ] | Name                      | ID    | Cat | |
|  |-----|---------------------------|-------|-----| |
|  | [-] | International Space St... | 25544 | Stn | |
|  |     | (Already added)                         | |
|  |-----|---------------------------|-------|-----| |
|  | [x] | Hubble Space Telescope    | 20580 | Sci | |
|  |-----|---------------------------|-------|-----| |
|  | [x] | Tiangong Space Station    | 48274 | Stn | |
|  |-----|---------------------------|-------|-----| |
|  | [ ] | Landsat 9                 | 49260 | EO  | |
|  |-----|---------------------------|-------|-----| |
|  | [ ] | GOES-18                   | 51850 | Wx  | |
|  |-----|---------------------------|-------|-----| |
|  | [ ] | Terra                     | 25994 | Sci | |
|  +-----------------------------------------------+ |
|                                                     |
|  Selected: 2 satellites                            |
|                                                     |
|                [Cancel]  [Add Selected]            |
+----------------------------------------------------+
```

### 5.9 Dialog: Import/Export

**Export Dialog:**
```
+--------------------------------------------+
|  Export Satellite Configuration       [X]  |
+--------------------------------------------+
|                                            |
|  Export includes:                          |
|  - All configured satellites (3)           |
|  - Custom colors and priorities            |
|  - Visibility settings                     |
|                                            |
|  Note: Custom icon files are NOT exported. |
|  Only icon paths are saved in the config.  |
|                                            |
|  Save to: [                    ] [Browse]  |
|                                            |
|           [Cancel]  [Export]               |
+--------------------------------------------+
```

**Import Dialog:**
```
+--------------------------------------------+
|  Import Satellite Configuration       [X]  |
+--------------------------------------------+
|                                            |
|  File: satellites_backup.json              |
|                                            |
|  Found 5 satellites in configuration:      |
|  +--------------------------------------+  |
|  | [x] ISS (25544) - will update        |  |
|  | [x] Hubble (20580) - will update     |  |
|  | [x] Tiangong (48274) - NEW           |  |
|  | [x] Landsat 9 (49260) - NEW          |  |
|  | [x] GOES-18 (51850) - NEW            |  |
|  +--------------------------------------+  |
|                                            |
|  Import mode:                              |
|  (*) Merge with existing (update + add)   |
|  ( ) Replace all (delete existing first)  |
|                                            |
|           [Cancel]  [Import]               |
+--------------------------------------------+
```

### 5.10 Live Preview Implementation

The live preview thumbnail should:

1. **Size:** 320x180 pixels (16:9 aspect ratio)
2. **Update Frequency:** Every 5 seconds while Settings window is open
3. **Generation:** Use a lightweight render path (skip some effects if needed)
4. **Interactivity:** Click to see full-size preview in a popup

```csharp
private async Task UpdatePreviewAsync()
{
    // Generate preview on background thread
    var previewBitmap = await Task.Run(() =>
    {
        // Use current settings to generate small preview
        return GeneratePreview(320, 180, Settings.Current);
    });

    // Update UI on main thread
    _previewPictureBox.Image?.Dispose();
    _previewPictureBox.Image = previewBitmap;
    _lastUpdateLabel.Text = $"Last updated: just now";
}

private System.Windows.Forms.Timer _previewTimer;

private void StartPreviewUpdates()
{
    _previewTimer = new System.Windows.Forms.Timer { Interval = 5000 };
    _previewTimer.Tick += async (s, e) => await UpdatePreviewAsync();
    _previewTimer.Start();

    // Initial update
    _ = UpdatePreviewAsync();
}
```

---

## 6. Implementation Plan

### 6.1 Phase 1: Core Infrastructure

**New Files:**
```
Shared/
  Models/
    SatelliteConfig.cs       # Satellite configuration model
    SatellitePreset.cs       # Preset satellite definition
  Services/
    SatelliteConfigManager.cs  # Load/save satellite configurations
    BatchTleService.cs         # Batch TLE fetching with timeout
    LagrangePointDetector.cs   # Detect L-point satellites
```

**Modified Files:**
```
Shared/
  Settings.cs                # Add satellite-related settings
  TleDataService.cs          # Support batch fetching, timeouts
```

### 6.2 Phase 2: Multi-Satellite Tracking

**New Files:**
```
ImagePainter/
  MultiSatelliteTracker.cs   # Track and render multiple satellites
  SatelliteRenderer.cs       # Render single satellite (extracted from ISSTracker)
  Resources/
    Icons/
      satellite-default.png
      satellite-station.png
      satellite-telescope.png
      satellite-weather.png
      satellite-earth-obs.png
      satellite-science.png
      satellite-gps.png
```

**Modified Files:**
```
ImagePainter/
  Program.cs                 # Use MultiSatelliteTracker
  ISSTracker.cs              # Refactor, possibly deprecate
```

### 6.3 Phase 3: Settings UI Redesign

**New Files:**
```
Settings/
  Controls/
    SatelliteListControl.cs    # Draggable satellite list
    LivePreviewControl.cs      # Live thumbnail preview
  Dialogs/
    SatelliteEditDialog.cs     # Add/Edit satellite
    PresetSelectorDialog.cs    # Select from presets
    ImportExportDialog.cs      # Import/Export config
    LagrangeWarningDialog.cs   # L-point warning
  TabPages/
    DisplayTabPage.cs          # Display settings tab
    SatellitesTabPage.cs       # Satellite management tab
    ScheduleTabPage.cs         # Schedule settings tab
    AdvancedTabPage.cs         # Advanced settings tab
```

**Modified Files:**
```
Settings/
  SettingsForm.cs              # Refactor to tabbed interface
  SettingsForm.Designer.cs     # Update layout
```

### 6.4 Phase 4: Polish and Testing

- Icon set finalization
- Color palette refinement
- Performance testing with 15 satellites
- Import/Export testing
- Lagrange point detection testing
- UI accessibility review

---

## 7. Design Decisions Summary

| Question | Decision |
|----------|----------|
| Lagrange point satellites? | Warn user, allow with disclaimer |
| API request limits? | 50 satellites max, 5s timeout, batch preferred, async fallback |
| Satellite groups (Starlink)? | No - cannot be reliably tracked over time |
| Orbit colors? | Auto-suggest from palette, user can override |
| Live preview? | Yes - 320x180 thumbnail, updates every 5 seconds |
| Dark/Light mode? | Already implemented - leverage existing |
| Import/Export? | Yes - JSON configuration files |

---

## 8. Future Enhancements

After initial implementation, consider:

- **Ground Station Tracking:** Show satellite passes over user's location
- **Visibility Predictions:** Calculate when satellites are visible
- **Multi-Monitor Support:** Different wallpapers per monitor
- **Satellite Telemetry Overlay:** Show altitude, speed, etc.
- **Notification System:** Alert when specific satellite is overhead
- **Cloud Sync:** Sync satellite configurations across devices

---

## Appendix A: Earth-Orbiting Satellites by NORAD ID

| NORAD ID | Name | Type | Orbit |
|----------|------|------|-------|
| 25544 | ISS (ZARYA) | Space Station | LEO |
| 48274 | CSS (TIANHE) | Space Station | LEO |
| 20580 | HST (Hubble) | Telescope | LEO |
| 43013 | NOAA 20 | Weather | LEO |
| 51850 | GOES 18 | Weather | GEO |
| 49260 | LANDSAT 9 | Earth Obs | LEO |
| 41019 | GPS IIF-12 | Navigation | MEO |
| 28654 | NOAA 18 | Weather | LEO |
| 33591 | NOAA 19 | Weather | LEO |
| 27424 | AQUA | Earth Science | LEO |
| 25994 | TERRA | Earth Science | LEO |
| 40697 | SENTINEL-2A | Earth Obs | LEO |
| 39084 | LANDSAT 8 | Earth Obs | LEO |

---

## Appendix B: Known Lagrange Point Satellites

These satellites should trigger a warning if user attempts to add them:

| NORAD ID | Name | L-Point | Distance from Earth |
|----------|------|---------|---------------------|
| 50463 | James Webb Space Telescope | L2 | ~1.5 million km |
| 43435 | DSCOVR | L1 | ~1.5 million km |
| 39479 | Gaia | L2 | ~1.5 million km |
| 28928 | SOHO | L1 | ~1.5 million km |
| 52195 | Euclid | L2 | ~1.5 million km |

---

## Appendix C: CelesTrak API Reference

**Single Satellite TLE:**
```
GET https://celestrak.org/NORAD/elements/gp.php?CATNR=25544&FORMAT=TLE
```

**Multiple Satellites (comma-separated):**
```
GET https://celestrak.org/NORAD/elements/gp.php?CATNR=25544,20580,48274&FORMAT=TLE
```

**By Group:**
```
GET https://celestrak.org/NORAD/elements/gp.php?GROUP=stations&FORMAT=TLE
```

**Response Format (TLE):**
```
ISS (ZARYA)
1 25544U 98067A   24001.50000000  .00016717  00000-0  10270-3 0  9025
2 25544  51.6400 208.9163 0006703 276.5736  83.4654 15.49999999999999
```

---

*Document Version: 2.0*
*Last Updated: January 2026*
