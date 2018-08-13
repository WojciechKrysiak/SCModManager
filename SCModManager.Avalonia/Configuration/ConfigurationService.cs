using Autofac.Features.Indexed;
using PDXModLib.Interfaces;
using System.Configuration;

namespace SCModManager.Avalonia.Configuration
{
	public interface IConfigurationService
	{
		string InitialContext { get; set; }
		IGameConfiguration LoadConfiguration(string context);
		void SaveConfiguration(IGameConfiguration configuration);
	}

    public class ConfigurationService : IConfigurationService
    {
		private readonly IIndex<string, IDefaultGameConfiguration> configurations;
		private readonly System.Configuration.Configuration _configuration;

		public ConfigurationService(IIndex<string, IDefaultGameConfiguration> configurations)
		{
			this.configurations = configurations;
            _configuration = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
		}

		public string InitialContext { get; set; } = "Stellaris";

		public IGameConfiguration LoadConfiguration(string context)
		{
			var section = _configuration.Sections[context] as GameConfigurationSection;

			if (section == null)
			{
				section = new GameConfigurationSection();
				_configuration.Sections.Add(context, section);

				if (configurations.TryGetValue(context, out var configuration))
					section.Init(configuration);
				_configuration.Save(ConfigurationSaveMode.Modified);
			}

			return section;
		}

		public void SaveConfiguration(IGameConfiguration configuration)
		{
			var section = _configuration.Sections[configuration.GameName] as GameConfigurationSection;

			if (section == null)
			{
				section = new GameConfigurationSection();
				_configuration.Sections.Add(configuration.GameName, section);
			}

			section.Init(configuration);
			_configuration.Save(ConfigurationSaveMode.Modified);
		}
	}
}
