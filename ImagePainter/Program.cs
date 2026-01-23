using Microsoft.Win32;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Text;
using WorldMapWallpaper.Properties;
using WorldMapWallpaper.Shared;

namespace WorldMapWallpaper
{
    /// <summary>
    /// The main class of the WorldMapWallpaper application.<br/>
    /// Generates dynamic wallpapers showing day/night terminator lines and world clocks.
    /// </summary>
    internal class Program
    {

        /// <summary>
        /// Logger instance for recording application events and debugging information.
        /// </summary>
        static readonly Logger log = new();

        /// <summary>
        /// Calculates the terminator latitude for a given longitude and solar parameters.
        /// The terminator is the line that separates the illuminated day side and dark night side of Earth.
        /// </summary>
        /// <param name="longitude">Longitude in degrees (-180 to 180).</param>
        /// <param name="timeOffset">Time offset for terminator calculation in degrees.</param>
        /// <param name="declination">Solar declination angle in degrees.</param>
        /// <returns>Terminator latitude in degrees.</returns>
        /// <remarks>
        /// This calculation is based on the spherical geometry of Earth and the position of the sun.
        /// The formula uses trigonometric functions to determine where the terminator line intersects
        /// a given longitude.
        /// </remarks>
        public static double GetTerminatorLatitude(double longitude, double timeOffset, double declination)
        {
            var adjustedLongitude = longitude + timeOffset;
            var tanLat = Trig.Cos(adjustedLongitude) / Trig.Tan(declination);
            return Trig.Atan(tanLat);
        }

        /// <summary>
        /// Gets the next wallpaper filename, alternating between WorldMap01.jpg and WorldMap02.jpg
        /// to avoid file locking issues.
        /// </summary>
        /// <returns>The full path to the next wallpaper file.</returns>
        private static string GetNextWallpaperFileName()
        {
            log.Info("Getting the current desktop wallpaper name.");
            var sbWPFN = new StringBuilder(256);
            NativeMethods.SystemParametersInfo(SPI.SPI_GETDESKWALLPAPER, 256, sbWPFN, SPIF.None);
            var wpfn = sbWPFN.ToString();
            log.Debug($"The current desktop wallpaper name is \"{wpfn ?? "null"}\".");

            var fileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), "WorldMap01.jpg");
            if (string.Compare(wpfn, fileName, true) == 0)
            {
                fileName = wpfn.EndsWith("01.jpg", StringComparison.OrdinalIgnoreCase)
                         ? wpfn.Replace("01.jpg", "02.jpg")
                         : wpfn.Replace("02.jpg", "01.jpg");
            }

