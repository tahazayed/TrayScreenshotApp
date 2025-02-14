using Microsoft.Extensions.Logging;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace TrayScreenshotApp
{
    public class ScreenshotManager
    {
        [DllImport("gdi32.dll")]
        private static extern bool BitBlt(IntPtr hdcDest, int xDest, int yDest, int wDest, int hDest, IntPtr hdcSource, int xSrc, int ySrc, CopyPixelOperation rop);

        [DllImport("user32.dll")]
        private static extern IntPtr GetDC(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hwnd, out RECT lpRect);

        private readonly SettingsManager _settingsManager;
        private readonly ILogger<ScreenshotManager> _logger;

        private int hotkeyCounter = 0;

        public ScreenshotManager(SettingsManager settingsManager, ILogger<ScreenshotManager> logger)
        {
            _settingsManager = settingsManager;
            _logger = logger;
        }

        /// <summary>
        /// Captures the selected screenshot mode asynchronously.
        /// </summary>
        public async Task TakeScreenShotAsync()
        {
            Interlocked.Increment(ref hotkeyCounter);
            _logger.LogDebug("Hotkey pressed {Count} times", hotkeyCounter);

            try
            {
                CaptureMode captureMode = _settingsManager.Settings.CaptureMode;
                _logger.LogDebug($"Capture mode: {captureMode}");

                EnsureScreenshotFolderExists();

                Bitmap finalBitmap = captureMode switch
                {
                    CaptureMode.VirtualDesktop => await Task.Run(() => CaptureVirtualDesktop()),
                    CaptureMode.ActiveWindow => await Task.Run(() => CaptureActiveWindow()),
                    _ => await Task.Run(() => CaptureActiveScreen())
                };

                string filePath = Path.Combine(_settingsManager.Settings.ScreenshotPath, $"Screenshot_{DateTime.Now:yyyy-MM-dd_HH-mm-ss-fff}.png");
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

            Screen activeScreen = Screen.PrimaryScreen;
            Bitmap bitmap = new(activeScreen.Bounds.Width, activeScreen.Bounds.Height);

            using (Graphics g = Graphics.FromImage(bitmap))
            {
                IntPtr desktopDC = GetDC(IntPtr.Zero);
                IntPtr bitmapDC = g.GetHdc();

                if (!BitBlt(bitmapDC, 0, 0, activeScreen.Bounds.Width, activeScreen.Bounds.Height, desktopDC, activeScreen.Bounds.X, activeScreen.Bounds.Y, CopyPixelOperation.SourceCopy))
                {
                    _logger.LogError("BitBlt operation failed for active screen.");
                }

                g.ReleaseHdc(bitmapDC);
                ReleaseDC(IntPtr.Zero, desktopDC);
            }

            return bitmap;
        }

        private Bitmap CaptureVirtualDesktop()
        {
            _logger.LogDebug("Capturing virtual desktop...");

            int minX = int.MaxValue, minY = int.MaxValue;
            int maxX = int.MinValue, maxY = int.MinValue;

            foreach (var screen in Screen.AllScreens)
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
                IntPtr desktopDC = GetDC(IntPtr.Zero);
                IntPtr bitmapDC = g.GetHdc();

                if (!BitBlt(bitmapDC, 0, 0, width, height, desktopDC, minX, minY, CopyPixelOperation.SourceCopy))
                {
                    _logger.LogError("BitBlt operation failed for virtual desktop.");
                }

                g.ReleaseHdc(bitmapDC);
                ReleaseDC(IntPtr.Zero, desktopDC);
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

            GetWindowRect(hwnd, out RECT rect);
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
                IntPtr desktopDC = GetDC(IntPtr.Zero);
                IntPtr bitmapDC = g.GetHdc();

                if (!BitBlt(bitmapDC, 0, 0, width, height, desktopDC, rect.Left, rect.Top, CopyPixelOperation.SourceCopy))
                {
                    _logger.LogError("BitBlt operation failed for active window.");
                }

                g.ReleaseHdc(bitmapDC);
                ReleaseDC(IntPtr.Zero, desktopDC);
            }

            return bitmap;
        }

        private void EnsureScreenshotFolderExists()
        {
            string screenshotFolder = _settingsManager.Settings.ScreenshotPath;

            if (!Directory.Exists(screenshotFolder))
            {
                Directory.CreateDirectory(screenshotFolder);
                _logger.LogDebug("Screenshot folder created: {0}", screenshotFolder);
            }
        }

        private struct RECT
        {
            public int Left, Top, Right, Bottom;
        }
    }
}