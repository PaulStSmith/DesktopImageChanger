using System.Collections.Concurrent;
using System.Drawing;
using WorldMapWallpaper.Properties;
using WorldMapWallpaper.Shared;
using WorldMapWallpaper.Shared.Models;
using WorldMapWallpaper.Shared.Services;

namespace WorldMapWallpaper;

/// <summary>
/// Tracks multiple satellites and renders them on the world map.
/// Uses BatchTleService for efficient TLE data fetching and caching.
/// </summary>
public class MultiSatelliteTracker
{
    private readonly Logger _logger;
    private readonly double _timeOffset;
    private readonly double _declination;
    private readonly BatchTleService _tleService;
    private readonly SatelliteConfigManager _configManager;

    /// <summary>
    /// Cache of parsed satellite records from TLE data.
    /// </summary>
    private readonly ConcurrentDictionary<int, SatelliteRecord> _satelliteRecords = new();

    /// <summary>
    /// Cache of custom icons loaded for satellites.
    /// </summary>
    private readonly ConcurrentDictionary<int, Bitmap?> _iconCache = new();

    /// <summary>
    /// Default ISS icon for backwards compatibility.
    /// </summary>
    private readonly Bitmap? _defaultIssIcon;

    /// <summary>
    /// Orbit visualization parameters.
    /// </summary>
    private const int OrbitSegments = 50;
    private const double MinutesBeforeCurrent = 10.0;
    private const double MinutesAfterCurrent = 10.0;

    /// <summary>
    /// Initializes a new instance of the MultiSatelliteTracker class.
    /// </summary>
    /// <param name="logger">Logger for debug messages.</param>
    /// <param name="timeOffset">Time offset for sunlight calculation.</param>
    /// <param name="declination">Solar declination for sunlight calculation.</param>
    public MultiSatelliteTracker(Logger logger, double timeOffset, double declination)
    {
        _logger = logger;
        _timeOffset = timeOffset;
        _declination = declination;
        _tleService = new BatchTleService(msg => logger.Debug(msg));
        _configManager = SatelliteConfigManager.Instance;

        // Try to load the default ISS icon
        try
        {
            _defaultIssIcon = Resources.ISSIcon;
        }
        catch
        {
            _logger.Debug("Could not load default ISS icon");
        }
    }

    /// <summary>
    /// Plots all enabled satellites on the map.
    /// </summary>
    /// <param name="map">The world map bitmap to draw on.</param>
    /// <returns>The map with satellites plotted.</returns>
    public Bitmap PlotSatellites(Bitmap map)
    {
        if (!Settings.SatelliteTrackingEnabled)
        {
            _logger.Debug("Satellite tracking is disabled");
            return map;
        }

        try
        {
            var visibleSatellites = _configManager.GetVisibleSatellites().ToList();

            if (visibleSatellites.Count == 0)
            {
                _logger.Debug("No visible satellites configured");
                return map;
            }

            _logger.Info($"Tracking {visibleSatellites.Count} satellite(s)");

            // Fetch TLE data for all enabled satellites
            var tleData = _tleService.GetTleForSatellitesAsync(visibleSatellites)
                .GetAwaiter().GetResult();

            // Parse TLE data into satellite records
            UpdateSatelliteRecords(tleData);

            // Calculate positions and prepare render data
            var renderData = new List<SatelliteRenderer.SatelliteRenderData>();
            var currentTime = DateTime.UtcNow;

            foreach (var config in visibleSatellites)
            {
                try
                {
                    var position = CalculatePosition(config.NoradId, currentTime);
                    if (position == null)
                    {
                        _logger.Debug($"Could not calculate position for {config.Name}");
                        continue;
                    }

                    // Calculate sunlight status
                    position = CalculateSunlightStatus(position);

                    // Calculate orbit path if enabled
                    List<SatelliteRenderer.SatellitePosition>? orbitPath = null;
                    if (config.ShowOrbit)
                    {
                        orbitPath = CalculateOrbitPath(config.NoradId, currentTime);
                    }

                    // Load custom icon if specified
                    var icon = GetSatelliteIcon(config);

                    renderData.Add(new SatelliteRenderer.SatelliteRenderData
                    {
                        Config = config,
                        Position = position,
                        OrbitPath = orbitPath,
                        CustomIcon = icon
                    });

                    _logger.Debug($"Prepared render data for {config.Name}: " +
                        $"Lat={position.Latitude:F2}, Lon={position.Longitude:F2}");
                }
                catch (Exception ex)
                {
                    _logger.Debug($"Error preparing render data for {config.Name}: {ex.Message}");
                }
            }

            if (renderData.Count == 0)
            {
                _logger.Debug("No satellite positions available to render");
                return map;
            }

            // Create a copy of the map and render satellites
            var mapCopy = new Bitmap(map);
            using var graphics = Graphics.FromImage(mapCopy);

            var renderer = new SatelliteRenderer(_logger, mapCopy.Width, mapCopy.Height);
            renderer.RenderSatellites(graphics, renderData.OrderBy(r => r.Config.Priority));

            _logger.Info($"Rendered {renderData.Count} satellite(s) on map");
            return mapCopy;
        }
        catch (Exception ex)
        {
            _logger.Debug($"Error plotting satellites: {ex.Message}");
            return map;
        }
    }

    /// <summary>
    /// Updates the satellite record cache from TLE data.
    /// </summary>
    private void UpdateSatelliteRecords(Dictionary<int, TleData> tleData)
    {
        foreach (var kvp in tleData)
        {
            var record = SatelliteRecord.ParseFromTle(kvp.Value);
            if (record != null)
            {
                _satelliteRecords[kvp.Key] = record;
                _logger.Debug($"Updated satellite record for {record.SatelliteName} (#{kvp.Key})");
            }
        }
    }