            return fileName;
        }

        /// <summary>
        /// Calculates solar parameters for the current UTC time.
        /// </summary>
        /// <returns>A tuple containing time offset and declination values.</returns>
        private static (double TimeOffset, double Declination) CalculateSolarParameters()
        {
            var NOW = DateTime.UtcNow;
            var TimeSeconds = NOW.Hour * 3600 + NOW.Minute * 60 + NOW.Second;
            
            // Since the world's borders are at longitude +-180 degrees but we are
            // comparing to UTC time (which takes place at longitude 0 degrees),
            // we have to shift the time by exactly 12 hours using NOON_SECS.
            var NoonSeconds = 86400 / 2;
            
            // We calculate the horizontal offset on the basis of seconds. Therefore we
            // divide the maximum offset (360) by the amount of seconds in a day.
            var AngleStep = (double)360 / 86400;
            
            // Now let's add everything together... the offset is now in the
            // range of [0, 360)
            var TimeOffset = (TimeSeconds + NoonSeconds) * AngleStep;

            // Calculate the sun's position throughout the year using vernal equinox
            var DayOfYear = NOW.DayOfYear;
            var VE2000 = new DateTime(2000, 03, 20, 07, 36, 0);
            var VE = VE2000.AddDays((double)(NOW.Year - 2000) * 365.2425);
            var VernalEquinox = VE.DayOfYear;

            log.Debug($"The current UTC time is {NOW}.");
            log.Debug($"Calculated Vernal Equinox is {VE}.");

            var MaxDeclination = 23.44;
            var Declination = Trig.Sin(360.0 * (DayOfYear - VernalEquinox) / 365) * MaxDeclination;

            return (TimeOffset, Declination);
        }

        /// <summary>
        /// Generates the terminator curve points for day/night boundary.
        /// </summary>
        /// <param name="timeOffset">Time offset for calculation.</param>
        /// <param name="declination">Solar declination.</param>
        /// <param name="imageWidth">Image width.</param>
        /// <param name="imageHeight">Image height.</param>
        /// <returns>List of points defining the terminator curve.</returns>
        private static List<PointF> GenerateTerminatorCurve(double timeOffset, double declination, int imageWidth, int imageHeight)
        {
            log.Info("Calculating the points of the terminator curve.");
            
            var pts = new List<PointF>(
            [
                new Point(imageWidth, 0),
                new Point(0, 0)
            ]);

            var y0 = (float)imageHeight / 2;
            var x0 = (float)imageWidth / 2;
            var xs = (float)imageWidth / 360;
            var ys = (float)imageHeight / 180;
            
            for (var a = -180; a <= 180; a++)
            {
                var arctanLat = GetTerminatorLatitude(a, timeOffset, declination);
                var y = y0 + (float)(arctanLat * ys);
                var x = x0 + (a * xs);
                pts.Add(new PointF(x, y));
            }
            pts.Add(new Point(imageWidth, 0));

            return pts;
        }

        /// <summary>
        /// Creates the alpha mask for blending day and night regions.
        /// </summary>
        /// <param name="terminatorPoints">Points defining the terminator curve.</param>
        /// <param name="declination">Solar declination.</param>
        /// <param name="imageWidth">Image width.</param>
        /// <param name="imageHeight">Image height.</param>
        /// <returns>Alpha mask bitmap.</returns>
        private static Bitmap CreateAlphaMask(List<PointF> terminatorPoints, double declination, int imageWidth, int imageHeight)
        {
            log.Info("Generating the alpha mask.");
            var bmpAlphaMask = new Bitmap(imageWidth, imageHeight);
            using (var g = Graphics.FromImage(bmpAlphaMask))
            {
                g.FillRectangle((declination >= 0) ? Brushes.Black : Brushes.White, new RectangleF(0, 0, imageWidth, imageHeight));
                g.FillClosedCurve((declination >= 0) ? Brushes.White : Brushes.Black, terminatorPoints.ToArray());
            }
            return GaussianBlur.Apply(bmpAlphaMask, 16);
        }

        /// <summary>
        /// Draws timezone clocks on the image.
        /// </summary>
        /// <param name="graphics">Graphics context.</param>
        /// <param name="clockImage">Clock face image.</param>
        /// <param name="currentTime">Current UTC time.</param>
        /// <param name="imageWidth">Image width.</param>
        /// <param name="imageHeight">Image height.</param>
        private static void DrawTimezonClocks(Graphics graphics, Bitmap clockImage, DateTime currentTime, int imageWidth, int imageHeight)
        {
            log.Debug("Drawing the clock faces.");
            var tz = (float)imageWidth / 24;
            var dx = tz / 2;
            var x0 = (float)imageWidth / 2;
            
            for (var a = -12; a <= 12; a++)
            {
                var x = x0 + (float)(a * tz);

                // Draw the clock line
                using (var p = new Pen(Color.FromArgb(6, 255, 255, 255), 1))
                    graphics.DrawLine(p, x - dx, 0, x - dx, imageHeight);

                // Draw clock face
                graphics.DrawImage(clockImage, new RectangleF(x - clockImage.Width / 2, 5, clockImage.Width, clockImage.Height), 
                                 new RectangleF(0, 0, clockImage.Width, clockImage.Height), GraphicsUnit.Pixel);

                // Draw clock hands
                var y = (float)(5 + (clockImage.Height / 2));
                var handLength = (float)(clockImage.Width / 3);
                var centerPoint = new PointF(x, y);
                const int alphaHand = 64;

                var hc = currentTime.AddHours(a);
                var ah = 30 * ((hc.Minute / 360) + hc.Hour < 12 ? hc.Hour : hc.Hour - 12) - 90;
                var xh = (float)(x + handLength * Trig.Cos(ah));
                var yh = (float)((5 + (clockImage.Height / 2)) + handLength * Trig.Sin(ah));

                var am = 6 * hc.Minute - 90;
                var xm = (float)(x + handLength * Trig.Cos(am));
                var ym = (float)((5 + (clockImage.Height / 2)) + handLength * Trig.Sin(am));

                graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

                using (var p = new Pen(Color.FromArgb(alphaHand, Color.Red), 3))
                    graphics.DrawLine(p, centerPoint, new PointF(xh, yh));

                using (var p = new Pen(Color.FromArgb(alphaHand, Color.Red), 2))
                    graphics.DrawLine(p, centerPoint, new PointF(xm, ym));
            }
        }

        /// <summary>
        /// Sets the generated image as desktop wallpaper and cleans up the previous file.
        /// </summary>
        /// <param name="fileName">Path to the wallpaper file.</param>
        /// <param name="previousWallpaper">Path to the previous wallpaper file.</param>
        private static void SetDesktopWallpaper(string fileName, string? previousWallpaper)
        {
            var timeOut = false;
            log.Debug("Waiting for the file to be saved.");
            var startTime = DateTime.UtcNow;
            while (!File.Exists(fileName))
            {
                var elapsed = DateTime.UtcNow - startTime;
                if (elapsed.TotalSeconds > 5)
                {
                    timeOut = true;
                    break;
                }
                Thread.Sleep(1);
            }

            if (!timeOut)
            {
                log.Info("Setting the desktop wallpaper.");
                NativeMethods.SystemParametersInfo(SPI.SPI_SETDESKWALLPAPER, 0, fileName, SPIF.UpdateIniFile | SPIF.SendChange);

                // Refresh the desktop to ensure wallpaper updates immediately
                log.Debug("Refreshing the desktop.");
                Registry.CurrentUser.OpenSubKey(@"Control Panel\Desktop", true)?.Close();

                // Delete the previous wallpaper file
                try
                {
                    if (!string.IsNullOrEmpty(previousWallpaper) && File.Exists(previousWallpaper))
                    {
                        log.Info($"Deleting the previous wallpaper file \"{previousWallpaper}\".");
                        File.Delete(previousWallpaper);
                    }
                }
                catch { }
            }
        }

        /// <summary>
        /// The main entry point for the WorldMapWallpaper application.
        /// Generates a dynamic wallpaper showing the current day/night terminator line
        /// and world time zone clocks, then sets it as the desktop wallpaper.
        /// </summary>
        static void Main(string[] args)
        {
            log.Info("Starting the WorldMapWallpaper application.");

            // Check for command line arguments
            if (args.Length > 0 && args[0] == "--settings")
            {
                log.Info("Launching settings application.");
                try
                {
                    var settingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "WorldMapWallpaper.Settings.exe");
                    if (File.Exists(settingsPath))
                    {
                        var startInfo = new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = settingsPath,
                            UseShellExecute = true
                        };
                        System.Diagnostics.Process.Start(startInfo);
                    }
                    else
                    {
                        log.Info($"Settings application not found at: {settingsPath}");
                    }
                }
                catch (Exception ex)
                {
                    log.Info($"Failed to launch settings: {ex.Message}");
                }
                return;
            }

            try
            {
                // Log current settings
                log.Info($"Current settings - ISS: {Settings.ShowISS}, TimeZones: {Settings.ShowTimeZones}, PoliticalMap: {Settings.ShowPoliticalMap}, UpdateInterval: {Settings.UpdateInterval}");

                // Load resources
                var map = Resources.WorldPoliticalMap;
                var day = Resources.EarthDay;
                var night = Resources.EarthNight;
                var clock = Resources.ClockFace32;

                // Get wallpaper filename and remember current one for cleanup
                var currentWallpaper = Shared.WallpaperMonitor.GetCurrentWallpaperPath();
                var fileName = GetNextWallpaperFileName();

                // Calculate solar parameters
                var (timeOffset, declination) = CalculateSolarParameters();
                var currentTime = DateTime.UtcNow;

                // Generate terminator curve
                var terminatorPoints = GenerateTerminatorCurve(timeOffset, declination, day.Width, day.Height);

                // Create alpha mask for day/night blending
                var alphaMask = CreateAlphaMask(terminatorPoints, declination, day.Width, day.Height);

                // Composite the final image
                log.Info("Drawing the image.");
                using (var g = Graphics.FromImage(night))
                {
                    // Apply day/night blending
                    var dayImageCopy = (Bitmap)day.Clone();
                    log.Debug("Apply the alpha mask to the day image.");
                    SetAlphaMask(dayImageCopy, alphaMask);
                    log.Debug("Drawing the day image over the night image.");
                    g.DrawImage(dayImageCopy, 0, 0);

                    // Draw political map overlay (if enabled)
                    if (Settings.ShowPoliticalMap)
                    {
                        log.Debug("Drawing the political map.");
                        g.DrawImage(map, 0, 0, day.Width, day.Height);
                    }
                    else
                    {
                        log.Debug("Political map disabled by user settings.");
                    }

                    // Draw timezone clocks (if enabled)
                    if (Settings.ShowTimeZones)
                    {
                        log.Debug("Drawing timezone clocks.");
                        DrawTimezonClocks(g, clock, currentTime, day.Width, day.Height);
                    }
                    else
                    {
                        log.Debug("Timezone clocks disabled by user settings.");
                    }
                }

                // Add ISS tracking (if enabled)
                Bitmap finalImage;
                if (Settings.ShowISS)
                {
                    log.Debug("Adding ISS tracking to wallpaper.");
                    var issTracker = new ISSTracker(log, timeOffset, declination);
                    finalImage = issTracker.PlotISS(night);
                }
                else
                {
                    log.Debug("ISS tracking disabled by user settings.");
                    finalImage = night;
                }

                // Apply resolution scaling (LAST step before saving)
                finalImage = ResolutionScaler.ScaleImage(finalImage, log);

                // Save and set wallpaper
                log.Info($"Saving the new wallpaper to \"{fileName}\".");
                finalImage.Save(fileName);

                SetDesktopWallpaper(fileName, currentWallpaper);

                log.Info("WorldMapWallpaper application completed successfully.");
            }
            catch (Exception ex)
            {
                log.Info($"Error in WorldMapWallpaper application: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Applies the specified alpha mask to the specified image.
        /// This method modifies the alpha channel of the target image based on the grayscale values
        /// of the alpha mask, enabling smooth blending between day and night regions.
        /// </summary>
        /// <param name="image">The image to apply the alpha mask to. Must be in 32bpp ARGB format.</param>
        /// <param name="alphaMask">The alpha mask to apply. Must be a grayscale image of the same size as the target image.</param>
        /// <remarks>
        /// The alpha mask must be a grayscale image where:
        /// <list type="bullet">
        /// <item><description>White pixels (255) result in full opacity (255 alpha)</description></item>
        /// <item><description>Black pixels (0) result in full transparency (0 alpha)</description></item>
        /// <item><description>Gray pixels result in partial transparency</description></item>
        /// </list>
        /// <br/>
        /// This method uses unsafe code for performance optimization when processing large images.
        /// <br/>
        /// Adapted from <i>(accessed 2023-08-23)</i>:<br/>
        /// <a href="https://danbystrom.se/2008/08/24/soft-edged-images-in-gdi/">https://danbystrom.se/2008/08/24/soft-edged-images-in-gdi/</a><br/>
        /// </remarks>
        /// <exception cref="ArgumentNullException">Thrown when either <paramref name="image"/> or <paramref name="alphaMask"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown when the image and alpha mask have different dimensions.</exception>
        public static void SetAlphaMask(Bitmap image, Bitmap alphaMask)
        {
            ArgumentNullException.ThrowIfNull(image, nameof(image));
            ArgumentNullException.ThrowIfNull(alphaMask, nameof(alphaMask));
            if (image.Size != alphaMask.Size)
                throw new ArgumentException("The image and alpha mask must be the same size.");

            var r = new Rectangle(Point.Empty, image.Size);
            var bdDst = image.LockBits(r, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            var bdSrc = alphaMask.LockBits(r, ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
            unsafe
            {
                var bpSrc = (byte*)bdSrc.Scan0.ToPointer();
                var bpDst = (byte*)bdDst.Scan0.ToPointer() + 3; // 3 is the alpha channel | 0 - Blue | 1 - Green | 2 - Red | 3 - Alpha |
                for (var i = r.Height * r.Width; i > 0; i--)
                {
                    *bpDst = *bpSrc;
                    bpSrc += 4;
                    bpDst += 4;
                }
            }
            image.UnlockBits(bdSrc);
            alphaMask.UnlockBits(bdDst);
        }
    }
}