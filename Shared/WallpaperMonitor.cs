using Microsoft.Win32;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace WorldMapWallpaper.Shared;

/// <summary>
/// Monitors Windows wallpaper changes to detect when the user switches 
/// away from World Map Wallpaper to another wallpaper provider.
/// </summary>
public class WallpaperMonitor : IDisposable
{
    private const string WallpaperRegistryPath = @"Control Panel\Desktop";
    private const string WallpaperValueName = "Wallpaper";
    private const int MonitorIntervalMs = 2000; // Check every 2 seconds

    private Timer? _monitorTimer;
    private string? _lastWallpaperPath;
    private bool _disposed;

    /// <summary>
    /// Raised when the wallpaper changes. The parameter indicates whether 
    /// our wallpaper is currently active.
    /// </summary>
    public event Action<bool>? WallpaperChanged;

    /// <summary>
    /// Starts monitoring wallpaper changes.
    /// </summary>
    public void Start()
    {
        ObjectDisposedException.ThrowIf(_disposed, nameof(WallpaperMonitor));

        // Get initial wallpaper path
        _lastWallpaperPath = GetCurrentWallpaperPath();

        // Start monitoring timer
        _monitorTimer = new Timer(CheckWallpaperChange, null, 
            TimeSpan.FromMilliseconds(MonitorIntervalMs), 
            TimeSpan.FromMilliseconds(MonitorIntervalMs));
    }

    /// <summary>
    /// Stops monitoring wallpaper changes.
    /// </summary>
    public void Stop()
    {
        _monitorTimer?.Dispose();
        _monitorTimer = null;
    }

    /// <summary>
    /// Checks if the wallpaper has changed and raises the event if necessary.
    /// </summary>
    /// <param name="state">Timer state (unused).</param>
    private void CheckWallpaperChange(object? state)
    {
        try
        {
            var currentWallpaperPath = GetCurrentWallpaperPath();
            
            if (currentWallpaperPath != _lastWallpaperPath)
            {
                _lastWallpaperPath = currentWallpaperPath;
                var isOurWallpaper = IsOurWallpaperActive(currentWallpaperPath);
                WallpaperChanged?.Invoke(isOurWallpaper);
            }
        }
        catch
        {
            // Silently handle any registry or file access errors
        }
    }

    /// <summary>
    /// Gets the current wallpaper path from the Windows API.
    /// </summary>
    /// <returns>The current wallpaper file path, or null if it can't be determined.</returns>
    public static string? GetCurrentWallpaperPath()
    {
        try
        {
            var sbWPFN = new StringBuilder(256);
            NativeMethods.SystemParametersInfo(SPI.SPI_GETDESKWALLPAPER, 256, sbWPFN, SPIF.None);
            return sbWPFN.ToString();
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Determines if the specified wallpaper path indicates our wallpaper is active.
    /// </summary>
    /// <param name="wallpaperPath">The wallpaper file path.</param>
    /// <returns>True if our wallpaper is active, false otherwise.</returns>
    private static bool IsOurWallpaperActive(string? wallpaperPath)
    {
        if (string.IsNullOrEmpty(wallpaperPath))
            return false;

        try
        {
            var fileName = Path.GetFileName(wallpaperPath);
            var normalizedWallpaperDirectory = NormalizeDirectoryPath(Path.GetDirectoryName(wallpaperPath));
            var normalizedOutputDirectory = NormalizeDirectoryPath(Settings.WallpaperOutputDirectory);
            var isKnownWallpaperFile =
                string.Equals(fileName, "WorldMap01.jpg", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(fileName, "WorldMap02.jpg", StringComparison.OrdinalIgnoreCase);

            return isKnownWallpaperFile &&
                   !string.IsNullOrWhiteSpace(normalizedWallpaperDirectory) &&
                   !string.IsNullOrWhiteSpace(normalizedOutputDirectory) &&
                   string.Equals(normalizedWallpaperDirectory, normalizedOutputDirectory, StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    private static string? NormalizeDirectoryPath(string? directoryPath)
    {
        if (string.IsNullOrWhiteSpace(directoryPath))
            return null;

        return Path.GetFullPath(directoryPath)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    }

    /// <summary>
    /// Checks if our wallpaper is currently active.
    /// </summary>
    /// <returns>True if our wallpaper is active, false otherwise.</returns>
    public static bool IsCurrentlyActive()
    {
        var currentPath = GetCurrentWallpaperPath();
        return IsOurWallpaperActive(currentPath);
    }

    /// <summary>
    /// Disposes of the wallpaper monitor and stops monitoring.
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            Stop();
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }

}
