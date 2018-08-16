using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;
using NLog;
using PDXModLib.Interfaces;
using PDXModLib.ModData;
using PDXModLib.Utility;
using static CWTools.Parser.Types;
using CWTools.CSharp;
using static CWTools.Process.CK2Process;
using CWTools.Process;
using Microsoft.FSharp.Compiler;

namespace PDXModLib.GameContext
{
    public class GameContext : IGameContext
    {
        private readonly ILogger _logger;

        private const string SavedSelectionKey = "CurrentlySaved";
        private const string SelectionsKey = "Selections";

        #region Private fields

        private readonly IGameConfiguration _gameConfiguration;
        private readonly INotificationService _notificationService;

        private ModSelection _currentlySaved;

        private readonly IInstalledModManager _installedModManager;

        private EventRoot _settingsRoot;

        #endregion Private fields

        #region Public properties

        public IEnumerable<Mod> Mods => _installedModManager.Mods;

		private List<ModSelection> _selections = new List<ModSelection>();
		public IReadOnlyList<ModSelection> Selections => _selections;

        public ModSelection CurrentSelection { get; set; }

		#endregion Public properties

		static GameContext()
		{
			Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
		}

		public GameContext(IGameConfiguration gameConfiguration, INotificationService notificationService, IInstalledModManager installedModManager, ILogger logger)
        {
			_logger = logger;
			_gameConfiguration = gameConfiguration;
            _notificationService = notificationService;
            _installedModManager = installedModManager;
        }

        #region Public methods

        public async Task<bool> Initialize()
        {
            try
            {
				_logger.Debug($"Loading settings from {_gameConfiguration.SettingsPath}");

				if (null == (_settingsRoot = LoadGameSettings(_gameConfiguration.SettingsPath)))
                {
					_logger.Debug($"Settings not loading, attempting backup");

					if (File.Exists(_gameConfiguration.BackupPath))
                    {
						_logger.Debug($"Backup exists, asking user.");

						if (await _notificationService.RequestConfirmation("Settings.txt corrupted, backup available, reload from backup?", "Error"))
                        {
							_logger.Debug($"Loading backup from {_gameConfiguration.BackupPath}.");

							_settingsRoot = LoadGameSettings(_gameConfiguration.BackupPath);
                        }
                        else
                        {
							_logger.Debug($"User declined, exiting.");

							return false;
                        }
                    }
                    // haven't managed to load from backup
                    if (_settingsRoot == null)
                    {
						_logger.Debug($"Loading failed, notifying user and exiting");

						await _notificationService.ShowMessage(
                            "Settings.txt corrupted - no backup available, please run stellaris to recreate default settings.", "Error");
                        return false;
                    }

                    SaveSettings();

					// this is a backup of an incorrect file, we should delete it. 
					File.Delete(_gameConfiguration.BackupPath);
                }

                _installedModManager.Initialize();
                LoadSavedSelection();
            }
            catch (Exception ex)
            {
                await _notificationService.ShowMessage(ex.Message, "Error");
                return false;
            }

            return true;
        }

        public bool SaveSettings()
        {
            try
            {
				_logger.Debug($"Saving settings");
				_currentlySaved = CurrentSelection;
                SaveSelection();

				var mods = _settingsRoot.Child("last_mods");

				if (mods == null)
				{
					_logger.Debug($"No mods selected previously, creating a default selection node");

					mods = new Node("last_mods");
					_settingsRoot.AllChildren = _settingsRoot.AllChildren.Concat( new[] { Child.NewNodeC(mods.Value) }).ToList();
				}
				
				mods.Value.AllChildren = CurrentSelection.Contents.Select(c => Child.NewLeafValueC(new LeafValue(Value.NewQString(c.Key), Range.range0))).ToList();


                if (File.Exists(_gameConfiguration.BackupPath))
                {
					_logger.Debug($"Deleting backup path at {_gameConfiguration.BackupPath}");
					File.Delete(_gameConfiguration.BackupPath);
                }

				_logger.Debug($"Creating backup at {_gameConfiguration.BackupPath}");
				File.Move(_gameConfiguration.SettingsPath, _gameConfiguration.BackupPath);

				var visitor = new PrintingVisitor();

				visitor.Visit(_settingsRoot);

				_logger.Debug($"Saving game selection at {_gameConfiguration.SettingsPath}");

				File.WriteAllText(_gameConfiguration.SettingsPath, visitor.Result);
            }
            catch (Exception ex)
            {
				_logger.Error(ex, "Error saving game settings");
                return false;
            }

            return true;
        }

        public bool SaveSelection()
        {
            try
            {
				_logger.Debug($"Saving selections");

				var selectionsToSave = new Node("root");
				var selections = new Node(SelectionsKey);

				var savedSelection = Child.NewLeafC(new Leaf(SavedSelectionKey, Value.NewQString(_currentlySaved?.Name), Range.range0));

				selections.AllChildren = Selections.Select(s =>
				{

					var r = new Node($"\"{s.Name}\"");
					r.AllChildren = s.Contents.Select(c => Child.NewLeafValueC(new LeafValue(Value.NewQString(c.Key), Range.range0))).ToList();
					return Child.NewNodeC(r);
				}).ToList();

				selectionsToSave.AllChildren = 
					new[] { Child.NewNodeC(selections), savedSelection }.ToList();


				var visitor = new PrintingVisitor();

				visitor.Visit(selectionsToSave);

				_logger.Debug($"Writing all selections to {_gameConfiguration.SavedSelections}");

				File.WriteAllText(_gameConfiguration.SavedSelections, visitor.Result);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error saving mod selection settings");
                return false;
            }

            return true;
        }

