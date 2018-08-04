using Autofac;
using Autofac.Core;
using NLog;
using PDXModLib.Utility;
using System.Linq;
using System.Reflection;

namespace SCModManager.Avalonia
{
    public class RegistrationModule : Autofac.Module
    {
		protected override void Load(ContainerBuilder builder)
		{
			base.Load(builder);
			builder.RegisterType<NotificationService>().As<INotificationService>();
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
