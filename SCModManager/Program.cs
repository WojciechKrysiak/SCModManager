using Avalonia;
using Avalonia.Logging;
using Avalonia.Logging.Serilog;
using NLog;
using SCModManager.Avalonia;
using System;

namespace SCModManager
{
    class Program
    {
		private static ILogger logger = LogManager.GetCurrentClassLogger();

        static void Main(string[] args)
        {
			logger.Info("====================== Starting SCModManager ======================");

			AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;

			using (var context = new Avalonia.AppContext())
			{
				context.EnterInitialContext(BuildAvaloniaApp());
			}
		}

		public static AppBuilder BuildAvaloniaApp()
			=> AppBuilder.Configure<App>()
				.UseReactiveUI()
				.UsePlatformDetect();


		private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
		{
			logger.Fatal(e.ExceptionObject as Exception, "Unhandled exception!");
			if (e.IsTerminating)
				Environment.Exit(-1);
		}
	}
}
