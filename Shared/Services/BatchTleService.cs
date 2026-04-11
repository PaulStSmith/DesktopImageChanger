using System.Collections.Concurrent;
using System.Text.Json;
using WorldMapWallpaper.Shared.Models;

namespace WorldMapWallpaper.Shared.Services;

/// <summary>
/// Service for fetching TLE data for multiple satellites with batching, timeouts, and caching.
/// </summary>
public class BatchTleService
{
    private static readonly HttpClient _httpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(10) // Overall client timeout
    };

    /// <summary>
    /// Maximum number of satellites that can be fetched at once.
    /// </summary>
    public const int MaxSatellites = 50;

    /// <summary>
    /// Timeout per individual satellite request in seconds.
    /// </summary>
    public const int RequestTimeoutSeconds = 5;

    /// <summary>
    /// CelesTrak API URL template for single satellite.
    /// </summary>
    private const string CelesTrakSingleUrl = "https://celestrak.org/NORAD/elements/gp.php?CATNR={0}&FORMAT=TLE";

    /// <summary>
    /// CelesTrak API URL template for multiple satellites (comma-separated).
    /// </summary>
    private const string CelesTrakBatchUrl = "https://celestrak.org/NORAD/elements/gp.php?CATNR={0}&FORMAT=TLE";

    /// <summary>
    /// Cache directory for TLE data.
    /// </summary>
    private static readonly string CacheDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "WorldMapWallpaper", "tle_cache"
    );

    /// <summary>
    /// Default cache expiration in hours (7 days).
    /// </summary>
    public const int DefaultCacheExpirationHours = 168;

    /// <summary>
    /// Maximum age for stale data in days (30 days).
    /// </summary>
    public const int StaleDataMaxAgeDays = 30;

    private readonly Action<string>? _logAction;

    /// <summary>
    /// Initializes a new instance of the BatchTleService class.
    /// </summary>
    /// <param name="logAction">Optional logging action.</param>
    public BatchTleService(Action<string>? logAction = null)
    {
        _logAction = logAction;
        EnsureCacheDirectoryExists();
    }

    /// <summary>
    /// Fetches TLE data for multiple satellites.
    /// Tries batch request first, then falls back to parallel individual requests.
    /// Uses cache as fallback when network requests fail.
    /// </summary>
    /// <param name="noradIds">The NORAD IDs to fetch.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Dictionary mapping NORAD ID to TLE data (only successful fetches included).</returns>
    public async Task<Dictionary<int, TleData>> FetchMultipleTleAsync(
        IEnumerable<int> noradIds,
        CancellationToken cancellationToken = default)
    {
        var idList = noradIds.Take(MaxSatellites).ToList();

        if (idList.Count == 0)
            return new Dictionary<int, TleData>();

        _logAction?.Invoke($"Fetching TLE data for {idList.Count} satellite(s)...");

        // Try batch request first
        var results = await TryBatchFetchAsync(idList, cancellationToken);

        // Check which ones are missing
        var missing = idList.Where(id => !results.ContainsKey(id)).ToList();

        if (missing.Count > 0)
        {
            _logAction?.Invoke($"Batch fetch incomplete, trying individual requests for {missing.Count} satellite(s)...");

            // Try individual parallel requests for missing satellites
            var individualResults = await FetchIndividualParallelAsync(missing, cancellationToken);

            foreach (var kvp in individualResults)
            {
                results[kvp.Key] = kvp.Value;
            }
        }

        // For any still missing, try cache
        var stillMissing = idList.Where(id => !results.ContainsKey(id)).ToList();
        if (stillMissing.Count > 0)
        {
            _logAction?.Invoke($"Using cache fallback for {stillMissing.Count} satellite(s)...");

            foreach (var noradId in stillMissing)
            {
                var cached = LoadFromCache(noradId, maxAgeHours: StaleDataMaxAgeDays * 24);
                if (cached != null)
                {
                    results[noradId] = cached;
                    _logAction?.Invoke($"  Loaded {cached.SatelliteName} from cache");
                }
            }
        }

        // Cache all successful results
        foreach (var kvp in results)
        {
            SaveToCache(kvp.Key, kvp.Value);
        }

        _logAction?.Invoke($"TLE fetch complete: {results.Count}/{idList.Count} successful");
        return results;
    }

    /// <summary>
    /// Attempts to fetch TLE data using batch API request.
    /// </summary>
    private async Task<Dictionary<int, TleData>> TryBatchFetchAsync(
        List<int> noradIds,
        CancellationToken cancellationToken)
    {
        var results = new Dictionary<int, TleData>();

        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(RequestTimeoutSeconds));

            // Build batch URL with comma-separated NORAD IDs
            var idsString = string.Join(",", noradIds);
            var url = string.Format(CelesTrakBatchUrl, idsString);

            _logAction?.Invoke($"Trying batch TLE fetch: {url}");

            var response = await _httpClient.GetStringAsync(url, cts.Token);
            var parsedTles = ParseBatchTleResponse(response);

            foreach (var noradId in noradIds)
            {
                if (parsedTles.TryGetValue(noradId, out var tle))
                {
                    results[noradId] = tle;
                }
            }

            _logAction?.Invoke($"Batch fetch returned {results.Count} TLE(s)");
        }
        catch (OperationCanceledException)
        {
            _logAction?.Invoke("Batch TLE fetch timed out");
        }
        catch (Exception ex)
        {
            _logAction?.Invoke($"Batch TLE fetch failed: {ex.Message}");
        }

        return results;
    }

    /// <summary>
    /// Parses a multi-satellite TLE response once into a lookup keyed by NORAD ID.
    /// </summary>
    private static Dictionary<int, TleData> ParseBatchTleResponse(string response)
    {
        var results = new Dictionary<int, TleData>();
        var lines = response.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

        for (var i = 0; i + 2 < lines.Length; i += 3)
        {
            var line1 = lines[i + 1].Trim();
            if (!line1.StartsWith("1 ") || line1.Length < 7)
                continue;

            if (!int.TryParse(line1.Substring(2, 5).Trim(), out var noradId))
                continue;

            var tleBlock = string.Join(Environment.NewLine, lines[i].Trim(), line1, lines[i + 2].Trim());
            var tle = TleData.ParseFromText(tleBlock);
            if (tle != null)
            {
                results[noradId] = tle;
            }
        }

        return results;
    }

    /// <summary>
    /// Fetches TLE data for individual satellites in parallel.
    /// </summary>
    private async Task<Dictionary<int, TleData>> FetchIndividualParallelAsync(
        List<int> noradIds,
        CancellationToken cancellationToken)
    {
        var results = new ConcurrentDictionary<int, TleData>();

        var tasks = noradIds.Select(async noradId =>
        {
            try
            {
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(TimeSpan.FromSeconds(RequestTimeoutSeconds));

                var url = string.Format(CelesTrakSingleUrl, noradId);
                var response = await _httpClient.GetStringAsync(url, cts.Token);

                var tle = TleData.ParseFromText(response);
                if (tle != null && tle.CatalogNumber == noradId)
                {
                    results.TryAdd(noradId, tle);
                    _logAction?.Invoke($"  Fetched TLE for {tle.SatelliteName} (#{noradId})");
                }
            }
            catch (OperationCanceledException)
            {
                _logAction?.Invoke($"  Timeout fetching TLE for #{noradId}");
            }
            catch (Exception ex)
            {
                _logAction?.Invoke($"  Failed to fetch TLE for #{noradId}: {ex.Message}");
            }
        });

        await Task.WhenAll(tasks);
        return results.ToDictionary(kv => kv.Key, kv => kv.Value);
    }

    /// <summary>
    /// Fetches TLE data for a single satellite.
    /// </summary>
    /// <param name="noradId">The NORAD catalog number.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>TLE data or null if fetch fails.</returns>
    public async Task<TleData?> FetchSingleTleAsync(int noradId, CancellationToken cancellationToken = default)
    {
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(RequestTimeoutSeconds));

            var url = string.Format(CelesTrakSingleUrl, noradId);
            var response = await _httpClient.GetStringAsync(url, cts.Token);

            var tle = TleData.ParseFromText(response);
            if (tle != null && tle.CatalogNumber == noradId)
            {
                SaveToCache(noradId, tle);
                return tle;
            }
        }
        catch (OperationCanceledException)
        {
            _logAction?.Invoke($"Timeout fetching TLE for #{noradId}");
        }
        catch (Exception ex)
        {
            _logAction?.Invoke($"Failed to fetch TLE for #{noradId}: {ex.Message}");
        }

        // Try cache as fallback
        return LoadFromCache(noradId, maxAgeHours: StaleDataMaxAgeDays * 24);
    }

    /// <summary>
    /// Gets TLE data for configured satellites from cache or network.
    /// </summary>
    /// <param name="satellites">The satellite configurations.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Dictionary mapping NORAD ID to TLE data.</returns>
    public async Task<Dictionary<int, TleData>> GetTleForSatellitesAsync(
        IEnumerable<SatelliteConfig> satellites,
        CancellationToken cancellationToken = default)
    {
        var enabledIds = satellites
            .Where(s => s.Enabled)
            .Select(s => s.NoradId)
            .ToList();

        return await FetchMultipleTleAsync(enabledIds, cancellationToken);
    }

    /// <summary>
    /// Saves TLE data to cache.
    /// </summary>
    private void SaveToCache(int noradId, TleData tleData)
    {
        try
        {
            var cacheFile = GetCacheFilePath(noradId);
            var cacheEntry = new TleCacheEntry
            {
                TleData = tleData,
                CachedAt = DateTime.UtcNow
            };

            var json = JsonSerializer.Serialize(cacheEntry, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(cacheFile, json);
        }
        catch
        {
            // Silently fail cache write
        }
    }

    /// <summary>
    /// Loads TLE data from cache.
    /// </summary>
    private TleData? LoadFromCache(int noradId, double maxAgeHours = DefaultCacheExpirationHours)
    {
        try
        {
            var cacheFile = GetCacheFilePath(noradId);
            if (!File.Exists(cacheFile))
                return null;

            var json = File.ReadAllText(cacheFile);
            var cacheEntry = JsonSerializer.Deserialize<TleCacheEntry>(json);

            if (cacheEntry?.TleData == null)
                return null;

            var age = DateTime.UtcNow - cacheEntry.CachedAt;
            if (age.TotalHours > maxAgeHours)
                return null;

            return cacheEntry.TleData;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Gets cache information for all cached satellites.
    /// </summary>
    /// <returns>List of cache info entries.</returns>
    public List<SatelliteCacheInfo> GetCacheInfo()
    {
        var result = new List<SatelliteCacheInfo>();

        try
        {
            if (!Directory.Exists(CacheDirectory))
                return result;

            foreach (var file in Directory.GetFiles(CacheDirectory, "*.json"))
            {
                try
                {
                    var json = File.ReadAllText(file);
                    var cacheEntry = JsonSerializer.Deserialize<TleCacheEntry>(json);

                    if (cacheEntry?.TleData != null)
                    {
                        result.Add(new SatelliteCacheInfo
                        {
                            NoradId = cacheEntry.TleData.CatalogNumber,
                            SatelliteName = cacheEntry.TleData.SatelliteName,
                            CachedAt = cacheEntry.CachedAt,
                            Age = DateTime.UtcNow - cacheEntry.CachedAt,
                            EpochDate = cacheEntry.TleData.EpochDate
                        });
                    }
                }
                catch
                {
                    // Skip invalid cache files
                }
            }
        }
        catch
        {
            // Return empty list on error
        }

        return result.OrderBy(c => c.SatelliteName).ToList();
    }

    /// <summary>
    /// Clears all cached TLE data.
    /// </summary>
    public void ClearCache()
    {
        try
        {
            if (Directory.Exists(CacheDirectory))
            {
                foreach (var file in Directory.GetFiles(CacheDirectory, "*.json"))
                {
                    File.Delete(file);
                }
                _logAction?.Invoke("TLE cache cleared");
            }
        }
        catch (Exception ex)
        {
            _logAction?.Invoke($"Failed to clear TLE cache: {ex.Message}");
        }
    }

    /// <summary>
    /// Clears cached TLE data for a specific satellite.
    /// </summary>
    /// <param name="noradId">The NORAD catalog number.</param>
    public void ClearCacheFor(int noradId)
    {
        try
        {
            var cacheFile = GetCacheFilePath(noradId);
            if (File.Exists(cacheFile))
            {
                File.Delete(cacheFile);
            }
        }
        catch
        {
            // Silently fail
        }
    }

    /// <summary>
    /// Forces a refresh of TLE data for all configured satellites.
    /// </summary>
    /// <param name="satellites">The satellite configurations.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Dictionary mapping NORAD ID to TLE data.</returns>
    public async Task<Dictionary<int, TleData>> RefreshAllTleAsync(
        IEnumerable<SatelliteConfig> satellites,
        CancellationToken cancellationToken = default)
    {
        // Clear cache for these satellites first
        foreach (var sat in satellites)
        {
            ClearCacheFor(sat.NoradId);
        }

        return await GetTleForSatellitesAsync(satellites, cancellationToken);
    }

    private string GetCacheFilePath(int noradId)
    {
        return Path.Combine(CacheDirectory, $"tle_{noradId}.json");
    }

    private void EnsureCacheDirectoryExists()
    {
        if (!Directory.Exists(CacheDirectory))
        {
            Directory.CreateDirectory(CacheDirectory);
        }
    }

    /// <summary>
    /// Cache entry for TLE data.
    /// </summary>
    private class TleCacheEntry
    {
        public TleData? TleData { get; set; }
        public DateTime CachedAt { get; set; }
    }
}

/// <summary>
/// Information about a cached satellite TLE.
/// </summary>
public class SatelliteCacheInfo
{
    /// <summary>Gets or sets the NORAD catalog number.</summary>
    public int NoradId { get; set; }

    /// <summary>Gets or sets the satellite name.</summary>
    public string SatelliteName { get; set; } = string.Empty;

    /// <summary>Gets or sets when the data was cached.</summary>
    public DateTime CachedAt { get; set; }

    /// <summary>Gets or sets the age of the cache entry.</summary>
    public TimeSpan Age { get; set; }

    /// <summary>Gets or sets the TLE epoch date.</summary>
    public DateTime EpochDate { get; set; }

    /// <summary>Gets whether the cache is fresh (less than 7 days).</summary>
    public bool IsFresh => Age.TotalHours < BatchTleService.DefaultCacheExpirationHours;
}