        public Task<bool> SaveMergedMod(MergedMod mod, bool mergedFilesOnly)
        {
            return _installedModManager.SaveMergedMod(mod, mergedFilesOnly);
        }

        private void LoadSavedSelection()
        {
			_logger.Debug($"Attempting to load saved selections from {_gameConfiguration.SavedSelections}");
            if (File.Exists(_gameConfiguration.SavedSelections))
            {
				_logger.Debug($"Settings file exists, parsing.");

				var adapter = CWToolsAdapter.Parse(_gameConfiguration.SavedSelections);

				if (adapter.Root != null) 
				{
					UpgradeFormat(adapter.Root);

					var selectionIdx = adapter.Root.Leafs(SavedSelectionKey).FirstOrDefault().Value.ToRawString();

					_logger.Debug($"Current selection was previously saved as {selectionIdx}");

					var selections = adapter.Root.Child(SelectionsKey).Value?.AllChildren ?? Enumerable.Empty<Child>();

                    foreach (var selection in selections.Where(s => s.IsNodeC).Select(s => s.node))
                    {
						var key = selection.Key.Trim('"');
                        ModSelection modSelection;
                        if (selection.Key.Equals(selectionIdx))
                        {
                            modSelection = CreateDefaultSelection(key);
                            CurrentSelection = modSelection;
                        }
                        else
                        {
                            modSelection = CreateFromScObject(key, selection.AllChildren);
                        }

                        _selections.Add(modSelection);
                    }
                }
            }

            // only happens if the config file couldn't have been loaded
            if (CurrentSelection == null)
            {
				_logger.Debug($"Settings file does not exist, creating default selection.");
                CurrentSelection = CreateDefaultSelection();
				_selections.Add(CurrentSelection);
            }

            _currentlySaved = CurrentSelection;
            SaveSelection();
        }

        public void LoadMods()
        {
            _installedModManager.LoadMods();
        }

        public void DeleteCurrentSelection()
        {
			_selections.Remove(CurrentSelection);
            CurrentSelection = Selections.FirstOrDefault();
            if (!Selections.Contains(_currentlySaved))
            {
                _currentlySaved = null;
            }
            SaveSelection();
        }

        public void DuplicateCurrentSelection(string newName)
        {
			_logger.Debug($"Duplicating current selection and naming it {newName}");
            var sel = new ModSelection(newName);
            sel.Contents.AddRange(CurrentSelection.Contents);
            CurrentSelection = sel;
			_selections.Add(sel);
            SaveSelection();
        }

        #endregion Public methods

        #region Private methods

        private EventRoot LoadGameSettings(string path)
        {
			var result = CWTools.Parser.CKParser.parseEventFile(path);

			if (result.IsFailure)
				return null;

			var root = CWTools.Process.CK2Process.processEventFile(result.GetResult());

			if (!root.All.Any())
				return null;
				
            return root;
        }

        private void UpgradeFormat(Node selectionsDocument)
        {
            //Upgrade from Stellaris only names; 
            var upgradeNeeded = selectionsDocument.Child("SavedToStellaris") != null;
            if (upgradeNeeded)
            {
				_logger.Debug($"Upgrading selection from old Stellaris format");

				var newNode = new Node(SavedSelectionKey);
				newNode.AllChildren = selectionsDocument.Child("SavedToStellaris").Value.AllChildren;

				var replacements = selectionsDocument.AllChildren.Where(f => !(f.IsNodeC && f.node.Key == "SavedToStellaris")).ToList(); ;

				replacements.Add(Child.NewNodeC(newNode));

				selectionsDocument.AllChildren = replacements;
            }

			var upgradeSelectionKeys = selectionsDocument.Child(SelectionsKey).Value?.Nodes.All(c => c.Key.StartsWith("\"") && c.Key.EndsWith("\"")) ?? false;
			if (upgradeSelectionKeys)
			{
				_logger.Debug($"Upgrading selection from old parser format");
				var selections = selectionsDocument.Child(SelectionsKey).Value;
				var nodes = selections.Nodes.ToList();
				var newChildren = new List<Child>();
				foreach (var node in nodes)
				{
					var nn = new Node(node.Key.Trim('"'), Range.range0);
					nn.AllChildren = node.AllChildren;
					newChildren.Add(Child.NewNodeC(nn));
				}
				selections.AllChildren = newChildren;
			}
			var ss2 = selectionsDocument.Child(SelectionsKey).Value;
		}

        private ModSelection CreateDefaultSelection(string name = "Default selection")
        {
            return CreateFromScObject(name, _settingsRoot.Child("last_mods")?.Value.AllChildren ?? Enumerable.Empty<Child>());
        }

        private ModSelection CreateFromScObject(string name, IEnumerable<Child> contents)
        {
			_logger.Debug($"Creating selection named {name}");
            var selection = new ModSelection(name);

            foreach (var mod in contents.Where(c => c.IsLeafValueC).Select(c => c.lefavalue))
            {
                var installed = _installedModManager.Mods.FirstOrDefault(m => m.Key == mod.Value.ToRawString());
                if (installed != null)
                    selection.Contents.Add(installed);
            }
            return selection;
        }

        #endregion Private methods
    }
}
