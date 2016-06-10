using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using SCModManager.SCFormat;

namespace SCModManager
{
    public class Mod
    {
        public string Name { get; set; }

        public List<ModFile> Files { get; set; } = new List<ModFile>();

        public int Conflicts { get; private set; }
        public bool Selected { get; set; }

        internal static Mod Load(string dir)
        {
            var Mod = new Mod();
            var zip = Directory.EnumerateFiles(dir, "*.zip").FirstOrDefault();
            if (zip == null)
            {
                return null;
            }
            

            var file = Ionic.Zip.ZipFile.Read(zip);
            
            var entry = file.FirstOrDefault(ze => string.Compare(ze.FileName, "descriptor.mod", true) == 0);

            if (entry == null)
            {
                return null;
            }

            using (var stream = entry.OpenReader())
            {
                var parser = new Parser(new Scanner(stream));

                parser.Parse();
   
                Mod.Name = parser.Root["name"]?.ToString();
            }

            foreach(var item in file)
            {
                if (item == entry)
                {
                    continue;
                }

                using (var stream = item.OpenReader())
                {
                    Mod.Files.Add(ModFile.Load(item.FileName, stream));
                }
            }
            return Mod;
        }

        public void MarkConflicts(IEnumerable<Mod> allMods)
        {
            foreach(var mod in allMods)
            {
                if (mod == this)
                {
                    continue;
                }

                if (CheckConflicts(mod))
                {
                    Conflicts++;
                }
            }
        }

        bool CheckConflicts(Mod other)
        {
            foreach(var file in Files)
            {
                if (other.Files.Any(f => string.Compare(f.Path, file.Path, true) == 0))
                {
                    return true;
                }
            }

            return false;
        }
    }
}