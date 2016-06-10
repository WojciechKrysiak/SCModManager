using SCModManager.SCFormat;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SCModManager
{
    class ModContext : INotifyPropertyChanged
    {
        const string ModsDir = @"{0}\Paradox Interactive\Stellaris\workshop\content\281990";
        const string SettingsPath = @"{0}\Paradox Interactive\Stellaris\settings.txt";

        List<Mod> _mods = new List<Mod>();

        Mod _selectedMod;
        ModFile _selectedModFile;

        public ModContext()
        {
            var documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            var modsDir = string.Format(ModsDir, documents);

            var settingsPath = string.Format(SettingsPath, documents);
            IEnumerable<string> selectedMods = Enumerable.Empty<string>();

            using (var stream = new FileStream(settingsPath, FileMode.Open, FileAccess.Read))
            {
                var settingsParser = new Parser(new Scanner(stream));

                settingsParser.Parse();

                selectedMods = (settingsParser.Root["last_mods"] as SCObject).Select(kvp => kvp.value.ToString());
            }


            foreach (var dir in Directory.EnumerateDirectories(modsDir))
            {
                var mod = Mod.Load(dir);
                var modId = Path.GetFileName(dir);
                if (mod != null)
                {
                    mod.Selected = selectedMods.Any(sm => sm.Contains(modId));
                    _mods.Add(mod);
                }
            }

            foreach (var mod in Mods)
                mod.MarkConflicts(Mods);

        }

        public IEnumerable<Mod> Mods => _mods;

        public Mod SelectedMod
        {
            get { return _selectedMod; }
            set
            {
                if (value != _selectedMod)
                {
                    _selectedMod = value;
                    OnPropertyChanged(nameof(SelectedMod));
                }
            }
        }

        public ModFile SelectedModFile
        {
            get { return _selectedModFile; }
            set
            {
                if (value != _selectedModFile)
                {
                    _selectedModFile = value;
                    OnPropertyChanged(nameof(SelectedModFile));
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
        }
    }
}
