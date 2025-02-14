using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Reflection;

namespace TrayScreenshotApp
{
    public class TrayIconManager : ApplicationContext
    {
        private readonly NotifyIcon _trayIcon;
        private readonly SettingsManager _settingsManager;
        private readonly HotkeyManager _hotkeyManager;
        private readonly ScreenshotManager _screenshotManager;
        private ToolStripMenuItem autoStartMenuItem;
        private ToolStripMenuItem loggingMenuItem;
        private ToolStripMenuItem logLevelMenuItem;
        private ToolStripMenuItem captureModeMenuItem;
        private ToolStripMenuItem viewLogsMenuItem;
        private ToolStripMenuItem viewScreenshotsMenuItem;

        private readonly ILogger<TrayIconManager> _logger;
        private readonly string _appVersion;
        private readonly string _logDirectory = "logs";

        public TrayIconManager(ILogger<TrayIconManager> logger,
                                 SettingsManager settingsManager,
                                 HotkeyManager hotkeyManager,
                                 ScreenshotManager screenshotManager)
        {
            _logger = logger;
            _appVersion = GetAppVersion();

            try
            {
                _logger.LogDebug($"Initializing TrayIconManager - Version {_appVersion}");

                _settingsManager = settingsManager;
                _hotkeyManager = hotkeyManager;
                _screenshotManager = screenshotManager;

                _trayIcon = new NotifyIcon
                {
                    Icon = GetEmbeddedIcon("TrayScreenshotApp.Resources.app_icon.ico"), // ✅ FIXED
                    Visible = true,
                    ContextMenuStrip = CreateContextMenu(),
                    Text = $"Screenshot App v{_appVersion}"
                };

                Application.AddMessageFilter(new HotkeyMessageFilter(_hotkeyManager, _screenshotManager));

                _logger.LogDebug("Application started successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Failed to initialize TrayIconManager.");
                _ = MessageBox.Show("Critical error during startup. See log for details.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Application.Exit();
            }
        }

        private string GetAppVersion()
        {
            var version = Assembly.GetExecutingAssembly().GetName().Version;
            return version != null ? version.ToString() : "Unknown";
        }

        private ContextMenuStrip CreateContextMenu()
        {
            _logger.LogDebug("Creating context menu...");
            var contextMenu = new ContextMenuStrip();

            contextMenu.Items.Add($"App Version: v{_appVersion}").Enabled = false;
            _ = contextMenu.Items.Add(new ToolStripSeparator());

            captureModeMenuItem = new ToolStripMenuItem("Capture Mode");
            AddCaptureModeMenuItems();
            _ = contextMenu.Items.Add(captureModeMenuItem);
            _ = contextMenu.Items.Add(new ToolStripSeparator());
            _ = contextMenu.Items.Add("Change Hotkey", null, (s, e) => ChangeHotkey());
            _ = contextMenu.Items.Add("Change Screenshot Path", null, (s, e) => ChangeScreenshotPath());

            loggingMenuItem = new ToolStripMenuItem("Enable Logging", null, ToggleLogging)
            {
                Checked = _settingsManager.Settings.EnableLogging
            };
            _ = contextMenu.Items.Add(loggingMenuItem);


            logLevelMenuItem = new ToolStripMenuItem("Set Log Level");
            foreach (LogLevel level in Enum.GetValues(typeof(LogLevel)))
            {
                var item = new ToolStripMenuItem(level.ToString(), null, (s, e) => ChangeLogLevel(level))
                {
                    Checked = (_settingsManager.Settings.LogLevel == level)
                };
                _ = logLevelMenuItem.DropDownItems.Add(item);
            }
            _ = contextMenu.Items.Add(logLevelMenuItem);

            autoStartMenuItem = new ToolStripMenuItem("Start with Windows", null, ToggleAutoStart)
            {
                Checked = _settingsManager.Settings.StartWithWindows
            };
            _ = contextMenu.Items.Add(autoStartMenuItem);

            viewLogsMenuItem = new ToolStripMenuItem("View Logs", null, (s, e) => OpenFolder(_logDirectory));
            _ = contextMenu.Items.Add(viewLogsMenuItem);

            viewScreenshotsMenuItem = new ToolStripMenuItem("View Screenshots", null, (s, e) => OpenFolder(_settingsManager.Settings.ScreenshotPath));
            _ = contextMenu.Items.Add(viewScreenshotsMenuItem);


            _ = contextMenu.Items.Add(new ToolStripSeparator());
            _ = contextMenu.Items.Add("Exit", null, (s, e) => ExitApplication());

            return contextMenu;
        }

        private Icon GetEmbeddedIcon(string resourceName)
        {
            try
            {
                _logger.LogDebug($"Loading embedded icon: {resourceName}");
                var assembly = Assembly.GetExecutingAssembly();
                using var stream = assembly.GetManifestResourceStream(resourceName);
                if (stream == null)
                {
                    _logger.LogWarning($"Resource '{resourceName}' not found. Using default icon.");
                    return SystemIcons.Application;
                }

                return new Icon(stream);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load embedded icon.");
                return SystemIcons.Application;
            }
        }

        private void ChangeHotkey()
        {
            _hotkeyManager.ChangeHotkey();
        }

        private void ChangeScreenshotPath()
        {
            _hotkeyManager.ChangeScreenshotPath();
        }

        private void ToggleAutoStart(object sender, EventArgs e)
        {
            bool enableAutoStart = !_settingsManager.Settings.StartWithWindows;
            _settingsManager.UpdateAutoStart(enableAutoStart);
            autoStartMenuItem.Checked = enableAutoStart;
            _logger.LogDebug($"Auto-start option changed: {enableAutoStart}");
        }

        private void ToggleLogging(object sender, EventArgs e)
        {
            bool enableLogging = !_settingsManager.Settings.EnableLogging;
            _settingsManager.UpdateLogging(enableLogging);
            loggingMenuItem.Checked = enableLogging;
            _logger.LogDebug($"Logging option changed: {enableLogging}");
        }

        private void ChangeLogLevel(LogLevel level)
        {
            _settingsManager.UpdateLogLevel(level);
            foreach (ToolStripMenuItem item in logLevelMenuItem.DropDownItems)
            {
                item.Checked = (item.Text == level.ToString());
            }
        }

        private void AddCaptureModeMenuItems()
        {
            var captureModes = new (CaptureMode Mode, string DisplayName)[]
            {
                (CaptureMode.ActiveScreen, "Active Screen"),
                (CaptureMode.ActiveWindow, "Active Window"),
                (CaptureMode.VirtualDesktop, "Virtual Desktop")
            };

            captureModeMenuItem.DropDownItems.Clear();

            foreach (var (mode, displayName) in captureModes)
            {
                var item = new ToolStripMenuItem(displayName, null, (s, e) => ChangeCaptureMode(mode))
                {
                    Checked = (_settingsManager.Settings.CaptureMode == mode)
                };
                _ = captureModeMenuItem.DropDownItems.Add(item);
            }
        }

        private void ChangeCaptureMode(CaptureMode mode)
        {
            _settingsManager.UpdateCaptureMode(mode);
            foreach (ToolStripMenuItem item in captureModeMenuItem.DropDownItems)
            {
                item.Checked = (item.Text == GetCaptureModeDisplayName(mode));
            }
        }

        private string GetCaptureModeDisplayName(CaptureMode mode)
        {
            return mode switch
            {
                CaptureMode.ActiveScreen => "Active Screen",
                CaptureMode.ActiveWindow => "Active Window",
                CaptureMode.VirtualDesktop => "Virtual Desktop",
                _ => "Unknown"
            };
        }

        private void OpenFolder(string folderPath)
        {
            try
            {
                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                    _logger.LogInformation("Created folder: {Path}", folderPath);
                }

                Process.Start(new ProcessStartInfo { FileName = folderPath, UseShellExecute = true });
                _logger.LogDebug("Opened folder: {Path}", folderPath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to open folder: {Path}", folderPath);
                MessageBox.Show($"Failed to open folder:\n{folderPath}\nError: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ExitApplication()
        {
            _trayIcon.Visible = false;
            Application.Exit();
        }
    }
}
