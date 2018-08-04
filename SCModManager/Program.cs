using Avalonia;
using Avalonia.Logging;
using Avalonia.Logging.Serilog;
using SCModManager.Avalonia;
using SCModManager.Avalonia.Views;

namespace SCModManager.Windows
{
    class Program
    {
        static void Main(string[] args)
        {
		//	BuildAvaloniaApp().Start<LoadingScreen>();
			using (var context = new AppContext())
			{
				context.EnterInitialContext(BuildAvaloniaApp());
			}
		}

		public static AppBuilder BuildAvaloniaApp()
			=> AppBuilder.Configure<App>()
				.UseReactiveUI()
                .UsePlatformDetect()
                .LogToDebug(LogEventLevel.Information);
    }
}
