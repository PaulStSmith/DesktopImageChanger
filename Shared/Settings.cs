using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Win32;

namespace WorldMapWallpaper.Shared;

/// <summary>
/// Manages application settings stored in a scope-aware JSON configuration file.
/// </summary>
public static class Settings
{
    private const string RegistryKeyPath = @"SOFTWARE\WorldMapWallpaper";
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private static readonly object SyncRoot = new();
    private static SettingsData? _cachedSettings;

    /// <summary>
    /// Gets or sets whether to show the International Space Station on the wallpaper.
    /// </summary>
    public static bool ShowISS
    {
        get => GetSettings().ShowISS;
        set => Update(settings => settings.ShowISS = value);
    }

    /// <summary>
    /// Gets or sets whether to show time zone clocks on the wallpaper.
    /// </summary>
    public static bool ShowTimeZones
    {
        get => GetSettings().ShowTimeZones;
        set => Update(settings => settings.ShowTimeZones = value);
    }

    /// <summary>
    /// Gets or sets whether to show the political map overlay on the wallpaper.
    /// </summary>
    public static bool ShowPoliticalMap
    {
        get => GetSettings().ShowPoliticalMap;
        set => Update(settings => settings.ShowPoliticalMap = value);
    }

    /// <summary>
    /// Gets or sets the wallpaper update interval.
    /// </summary>
    public static UpdateInterval UpdateInterval
    {
        get => GetSettings().UpdateInterval;
        set => Update(settings => settings.UpdateInterval = value);
    }

    /// <summary>
    /// Gets or sets whether the wallpaper is currently active (being used by Windows).
    /// </summary>
    public static bool IsActive
    {
        get => GetSettings().IsActive;
        set => Update(settings => settings.IsActive = value);
    }

    /// <summary>
    /// Gets or sets the resolution scaling mode for the wallpaper.
    /// </summary>
    public static ResolutionMode ResolutionMode
    {
        get => GetSettings().ResolutionMode;
        set => Update(settings => settings.ResolutionMode = value);
    }

    /// <summary>
    /// Gets or sets the custom resolution width (0 = use auto-detect from screen).
    /// </summary>
    public static int CustomResolutionWidth
    {
        get => GetSettings().CustomResolutionWidth;
        set => Update(settings => settings.CustomResolutionWidth = Math.Max(0, value));
    }

    /// <summary>
    /// Gets or sets the custom resolution height (0 = use auto-detect from screen).
    /// </summary>
    public static int CustomResolutionHeight
    {
        get => GetSettings().CustomResolutionHeight;
        set => Update(settings => settings.CustomResolutionHeight = Math.Max(0, value));
    }

    /// <summary>
    /// Gets or sets whether satellite tracking is enabled.
    /// This is the master switch for all satellite tracking features.
    /// </summary>
    public static bool SatelliteTrackingEnabled
    {
        get => GetSettings().SatelliteTrackingEnabled;
        set => Update(settings => settings.SatelliteTrackingEnabled = value);
    }

    /// <summary>
    /// Gets or sets the folder where generated wallpaper images are saved.
    /// </summary>
    public static string WallpaperOutputDirectory
    {
        get => GetSettings().WallpaperOutputDirectory;
        set => Update(settings =>
            settings.WallpaperOutputDirectory = string.IsNullOrWhiteSpace(value)
                ? GetDefaultWallpaperOutputDirectory()
                : value.Trim());
    }

    /// <summary>
    /// Gets or sets whether the settings tray app should provide automatic updates
    /// when Task Scheduler integration is unavailable.
    /// </summary>
    public static bool TrayAutoUpdateFallbackEnabled
    {
        get => GetSettings().TrayAutoUpdateFallbackEnabled;
        set => Update(settings => settings.TrayAutoUpdateFallbackEnabled = value);
    }

    /// <summary>
    /// Gets the path to the active settings.json file.
    /// </summary>
    public static string SettingsFilePath => AppStoragePaths.SettingsFilePath;

    /// <summary>
    /// Resets all settings to their default values.
    /// </summary>
    public static void ResetToDefaults()
    {
        lock (SyncRoot)
        {
            var defaults = CreateDefaultSettings();
            SaveSettings(defaults);
            _cachedSettings = defaults;
        }
    }

    /// <summary>
    /// Forces the current process to reload settings from disk.
    /// </summary>
    public static void Reload()
    {
        lock (SyncRoot)
        {
            _cachedSettings = null;
        }
    }

    private static SettingsData GetSettings()
    {
        lock (SyncRoot)
        {
            _cachedSettings ??= LoadSettings();
            return Clone(_cachedSettings);
        }
    }

    private static void Update(Action<SettingsData> updateAction)
    {
        lock (SyncRoot)
        {
            var settings = _cachedSettings ?? LoadSettings();
            var updated = Clone(settings);
            updateAction(updated);
            SaveSettings(updated);
            _cachedSettings = updated;
        }
    }

