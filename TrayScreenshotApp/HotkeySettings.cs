using System.Text.Json.Serialization;

namespace TrayScreenshotApp
{
    public class HotkeySettings
    {
        [JsonPropertyName("Modifiers")]
        public uint Modifiers { get; set; }

        [JsonPropertyName("Key")]
        public uint Key { get; set; }

        [JsonPropertyName("ScreenshotPath")]
        public string ScreenshotPath { get; set; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), "Screenshots");

        [JsonPropertyName("CaptureMode")]
        public CaptureMode CaptureMode { get; set; } = CaptureMode.ActiveScreen; // Default mode

        [JsonPropertyName("StartWithWindows")]
        public bool StartWithWindows { get; set; } = false;

        [JsonPropertyName("EnableLogging")]
        public bool EnableLogging { get; set; } = true;

        [JsonPropertyName("LogLevel")]
        public Microsoft.Extensions.Logging.LogLevel LogLevel { get; set; } = Microsoft.Extensions.Logging.LogLevel.Information;
    }
}
