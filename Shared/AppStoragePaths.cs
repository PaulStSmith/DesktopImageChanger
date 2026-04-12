using Microsoft.Win32;

namespace WorldMapWallpaper.Shared;

/// <summary>
/// Resolves scope-aware storage locations for application data and settings.
/// </summary>
public static class AppStoragePaths
{
    private const string AppName = "WorldMapWallpaper";
    private const string UninstallRegistryPath = @"Software\Microsoft\Windows\CurrentVersion\Uninstall\WorldMapWallpaper";
    private const string InstallContextFileName = "install-context.ini";

    private static readonly Lazy<InstallationScope> ScopeResolver = new(ResolveInstallScopeCore);

    /// <summary>
    /// Gets the detected installation scope for the current application instance.
    /// </summary>
    public static InstallationScope InstallScope => ScopeResolver.Value;

    /// <summary>
    /// Gets the root data directory for the current installation scope.
    /// </summary>
    public static string DataDirectory => GetDataDirectory(InstallScope);

    /// <summary>
    /// Gets the settings file path for the current installation scope.
    /// </summary>
    public static string SettingsFilePath => Path.Combine(DataDirectory, "settings.json");

    /// <summary>
    /// Gets the settings file path for a specific installation scope.
    /// </summary>
    public static string GetSettingsFilePath(InstallationScope scope)
    {
        return Path.Combine(GetDataDirectory(scope), "settings.json");
    }

    /// <summary>
    /// Gets the default wallpaper output directory for a specific scope.
    /// </summary>
    public static string GetDefaultWallpaperOutputDirectory(InstallationScope scope)
    {
        var specialFolder = scope == InstallationScope.AllUsers
            ? Environment.SpecialFolder.CommonPictures
            : Environment.SpecialFolder.MyPictures;

        return Environment.GetFolderPath(specialFolder);
    }

    /// <summary>
    /// Gets a file path under the current scope's data directory.
    /// </summary>
    public static string GetDataFilePath(string fileName)
    {
        return Path.Combine(DataDirectory, fileName);
    }

    /// <summary>
    /// Gets a directory path under the current scope's data directory.
    /// </summary>
    public static string GetDataSubdirectory(string directoryName)
    {
        return Path.Combine(DataDirectory, directoryName);
    }

    /// <summary>
    /// Ensures the current scope's data directory exists and returns it.
    /// </summary>
    public static string EnsureDataDirectoryExists()
    {
        var directory = DataDirectory;
        Directory.CreateDirectory(directory);
        return directory;
    }

    private static string GetDataDirectory(InstallationScope scope)
    {
        var baseFolder = scope == InstallationScope.AllUsers
            ? Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData)
            : Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

        return Path.Combine(baseFolder, AppName);
    }

    private static InstallationScope ResolveInstallScopeCore()
    {
        var installContextPath = Path.Combine(AppContext.BaseDirectory, InstallContextFileName);
        var scopeFromContext = TryReadScopeFromInstallContext(installContextPath);
        if (scopeFromContext.HasValue)
            return scopeFromContext.Value;

        var baseDirectory = NormalizePath(AppContext.BaseDirectory);
        if (baseDirectory != null)
        {
            var scopeFromRegistry = TryResolveScopeFromUninstallRegistry(baseDirectory);
            if (scopeFromRegistry.HasValue)
                return scopeFromRegistry.Value;
        }

        return InstallationScope.CurrentUser;
    }

    private static InstallationScope? TryReadScopeFromInstallContext(string installContextPath)
    {
        try
        {
            if (!File.Exists(installContextPath))
                return null;

            foreach (var rawLine in File.ReadLines(installContextPath))
            {
                var line = rawLine.Trim();
                if (!line.StartsWith("Scope=", StringComparison.OrdinalIgnoreCase))
                    continue;

                var value = line["Scope=".Length..].Trim();
                if (value.Equals("AllUsers", StringComparison.OrdinalIgnoreCase))
                    return InstallationScope.AllUsers;

                if (value.Equals("CurrentUser", StringComparison.OrdinalIgnoreCase))
                    return InstallationScope.CurrentUser;
            }
        }
        catch
        {
            // Fall back to registry/default detection.
        }

        return null;
    }

    private static InstallationScope? TryResolveScopeFromUninstallRegistry(string baseDirectory)
    {
        try
        {
            using var machineKey = Registry.LocalMachine.OpenSubKey(UninstallRegistryPath);
            if (machineKey != null && IsMatchingInstallLocation(machineKey, baseDirectory))
                return InstallationScope.AllUsers;

            using var userKey = Registry.CurrentUser.OpenSubKey(UninstallRegistryPath);
            if (userKey != null && IsMatchingInstallLocation(userKey, baseDirectory))
                return InstallationScope.CurrentUser;
        }
        catch
        {
            // Fall through to default scope.
        }

        return null;
    }

    private static bool IsMatchingInstallLocation(RegistryKey key, string baseDirectory)
    {
        var installLocation = key.GetValue("InstallLocation")?.ToString();
        var normalizedInstallLocation = NormalizePath(installLocation);
        return normalizedInstallLocation != null &&
               string.Equals(normalizedInstallLocation, baseDirectory, StringComparison.OrdinalIgnoreCase);
    }

    private static string? NormalizePath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return null;

        return Path.GetFullPath(path)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    }
}

/// <summary>
/// Installation scope used to resolve shared storage.
/// </summary>
public enum InstallationScope
{
    CurrentUser,
    AllUsers
}
