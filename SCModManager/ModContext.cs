using SCModManager.SCFormat;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.CommandWpf;

namespace SCModManager
{
    class ModContext : ObservableObject
    {
        private static readonly string ModsDir = $"{Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)}\\Paradox Interactive\\Stellaris\\mod";
        private static readonly string SettingsPath = $"{Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)}\\Paradox Interactive\\Stellaris\\settings.txt";

        private readonly SCObject _settinsRoot;

        readonly List<Mod> _mods = new List<Mod>();

        Mod _selectedMod;
        ModFile _selectedModFile;
        private readonly SCObject _lastMods;

        public ModContext()
        {
            IEnumerable<string> selectedMods = Enumerable.Empty<string>();

            using (var stream = new FileStream(SettingsPath, FileMode.Open, FileAccess.Read))
            {
                var settingsParser = new Parser(new Scanner(stream));

                settingsParser.Parse();

                _settinsRoot = settingsParser.Root;

                _lastMods = _settinsRoot["last_mods"] as SCObject;

                if (_lastMods != null)
                    selectedMods = _lastMods.Select(kvp => kvp.Value.ToString()).ToList();
            }

            foreach (var file in Directory.EnumerateFiles(ModsDir, "*.mod"))
            {
                var mod = Mod.Load(file);
                if (mod != null)
                {
                    mod.Selected = selectedMods.Any(sm => sm.Contains(mod.Id));
                    mod.PropertyChanged += ModOnPropertyChanged ;
                    _mods.Add(mod);
                }
            }

            foreach (var mod in Mods)
                mod.MarkConflicts(Mods);

            SaveSettingsCommand = new RelayCommand(SaveSettings);

        }

        private void ModOnPropertyChanged(object sender, PropertyChangedEventArgs propertyChangedEventArgs)
        {
            if (propertyChangedEventArgs.PropertyName == nameof(Mod.Selected))
            {
                var mod = sender as Mod;
                if (mod.Selected)
                {
                    _lastMods.Add(null, null, new SCString(mod.Key));
                }
                else
                {
                    var item = _lastMods.FirstOrDefault(kvp => (kvp.Value as SCString)?.Text == mod.Key);
                    _lastMods.Remove(item);
                }
            }
        }

        private void SaveSettings()
        {
            try
            {
                var backup = Path.ChangeExtension(SettingsPath, "bak");
                if (File.Exists(backup))
                {
                    File.Delete(backup);
                }
                File.Move(SettingsPath, backup);

                using (var stream = new FileStream(SettingsPath, FileMode.Create, FileAccess.Write))
                {
                    _settinsRoot.WriteToStream(stream);
                }
            }
            catch 
            { }
        }

        public ICommand SaveSettingsCommand { get; }

        public IEnumerable<Mod> Mods => _mods;

        public Mod SelectedMod
        {
            get { return _selectedMod; }
            set
            {
                if (Set(ref _selectedMod, value))
                    MarkConflicted();
            }
        }

        public ModFile SelectedModFile
        {
            get { return _selectedModFile; }
            set { Set(ref _selectedModFile, value); }
        }


        private void MarkConflicted()
        {
            foreach(var mod in Mods)
            {
                mod.SetHasConflictWithMod(SelectedMod);
            }
        }
    }
}
