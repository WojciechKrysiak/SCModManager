using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CWTools.Process;
using NLog;
using NLog.Targets.Wrappers;
using PDXModLib.Interfaces;
using PDXModLib.ModData;
using PDXModLib.Utility;

namespace PDXModLib.GameContext
{
    public class InstalledModManager : IInstalledModManager
    {
        private readonly IGameConfiguration _gameConfiguration;
        private readonly INotificationService _notificationService;
		private readonly ILogger _logger;
		private readonly List<Mod> _mods = new List<Mod>();

        public IEnumerable<Mod> Mods => _mods;

        public InstalledModManager(IGameConfiguration gameConfiguration, INotificationService notificationService, ILogger logger)
        {
            _gameConfiguration = gameConfiguration;
            _notificationService = notificationService;
			_logger = logger;
		}

        public void Initialize()
        {
            LoadMods();
        }

        public void LoadMods()
        { 
            foreach (var file in Directory.EnumerateFiles(_gameConfiguration.ModsDir, "*.mod"))
            {
				var fileName = Path.GetFileName(file);

                if (Mods.Any(m => m.Id == fileName))
                {
					_logger.Debug($"Mod file skipped as it is already loaded: {fileName}");

					continue;
                }

				_logger.Debug($"Loading mod file: {file}");
				Mod mod = null;
                try
                {
                    mod = Mod.Load(file);
                    mod.LoadFiles(_gameConfiguration.BasePath);
                }
                catch (Exception exception)
                {
                    _logger.Error(exception, $"Error loading Mod {fileName}");
                }

                if (mod != null)
                {
                    _mods.Add(mod);
                }
            }
        }

        public async Task<bool> SaveMergedMod(MergedMod mod)
        {
            try
            {
				_logger.Debug($"Saving mod: {mod.FileName} to {_gameConfiguration.ModsDir}");

				var path = Path.Combine(_gameConfiguration.ModsDir, mod.FileName);

                var descPath = Path.Combine(_gameConfiguration.ModsDir, $"{mod.FileName}.mod");

                if (Directory.Exists(path))
                {
                    if (!await _notificationService.RequestConfirmation("Overwrite existing mod?", "Overwrite mod"))
                        return true;
                    foreach (var file in Directory.EnumerateFiles(path, "*.*", SearchOption.AllDirectories))
                        File.Delete(file);

                    Directory.Delete(path, true);
                    File.Delete(descPath);
                }

				var node = new Node(mod.Name);
				node.AllChildren = mod.ToDescriptor().ToList();
				var visitor = new PrintingVisitor();
				visitor.Visit(node);

				var contents = visitor.Result;

                File.WriteAllText(descPath, contents);

                using (var saver = new DiskFileSaver(path))
                {
                    foreach (var modFile in mod.Files)
                    {
                        modFile.Save(saver);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error saving mod!");
                return false;
            }

            return true;
        }
    }
}
