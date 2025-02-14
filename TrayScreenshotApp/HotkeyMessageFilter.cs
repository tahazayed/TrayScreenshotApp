namespace TrayScreenshotApp
{
    public class HotkeyMessageFilter : IMessageFilter
    {
        private readonly HotkeyManager _hotkeyManager;
        private readonly ScreenshotManager _screenshotManager;

        public HotkeyMessageFilter(HotkeyManager hotkeyManager, ScreenshotManager screenshotManager)
        {
            _hotkeyManager = hotkeyManager;
            _screenshotManager = screenshotManager;
        }

        public bool PreFilterMessage(ref Message m)
        {
            const int WM_HOTKEY = 0x0312;

            if (m.Msg == WM_HOTKEY)
            {
                _ = Task.Run(async () => await _screenshotManager.TakeScreenShotAsync());
                return true;
            }

            return false;
        }

        public void Dispose()
        {
            // No resources to dispose
        }
    }
}