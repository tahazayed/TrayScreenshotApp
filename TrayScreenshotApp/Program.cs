using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Core;
using System.Reflection;

namespace TrayScreenshotApp
{
    internal static class Program
    {
        private static readonly LoggingLevelSwitch _logLevelSwitch = new();

        [STAThread]
        private static void Main()
        {
            Log.Logger = new LoggerConfiguration()
                           .MinimumLevel.ControlledBy(_logLevelSwitch)
                           .WriteTo.Async(a => a.File("logs/app_log.txt",
                               fileSizeLimitBytes: 5_000_000,
                               rollOnFileSizeLimit: true,
                               retainedFileCountLimit: 7,
                               shared: true))
                           .CreateLogger();

            var services = new ServiceCollection()
                .AddLogging(builder =>
                {
                    _ = builder.ClearProviders();
                    _ = builder.AddSerilog();

                })
                .AddSingleton(sp => new SettingsManager(
                    sp.GetRequiredService<ILogger<SettingsManager>>(),
                    UpdateLogLevel // ✅ Pass log level update callback
                ))
                 .AddSingleton<HotkeyManager>(sp => new HotkeyManager(
                    sp.GetRequiredService<SettingsManager>(),
                    sp.GetRequiredService<ILogger<HotkeyManager>>(),
                    () => sp.GetRequiredService<HotkeySelectionForm>()
                ))
                .AddSingleton<ScreenshotManager>()
                .AddSingleton<TrayIconManager>()
                .AddTransient<HotkeySelectionForm>()
                .BuildServiceProvider();

            var appVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "Unknown";
            var trayLogger = services.GetRequiredService<ILogger<TrayIconManager>>();

            trayLogger.LogInformation($"Starting Screenshot App - Version {appVersion}");

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            Application.Run(services.GetRequiredService<TrayIconManager>());

            trayLogger.LogInformation($"Exit Screenshot App - Version {appVersion}");
        }

        private static Serilog.Events.LogEventLevel ConvertLogLevel(LogLevel logLevel)
        {
            return logLevel switch
            {
                LogLevel.Trace => Serilog.Events.LogEventLevel.Verbose,
                LogLevel.Debug => Serilog.Events.LogEventLevel.Debug,
                LogLevel.Information => Serilog.Events.LogEventLevel.Information,
                LogLevel.Warning => Serilog.Events.LogEventLevel.Warning,
                LogLevel.Error => Serilog.Events.LogEventLevel.Error,
                LogLevel.Critical => Serilog.Events.LogEventLevel.Fatal,
                _ => Serilog.Events.LogEventLevel.Information
            };
        }

        public static void UpdateLogLevel(LogLevel newLevel)
        {
            _logLevelSwitch.MinimumLevel = ConvertLogLevel(newLevel);
            Log.Information("Log level changed to: {NewLevel}", newLevel);
        }
    }
}
