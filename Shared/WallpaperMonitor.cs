using Microsoft.Win32;
using System.IO;

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
        if (_disposed)
            throw new ObjectDisposedException(nameof(WallpaperMonitor));

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
    /// Gets the current wallpaper path from the Windows registry.
    /// </summary>
    /// <returns>The current wallpaper file path, or null if it can't be determined.</returns>
    private static string? GetCurrentWallpaperPath()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(WallpaperRegistryPath);
            return key?.GetValue(WallpaperValueName)?.ToString();
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
            // Check if the wallpaper path contains our application directory or temp files
            var fileName = Path.GetFileName(wallpaperPath);
            var directory = Path.GetDirectoryName(wallpaperPath);

            // Our wallpaper files typically have specific naming patterns
            return fileName?.StartsWith("WorldMapWallpaper", StringComparison.OrdinalIgnoreCase) == true ||
                   fileName?.StartsWith("worldmap", StringComparison.OrdinalIgnoreCase) == true ||
                   directory?.Contains("WorldMapWallpaper", StringComparison.OrdinalIgnoreCase) == true ||
                   directory?.Contains("Temp", StringComparison.OrdinalIgnoreCase) == true; // Our temp files
        }
        catch
        {
            return false;
        }
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