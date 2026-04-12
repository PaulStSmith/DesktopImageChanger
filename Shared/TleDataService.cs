using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace WorldMapWallpaper.Shared
{
    /// <summary>
    /// Service for fetching and managing TLE data from external sources.
    /// </summary>
    public class TleDataService
    {
        private static readonly HttpClient _httpClient = new()
        {
            Timeout = TimeSpan.FromSeconds(30)
        };

        // TLE data sources
        private const string CELESTRAK_STATIONS_URL = "https://celestrak.org/NORAD/elements/stations.txt";
        private const string BACKUP_CELESTRAK_URL = "https://celestrak.org/NORAD/elements/active.txt";
        
        // ISS catalog numbers
        private const int ISS_ZARYA_CATALOG = 25544;
        private const int ISS_NAUKA_CATALOG = 49044;

        // Cache settings
        private static string CacheDirectory => AppStoragePaths.EnsureDataDirectoryExists();
        private static string TleCacheFile => AppStoragePaths.GetDataFilePath("iss_tle_cache.json");

        private readonly Action<string>? _logAction;

        /// <summary>
        /// Initializes a new instance of the TleDataService class.
        /// </summary>
        /// <param name="logAction">Optional action for debug messages.</param>
        public TleDataService(Action<string>? logAction = null)
        {
            _logAction = logAction;
            
            // Ensure cache directory exists
            if (!Directory.Exists(CacheDirectory))
                Directory.CreateDirectory(CacheDirectory);
        }

        /// <summary>
        /// Fetches the latest ISS TLE data from CelesTrak.
        /// </summary>
        /// <returns>ISS TLE data or null if fetch fails.</returns>
        public async Task<TleData?> FetchIssTleAsync()
        {
            try
            {
                _logAction?.Invoke("Fetching ISS TLE data from CelesTrak...");

                // Try primary source first
                var tleData = await TryFetchFromUrl(CELESTRAK_STATIONS_URL);
                
                // If primary fails, try backup source
                if (tleData == null)
                {
                    _logAction?.Invoke("Primary TLE source failed, trying backup...");
                    tleData = await TryFetchFromUrl(BACKUP_CELESTRAK_URL);
                }

                if (tleData != null)
                {
                    _logAction?.Invoke($"Successfully fetched TLE for {tleData.SatelliteName} (#{tleData.CatalogNumber})");
                    
                    // Cache the successful result
                    await CacheTleDataAsync(tleData);
                    
                    return tleData;
                }
                else
                {
                    _logAction?.Invoke("All TLE sources failed");
                    return null;
                }
            }
            catch (Exception ex)
            {
                _logAction?.Invoke($"Error fetching TLE data: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Attempts to fetch TLE data from a specific URL.
        /// </summary>
        /// <param name="url">The URL to fetch from.</param>
        /// <returns>ISS TLE data or null if not found.</returns>
        private async Task<TleData?> TryFetchFromUrl(string url)
        {
            try
            {
                var response = await _httpClient.GetStringAsync(url);
                
                // Try to find ISS (Zarya) first
                var tleData = TleData.ExtractSatelliteTle(response, ISS_ZARYA_CATALOG);
                
                // If Zarya not found, try Nauka module
                if (tleData == null)
                {
                    _logAction?.Invoke("ISS Zarya not found, trying Nauka module...");
                    tleData = TleData.ExtractSatelliteTle(response, ISS_NAUKA_CATALOG);
                }

                return tleData;
            }
            catch (Exception ex)
            {
                _logAction?.Invoke($"Failed to fetch from {url}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Loads ISS TLE data from cache if available and still valid.
        /// </summary>
        /// <param name="maxAgeHours">Maximum age of cached data in hours (default: 168 = 1 week).</param>
        /// <returns>Cached TLE data or null if not available or expired.</returns>
        public async Task<TleData?> LoadCachedTleAsync(double maxAgeHours = 168)
        {
            try
            {
                if (!File.Exists(TleCacheFile))
                {
                    _logAction?.Invoke("No TLE cache file found");
                    return null;
                }

                var json = await File.ReadAllTextAsync(TleCacheFile);
                var cacheData = JsonConvert.DeserializeObject<TleCacheData>(json);

                if (cacheData?.TleData == null)
                {
                    _logAction?.Invoke("Invalid TLE cache format");
                    return null;
                }

                // Check if cache is still valid
                var age = DateTime.UtcNow - cacheData.CachedAt;
                if (age.TotalHours > maxAgeHours)
                {
                    _logAction?.Invoke($"TLE cache expired (age: {age.TotalHours:F1} hours)");
                    return null;
                }

                _logAction?.Invoke($"Loaded TLE from cache (age: {age.TotalHours:F1} hours)");
                return cacheData.TleData;
            }
            catch (Exception ex)
            {
                _logAction?.Invoke($"Error loading TLE cache: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Gets ISS TLE data, trying cache first, then fetching if needed.
        /// </summary>
        /// <param name="maxCacheAgeHours">Maximum age of cached data before fetching new (default: 168 = 1 week).</param>
        /// <returns>ISS TLE data or null if unavailable.</returns>
        public async Task<TleData?> GetIssTleAsync(double maxCacheAgeHours = 168)
        {
            // First try to load from cache
            var cachedTle = await LoadCachedTleAsync(maxCacheAgeHours);
            if (cachedTle != null)
                return cachedTle;

            // Cache miss or expired, fetch fresh data
            _logAction?.Invoke("TLE cache miss or expired, fetching fresh data...");
            return await FetchIssTleAsync();
        }

        /// <summary>
        /// Forces a refresh of TLE data by fetching from the network.
        /// </summary>
        /// <returns>Fresh ISS TLE data or null if fetch fails.</returns>
        public async Task<TleData?> RefreshIssTleAsync()
        {
            _logAction?.Invoke("Force refreshing ISS TLE data...");
            return await FetchIssTleAsync();
        }

        /// <summary>
        /// Caches TLE data to disk for offline use.
        /// </summary>
        /// <param name="tleData">The TLE data to cache.</param>
        private async Task CacheTleDataAsync(TleData tleData)
        {
            try
            {
                var cacheData = new TleCacheData
                {
                    TleData = tleData,
                    CachedAt = DateTime.UtcNow,
                    Source = "CelesTrak"
                };

                var json = JsonConvert.SerializeObject(cacheData, Formatting.Indented);
                await File.WriteAllTextAsync(TleCacheFile, json);
                
                _logAction?.Invoke($"Cached TLE data to {TleCacheFile}");
            }
            catch (Exception ex)
            {
                _logAction?.Invoke($"Failed to cache TLE data: {ex.Message}");
            }
        }

        /// <summary>
        /// Checks if the TLE cache exists and when it was last updated.
        /// </summary>
        /// <returns>Cache info or null if no cache exists.</returns>
        public TleCacheInfo? GetCacheInfo()
        {
            try
            {
                if (!File.Exists(TleCacheFile))
                    return null;

                var json = File.ReadAllText(TleCacheFile);
                var cacheData = JsonConvert.DeserializeObject<TleCacheData>(json);

                if (cacheData?.TleData == null)
                    return null;

                return new TleCacheInfo
                {
                    CachedAt = cacheData.CachedAt,
                    SatelliteName = cacheData.TleData.SatelliteName,
                    CatalogNumber = cacheData.TleData.CatalogNumber,
                    EpochDate = cacheData.TleData.EpochDate,
                    Source = cacheData.Source,
                    Age = DateTime.UtcNow - cacheData.CachedAt
                };
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Clears the TLE cache.
        /// </summary>
        public void ClearCache()
        {
            try
            {
                if (File.Exists(TleCacheFile))
                {
                    File.Delete(TleCacheFile);
                    _logAction?.Invoke("TLE cache cleared");
                }
            }
            catch (Exception ex)
            {
                _logAction?.Invoke($"Failed to clear TLE cache: {ex.Message}");
            }
        }

        /// <summary>
        /// Internal class for TLE cache data structure.
        /// </summary>
        private class TleCacheData
        {
            [JsonProperty("tle_data")]
            public TleData? TleData { get; set; }

            [JsonProperty("cached_at")]
            public DateTime CachedAt { get; set; }

            [JsonProperty("source")]
            public string Source { get; set; } = string.Empty;
        }
    }

    /// <summary>
    /// Information about cached TLE data.
    /// </summary>
    public class TleCacheInfo
    {
        /// <summary>
        /// Gets or sets when the data was cached.
        /// </summary>
        public DateTime CachedAt { get; set; }

        /// <summary>
        /// Gets or sets the satellite name.
        /// </summary>
        public string SatelliteName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the catalog number.
        /// </summary>
        public int CatalogNumber { get; set; }

        /// <summary>
        /// Gets or sets the TLE epoch date.
        /// </summary>
        public DateTime EpochDate { get; set; }

        /// <summary>
        /// Gets or sets the data source.
        /// </summary>
        public string Source { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the age of the cached data.
        /// </summary>
        public TimeSpan Age { get; set; }

        /// <summary>
        /// Gets whether the cache is considered fresh (less than 1 week old).
        /// </summary>
        public bool IsFresh => Age.TotalHours < 168;

        /// <summary>
        /// Returns a string representation of the cache info.
        /// </summary>
        /// <returns>Formatted cache information.</returns>
        public override string ToString()
        {
            return $"{SatelliteName} (#{CatalogNumber}) - Cached: {CachedAt:yyyy-MM-dd HH:mm} UTC " +
                   $"(Age: {Age.TotalHours:F1}h), Epoch: {EpochDate:yyyy-MM-dd HH:mm} UTC";
        }
    }
}
