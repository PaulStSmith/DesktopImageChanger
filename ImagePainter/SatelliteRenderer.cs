using System.Drawing;
using System.Drawing.Drawing2D;
using WorldMapWallpaper.Shared;
using WorldMapWallpaper.Shared.Models;

namespace WorldMapWallpaper;

/// <summary>
/// Provides rendering functionality for satellites on the world map.
/// Supports custom icons, colors, and orbit trail rendering.
/// </summary>
public class SatelliteRenderer
{
    private readonly Logger _logger;
    private readonly int _mapWidth;
    private readonly int _mapHeight;

    /// <summary>
    /// Default icon size for satellites without custom icons.
    /// </summary>
    public const int DefaultIconSize = 16;

    /// <summary>
    /// Initializes a new instance of the SatelliteRenderer class.
    /// </summary>
    /// <param name="logger">Logger for debug messages.</param>
    /// <param name="mapWidth">Width of the map in pixels.</param>
    /// <param name="mapHeight">Height of the map in pixels.</param>
    public SatelliteRenderer(Logger logger, int mapWidth, int mapHeight)
    {
        _logger = logger;
        _mapWidth = mapWidth;
        _mapHeight = mapHeight;
    }

    /// <summary>
    /// Represents a satellite position for rendering.
    /// </summary>
    public class SatellitePosition
    {
        /// <summary>Gets or sets the latitude in degrees (-90 to 90).</summary>
        public double Latitude { get; set; }

        /// <summary>Gets or sets the longitude in degrees (-180 to 180).</summary>
        public double Longitude { get; set; }

        /// <summary>Gets or sets the timestamp of the position.</summary>
        public DateTime Timestamp { get; set; }

        /// <summary>Gets or sets whether the satellite is in sunlight.</summary>
        public bool IsInSunlight { get; set; }

        /// <summary>Gets or sets the altitude in kilometers.</summary>
        public double AltitudeKm { get; set; }

        /// <summary>Creates a new SatellitePosition instance.</summary>
        public SatellitePosition(double latitude, double longitude, DateTime timestamp, bool inSunlight = false, double altitudeKm = 0)
        {
            Latitude = latitude;
            Longitude = longitude;
            Timestamp = timestamp;
            IsInSunlight = inSunlight;
            AltitudeKm = altitudeKm;
        }
    }

    /// <summary>
    /// Render data for a single satellite including its configuration and current position.
    /// </summary>
    public class SatelliteRenderData
    {
        /// <summary>Gets or sets the satellite configuration.</summary>
        public required SatelliteConfig Config { get; set; }

        /// <summary>Gets or sets the current position.</summary>
        public required SatellitePosition Position { get; set; }

        /// <summary>Gets or sets the orbital path points (optional).</summary>
        public List<SatellitePosition>? OrbitPath { get; set; }

        /// <summary>Gets or sets the custom icon to use (optional).</summary>
        public Bitmap? CustomIcon { get; set; }
    }

    /// <summary>
    /// Renders a single satellite on the map.
    /// </summary>
    /// <param name="graphics">The graphics context to draw on.</param>
    /// <param name="data">The satellite render data.</param>
    public void RenderSatellite(Graphics graphics, SatelliteRenderData data)
    {
        if (data.Config == null || data.Position == null)
            return;

        try
        {
            var pixelPos = GetPixelCoordinates(data.Position.Latitude, data.Position.Longitude);

            // Draw orbit first (so satellite appears on top)
            if (data.Config.ShowOrbit && data.OrbitPath != null && data.OrbitPath.Count > 1)
            {
                DrawOrbitTrail(graphics, data.OrbitPath, data.Config.Color ?? "#FFFFFF");
            }

            // Draw satellite icon or marker
            if (data.CustomIcon != null)
            {
                DrawSatelliteIcon(graphics, pixelPos, data.CustomIcon);
            }
            else
            {
                DrawSatelliteMarker(graphics, pixelPos, data.Config.Color ?? "#FFFFFF");
            }

            // Draw info label
            var labelY = pixelPos.Y + (data.CustomIcon?.Height ?? DefaultIconSize) / 2 + 2;
            DrawSatelliteInfo(graphics, data.Config.Name, data.Position, pixelPos.X, labelY);

            _logger?.Debug($"Rendered satellite {data.Config.Name} at ({pixelPos.X}, {pixelPos.Y})");
        }
        catch (Exception ex)
        {
            _logger?.Debug($"Error rendering satellite {data.Config.Name}: {ex.Message}");
        }
    }

