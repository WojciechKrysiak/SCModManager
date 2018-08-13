using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SCModManager.Avalonia.Platform
{
	class PlatformOSX : IPlatfomValues
	{
		public string DefaultSteamInstallDir { get; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), @"/Library/Application Support/Steam/");

		public string SteamConfigPath { get; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), @"/Library/Application Support/Steam/config/config.vdf");

		public string SettingsBasePath { get; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), @"Documents/Paradox Interactive");
	}
}
