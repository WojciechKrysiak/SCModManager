using Autofac;
using Autofac.Core;
using Avalonia.Controls;
using Avalonia.Threading;
using NLog;
using PDXModLib.Interfaces;
using PDXModLib.Utility;
using SCModManager.Avalonia.Configuration;
using SCModManager.Avalonia.Views;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace SCModManager.Avalonia
{
	public interface IAppContext
	{
		Task SwitchContext(string context);
	}

    public sealed class AppContext : IAppContext, IDisposable
    {
		IContainer container;
		ILifetimeScope contextScope;

		IConfigurationService configuretionService;
		Lazy<INotificationService> notificationService;
		private static ILogger logger = LogManager.GetCurrentClassLogger();

		private Window _mainAppWindow;

		public AppContext()
		{
			logger.Info("Creating AppContext");

			var builder = new ContainerBuilder();
			
			builder.RegisterModule<PDXModLib.RegistrationModule>();
			builder.RegisterModule<SCModManager.Avalonia.RegistrationModule>();

			builder.RegisterInstance(this).As<IAppContext>();

			builder.Register(cc => _mainAppWindow ?? App.Current.MainWindow);

			container = builder.Build();

			notificationService = container.Resolve<Lazy<INotificationService>>();
			configuretionService = container.Resolve<IConfigurationService>();
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

			var context = configuretionService.InitialContext;

			logger.Info($"Using default context: {context}");

			logger.Info($"Setting up Avalonia infractructure");
			appBuilderBase.SetupWithoutStarting();
			var loadingWindow = new LoadingScreen();
			void RunInitTask(object sender, EventArgs args)
			{
				loadingWindow.Activated -= RunInitTask;
				Task.Run(() => InitialEnterScope(context));
			}
			loadingWindow.Activated += RunInitTask;

			appBuilderBase.Instance.Run(loadingWindow);
		}

		public async Task SwitchContext(string context)
		{
			logger.Info($"Switching context to {context}");

			await ShowLoadingScreen();

			contextScope.Dispose();

			if (!await EnterScope(context))
				Environment.Exit(-1);
		}

		public void Dispose()
		{
			contextScope?.Dispose();
			container?.Dispose();
		}

		private void InitialEnterScope(string context)
		{
			try
			{
				logger.Debug($"Initial enter for scope {context}");
				if (!EnterScope(context).Result)
					Environment.Exit(-1);
			}
			catch (Exception e)
			{
				logger.Fatal(e, "Unhadled exception starting initial scope, exiting!");
				Environment.Exit(-1);
			}
		}

		private Task<bool> EnterScope(string context)
		{
			logger.Debug($"Entering scope {context}");

			void ConfigureContextScope(ContainerBuilder builder)
			{
				builder.RegisterInstance(configuretionService.LoadConfiguration(context)).As<IGameConfiguration>();
			}

			contextScope = container.BeginLifetimeScope(ConfigureContextScope);
			return StartContext(contextScope);
		}


		private async Task ShowLoadingScreen()
		{
			await Dispatcher.UIThread.InvokeAsync(() =>
			{
				Debug.Assert(App.Current.MainWindow == _mainAppWindow, "Something changed the main application window");
				_mainAppWindow = null;
				var oldWindow = App.Current.MainWindow;
				oldWindow.Hide();
				var loadingScreen = new LoadingScreen();
				App.Current.MainWindow = loadingScreen;
				loadingScreen.Show();
				oldWindow.Close();
				loadingScreen.BringIntoView();
			}, DispatcherPriority.Normal);
		}

		private async Task<bool> StartContext(ILifetimeScope scope)
		{
			logger.Debug($"Starting context");
			logger.Debug($"creating new main window");
			Debug.Assert(_mainAppWindow == null, "Main application window is not null!");

			await Dispatcher.UIThread.InvokeAsync(() =>
			{
				_mainAppWindow = new MainWindow();
			});

			logger.Debug($"Resolving viewModel");
			var viewModel = scope.Resolve<ModContext>();
			logger.Debug($"ViewModel resolved");
			if (!await viewModel.Initialize())
				return false;

			await Dispatcher.UIThread.InvokeAsync(() =>
			{
				var oldWindow = App.Current.MainWindow;
				oldWindow.Hide();
				_mainAppWindow.DataContext = viewModel;
				App.Current.MainWindow = _mainAppWindow;
				_mainAppWindow.Show();
				oldWindow.Close();
				_mainAppWindow.BringIntoView();
			}, DispatcherPriority.Normal);
			return true;
		}
	}
}
