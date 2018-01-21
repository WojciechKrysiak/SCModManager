using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PDXModLib.Interfaces
{
    public interface IGameConfiguration
    {
        string BasePath { get; }
        string ModsDir { get; }
        string SettingsPath { get; }
        string BackupPath { get; }
        string SavedSelections { get; }

        IReadOnlyCollection<string>  WhiteListedFiles { get; }
    }
}
