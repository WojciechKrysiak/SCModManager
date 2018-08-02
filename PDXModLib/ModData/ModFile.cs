using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using ICSharpCode.SharpZipLib.Zip;
using NLog;
using PDXModLib.Interfaces;
using PDXModLib.Utility;
using static CWTools.Process.CK2Process;

namespace PDXModLib.ModData
{
    public abstract class ModFile
    {
        private static readonly Encoding NoBomEncoding = new UTF8Encoding(false, true);
        private static readonly string[] SCExtensions = { ".gfx", ".gui", ".txt" };

        private static readonly string[] CodeExtensions = { ".lua" };

        private static readonly string[] LocalisationExtensions = { ".yml" };

        public string Path { get; set; }

        public string Directory => System.IO.Path.GetDirectoryName(Path);
        public string Filename => System.IO.Path.GetFileName(Path);

        protected static readonly Logger Log = LogManager.GetCurrentClassLogger();

        internal virtual Encoding BoMEncoding { get; } = NoBomEncoding;

        public Mod SourceMod { get; }

        protected ModFile(string path, Mod sourceMod)
        {
            Path = path.Replace(@"/", @"\");
            SourceMod = sourceMod;
        }

        public abstract string RawContents { get; }

        internal static ModFile Load(IModFileLoader loader, string path , Mod sourceMod)
        {
            if (SCExtensions.Contains(System.IO.Path.GetExtension(path).ToLower()))
            {
                return new SCModFile(loader, path, sourceMod);
            }

            if (CodeExtensions.Contains(System.IO.Path.GetExtension(path).ToLower()))
            {
                return new CodeModFile(loader, path, sourceMod);
            }

            if (LocalisationExtensions.Contains(System.IO.Path.GetExtension(path).ToLower()))
            {
                return new LocalisationFile(loader, path, sourceMod);
            }

            return new BinaryModFile(loader, path, sourceMod);
        }

        protected static string NormalizeLineEndings(string source)
        {
            return Regex.Replace(source, @"\r\n|\n\r|\n|\r", Environment.NewLine);
        }

        public virtual void Save(IModFileSaver saver)
        {
            saver.Save(Path, RawContents, BoMEncoding);
        }

		public override string ToString()
		{
			return Filename;
		}
	}

    internal class SCModFile : ModFile
    {
        private readonly IModFileLoader _loader;
        private string _rawContents;
        internal EventRoot Contents { get; private set; }

        public override string RawContents
        {
            get
            {
                if (_rawContents == null)
                {
                    _rawContents = NormalizeLineEndings(LoadSCFileContents(_loader));
                }
                return _rawContents;
            }
        }

        internal bool ParseError { get; private set; }

        public SCModFile(IModFileLoader loader, string path, Mod sourceMod)
            : base(path, sourceMod)
        {
            _loader = loader;
        }

        private string LoadSCFileContents(IModFileLoader loader)
        {
            using (var stream = loader.OpenStream())
            {
                using (var sr = new StreamReader(stream))
                {
					var contents = sr.ReadToEnd();

					var adapter = CWToolsAdapter.Parse(Path, contents);

                    ParseError = adapter.ParseError != null;
                    Contents = adapter.Root;
					return contents;
                }
            }
        }
    }

    internal class CodeModFile : ModFile
    {
        private readonly IModFileLoader _loader;
        public string Contents { get; set; }
        private string _rawContents;

        public override string RawContents
        {
            get
            {
                if (_rawContents == null)
                {
                    using (var stream = _loader.OpenStream())
                    {
                        using (var sr = new StreamReader(stream))
                        {
                            _rawContents = NormalizeLineEndings(sr.ReadToEnd());
                        }
                    }
                }
                return _rawContents;
            }
        }

        public CodeModFile(IModFileLoader loader, string path, Mod sourceMod)
            : base(path, sourceMod)
        {
            _loader = loader;
        }
    }

    internal class LocalisationFile : ModFile
    {
        private readonly IModFileLoader _loader;
        public string Contents { get; set; }
        private string _rawContents;

        internal override Encoding BoMEncoding { get; } = Encoding.UTF8;

        public override string RawContents
        {
            get
            {
                if (_rawContents == null)
                {
                    using (var stream = _loader.OpenStream())
                    {
                        using (var sr = new StreamReader(stream))
                        {
                            _rawContents = NormalizeLineEndings(sr.ReadToEnd());
                        }
                    }
                }
                return _rawContents;
            }
        }

        public LocalisationFile(IModFileLoader loader, string path, Mod sourceMod)
            : base(path, sourceMod)
        {
            _loader = loader;
        }
    }

    internal class BinaryModFile : ModFile
    {
        private readonly IModFileLoader _loader;
        public string Contents => "BinaryModFile";
        public override string RawContents => Contents;

        public BinaryModFile(IModFileLoader loader, string path, Mod sourceMod)
            : base(path, sourceMod)
        {
            _loader = loader;
        }

        public override void Save(IModFileSaver saver)
        {
            saver.Save(Path, _loader.OpenStream);
        }
    }

    public class MergedModFile : ModFile
    {
        string contents;
        private readonly List<ModFile> _sourceFiles;

        internal override Encoding BoMEncoding { get; }

        public override string RawContents => GetContents();

        public IReadOnlyList<ModFile> SourceFiles => _sourceFiles;

        public int SourceFileCount => SourceFiles.Count;

        public bool Resolved => contents != null ? SourceFileCount == 0 : SourceFileCount == 1;

        public MergedModFile(string path, IEnumerable<ModFile> source, Mod sourceMod)
            :base(path, sourceMod)
        {
            _sourceFiles = source.ToList();
            BoMEncoding = _sourceFiles[0].BoMEncoding;
        }

        public void SaveResult(string toSave)
        {
            contents = toSave;
        }

        public void RemoveSourceFile(ModFile file)
        {
            _sourceFiles.Remove(file);
        }

        public override void Save(IModFileSaver saver)
        {
            if (Resolved)
                base.Save(saver);
            else
            {
                var extension = System.IO.Path.GetExtension(Path);
                var newPath = System.IO.Path.ChangeExtension(Path, $"{extension}.mzip");
                saver.Save(newPath, CreateMergeZip);
            }
        }

        private string GetContents()
        {
            if (contents != null)
                return contents;

            if (SourceFileCount == 1)
                return SourceFiles[0].RawContents;

            return null;
        }

        private MemoryStream CreateMergeZip()
        {
            var result = new MemoryStream();
            using (var saver = new MergeZipFileSaver(result))
            {
                foreach (var sourceFile in SourceFiles)
                {
                    sourceFile.Save(saver);
                }
            }

            return result;
        }

        private class MergeZipFileSaver : IModFileSaver
        {
            private int _index;
            private ZipFile _zipFile; 

            public MergeZipFileSaver(Stream outputStream)
            {
				_zipFile = ZipFile.Create(outputStream);
				_zipFile.BeginUpdate(new MemoryArchiveStorage());
			}

			public void Save(string path, Func<Stream> getStream)
			{
				_zipFile.Add(new FunctorDataSource(getStream), GetPath(path));
			}

			public void Save(string path, string text, Encoding encoding)
			{
				_zipFile.Add(new FunctorDataSource(text, encoding), GetPath(path));
			}

            public void Dispose()
            {
				_zipFile.CommitUpdate();
                _zipFile?.Close();
            }

            private string GetPath(string path)
            {
                var filename = System.IO.Path.GetFileName(path);
                var i = _index++;
                return $"{i:00}/{filename}";
            }
        }
    }
}