    /// <summary>
    /// Renders multiple satellites on the map.
    /// </summary>
    /// <param name="graphics">The graphics context to draw on.</param>
    /// <param name="satellites">The satellites to render, sorted by priority.</param>
    public void RenderSatellites(Graphics graphics, IEnumerable<SatelliteRenderData> satellites)
    {
        // First pass: draw all orbits
        foreach (var sat in satellites)
        {
            if (sat.Config.ShowOrbit && sat.OrbitPath != null && sat.OrbitPath.Count > 1)
            {
                DrawOrbitTrail(graphics, sat.OrbitPath, sat.Config.Color ?? "#FFFFFF");
            }
        }

        // Second pass: draw all satellite icons and labels (so they appear on top)
        foreach (var sat in satellites)
        {
            RenderSatelliteIconAndLabel(graphics, sat);
        }
    }

    /// <summary>
    /// Renders just the icon and label for a satellite (without orbit).
    /// </summary>
    private void RenderSatelliteIconAndLabel(Graphics graphics, SatelliteRenderData data)
    {
        if (data.Config == null || data.Position == null)
            return;

        try
        {
            var pixelPos = GetPixelCoordinates(data.Position.Latitude, data.Position.Longitude);

            // Draw satellite icon or marker
            if (data.CustomIcon != null)
            {
                DrawSatelliteIcon(graphics, pixelPos, data.CustomIcon);
            }
            else
            {
                DrawSatelliteMarker(graphics, pixelPos, data.Config.Color ?? "#FFFFFF");
            }

            // Draw info label
            var labelY = pixelPos.Y + (data.CustomIcon?.Height ?? DefaultIconSize) / 2 + 2;
            DrawSatelliteInfo(graphics, data.Config.Name, data.Position, pixelPos.X, labelY);
        }
        catch (Exception ex)
        {
            _logger?.Debug($"Error rendering satellite icon for {data.Config.Name}: {ex.Message}");
        }
    }

    /// <summary>
    /// Converts latitude and longitude to pixel coordinates on the map.
    /// Assumes equirectangular projection.
    /// </summary>
    /// <param name="latitude">Latitude in degrees (-90 to 90).</param>
    /// <param name="longitude">Longitude in degrees (-180 to 180).</param>
    /// <returns>Pixel coordinates on the map.</returns>
    public Point GetPixelCoordinates(double latitude, double longitude)
    {
        var x = (int)((longitude + 180.0) * _mapWidth / 360.0);
        var y = (int)((90.0 - latitude) * _mapHeight / 180.0);

        // Ensure coordinates are within bounds
        x = Math.Max(0, Math.Min(_mapWidth - 1, x));
        y = Math.Max(0, Math.Min(_mapHeight - 1, y));

        return new Point(x, y);
    }

    /// <summary>
    /// Draws a satellite icon centered at the specified position.
    /// </summary>
    private void DrawSatelliteIcon(Graphics graphics, Point position, Bitmap icon)
    {
        var drawX = position.X - (icon.Width / 2);
        var drawY = position.Y - (icon.Height / 2);
        graphics.DrawImage(icon, drawX, drawY);
    }

