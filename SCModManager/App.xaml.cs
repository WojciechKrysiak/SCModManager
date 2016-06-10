using SCModManager.SCFormat;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace SCModManager
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            var path = @"D:\00_planet_classes.txt";

            using (var mr = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                var parser = new Parser(new Scanner(mr));

                parser.Parse();

                var rr = parser.Root;

                return;
            }
        }
    }
}
