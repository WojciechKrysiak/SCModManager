using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using NLog;
using Ionic.Zip;
using SCModManager.DiffMerge;
using SCModManager.SteamWorkshop;
using System.Threading;
using System.Linq.Expressions;
using System.Reactive.Subjects;
using PDXModLib.GameContext;
using PDXModLib.Interfaces;
using PDXModLib.ModData;
using PDXModLib.SCFormat;
using ReactiveUI;
using SCModManager.Configuration;
using SCModManager.ViewModels;
using SCModManager.Views;

namespace SCModManager
{
    class ModContext : ReactiveObject
    {
        private System.Configuration.Configuration _configuration;
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private GameContext _gameContext;
        private IModConflictCalculator _modConflictCalculator;
        private GameConfigurationSection _gameConfiguration;

        private readonly Subject<bool> _canMerge = new Subject<bool>();
        private readonly Subject<bool> _canDelete = new Subject<bool>();
        private ModConflictPreviewVm _conflictPreviewVm;

        public enum Filter_Type { All, Selected, Unselected };

        private Filter_Type _current_filter_type;

        private readonly string product;

        private List<ModVM> _mods = new List<ModVM>();

        private List<ModConflictDescriptor> _modConflicts = new List<ModConflictDescriptor>();

        public ICommand SaveSettingsCommand { get; }
        public ICommand Duplicate { get; }
        public ICommand Delete { get; }
        public ICommand MergeModsCommand { get; }
        public ICommand ShowPreferences { get; }

        public IEnumerable<ModVM> Mods => _mods.Where(mvm => Custom_Current_Filter(mvm.Mod));
        public IEnumerable<ModSelection> Selections => _gameContext.Selections.ToArray();

        private Window mergeWindow;
        private string _errorReason;
        private bool _conflictsMode;
        private ModVM _selectedMod;

        private Func<Mod, bool> Custom_Current_Filter {
            get {
                switch (_current_filter_type) {
                    case Filter_Type.All:
                        return IncludeAll;
                    case Filter_Type.Selected:
                        return IsSelected;
                    case Filter_Type.Unselected:
                        return IsUnselected;
                    default:
                        return IncludeAll;
                }
            }
        }

        public ModSelection CurrentSelection
        {
            get { return _gameContext.CurrentSelection; }
            set
            {
                if (_gameContext.CurrentSelection != value)
                {
                    _gameContext.CurrentSelection = value;
                    UpdateSelections();
                    _conflictPreviewVm?.ApplyModFilter(Custom_Current_Filter);
                    this.RaisePropertyChanged();
                }
            }
        }

        public ModVM SelectedMod
        {
            get { return _selectedMod; }
            set
            {
                this.RaiseAndSetIfChanged(ref _selectedMod, value);

                if (value == null)
                {
                    _mods.ForEach(m => m.HasConflictWithSelected = false);
                    ConflictPreviewVm = null;
                }
                else
                {
                    _mods.ForEach(m => m.HasConflictWithSelected = value.ModConflict.ConflictingMods.Contains(m.Mod));
                    ConflictPreviewVm = new ModConflictPreviewVm(value.ModConflict, Custom_Current_Filter);
                }
                
            }
        }

        public ModConflictPreviewVm ConflictPreviewVm
        {
            get { return _conflictPreviewVm; }
            set { this.RaiseAndSetIfChanged(ref _conflictPreviewVm, value); }
        }

        public string ErrorReason
        {
            get { return _errorReason; }
            set
            {
                ConflictMode = true;
                this.RaiseAndSetIfChanged(ref _errorReason, value);
            }
        }

        public bool ConflictMode
        {
            get { return _conflictsMode; }
            set
            {
                this.RaiseAndSetIfChanged(ref _conflictsMode, value);
            }
        }

        public Filter_Type Custom_Filter_Selection {
            get { return _current_filter_type; }
            set {
                this.RaiseAndSetIfChanged(ref _current_filter_type, value);
                _mods.ForEach(m => m.UpdateModFilter(Custom_Current_Filter));
                _conflictPreviewVm?.ApplyModFilter(Custom_Current_Filter);
                this.RaisePropertyChanged(nameof(Mods));
            }
        }

        public ModContext(string product)
        {
            this.product = product;
            SaveSettingsCommand = ReactiveCommand.Create(() => _gameContext.SaveSettings());
            MergeModsCommand = ReactiveCommand.Create(MergeMods, _canMerge);
            Duplicate = ReactiveCommand.Create(DoDuplicate);
            Delete = ReactiveCommand.Create(DoDelete, _canDelete);
            ShowPreferences = ReactiveCommand.Create(DoShowPreferences);
            _configuration = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
        }

        public bool Initialize()
        {
            LoadConfiguration(product);

            var notificationService = new NotificationService();
            var installedModManager = new InstalledModManager(_gameConfiguration, notificationService);
            _gameContext = new GameContext(_gameConfiguration, notificationService, installedModManager);

            _modConflictCalculator = new ModConflictCalculator(_gameConfiguration, installedModManager);

            if (!_gameContext.Initialize())
            {
                return false;
            }

            SortAndCreateViewModels();

            SteamWebApiIntegration.LoadModDescriptors(_mods, (s) => { });
            
            _canDelete.OnNext(_gameContext.Selections.Count > 1);
            return true;
        }

