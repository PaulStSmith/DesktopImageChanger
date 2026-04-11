namespace WorldMapWallpaper.Shared.Services;

/// <summary>
/// Detects satellites at Lagrange points that cannot be accurately projected onto an Earth-centered map.
/// </summary>
public static class LagrangePointDetector
{
    /// <summary>
    /// Semi-major axis threshold in kilometers. Satellites with orbits larger than this
    /// are likely at Lagrange points or in heliocentric orbits.
    /// Earth's sphere of influence is approximately 925,000 km, and L1/L2 are at ~1.5 million km.
    /// </summary>
    private const double SemiMajorAxisThresholdKm = 500_000;

    /// <summary>
    /// Known Lagrange point satellites by NORAD ID.
    /// </summary>
    private static readonly Dictionary<int, LagrangePointInfo> KnownLagrangePointSatellites = new()
    {
        { 50463, new LagrangePointInfo("James Webb Space Telescope", "L2", 1_500_000) },
        { 43435, new LagrangePointInfo("DSCOVR", "L1", 1_500_000) },
        { 39479, new LagrangePointInfo("Gaia", "L2", 1_500_000) },
        { 28928, new LagrangePointInfo("SOHO", "L1", 1_500_000) },
        { 52195, new LagrangePointInfo("Euclid", "L2", 1_500_000) },
    };

    /// <summary>
    /// Checks if a satellite is known to be at a Lagrange point.
    /// </summary>
    /// <param name="noradId">The NORAD catalog number.</param>
    /// <returns>True if the satellite is known to be at a Lagrange point.</returns>
    public static bool IsKnownLagrangePointSatellite(int noradId)
    {
        return KnownLagrangePointSatellites.ContainsKey(noradId);
    }

    /// <summary>
    /// Gets information about a known Lagrange point satellite.
    /// </summary>
    /// <param name="noradId">The NORAD catalog number.</param>
    /// <returns>Lagrange point info if known, null otherwise.</returns>
    public static LagrangePointInfo? GetLagrangePointInfo(int noradId)
    {
        return KnownLagrangePointSatellites.TryGetValue(noradId, out var info) ? info : null;
    }

    /// <summary>
    /// Checks if a semi-major axis indicates a potential Lagrange point orbit.
    /// </summary>
    /// <param name="semiMajorAxisKm">The semi-major axis in kilometers.</param>
    /// <returns>True if the orbit is suspiciously large for Earth orbit.</returns>
    public static bool IsPotentialLagrangePointOrbit(double semiMajorAxisKm)
    {
        return semiMajorAxisKm > SemiMajorAxisThresholdKm;
    }

    /// <summary>
    /// Calculates the semi-major axis from TLE mean motion.
    /// </summary>
    /// <param name="meanMotionRevsPerDay">Mean motion in revolutions per day from TLE.</param>
    /// <returns>Semi-major axis in kilometers.</returns>
    public static double CalculateSemiMajorAxisFromMeanMotion(double meanMotionRevsPerDay)
    {
        // Kepler's third law: a^3 = GM / (2*pi*n)^2
        // Where:
        //   a = semi-major axis
        //   GM = Earth's gravitational parameter = 398600.4418 km^3/s^2
        //   n = mean motion in radians/second

        const double EarthGM = 398600.4418; // km^3/s^2

        // Convert revs/day to radians/second
        var meanMotionRadPerSec = meanMotionRevsPerDay * 2 * Math.PI / 86400.0;

        if (meanMotionRadPerSec <= 0)
            return double.MaxValue; // Invalid mean motion

        // a = (GM / n^2)^(1/3)
        var semiMajorAxis = Math.Pow(EarthGM / (meanMotionRadPerSec * meanMotionRadPerSec), 1.0 / 3.0);

        return semiMajorAxis;
    }

