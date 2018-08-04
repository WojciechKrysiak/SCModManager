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
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

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

		public GameContext(IGameConfiguration gameConfiguration, INotificationService notificationService, IInstalledModManager installedModManager)
        {
            _gameConfiguration = gameConfiguration;
            _notificationService = notificationService;
            _installedModManager = installedModManager;
        }

        #region Public methods

        public async Task<bool> Initialize()
        {
            try

            {
                if (null == (_settingsRoot = LoadGameSettings(_gameConfiguration.SettingsPath)))
                {
                    if (File.Exists(_gameConfiguration.BackupPath))
                    {
                        if (await _notificationService.RequestConfirmation("Settings.txt corrupted, backup available, reload from backup?", "Error"))
                        {
                            _settingsRoot = LoadGameSettings(_gameConfiguration.BackupPath);
                        }
                        else
                        {
                            return false;
                        }
                    }
                    // haven't managed to load from backup
                    if (_settingsRoot == null)
                    {
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
                _currentlySaved = CurrentSelection;
                SaveSelection();

				var mods = _settingsRoot.Child("last_mods");

				if (mods == null)
				{
					mods = new Node("last_mods");
					_settingsRoot.AllChildren = _settingsRoot.AllChildren.Concat( new[] { Child.NewNodeC(mods.Value) }).ToList();
				}
				
				mods.Value.AllChildren = CurrentSelection.Contents.Select(c => Child.NewLeafValueC(new LeafValue(Value.NewQString(c.Key), Range.range0))).ToList();


                if (File.Exists(_gameConfiguration.BackupPath))
                {
                    File.Delete(_gameConfiguration.BackupPath);
                }
                File.Move(_gameConfiguration.SettingsPath, _gameConfiguration.BackupPath);

				var visitor = new PrintingVisitor();

				visitor.Visit(_settingsRoot);

				File.WriteAllText(_gameConfiguration.SettingsPath, visitor.Result);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error saving game settings");
                return false;
            }

            return true;
        }

        public bool SaveSelection()
        {
            try
            {
				var selectionsToSave = new Node("root");
				var selections = new Node(SelectionsKey);

				var savedSelection = Child.NewLeafC(new Leaf(SavedSelectionKey, Value.NewQString(_currentlySaved?.Name), Range.range0));


				var children = Selections.Select(s =>
				{
					var r = new Node(s.Name);
					r.AllChildren = s.Contents.Select(c => Child.NewLeafValueC(new LeafValue(Value.NewQString(c.Key), Range.range0))).ToList();
					return Child.NewNodeC(r);
				});

				selectionsToSave.AllChildren = 
					new[] { Child.NewNodeC(selections), savedSelection }.Concat(children).ToList();


				var visitor = new PrintingVisitor();

				visitor.Visit(selectionsToSave);

				File.WriteAllText(_gameConfiguration.SavedSelections, visitor.Result);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error saving mod selection settings");
                return false;
            }

            return true;
        }

        public Task<bool> SaveMergedMod(MergedMod mod)
        {
            return _installedModManager.SaveMergedMod(mod);
        }

        private void LoadSavedSelection()
        {
            if (File.Exists(_gameConfiguration.SavedSelections))
            {
               // using (var stream = new FileStream(_gameConfiguration.SavedSelections, FileMode.Open, FileAccess.Read))
                {
					var adapter = CWToolsAdapter.Parse(_gameConfiguration.SavedSelections);

					if (adapter.Root != null) 
					{
						UpgradeFormat(adapter.Root);

						var selectionIdx = adapter.Root.Leafs(SavedSelectionKey).FirstOrDefault().Value.ToRawString();

                        var selections = adapter.Root.Child(SelectionsKey).Value?.AllChildren ?? Enumerable.Empty<Child>();

                        foreach (var selection in selections.Where(s => s.IsNodeC).Select(s => s.node))
                        {
							var key = selection.Key;
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
            }

            // only happens if the config file couldn't have been loaded
            if (CurrentSelection == null)
            {
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
            //using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
				var result = CWTools.Parser.CKParser.parseEventFile(path);

				if (result.IsFailure)
					return null;

				var root = CWTools.Process.CK2Process.processEventFile(result.GetResult());

				if (!root.All.Any())
					return null;
				
                return root;
            }
        }

        private void UpgradeFormat(Node selectionsDocument)
        {
            //Upgrade from Stellaris only names; 
            var upgradeNeeded = selectionsDocument.Child("SavedToStellaris") != null;
            if (upgradeNeeded)
            {
				var newNode = new Node(SavedSelectionKey);
				newNode.AllChildren = selectionsDocument.Child("SavedToStellaris").Value.AllChildren;

				var replacements = selectionsDocument.AllChildren.Where(f => !(f.IsNodeC && f.node.Key == "SavedToStellaris")).ToList(); ;

				replacements.Add(Child.NewNodeC(newNode));

				selectionsDocument.AllChildren = replacements;
            }

			var upgradeSelectionKeys = selectionsDocument.Child(SelectionsKey).Value?.Nodes.All(c => c.Key.StartsWith("\"") && c.Key.EndsWith("\"")) ?? false;
			if (upgradeSelectionKeys)
			{
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
            return CreateFromScObject(name, _settingsRoot.Child("last_mods").Value?.AllChildren ?? Enumerable.Empty<Child>());
        }

        private ModSelection CreateFromScObject(string name, IEnumerable<Child> contents)
        {
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