    private static SettingsData LoadSettings()
    {
        var settingsPath = SettingsFilePath;
        var defaults = CreateDefaultSettings();

        try
        {
            if (File.Exists(settingsPath))
            {
                var json = File.ReadAllText(settingsPath);
                var settings = JsonSerializer.Deserialize<SettingsData>(json, JsonOptions);
                if (settings != null)
                    return NormalizeSettings(settings);
            }
        }
        catch
        {
            // Fall through to migration/defaults.
        }

        var migration = TryMigrateRegistrySettings(defaults);
        if (SaveSettings(migration.Settings))
            DeleteMigratedRegistryValues(migration.MigratedKeys);
        return migration.Settings;
    }

    private static MigrationResult TryMigrateRegistrySettings(SettingsData defaults)
    {
        var migrated = Clone(defaults);
        var migratedKeys = new List<(RegistryHive Hive, string Name)>();

        foreach (var hive in GetMigrationHiveOrder())
        {
            foreach (var registryView in GetMigrationViews())
            {
                try
                {
                    using var baseKey = RegistryKey.OpenBaseKey(hive, registryView);
                    using var key = baseKey.OpenSubKey(RegistryKeyPath);
                    if (key == null)
                        continue;

                    ApplyRegistryValue(key, "ShowISS", value => migrated.ShowISS = value, migratedKeys, hive);
                    ApplyRegistryValue(key, "ShowTimeZones", value => migrated.ShowTimeZones = value, migratedKeys, hive);
                    ApplyRegistryValue(key, "ShowPoliticalMap", value => migrated.ShowPoliticalMap = value, migratedKeys, hive);
                    ApplyRegistryValue<UpdateInterval>(key, "UpdateInterval", value => migrated.UpdateInterval = value, migratedKeys, hive);
                    ApplyRegistryValue(key, "IsActive", value => migrated.IsActive = value, migratedKeys, hive);
                    ApplyRegistryValue<ResolutionMode>(key, "ResolutionMode", value => migrated.ResolutionMode = value, migratedKeys, hive);
                    ApplyRegistryValue(key, "CustomResolutionWidth", value => migrated.CustomResolutionWidth = value, migratedKeys, hive);
                    ApplyRegistryValue(key, "CustomResolutionHeight", value => migrated.CustomResolutionHeight = value, migratedKeys, hive);
                    ApplyRegistryValue(key, "SatelliteTrackingEnabled", value => migrated.SatelliteTrackingEnabled = value, migratedKeys, hive);
                    ApplyRegistryValue(key, "WallpaperOutputDirectory", value => migrated.WallpaperOutputDirectory = value, migratedKeys, hive);
                    ApplyRegistryValue(key, "TrayAutoUpdateFallbackEnabled", value => migrated.TrayAutoUpdateFallbackEnabled = value, migratedKeys, hive);
                }
                catch
                {
                    // Ignore malformed or inaccessible legacy settings.
                }
            }
        }

        return new MigrationResult(NormalizeSettings(migrated), migratedKeys);
    }

    private static IEnumerable<RegistryHive> GetMigrationHiveOrder()
    {
        if (AppStoragePaths.InstallScope == InstallationScope.AllUsers)
            return [RegistryHive.LocalMachine, RegistryHive.CurrentUser];

        return [RegistryHive.CurrentUser, RegistryHive.LocalMachine];
    }

    private static IEnumerable<RegistryView> GetMigrationViews()
    {
        yield return RegistryView.Default;

        if (Environment.Is64BitOperatingSystem)
        {
            var alternateView = Environment.Is64BitProcess ? RegistryView.Registry32 : RegistryView.Registry64;
            yield return alternateView;
        }
    }

    private static void ApplyRegistryValue(
        RegistryKey key,
        string valueName,
        Action<bool> applyValue,
        ICollection<(RegistryHive Hive, string Name)> migratedKeys,
        RegistryHive hive)
    {
        var rawValue = key.GetValue(valueName)?.ToString();
        if (bool.TryParse(rawValue, out var parsedValue))
        {
            applyValue(parsedValue);
            migratedKeys.Add((hive, valueName));
        }
    }

    private static void ApplyRegistryValue(
        RegistryKey key,
        string valueName,
        Action<int> applyValue,
        ICollection<(RegistryHive Hive, string Name)> migratedKeys,
        RegistryHive hive)
    {
        var rawValue = key.GetValue(valueName)?.ToString();
        if (int.TryParse(rawValue, out var parsedValue))
        {
            applyValue(parsedValue);
            migratedKeys.Add((hive, valueName));
        }
    }

