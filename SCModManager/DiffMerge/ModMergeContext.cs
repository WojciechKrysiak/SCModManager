using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using SCModManager.ModData;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Input;

namespace SCModManager.DiffMerge
{
    class ModMergeContext : ObservableObject
    {
        public List<Mod> BaseMods { get; }
        private ModFile _selected;

        private  bool onlyConflicts;

        private List<ModFile> modFiles = new List<ModFile>();

        private Action<ModMergeContext> saveAction;

        // public IEnumerable<ModToProcess> ModFiles => modFiles.Where(mf => mf.HasConflict);
        Dictionary<ModFile, MergeProcess> _currentProcesses = new Dictionary<ModFile, MergeProcess>();

        private IEnumerable<ModFileHolder> _modFileTree;

        public IEnumerable<ModFileHolder> ModFileTree => _modFileTree;

        public ModFile SelectedModFile
        {
            get { return _selected; }
            set
            {
                if (CurrentProcess != null)
                {
                    CurrentProcess.FileResolved -= CurrentProcess_FileResolved;
                }

                Set(ref _selected, value);

                if (CurrentProcess != null)
                {
                    CurrentProcess.FileResolved += CurrentProcess_FileResolved;
                }

                RaisePropertyChanged(nameof(CurrentProcess));
            }
        }

        private void CurrentProcess_FileResolved(object sender, EventArgs e)
        {
            var selected = _selected;
            if (!ReferenceHasConflicts(_selected))
            {
                _currentProcesses.Remove(_selected);
                SelectedModFile = null;
            }

            UpdateModList();

            Save.RaiseCanExecuteChanged();
        }

        public MergeProcess CurrentProcess
        {
            get
            {
                if (ReferenceHasConflicts(_selected))
                {
                    if (!_currentProcesses.ContainsKey(_selected))
                    {
                        _currentProcesses.Add(_selected, new MergeProcess(_selected as MergedModFile));
                    }

                    return _currentProcesses[_selected];
                }
                return null;

            }
        }

        public bool OnlyConflicts
        {
            get { return onlyConflicts; }
            set { Set(ref onlyConflicts, value); }
        }

        private MergedMod result;

        public MergedMod Result => result;

        public RelayCommand Save { get; }

        public ModMergeContext(IEnumerable<Mod> source, Action<ModMergeContext> save)
        {
            BaseMods = source.ToList();

            result = new MergedMod("Merge result", BaseMods);

            modFiles.AddRange(result.Files);

            LeftBefore = new RelayCommand<ModFile>(DoBefore);
            LeftAfter = new RelayCommand<ModFile>(DoAfter);
            RightBefore = new RelayCommand<ModFile>(DoBefore);
            RightAfter = new RelayCommand<ModFile>(DoAfter);

            saveAction = save;
            Save = new RelayCommand(SaveAction, () => !modFiles.Any(ReferenceHasConflicts));

            _modFileTree = new ModDirectory(string.Empty, 0, modFiles, ReferenceHasConflicts).Files;
        }

        private static bool ReferenceHasConflicts(ModFile mf)
        {
            return (mf as MergedModFile)?.SourceFileCount > 1;
        }

        private void SaveAction()
        {
            var confirm = new NameConfirm();
            var confirmVM = new NameConfirmVM(result.Name);
            confirmVM.ShouldClose += (o, b) =>
            {
                if (b)
                {
                    result.Name = confirmVM.Name;
                    saveAction(this);
                }
                confirm.Close();
            };

            confirm.DataContext = confirmVM;
            confirm.ShowDialog();
        }

        private List<ModNameParse> GetMatchingFiles(ModNameParse modFile)
        {
            return modFiles.Select(mf => ModNameParse.Parse(mf)).Where(mft => modFile.Filename == mft?.Filename).OrderBy(r => r.Prefix).ToList();
        }

        public ICommand LeftBefore { get; }

