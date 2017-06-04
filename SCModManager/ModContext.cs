using SCModManager.SCFormat;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.CommandWpf;
using NLog;
using Ionic.Zip;

namespace SCModManager
{
    class ModContext : ObservableObject
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private static readonly string ModsDir = $"{Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)}\\Paradox Interactive\\Stellaris\\mod";
        private static readonly string SettingsPath = $"{Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)}\\Paradox Interactive\\Stellaris\\settings.txt";
        private static readonly string SavedSelections = $"{Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)}\\Paradox Interactive\\Stellaris\\saved_selections.txt";

        private readonly SCObject _settingsRoot;

        private readonly SCObject _savedSelectionsDocument;

        readonly List<Mod> _mods = new List<Mod>();

        Mod _selectedMod;
        ModFile _selectedModFile;

        public SCObject Selections => (_savedSelectionsDocument["Selections"] as SCObject);

        public SCKeyValObject CurrentSelection
        {
            get {
                var selectionKey = _savedSelectionsDocument["CurrentSelection"] as SCString;

                if (selectionKey == null)
                    return null;

                var obj = (_savedSelectionsDocument["Selections"] as SCObject);
                return obj?.FirstOrDefault(sckv => (sckv.Key as SCString)?.Text == selectionKey.Text) as SCKeyValObject;
            }
            set {

                _savedSelectionsDocument["CurrentSelection"] = new SCString(value.Key.ToString());

                var obj = value.Value as SCObject;
                
                if (obj != null)
                {
                    foreach (var mod in _mods)
                    {
                        mod.Selected = obj?.Any(kvp => (kvp.Value as SCString)?.Text == mod.Key) ?? false;
                    }
                }
                RaisePropertyChanged();
            }
        }