        private bool DoModsNeedToBeMerged()
        {
            return Mods?.Where(mvm => mvm.Selected).Any(mvm => mvm.ModConflict.ConflictingMods.Any(IsSelected))?? false;
        }

        private void SortAndCreateViewModels()
        {
            _modConflicts = _modConflictCalculator.CalculateAllConflicts().ToList();

            _mods?.ForEach(mvm =>
            {
                mvm.PropertyChanged -= ModOnPropertyChanged;
                mvm.Mod.Dispose();
            });
            _mods = _modConflicts.Select(mc => new ModVM(mc, IsSelected(mc.Mod))).OrderBy(m => m.Name).ToList();
            _mods.ForEach(mvm => mvm.PropertyChanged += ModOnPropertyChanged);

            this.RaisePropertyChanged(nameof(Mods));

            _canMerge.OnNext(DoModsNeedToBeMerged());
        }

        private void DoDelete()
        {
            var selection = CurrentSelection;

            if (MessageBox.Show($"Are you sure you want to delete {selection.Name}?", "Confirm deletion", MessageBoxButton.OKCancel, MessageBoxImage.Question) == MessageBoxResult.OK)
            {
                _gameContext.DeleteCurrentSelection();
                _canDelete.OnNext(_gameContext.Selections.Count > 1);
                this.RaisePropertyChanged(nameof(CurrentSelection));
                this.RaisePropertyChanged(nameof(Selections));
            }
        }

        private void DoDuplicate()
        {
            int cnt = _gameContext.Selections.Count();

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

            _gameContext.DuplicateCurrentSelection(name);

            this.RaisePropertyChanged(nameof(Selections));
            this.RaisePropertyChanged(nameof(CurrentSelection));
        }

        private void MergeMods()
        {
            mergeWindow = new Merge();
            mergeWindow.Closed += MergeWindow_Closed;
            mergeWindow.DataContext = new ModMergeContext(Mods.Where(m => m.Selected).Select(m => m.ModConflict.Filter(IsSelected)), SaveMergedMod);
            mergeWindow.ShowDialog();
        }

        private void MergeWindow_Closed(object sender, EventArgs e)
        {
            mergeWindow.Closed -= MergeWindow_Closed;
            mergeWindow = null;
        }

        private void SaveMergedMod(ModMergeContext mod)
        {

            if (!_gameContext.SaveMergedMod(mod.Result))
            {
                MessageBox.Show("Error saving merged mod file. Please check the log file", "Error",
                    MessageBoxButton.OK);
                return;
            }

            mergeWindow.Close();
            _gameContext.LoadMods();
            SortAndCreateViewModels();
        }

        private bool IsSelected(Mod mod)
        {
            return CurrentSelection.Contents.Contains(mod);
        }

        private bool IsUnselected(Mod mod) {
            return !CurrentSelection.Contents.Contains(mod);
        }

        private static bool IncludeAll(Mod mod) => true;

        private void ModOnPropertyChanged(object sender, PropertyChangedEventArgs propertyChangedEventArgs)
        {
            if (propertyChangedEventArgs.PropertyName == nameof(ModVM.Selected))
            {
                var modConflict = sender as ModVM;
                var mod = modConflict.Mod;
                if (modConflict.Selected)
                {
                    if (!CurrentSelection.Contents.Contains(mod))
                        CurrentSelection.Contents.Add(mod);
                }
                else
                {
                    CurrentSelection.Contents.Remove(mod);
                }
                _gameContext.SaveSelection();
                _canMerge.OnNext(DoModsNeedToBeMerged());
                this.RaisePropertyChanged(nameof(Mods));
            }
        }

        private void UpdateSelections()
        {
            _mods.ForEach(modVm => modVm.Selected = IsSelected(modVm.Mod));
            this.RaisePropertyChanged(nameof(Mods));
        }

        private void DoShowPreferences()
        {
            var vm = new PreferencesWindowViewModel(_gameConfiguration);
            var pw = new PreferencesWindow {DataContext = vm};

            vm.ShouldClose += (sender, save) =>
            {
                if (save)
                {
                    _configuration.Save(ConfigurationSaveMode.Full);
                    Initialize();
                }
                pw.Close();
            };
            pw.ShowDialog();
        }

        private void LoadConfiguration(string game)
        {
            var section = _configuration.Sections[game] as GameConfigurationSection;
            
            if (section == null)
            {
                section = new GameConfigurationSection(new StellarisConfiguration());
                _configuration.Sections.Add(game, section);
                _configuration.Save(ConfigurationSaveMode.Full);
            }

            _gameConfiguration = section;

            if (string.IsNullOrEmpty(_gameConfiguration.BasePath) || !Directory.Exists(_gameConfiguration.BasePath) || !File.Exists(_gameConfiguration.SettingsPath))
            {
                MessageBox.Show(
                    $"There is an error configuration, please sellect a valid documents directory for {game}");
                DoShowPreferences();
            }
        }
    }
}
