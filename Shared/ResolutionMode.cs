namespace WorldMapWallpaper.Shared;

/// <summary>
/// Defines the available resolution modes for wallpaper generation.
/// Controls how the generated wallpaper is scaled to fit different screen resolutions.
/// </summary>
public enum ResolutionMode
{
    /// <summary>
    /// Keep the default 1920x1080 resolution without any scaling.
    /// </summary>
    None,

    /// <summary>
    /// Scale to fit within screen bounds while preserving aspect ratio.
    /// May result in letterboxing (black bars top/bottom) or pillarboxing (black bars left/right).
    /// </summary>
    Fit,

    /// <summary>
    /// Scale to fill the entire screen.
    /// May distort the aspect ratio if the screen is not 16:9.
    /// </summary>
    Stretch
}

/// <summary>
/// Extension methods for ResolutionMode enum.
/// </summary>
public static class ResolutionModeExtensions
{
    /// <summary>
    /// Gets a user-friendly display name for the resolution mode.
    /// </summary>
    /// <param name="mode">The resolution mode.</param>
    /// <returns>A formatted display string.</returns>
    public static string ToDisplayString(this ResolutionMode mode) => mode switch
    {
        ResolutionMode.None => "Original (1920x1080)",
        ResolutionMode.Fit => "Fit to Screen",
        ResolutionMode.Stretch => "Stretch to Screen",
        _ => "Original (1920x1080)"
    };
}
