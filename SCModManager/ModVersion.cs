using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SCModManager
{
    public class ModVersion
    {
        public string Major { get; }
        public string Minor { get; }
        public string Revision { get; }

        public ModVersion(string version)
        {
            Major = Minor = Revision = "*";

            if (version == null)
            {
                return;
            }

            var elements = version?.Split('.');

            if (elements.Length > 0)
                Major = elements[0];

            if (elements.Length > 1)
                Minor = elements[1];

            if (elements.Length > 2)
                Revision = elements[2];
        }


        public bool IsCompatible(ModVersion other)
        {
            if (Major == "*" || other.Major == "*")
                return true;

            if (Major != other.Major)
                return false;

            if (Minor == "*" || other.Minor == "*")
                return true;

            if (Minor != other.Minor)
                return false;

            if (Revision == "*" || other.Revision == "*")
                return true;

            return Revision != other.Revision;
        }

        public override string ToString()
        {
            return $"{Major}.{Minor}.{Revision}";
        }
    }
}
