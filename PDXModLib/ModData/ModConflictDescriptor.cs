using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PDXModLib.ModData
{
    public class ModConflictDescriptor
    {
        public ModConflictDescriptor(Mod mod, IEnumerable<ModFileConflictDescriptor> fileConflicts)
        {
            Mod = mod;
            FileConflicts = fileConflicts.ToList();
            ConflictingMods = FileConflicts.SelectMany(fc => fc.ConflictingModFiles).Select(mf => mf.SourceMod).Distinct();
        }

        public Mod Mod { get; }

        public IEnumerable<ModFileConflictDescriptor> FileConflicts { get; }

        public IEnumerable<Mod> ConflictingMods { get; }

        public ModConflictDescriptor Filter(Func<Mod, bool> filterFunc)
        {
            return new ModConflictDescriptor(Mod, FileConflicts.Select(fc => fc.Filter(filterFunc)));
        }
    }
}