    private static void ApplyRegistryValue(
        RegistryKey key,
        string valueName,
        Action<string> applyValue,
        ICollection<(RegistryHive Hive, string Name)> migratedKeys,
        RegistryHive hive)
    {
        var rawValue = key.GetValue(valueName)?.ToString();
        if (!string.IsNullOrWhiteSpace(rawValue))
        {
            applyValue(rawValue);
            migratedKeys.Add((hive, valueName));
        }
    }

    private static void ApplyRegistryValue<TEnum>(
        RegistryKey key,
        string valueName,
        Action<TEnum> applyValue,
        ICollection<(RegistryHive Hive, string Name)> migratedKeys,
        RegistryHive hive) where TEnum : struct, Enum
    {
        var rawValue = key.GetValue(valueName)?.ToString();
        if (Enum.TryParse<TEnum>(rawValue, out var parsedValue))
        {
            applyValue(parsedValue);
            migratedKeys.Add((hive, valueName));
        }
    }

    private static void DeleteMigratedRegistryValues(IEnumerable<(RegistryHive Hive, string Name)> migratedKeys)
    {
        foreach (var group in migratedKeys
            .Distinct()
            .GroupBy(item => item.Hive))
        {
            foreach (var registryView in GetMigrationViews())
            {
                try
                {
                    using var baseKey = RegistryKey.OpenBaseKey(group.Key, registryView);
                    using var key = baseKey.OpenSubKey(RegistryKeyPath, writable: true);
                    if (key == null)
                        continue;

                    foreach (var entry in group)
                    {
                        key.DeleteValue(entry.Name, throwOnMissingValue: false);
                    }
                }
                catch
                {
                    // Keep legacy values if cleanup fails.
                }
            }
        }
    }

    private static bool SaveSettings(SettingsData settings)
    {
        var normalized = NormalizeSettings(settings);
        try
        {
            var settingsDirectory = Path.GetDirectoryName(SettingsFilePath);
            if (!string.IsNullOrWhiteSpace(settingsDirectory))
                Directory.CreateDirectory(settingsDirectory);

            var json = JsonSerializer.Serialize(normalized, JsonOptions);
            File.WriteAllText(SettingsFilePath, json);
            return true;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
        catch (IOException)
        {
            return false;
        }
    }

    private static SettingsData NormalizeSettings(SettingsData settings)
    {
        settings.CustomResolutionWidth = Math.Max(0, settings.CustomResolutionWidth);
        settings.CustomResolutionHeight = Math.Max(0, settings.CustomResolutionHeight);
        settings.WallpaperOutputDirectory = string.IsNullOrWhiteSpace(settings.WallpaperOutputDirectory)
            ? GetDefaultWallpaperOutputDirectory()
            : settings.WallpaperOutputDirectory.Trim();
        return settings;
    }

    private static SettingsData CreateDefaultSettings()
    {
        return new SettingsData
        {
            WallpaperOutputDirectory = GetDefaultWallpaperOutputDirectory()
        };
    }

    private static string GetDefaultWallpaperOutputDirectory()
    {
        return AppStoragePaths.GetDefaultWallpaperOutputDirectory(AppStoragePaths.InstallScope);
    }

    private static SettingsData Clone(SettingsData settings)
    {
        return new SettingsData
        {
            ShowISS = settings.ShowISS,
            ShowTimeZones = settings.ShowTimeZones,
            ShowPoliticalMap = settings.ShowPoliticalMap,
            UpdateInterval = settings.UpdateInterval,
            IsActive = settings.IsActive,
            ResolutionMode = settings.ResolutionMode,
            CustomResolutionWidth = settings.CustomResolutionWidth,
            CustomResolutionHeight = settings.CustomResolutionHeight,
            SatelliteTrackingEnabled = settings.SatelliteTrackingEnabled,
            WallpaperOutputDirectory = settings.WallpaperOutputDirectory,
            TrayAutoUpdateFallbackEnabled = settings.TrayAutoUpdateFallbackEnabled
        };
    }

    private sealed class SettingsData
    {
        public bool ShowISS { get; set; } = true;
        public bool ShowTimeZones { get; set; } = true;
        public bool ShowPoliticalMap { get; set; } = true;
        public UpdateInterval UpdateInterval { get; set; } = UpdateInterval.Hourly;
        public bool IsActive { get; set; } = true;
        public ResolutionMode ResolutionMode { get; set; } = ResolutionMode.None;
        public int CustomResolutionWidth { get; set; }
        public int CustomResolutionHeight { get; set; }
        public bool SatelliteTrackingEnabled { get; set; } = true;
        public string WallpaperOutputDirectory { get; set; } = string.Empty;
        public bool TrayAutoUpdateFallbackEnabled { get; set; }
    }

    private sealed record MigrationResult(SettingsData Settings, IReadOnlyCollection<(RegistryHive Hive, string Name)> MigratedKeys);
}
