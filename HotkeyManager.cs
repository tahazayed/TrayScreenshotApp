using Microsoft.Extensions.Logging;
using System.Runtime.InteropServices;

namespace TrayScreenshotApp
{
    public class HotkeyManager
    {
        private const int HOTKEY_ID = 1;
        private readonly SettingsManager _settingsManager;
        private readonly ILogger<HotkeyManager> _logger;
        private readonly Func<HotkeySelectionForm> _hotkeyFormFactory; // ✅ Injected factory

        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        public HotkeyManager(SettingsManager settingsManager,
                             ILogger<HotkeyManager> logger,
                             Func<HotkeySelectionForm> hotkeyFormFactory) // ✅ Injected factory
        {
            _settingsManager = settingsManager;
            _logger = logger;
            _hotkeyFormFactory = hotkeyFormFactory; // ✅ Store the factory method

            _logger.LogDebug("Initializing Hotkey Manager.");
            RegisterHotkey();
        }

        public void RegisterHotkey()
        {
            _logger.LogDebug($"Registering hotkey: Ctrl + Alt + {(Keys)_settingsManager.Settings.Key}");

            _ = UnregisterHotKey(IntPtr.Zero, HOTKEY_ID);
            bool success = RegisterHotKey(IntPtr.Zero, HOTKEY_ID, _settingsManager.Settings.Modifiers, _settingsManager.Settings.Key);

            if (success)
            {
                _logger.LogDebug("Hotkey registered successfully.");
            }
            else
            {
                _logger.LogError("Failed to register hotkey. It may be used by another application.");
            }
        }

        /// <summary>
        /// ✅ Uses DI factory to create `HotkeySelectionForm`
        /// </summary>
        public void ChangeHotkey()
        {
            _logger.LogDebug("Opening Hotkey Selection Form.");
            using var form = _hotkeyFormFactory.Invoke(); // ✅ Use injected factory
            _ = form.ShowDialog();
        }
        public void ChangeScreenshotPath()
        {
            _logger.LogDebug("Opening folder selection dialog for screenshot path.");

            using FolderBrowserDialog folderDialog = new();
            folderDialog.Description = "Select a folder to save screenshots";
            folderDialog.SelectedPath = _settingsManager.Settings.ScreenshotPath;

            if (folderDialog.ShowDialog() == DialogResult.OK)
            {
                _logger.LogDebug($"Screenshot path changed from '{_settingsManager.Settings.ScreenshotPath}' to '{folderDialog.SelectedPath}'.");
                _settingsManager.UpdateScreenshotPath(folderDialog.SelectedPath);
            }
            else
            {
                _logger.LogDebug("Screenshot path selection canceled.");
            }
        }

        public bool TryRegisterHotkey(uint modifiers, uint key)
        {
            _logger.LogDebug($"Attempting to register hotkey: Ctrl + Alt + {(Keys)key}");

            _ = UnregisterHotKey(IntPtr.Zero, HOTKEY_ID);
            bool success = RegisterHotKey(IntPtr.Zero, HOTKEY_ID, modifiers, key);

            if (success)
            {
                _logger.LogDebug("Hotkey registered successfully.");
                _settingsManager.UpdateHotkey(modifiers, key);
            }
            else
            {
                _logger.LogError($"Hotkey registration failed for: Ctrl + Alt + {(Keys)key}");
            }

            return success;
        }
    }
}
