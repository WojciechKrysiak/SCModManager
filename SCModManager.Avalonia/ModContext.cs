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
using SCModManager.Avalonia.DiffMerge;
using SCModManager.Avalonia.SteamWorkshop;
using System.Threading;
using System.Linq.Expressions;
using System.Reactive.Subjects;
using PDXModLib.GameContext;
using PDXModLib.Interfaces;
using PDXModLib.ModData;
using ReactiveUI;
using SCModManager.Avalonia.Configuration;
using SCModManager.Avalonia.ViewModels;
using SCModManager.Avalonia.Views;
using Avalonia;
using Avalonia.Controls;
using PDXModLib.Utility;
using SCModManager.Avalonia;
using SCModManager.Avalonia.Utility;

namespace SCModManager.Avalonia
{
    public class ModContext : ReactiveObject
    {
        private IGameContext _gameContext;
        private IModConflictCalculator _modConflictCalculator;
		private readonly IAppContext _appContext;
		private IGameConfiguration _gameConfiguration;
		private readonly ILogger _logger;
		private readonly ISteamIntegration steamIntegration;
		private readonly Func<ModConflictDescriptor, bool, ModVM> newModVM;
		private readonly IShowDialog<PreferencesWindowViewModel, bool> _newPreferencesWindow;
		private readonly IShowDialog<NameConfirmVM, string, string> newNameConfirmDialog;
		private readonly IShowDialog<ModMergeViewModel, MergedMod, IEnumerable<ModConflictDescriptor>> newModMergeDialog;
		private readonly Subject<bool> _canMerge = new Subject<bool>();
        private readonly Subject<bool> _canDelete = new Subject<bool>();
        private ModConflictPreviewVm _conflictPreviewVm;
        private bool _filterSelection;

        private readonly string product;

        private List<ModVM> _mods = new List<ModVM>();

        private List<ModConflictDescriptor> _modConflicts = new List<ModConflictDescriptor>();

        public ICommand SaveSettingsCommand { get; }
		public ICommand Duplicate { get; }
        public ICommand Delete { get; }
        public ICommand MergeModsCommand { get; }
		public ICommand RunGameCommand { get; }

		public ICommand ShowPreferences { get; }

        public IEnumerable<ModVM> Mods => _mods.Where(mvm => CurrentFilter(mvm.Mod));
        public IEnumerable<ModSelection> Selections => _gameContext.Selections.ToArray();

        private string _errorReason;
        private bool _conflictsMode;
        private ModVM _selectedMod;
		private INotificationService _notificationService;

