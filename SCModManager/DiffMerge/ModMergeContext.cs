using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Subjects;
using System.Text.RegularExpressions;
using System.Windows.Input;
using PDXModLib.ModData;
using ReactiveUI;
using SCModManager.ViewModels;

namespace SCModManager.DiffMerge
{
    class ModMergeContext : ReactiveObject
    {
        public List<Mod> BaseMods { get; }
        private ModFile _selected;

        private bool onlyConflicts;

        private readonly IEnumerable<ModFile> modFiles;

        private readonly Action<ModMergeContext> saveAction;

        private readonly Subject<bool> _canSave = new Subject<bool>();

        readonly Dictionary<ModFile, MergeProcess> _currentProcesses = new Dictionary<ModFile, MergeProcess>(ReferenceEqualityComparer.Default);

        private readonly ModDirectory _rootDirectory;

        private bool _ignoreWhiteSpace;

        public ModDirectory RootDirectory => _rootDirectory;

        public ModFile SelectedModFile
        {
            get { return _selected; }
            set
            {
                if (CurrentProcess != null)
                {
                    CurrentProcess.FileResolved -= CurrentProcess_FileResolved;
                }

                this.RaiseAndSetIfChanged(ref _selected, value);

                if (CurrentProcess != null)
                {
                    CurrentProcess.FileResolved += CurrentProcess_FileResolved;
                }

                this.RaisePropertyChanged(nameof(CurrentProcess));
            }
        }

        private void CurrentProcess_FileResolved(object sender, EventArgs e)
        {
            CheckFileResolved();
        }

        private void CheckFileResolved()
        {
            var selected = _selected;
            if (selected != null)
            {
                if (!ReferenceHasConflicts(_selected))
                {
                    _currentProcesses.Remove(_selected);
                    SelectedModFile = null;
                }

                UpdateModList(selected.Path);
            }

            _canSave.OnNext(!modFiles.Any(ReferenceHasConflicts));
        }

        public MergeProcess CurrentProcess
        {
            get
            {
                if (_selected != null && ReferenceHasConflicts(_selected))
                {
                    if (!_currentProcesses.ContainsKey(_selected))
                    {
                        _currentProcesses.Add(_selected, new MergeProcess(_selected as MergedModFile));
                    }

                    _currentProcesses[_selected].HideWhiteSpace = IgnoreWhiteSpace;

                    return _currentProcesses[_selected];
                }
                return null;

            }
        }

        public bool OnlyConflicts
        {
            get { return onlyConflicts; }
            set {
				this.RaiseAndSetIfChanged(ref onlyConflicts, value);
			}
        }

        public bool IgnoreWhiteSpace
        {
            get { return _ignoreWhiteSpace; }
            set {
                this.RaiseAndSetIfChanged(ref _ignoreWhiteSpace, value);
                if (CurrentProcess != null)
                    CurrentProcess.HideWhiteSpace = _ignoreWhiteSpace;
            }
        }

        public MergedMod Result { get; }

        public ICommand Save { get; }

        public ModMergeContext(IEnumerable<ModConflictDescriptor> source, Action<ModMergeContext> save)
        {
            Result = new MergedMod("Merge result", source);

            modFiles = Result.Files;

            LeftBefore = ReactiveCommand.Create<ModFileToMerge>(DoBefore);
            LeftAfter = ReactiveCommand.Create<ModFileToMerge>(DoAfter);
            RightBefore = ReactiveCommand.Create<ModFileToMerge>(DoBefore);
            RightAfter = ReactiveCommand.Create<ModFileToMerge>(DoAfter);

            saveAction = save;
            Save = ReactiveCommand.Create(DoSave, _canSave);

            _rootDirectory = ModDirectory.CreateRoot(Result.ToModConflictDescriptor());
        }

        private static bool ReferenceHasConflicts(ModFile mf)
        {
            return !((mf as MergedModFile)?.Resolved ?? true);
        }

        private void DoSave()
        {
            var confirm = new NameConfirm();
            var confirmVM = new NameConfirmVM(Result.Name);
            confirmVM.ShouldClose += (o, b) =>
            {
                if (b)
                {
                    Result.Name = confirmVM.Name;
                    saveAction(this);
                }
                confirm.Close();
            };

            confirm.DataContext = confirmVM;
            confirm.ShowDialog();
        }

        private List<ModNameParse> GetMatchingFiles(ModNameParse modFile)
        {
            return modFiles.Select(ModNameParse.Parse).Where(mft => modFile.Filename == mft?.Filename).OrderBy(r => r.Path).ToList();
        }

        public ICommand LeftBefore { get; }

