using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace SCModManager.Avalonia.Platform
{
	class PlatformLinux : IPlatfomInterface
	{
		public string DefaultSteamInstallDir { get; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Steam");

		public string SteamConfigPath { get; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Steam/config/config.vdf");

		public string SettingsBasePath { get; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Paradox Interactive");

		public void LaunchUrl(string url)
		{
			Process.Start("xdg-open", url);
		}
	}
}
