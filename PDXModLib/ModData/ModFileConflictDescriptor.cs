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
            return File.Equals((obj as ModFileConflictDescriptor)?.File);
        }

        public override int GetHashCode()
        {
            return File.GetHashCode();
        }
    }
}
