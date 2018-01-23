using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ionic.Zip;

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
        private readonly ZipEntry _zipEntry;

        public Stream OpenStream()
        {
            return _zipEntry.OpenReader();
        }

        public ZipFileLoader(ZipEntry zipEntry)
        {
            _zipEntry = zipEntry;
        }
    }
}
