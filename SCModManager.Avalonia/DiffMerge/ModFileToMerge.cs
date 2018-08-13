using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PDXModLib.ModData;

namespace SCModManager.Avalonia.DiffMerge
{
    public class ModFileToMerge
    {
        public ModFile Source { get; }

        public string ModName => Source.SourceMod.Name;

        public string RawContents => Source.RawContents;

        public ModFileToMerge(ModFile source)
        {
            Source = source;
        }
    }
}
