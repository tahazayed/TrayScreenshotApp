namespace TrayScreenshotApp
{
    public enum CaptureMode
    {
        ActiveScreen = 1,   // Default: Captures the currently focused screen
        ActiveWindow,    // Captures the currently active application window
        VirtualDesktop // Captures all monitors as a single image
    }
}
