# TrayScreenshotApp

TrayScreenshotApp is a lightweight C# application that allows users to capture screenshots directly from the system tray using customizable hotkeys.

## Features
- **Tray Icon Integration:** Quickly access the app from the system tray.
- **Custom Hotkeys:** Assign and manage hotkeys for capturing screenshots.
- **Multiple Capture Modes:** Support for different screenshot modes (e.g., full screen, window, region).
- **Settings Management:** Save and load user preferences seamlessly.

## Project Structure
- `CaptureModeEnum.cs` - Enum defining different screenshot capture modes.
- `HotkeyManager.cs` - Handles hotkey registration and management.
- `ScreenshotManager.cs` - Core logic for capturing screenshots.
- `TrayIconManager.cs` - Manages the system tray icon and its interactions.
- `SettingsManager.cs` - Handles saving and loading application settings.
- `Program.cs` - Main entry point of the application.

## Getting Started
### Prerequisites
- Visual Studio 2022 or later
- .NET 9

### Installation
1. Clone the repository.
2. Open `TrayScreenshotApp.sln` in Visual Studio.
3. Build and run the project.

### Usage
- Right-click the tray icon for options.
- Configure hotkeys through the settings interface.
- Capture screenshots using your assigned hotkeys.

## License
This project is licensed under the MIT License. See `LICENSE.txt` for details.

## Contributions
Contributions are welcome! Feel free to submit issues or pull requests.

## Acknowledgments
Thanks to all contributors and open-source libraries used in this project.