    /// <summary>
    /// Draws a default satellite marker (colored circle) at the specified position.
    /// </summary>
    private void DrawSatelliteMarker(Graphics graphics, Point position, string hexColor)
    {
        var color = ParseHexColor(hexColor);

        using var brush = new SolidBrush(color);
        using var pen = new Pen(Color.White, 1);

        var rect = new Rectangle(
            position.X - DefaultIconSize / 2,
            position.Y - DefaultIconSize / 2,
            DefaultIconSize,
            DefaultIconSize);

        // Draw filled circle
        graphics.FillEllipse(brush, rect);
        // Draw white border
        graphics.DrawEllipse(pen, rect);

        // Draw inner highlight
        using var highlightBrush = new SolidBrush(Color.FromArgb(100, Color.White));
        var highlightRect = new Rectangle(
            position.X - DefaultIconSize / 4,
            position.Y - DefaultIconSize / 4,
            DefaultIconSize / 2,
            DefaultIconSize / 2);
        graphics.FillEllipse(highlightBrush, highlightRect);
    }

    /// <summary>
    /// Draws satellite information text below the satellite icon.
    /// </summary>
    private void DrawSatelliteInfo(Graphics graphics, string name, SatellitePosition position, int centerX, int startY)
    {
        using var font = new Font("Arial", 7f, FontStyle.Regular);
        using var whiteBrush = new SolidBrush(Color.White);
        using var shadowBrush = new SolidBrush(Color.FromArgb(128, Color.Black));

        var format = new StringFormat { Alignment = StringAlignment.Center };

        // Format coordinates with proper hemisphere indicators
        var latDir = position.Latitude >= 0 ? "N" : "S";
        var lonDir = position.Longitude >= 0 ? "E" : "W";
        var latValue = Math.Abs(position.Latitude);
        var lonValue = Math.Abs(position.Longitude);

        // Truncate name if too long
        var displayName = name.Length > 20 ? name[..17] + "..." : name;

        var lines = new[]
        {
            displayName,
            $"{latValue:F2}{latDir} {lonValue:F2}{lonDir}",
            $"{position.Timestamp:HH:mm} UTC"
        };

        var lineHeight = graphics.MeasureString("A", font).Height;

        for (var i = 0; i < lines.Length; i++)
        {
            var y = startY + (i * lineHeight);

            // Draw shadow offset by 1 pixel
            graphics.DrawString(lines[i], font, shadowBrush, centerX + 1, y + 1, format);
            // Draw main text
            graphics.DrawString(lines[i], font, whiteBrush, centerX, y, format);
        }
    }

    /// <summary>
    /// Draws the orbital trail for a satellite.
    /// </summary>
    private void DrawOrbitTrail(Graphics graphics, List<SatellitePosition> orbitPath, string hexColor)
    {
        if (orbitPath == null || orbitPath.Count < 2)
            return;

        var baseColor = ParseHexColor(hexColor);
        var orbitColor = Color.FromArgb(80, baseColor);

        using var orbitPen = new Pen(orbitColor, 2);
        orbitPen.DashStyle = DashStyle.Dot;

        using var arrowPen = new Pen(Color.FromArgb(120, baseColor), 1);
        using var arrowBrush = new SolidBrush(Color.FromArgb(120, baseColor));

        var points = new List<PointF>();
        foreach (var pos in orbitPath)
        {
            var pixel = GetPixelCoordinates(pos.Latitude, pos.Longitude);
            points.Add(new PointF(pixel.X, pixel.Y));
        }

        for (var i = 1; i < points.Count; i++)
        {
            var p1 = points[i - 1];
            var p2 = points[i];

            // Handle map edge wrapping (don't draw lines across the entire map)
            var deltaX = Math.Abs(p2.X - p1.X);
            if (deltaX < _mapWidth / 2) // Only draw if points are close
            {
                graphics.DrawLine(orbitPen, p1, p2);

                // Draw direction arrows every 10 segments
                if (i % 10 == 0)
                {
                    DrawOrbitDirectionArrow(graphics, arrowPen, arrowBrush, p1, p2);
                }
            }
        }
    }

