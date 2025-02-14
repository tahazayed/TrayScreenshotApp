using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using System.Text.Json;

namespace TrayScreenshotApp
{
    public class SettingsManager
    {
        private readonly string settingsFilePath = "settings.json";
        private const string RegistryKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string AppName = "TrayScreenshotApp";

        public HotkeySettings Settings { get; private set; }
        private readonly ILogger<SettingsManager> _logger;
        private readonly Action<LogLevel> _logLevelUpdater; // ✅ Log level update callback

        public SettingsManager(ILogger<SettingsManager> logger, Action<LogLevel> logLevelUpdater)
        {
            _logger = logger;
            _logLevelUpdater = logLevelUpdater; // ✅ Store callback function
            LoadSettings();
        }

        /// <summary>
        /// Loads settings from settings.json or creates new defaults.
        /// </summary>
        private void LoadSettings()
        {
            if (File.Exists(settingsFilePath))
            {
                try
                {
                    string jsonString = File.ReadAllText(settingsFilePath);
                    Settings = JsonSerializer.Deserialize<HotkeySettings>(jsonString) ?? GetDefaultSettings();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to load settings. Using default settings.");
                    Settings = GetDefaultSettings();
                }
            }
            else
            {
                _logger.LogWarning("Settings file not found. Creating new settings.json.");
                Settings = GetDefaultSettings();
                SaveSettings();
            }
            // ✅ Update log level dynamically on load
            _logLevelUpdater(Settings.LogLevel);
        }

        /// <summary>
        /// Provides default settings when no settings file exists.
        /// </summary>
        private HotkeySettings GetDefaultSettings()
        {
            return new HotkeySettings
            {
                Modifiers = 0x0003, // Ctrl + Alt
                Key = (uint)Keys.S, // Default: 'S' key
                ScreenshotPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), "Screenshots"),
                CaptureMode = CaptureMode.ActiveScreen, // Default to capturing active screen
                StartWithWindows = true,
                EnableLogging = true,
                LogLevel = LogLevel.Information
            };
        }

        /// <summary>
        /// Saves the settings to settings.json and applies logging settings dynamically.
        /// </summary>
        public void SaveSettings()
        {
            try
            {
                File.WriteAllText(settingsFilePath, JsonSerializer.Serialize(Settings, new JsonSerializerOptions { WriteIndented = true }));
                _logger.LogDebug("Settings saved successfully.");
                ApplyAutoStartSetting();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save settings.");
            }
        }

        /// <summary>
        /// Updates the hotkey settings and saves them.
        /// </summary>
        public void UpdateHotkey(uint modifiers, uint key)
        {
            _logger.LogDebug($"Hotkey updated: Modifiers={modifiers}, Key={key}");
            Settings.Modifiers = modifiers;
            Settings.Key = key;
            SaveSettings();
        }

        /// <summary>
        /// Updates the screenshot save path and saves the settings.
        /// </summary>
        public void UpdateScreenshotPath(string newPath)
        {
            if (string.IsNullOrWhiteSpace(newPath))
            {
                return;
            }

            _logger.LogDebug($"Screenshot path updated: {newPath}");
            Settings.ScreenshotPath = newPath;
            SaveSettings();
        }

        /// <summary>
        /// Updates the screenshot capture mode and saves the settings.
        /// </summary>
        public void UpdateCaptureMode(CaptureMode mode)
        {
            _logger.LogDebug($"Capture mode updated: {mode}");
            Settings.CaptureMode = mode;
            SaveSettings();
        }

        /// <summary>
        /// Enables or disables Windows startup for the application.
        /// </summary>
        public void UpdateAutoStart(bool enable)
        {
            _logger.LogDebug($"Auto-start setting changed: {enable}");
            Settings.StartWithWindows = enable;
            SaveSettings();
        }

        /// <summary>
        /// Enables or disables logging dynamically.
        /// </summary>
        public void UpdateLogging(bool enable)
        {
            _logger.LogDebug($"Logging enabled: {enable}");
            Settings.EnableLogging = enable;
            SaveSettings();
        }

        /// <summary>
        /// Updates the log level dynamically.
        /// </summary>
        public void UpdateLogLevel(LogLevel level)
        {
            _logger.LogDebug($"Log level changed: {level}");
            Settings.LogLevel = level;
            SaveSettings();

            // ✅ Notify the program to update log level in Serilog
            _logLevelUpdater(level);
        }

        /// <summary>
        /// Ensures the application starts with Windows if enabled.
        /// </summary>
        private void ApplyAutoStartSetting()
        {
            try
            {
                using RegistryKey key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath, true);
                if (key == null)
                {
                    _logger.LogError("Failed to access Windows registry for startup settings.");
                    return;
                }

                string exePath = Application.ExecutablePath;

                if (Settings.StartWithWindows)
                {
                    if (key.GetValue(AppName) == null)
                    {
                        key.SetValue(AppName, $"\"{exePath}\"");
                        _logger.LogDebug("Application registered to start with Windows.");
                    }
                }
                else
                {
                    if (key.GetValue(AppName) != null)
                    {
                        key.DeleteValue(AppName, false);
                        _logger.LogDebug("Application removed from Windows startup.");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to apply auto-start settings.");
            }
        }
    }
}
