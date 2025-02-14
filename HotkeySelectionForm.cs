using Microsoft.Extensions.Logging;

namespace TrayScreenshotApp
{
    public class HotkeySelectionForm : Form
    {
        private readonly HotkeyManager _hotkeyManager;
        private readonly SettingsManager _settingsManager;
        private readonly ILogger<HotkeySelectionForm> _logger;

        private Label fixedLabel = null!;
        private ComboBox keyDropdown = null!;
        private Button saveButton = null!;
        private Button cancelButton = null!;
        private readonly uint newModifiers = 0x0003; // Ctrl + Alt by default
        private Keys newHotkey = Keys.None;

        public HotkeySelectionForm(HotkeyManager hotkeyManager, SettingsManager settingsManager, ILogger<HotkeySelectionForm> logger)
        {
            _hotkeyManager = hotkeyManager ?? throw new ArgumentNullException(nameof(hotkeyManager));
            _settingsManager = settingsManager ?? throw new ArgumentNullException(nameof(settingsManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _logger.LogDebug("Opening Hotkey Selection Form.");

            InitializeUI();
            LoadAllowedKeys();
            DisplayExistingHotkey();
        }

        /// <summary>
        /// Initializes the UI components.
        /// </summary>
        private void InitializeUI()
        {
            Width = 300;
            Height = 220;
            Text = "Set New Hotkey";
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;

            fixedLabel = new Label
            {
                Text = "Ctrl + Alt +",
                Dock = DockStyle.Top,
                TextAlign = System.Drawing.ContentAlignment.MiddleCenter
            };

            keyDropdown = new ComboBox
            {
                Dock = DockStyle.Top,
                DropDownStyle = ComboBoxStyle.DropDownList
            };

            saveButton = new Button
            {
                Text = "Save",
                Dock = DockStyle.Bottom,
                Enabled = false
            };

            cancelButton = new Button
            {
                Text = "Cancel",
                Dock = DockStyle.Bottom
            };

            saveButton.Click += SaveButton_Click!;
            cancelButton.Click += CancelButton_Click!;

            Controls.Add(cancelButton);
            Controls.Add(saveButton);
            Controls.Add(keyDropdown);
            Controls.Add(fixedLabel);
        }

        /// <summary>
        /// Loads allowed keys into the dropdown.
        /// </summary>
        private void LoadAllowedKeys()
        {
            List<Keys> validKeys = Enum.GetValues(typeof(Keys))
                .Cast<Keys>()
                .Where(IsAllowedKey)
                .ToList();

            validKeys.Add(Keys.PrintScreen); // ✅ Manually add Print Screen key

            keyDropdown.Items.Clear();
            foreach (var key in validKeys)
            {
                _ = keyDropdown.Items.Add(key);
            }

            keyDropdown.SelectedIndexChanged += KeyDropdown_SelectedIndexChanged!;
        }

        /// <summary>
        /// Checks if a key is allowed for selection.
        /// </summary>
        private bool IsAllowedKey(Keys key)
        {
            return key is >= Keys.A and <= Keys.Z or  // A-Z
                   >= Keys.D0 and <= Keys.D9 or // 0-9
                   >= Keys.F1 and <= Keys.F12 or // F1-F12
                   Keys.PrintScreen; // ✅ Allow Print Screen
        }

        /// <summary>
        /// Displays the currently assigned hotkey.
        /// </summary>
        private void DisplayExistingHotkey()
        {
            newHotkey = (Keys)(_settingsManager.Settings.Key != 0 ? _settingsManager.Settings.Key : (uint)Keys.None); // ✅ Explicit cast

            if (keyDropdown.Items.Contains(newHotkey))
            {
                keyDropdown.SelectedItem = newHotkey;
            }
            else
            {
                keyDropdown.SelectedIndex = 0; // 🔹 Default to first key if not found
            }

            _logger.LogDebug($"Current hotkey displayed: Ctrl + Alt + {newHotkey}");
        }

        /// <summary>
        /// Handles key selection change.
        /// </summary>
        private void KeyDropdown_SelectedIndexChanged(object? sender, EventArgs e)
        {
            newHotkey = (Keys)keyDropdown.SelectedItem!;
            saveButton.Enabled = true;

            _logger.LogDebug($"User selected hotkey: Ctrl + Alt + {newHotkey}");
        }

        /// <summary>
        /// Handles the save action.
        /// </summary>
        private void SaveButton_Click(object? sender, EventArgs e)
        {
            if (newHotkey == Keys.None)
            {
                return;
            }

            _logger.LogDebug($"Attempting to register hotkey: Ctrl + Alt + {newHotkey}");

            bool success = _hotkeyManager.TryRegisterHotkey(newModifiers, (uint)newHotkey);

            if (!success)
            {
                _logger.LogWarning($"Hotkey registration failed for: Ctrl + Alt + {newHotkey}");
                _ = MessageBox.Show("Failed to register the selected hotkey. Please choose another key.",
                                "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return; // 🔹 Prevent the window from closing
            }

            _settingsManager.UpdateHotkey(newModifiers, (uint)newHotkey);
            _logger.LogDebug($"Hotkey updated successfully: Ctrl + Alt + {newHotkey}");
            Close();
        }

        /// <summary>
        /// Handles the cancel action.
        /// </summary>
        private void CancelButton_Click(object? sender, EventArgs e)
        {
            _logger.LogDebug("User canceled hotkey selection.");
            Close();
        }
    }
}
