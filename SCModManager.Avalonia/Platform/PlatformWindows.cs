using Microsoft.Win32;
using NLog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace SCModManager.Avalonia.Platform
{
	class PlatformWindows : IPlatfomInterface
	{
		private string fallbackSteamPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), @"Steam\");

		public string DefaultSteamInstallDir { get; } 

		public string SteamConfigPath { get; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), @"Steam\config\config.vdf");

		public string SettingsBasePath { get; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), @"Paradox Interactive");

		public PlatformWindows(ILogger logger)
		{
			const string keyPath = @"Software\Valve\Steam";
			try
			{
				using (var key = Registry.CurrentUser.OpenSubKey(keyPath))
				{
					if (key != null)
					{
						var value = key.GetValue("SteamPath");
						if (value != null)
							DefaultSteamInstallDir = value as string;
					}
				}

				if (DefaultSteamInstallDir != null)
					return;

				using (var key = Registry.LocalMachine.OpenSubKey(keyPath))
				{
					if (key != null)
					{
						var value = key.GetValue("SteamPath");
						if (value != null)
							DefaultSteamInstallDir = value as string;
					}
				}

				if (DefaultSteamInstallDir == null)
					DefaultSteamInstallDir = fallbackSteamPath;

			}
			catch (Exception e)
			{
				logger.Error(e, "Exception caught while trying to read registry");
				DefaultSteamInstallDir = fallbackSteamPath;
			}
		}

		public void LaunchUrl(string url)
		{
			Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
		}
	}
}
