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

namespace SCModManager
{
    public abstract class ModFile : ObservableObject
    {
        private static string[] SCExtensions = new[] { ".gfx", ".gui", ".txt" };

        private static string[] CodeExtensions = new[] { ".lua" };

        private static string[] ImageExtensions = new[] { ".dds", ".png", ".jpg" };

        public string Path { get; set; }

        public string Directory => System.IO.Path.GetDirectoryName(Path);
        public string Filename => System.IO.Path.GetFileName(Path);

        public List<Mod> Conflicts { get; } = new List<Mod>();

        public bool HasConflicts => Conflicts.Count > 0;

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
    }
}
