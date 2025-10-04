using System.Runtime.InteropServices;
using System.Text;

namespace WorldMapWallpaper.Shared;

/// <summary>
/// Centralized P/Invoke declarations for Windows API calls used throughout the application.
/// This class consolidates all native method imports to avoid duplication and provide
/// a single point of maintenance for Windows API interactions.
/// </summary>
public static class NativeMethods
{
    /// <summary>
    /// Sets a string parameter in the system parameters.
    /// Used primarily for setting the desktop wallpaper.
    /// </summary>
    /// <param name="uiAction">The system parameter to set (e.g., SPI_SETDESKWALLPAPER).</param>
    /// <param name="uiParam">A parameter whose usage and format depends on the system parameter being set.</param>
    /// <param name="pvParam">A parameter whose usage and format depends on the system parameter being set.</param>
    /// <param name="fWinIni">Flags specifying how the user profile is to be updated.</param>
    /// <returns>True if the function succeeds; otherwise, false.</returns>
    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool SystemParametersInfo(SPI uiAction, uint uiParam, string? pvParam, SPIF fWinIni);

    /// <summary>
    /// Retrieves a string parameter from the system parameters.
    /// Used primarily for getting the current desktop wallpaper path.
    /// </summary>
    /// <param name="uiAction">The system parameter to retrieve (e.g., SPI_GETDESKWALLPAPER).</param>
    /// <param name="uiParam">A parameter whose usage and format depends on the system parameter being retrieved.</param>
    /// <param name="pvParam">A parameter whose usage and format depends on the system parameter being retrieved.</param>
    /// <param name="fWinIni">Flags specifying how the user profile is to be updated.</param>
    /// <returns>True if the function succeeds; otherwise, false.</returns>
    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool SystemParametersInfo(SPI uiAction, uint uiParam, StringBuilder pvParam, SPIF fWinIni);
}