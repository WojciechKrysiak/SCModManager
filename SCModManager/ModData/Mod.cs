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
using SCModManager.SteamWorkshop;

namespace SCModManager.ModData
{
    public class Mod : ObservableObject
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private string archive;

        private string folder;
        private SteamWorkshopDescriptor _remoteDescriptor;

        protected Mod(string id)
        {
            Id = id;
        }

        public string Id { get; }

        public string Key => $"mod/{Id}";

        public string Name { get; set; }

        public List<ModFile> Files { get; } = new List<ModFile>();

        public List<string> Tags { get; } = new List<string>();

        public virtual string FileName => archive;

        public bool ParseError { get; set; }

        public string Description { get; private set; }

        public string PictureName { get; private set; }

        public string RemoteFileId { get; private set; }

        public ImageSource Image { get; private set; }

        public SupportedVersion SupportedVersion { get; protected set; }

        internal static Mod Load(string modDescriptor)
        {
            var id = Path.GetFileName(modDescriptor);
            var mod = new Mod(id);

            IEnumerable<string> tags;
            using (var stream = new FileStream(modDescriptor, FileMode.Open, FileAccess.Read))
            {
                var parser = new Parser(new Scanner(stream));

                parser.Parse();

                mod.Name = parser.Root["name"]?.ToString();

                mod.archive = (parser.Root["archive"] as SCString)?.Text;
                mod.folder = (parser.Root["path"] as SCString)?.Text;

                if (string.IsNullOrEmpty(mod.archive) &&
                    string.IsNullOrEmpty(mod.folder))
                {
                    Log.Debug($"Both archive and folder for {modDescriptor} are empty");
                    mod.Name = id;
                    mod.ParseError = true;
                    return mod;
                }

                mod.PictureName = (parser.Root["picture"] as SCString)?.Text;

                tags = (parser.Root["tags"] as SCObject)?.Select(i => (i.Value as SCString)?.Text);

                mod.SupportedVersion = new SupportedVersion((parser.Root["supported_version"] as SCString)?.Text);
                mod.RemoteFileId = (parser.Root["remote_file_id"] as SCString)?.Text;
            }

            if (tags != null)
            {
                mod.Description = string.Join(", ", tags.Where(t => t != null));
                mod.Tags.AddRange(tags.Where(t => t != null));
            }
            return mod;
        }


        public void LoadFiles(string basePath)
        {
            var mPath = Path.Combine(basePath, archive ?? folder);

            if (Path.GetExtension(mPath) == ".zip")
            {
                var file = ZipFile.Read(mPath);

                foreach (var item in file)
                {
                    if (string.Compare(item.FileName, "descriptor.mod", true) == 0)
                    {
                        continue;
                    }

                    var modFile = ModFile.Load(item, this);
                    Files.Add(modFile);

                }
            }
            else
            {
                mPath = mPath + "\\";
                var items = Directory.EnumerateFiles(mPath, "*.*", SearchOption.AllDirectories);
                foreach (var item in items)
                {
                    if (string.Compare(Path.GetFileName(item), "descriptor.mod", true) != 0)
                    {
                        var refPath = Uri.UnescapeDataString(new Uri(mPath).MakeRelativeUri(new Uri(item)).OriginalString);
                        var modFile = ModFile.Load(refPath, mPath, this);
                        Files.Add(modFile);
                    }
                }

            }
        }

        protected virtual SCKeyValObject SCName => SCKeyValObject.Create("name", Name);
        protected virtual SCKeyValObject SCFileName => SCKeyValObject.Create("archive", FileName);
        protected virtual SCKeyValObject SCTags => new SCKeyValObject(new SCIdentifier("tags"), SCObject.Create(Tags));
        protected virtual SCKeyValObject SCSupportedVersion => new SCKeyValObject(new SCIdentifier("supported_version"), new SCString(SupportedVersion.ToString()));

        public IEnumerable<SCKeyValObject> DescriptorContents => ToDescriptor();

        public SteamWorkshopDescriptor RemoteDescriptor
        {
            get { return _remoteDescriptor; }
            set { Set(ref _remoteDescriptor, value); }
        }

        public IEnumerable<SCKeyValObject> ToDescriptor()
        {
            yield return SCName;
            yield return SCFileName;
            yield return SCTags;
            if (!string.IsNullOrEmpty(PictureName))
                yield return new SCKeyValObject(new SCIdentifier("picture"), new SCString(PictureName));

            yield return SCSupportedVersion;
        }

        public IEnumerable<KeyValuePair<string, string>> DisplayValues
        {
            get {
                var retval = new Dictionary<string, string>();

                retval.Add("Title", RemoteDescriptor?.Title ?? Name);
                if (RemoteDescriptor != null)
                {
                    retval.Add("Tags", string.Join(",", RemoteDescriptor.Tags?.Select(t => t.Tag) ?? new string[0]));
                    retval.Add("Created", new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc).AddSeconds(RemoteDescriptor.TimeCreated).ToLocalTime().ToShortDateString());
                    retval.Add("Modified", new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc).AddSeconds(RemoteDescriptor.TimeUpdate).ToLocalTime().ToShortDateString());
                    retval.Add("Subscriptions", RemoteDescriptor.LifetimeSubscriptions.ToString());
                    retval.Add("Favorited", RemoteDescriptor.Favorited.ToString());
                } else
                {
                    retval.Add("Tags", string.Join(",", Tags));
                }

                retval.Add("Supported version", this.SupportedVersion.ToString());

                return retval;
            }
        }

    }

    public class SupportedVersion
    {
        public int Major { get; }

        public int Minor { get; }

        public int Patch { get; }

        public SupportedVersion(string source)
        {
            var ver = source.Split('.');

            Major = ver[0] == "*" ? Int32.MaxValue : int.Parse(ver[0]);
            Minor = ver[1] == "*" ? Int32.MaxValue : int.Parse(ver[1]);
            Patch = ver[2] == "*" ? Int32.MaxValue : int.Parse(ver[2]);
        }

        public SupportedVersion(int maj, int min, int pat)
        {
            Major = maj;
            Minor = min;
            Patch = pat;
        }

        public static SupportedVersion Combine(IEnumerable<SupportedVersion> source)
        {
            int ma = Int32.MaxValue;
            int mi = Int32.MaxValue;
            int pa = Int32.MaxValue;

            foreach (var s in source)
            {
                if (ma > s.Major)
                {
                    ma = s.Major;
                    mi = Int32.MaxValue;
                    pa = Int32.MaxValue;
                }
                else if (ma == s.Major)
                {
                    if (mi > s.Minor)
                    {
                        mi = s.Minor;
                        pa = Int32.MaxValue;
                    }
                    else
                    {
                        if (pa > s.Patch)
                            pa = s.Patch;
                    }
                }
            }

            return new SupportedVersion(ma, mi, pa);
        }

        public override string ToString()
        {
            var mj = Major < Int32.MaxValue ? Major.ToString() : "*";
            var mi = Minor < Int32.MaxValue ? Minor.ToString() : "*";
            var pa = Patch < Int32.MaxValue ? Patch.ToString() : "*";

            return $"{mj}.{mi}.{pa}";
        }

    }

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
        }

        private IEnumerable<Mod> _source;

        protected override SCKeyValObject SCFileName => SCKeyValObject.Create("path", $"mod/{FileName}");

        protected override SCKeyValObject SCTags => new SCKeyValObject(new SCIdentifier("tags"), SCObject.Create(_source.SelectMany(m => m.Tags).Distinct().ToList()));
    }
}