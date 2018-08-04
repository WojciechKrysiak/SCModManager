using Autofac;
using Autofac.Core;
using Avalonia.Controls;
using Avalonia.Threading;
using NLog;
using PDXModLib.Interfaces;
using PDXModLib.Utility;
using SCModManager.Avalonia.Views;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace SCModManager.Avalonia
{
    public sealed class AppContext : IDisposable
    {
		IContainer container;
		ILifetimeScope contextScope;

		Lazy<INotificationService> notificationService;

		private static ILogger logger = LogManager.GetCurrentClassLogger();

		public AppContext()
		{
			logger.Info("Creating AppContext");

			var builder = new ContainerBuilder();
			
			builder.RegisterModule<PDXModLib.RegistrationModule>();
			builder.RegisterModule<SCModManager.Avalonia.RegistrationModule>();

			container = builder.Build();

			notificationService = container.Resolve<Lazy<INotificationService>>();
			logger.Info("AppContext created");
		}

		public void EnterInitialContext<TAppBuilder>(AppBuilderBase<TAppBuilder> appBuilderBase) where TAppBuilder : AppBuilderBase<TAppBuilder>, new()
		{
			logger.Info("Entering initial context");

			if (contextScope != null)
			{
				logger.Fatal("Life time scope already created, EnterInitialContext should only be called once!");
				Environment.Exit(-1);
			}

			logger.Info("Loading configuration");

			var context = "Stellaris";

			appBuilderBase.SetupWithoutStarting();
			var loadingWindow = new LoadingScreen();
			loadingWindow.Activated += (o, e) => Task.Run(() => EnterScope(context));

			appBuilderBase.Instance.Run(loadingWindow);
		}

		public async Task SwitchContext(string context)
		{
			logger.Info($"Switching context to {context}");

			await ShowLoadingScreen();

			contextScope.Dispose();

			await EnterScope(context);
		}


		public void Dispose()
		{
			contextScope?.Dispose();
			container?.Dispose();
		}

		private Task EnterScope(string context)
		{
			void ConfigureContextScope(ContainerBuilder builder)
			{
				builder.RegisterInstance(new StellarisConfiguration()).As<IGameConfiguration>();
				builder.RegisterType<ModContext>().AsSelf().WithParameter(
					(pi, ctx) => pi.ParameterType == typeof(string),
					(pi, ctx) => context);
			}

			contextScope = container.BeginLifetimeScope(ConfigureContextScope);
			return StartContext(contextScope);
		}


		private async Task ShowLoadingScreen()
		{
			await Dispatcher.UIThread.InvokeAsync(() =>
			{
				var oldWindow = App.Current.MainWindow;
				oldWindow.Hide();
				var loadingScreen = new LoadingScreen();
				App.Current.MainWindow = loadingScreen;
				loadingScreen.Show();
				oldWindow.Close();
				loadingScreen.BringIntoView();
			}, DispatcherPriority.Normal);
		}


		private async Task StartContext(ILifetimeScope scope)
		{
			var viewModel = scope.Resolve<ModContext>();
			await viewModel.Initialize();

			await Dispatcher.UIThread.InvokeAsync(() =>
			{
				var oldWindow = App.Current.MainWindow;
				oldWindow.Hide();
				var window = new MainWindow { DataContext = viewModel };
				App.Current.MainWindow = window;
				window.Show();
				oldWindow.Close();
				window.BringIntoView();
			}, DispatcherPriority.Normal);
		}
	}
}