    /// <summary>
    /// Performs a comprehensive check to determine if a satellite might be at a Lagrange point.
    /// </summary>
    /// <param name="noradId">The NORAD catalog number.</param>
    /// <param name="meanMotionRevsPerDay">Mean motion from TLE (optional).</param>
    /// <returns>Detection result with details.</returns>
    public static LagrangePointDetectionResult DetectLagrangePoint(int noradId, double? meanMotionRevsPerDay = null)
    {
        // First check known satellites
        if (KnownLagrangePointSatellites.TryGetValue(noradId, out var knownInfo))
        {
            return new LagrangePointDetectionResult
            {
                IsLagrangePoint = true,
                Confidence = DetectionConfidence.Definite,
                LagrangePoint = knownInfo.LagrangePoint,
                DistanceFromEarthKm = knownInfo.ApproximateDistanceKm,
                SatelliteName = knownInfo.Name,
                Message = $"{knownInfo.Name} is located at Lagrange Point {knownInfo.LagrangePoint}, " +
                          $"approximately {knownInfo.ApproximateDistanceKm / 1_000_000.0:F1} million km from Earth."
            };
        }

        // If we have mean motion, check orbital characteristics
        if (meanMotionRevsPerDay.HasValue && meanMotionRevsPerDay.Value > 0)
        {
            var semiMajorAxis = CalculateSemiMajorAxisFromMeanMotion(meanMotionRevsPerDay.Value);

            if (IsPotentialLagrangePointOrbit(semiMajorAxis))
            {
                return new LagrangePointDetectionResult
                {
                    IsLagrangePoint = true,
                    Confidence = DetectionConfidence.Suspected,
                    DistanceFromEarthKm = semiMajorAxis,
                    Message = $"This satellite has an orbital semi-major axis of {semiMajorAxis:N0} km, " +
                              $"which suggests it may be at a Lagrange point or in a distant orbit."
                };
            }
        }

        // Not detected as Lagrange point
        return new LagrangePointDetectionResult
        {
            IsLagrangePoint = false,
            Confidence = DetectionConfidence.NotLagrangePoint,
            Message = "This satellite appears to be in a standard Earth orbit."
        };
    }

    /// <summary>
    /// Gets the warning message for displaying to users when adding a Lagrange point satellite.
    /// </summary>
    /// <param name="result">The detection result.</param>
    /// <returns>A formatted warning message.</returns>
    public static string GetWarningMessage(LagrangePointDetectionResult result)
    {
        if (!result.IsLagrangePoint)
            return string.Empty;

        var lines = new List<string>
        {
            result.Message,
            string.Empty,
            "Satellites at Lagrange points cannot be correctly projected onto the Earth map.",
            "The position shown will not be accurate."
        };

        return string.Join(Environment.NewLine, lines);
    }
}

/// <summary>
/// Information about a known Lagrange point satellite.
/// </summary>
public class LagrangePointInfo
{
    /// <summary>
    /// Gets the satellite name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the Lagrange point (L1, L2, etc.).
    /// </summary>
    public string LagrangePoint { get; }

    /// <summary>
    /// Gets the approximate distance from Earth in kilometers.
    /// </summary>
    public double ApproximateDistanceKm { get; }

    /// <summary>
    /// Creates a new instance of LagrangePointInfo.
    /// </summary>
    public LagrangePointInfo(string name, string lagrangePoint, double approximateDistanceKm)
    {
        Name = name;
        LagrangePoint = lagrangePoint;
        ApproximateDistanceKm = approximateDistanceKm;
    }
}

/// <summary>
/// Result of Lagrange point detection.
/// </summary>
public class LagrangePointDetectionResult
{
    /// <summary>
    /// Gets or sets whether the satellite is detected as being at a Lagrange point.
    /// </summary>
    public bool IsLagrangePoint { get; set; }

    /// <summary>
    /// Gets or sets the confidence level of the detection.
    /// </summary>
    public DetectionConfidence Confidence { get; set; }

    /// <summary>
    /// Gets or sets the Lagrange point (L1, L2, etc.) if known.
    /// </summary>
    public string? LagrangePoint { get; set; }

    /// <summary>
    /// Gets or sets the approximate distance from Earth in kilometers.
    /// </summary>
    public double? DistanceFromEarthKm { get; set; }

    /// <summary>
    /// Gets or sets the satellite name if known.
    /// </summary>
    public string? SatelliteName { get; set; }

    /// <summary>
    /// Gets or sets a human-readable message about the detection.
    /// </summary>
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Confidence level for Lagrange point detection.
/// </summary>
public enum DetectionConfidence
{
    /// <summary>Not detected as a Lagrange point satellite.</summary>
    NotLagrangePoint,

    /// <summary>Suspected based on orbital characteristics.</summary>
    Suspected,

    /// <summary>Definitively known to be at a Lagrange point.</summary>
    Definite
}
