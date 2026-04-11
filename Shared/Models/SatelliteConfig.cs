using System.Text.Json.Serialization;

namespace WorldMapWallpaper.Shared.Models;

/// <summary>
/// Represents the configuration for a tracked satellite.
/// </summary>
public class SatelliteConfig
{
    /// <summary>
    /// Gets or sets the display name of the satellite.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the NORAD catalog number for the satellite.
    /// </summary>
    [JsonPropertyName("noradId")]
    public int NoradId { get; set; }

    /// <summary>
    /// Gets or sets whether this satellite is enabled for display.
    /// </summary>
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the path to a custom icon file.
    /// Null or empty means use the default icon for the category.
    /// </summary>
    [JsonPropertyName("iconPath")]
    public string? IconPath { get; set; }

    /// <summary>
    /// Gets or sets the orbit trail color in hex format (e.g., "#FF6B35").
    /// Null means auto-assign from the default palette.
    /// </summary>
    [JsonPropertyName("color")]
    public string? Color { get; set; }

    /// <summary>
    /// Gets or sets whether to show the orbital path for this satellite.
    /// </summary>
    [JsonPropertyName("showOrbit")]
    public bool ShowOrbit { get; set; } = true;

    /// <summary>
    /// Gets or sets the display priority (lower = higher priority).
    /// Used when more satellites are enabled than the visibility limit.
    /// </summary>
    [JsonPropertyName("priority")]
    public int Priority { get; set; } = 100;

    /// <summary>
    /// Gets or sets the satellite category for icon selection.
    /// </summary>
    [JsonPropertyName("category")]
    public SatelliteCategory Category { get; set; } = SatelliteCategory.Default;

    /// <summary>
    /// Gets or sets whether this satellite is at a Lagrange point.
    /// If true, position display will not be accurate.
    /// </summary>
    [JsonPropertyName("isLagrangePoint")]
    public bool IsLagrangePoint { get; set; }
}

/// <summary>
/// Categories of satellites for icon assignment.
/// </summary>
public enum SatelliteCategory
{
    /// <summary>Default generic satellite icon.</summary>
    Default,

    /// <summary>Space stations (ISS, Tiangong).</summary>
    SpaceStation,

    /// <summary>Telescopes (Hubble).</summary>
    Telescope,

    /// <summary>Weather satellites (GOES, NOAA).</summary>
    Weather,

    /// <summary>Earth observation satellites (Landsat, Sentinel).</summary>
    EarthObservation,

    /// <summary>Science satellites (Terra, Aqua).</summary>
    Science,

    /// <summary>Navigation satellites (GPS).</summary>
    Navigation
}

/// <summary>
/// Root configuration object for satellite tracking.
/// </summary>
public class SatelliteConfigRoot
{
    /// <summary>
    /// Configuration file version for migration purposes.
    /// </summary>
    [JsonPropertyName("version")]
    public int Version { get; set; } = 1;

    /// <summary>
    /// Maximum number of satellites to display at once (1-15).
    /// </summary>
    [JsonPropertyName("maxVisibleSatellites")]
    public int MaxVisibleSatellites { get; set; } = 5;

    /// <summary>
    /// Number of orbit trail points to render (before + after current position).
    /// </summary>
    [JsonPropertyName("defaultOrbitPoints")]
    public int DefaultOrbitPoints { get; set; } = 50;

    /// <summary>
    /// Visibility mode for satellite display.
    /// </summary>
    [JsonPropertyName("visibilityMode")]
    public SatelliteVisibilityMode VisibilityMode { get; set; } = SatelliteVisibilityMode.All;

    /// <summary>
    /// List of configured satellites.
    /// </summary>
    [JsonPropertyName("satellites")]
    public List<SatelliteConfig> Satellites { get; set; } = new();
}

/// <summary>
/// Visibility modes for satellite display.
/// </summary>
public enum SatelliteVisibilityMode
{
    /// <summary>Show all enabled satellites up to the limit.</summary>
    All,

    /// <summary>Only show satellites currently in sunlight.</summary>
    DaylightOnly
}

/// <summary>
/// Extension methods for SatelliteCategory enum.
/// </summary>
public static class SatelliteCategoryExtensions
{
    /// <summary>
    /// Gets a user-friendly display string for the satellite category.
    /// </summary>
    /// <param name="category">The satellite category.</param>
    /// <returns>A display string.</returns>
    public static string ToDisplayString(this SatelliteCategory category)
    {
        return category switch
        {
            SatelliteCategory.Default => "Other",
            SatelliteCategory.SpaceStation => "Space Station",
            SatelliteCategory.Telescope => "Telescope",
            SatelliteCategory.Weather => "Weather",
            SatelliteCategory.EarthObservation => "Earth Observation",
            SatelliteCategory.Science => "Science",
            SatelliteCategory.Navigation => "Navigation",
            _ => category.ToString()
        };
    }
}
