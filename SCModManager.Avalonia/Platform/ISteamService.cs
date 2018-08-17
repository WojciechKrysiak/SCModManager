using Gameloop.Vdf;
using Gameloop.Vdf.Linq;
using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace SCModManager.Avalonia.Platform
{
    public interface ISteamService
    {
		string LocateAppInstalationDirectory(int appID);
    }
		
}
