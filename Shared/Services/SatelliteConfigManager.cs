using System.Text.Json;
using WorldMapWallpaper.Shared.Models;

namespace WorldMapWallpaper.Shared.Services;

/// <summary>
/// Manages loading, saving, and manipulating satellite configurations.
/// </summary>
public class SatelliteConfigManager
{
    private static string ConfigDirectory => AppStoragePaths.EnsureDataDirectoryExists();

    private static string ConfigFilePath => AppStoragePaths.GetDataFilePath("satellites.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    private SatelliteConfigRoot _config;
    private static SatelliteConfigManager? _instance;
    private static readonly object _lock = new();

    /// <summary>
    /// Gets the singleton instance of the SatelliteConfigManager.
    /// </summary>
    public static SatelliteConfigManager Instance
    {
        get
        {
            if (_instance == null)
            {
                lock (_lock)
                {
                    _instance ??= new SatelliteConfigManager();
                }
            }
            return _instance;
        }
    }

    private SatelliteConfigManager()
    {
        _config = Load();
    }

    /// <summary>
    /// Gets the current configuration.
    /// </summary>
    public SatelliteConfigRoot Config => _config;

    /// <summary>
    /// Gets or sets the maximum number of visible satellites.
    /// </summary>
    public int MaxVisibleSatellites
    {
        get => _config.MaxVisibleSatellites;
        set
        {
            _config.MaxVisibleSatellites = Math.Clamp(value, 1, 15);
            Save();
        }
    }

    /// <summary>
    /// Gets or sets the visibility mode.
    /// </summary>
    public SatelliteVisibilityMode VisibilityMode
    {
        get => _config.VisibilityMode;
        set
        {
            _config.VisibilityMode = value;
            Save();
        }
    }

    /// <summary>
    /// Gets all configured satellites.
    /// </summary>
    public IReadOnlyList<SatelliteConfig> Satellites => _config.Satellites.AsReadOnly();

    /// <summary>
    /// Gets enabled satellites sorted by priority, limited to max visible.
    /// </summary>
    public IEnumerable<SatelliteConfig> GetVisibleSatellites()
    {
        return _config.Satellites
            .Where(s => s.Enabled)
            .OrderBy(s => s.Priority)
            .Take(_config.MaxVisibleSatellites);
    }

    /// <summary>
    /// Adds a new satellite to the configuration.
    /// </summary>
    /// <param name="satellite">The satellite configuration to add.</param>
    /// <returns>True if added, false if already exists.</returns>
    public bool AddSatellite(SatelliteConfig satellite)
    {
        if (_config.Satellites.Any(s => s.NoradId == satellite.NoradId))
            return false;

        // Auto-assign priority if not set
        if (satellite.Priority == 100)
        {
            satellite.Priority = _config.Satellites.Count > 0
                ? _config.Satellites.Max(s => s.Priority) + 1
                : 1;
        }

        // Auto-assign color if not set
        if (string.IsNullOrEmpty(satellite.Color))
        {
            satellite.Color = SatellitePreset.GetSuggestedColor(_config.Satellites.Count);
        }

        _config.Satellites.Add(satellite);
        Save();
        return true;
    }

    /// <summary>
    /// Adds a satellite from a preset.
    /// </summary>
    /// <param name="preset">The preset to add.</param>
    /// <returns>True if added, false if already exists.</returns>
    public bool AddFromPreset(SatellitePreset preset)
    {
        if (_config.Satellites.Any(s => s.NoradId == preset.NoradId))
            return false;

        var priority = _config.Satellites.Count > 0
            ? _config.Satellites.Max(s => s.Priority) + 1
            : 1;

        var config = preset.ToConfig(priority);
        _config.Satellites.Add(config);
        Save();
        return true;
    }

    /// <summary>
    /// Updates an existing satellite configuration.
    /// </summary>
    /// <param name="satellite">The updated satellite configuration.</param>
    /// <returns>True if updated, false if not found.</returns>
    public bool UpdateSatellite(SatelliteConfig satellite)
    {
        var index = _config.Satellites.FindIndex(s => s.NoradId == satellite.NoradId);
        if (index < 0)
            return false;

        _config.Satellites[index] = satellite;
        Save();
        return true;
    }

    /// <summary>
    /// Removes a satellite by NORAD ID.
    /// </summary>
    /// <param name="noradId">The NORAD ID of the satellite to remove.</param>
    /// <returns>True if removed, false if not found.</returns>
    public bool RemoveSatellite(int noradId)
    {
        var satellite = _config.Satellites.FirstOrDefault(s => s.NoradId == noradId);
        if (satellite == null)
            return false;

        _config.Satellites.Remove(satellite);
        Save();
        return true;
    }

    /// <summary>
    /// Sets the enabled state of a satellite.
    /// </summary>
    /// <param name="noradId">The NORAD ID.</param>
    /// <param name="enabled">Whether the satellite should be enabled.</param>
    /// <returns>True if updated, false if not found.</returns>
    public bool SetSatelliteEnabled(int noradId, bool enabled)
    {
        var satellite = _config.Satellites.FirstOrDefault(s => s.NoradId == noradId);
        if (satellite == null)
            return false;

        satellite.Enabled = enabled;
        Save();
        return true;
    }

    /// <summary>
    /// Reorders satellites by priority.
    /// </summary>
    /// <param name="noradIds">NORAD IDs in the desired priority order.</param>
    public void ReorderSatellites(IEnumerable<int> noradIds)
    {
        var priority = 1;
        foreach (var noradId in noradIds)
        {
            var satellite = _config.Satellites.FirstOrDefault(s => s.NoradId == noradId);
            if (satellite != null)
            {
                satellite.Priority = priority++;
            }
        }
        Save();
    }

    /// <summary>
    /// Checks if a satellite with the given NORAD ID exists.
    /// </summary>
    /// <param name="noradId">The NORAD ID to check.</param>
    /// <returns>True if exists, false otherwise.</returns>
    public bool ContainsSatellite(int noradId)
    {
        return _config.Satellites.Any(s => s.NoradId == noradId);
    }

    /// <summary>
    /// Gets a satellite by NORAD ID.
    /// </summary>
    /// <param name="noradId">The NORAD ID.</param>
    /// <returns>The satellite config, or null if not found.</returns>
    public SatelliteConfig? GetSatellite(int noradId)
    {
        return _config.Satellites.FirstOrDefault(s => s.NoradId == noradId);
    }

    /// <summary>
    /// Exports the current configuration to a file.
    /// </summary>
    /// <param name="filePath">The file path to export to.</param>
    public void Export(string filePath)
    {
        var json = JsonSerializer.Serialize(_config, JsonOptions);
        File.WriteAllText(filePath, json);
    }

    /// <summary>
    /// Imports configuration from a file.
    /// </summary>
    /// <param name="filePath">The file path to import from.</param>
    /// <param name="mergeMode">If true, merge with existing; if false, replace all.</param>
    /// <returns>The number of satellites imported.</returns>
    public int Import(string filePath, bool mergeMode)
    {
        var json = File.ReadAllText(filePath);
        var imported = JsonSerializer.Deserialize<SatelliteConfigRoot>(json, JsonOptions);

        if (imported == null)
            return 0;

        var count = 0;
        var importedSatellites = imported.Satellites ?? new List<SatelliteConfig>();

        if (mergeMode)
        {
            // Merge: update existing, add new
            foreach (var satellite in importedSatellites)
            {
                var existing = _config.Satellites.FirstOrDefault(s => s.NoradId == satellite.NoradId);
                if (existing != null)
                {
                    // Update existing
                    var index = _config.Satellites.IndexOf(existing);
                    _config.Satellites[index] = satellite;
                }
                else
                {
                    // Add new
                    _config.Satellites.Add(satellite);
                }
                count++;
            }
        }
        else
        {
            // Replace all
            _config.Satellites = importedSatellites;
            count = _config.Satellites.Count;
        }

        // Also import settings
        _config.MaxVisibleSatellites = Math.Clamp(imported.MaxVisibleSatellites, 1, 15);
        _config.VisibilityMode = imported.VisibilityMode;

        Save();
        return count;
    }

    /// <summary>
    /// Reloads the configuration from disk.
    /// </summary>
    public void Reload()
    {
        _config = Load();
    }

    /// <summary>
    /// Resets the configuration to defaults (ISS only).
    /// </summary>
    public void ResetToDefaults()
    {
        _config = CreateDefault();
        Save();
    }

    /// <summary>
    /// Saves the current configuration to disk.
    /// </summary>
    public void Save()
    {
        try
        {
            EnsureDirectoryExists();
            var json = JsonSerializer.Serialize(_config, JsonOptions);
            File.WriteAllText(ConfigFilePath, json);
        }
        catch
        {
            // Silently fail - configuration will use in-memory values
        }
    }

    private SatelliteConfigRoot Load()
    {
        try
        {
            if (File.Exists(ConfigFilePath))
            {
                var json = File.ReadAllText(ConfigFilePath);
                var config = JsonSerializer.Deserialize<SatelliteConfigRoot>(json, JsonOptions);
                if (config != null)
                    return config;
            }
        }
        catch
        {
            // Fall through to create default
        }

        // Create default configuration with ISS
        var defaultConfig = CreateDefault();
        _config = defaultConfig;
        Save();
        return defaultConfig;
    }

    private static SatelliteConfigRoot CreateDefault()
    {
        var issPreset = SatellitePreset.FindByNoradId(25544);
        var config = new SatelliteConfigRoot();

        if (issPreset != null)
        {
            config.Satellites.Add(issPreset.ToConfig(1));
        }

        return config;
    }

    private static void EnsureDirectoryExists()
    {
        Directory.CreateDirectory(ConfigDirectory);
    }
}
