using ICSharpCode.SharpZipLib.Zip;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PDXModLib.Utility
{
    public interface IModFileSaver: IDisposable
    {
        void Save(string path, Func<Stream> getStream);
        void Save(string path, string text, Encoding encoding);
    } 

    internal sealed class DiskFileSaver : IModFileSaver
    {
        private readonly string _basePath;

        public DiskFileSaver(string basePath)
        {
            _basePath = basePath;
        }

        public void Save(string path, Func<Stream> getStream)
        {
            path = Path.Combine(_basePath, path);
            VerifyDir(path);
			using (var stream = getStream())
			{
				using (var fileS = File.OpenWrite(path))
				{
					stream.CopyTo(fileS);
				}
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

	class FunctorDataSource : IStaticDataSource
	{
		private readonly Func<Stream> accessor;

		public FunctorDataSource(Func<Stream> accessor)
		{
			this.accessor = accessor;
		}

		public FunctorDataSource(string contents, Encoding encoding)
		{
			this.accessor = () => new MemoryStream(encoding.GetBytes(contents));
		}

		public Stream GetSource()
		{
			return accessor();
		}
	}

	class ZipFileSaver : IModFileSaver
    {
        private ZipFile _zipFile;

        public ZipFileSaver(string targetPath)
        {
            _zipFile = ZipFile.Create(File.OpenWrite(targetPath));
			_zipFile.BeginUpdate(new MemoryArchiveStorage() );
        }

        public void Save(string path, Func<Stream> getStream)
        {
			_zipFile.Add(new FunctorDataSource(getStream), path);
        }

        public void Save(string path, string text, Encoding encoding)
        {
			_zipFile.Add(new FunctorDataSource(text, encoding), path);
        }

        public void Dispose()
        {
			_zipFile.CommitUpdate();
            _zipFile?.Close();
        }
	}
}
