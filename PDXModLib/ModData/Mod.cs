using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using CWTools.Process;
using Microsoft.FSharp.Compiler;
using NLog;
using PDXModLib.Utility;
using static CWTools.Parser.Types;

namespace PDXModLib.ModData
{
	public class Mod : IDisposable
	{
		private ZipArchive _zipFile;

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

		public virtual string Folder => _folder;

		public bool ParseError { get; set; }

		public string Description { get; private set; }

		public string PictureName { get; private set; }

		public string RemoteFileId { get; private set; }

		public SupportedVersion SupportedVersion { get; protected set; }

		static Mod()
		{
		}

		public static Mod Load(string modDescriptor)
        {
            var id = Path.GetFileName(modDescriptor);
            var mod = new Mod(id);

            IEnumerable<string> tags;
			var adapter = CWToolsAdapter.Parse(modDescriptor);

            //using (var stream = new FileStream(modDescriptor, FileMode.Open, FileAccess.Read))
            {
                //var parser = new Parser(new Scanner(stream));
				//
                //parser.Parse();

				mod.Name = adapter.Root.Get("name").AsString(); 

                mod._archive = adapter.Root.Get("archive").AsString();
                mod._folder = adapter.Root.Get("path").AsString();

                if (string.IsNullOrEmpty(mod._archive) &&
                    string.IsNullOrEmpty(mod._folder))
                {
                    Log.Debug($"Both archive and folder for {modDescriptor} are empty");
                    mod.Name = id;
                    mod.ParseError = true;
                    return mod;
                }

				mod.PictureName = adapter.Root.Get("picture").AsString();

                tags = adapter.Root.Child("tags").Value.LeafValues.Select(s => s.Value.ToRawString()).ToList();

                mod.SupportedVersion = new SupportedVersion(adapter.Root.Get("supported_version").AsString());
				mod.RemoteFileId = (adapter.Root.Get("remote_file_id").AsString());
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
                
                _zipFile = ZipFile.OpenRead(mPath);

                foreach (var item in _zipFile.Entries)
                {
                    if (string.Compare(item.Name, "descriptor.mod", true) == 0)
                    {
                        continue;
                    }

                    var modFile = ModFile.Load(new ZipFileLoader(item), item.FullName, this);
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

        protected virtual Child SCName => Child.NewLeafC(new Leaf("name", Value.NewQString(Name), Range.range0));
        protected virtual Child SCFileName => Child.NewLeafC(new Leaf("archive", Value.NewQString(FileName), Range.range0));
        protected virtual Child SCTags => Child.NewNodeC(CreateTags());
        protected virtual Child SCSupportedVersion => Child.NewLeafC(new Leaf("supported_version", Value.NewQString(SupportedVersion.ToString()), Range.range0));

		private Node CreateTags()
		{
			var result = new Node("tags");
			result.AllChildren = Tags.Select(s => Child.NewLeafValueC(new LeafValue(Value.NewQString(s), Range.range0))).ToList();
			return result;
		}

        public IEnumerable<Child> DescriptorContents => ToDescriptor();

        public IEnumerable<Child> ToDescriptor()
        {
            yield return SCName;
            yield return SCFileName;
            yield return SCTags;
            if (!string.IsNullOrEmpty(PictureName))
                yield return Child.NewLeafC(new Leaf("picture", Value.NewQString(PictureName), Range.range0));

			yield return SCSupportedVersion;
        }

        public void Dispose()
        {
            _zipFile?.Dispose();
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return (obj as Mod)?.Id == Id;
        }

		public override string ToString()
		{
			return Name;
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
            : base($"Merge result")
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

        protected override Child SCFileName => Child.NewLeafC(new Leaf("path", Value.NewQString($"mod/{FileName}"), Range.range0));

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