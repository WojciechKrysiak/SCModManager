using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Ionic.Zip;
using NLog;
using PDXModLib.SCFormat;
using PDXModLib.Utility;

namespace PDXModLib.ModData
{
    public class Mod : IDisposable
    {
        private ZipFile _zipFile;

        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private string _archive;

        private string _folder;

        protected Mod(string id)
        {
            Id = id;
        }

        public string Id { get; }

        public string Key => $"mod/{Id}";

        public string Name { get; set; }

        public List<ModFile> Files { get; } = new List<ModFile>();

        public List<string> Tags { get; } = new List<string>();

        public virtual string FileName => _archive;

        public bool ParseError { get; set; }

        public string Description { get; private set; }

        public string PictureName { get; private set; }

        public string RemoteFileId { get; private set; }

        public SupportedVersion SupportedVersion { get; protected set; }

        public static Mod Load(string modDescriptor)
        {
            var id = Path.GetFileName(modDescriptor);
            var mod = new Mod(id);

            IEnumerable<string> tags;
            using (var stream = new FileStream(modDescriptor, FileMode.Open, FileAccess.Read))
            {
                var parser = new Parser(new Scanner(stream));

                parser.Parse();

                mod.Name = parser.Root["name"]?.ToString();

                mod._archive = (parser.Root["archive"] as SCString)?.Text;
                mod._folder = (parser.Root["path"] as SCString)?.Text;

                if (string.IsNullOrEmpty(mod._archive) &&
                    string.IsNullOrEmpty(mod._folder))
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
                mod.Description = string.Join(", ", tags.Where(t => t != null).ToArray());
                mod.Tags.AddRange(tags.Where(t => t != null));
            }
            return mod;
        }

        public void LoadFiles(string basePath)
        {
            var mPath = Path.Combine(basePath, _archive ?? _folder);

            if (Path.GetExtension(mPath) == ".zip")
            {
                _zipFile = ZipFile.Read(mPath);

                foreach (var item in _zipFile)
                {
                    if (string.Compare(item.FileName, "descriptor.mod", true) == 0)
                    {
                        continue;
                    }

                    var modFile = ModFile.Load(new ZipFileLoader(item), item.FileName, this);
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
                        var modFile = ModFile.Load(new DiskFileLoader(item), refPath, this);
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

        public IEnumerable<SCKeyValObject> ToDescriptor()
        {
            yield return SCName;
            yield return SCFileName;
            yield return SCTags;
            if (!string.IsNullOrEmpty(PictureName))
                yield return new SCKeyValObject(new SCIdentifier("picture"), new SCString(PictureName));

            yield return SCSupportedVersion;
        }

        public void Dispose()
        {
            _zipFile?.Dispose();
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

    public class MergedMod : Mod
    {
        public MergedMod(string name, IEnumerable<ModConflictDescriptor> source)
            : base($"Merged")
        {
            Name = name;

            var listSource = source.ToList();

            Tags.AddRange(listSource.Select(mcd => mcd.Mod).SelectMany(m => m.Tags).Distinct());

            SupportedVersion = SupportedVersion.Combine(listSource.Select(s => s.Mod.SupportedVersion));

            var distinctConflicts = listSource.SelectMany(mcd => mcd.FileConflicts).Distinct();

            foreach (var conflict in distinctConflicts)
            {
                if (!conflict.ConflictingModFiles.Any())
                {
                    Files.Add(conflict.File);
                }
                else
                {
                    Files.Add(new MergedModFile(conflict.File.Path, conflict.ConflictingModFiles.Concat(new[] {conflict.File}), this));
                }
            }
        }

        public override string FileName => Name;

        protected override SCKeyValObject SCFileName => SCKeyValObject.Create("path", $"mod/{FileName}");

        public ModConflictDescriptor ToModConflictDescriptor()
        {
            return new ModConflictDescriptor(this, Files.Select(ToConflictDescriptor));
        }

        ModFileConflictDescriptor ToConflictDescriptor(ModFile file)
        {
            var mmf = file as MergedModFile;
            if (mmf != null)
            {
                return new ModFileConflictDescriptor(mmf, mmf.SourceFiles);
            }

            return new ModFileConflictDescriptor(file, Enumerable.Empty<ModFile>());
        }
    }
}