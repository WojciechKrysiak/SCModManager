using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PDXModLib.Interfaces;
using SCModManager.Avalonia.Platform;

namespace SCModManager.Avalonia.Configuration.Defaults
{
    public class StellarisConfiguration : IDefaultGameConfiguration
    {
		private readonly ISteamService steamService;

		public int AppId => 281990;
		public string GameName => "Stellaris";
		public string BasePath { get; }
        public string ModsDir { get; }
        public string SettingsPath { get; }
        public string BackupPath { get; }
        public string SavedSelections { get; }
        public IReadOnlyCollection<string> WhiteListedFiles { get; } = new[] {"description.txt", "modinfo.lua", "descriptor.mod", "readme.txt"};

		private string _gameInstallationDirectory;
		public string GameInstallationDirectory => _gameInstallationDirectory ?? (_gameInstallationDirectory = steamService.LocateAppInstalationDirectory(AppId));

		public StellarisConfiguration(ISteamService steamService, IPlatfomInterface platfomValues)
        {
            BasePath = Path.Combine(platfomValues.SettingsBasePath, "Stellaris");

            ModsDir = Path.Combine(BasePath,"mod");
            SettingsPath = Path.Combine(BasePath, "settings.txt");
            BackupPath = Path.Combine(BasePath, "settings.bak");
			SavedSelections = Path.Combine(BasePath, "saved_selections.txt");
			this.steamService = steamService;
		}
    }
}
