using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using WorldMapWallpaper.Shared;

namespace WorldMapWallpaper;

/// <summary>
/// Handles resolution scaling for wallpaper images.
/// Provides high-quality scaling using bicubic interpolation.
/// </summary>
internal static class ResolutionScaler
{
    /// <summary>
    /// Default source image dimensions.
    /// </summary>
    private const int SourceWidth = 1920;
    private const int SourceHeight = 1080;

    /// <summary>
    /// Gets the target resolution based on settings.
    /// Uses custom dimensions if specified, otherwise auto-detects from primary screen.
    /// </summary>
    /// <returns>Target width and height.</returns>
    public static (int Width, int Height) GetTargetResolution()
    {
        var customWidth = Settings.CustomResolutionWidth;
        var customHeight = Settings.CustomResolutionHeight;

        // If custom dimensions are specified (both > 0), use them
        if (customWidth > 0 && customHeight > 0)
        {
            return (customWidth, customHeight);
        }

        // Otherwise, auto-detect from primary screen
        return GetPrimaryScreenResolution();
    }

    /// <summary>
    /// Gets the primary monitor resolution using Windows API.
    /// </summary>
    /// <returns>Primary screen width and height, or default 1920x1080 if detection fails.</returns>
    public static (int Width, int Height) GetPrimaryScreenResolution()
    {
        try
        {
            var width = NativeMethods.GetSystemMetrics(NativeMethods.SM_CXSCREEN);
            var height = NativeMethods.GetSystemMetrics(NativeMethods.SM_CYSCREEN);

            if (width > 0 && height > 0)
            {
                return (width, height);
            }
        }
        catch
        {
            // Fallback to default on any error
        }

        // Fallback to source resolution
        return (SourceWidth, SourceHeight);
    }

    /// <summary>
    /// Scales the image according to the current resolution settings.
    /// This should be called as the LAST step before saving the wallpaper.
    /// </summary>
    /// <param name="source">The source bitmap to scale.</param>
    /// <param name="log">Logger for debug output.</param>
    /// <returns>Scaled bitmap, or the original bitmap if no scaling is needed.</returns>
    public static Bitmap ScaleImage(Bitmap source, Logger log)
    {
        var mode = Settings.ResolutionMode;

        if (mode == ResolutionMode.None)
        {
            log.Debug("Resolution mode is None, keeping original 1920x1080.");
            return source;
        }

        var (targetWidth, targetHeight) = GetTargetResolution();

        // If target matches source, no scaling needed
        if (targetWidth == source.Width && targetHeight == source.Height)
        {
            log.Debug($"Target resolution {targetWidth}x{targetHeight} matches source, no scaling needed.");
            return source;
        }

        log.Info($"Scaling image from {source.Width}x{source.Height} to {targetWidth}x{targetHeight} using {mode} mode.");

        return mode switch
        {
            ResolutionMode.Fit => ScaleFit(source, targetWidth, targetHeight, log),
            ResolutionMode.Stretch => ScaleStretch(source, targetWidth, targetHeight, log),
            _ => source
        };
    }

    /// <summary>
    /// Scales image to fit within bounds, preserving aspect ratio.
    /// Adds letterboxing (black bars top/bottom) or pillarboxing (black bars left/right) as needed.
    /// </summary>
    /// <param name="source">The source bitmap.</param>
    /// <param name="targetWidth">Target width in pixels.</param>
    /// <param name="targetHeight">Target height in pixels.</param>
    /// <param name="log">Logger for debug output.</param>
    /// <returns>A new scaled bitmap with letterboxing/pillarboxing if needed.</returns>
    private static Bitmap ScaleFit(Bitmap source, int targetWidth, int targetHeight, Logger log)
    {
        // Calculate aspect ratios
        var sourceRatio = (double)source.Width / source.Height;
        var targetRatio = (double)targetWidth / targetHeight;

        int scaledWidth, scaledHeight;
        int offsetX, offsetY;

        if (sourceRatio > targetRatio)
        {
            // Source is wider than target - fit to width, add letterboxing (top/bottom bars)
            scaledWidth = targetWidth;
            scaledHeight = (int)(targetWidth / sourceRatio);
            offsetX = 0;
            offsetY = (targetHeight - scaledHeight) / 2;
            log.Debug($"Letterboxing: scaled to {scaledWidth}x{scaledHeight}, offset Y={offsetY}");
        }
        else
        {
            // Source is taller than target - fit to height, add pillarboxing (left/right bars)
            scaledHeight = targetHeight;
            scaledWidth = (int)(targetHeight * sourceRatio);
            offsetX = (targetWidth - scaledWidth) / 2;
            offsetY = 0;
            log.Debug($"Pillarboxing: scaled to {scaledWidth}x{scaledHeight}, offset X={offsetX}");
        }

        // Create target bitmap with black background
        var result = new Bitmap(targetWidth, targetHeight, PixelFormat.Format32bppArgb);

        using (var graphics = Graphics.FromImage(result))
        {
            // Set high-quality rendering
            ConfigureHighQualityGraphics(graphics);

            // Fill background with black for letterbox/pillarbox areas
            graphics.Clear(Color.Black);

            // Draw scaled image centered
            graphics.DrawImage(source,
                new Rectangle(offsetX, offsetY, scaledWidth, scaledHeight),
                new Rectangle(0, 0, source.Width, source.Height),
                GraphicsUnit.Pixel);
        }

        return result;
    }

    /// <summary>
    /// Scales image to exactly fill target dimensions.
    /// May distort aspect ratio if source and target ratios differ.
    /// </summary>
    /// <param name="source">The source bitmap.</param>
    /// <param name="targetWidth">Target width in pixels.</param>
    /// <param name="targetHeight">Target height in pixels.</param>
    /// <param name="log">Logger for debug output.</param>
    /// <returns>A new stretched bitmap.</returns>
    private static Bitmap ScaleStretch(Bitmap source, int targetWidth, int targetHeight, Logger log)
    {
        var result = new Bitmap(targetWidth, targetHeight, PixelFormat.Format32bppArgb);

        using (var graphics = Graphics.FromImage(result))
        {
            // Set high-quality rendering
            ConfigureHighQualityGraphics(graphics);

            // Stretch to fill entire target
            graphics.DrawImage(source,
                new Rectangle(0, 0, targetWidth, targetHeight),
                new Rectangle(0, 0, source.Width, source.Height),
                GraphicsUnit.Pixel);
        }

        log.Debug($"Stretched image to {targetWidth}x{targetHeight}");
        return result;
    }

    /// <summary>
    /// Configures graphics object for high-quality rendering.
    /// Uses bicubic interpolation and other quality settings for best results.
    /// </summary>
    /// <param name="graphics">The graphics object to configure.</param>
    private static void ConfigureHighQualityGraphics(Graphics graphics)
    {
        graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
        graphics.SmoothingMode = SmoothingMode.HighQuality;
        graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
        graphics.CompositingQuality = CompositingQuality.HighQuality;
    }
}
