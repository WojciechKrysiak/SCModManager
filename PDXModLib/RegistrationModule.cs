using Autofac;
using PDXModLib.GameContext;
using PDXModLib.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace PDXModLib
{
    public class RegistrationModule : Module
    {

		protected override void Load(ContainerBuilder builder)
		{
			base.Load(builder);
			builder.RegisterType<InstalledModManager>().As<IInstalledModManager>().InstancePerLifetimeScope();
			builder.RegisterType<PDXModLib.GameContext.GameContext>().As<IGameContext>().InstancePerLifetimeScope();
			builder.RegisterType<ModConflictCalculator>().As<IModConflictCalculator>().InstancePerLifetimeScope();

		}
	}
}
