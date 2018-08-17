using System;
using System.Collections.Generic;
using System.Text;

namespace SCModManager.Avalonia.Platform
{
    public interface IPlatfomInterface
    {
		string DefaultSteamInstallDir { get; }
		string SteamConfigPath { get; }
		string SettingsBasePath { get; }

		void LaunchUrl(string url);
	}
}
