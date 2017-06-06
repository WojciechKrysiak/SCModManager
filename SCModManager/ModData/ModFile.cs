using SCModManager.SCFormat;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using GalaSoft.MvvmLight;

using NLog;
using Ionic.Zip;
using System.Text.RegularExpressions;

namespace SCModManager.ModData
{
    public abstract class ModFileReference : ObservableObject
    {
        public string Path { get; set; }

        public abstract string Filename { get; }

        public abstract bool HasConflicts {get;}
    }

    public class ModDirectory : ModFileReference
    {
        public override string Filename { get; }

        public IEnumerable<ModFileReference> Files { get; }

        public bool AllConflicfts => Files.All(f => f.HasConflicts);

        public bool NoConflicts => !Files.Any(f => f.HasConflicts);

        public override bool HasConflicts => Files.Any(f => f.HasConflicts);

        public ModDirectory(string name, int level, IEnumerable<ModFile> source)
        {
            Filename = name;

            var kids = source.Select(m => Tuple.Create(m.Path.Split(System.IO.Path.DirectorySeparatorChar, System.IO.Path.AltDirectorySeparatorChar), m));
            List<ModFileReference> result = new List<ModFileReference>();
            foreach (var kid in kids.Where(t => t.Item1.Length > level + 2).GroupBy(k => k.Item1[level + 1]).OrderBy(g => g.Key))
            {
                result.Add(new ModDirectory(kid.Key, level + 1, kid.Select(k => k.Item2)));
            }

            foreach (var kid in kids.Where(t => t.Item1.Length == level + 2))
            {
                result.Add(kid.Item2);
            }
            Files = result;
        }
    }

    public abstract class ModFile : ModFileReference
    {
        private static string[] SCExtensions = new[] { ".gfx", ".gui", ".txt" };

        private static string[] CodeExtensions = new[] { ".lua" };

        private static string[] LocalisationExtensions = new[] { ".yml" };

        private static string[] ImageExtensions = new[] { ".dds", ".png", ".jpg" };

        public string Directory => System.IO.Path.GetDirectoryName(Path);
        public override string Filename => System.IO.Path.GetFileName(Path);

        public List<Mod> Conflicts { get; } = new List<Mod>();

        public override bool HasConflicts => Conflicts.Count > 0;

        protected static readonly Logger Log = LogManager.GetCurrentClassLogger();

        public Mod SourceMod { get; }

        public void ClearConflicts()
        {
            Conflicts.Clear();
            this.RaisePropertyChanged(nameof(HasConflicts));
        }

        public void AddConflict(Mod other)
        {
            Conflicts.Add(other);
            this.RaisePropertyChanged(nameof(HasConflicts));
        }

        protected ModFile(string path, Mod sourceMod)
        {
            Path = path;
            SourceMod = sourceMod;
        }

        public abstract string RawContents { get; }


        internal static ModFile Load(string refPath, string basePath, Mod sourceMod)
        {
            string path = System.IO.Path.Combine(basePath, refPath);

            if (SCExtensions.Contains(System.IO.Path.GetExtension(path).ToLower()))
            {
                using (var stream = File.OpenRead(path))
                {
                    using (var mr = new MemoryStream())
                    {
                        byte[] buffer = new byte[1024];
                        int size = 0;
                        while (stream.CanRead && ((size = stream.Read(buffer, 0, 1024)) > 0))
                        {
                            mr.Write(buffer, 0, size);
                        }

                        mr.Seek(0, SeekOrigin.Begin);

                        var parser = new Parser(new Scanner(mr));

                        parser.Parse();

                        mr.Seek(0, SeekOrigin.Begin);

                        using (var sr = new StreamReader(mr))
                        {
                            return new SCModFile(refPath, parser.Root, NormalizeLineEndings(sr.ReadToEnd()), parser.ParseError, sourceMod);
                        }
                    }
                }
            }

            if (CodeExtensions.Contains(System.IO.Path.GetExtension(path).ToLower()))
            {
                using (var stream = File.OpenRead(path))
                {
                    using (var sr = new StreamReader(stream))
                    {
                        return new CodeModFile(refPath, NormalizeLineEndings(sr.ReadToEnd()), sourceMod);
                    }
                }
            }

            return new BinaryModFile(refPath, path, sourceMod);
        }