    /// <summary>
    /// Calculates the current position of a satellite.
    /// </summary>
    private SatelliteRenderer.SatellitePosition? CalculatePosition(int noradId, DateTime time)
    {
        if (!_satelliteRecords.TryGetValue(noradId, out var record))
        {
            _logger.Debug($"No satellite record found for #{noradId}");
            return null;
        }

        try
        {
            // Use the basic orbital propagator
            var positionVelocity = BasicOrbitalPropagator.Propagate(record, time);
            var gmst = OrbitalMath.GreenwichMeanSiderealTime(time);
            var geodetic = CoordinateTransforms.EciToGeodetic(positionVelocity.Position, gmst);

            var latDegrees = OrbitalMath.RadiansToDegrees(geodetic.Latitude);
            var lonDegrees = OrbitalMath.RadiansToDegrees(geodetic.Longitude);

            return new SatelliteRenderer.SatellitePosition(
                latDegrees,
                lonDegrees,
                time,
                false,
                geodetic.Altitude);
        }
        catch (Exception ex)
        {
            _logger.Debug($"Error calculating position for #{noradId}: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Calculates the orbital path for a satellite.
    /// </summary>
    private List<SatelliteRenderer.SatellitePosition>? CalculateOrbitPath(int noradId, DateTime currentTime)
    {
        if (!_satelliteRecords.TryGetValue(noradId, out var record))
            return null;

        var orbitPoints = new List<SatelliteRenderer.SatellitePosition>();

        try
        {
            for (var i = 0; i < OrbitSegments; i++)
            {
                var minutesFromNow = -MinutesBeforeCurrent +
                    (i * (MinutesBeforeCurrent + MinutesAfterCurrent) / (OrbitSegments - 1));
                var targetTime = currentTime.AddMinutes(minutesFromNow);

                try
                {
                    var positionVelocity = BasicOrbitalPropagator.Propagate(record, targetTime);
                    var gmst = OrbitalMath.GreenwichMeanSiderealTime(targetTime);
                    var geodetic = CoordinateTransforms.EciToGeodetic(positionVelocity.Position, gmst);

                    var latitude = OrbitalMath.RadiansToDegrees(geodetic.Latitude);
                    var longitude = OrbitalMath.RadiansToDegrees(geodetic.Longitude);

                    orbitPoints.Add(new SatelliteRenderer.SatellitePosition(
                        latitude, longitude, targetTime, false, geodetic.Altitude));
                }
                catch
                {
                    // Skip this point if calculation fails
                }
            }
        }
        catch (Exception ex)
        {
            _logger.Debug($"Error calculating orbit path for #{noradId}: {ex.Message}");
        }

        return orbitPoints.Count > 1 ? orbitPoints : null;
    }

    /// <summary>
    /// Calculates whether a satellite is in sunlight based on the terminator position.
    /// </summary>
    private SatelliteRenderer.SatellitePosition CalculateSunlightStatus(SatelliteRenderer.SatellitePosition position)
    {
        try
        {
            var terminatorLat = Program.GetTerminatorLatitude(position.Longitude, _timeOffset, _declination);

            var inSunlight = (_declination >= 0 && position.Latitude >= terminatorLat) ||
                            (_declination < 0 && position.Latitude <= terminatorLat);

            return new SatelliteRenderer.SatellitePosition(
                position.Latitude,
                position.Longitude,
                position.Timestamp,
                inSunlight,
                position.AltitudeKm);
        }
        catch
        {
            return position;
        }
    }

    /// <summary>
    /// Gets the appropriate icon for a satellite.
    /// </summary>
    private Bitmap? GetSatelliteIcon(SatelliteConfig config)
    {
        // Check cache first
        if (_iconCache.TryGetValue(config.NoradId, out var cachedIcon))
            return cachedIcon;

        Bitmap? icon = null;

        // Try to load custom icon if specified
        if (!string.IsNullOrEmpty(config.IconPath))
        {
            icon = SatelliteRenderer.LoadSatelliteIcon(config.IconPath);
            if (icon != null)
            {
                _logger.Debug($"Loaded custom icon for {config.Name}: {config.IconPath}");
            }
        }

        // Use default ISS icon for ISS (NORAD ID 25544)
        if (icon == null && config.NoradId == 25544)
        {
            icon = _defaultIssIcon;
        }

        // Cache the result (even if null - means we'll use the default marker)
        _iconCache[config.NoradId] = icon;

        return icon;
    }

    /// <summary>
    /// Clears the satellite record and icon caches.
    /// </summary>
    public void ClearCaches()
    {
        _satelliteRecords.Clear();

        // Dispose and clear icon cache (except default ISS icon)
        foreach (var kvp in _iconCache)
        {
            if (kvp.Value != null && kvp.Value != _defaultIssIcon)
            {
                kvp.Value.Dispose();
            }
        }
        _iconCache.Clear();

        _logger.Debug("Cleared satellite caches");
    }

    /// <summary>
    /// Forces a refresh of TLE data for all enabled satellites.
    /// </summary>
    public void RefreshTleData()
    {
        try
        {
            var satellites = _configManager.Satellites.Where(s => s.Enabled).ToList();
            if (satellites.Count == 0) return;

            _tleService.RefreshAllTleAsync(satellites).GetAwaiter().GetResult();
            _logger.Info("Refreshed TLE data for all satellites");
        }
        catch (Exception ex)
        {
            _logger.Debug($"Error refreshing TLE data: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets cache information for diagnostics.
    /// </summary>
    public List<SatelliteCacheInfo> GetTleCacheInfo()
    {
        return _tleService.GetCacheInfo();
    }
}
