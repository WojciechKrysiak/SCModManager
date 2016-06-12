using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using GalaSoft.MvvmLight;
using Ionic.Zip;
using NLog;
using SCModManager.SCFormat;

namespace SCModManager
{
    public class Mod : ObservableObject
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private int conflicts;
        private bool hasConflict;
        private string name;
        private bool selected;

        private Mod(string id)
        {
            Id = id;
        }

        public string Id { get; }

        public string Key => $"mod/{Id}.mod";

        public string Name
        {
            get { return name; }
            set { Set(ref name, value); }
        }

        public List<ModFile> Files { get; set; } = new List<ModFile>();

        public int Conflicts
        {
            get { return conflicts; }
            private set { Set(ref conflicts, value); }
        }

        public bool Selected
        {
            get { return selected; }
            set { Set(ref selected, value); }
        }

        public bool ParseError { get; set; }

        public bool HasConflict
        {
            get { return hasConflict; }
            private set { Set(ref hasConflict, value); }
        }

        public string Description { get; private set; }

        public ImageSource Image { get; private set; }

        public ModVersion SupportedVersion { get; private set; }
        

        internal static Mod Load(string modDescriptor)
        {
            var id = Path.GetFileName(modDescriptor);
            var mod = new Mod(id);

            var path = Directory.GetParent(modDescriptor).Parent.FullName;

            string zip = null;
            string picture = null;
            IEnumerable<string> tags;
            using (var stream = new FileStream(modDescriptor, FileMode.Open, FileAccess.Read))
            {
                var parser = new Parser(new Scanner(stream));

                parser.Parse();

                mod.Name = parser.Root["name"]?.ToString();

                zip = (parser.Root["archive"] as SCString)?.Text;

                if (zip == null)
                {
                    Log.Debug($"Zip is null for {modDescriptor}");
                    mod.Name = id;
                    mod.ParseError = true; 
                    return mod;
                }

                picture = (parser.Root["picture"] as SCString)?.Text;

                tags = (parser.Root["tags"] as SCObject)?.Select(i => (i.Value as SCString)?.Text);

                mod.SupportedVersion = new ModVersion((parser.Root["supported_version"] as SCString)?.Text);
            }

            if (tags != null)
            {
                mod.Description = string.Join(", ", tags.Where(t => t != null));
            }

            var file = ZipFile.Read(Path.Combine(path, zip));

            foreach (var item in file)
            {
                if (string.Compare(item.FileName, "descriptor.mod", true) == 0)
                {
                    continue;
                }

                using (var stream = item.OpenReader())
                {
                    var modFile = ModFile.Load(item.FileName, stream);
                    mod.Files.Add(modFile);

                    if (string.Compare(item.FileName, picture, true) == 0 && modFile is BinaryModFile)
                    {
                        var image = new BitmapImage();
                        image.BeginInit();
                        image.StreamSource = (modFile as BinaryModFile).DataStream;
                        image.EndInit();
                        mod.Image = image;
                    }
                }

            }
            return mod;
        }

      public void MarkConflicts(IEnumerable<Mod> allMods)
        {
            foreach (var mod in allMods)
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

        private bool CheckConflicts(Mod other)
        {
            foreach (var file in Files)
            {
                if (other.Files.Any(f => string.Compare(f.Path, file.Path, true) == 0))
                {
                    return true;
                }
            }

            return false;
        }

        public void SetHasConflictWithMod(Mod other)
        {
            HasConflict = this != other && CheckConflicts(other);
        }
    }
}