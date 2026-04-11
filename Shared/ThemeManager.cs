using Microsoft.Win32;
using System.Drawing;

namespace WorldMapWallpaper.Shared;

/// <summary>
/// Manages Windows system theme detection and color scheme application.
/// </summary>
public static class ThemeManager
{
    /// <summary>
    /// Detects if Windows is currently using a dark theme.
    /// </summary>
    /// <returns>True if dark theme is active, false for light theme.</returns>
    public static bool IsDarkTheme()
    {
        try
        {
            // Check Windows 10/11 personalization setting
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            var value = key?.GetValue("AppsUseLightTheme");
            
            // AppsUseLightTheme = 0 means dark theme, 1 means light theme
            return value is int intValue && intValue == 0;
        }
        catch
        {
            // Default to light theme if we can't detect
            return false;
        }
    }

    /// <summary>
    /// Gets the appropriate color scheme for the current Windows theme.
    /// </summary>
    /// <returns>A ColorScheme with appropriate colors for the current theme.</returns>
    public static ColorScheme GetCurrentColorScheme()
    {
        return IsDarkTheme() ? GetDarkColorScheme() : GetLightColorScheme();
    }

    /// <summary>
    /// Gets a dark color scheme.
    /// </summary>
    /// <returns>Dark theme ColorScheme.</returns>
    public static ColorScheme GetDarkColorScheme()
    {
        return new ColorScheme
        {
            BackgroundColor = Color.FromArgb(32, 32, 32),        // Dark gray background
            SurfaceColor = Color.FromArgb(45, 45, 45),           // Slightly lighter surface
            PrimaryTextColor = Color.FromArgb(255, 255, 255),    // White text
            SecondaryTextColor = Color.FromArgb(180, 180, 180),  // Light gray text
            AccentColor = Color.FromArgb(0, 120, 215),           // Windows blue
            BorderColor = Color.FromArgb(60, 60, 60),            // Dark border
            ButtonBackColor = Color.FromArgb(55, 55, 55),        // Dark button
            ButtonHoverColor = Color.FromArgb(70, 70, 70),       // Button hover
            GroupBoxBackColor = Color.FromArgb(40, 40, 40),      // GroupBox background
            SuccessColor = Color.FromArgb(16, 124, 16),          // Dark green
            ErrorColor = Color.FromArgb(196, 43, 28),            // Dark red
            WarningColor = Color.FromArgb(255, 185, 0)           // Warning orange
        };
    }

    /// <summary>
    /// Gets a light color scheme.
    /// </summary>
    /// <returns>Light theme ColorScheme.</returns>
    public static ColorScheme GetLightColorScheme()
    {
        return new ColorScheme
        {
            BackgroundColor = Color.FromArgb(255, 255, 255),     // White background
            SurfaceColor = Color.FromArgb(250, 250, 250),        // Light gray surface
            PrimaryTextColor = Color.FromArgb(0, 0, 0),          // Black text
            SecondaryTextColor = Color.FromArgb(96, 96, 96),     // Gray text
            AccentColor = Color.FromArgb(0, 120, 215),           // Windows blue
            BorderColor = Color.FromArgb(200, 200, 200),         // Light border
            ButtonBackColor = Color.FromArgb(245, 245, 245),     // Light button
            ButtonHoverColor = Color.FromArgb(230, 230, 230),    // Button hover
            GroupBoxBackColor = Color.FromArgb(248, 248, 248),   // GroupBox background
            SuccessColor = Color.FromArgb(16, 124, 16),          // Green
            ErrorColor = Color.FromArgb(196, 43, 28),            // Red
            WarningColor = Color.FromArgb(210, 140, 0)           // Warning orange
        };
    }
}

/// <summary>
/// Represents a color scheme for theming UI controls.
/// </summary>
public class ColorScheme
{
    public Color BackgroundColor { get; set; }
    public Color SurfaceColor { get; set; }
    public Color PrimaryTextColor { get; set; }
    public Color SecondaryTextColor { get; set; }
    public Color AccentColor { get; set; }
    public Color BorderColor { get; set; }
    public Color ButtonBackColor { get; set; }
    public Color ButtonHoverColor { get; set; }
    public Color GroupBoxBackColor { get; set; }
    public Color SuccessColor { get; set; }
    public Color ErrorColor { get; set; }
    public Color WarningColor { get; set; }
}