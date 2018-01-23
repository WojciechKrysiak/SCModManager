using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Mime;
using System.Text;
using NLog;
using PDXModLib.Interfaces;
using PDXModLib.ModData;
using PDXModLib.SCFormat;
using PDXModLib.Utility;

namespace PDXModLib.GameContext
{
    public class GameContext
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private const string SavedSelectionKey = "CurrentlySaved";
        private const string SelectionsKey = "Selections";

        #region Private fields

        private readonly IGameConfiguration _gameConfiguration;
        private readonly INotificationService _notificationService;

        private ModSelection _currentlySaved;

        private readonly IInstalledModManager _installedModManager;

        private SCObject _settingsRoot;

        #endregion Private fields

        #region Public properties

        public IEnumerable<Mod> Mods => _installedModManager.Mods;

        public List<ModSelection> Selections { get; } = new List<ModSelection>();

        public ModSelection CurrentSelection { get; set; }

        #endregion Public properties

        public GameContext(IGameConfiguration gameConfiguration, INotificationService notificationService, IInstalledModManager installedModManager)
        {
            _gameConfiguration = gameConfiguration;
            _notificationService = notificationService;
            _installedModManager = installedModManager;
        }

        #region Public methods

        public bool Initialize()
        {
            try

            {
                if (null == (_settingsRoot = LoadGameSettings(_gameConfiguration.SettingsPath)))
                {
                    if (File.Exists(_gameConfiguration.BackupPath))
                    {
                        if (_notificationService.RequestConfirmation("Settings.txt corrupted, backup available, reload from backup?", "Error"))
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
                        _notificationService.ShowMessage(
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
                _notificationService.ShowMessage(ex.Message, "Error");
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

                var mods = new SCObject();

                foreach (var sm in CurrentSelection.Contents)
                    mods.Add(SCKeyValObject.Create(sm.Key));

                _settingsRoot["last_mods"] = mods;

                if (File.Exists(_gameConfiguration.BackupPath))
                {
                    File.Delete(_gameConfiguration.BackupPath);
                }
                File.Move(_gameConfiguration.SettingsPath, _gameConfiguration.BackupPath);

                using (var stream = new FileStream(_gameConfiguration.SettingsPath, FileMode.Create, FileAccess.Write))
                {
                    _settingsRoot.WriteToStream(stream);
                }
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
                var selections = new SCObject();

                var selectionsToSave = new SCObject { [SavedSelectionKey] = new SCString(_currentlySaved?.Name), [SelectionsKey] = selections };

                foreach (var modSelection in Selections)
                {
                    var mods = new SCObject();

                    foreach (var sm in modSelection.Contents)
                        mods.Add(SCKeyValObject.Create(sm.Key));

                    selections[new SCString(modSelection.Name)] = mods;
                }

                using (var stream = new FileStream(_gameConfiguration.SavedSelections, FileMode.Create, FileAccess.Write))
                {
                    selectionsToSave.WriteToStream(stream);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error saving mod selection settings");
                return false;
            }

            return true;
        }

        public bool SaveMergedMod(MergedMod mod)
        {
            return _installedModManager.SaveMergedMod(mod);
        }

        public void LoadSavedSelection()
        {
            if (File.Exists(_gameConfiguration.SavedSelections))
            {
                using (var stream = new FileStream(_gameConfiguration.SavedSelections, FileMode.Open, FileAccess.Read))
                {
                    var selectionParser = new Parser(new Scanner(stream));

                    selectionParser.Parse();

                    if (!selectionParser.ParseError && selectionParser.Root.Any())
                    {
                        var selectionDocument = selectionParser.Root;
                        UpgradeFormat(selectionDocument);

                        var selectionIdx = selectionDocument[SavedSelectionKey] as SCString;

                        var selections = selectionDocument[SelectionsKey] as SCObject ?? new SCObject();

                        foreach (var selection in selections)
                        {
                            var key = (selection.Key as SCString).Text;
                            ModSelection modSelection;
                            if (selection.Key.Equals(selectionIdx))
                            {
                                modSelection = CreateDefaultSelection(key);
                                CurrentSelection = modSelection;
                            }
                            else
                            {
                                modSelection = CreateFromScObject(key, selection.Value);
                            }

                            Selections.Add(modSelection);
                        }
                    }
                }
            }

            // only happens if the config file couldn't have been loaded
            if (CurrentSelection == null)
            {
                CurrentSelection = CreateDefaultSelection();
                Selections.Add(CurrentSelection);
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
            Selections.Remove(CurrentSelection);
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
            Selections.Add(sel);
            SaveSelection();
        }

        #endregion Public methods

        #region Private methods

        private SCObject LoadGameSettings(string path)
        {
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                var settingsParser = new Parser(new Scanner(stream));

                settingsParser.Parse();
                if (!settingsParser.Root.Any() || settingsParser.ParseError)
                    return null;
                return settingsParser.Root;
            }
        }

        private void UpgradeFormat(SCObject selectionsDocument)
        {
            //Upgrade from Stellaris only names; 
            var upgradeNeeded = selectionsDocument["SavedToStellaris"] != null;
            if (upgradeNeeded)
            {
                selectionsDocument[SavedSelectionKey] = selectionsDocument["SavedToStellaris"];
                selectionsDocument.Remove("SavedToStellaris");
            }
        }

        private ModSelection CreateDefaultSelection(string name = "Default selection")
        {
            return CreateFromScObject(name, _settingsRoot["last_mods"]);
        }

        private ModSelection CreateFromScObject(string name, SCValue contents)
        {
            var selection = new ModSelection(name);
            var defSelection = contents as SCObject ?? new SCObject();

            foreach (var mod in defSelection)
            {
                var installed = _installedModManager.Mods.FirstOrDefault(m => m.Key == (mod.Value as SCString)?.Text);
                if (installed != null)
                    selection.Contents.Add(installed);
            }
            return selection;
        }

        #endregion Private methods
    }
}
