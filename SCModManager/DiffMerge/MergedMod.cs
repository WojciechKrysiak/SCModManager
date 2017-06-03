using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using Ionic.Zip;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Input;

namespace SCModManager
{
    class MergedMod : Mod
    {
        public MergedMod(string name, IEnumerable<Mod> source)
            : base($"Merged")
        {
            Name = name;

            _source = source.ToList();

            SupportedVersion = SupportedVersion.Combine(source.Select(s => s.SupportedVersion));


            var modGroups = source.SelectMany(m => m.Files).GroupBy(mf => mf.Path);

            foreach (var group in modGroups)
            {
                if (group.Count() == 1)
                {
                    Files.Add(group.First());
                }
                else
                {
                    Files.Add(new MergedModFile(group.Key, group, this));
                }
            }
        }

        public override string FileName 
        {
            get
            {
                return Name;
            }

            protected set
            {
                throw new NotImplementedException();
            }
        }

        private IEnumerable<Mod> _source;
    }

    class ModToProcess : ObservableObject
    {
        public ModFile File { get; }

        public string Description
        {
            get
            {
                if (File is MergedModFile)
                {
                    int maxCnt = (File as MergedModFile).SourceFileCount;
                    return $"{File.Path}({maxCnt})";
                }
                else
                    return File.Path;
            }

        }

        public bool HasConflict => (File as MergedModFile)?.SourceFileCount >= 2;

        MergeProcess _process;

        public MergeProcess Process
        {
            get
            {
                var mf = File as MergedModFile;
                if (mf == null)
                    return null;

                return _process ?? (_process = new MergeProcess(mf));
            }
        }

        public ModToProcess(ModFile file)
        {
            File = file;
        }
    }

    class ModMergeContext : ObservableObject
    {
        public List<Mod> BaseMods { get; }
        private ModToProcess _selected;

        private List<ModToProcess> modFiles = new List<ModToProcess>();

        public IEnumerable<ModToProcess> ModFiles => modFiles.Where(mf => mf.HasConflict);

        public ModToProcess SelectedModFile
        {
            get { return _selected; }
            set
            {
                if (CurrentProcess != null)
                {
                    CurrentProcess.FileResolved -= CurrentProcess_FileResolved;
                }

                _selected = value;

                RaisePropertyChanged(nameof(SelectedModFile));

                if (CurrentProcess != null)
                {
                    CurrentProcess.FileResolved += CurrentProcess_FileResolved;
                }

                RaisePropertyChanged(nameof(CurrentProcess));
            }
        }

        private void CurrentProcess_FileResolved(object sender, EventArgs e)
        {
            if (!SelectedModFile.HasConflict)
            {
                SelectedModFile = null;
            }
            RaisePropertyChanged(nameof(modFiles));
            Save.RaiseCanExecuteChanged();
        }

        public MergeProcess CurrentProcess => _selected?.Process;

        private MergedMod result;

        public MergedMod Result => result;

        public RelayCommand Save { get; }

        public ModMergeContext(IEnumerable<Mod> source, Action<ModMergeContext> save)
        {
            BaseMods = source.ToList();

            result = new MergedMod("Merge result", BaseMods);

            modFiles.AddRange(result.Files.Select(f => new ModToProcess(f)));

            LeftBefore = new RelayCommand<ModFile>(DoBefore);
            LeftAfter = new RelayCommand<ModFile>(DoAfter);
            RightBefore = new RelayCommand<ModFile>(DoBefore);
            RightAfter = new RelayCommand<ModFile>(DoAfter);

            saveAction = save;
            Save = new RelayCommand(SaveAction, () => !modFiles.Any(mf => mf.HasConflict));
        }

        private Action<ModMergeContext> saveAction;

        private void SaveAction()
        {
            saveAction(this);
        }

        private List<ModNameParse> GetMatchingFiles(ModNameParse modFile)
        {
            return modFiles.Select(mftp => ModNameParse.Parse(mftp.File)).Where(mft => modFile.Filename == mft?.Filename).OrderBy(r => r.Prefix).ToList();
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
                        
                        modFiles.First(mtp => mtp.File == surroundings[i].File).RaisePropertyChanged(nameof(ModToProcess.Description));
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

            modFiles.Add(new ModToProcess(modFile));

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

                        modFiles.First(mtp => mtp.File == surroundings[i].File).RaisePropertyChanged(nameof(ModToProcess.Description));
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

            modFiles.Add(new ModToProcess(modFile));

            return;
        }

        public ICommand RightBefore { get; }

        public ICommand RightAfter { get; }

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
