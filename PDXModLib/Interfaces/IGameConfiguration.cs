using System;
using System.Collections.Generic;
using System.Text;

namespace PDXModLib.Interfaces
{
    public interface IGameConfiguration :IDefaultGameConfiguration
    {
		bool SettingsDirectoryValid { get; }

		bool GameDirectoryValid { get; }
	}
}
