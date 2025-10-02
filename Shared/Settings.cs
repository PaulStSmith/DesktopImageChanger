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
    /// Resets all settings to their default values.
    /// </summary>
    public static void ResetToDefaults()
    {
        ShowISS = true;
        ShowTimeZones = true;
        ShowPoliticalMap = true;
        UpdateInterval = UpdateInterval.Hourly;
        IsActive = true;
    }
}