        private void DoBefore(ModFileToMerge modFileToMerge)
        {
            var modFile = modFileToMerge.Source;
            var match = ModNameParse.Parse(modFile);

            if (match?.Filename == null)
            {
                throw new InvalidDataException($"provided file path \"{modFile.Path}\" was not in en expected format - please contact the developer.");
            }

            var surroundings = GetMatchingFiles(match);

            var hasPrefix = match.HasPrefix;
            if (hasPrefix)
            {
                var prefix = match.Prefix;
                if (prefix == 0 || surroundings.Any(s => s.HasPrefix && s.Prefix == prefix - 1))
                {
                    var toModify = surroundings.Where(s => s.HasPrefix && s.Prefix >= prefix).OrderBy(s => s.Prefix);
                    var modPrefix = prefix;
                    foreach (var item in toModify)
                    {
                        if (modPrefix != item.Prefix)
                            break;
                        modPrefix++;
                        item.Prefix = modPrefix;
                        item.File.Path = item.Path;
                    }
                }
                else
                {
                    match.Prefix = prefix - 1;
                    modFile.Path = match.Path;
                }
            }
            else
            {
                int prefix = 0;
                if (surroundings.Any(s => s.HasPrefix))
                {
                    prefix = surroundings.Where(s => s.HasPrefix).Max(s => s.Prefix) + 1;
                }
                match.Prefix = prefix;
                modFile.Path = match.Path;
            }

            Result.Files.Add(modFile);

            CurrentProcess.Remove(modFile);
        }

        public ICommand LeftAfter { get; }

        private void DoAfter(ModFileToMerge modFileToMerge)
        {
            var modFile = modFileToMerge.Source;
            var match = ModNameParse.Parse(modFile);

            if (match?.Filename == null)
            {
                throw new InvalidDataException($"provided file path \"{modFile.Path}\" was not in en expected format - please contact the developer.");
            }

            var surroundings = GetMatchingFiles(match);

            var hasPrefix = match.HasPrefix;
            if (hasPrefix)
            {
                var prefix = match.Prefix;
                if (surroundings.Any(s => s.HasPrefix && s.Prefix == prefix + 1))
                {
                    var toModify = surroundings.Where(s => s.HasPrefix && s.Prefix <= prefix).OrderByDescending(s => s.Prefix);
                    var modPrefix = prefix;
                    foreach (var item in toModify)
                    {
                        if (modPrefix != item.Prefix)
                            break;
                        modPrefix--;
                        item.Prefix = modPrefix;
                        item.File.Path = item.Path;
                    }
                }
                else
                {
                    match.Prefix = prefix + 1;
                    modFile.Path = match.Path;
                }
            }
            else
            {
                var dir = Path.GetDirectoryName(modFile.Path);
                var filename = Path.GetFileName(modFile.Path);
                var extension = Path.GetExtension(modFile.Path);
                var postfix = 0;
                string newPath;
                do
                {
                    newPath = Path.Combine(dir, $"{filename}_{postfix++}{extension}");
                } while (modFiles.Any(mf => string.CompareOrdinal(mf.Path, newPath) == 0));

                modFile.Path = newPath;
            }

            Result.Files.Add(modFile);

            CurrentProcess.Remove(modFile);
        }

        public ICommand RightBefore { get; }

        public ICommand RightAfter { get; }


        private void UpdateModList(string path)
        {
            var directory = Path.GetDirectoryName(path) ?? string.Empty;
            
            var newContents = Result.ToModConflictDescriptor().FileConflicts.Where(fc => fc.File.Path.StartsWith(directory)).ToList();
            _rootDirectory.UpdateDirectoryContents(directory, newContents);

            var file = _rootDirectory.GetFile(path);
            if (file?.HasConflicts ?? false)
            {
                SelectedModFile = file.File;
            }
        }

        private class ModNameParse
        {
            private static readonly Regex PDXPattern = new Regex(@"(?<directory>(.+\\)+)?(?<prefix>[\d|\w][\d|\w](?=_))?(?<filename>.+)(?<extension>\..+)");

            private static readonly List<char> characters = new List<char> {'0', '1', '2', '3', '4', '5', '6', '7', '8', '9',
                                                                            'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j',
                                                                            'k', 'l', 'm', 'n', 'o', 'p', 'q', 'r', 's', 't',
                                                                            'u', 'v', 'w', 'x', 'y', 'z'};


            public static readonly int MaxNum = characters.Count * characters.Count - 1;

            public ModFile File {get;}

            public string Extension { get; }

            public string Directory { get; }

            public string Filename { get; private set; }

            public int Prefix { get; set; }

            public string Path => $"{Directory}{StringPrefix}{Filename}{Extension}";

            public bool HasPrefix => Match.Groups["prefix"].Success;

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
                if (HasPrefix)
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