    /// <summary>
    /// Draws a direction arrow on the orbital path.
    /// </summary>
    private static void DrawOrbitDirectionArrow(Graphics graphics, Pen pen, Brush brush, PointF p1, PointF p2)
    {
        // Calculate direction vector
        var dx = p2.X - p1.X;
        var dy = p2.Y - p1.Y;
        var length = Math.Sqrt(dx * dx + dy * dy);

        if (length < 2) return; // Skip very short segments

        // Normalize direction vector
        var dirX = dx / length;
        var dirY = dy / length;

        // Arrow parameters
        const float arrowSize = 4.0f;
        const float arrowAngle = 0.5f; // radians (~30 degrees)

        // Calculate arrow position (midpoint of segment)
        var arrowX = (p1.X + p2.X) / 2;
        var arrowY = (p1.Y + p2.Y) / 2;

        // Calculate arrow tip
        var tipX = arrowX + dirX * arrowSize;
        var tipY = arrowY + dirY * arrowSize;

        // Calculate arrow wings (perpendicular to direction)
        var perpX = -dirY;
        var perpY = dirX;

        var wingLength = arrowSize * Math.Sin(arrowAngle);
        var backLength = arrowSize * Math.Cos(arrowAngle);

        var leftWingX = arrowX - dirX * backLength + perpX * wingLength;
        var leftWingY = arrowY - dirY * backLength + perpY * wingLength;

        var rightWingX = arrowX - dirX * backLength - perpX * wingLength;
        var rightWingY = arrowY - dirY * backLength - perpY * wingLength;

        // Draw arrow as small triangle
        var arrowPoints = new PointF[]
        {
            new((float)tipX, (float)tipY),
            new((float)leftWingX, (float)leftWingY),
            new((float)rightWingX, (float)rightWingY)
        };

        graphics.FillPolygon(brush, arrowPoints);
        graphics.DrawPolygon(pen, arrowPoints);
    }

    /// <summary>
    /// Parses a hex color string to a Color value.
    /// </summary>
    /// <param name="hexColor">Color in #RRGGBB or #RGB format.</param>
    /// <returns>The parsed color, or white if parsing fails.</returns>
    public static Color ParseHexColor(string hexColor)
    {
        try
        {
            if (string.IsNullOrEmpty(hexColor))
                return Color.White;

            var hex = hexColor.TrimStart('#');

            // Handle short format #RGB
            if (hex.Length == 3)
            {
                hex = $"{hex[0]}{hex[0]}{hex[1]}{hex[1]}{hex[2]}{hex[2]}";
            }

            if (hex.Length != 6)
                return Color.White;

            var r = Convert.ToInt32(hex.Substring(0, 2), 16);
            var g = Convert.ToInt32(hex.Substring(2, 2), 16);
            var b = Convert.ToInt32(hex.Substring(4, 2), 16);

            return Color.FromArgb(r, g, b);
        }
        catch
        {
            return Color.White;
        }
    }

    /// <summary>
    /// Loads a custom satellite icon from a file path.
    /// </summary>
    /// <param name="iconPath">Path to the icon file.</param>
    /// <param name="maxSize">Maximum dimension for the icon (will be scaled if larger).</param>
    /// <returns>The loaded bitmap, or null if loading fails.</returns>
    public static Bitmap? LoadSatelliteIcon(string? iconPath, int maxSize = 48)
    {
        if (string.IsNullOrEmpty(iconPath) || !File.Exists(iconPath))
            return null;

        try
        {
            var original = new Bitmap(iconPath);

            // Scale if necessary
            if (original.Width > maxSize || original.Height > maxSize)
            {
                var scale = Math.Min((double)maxSize / original.Width, (double)maxSize / original.Height);
                var newWidth = (int)(original.Width * scale);
                var newHeight = (int)(original.Height * scale);

                var scaled = new Bitmap(newWidth, newHeight);
                using (var g = Graphics.FromImage(scaled))
                {
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g.SmoothingMode = SmoothingMode.HighQuality;
                    g.DrawImage(original, 0, 0, newWidth, newHeight);
                }

                original.Dispose();
                return scaled;
            }

            return original;
        }
        catch
        {
            return null;
        }
    }
}
