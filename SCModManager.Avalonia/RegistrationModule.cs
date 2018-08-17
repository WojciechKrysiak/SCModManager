using Autofac;
using Autofac.Core;
using NLog;
using PDXModLib.Interfaces;
using PDXModLib.Utility;
using SCModManager.Avalonia.Configuration;
using SCModManager.Avalonia.Configuration.Defaults;
using SCModManager.Avalonia.Platform;
using SCModManager.Avalonia.Utility.VMExtensions;
using SCModManager.Avalonia.Views;
using SCModManager.Avalonia.ViewModels;
using System;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using SCModManager.Avalonia.DiffMerge;
using PDXModLib.ModData;
using SCModManager.Avalonia.SteamWorkshop;

namespace SCModManager.Avalonia
{
    public class RegistrationModule : Autofac.Module
    {
		protected override void Load(ContainerBuilder builder)
		{
			base.Load(builder);
			builder.RegisterSource(new AutoWindowDisplayRegistrationSource());

			builder.RegisterType<NotificationService>().As<INotificationService>();
			builder.RegisterType<ConfigurationService>().As<IConfigurationService>();
			builder.RegisterType<StellarisConfiguration>().Keyed<IDefaultGameConfiguration>("Stellaris");
			builder.RegisterType<ModContext>().AsSelf();
			builder.RegisterType<SteamIntegration>().As<ISteamService>().As<ISteamIntegration>().SingleInstance();
			builder.RegisterType<ModVM>().AsSelf();

			builder.RegisterDialog<PreferencesWindow, PreferencesWindowViewModel, bool>();
			builder.RegisterDialog<NameConfirm, NameConfirmVM, string>();
			builder.RegisterDialog<Merge, ModMergeViewModel, MergedMod>();
			builder.RegisterDialog<NotificationView, NotificationViewModel, DialogResult, NotificationViewModel.Factory>();


			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
				builder.RegisterType<PlatformWindows>().As<IPlatfomInterface>();
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
				builder.RegisterType<PlatformLinux>().As<IPlatfomInterface>();
			if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
				builder.RegisterType<PlatformOSX>().As<IPlatfomInterface>();
		}

		protected override void AttachToComponentRegistration(IComponentRegistry componentRegistry, IComponentRegistration registration)
		{
			// Handle constructor parameters.
			registration.Preparing += OnComponentPreparing;

			// Handle properties.
			registration.Activated += (sender, e) => InjectLoggerProperties(e.Instance);
		}

		private static void OnComponentPreparing(object sender, PreparingEventArgs e)
		{
			e.Parameters = e.Parameters.Union(
				new[]
				{
					new ResolvedParameter(
						(p, i) => p.ParameterType == typeof (ILogger),
						(p, i) => LogManager.LogFactory.GetLogger(p.Member.DeclaringType.FullName))
				});
		}

		private static void InjectLoggerProperties(object instance)
		{
			var instanceType = instance.GetType();

			// Get all the injectable properties to set.
			// If you wanted to ensure the properties were only UNSET properties,
			// here's where you'd do it.
			var properties = instanceType
			  .GetProperties(BindingFlags.Public | BindingFlags.Instance)
			  .Where(p => p.PropertyType == typeof(ILogger) && p.CanWrite && p.GetIndexParameters().Length == 0);

			// Set the properties located.
			foreach (var propToSet in properties)
			{
				propToSet.SetValue(instance, LogManager.GetLogger(instanceType.FullName), null);
			}
		}
	}
}
