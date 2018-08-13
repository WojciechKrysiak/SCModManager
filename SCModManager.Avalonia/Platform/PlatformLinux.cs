using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SCModManager.Avalonia.Platform
{
	class PlatformLinux : IPlatfomValues
	{
		public string DefaultSteamInstallDir { get; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Steam");

		public string SteamConfigPath { get; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Steam/config/config.vdf");

		public string SettingsBasePath { get; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Paradox Interactive");
	}
}
