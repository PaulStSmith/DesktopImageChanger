namespace WorldMapWallpaper.Shared.Models;

/// <summary>
/// Represents a preset satellite that users can easily add.
/// </summary>
public class SatellitePreset
{
    /// <summary>
    /// Gets the display name of the satellite.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the NORAD catalog number.
    /// </summary>
    public int NoradId { get; }

    /// <summary>
    /// Gets the satellite category.
    /// </summary>
    public SatelliteCategory Category { get; }

    /// <summary>
    /// Gets the orbit type description (LEO, MEO, GEO).
    /// </summary>
    public string OrbitType { get; }

    /// <summary>
    /// Gets the suggested orbit trail color.
    /// </summary>
    public string SuggestedColor { get; }

    /// <summary>
    /// Gets whether this satellite is at a Lagrange point.
    /// </summary>
    public bool IsLagrangePoint { get; }

    /// <summary>
    /// Gets the Lagrange point location (L1, L2, etc.) if applicable.
    /// </summary>
    public string? LagrangePointLocation { get; }

    private SatellitePreset(
        string name,
        int noradId,
        SatelliteCategory category,
        string orbitType,
        string suggestedColor,
        bool isLagrangePoint = false,
        string? lagrangePointLocation = null)
    {
        Name = name;
        NoradId = noradId;
        Category = category;
        OrbitType = orbitType;
        SuggestedColor = suggestedColor;
        IsLagrangePoint = isLagrangePoint;
        LagrangePointLocation = lagrangePointLocation;
    }

    /// <summary>
    /// Converts this preset to a SatelliteConfig for use in tracking.
    /// </summary>
    /// <param name="priority">The priority to assign.</param>
    /// <returns>A new SatelliteConfig based on this preset.</returns>
    public SatelliteConfig ToConfig(int priority)
    {
        return new SatelliteConfig
        {
            Name = Name,
            NoradId = NoradId,
            Enabled = true,
            IconPath = null,
            Color = SuggestedColor,
            ShowOrbit = true,
            Priority = priority,
            Category = Category,
            IsLagrangePoint = IsLagrangePoint
        };
    }

    /// <summary>
    /// Gets all available preset satellites.
    /// </summary>
    public static IReadOnlyList<SatellitePreset> All => _presets;

    /// <summary>
    /// Gets preset satellites filtered by category.
    /// </summary>
    /// <param name="category">The category to filter by.</param>
    /// <returns>Presets matching the category.</returns>
    public static IEnumerable<SatellitePreset> ByCategory(SatelliteCategory category)
    {
        return _presets.Where(p => p.Category == category);
    }

    /// <summary>
    /// Gets Earth-orbiting preset satellites (excludes Lagrange point satellites).
    /// </summary>
    public static IEnumerable<SatellitePreset> EarthOrbiting =>
        _presets.Where(p => !p.IsLagrangePoint);

    /// <summary>
    /// Gets Lagrange point satellites (for warning purposes).
    /// </summary>
    public static IEnumerable<SatellitePreset> LagrangePointSatellites =>
        _presets.Where(p => p.IsLagrangePoint);

    /// <summary>
    /// Finds a preset by NORAD ID.
    /// </summary>
    /// <param name="noradId">The NORAD catalog number.</param>
    /// <returns>The preset if found, null otherwise.</returns>
    public static SatellitePreset? FindByNoradId(int noradId)
    {
        return _presets.FirstOrDefault(p => p.NoradId == noradId);
    }

    // Default color palette for orbit trails
    private static readonly string[] _defaultColors =
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

    /// <summary>
    /// Gets the default color palette for orbit trails.
    /// </summary>
    public static IReadOnlyList<string> DefaultColorPalette => _defaultColors;

    /// <summary>
    /// Gets a suggested color for a new satellite based on index.
    /// </summary>
    /// <param name="index">The satellite index.</param>
    /// <returns>A hex color string.</returns>
    public static string GetSuggestedColor(int index)
    {
        return _defaultColors[index % _defaultColors.Length];
    }

    // Preset satellite definitions
    private static readonly List<SatellitePreset> _presets = new()
    {
        // Space Stations
        new SatellitePreset(
            "International Space Station",
            25544,
            SatelliteCategory.SpaceStation,
            "LEO",
            "#FF6B35"),

        new SatellitePreset(
            "Tiangong Space Station",
            48274,
            SatelliteCategory.SpaceStation,
            "LEO",
            "#FFE66D"),

        // Telescopes (Earth-orbiting)
        new SatellitePreset(
            "Hubble Space Telescope",
            20580,
            SatelliteCategory.Telescope,
            "LEO",
            "#4ECDC4"),

        // Weather Satellites
        new SatellitePreset(
            "GOES-18",
            51850,
            SatelliteCategory.Weather,
            "GEO",
            "#95E1D3"),

        new SatellitePreset(
            "NOAA-20",
            43013,
            SatelliteCategory.Weather,
            "LEO",
            "#F38181"),

        new SatellitePreset(
            "NOAA-19",
            33591,
            SatelliteCategory.Weather,
            "LEO",
            "#AA96DA"),

        new SatellitePreset(
            "NOAA-18",
            28654,
            SatelliteCategory.Weather,
            "LEO",
            "#81B214"),

        // Earth Observation
        new SatellitePreset(
            "Landsat 9",
            49260,
            SatelliteCategory.EarthObservation,
            "LEO",
            "#2C786C"),

        new SatellitePreset(
            "Landsat 8",
            39084,
            SatelliteCategory.EarthObservation,
            "LEO",
            "#F9ED69"),

        new SatellitePreset(
            "Sentinel-2A",
            40697,
            SatelliteCategory.EarthObservation,
            "LEO",
            "#B83B5E"),

        // Science Satellites
        new SatellitePreset(
            "Terra",
            25994,
            SatelliteCategory.Science,
            "LEO",
            "#4ECDC4"),

        new SatellitePreset(
            "Aqua",
            27424,
            SatelliteCategory.Science,
            "LEO",
            "#95E1D3"),

        // Navigation
        new SatellitePreset(
            "GPS IIF-12",
            41019,
            SatelliteCategory.Navigation,
            "MEO",
            "#FF6B35"),

        // Lagrange Point Satellites (with warnings)
        new SatellitePreset(
            "James Webb Space Telescope",
            50463,
            SatelliteCategory.Telescope,
            "L2",
            "#AA96DA",
            isLagrangePoint: true,
            lagrangePointLocation: "L2"),

        new SatellitePreset(
            "DSCOVR",
            43435,
            SatelliteCategory.Science,
            "L1",
            "#81B214",
            isLagrangePoint: true,
            lagrangePointLocation: "L1"),

        new SatellitePreset(
            "Gaia",
            39479,
            SatelliteCategory.Telescope,
            "L2",
            "#F38181",
            isLagrangePoint: true,
            lagrangePointLocation: "L2"),

        new SatellitePreset(
            "SOHO",
            28928,
            SatelliteCategory.Science,
            "L1",
            "#FFE66D",
            isLagrangePoint: true,
            lagrangePointLocation: "L1"),

        new SatellitePreset(
            "Euclid",
            52195,
            SatelliteCategory.Telescope,
            "L2",
            "#2C786C",
            isLagrangePoint: true,
            lagrangePointLocation: "L2"),
    };
}