        public ModContext()
        {
            try
            {
                using (var stream = new FileStream(SettingsPath, FileMode.Open, FileAccess.Read))
                {
                    var settingsParser = new Parser(new Scanner(stream));

                    settingsParser.Parse();

                    _settingsRoot = settingsParser.Root;
                }

                LoadMods();

                if (!File.Exists(SavedSelections))
                {
                    _savedSelectionsDocument = new SCObject();
                    var selection = new SCKeyValObject(new SCString("Stellaris selection"), _settingsRoot["last_mods"]);
                    _savedSelectionsDocument["Selections"] = new SCObject() { selection };
                    CurrentSelection = selection;
                }
                else using (var stream = new FileStream(SavedSelections, FileMode.Open, FileAccess.Read))
                {
                    var selectionParser = new Parser(new Scanner(stream));

                    selectionParser.Parse();

                    _savedSelectionsDocument = selectionParser.Root;
                    var selectionIdx = _savedSelectionsDocument["SavedToStellaris"] as SCString;
                    if (selectionIdx != null)
                    {
                        var selection = new SCKeyValObject(selectionIdx, _settingsRoot["last_mods"]);
                        Selections[selectionIdx] = _settingsRoot["last_mods"];
                        CurrentSelection = selection;
                    }
                }

                MarkConflicts();

                SortAndUpdate();

                CurrentSelection = Selections.FirstOrDefault(kvo => ((kvo.Key as SCString).Text == "Stellaris selection"));

                SaveSettingsCommand = new RelayCommand(SaveSettings);
                MergeModsCommand = new RelayCommand(MergeMods);
                Duplicate = new RelayCommand(DoDuplicate);
                Delete = new RelayCommand(DoDelete,() => Selections.Count() > 1);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SortAndUpdate()
        {
            var sorted = Mods.OrderBy(m => m.Name).ToList();
            _mods.Clear();
            _mods.AddRange(sorted);
            RaisePropertyChanged(nameof(Mods));
        }

        private void DoDelete()
        {
            var selection = CurrentSelection;

            if (MessageBox.Show($"Are you sure you want to delete {selection.Key}?", "Confirm deletion", MessageBoxButton.OKCancel, MessageBoxImage.Question) == MessageBoxResult.OK)
            {
                Selections.Remove(selection);
                CurrentSelection = Selections.FirstOrDefault();
                Delete.RaiseCanExecuteChanged();
                SaveSelection();
            }
        }

        public void LoadMods()
        {
            _mods?.ForEach(m => m.PropertyChanged -= ModOnPropertyChanged);
            _mods.Clear();
            if (Directory.Exists(ModsDir))
            {
                foreach (var file in Directory.EnumerateFiles(ModsDir, "*.mod"))
                {
                    Mod mod = null;
                    try
                    {
                        mod = Mod.Load(file);
                    }
                    catch (Exception exception)
                    {
                        Log.Error(exception);
                    }

                    if (mod != null)
                    {
                        _mods.Add(mod);
                    }
                }
            }
            else
            {
                MessageBox.Show("No mods installed - nothing to do!");
                Application.Current.Shutdown();
            }
            RaisePropertyChanged(nameof(Mods));
        }

        public void MarkConflicts()
        {
            foreach (var mod in Mods)
            {
                mod.MarkConflicts(Mods);
                mod.PropertyChanged += ModOnPropertyChanged;
            }
        }

        private void DoDuplicate()
        {
            var selection = CurrentSelection;

            int cnt = Selections.Count();

            var name = $"Selection {cnt + 1}";

            var confirm = new NameConfirm();
            var confirmVM = new NameConfirmVM(name);
            confirmVM.ShouldClose += (o, b) =>
            {
                if (b)
                {
                    name = confirmVM.Name;
                }
                confirm.Close();
            };

            confirm.DataContext = confirmVM;
            confirm.ShowDialog();

            var newSelection = new SCKeyValObject(new SCString(name), SCObject.Create(selection.Value as SCObject));
            Selections.Add(newSelection);

            CurrentSelection = newSelection;
            SaveSelection();
        }

        private Window mergeWindow;
        private Mod _selectedModConflict;
        private ComparisonContext _comparisonContext;

        private void MergeMods()
        {
            mergeWindow = new Merge();
            mergeWindow.Closed += MergeWindow_Closed;
            mergeWindow.DataContext = new ModMergeContext(_mods.Where(m => m.Selected), SaveMergedMod);
            mergeWindow.ShowDialog();
        }

        private void MergeWindow_Closed(object sender, EventArgs e)
        {
            mergeWindow = null;
        }

        private void SaveMergedMod(ModMergeContext mod)
        {
            var path = Path.Combine(ModsDir, mod.Result.FileName);

            var descPath = Path.Combine(ModsDir, $"{mod.Result.FileName}.mod");

            if (Directory.Exists(path))
            {
                if (MessageBox.Show("Overwrite existing mod?", "Overwrite mod", MessageBoxButton.OKCancel) == MessageBoxResult.Cancel)
                    return;
                foreach(var file in Directory.EnumerateFiles(path, "*.*", SearchOption.AllDirectories))
                    File.Delete(file);

                Directory.Delete(path, true);
                File.Delete(descPath);
            }

            var contents = string.Join(Environment.NewLine, mod.Result.ToDescriptor());

            File.WriteAllText(descPath, contents);

            foreach(var modFile in mod.Result.Files)
            {
                var dir = Path.Combine(path, modFile.Directory);
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
                var fn = Path.Combine(path, modFile.Path);

                modFile.Save(fn);
                
            }

            LoadMods();
            MarkConflicts();
            mergeWindow.Close();
            SortAndUpdate();
        }

        private void ModOnPropertyChanged(object sender, PropertyChangedEventArgs propertyChangedEventArgs)
        {
            if (propertyChangedEventArgs.PropertyName == nameof(Mod.Selected))
            {
                var mod = sender as Mod;
                if (mod.Selected)
                {
                    (CurrentSelection.Value as SCObject).Add(SCKeyValObject.Create(mod.Key));
                }
                else
                {
                    var selected = (CurrentSelection.Value as SCObject).FirstOrDefault(kvp => (kvp.Value as SCString)?.Text == mod.Key);
                    (CurrentSelection.Value as SCObject).Remove(selected);
                }
                SaveSelection();
            }
        }

        private void SaveSettings()
        {
            try
            {
                _savedSelectionsDocument["SavedToStellaris"] = new SCString(CurrentSelection.Key.ToString()); 

                _settingsRoot["last_mods"] = CurrentSelection.Value;

                var backup = Path.ChangeExtension(SettingsPath, "bak");
                if (File.Exists(backup))
                {
                    File.Delete(backup);
                }
                File.Move(SettingsPath, backup);

                using (var stream = new FileStream(SettingsPath, FileMode.Create, FileAccess.Write))
                {
                    _settingsRoot.WriteToStream(stream);
                }
            }
            catch 
            { }
        }

        private void SaveSelection()
        {
            try
            {
                using (var stream = new FileStream(SavedSelections, FileMode.Create, FileAccess.Write))
                {
                    _savedSelectionsDocument.WriteToStream(stream);
                }
            }
            catch
            { }
        }

        public ICommand SaveSettingsCommand { get; }
        public ICommand Duplicate { get; }
        public RelayCommand Delete { get; }

        public IEnumerable<Mod> Mods => _mods;

        public Mod SelectedMod
        {
            get { return _selectedMod; }
            set
            {
                Set(ref _selectedMod, value);
            }
        }

        public ModFile SelectedModFile
        {
            get { return _selectedModFile; }
            set {
                Set(ref _selectedModFile, value);
                SelectedConflict = null;
            }
        }


        public Mod SelectedConflict
        {
            get { return _selectedModConflict; }
            set {
                Set(ref _selectedModConflict, value);
                if (_selectedModConflict != null)
                    ComparisonContext = new ComparisonContext(SelectedModFile, _selectedModConflict.Files.FirstOrDefault(mf => mf.Path == SelectedModFile.Path));
                else
                    ComparisonContext = null;
            }
        }

        public ComparisonContext ComparisonContext
        {
            get { return _comparisonContext; }
            set
            {
                Set(ref _comparisonContext, value);
            }
        }

        public ICommand MergeModsCommand { get; }
    }
}
