using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PDXModLib.Interfaces;

namespace SCModManager
{
    class StellarisConfiguration : IGameConfiguration
    {
        public string BasePath { get; }
        public string ModsDir { get; }
        public string SettingsPath { get; }
        public string BackupPath { get; }
        public string SavedSelections { get; }
        public IReadOnlyCollection<string> WhiteListedFiles { get; } = new[] {"description.txt", "modinfo.lua", "descriptor.mod, readme.txt"};

        public StellarisConfiguration()
        {
            BasePath = $"{Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)}\\Paradox Interactive\\Stellaris";

            ModsDir = $"{BasePath}\\mod";
            SettingsPath = $"{BasePath}\\settings.txt";
            BackupPath = $"{BasePath}\\settings.bak";
            SavedSelections = $"{BasePath}\\saved_selections.txt";
        }
    }
}
