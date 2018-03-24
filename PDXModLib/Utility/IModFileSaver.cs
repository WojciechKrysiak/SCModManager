using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ionic.Zip;

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
        private ZipFile _zipFile;

        public ZipFileSaver(string targetPath)
        {
            _zipFile = new ZipFile(targetPath);
        }

        public void Save(string path, Stream stream)
        {
            _zipFile.AddEntry(path, stream);
        }

        public void Save(string path, string text, Encoding encoding)
        {
            _zipFile.AddEntry(path, text, encoding);
        }

        public void Dispose()
        {
            _zipFile?.Dispose();
        }
    }
}
