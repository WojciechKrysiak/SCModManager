using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PDXModLib.ModData;

namespace PDXModLib.GameContext
{
    public class ModSelection
    {
        private static int _counter = 0;

        public int Idx { get; set; }

        public string Name { get; set; }

        public List<Mod> Contents { get; } = new List<Mod>();

        public ModSelection(string name)
        {
            Name = name;
            Idx = _counter++;
        }
    }
}