        internal static ModFile Load(ZipEntry item, Mod sourceMod)
        {
            var path = item.FileName;

            if (SCExtensions.Contains(System.IO.Path.GetExtension(path).ToLower()))
            {
                using (var stream = item.OpenReader())
                {
                    using (var mr = new MemoryStream())
                    {
                        byte[] buffer = new byte[1024];
                        int size = 0;
                        while (stream.CanRead && ((size = stream.Read(buffer, 0, 1024)) > 0))
                        {
                            mr.Write(buffer, 0, size);
                        }

                        mr.Seek(0, SeekOrigin.Begin);

                        var parser = new Parser(new Scanner(mr));

                        parser.Parse();

                        mr.Seek(0, SeekOrigin.Begin);

                        using (var sr = new StreamReader(mr))
                        {
                            return new SCModFile(path, parser.Root, NormalizeLineEndings(sr.ReadToEnd()), parser.ParseError, sourceMod);
                        }
                    }
                }
            }

            if (CodeExtensions.Contains(System.IO.Path.GetExtension(path).ToLower()))
            {
                using (var stream = item.OpenReader())
                {
                    using (var sr = new StreamReader(stream))
                    {
                        return new CodeModFile(path, NormalizeLineEndings(sr.ReadToEnd()), sourceMod);
                    }
                }
            }

            if (LocalisationExtensions.Contains(System.IO.Path.GetExtension(path).ToLower()))
            {
                using (var stream = item.OpenReader())
                {
                    using (var sr = new StreamReader(stream))
                    {
                        return new LocalisationFile(path, NormalizeLineEndings(sr.ReadToEnd()), sourceMod);
                    }
                }
            }

            return new BinaryModFile(item, sourceMod);
        }

        private static string NormalizeLineEndings(string source)
        {
            return Regex.Replace(source, @"\r\n|\n\r|\n|\r", Environment.NewLine);
        }

        internal abstract void Save(ZipFile entry);

        internal virtual void Save(string fn)
        {
            File.WriteAllText(fn, RawContents);
        }
    }

    public class SCModFile : ModFile
    {
        public SCObject Contents { get; set; }
        public override string RawContents { get; }

        public bool ParseError { get; }
    
        internal SCModFile(string path, SCObject contents, string rawContents, Mod sourceMod)
            : base(path, sourceMod)
        {
            Contents = contents;
            RawContents = rawContents;
        }

        public SCModFile(string path, SCObject contents, string rawContents, bool parseError, Mod sourceMod) : this(path, contents, rawContents, sourceMod)
        {
            ParseError = parseError;
        }

        internal override void Save(ZipFile entry)
        {
            entry.AddEntry(Path, RawContents);
        }
    }

    public class CodeModFile : ModFile
    {
        public string Contents { get; set; }

        public override string RawContents => Contents;

        internal CodeModFile(string path, string contents, Mod sourceMod)
            : base(path, sourceMod)
        {
            Contents = contents;
        }

        internal override void Save(ZipFile entry)
        {
            entry.AddEntry(Path, RawContents);
        }
    }

    public class LocalisationFile : ModFile
    {
        public string Contents { get; set; }

        public override string RawContents => Contents;

        internal LocalisationFile(string path, string contents, Mod sourceMod)
            : base(path, sourceMod)
        {
            Contents = contents;
        }

        internal override void Save(ZipFile entry)
        {
            entry.AddEntry(Path, RawContents, Encoding.UTF8);
        }

        internal override void Save(string fn)
        {
            File.WriteAllText(fn, RawContents, Encoding.UTF8);
        }
    }



    public class BinaryModFile : ModFile
    {
        public string Contents => "BinaryModFile";
        public override string RawContents => Contents;

        string originalFilePath;

        ZipEntry _entry;

        internal BinaryModFile(ZipEntry entry, Mod sourceMod)
            : base(entry.FileName, sourceMod)
        {
            _entry = entry;
        }


        internal BinaryModFile(string refPath, string path, Mod sourceMod)
            : base(refPath, sourceMod)
        {
            originalFilePath = path;
        }

        internal override void Save(ZipFile entry)
        {
            entry.AddEntry(Path, openDelegate, closeDelegate);
        }

        private void closeDelegate(string entryName, Stream stream)
        {
            stream.Close();
        }

        private Stream openDelegate(string entryName)
        {
            return (Stream)_entry?.OpenReader() ?? File.OpenRead(originalFilePath);
        }

        internal override void Save(string fn)
        {
            using (var stream = _entry.OpenReader())
            {
                using (var fileS = File.OpenWrite(fn))
                {
                    stream.CopyTo(fileS);
                }
            }
        }
    }

    public class MergedModFile : ModFile
    {
        string contents;

        public override string RawContents => contents;

        public List<ModFile> SourceFiles
        {
            get;
        }

        public int SourceFileCount => SourceFiles.Count;

        public override bool HasConflicts => SourceFileCount > 1;

        public bool Resolved { get; set; }

        public MergedModFile(string path, IEnumerable<ModFile> source, Mod sourceMod)
            :base(path, sourceMod)
        {
            SourceFiles = source.ToList();
        }

        public void SaveResult(string toSave)
        {
            contents = toSave;
        }

        internal override void Save(ZipFile entry)
        {
            entry.AddEntry(Path, RawContents);
        }
        
        internal override void Save(string fn)
        {
            File.WriteAllText(fn, RawContents, Encoding.UTF8);
        }
    }
}
