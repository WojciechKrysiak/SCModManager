using SCModManager.SCFormat;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SCModManager
{
    public class ModFile
    {
        private static string[] SCExtensions = new[] { ".gfx", ".gui", ".txt" };

        private static string[] CodeExtensions = new[] { ".lua" };

        private static string[] ImageExtensions = new[] { ".dds", ".png", ".jpg" };

        public string Path { get; set; }


        protected ModFile(string path)
        {
            Path = path;
        }

        internal static ModFile Load(string path, Stream stream)
        {
            if (SCExtensions.Contains(System.IO.Path.GetExtension(path).ToLower()))
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
                        return new SCModFile(path, parser.Root, sr.ReadToEnd(), parser.ParseError);
                    }
                }

            }

            if (CodeExtensions.Contains(System.IO.Path.GetExtension(path).ToLower()))
            {
                using (var sr = new StreamReader(stream))
                {
                    return new CodeModFile(path, sr.ReadToEnd());
                }
            }

            using (var mr = new MemoryStream())
            {
                byte[] buffer = new byte[1024];
                int size = 0;
                while (stream.CanRead && ((size = stream.Read(buffer, 0, 1024)) > 0))
                {
                    mr.Write(buffer, 0, size);
                }
                return new BinaryModFile(path, mr.GetBuffer());
            }

        }
    }

    public class SCModFile : ModFile
    {
        private bool parseError;

        public SCObject Contents { get; set; }
        public string RawContents { get; set; }

        public bool ParseError
        {
            get { return parseError; }
            set { parseError = value; }
        }

        internal SCModFile(string path, SCObject contents, string rawContents)
            : base(path)
        {
            Contents = contents;
            RawContents = rawContents;
        }

        public SCModFile(string path, SCObject contents, string rawContents, bool parseError) : this(path, contents, rawContents)
        {
            this.parseError = parseError;
        }
    }

    public class CodeModFile : ModFile
    {
        public string Contents { get; set; }

        public string RawContents => Contents;

        internal CodeModFile(string path, string contents)
            : base(path)
        {
            Contents = contents;
        }
    }

    public class BinaryModFile : ModFile
    {
        public string Contents => "BinaryModFile";
        public string RawContents => Contents;

        byte[] contents;

        internal BinaryModFile(string path, byte[] contents)
            : base(path)
        {
            this.contents = contents;
        }
    }
}
