using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
        private readonly List<Mod> _mods = new List<Mod>();
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        public IEnumerable<Mod> Mods => _mods;

        public InstalledModManager(IGameConfiguration gameConfiguration, INotificationService notificationService)
        {
            _gameConfiguration = gameConfiguration;
            _notificationService = notificationService;
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
                    continue;
                }

                Mod mod = null;
                try
                {
                    mod = Mod.Load(file);
                    mod.LoadFiles(_gameConfiguration.BasePath);
                }
                catch (Exception exception)
                {
                    Log.Error(exception, $"Error loading Mod {fileName}");
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

                var contents = string.Join(Environment.NewLine, mod.ToDescriptor());

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
                Log.Error(ex, "Error saving mod!");
                return false;
            }

            return true;
        }
    }
}