		private Func<Mod, bool> CurrentFilter
        {
            get
            {
                if (_filterSelection)
                    return IsSelected;
                return IncludeAll;
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
                    _conflictPreviewVm?.ApplyModFilter(CurrentFilter);
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
                    ConflictPreviewVm = new ModConflictPreviewVm(value.ModConflict, CurrentFilter);
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

        public bool FilterSelection
        {
            get { return _filterSelection; }
            set
            {
                this.RaiseAndSetIfChanged(ref _filterSelection, value);
                _mods.ForEach(m => m.UpdateModFilter(CurrentFilter));
                _conflictPreviewVm?.ApplyModFilter(CurrentFilter);
                this.RaisePropertyChanged(nameof(Mods));
            }
        }

        public ModContext(INotificationService notificationService, 
						  IGameContext gameContext, 
						  IModConflictCalculator modConflictCalculator, 
						  IAppContext appContext, 
						  IGameConfiguration gameConfiguration,
						  ILogger logger,
						  ISteamIntegration steamIntegration,
						  Func<ModConflictDescriptor, bool, ModVM> newModVM,
						  IShowDialog<PreferencesWindowViewModel, bool> newPreferencesWindow,
						  IShowDialog<NameConfirmVM, string, string> newNameConfirmDialog,
						  IShowDialog<ModMergeViewModel, MergedMod, IEnumerable<ModConflictDescriptor>> newModMergeDialog
			
			)
        {
			logger.Debug($"Creating ModContext for {gameConfiguration.GameName}");

			_notificationService = notificationService;
			_gameContext = gameContext;
			_modConflictCalculator = modConflictCalculator;
			_appContext = appContext;
			_gameConfiguration = gameConfiguration;
			_logger = logger;
			this.steamIntegration = steamIntegration;
			this.newModVM = newModVM;
			_newPreferencesWindow = newPreferencesWindow;
			this.newNameConfirmDialog = newNameConfirmDialog;
			this.newModMergeDialog = newModMergeDialog;
			SaveSettingsCommand = ReactiveCommand.Create(() => _gameContext.SaveSettings());
            MergeModsCommand = ReactiveCommand.Create(MergeMods, _canMerge);
            Duplicate = ReactiveCommand.Create(DoDuplicate);
            Delete = ReactiveCommand.Create(DoDelete, _canDelete);
			ShowPreferences = ReactiveCommand.Create(DoShowPreferences);
			RunGameCommand = ReactiveCommand.Create(() => steamIntegration.Run(_gameConfiguration.AppId));
        }

        public async Task<bool> Initialize()
        {
			_logger.Debug($"Initializing ModContext for {_gameConfiguration.GameName}");

			if (! await _gameContext.Initialize())
            {
                return false;
            }

            await SortAndCreateViewModels();
            
            _canDelete.OnNext(_gameContext.Selections.Count > 1);
            return true;
		}

        private bool DoModsNeedToBeMerged()
        {
            return Mods?.Where(mvm => mvm.Selected).Any(mvm => mvm.ModConflict.ConflictingMods.Any(IsSelected))?? false;
        }

        private async Task SortAndCreateViewModels()
        {
            _modConflicts = _modConflictCalculator.CalculateAllConflicts().ToList();

            _mods?.ForEach(mvm =>
            {
                mvm.PropertyChanged -= ModOnPropertyChanged;
            });

            _mods = _modConflicts.Select(mc => newModVM(mc, IsSelected(mc.Mod))).OrderBy(m => m.Name).ToList();
            _mods.ForEach(mvm => mvm.PropertyChanged += ModOnPropertyChanged);

			await steamIntegration.DownloadDescriptors(new Subject<string>());

            this.RaisePropertyChanged(nameof(Mods));

            _canMerge.OnNext(DoModsNeedToBeMerged());
        }

        private async void DoDelete()
        {
            var selection = CurrentSelection;
            
            if (await _notificationService.RequestConfirmation($"Are you sure you want to delete {selection.Name}?", "Confirm deletion"))
            {
                _gameContext.DeleteCurrentSelection();
                _canDelete.OnNext(_gameContext.Selections.Count > 1);
                this.RaisePropertyChanged(nameof(CurrentSelection));
                this.RaisePropertyChanged(nameof(Selections));
            }
        }

		private async void DoDuplicate()
		{
			int cnt = _gameContext.Selections.Count();

			var name = $"Selection {cnt + 1}";

			name = await newNameConfirmDialog.Show(name);

			if (name != null)
			{
				_gameContext.DuplicateCurrentSelection(name);

				_canDelete.OnNext(_gameContext.Selections.Count > 1);

				this.RaisePropertyChanged(nameof(Selections));
				this.RaisePropertyChanged(nameof(CurrentSelection));
			}
        }

        private async void MergeMods()
        {
			var result = await newModMergeDialog.Show(Mods.Where(m => m.Selected).Select(m => m.ModConflict.Filter(IsSelected)));

			if (result != null)
			{
				if (!await _gameContext.SaveMergedMod(result))
				{
					await _notificationService.ShowMessage("Error saving merged mod file. Please check the log file", "Error");
					return;
				}

				_gameContext.LoadMods();
				await SortAndCreateViewModels();
			}

        }

        private bool IsSelected(Mod mod)
        {
            return CurrentSelection.Contents.Contains(mod);
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
                if (_filterSelection)
                {
                    this.RaisePropertyChanged(nameof(Mods));
                }
            }
        }

        private void UpdateSelections()
        {
            _mods.ForEach(modVm => modVm.Selected = IsSelected(modVm.Mod));
            if (_filterSelection)
            {
                this.RaisePropertyChanged(nameof(Mods));
            }
        }

        private async void DoShowPreferences()
        {
			if (await _newPreferencesWindow.Show())
			{
				await Initialize();
			}
		}

		// TODO: fixme
        private async Task CheckConfiguration()
        {
            if (!_gameConfiguration.SettingsDirectoryValid || !_gameConfiguration.GameDirectoryValid)
            {
				await _notificationService.ShowMessage("error", $"There is an error configuration, please sellect a valid documents directory for {_gameConfiguration.GameName}");
                DoShowPreferences();
            }
        }
    }
}
