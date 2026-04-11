using Microsoft.Win32;

namespace WorldMapWallpaper.Shared;

/// <summary>
/// Manages application settings stored in the Windows Registry.
/// All settings are stored under HKEY_CURRENT_USER\SOFTWARE\WorldMapWallpaper.
/// </summary>
public static class Settings
{
    private const string RegistryKeyPath = @"SOFTWARE\WorldMapWallpaper";

    /// <summary>
    /// Gets or sets whether to show the International Space Station on the wallpaper.
    /// </summary>
    public static bool ShowISS
    {
        get => GetBoolSetting("ShowISS", true);
        set => SetBoolSetting("ShowISS", value);
    }

    /// <summary>
    /// Gets or sets whether to show time zone clocks on the wallpaper.
    /// </summary>
    public static bool ShowTimeZones
    {
        get => GetBoolSetting("ShowTimeZones", true);
        set => SetBoolSetting("ShowTimeZones", value);
    }

    /// <summary>
    /// Gets or sets whether to show the political map overlay on the wallpaper.
    /// </summary>
    public static bool ShowPoliticalMap
    {
        get => GetBoolSetting("ShowPoliticalMap", true);
        set => SetBoolSetting("ShowPoliticalMap", value);
    }

    /// <summary>
    /// Gets or sets the wallpaper update interval.
    /// </summary>
    public static UpdateInterval UpdateInterval
    {
        get => GetEnumSetting("UpdateInterval", UpdateInterval.Hourly);
        set => SetEnumSetting("UpdateInterval", value);
    }

    /// <summary>
    /// Gets or sets whether the wallpaper is currently active (being used by Windows).
    /// </summary>
    public static bool IsActive
    {
        get => GetBoolSetting("IsActive", true);
        set => SetBoolSetting("IsActive", value);
    }

    /// <summary>
    /// Gets or sets the resolution scaling mode for the wallpaper.
    /// </summary>
    public static ResolutionMode ResolutionMode
    {
        get => GetEnumSetting("ResolutionMode", ResolutionMode.None);
        set => SetEnumSetting("ResolutionMode", value);
    }

    /// <summary>
    /// Gets or sets the custom resolution width (0 = use auto-detect from screen).
    /// </summary>
    public static int CustomResolutionWidth
    {
        get => GetIntSetting("CustomResolutionWidth", 0);
        set => SetIntSetting("CustomResolutionWidth", value);
    }

    /// <summary>
    /// Gets or sets the custom resolution height (0 = use auto-detect from screen).
    /// </summary>
    public static int CustomResolutionHeight
    {
        get => GetIntSetting("CustomResolutionHeight", 0);
        set => SetIntSetting("CustomResolutionHeight", value);
    }

    /// <summary>
    /// Gets or sets whether satellite tracking is enabled.
    /// This is the master switch for all satellite tracking features.
    /// </summary>
    public static bool SatelliteTrackingEnabled
    {
        get => GetBoolSetting("SatelliteTrackingEnabled", true);
        set => SetBoolSetting("SatelliteTrackingEnabled", value);
    }

    /// <summary>
    /// Gets or sets the folder where generated wallpaper images are saved.
    /// </summary>
    public static string WallpaperOutputDirectory
    {
        get => GetStringSetting("WallpaperOutputDirectory",
            Environment.GetFolderPath(Environment.SpecialFolder.MyPictures));
        set => SetStringSetting("WallpaperOutputDirectory", value);
    }

    /// <summary>
    /// Gets a boolean setting from the registry.
    /// </summary>
    /// <param name="name">The setting name.</param>
    /// <param name="defaultValue">The default value if the setting doesn't exist.</param>
    /// <returns>The setting value.</returns>
    private static bool GetBoolSetting(string name, bool defaultValue)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath);
            var value = key?.GetValue(name)?.ToString();
            return bool.TryParse(value, out var result) ? result : defaultValue;
        }
        catch
        {
            return defaultValue;
        }
    }

    /// <summary>
    /// Sets a boolean setting in the registry.
    /// </summary>
    /// <param name="name">The setting name.</param>
    /// <param name="value">The setting value.</param>
    private static void SetBoolSetting(string name, bool value)
    {
        try
        {
            using var key = Registry.CurrentUser.CreateSubKey(RegistryKeyPath);
            key?.SetValue(name, value.ToString());
        }
        catch
        {
            // Silently fail - settings will use defaults
        }
    }

    /// <summary>
    /// Gets an enum setting from the registry.
    /// </summary>
    /// <typeparam name="T">The enum type.</typeparam>
    /// <param name="name">The setting name.</param>
    /// <param name="defaultValue">The default value if the setting doesn't exist.</param>
    /// <returns>The setting value.</returns>
    private static T GetEnumSetting<T>(string name, T defaultValue) where T : struct, Enum
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath);
            var value = key?.GetValue(name)?.ToString();
            return Enum.TryParse<T>(value, out var result) ? result : defaultValue;
        }
        catch
        {
            return defaultValue;
        }
    }

    /// <summary>
    /// Sets an enum setting in the registry.
    /// </summary>
    /// <typeparam name="T">The enum type.</typeparam>
    /// <param name="name">The setting name.</param>
    /// <param name="value">The setting value.</param>
    private static void SetEnumSetting<T>(string name, T value) where T : struct, Enum
    {
        try
        {
            using var key = Registry.CurrentUser.CreateSubKey(RegistryKeyPath);
            key?.SetValue(name, value.ToString());
        }
        catch
        {
            // Silently fail - settings will use defaults
        }
    }

    /// <summary>
    /// Gets an integer setting from the registry.
    /// </summary>
    /// <param name="name">The setting name.</param>
    /// <param name="defaultValue">The default value if the setting doesn't exist.</param>
    /// <returns>The setting value.</returns>
    private static int GetIntSetting(string name, int defaultValue)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath);
            var value = key?.GetValue(name)?.ToString();
            return int.TryParse(value, out var result) ? result : defaultValue;
        }
        catch
        {
            return defaultValue;
        }
    }

    /// <summary>
    /// Sets an integer setting in the registry.
    /// </summary>
    /// <param name="name">The setting name.</param>
    /// <param name="value">The setting value.</param>
    private static void SetIntSetting(string name, int value)
    {
        try
        {
            using var key = Registry.CurrentUser.CreateSubKey(RegistryKeyPath);
            key?.SetValue(name, value.ToString());
        }
        catch
        {
            // Silently fail - settings will use defaults
        }
    }

    /// <summary>
    /// Gets a string setting from the registry.
    /// </summary>
    private static string GetStringSetting(string name, string defaultValue)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath);
            var value = key?.GetValue(name)?.ToString();
            return string.IsNullOrWhiteSpace(value) ? defaultValue : value;
        }
        catch
        {
            return defaultValue;
        }
    }

    /// <summary>
    /// Sets a string setting in the registry.
    /// </summary>
    private static void SetStringSetting(string name, string value)
    {
        try
        {
            using var key = Registry.CurrentUser.CreateSubKey(RegistryKeyPath);
            key?.SetValue(name, value);
        }
        catch
        {
            // Silently fail - settings will use defaults
        }
    }

    /// <summary>
    /// Resets all settings to their default values.
    /// </summary>
    public static void ResetToDefaults()
    {
        ShowISS = true;
        ShowTimeZones = true;
        ShowPoliticalMap = true;
        UpdateInterval = UpdateInterval.Hourly;
        IsActive = true;
        ResolutionMode = ResolutionMode.None;
        CustomResolutionWidth = 0;
        CustomResolutionHeight = 0;
        SatelliteTrackingEnabled = true;
        WallpaperOutputDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
    }
}
