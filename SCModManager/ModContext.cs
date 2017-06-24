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
using SCModManager.DiffMerge;
using SCModManager.ModData;
using SCModManager.SteamWorkshop;
using System.Threading;
using System.Linq.Expressions;

namespace SCModManager
{

    internal class ModConflictSelection : ObservableObject 
    {
        private Mod _mod;
        private bool _selected;
        private IEnumerable<Mod> _conflictingMods;

        public Mod Mod => _mod;

        public string Name => _mod.Name;

        public int ConflictCount => _conflictingMods.Count();

        public IEnumerable<Mod> ConflictingMods => _conflictingMods;

        public bool HasConflict => ConflictCount > 0;

        public bool ParseError => _mod.ParseError;

        public IEnumerable<ModFile> Files => _mod.Files;

        public bool Selected
        {
            get { return _selected; }
            set { Set(ref _selected, value); }
        }

        public ModConflictSelection(Mod mod, IEnumerable<Mod> conflictingMods, bool selected)
        {
            _mod = mod;
            _selected = selected;
            _conflictingMods = conflictingMods;
        }
    }

    class ModContext : ObservableObject
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private static readonly string BasePath = $"{Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)}\\Paradox Interactive\\Stellaris";

        private static readonly string ModsDir = $"{BasePath}\\mod";
        private static readonly string SettingsPath = $"{BasePath}\\settings.txt";
        private static readonly string SavedSelections = $"{BasePath}\\saved_selections.txt";

        private readonly SCObject _settingsRoot;

        private readonly SCObject _savedSelectionsDocument;

        private List<Mod> _mods = new List<Mod>();

        ModFile _selectedModFile;

        public ICommand SaveSettingsCommand { get; }
        public ICommand Duplicate { get; }
        public RelayCommand Delete { get; }
        public RelayCommand MergeModsCommand { get; }

        public IEnumerable<Mod> Mods => _mods;

        List<ModConflictSelection> _conflicts;

        public IEnumerable<ModConflictSelection> ModConflicts => _conflicts; 

        private IEnumerable<Mod> CalculateConflicts(Mod mod, List<Mod> _mods)
        {
            return _mods.Except(new[] { mod }).Where(m => m.Files.Any(mf => mod.Files.Any(mff => mff.Path == mf.Path))).ToList();
        } 

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
                
