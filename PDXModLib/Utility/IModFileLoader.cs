using System;
using ICSharpCode.SharpZipLib.Zip;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PDXModLib.Utility
{
    interface IModFileLoader
    {
        Stream OpenStream();
    }
    
    public class DiskFileLoader : IModFileLoader
    {
        private readonly string _path;

        public Stream OpenStream()
        {
            return File.OpenRead(_path);
        }

        public DiskFileLoader(string path)
        {
            _path = path;
        }
    }

    public class ZipFileLoader : IModFileLoader
    {
		private readonly ZipFile file;
		private readonly ZipEntry _zipEntry;

        public Stream OpenStream()
        {
			return file.GetInputStream(_zipEntry);
        }

        public ZipFileLoader(ZipFile file, ZipEntry zipEntry)
        {
			this.file = file;
			_zipEntry = zipEntry;
        }
    }
}
