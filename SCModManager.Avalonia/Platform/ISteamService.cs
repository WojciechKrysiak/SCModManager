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

	internal class SteamService : ISteamService
	{
		private readonly IPlatfomValues platfomValues;
		private readonly ILogger logger;

		public SteamService(IPlatfomValues platfomValues, ILogger logger)
		{
			this.platfomValues = platfomValues;
			this.logger = logger;
		}

		public string LocateAppInstalationDirectory(int appID)
		{
			if (!File.Exists(platfomValues.SteamConfigPath))
			{
				logger.Error($"config.vdf not found at {platfomValues.SteamConfigPath}, please notify the developer");
				return null;
			}

			VProperty config = null;
			logger.Info($"Loading steam config from {platfomValues.SteamConfigPath}");

			using (var stream = File.OpenRead(platfomValues.SteamConfigPath))
			{
				using (var reader = new StreamReader(stream))
				{
					config = new VdfSerializer().Deserialize(reader);
				}
			}

			var steamProperties = config.Value.OfType<VProperty>().FirstOrDefault(p => p.Key == "Software")?.Value.OfType<VProperty>().FirstOrDefault(p => p.Key == "Valve")?.Value.OfType<VProperty>().FirstOrDefault(p => p.Key == "Steam");

			var customPaths = steamProperties?.Value.OfType<VProperty>().Where(p => p.Key.StartsWith("BaseInstallFolder_") && p.Value is VValue).Select(p => (p.Value as VValue).Value as string) ?? Enumerable.Empty<string>();

			var paths = new[] { platfomValues.DefaultSteamInstallDir }.Concat(customPaths).Select(p => Path.Combine(p, "steamapps"));

			var manifestName = $"appmanifest_{appID}.acf";

			var foundManifestPaths = paths.SelectMany(path => Directory.EnumerateFiles(path, manifestName, SearchOption.TopDirectoryOnly)).ToArray();

			if (foundManifestPaths.Length == 0)
			{
				logger.Error($"Can't find {manifestName} automatically");
				return null;
			}

			if (foundManifestPaths.Length > 1)
			{
				logger.Info($"Found more than one {manifestName}. Selecting the first instance");
			}

			var manifestPath = foundManifestPaths[0];

			VProperty manifest = null;
			logger.Info($"Loading app manifest from {manifestPath}");

			using (var stream = File.OpenRead(manifestPath))
			{
				using (var reader = new StreamReader(stream))
				{
					manifest = new VdfSerializer().Deserialize(reader);
				}
			}

			var installDir = (manifest.Value.OfType<VProperty>().FirstOrDefault(p => p.Key == "installdir")?.Value as VValue)?.Value as string;

			if (installDir == null)
			{
				logger.Error($"Unable to parse app manifest. Format changed?");
				return null;
			}

			var fullPath = installDir;
			if (!Path.IsPathRooted(fullPath))
				fullPath = Path.Combine(Path.GetDirectoryName(manifestPath), "common", installDir);

			logger.Info($"Found application installation dir at {fullPath}");

			return fullPath;
		}
	}
}
