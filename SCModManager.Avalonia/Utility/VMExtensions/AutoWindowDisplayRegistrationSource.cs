using Autofac;
using Autofac.Builder;
using Autofac.Core;
using Autofac.Core.Activators.Delegate;
using Autofac.Core.Registration;
using Avalonia.Controls;
using Avalonia.Threading;
using NLog;
using SCModManager.Avalonia.ViewModels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SCModManager.Avalonia.Utility.VMExtensions
{
	internal class AutoWindowDisplayRegistrationSource : IRegistrationSource
	{
		public const string MetadataKey = "ViewModelDelegate";

		/// <summary>
		/// Retrieve registrations for an unregistered service, to be used
		/// by the container.
		/// </summary>
		/// <param name="service">The service that was requested.</param>
		/// <param name="registrationAccessor">A function that will return existing registrations for a service.</param>
		/// <returns>Registrations providing the service.</returns>
		public IEnumerable<IComponentRegistration> RegistrationsFor(Service service, Func<Service, IEnumerable<IComponentRegistration>> registrationAccessor)
		{
			if (service == null) throw new ArgumentNullException(nameof(service));
			if (registrationAccessor == null) throw new ArgumentNullException(nameof(registrationAccessor));

			var ts = service as IServiceWithType;
			if (ts == null || !IsValid(ts))
				return Enumerable.Empty<IComponentRegistration>();

			var parameterTypes = ts.ServiceType.GenericTypeArguments;

			var viewModelService = new TypedService(parameterTypes[0]);
			var viewModelRegistrations = registrationAccessor(viewModelService).ToArray();
			Debug.Assert(viewModelRegistrations.Length == 1);
			var viewModelRegistration = viewModelRegistrations[0];
			Type factoryDelegate = viewModelRegistration.Metadata.ContainsKey(MetadataKey) ? viewModelRegistration.Metadata[MetadataKey] as Type : null;

			var windowService = new KeyedService(viewModelService.ServiceType, typeof(Func<Window>));
			var windowRegistrations = registrationAccessor(windowService)
				.ToArray();
			Debug.Assert(windowRegistrations.Length == 1);// might be more for theming purposes
			var windowRegistration = windowRegistrations[0];

			var registration = new ComponentRegistration(
			   Guid.NewGuid(),
			   BuildActivator(ts, windowRegistration, factoryDelegate),
			   viewModelRegistration.Lifetime,
			   viewModelRegistration.Sharing,
			   viewModelRegistration.Ownership,
			   new[] { service },
			   windowRegistration.Metadata); // maybe merge the metadata

			return new IComponentRegistration[] { registration };
		}

		public bool IsAdapterForIndividualComponents => true;

		public override string ToString()
		{
			return "Automatic Window Factory Registration Source";
		}

		private bool IsValid(IServiceWithType service)
		{
			return service.ServiceType.IsInterface && service.ServiceType.IsGenericType &&
				   (service.ServiceType.Name.StartsWith("IShowWindow") || service.ServiceType.Name.StartsWith("IShowDialog"));
		}

		private IInstanceActivator BuildActivator(IServiceWithType requestedService, IComponentRegistration windowRegistration, Type factoryDelegate)
		{
			var isDialog = requestedService.ServiceType.Name.StartsWith("IShowDialog");
			int argumentCount = isDialog ? 10 : 9;
			var argumentArray = Enumerable.Repeat(typeof(object), argumentCount).ToArray();
			var argumentTypes = requestedService.ServiceType.GenericTypeArguments;
			Array.Copy(argumentTypes, argumentArray, argumentTypes.Length);

			var genericType = isDialog ? typeof(ShowDialog<,,,,,,,,,>) : typeof(ShowWindow<,,,,,,,,>);
			var typeToResolve = genericType.MakeGenericType(argumentArray);

			return new DelegateActivator(
				typeToResolve,
				(cc, iep) =>
				{
					// this needs to be different
					var logger = LogManager.LogFactory.GetLogger(genericType.FullName);
					var parentWindow = cc.Resolve<Window>();
					var scope = cc.Resolve<ILifetimeScope>();
					Func<Action<ContainerBuilder>, ILifetimeScope> openScope = scope.BeginLifetimeScope;
					var windowFunc = cc.ResolveComponent(windowRegistration, new Parameter[0]);

					return Activator.CreateInstance(typeToResolve, windowFunc, logger, parentWindow, openScope, factoryDelegate);
				});
		}

		private class ShowCommon 
		{
			private readonly Func<Window> _windowCreator;
			private readonly Func<Action<ContainerBuilder>, ILifetimeScope> beginScope;

			protected ShowCommon(Func<Window> windowCreator, Func<Action<ContainerBuilder>,ILifetimeScope> beginScope)
			{
				_windowCreator = windowCreator;
				this.beginScope = beginScope;
			}

			protected ILifetimeScope BeginScope()
			{
				Window window = null;
				ManualResetEvent windowCreated = new ManualResetEvent(false);

				void CreateAndSet()
				{
					window = _windowCreator();
					windowCreated.Set();
				}

				if (Dispatcher.UIThread.CheckAccess())
					CreateAndSet();
				else
					Dispatcher.UIThread.Post(CreateAndSet, DispatcherPriority.Background);

				windowCreated.WaitOne();

				windowCreated.Dispose();

				return beginScope(cb =>
					cb.Register(cc => window).AsSelf()
				);

			}
		}

		private class ShowWindow<TViewModel, T1, T2, T3, T4, T5, T6, T7, T8>
			: ShowCommon,
			  IShowWindow<TViewModel>,
			  IShowWindow<TViewModel, T1>,
			  IShowWindow<TViewModel, T1, T2>,
			  IShowWindow<TViewModel, T1, T2, T3>,
			  IShowWindow<TViewModel, T1, T2, T3, T4>,
			  IShowWindow<TViewModel, T1, T2, T3, T4, T5>,
			  IShowWindow<TViewModel, T1, T2, T3, T4, T5, T6>,
			  IShowWindow<TViewModel, T1, T2, T3, T4, T5, T6, T7>,
			  IShowWindow<TViewModel, T1, T2, T3, T4, T5, T6, T7, T8>
			where TViewModel : WindowViewModel
		{
			private readonly ILogger logger;
			private readonly Window parentWindow;
			private readonly Type factoryDelegate;

			public ShowWindow(Func<Window> windowFunc, ILogger logger, Window parentWindow, Func<Action<ContainerBuilder>, ILifetimeScope> openScope, Type factoryDelegate)
				: base(windowFunc, openScope)
			{
				this.logger = logger;
				this.parentWindow = parentWindow;
				this.factoryDelegate = factoryDelegate;
			}

			private void ShowImpl(Func<ILifetimeScope, TViewModel> resolve)
			{
				var scope = BeginScope();
				
				var viewModel = resolve(scope);
				Dispatcher.UIThread.Post(() =>
				{
					var window = scope.Resolve<Window>();
					window.DataContext = viewModel;
					window.Owner = parentWindow;
					window.Closed += (o, e) => scope.Dispose();
					window.Show();
				});
			}

			private TViewModel Resolve<D>(ILifetimeScope scope, params object[] parameters) where D : Delegate
			{
				if (factoryDelegate != null)
				{
					var d = scope.Resolve(factoryDelegate) as Delegate;
					return d.DynamicInvoke(parameters) as TViewModel;
				}

				return scope.Resolve<D>().DynamicInvoke(parameters) as TViewModel;
			}

			public void Show()
			{
				ShowImpl(s => Resolve<Func<TViewModel>>(s));
			}

			public void Show(T1 t1)
			{
				ShowImpl(s => Resolve<Func<T1, TViewModel>>(s, t1));
			}

			public void Show(T1 t1, T2 t2)
			{
				ShowImpl(s => Resolve<Func<T1, T2, TViewModel>>(s, t1, t2));
			}

			public void Show(T1 t1, T2 t2, T3 t3)
			{
				ShowImpl(s => Resolve<Func<T1, T2, T3, TViewModel>>(s, t1, t2, t3));
			}

			public void Show(T1 t1, T2 t2, T3 t3, T4 t4)
			{
				ShowImpl(s => Resolve<Func<T1, T2, T3, T4, TViewModel>>(s, t1, t2, t3, t4));
			}

			public void Show(T1 t1, T2 t2, T3 t3, T4 t4, T5 t5)
			{
				ShowImpl(s => Resolve<Func<T1, T2, T3, T4, T5, TViewModel>>(s, t1, t2, t3, t4, t5));
			}

			public void Show(T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6)
			{
				ShowImpl(s => Resolve<Func<T1, T2, T3, T4, T5, T6, TViewModel>>(s, t1, t2, t3, t4, t5, t6));
			}

			public void Show(T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7)
			{
				ShowImpl(s => Resolve<Func<T1, T2, T3, T4, T5, T6, T7, TViewModel>>(s, t1, t2, t3, t4, t5, t6, t7));
			}

			public void Show(T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8)
			{
				ShowImpl(s => Resolve<Func<T1, T2, T3, T4, T5, T6, T7, T8, TViewModel>>(s, t1, t2, t3, t4, t5, t6, t7, t8));
			}
		}

		private class ShowDialog<TViewModel, TResult, T1, T2, T3, T4, T5, T6, T7, T8>
		: ShowCommon,
		  IShowDialog<TViewModel, TResult>,
		  IShowDialog<TViewModel, TResult, T1>,
		  IShowDialog<TViewModel, TResult, T1, T2>,
		  IShowDialog<TViewModel, TResult, T1, T2, T3>,
		  IShowDialog<TViewModel, TResult, T1, T2, T3, T4>,
		  IShowDialog<TViewModel, TResult, T1, T2, T3, T4, T5>,
		  IShowDialog<TViewModel, TResult, T1, T2, T3, T4, T5, T6>,
		  IShowDialog<TViewModel, TResult, T1, T2, T3, T4, T5, T6, T7>,
		  IShowDialog<TViewModel, TResult, T1, T2, T3, T4, T5, T6, T7, T8>
		where TViewModel : DialogViewModel<TResult>
		{
			private readonly ILogger logger;
			private readonly Window parentWindow;
			private readonly Type factoryDelegate;

			public ShowDialog(Func<Window> windowFunc, ILogger logger, Window parentWindow, Func<Action<ContainerBuilder>, ILifetimeScope> openScope, Type factoryDelegate)
				: base(windowFunc, openScope)
			{
				this.logger = logger;
				this.parentWindow = parentWindow;
				this.factoryDelegate = factoryDelegate;
			}

			private async Task<TResult> ShowImpl(Func<ILifetimeScope, TViewModel> resolve)
			{
				using (var scope = BeginScope())
				{
					var viewModel = resolve(scope);
					Task<TResult> result = default;
					await Dispatcher.UIThread.InvokeAsync(() =>
					{
						var window = scope.Resolve<Window>();
						window.DataContext = viewModel;
						window.Owner = parentWindow;
						window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
						void onClosing(object sender, EventArgs a)
						{
							window.Closed -= onClosing;
							viewModel.Closing -= onClosing;
							window.Close(viewModel.Result);
						}

						viewModel.Closing += onClosing;
						window.Closed += onClosing;

						result = window.ShowDialog<TResult>();
					}, DispatcherPriority.Background);

					return await result;
				}
			}

			private TViewModel Resolve<D>(ILifetimeScope scope, params object[] parameters) where D : Delegate
			{
				if (factoryDelegate != null)
				{
					var d = scope.Resolve(factoryDelegate) as Delegate;
					return d.DynamicInvoke(parameters) as TViewModel;
				}

				return scope.Resolve<D>().DynamicInvoke(parameters) as TViewModel;
			}

			public Task<TResult> Show()
			{
				return ShowImpl(s =>Resolve<Func<TViewModel>>(s));
			}

			public Task<TResult> Show(T1 t1)
			{
				return ShowImpl(s => Resolve<Func<T1, TViewModel>>(s, t1));
			}

			public Task<TResult> Show(T1 t1, T2 t2)
			{
				return ShowImpl(s => Resolve<Func<T1, T2, TViewModel>>(s, t1, t2));
			}

			public Task<TResult> Show(T1 t1, T2 t2, T3 t3)
			{
				return ShowImpl(s => Resolve<Func<T1, T2, T3, TViewModel>>(s, t1, t2, t3));
			}

			public Task<TResult> Show(T1 t1, T2 t2, T3 t3, T4 t4)
			{
				return ShowImpl(s => Resolve<Func<T1, T2, T3, T4, TViewModel>>(s, t1, t2, t3, t4));
			}

			public Task<TResult> Show(T1 t1, T2 t2, T3 t3, T4 t4, T5 t5)
			{
				return ShowImpl(s => Resolve<Func<T1, T2, T3, T4, T5, TViewModel>>(s, t1, t2, t3, t4, t5));
			}

			public Task<TResult> Show(T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6)
			{
				return ShowImpl(s => Resolve<Func<T1, T2, T3, T4, T5, T6, TViewModel>>(s, t1, t2, t3, t4, t5, t6));
			}

			public Task<TResult> Show(T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7)
			{
				return ShowImpl(s => Resolve<Func<T1, T2, T3, T4, T5, T6, T7, TViewModel>>(s, t1, t2, t3, t4, t5, t6, t7));
			}

			public Task<TResult> Show(T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8)
			{
				return ShowImpl(s => Resolve<Func<T1, T2, T3, T4, T5, T6, T7, T8, TViewModel>>(s, t1, t2, t3, t4, t5, t6, t7, t8));
			}
		}
	}
}
