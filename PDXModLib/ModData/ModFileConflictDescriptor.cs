using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PDXModLib.ModData
{
    public class ModFileConflictDescriptor
    {
        public ModFileConflictDescriptor(ModFile file, IEnumerable<ModFile> conflictingModFiles)
        {
            File = file;
            ConflictingModFiles = conflictingModFiles.ToList();
        }

        public ModFile File { get; }

        public IReadOnlyCollection<ModFile> ConflictingModFiles { get; }

        public override bool Equals(object obj)
        {
            return File?.Path?.Equals((obj as ModFileConflictDescriptor)?.File?.Path) ?? false;
        }

        public override int GetHashCode()
        {
            return File?.Path?.GetHashCode() ?? -1;
        }

        public ModFileConflictDescriptor Filter(Func<Mod, bool> filterFunc)
        {
            return new ModFileConflictDescriptor(File, ConflictingModFiles.Where(cmf => filterFunc(cmf.SourceMod)));
        }
    }
}
