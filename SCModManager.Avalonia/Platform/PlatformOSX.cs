using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace SCModManager.Avalonia.Platform
{
	class PlatformOSX : IPlatfomInterface
	{
		public string DefaultSteamInstallDir { get; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), @"/Library/Application Support/Steam/");

		public string SteamConfigPath { get; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), @"/Library/Application Support/Steam/config/config.vdf");

		public string SettingsBasePath { get; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), @"Documents/Paradox Interactive");

		public void LaunchUrl(string url)
		{
			Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
		}
	}
}
