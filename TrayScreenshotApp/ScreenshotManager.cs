using Microsoft.Extensions.Logging;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace TrayScreenshotApp
{
    public class ScreenshotManager
    {
        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hwnd, out RECT lpRect);

        private readonly SettingsManager _settingsManager;
        private readonly ILogger<ScreenshotManager> _logger;

        private int hotkeyCounter = 0;

        public ScreenshotManager(SettingsManager settingsManager, ILogger<ScreenshotManager> logger)
        {
            _settingsManager = settingsManager ?? throw new ArgumentNullException(nameof(settingsManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task TakeScreenShotAsync()
        {
            Interlocked.Increment(ref hotkeyCounter);
            _logger.LogDebug("Hotkey pressed {Count} times", hotkeyCounter);

            try
            {
                var settings = _settingsManager.Settings ?? throw new InvalidOperationException("Settings cannot be null");
                var captureMode = settings.CaptureMode;
                _logger.LogDebug($"Capture mode: {captureMode}");

                EnsureScreenshotFolderExists();

                Bitmap finalBitmap = captureMode switch
                {
                    CaptureMode.VirtualDesktop => await Task.Run(() => CaptureVirtualDesktop()),
                    CaptureMode.ActiveWindow => await Task.Run(() => CaptureActiveWindow()),
                    _ => await Task.Run(() => CaptureActiveScreen())
                };

                string screenshotPath = settings.ScreenshotPath ?? throw new InvalidOperationException("Screenshot path cannot be null");
                string filePath = Path.Combine(screenshotPath, $"Screenshot_{DateTime.Now:yyyy-MM-dd_HH-mm-ss-fff}.png");

                _logger.LogDebug($"Saving screenshot to: {filePath}");
                await SaveScreenshotAsync(finalBitmap, filePath);
                _logger.LogDebug($"Screenshot saved successfully: {filePath}");

                finalBitmap.Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error capturing screenshot.");
            }
        }

        private async Task SaveScreenshotAsync(Bitmap bitmap, string filePath)
        {
            await Task.Run(() =>
            {
                try
                {
                    bitmap.Save(filePath, ImageFormat.Png);
                    _logger.LogDebug("Screenshot saved: {0}", filePath);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error saving screenshot.");
                }
            });
        }

        private Bitmap CaptureActiveScreen()
        {
            _logger.LogDebug("Capturing active screen...");

            Screen? activeScreen = Screen.PrimaryScreen;
            if (activeScreen == null)
            {
                _logger.LogWarning("Primary screen not found. Returning empty bitmap.");
                return new Bitmap(1, 1);
            }

            Rectangle bounds = activeScreen.Bounds;
            Bitmap bitmap = new(bounds.Width, bounds.Height);
            using (Graphics g = Graphics.FromImage(bitmap))
            {
                g.CopyFromScreen(bounds.Location, Point.Empty, bounds.Size);
            }

            return bitmap;
        }

        private Bitmap CaptureVirtualDesktop()
        {
            _logger.LogDebug("Capturing virtual desktop...");

            var screens = Screen.AllScreens;
            if (screens == null || screens.Length == 0)
            {
                _logger.LogWarning("No screens found. Returning empty bitmap.");
                return new Bitmap(1, 1);
            }

            int minX = int.MaxValue, minY = int.MaxValue;
            int maxX = int.MinValue, maxY = int.MinValue;

            foreach (var screen in screens)
            {
                minX = Math.Min(minX, screen.Bounds.X);
                minY = Math.Min(minY, screen.Bounds.Y);
                maxX = Math.Max(maxX, screen.Bounds.X + screen.Bounds.Width);
                maxY = Math.Max(maxY, screen.Bounds.Y + screen.Bounds.Height);
            }

            int width = maxX - minX;
            int height = maxY - minY;

            _logger.LogDebug($"Virtual desktop dimensions: Width={width}, Height={height}");

            Bitmap bitmap = new(width, height);
            using (Graphics g = Graphics.FromImage(bitmap))
            {
                g.CopyFromScreen(new Point(minX, minY), Point.Empty, new Size(width, height));
            }

            return bitmap;
        }

        private Bitmap CaptureActiveWindow()
        {
            _logger.LogDebug("Capturing active window...");

            IntPtr hwnd = GetForegroundWindow();
            if (hwnd == IntPtr.Zero)
            {
                _logger.LogWarning("No active window detected. Capturing active screen instead.");
                return CaptureActiveScreen();
            }

            if (!GetWindowRect(hwnd, out RECT rect))
            {
                _logger.LogWarning("GetWindowRect failed. Returning empty bitmap.");
                return new Bitmap(1, 1);
            }

            int width = rect.Right - rect.Left;
            int height = rect.Bottom - rect.Top;

            if (width <= 0 || height <= 0)
            {
                _logger.LogWarning("Invalid active window dimensions.");
                return new Bitmap(1, 1);
            }

            _logger.LogDebug($"Active window dimensions: Width={width}, Height={height}");

            Bitmap bitmap = new(width, height);
            using (Graphics g = Graphics.FromImage(bitmap))
            {
                g.CopyFromScreen(new Point(rect.Left, rect.Top), Point.Empty, new Size(width, height));
            }

            return bitmap;
        }

        private void EnsureScreenshotFolderExists()
        {
            string? screenshotFolder = _settingsManager.Settings?.ScreenshotPath;

            if (string.IsNullOrWhiteSpace(screenshotFolder))
            {
                throw new InvalidOperationException("Screenshot path is null or empty.");
            }

            if (!Directory.Exists(screenshotFolder))
            {
                Directory.CreateDirectory(screenshotFolder);
                _logger.LogDebug("Screenshot folder created: {0}", screenshotFolder);
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left, Top, Right, Bottom;
        }
    }
}
