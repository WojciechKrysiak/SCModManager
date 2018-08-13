using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using Autofac.Builder;
using Autofac.Core;
using Avalonia.Controls;
using Avalonia.Threading;
using NLog;
using SCModManager.Avalonia.ViewModels;

namespace SCModManager.Avalonia.Utility.VMExtensions
{

	public static class ContainerBuilderExtensions
	{
		public static void RegisterWindow<TWindow, TViewModel>(this ContainerBuilder builder)
			where TWindow : Window
			where TViewModel : WindowViewModel
		{
			builder.RegisterType<TWindow>()
				   .Keyed<Window>(typeof(TViewModel));
			builder.RegisterType<TViewModel>()
				   .AsSelf();
		}

		public static void RegisterWindow<TWindow, TViewModel, TDelegate>(this ContainerBuilder builder)
			where TWindow : Window
			where TViewModel : WindowViewModel
			where TDelegate : Delegate
		{
			builder.RegisterType<TWindow>()
				   .Keyed<Window>(typeof(TViewModel));
			builder.RegisterType<TViewModel>()
				   .AsSelf()
				   .WithMetadata(AutoWindowDisplayRegistrationSource.MetadataKey, typeof(TDelegate));
		}

		public static void RegisterDialog<TWindow, TViewModel, TResult>(this ContainerBuilder builder) where TWindow : Window where TViewModel : DialogViewModel<TResult>
		{
			builder.RegisterType<TWindow>()
				   .Keyed<Window>(typeof(TViewModel)); 
			builder.RegisterType<TViewModel>()
				   .AsSelf();
		}

		public static void RegisterDialog<TWindow, TViewModel, TResult, TDelegate>(this ContainerBuilder builder) 
			where TWindow : Window 
			where TViewModel : DialogViewModel<TResult>
			where TDelegate : Delegate
		{
			builder.RegisterType<TWindow>()
				   .Keyed<Window>(typeof(TViewModel));
			builder.RegisterType<TViewModel>()
				   .AsSelf()
				   .WithMetadata(AutoWindowDisplayRegistrationSource.MetadataKey, typeof(TDelegate));
		}
	}
}