        private void DoBefore(ModFile modFile)
        {
            var match = ModNameParse.Parse(modFile);

            if (match?.Filename == null)
            {
                throw new InvalidDataException($"provided file path \"{modFile.Path}\" was not in en expected format - please contact the developer.");
            }

            var surroundings = GetMatchingFiles(match);

            match = surroundings.First(mnp => mnp.Path == match.Path);

            var idx = surroundings.IndexOf(match);

            if (match.Prefix == 0)
            {
                match.Prefix = 0;

                var tmpPrefix = match.Prefix;

                for (int i = idx; i < surroundings.Count; i++)
                {
                    if (surroundings[i].Prefix - tmpPrefix <= 1)
                    {
                        tmpPrefix = surroundings[i].Prefix;
                        surroundings[i].Prefix++;
                    }
                    else
                        break;
                }
            }
            else
            {
                match.Prefix -= 1;
            }

            CurrentProcess.Remove(modFile);

            modFile.Path = match.Path;

            modFiles.Add(modFile);

            UpdateModList();

            return;
        }

        public ICommand LeftAfter { get; }

        private void DoAfter(ModFile modFile)
        {
            var match = ModNameParse.Parse(modFile);

            if (match?.Filename == null)
            {
                throw new InvalidDataException($"provided file path \"{modFile.Path}\" was not in en expected format - please contact the developer.");
            }

            var surroundings = GetMatchingFiles(match);

            match = surroundings.First(mnp => mnp.Path == match.Path);

            var idx = surroundings.IndexOf(match);

            if (match.Prefix == ModNameParse.MaxNum)
            {
                match.Prefix = ModNameParse.MaxNum;

                var tmpPrefix = match.Prefix;

                for (int i = idx; i >= 0; i--)
                {
                    if (tmpPrefix - surroundings[i].Prefix  <= 1)
                    {
                        tmpPrefix = surroundings[i].Prefix;
                        surroundings[i].Prefix--;
                    }
                    else
                        break;
                }
            }
            else
            {
                match.Prefix -= 1;
            }

            CurrentProcess.Remove(modFile);

            modFile.Path = match.Path;

            modFiles.Add(modFile);

            UpdateModList();

            return;
        }

        public ICommand RightBefore { get; }

        public ICommand RightAfter { get; }


        private void UpdateModList()
        {
            _modFileTree = new ModDirectory(string.Empty, 0, modFiles, ReferenceHasConflicts).Files;

            RaisePropertyChanged(nameof(ModFileTree));

        }

        private class ModNameParse
        {
            private static Regex PDXPattern = new Regex(@"(?<directory>(.+/)+)?(?<prefix>[\d|\w][\d|\w](?=_))?(?<filename>.+)(?<extension>\..+)");

            private static List<char> characters = new List<char> {'0', '1', '2', '3', '4', '5', '6', '7', '8', '9',
                                                                   'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j',
                                                                   'k', 'l', 'm', 'n', 'o', 'p', 'q', 'r', 's', 't',
                                                                   'u', 'v', 'w', 'x', 'y', 'z'};


            public static int MaxNum = characters.Count * characters.Count - 1;

            public ModFile File {get;}

            public string Extension { get; }

            public string Directory { get; }

            public string Filename { get; private set; }

            public int Prefix { get; set; }

            public string Path => $"{Directory}{StringPrefix}{Filename}{Extension}";

            public string StringPrefix
            {
                get
                {
                    if (Prefix < 100)
                    {
                        return Prefix.ToString("00");
                    }
                    else
                    {
                        int pf = Prefix - 100;

                        char dec = characters[pf / characters.Count];
                        char uni = characters[pf % characters.Count];

                        return new string(new[] { dec, uni });
                    }
                }
            }

            private void ParsePrefix()
            {
                if (Match.Groups["prefix"].Success)
                {
                    var prefix = Match.Groups["prefix"].Value;

                    if (char.IsDigit(prefix[0]) &&
                        char.IsDigit(prefix[1]))
                    {
                        Prefix = int.Parse(prefix);
                    } else
                    {
                        Prefix = characters.IndexOf(prefix[0]) * characters.Count + characters.IndexOf(prefix[1]);
                    }
                } else
                {
                    Filename = $"_{Filename}";
                }
            }


            public Match Match { get; }

            private ModNameParse(ModFile modFile, Match match)
            {
                File = modFile;
                Match = match;

                Directory = Match.Groups["directory"].Value;
                Filename = Match.Groups["filename"].Value;
                Extension = Match.Groups["extension"].Value;
                ParsePrefix();

            }

            public static ModNameParse Parse(ModFile mod)
            {
                var match = PDXPattern.Match(mod.Path);
                if (match.Success)
                {
                    return new ModNameParse(mod, match);
                }
                else
                {
                    return null;
                }
            }
        }
    }
}
