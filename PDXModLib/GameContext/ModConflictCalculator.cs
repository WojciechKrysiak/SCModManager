using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PDXModLib.Interfaces;
using PDXModLib.ModData;

namespace PDXModLib.GameContext
{
    public class ModConflictCalculator : IModConflictCalculator
    {
        private readonly IGameConfiguration _gameConfiguration;
        private readonly IInstalledModManager _installedModManager;

        public ModConflictCalculator(IGameConfiguration gameConfiguration, IInstalledModManager installedModManager)
        {
            _gameConfiguration = gameConfiguration;
            _installedModManager = installedModManager;
        }

        //public IEnumerable<Mod> CalculateConflicts(Mod mod)
        //{
        //    return _installedModManager.Mods.Except(new[] { mod }).Where(m => m.Files.Where(ShouldCompare).Any(mf => mod.Files.Any(mff => mff.Path == mf.Path))).ToList();
        //}

        public ModConflictDescriptor CalculateConflicts(Mod mod)
        {
            var fileConflicts = mod.Files.Select(CalculateConflicts);
            return new ModConflictDescriptor(mod, fileConflicts);
        }

        public bool HasConflicts(ModFile file, Func<Mod, bool> modFilter)
        {
            return _installedModManager.Mods.Where(m => m != file.SourceMod && modFilter(m)).SelectMany(m => m.Files).Any(mf => mf.Path == file.Path);
        }

        private bool ShouldCompare(ModFile mod)
        {
            return !_gameConfiguration.WhiteListedFiles.Contains(mod.Filename);
        }

        private ModFileConflictDescriptor CalculateConflicts(ModFile modfile)
        {
            var conflictingModFiles = ShouldCompare(modfile)
                ? _installedModManager.Mods.Where(m => m != modfile.SourceMod)
                                           .Select(m => m.Files.FirstOrDefault(mf => mf.Equals(modfile)))
                                           .Where(mf => mf != null)
                : Enumerable.Empty<ModFile>();

            return new ModFileConflictDescriptor(modfile, conflictingModFiles);
        }

        public IEnumerable<ModConflictDescriptor> CalculateAllConflicts()
        {
            var source = _installedModManager.Mods.ToDictionary(m => m, m => new List<List<ModFile>>());

            var allFiles = _installedModManager.Mods.SelectMany(m => m.Files);

            var groupped = allFiles.GroupBy(m => m);
            foreach (var conflictGroup in groupped)
            {
                var cgList = conflictGroup.ToList();
                foreach (var modFile in cgList)
                {
                    source[modFile.SourceMod].Add(cgList);
                }
            }

            foreach (var conflictSource in source)
            {
                var result = new List<ModFileConflictDescriptor>(100);
                var mod = conflictSource.Key;
                var files = conflictSource.Value;

                foreach (var fileList in files)
                {
                    var file = fileList.First(f => f.SourceMod == mod);
                    if (ShouldCompare(file))
                    {
                        result.Add(new ModFileConflictDescriptor(file, fileList.Where(f => f != file)));
                    }
                    else
                    {
                        result.Add(new ModFileConflictDescriptor(file, Enumerable.Empty<ModFile>()));
                    }
                }

                yield return new ModConflictDescriptor(mod, result);
            }
        }
    }
}