                if (obj != null && _conflicts != null)
                {
                    foreach (var conflict in _conflicts)
                    {
                        conflict.Selected = obj?.Any(kvp => (kvp.Value as SCString)?.Text == conflict.Mod.Key) ?? false;
                    }
                }
                RaisePropertyChanged();
            }
        }

        public ModConflictSelection SelectedModConflict
        {
            get { return _selectedModConflict; }
            set
            {
                Set(ref _selectedModConflict, value);
                RaisePropertyChanged(nameof(SelectedModFileTree));
            }
        }

        public IEnumerable<ModFileHolder> SelectedModFileTree
        {
            get
            {
                if (_selectedModConflict != null)
                {
                    var mfr = new ModDirectory(string.Empty, 0, _selectedModConflict.Files, ModFileHasConflicts);
                    return mfr.Files;
                }
                return null;
            }
        }

        private bool ModFileHasConflicts(ModFile modFile)
        {
            return _mods.Except(new[] { modFile.SourceMod }).SelectMany(m => m.Files).Any(mf => mf.Path == modFile.Path);
        }

        public ModFile SelectedModFile
        {
            get { return _selectedModFile; }
            set
            {
                Set(ref _selectedModFile, value);
                SelectedConflict = null;
                RaisePropertyChanged(nameof(SelectedConflictMods));
            }
        }

        public IEnumerable<Mod> SelectedConflictMods => SelectedModConflict?.ConflictingMods.Where(m => m.Files.Any(mf => mf.Path == SelectedModFile?.Path));

        public Mod SelectedConflict
        {
            get { return _selectedConflict; }
            set
            {
                Set(ref _selectedConflict, value);
                if (_selectedConflict != null)
                    ComparisonContext = new ComparisonContext(SelectedModFile, _selectedConflict.Files.FirstOrDefault(mf => mf.Path == SelectedModFile.Path));
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

        public string ErrorReason
        {
            get { return _errorReason; }
            set
            {
                Set(ref _errorReason, value);
                ConflictMode = true;
            }
        }

        public bool ConflictMode
        {
            get { return _conflictsMode; }
            set
            {
                Set(ref _conflictsMode, value);
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
                        var stellaris_selection = _settingsRoot["last_mods"] ?? new SCObject();
                        var selection = new SCKeyValObject(selectionIdx, stellaris_selection);
                        Selections[selectionIdx] = stellaris_selection;
                        CurrentSelection = selection;
                    }
                }

                SortAndUpdate();

                SaveSettingsCommand = new RelayCommand(SaveSettings);
                MergeModsCommand = new RelayCommand(MergeMods, DoModsNeedToBeMerged);
                Duplicate = new RelayCommand(DoDuplicate);
                Delete = new RelayCommand(DoDelete, () => Selections.Count() > 1);


                SteamWebApiIntegration.LoadModDescriptors(Mods, ConnectionError);

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ConnectionError(string obj)
        {
            ErrorReason = obj;
        }

        private bool DoModsNeedToBeMerged()
        {
            var selectedMods = _conflicts.Where(c => c.Selected).Select(c => c.Mod).ToList();

            if (selectedMods.Count < 2)
            {
                return false;
            }

            return selectedMods.Any(sm => CalculateConflicts(sm, selectedMods).Count() > 0);
        }

        private void SortAndUpdate()
        {
            _mods = Mods.OrderBy(m => m.Name).ToList();
            _conflicts?.ForEach(mod => mod.PropertyChanged -= ModOnPropertyChanged);
            _conflicts = _mods.Select(m => new ModConflictSelection(m, CalculateConflicts(m, _mods), IsSelected(m))).ToList();
            _conflicts.ForEach(mod => mod.PropertyChanged += ModOnPropertyChanged);
            RaisePropertyChanged(nameof(ModConflicts));
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
            if (Directory.Exists(ModsDir))
            {
                foreach (var file in Directory.EnumerateFiles(ModsDir, "*.mod"))
                {
                    var fileName = Path.GetFileName(file);

                    if (_mods.Any(m => m.Id == fileName))
                    {
                        continue;
                    }

                    Mod mod = null;
                    try
                    {
                        mod = Mod.Load(file);
                        mod.LoadFiles(BasePath);
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
        private ModConflictSelection _selectedModConflict;
        private ComparisonContext _comparisonContext;
        private Mod _selectedConflict;
        private string _errorReason;
        private bool _conflictsMode;

        private void MergeMods()
        {
            mergeWindow = new Merge();
            mergeWindow.Closed += MergeWindow_Closed;
            mergeWindow.DataContext = new ModMergeContext(_conflicts.Where(m => m.Selected).Select(m=>m.Mod), SaveMergedMod);
            mergeWindow.ShowDialog();
        }

        private void MergeWindow_Closed(object sender, EventArgs e)
        {
            mergeWindow.Closed -= MergeWindow_Closed;
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
            mergeWindow.Close();
            SortAndUpdate();
        }

        private bool IsSelected(Mod mod)
        {
            return (CurrentSelection?.Value as SCObject)?.Any(kvp => (kvp.Value as SCString)?.Text == mod.Key) ?? false;
        }

        private void ModOnPropertyChanged(object sender, PropertyChangedEventArgs propertyChangedEventArgs)
        {
            if (propertyChangedEventArgs.PropertyName == nameof(ModConflictSelection.Selected))
            {
                var modConflict = sender as ModConflictSelection;
                var mod = modConflict.Mod;
                if (modConflict.Selected)
                {
                    if (!(CurrentSelection.Value as SCObject).Any(kvp => (kvp.Value as SCString)?.Text == mod.Key))
                        (CurrentSelection.Value as SCObject).Add(SCKeyValObject.Create(mod.Key));
                }
                else
                {
                    var selected = (CurrentSelection.Value as SCObject).FirstOrDefault(kvp => (kvp.Value as SCString)?.Text == mod.Key);
                    (CurrentSelection.Value as SCObject).Remove(selected);
                }
                SaveSelection();
                MergeModsCommand?.RaiseCanExecuteChanged();
            }
        }

        private void SaveSettings()
        {
            try
            {
                _savedSelectionsDocument["SavedToStellaris"] = new SCString(CurrentSelection.Key.ToString());
                SaveSelection();

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
    }
}
