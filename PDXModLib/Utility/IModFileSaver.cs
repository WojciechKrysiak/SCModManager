using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PDXModLib.Utility
{
    public interface IModFileSaver: IDisposable
    {
        void Save(string path, Stream stream);
        void Save(string path, string text, Encoding encoding);
    } 

    internal sealed class DiskFileSaver : IModFileSaver
    {
        private readonly string _basePath;

        public DiskFileSaver(string basePath)
        {
            _basePath = basePath;
        }

        public void Save(string path, Stream stream)
        {
            path = Path.Combine(_basePath, path);
            VerifyDir(path);

            using (var fileS = File.OpenWrite(path))
            {
                stream.CopyTo(fileS);
            }
        }

        public void Save(string path, string text, Encoding encoding)
        {
            path = Path.Combine(_basePath, path);
            VerifyDir(path);
            File.WriteAllText(path, text, encoding);
        }

        public void Dispose()
        {
            // do nothing, nothing to dispose of
        }

        private void VerifyDir(string path)
        {
            var dir = Path.GetDirectoryName(path);
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
        }
    }

    class ZipFileSaver : IModFileSaver
    {
        private ZipArchive _zipFile;

        public ZipFileSaver(string targetPath)
        {
            _zipFile = new ZipArchive(File.OpenWrite(targetPath), ZipArchiveMode.Create);
        }

        public void Save(string path, Stream stream)
        {
            var entry = _zipFile.CreateEntry(path);
            using (var zipStream = entry.Open())
                stream.CopyTo(zipStream);
        }

        public void Save(string path, string text, Encoding encoding)
        {
            var entry = _zipFile.CreateEntry(path);
            using (var zipStream = entry.Open())
            {
                using (var writer = new StreamWriter(zipStream, encoding))
                {
                    writer.Write(text);
                }
            }
        }

        public void Dispose()
        {
            _zipFile?.Dispose();
        }
    }
}
