using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using System.Text.Json;

namespace TrayScreenshotApp
{
    /// <summary>
    /// Manages persistent application settings including hotkeys, logging, and startup configuration.
    /// </summary>
    public class SettingsManager
    {
        private readonly string settingsFilePath = "settings.json";
        private const string RegistryKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string AppName = "TrayScreenshotApp";

        // Holds current in-memory application settings
        public HotkeySettings Settings { get; private set; } = null!;

        private readonly ILogger<SettingsManager> _logger;
        private readonly Action<LogLevel> _logLevelUpdater;

        /// <summary>
        /// Constructs the SettingsManager and loads settings from disk.
        /// </summary>
        public SettingsManager(ILogger<SettingsManager> logger, Action<LogLevel> logLevelUpdater)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _logLevelUpdater = logLevelUpdater ?? throw new ArgumentNullException(nameof(logLevelUpdater));
            LoadSettings();
        }

        /// <summary>
        /// Loads settings from settings.json or initializes defaults if missing or corrupted.
        /// </summary>
        private void LoadSettings()
        {
            if (File.Exists(settingsFilePath))
            {
                try
                {
                    string jsonString = File.ReadAllText(settingsFilePath);
                    var deserialized = JsonSerializer.Deserialize<HotkeySettings>(jsonString);
                    Settings = deserialized ?? GetDefaultSettings();
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

            // Sync current log level with logging infrastructure
            _logLevelUpdater(Settings.LogLevel);
        }

        /// <summary>
        /// Returns default app settings in case of first run or deserialization failure.
        /// </summary>
        private HotkeySettings GetDefaultSettings()
        {
            return new HotkeySettings
            {
                Modifiers = 0x0003, // Ctrl + Alt
                Key = (uint)Keys.S, // Default key: 'S'
                ScreenshotPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), "Screenshots"),
                CaptureMode = CaptureMode.ActiveScreen,
                StartWithWindows = true,
                EnableLogging = true,
                LogLevel = LogLevel.Information
            };
        }

        /// <summary>
        /// Saves current settings to settings.json and applies auto-start settings.
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
        /// Updates the registered hotkey combination.
        /// </summary>
        public void UpdateHotkey(uint modifiers, uint key)
        {
            _logger.LogDebug($"Hotkey updated: Modifiers={modifiers}, Key={key}");
            Settings.Modifiers = modifiers;
            Settings.Key = key;
            SaveSettings();
        }

        /// <summary>
        /// Updates the directory where screenshots are saved.
        /// </summary>
        public void UpdateScreenshotPath(string newPath)
        {
            if (string.IsNullOrWhiteSpace(newPath))
                return;

            _logger.LogDebug($"Screenshot path updated: {newPath}");
            Settings.ScreenshotPath = newPath;
            SaveSettings();
        }

        /// <summary>
        /// Updates the screen capture mode (active window, desktop, etc).
        /// </summary>
        public void UpdateCaptureMode(CaptureMode mode)
        {
            _logger.LogDebug($"Capture mode updated: {mode}");
            Settings.CaptureMode = mode;
            SaveSettings();
        }

        /// <summary>
        /// Enables or disables launching the app on Windows startup.
        /// </summary>
        public void UpdateAutoStart(bool enable)
        {
            _logger.LogDebug($"Auto-start setting changed: {enable}");
            Settings.StartWithWindows = enable;
            SaveSettings();
        }

        /// <summary>
        /// Enables or disables logging feature at runtime.
        /// </summary>
        public void UpdateLogging(bool enable)
        {
            _logger.LogDebug($"Logging enabled: {enable}");
            Settings.EnableLogging = enable;
            SaveSettings();
        }

        /// <summary>
        /// Updates the log level used by Serilog and saves the setting.
        /// </summary>
        public void UpdateLogLevel(LogLevel level)
        {
            _logger.LogDebug($"Log level changed: {level}");
            Settings.LogLevel = level;
            SaveSettings();

            // Notify logging system to apply the change
            _logLevelUpdater(level);
        }

        /// <summary>
        /// Registers or removes the app from Windows startup via registry.
        /// </summary>
        private void ApplyAutoStartSetting()
        {
            try
            {
                using RegistryKey? key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath, writable: true);
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
