using Gameloop.Vdf;
using Gameloop.Vdf.Linq;
using Newtonsoft.Json;
using NLog;
using SCModManager.Avalonia.Platform;
using SCModManager.Avalonia.ViewModels;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reactive.Subjects;
using System.Threading.Tasks;

namespace SCModManager.Avalonia.SteamWorkshop
{
	public interface ISteamIntegration
	{
		void GetDescriptor(ModVM mod);
		Task DownloadDescriptors(ISubject<string> onError);

		void Run(int AppId);
	}

	internal class SteamIntegration : ISteamIntegration, ISteamService
	{
		private readonly IPlatfomInterface platfomInterface;
		private ConcurrentBag<ModVM> pendingRequests = new ConcurrentBag<ModVM>();
		private ConcurrentDictionary<string, SteamWorkshopDescriptor> downloadedDescriptors = new ConcurrentDictionary<string, SteamWorkshopDescriptor>();

		static string requestPath = "/ISteamRemoteStorage/GetPublishedFileDetails/v1/";

		readonly ILogger logger;

		readonly HttpClient client = new HttpClient()
		{
			BaseAddress = new Uri("https://api.steampowered.com/")
		};

		public SteamIntegration(IPlatfomInterface platfomInterface, ILogger logger)
		{
			this.platfomInterface = platfomInterface;
			this.logger = logger;

			client.DefaultRequestHeaders.Accept.Clear();
			client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
		}

		#region Workshop

		public void GetDescriptor(ModVM mod)
		{
			if (string.IsNullOrWhiteSpace(mod.Mod.RemoteFileId))
				return;

			if (downloadedDescriptors.TryGetValue(mod.Mod.RemoteFileId, out var descriptor))
				mod.RemoteDescriptor = descriptor;

			pendingRequests.Add(mod);
		}

		public async Task DownloadDescriptors(ISubject<string> onError)
		{
			List<ModVM> toRetrieve = new List<ModVM>();

			while (pendingRequests.TryTake(out var pending))
				toRetrieve.Add(pending);

			if (!toRetrieve.Any())
			{
				return;
			}

			var modDict = toRetrieve.ToDictionary(m => m.Mod.RemoteFileId, m => m);

			string[] modIds = modDict.Select(kvp => kvp.Key).ToArray();

			var content = new FormUrlEncodedContent(
				new[] { new KeyValuePair<string, string>("itemcount", modIds.Length.ToString()) }.Concat(
					modIds.Select((mi, i) => new KeyValuePair<string, string>($"publishedfileids[{i}]", mi)))
				);

			var serializer = new JsonSerializer();
			HttpResponseMessage response;
			try
			{
				response = await client.PostAsync(requestPath, content);
			}
			catch (Exception ex)
			{
				logger.Log(LogLevel.Warn, ex);
				onError.OnNext(ex.ToString());
				return;
			}

			if (response.IsSuccessStatusCode)
			{
				await response.Content.ReadAsStringAsync().ContinueWith(ts =>
				{
					if (ts.IsCompleted)
					{
						using (var tr = new StringReader(ts.Result))
						{
							using (var reader = new JsonTextReader(tr))
							{
								var result = serializer.Deserialize<WorkshopResponseHeader>(reader);

								var descriptors = result.Response.PublishedFileDetails;

								foreach (var descriptor in descriptors)
								{
									downloadedDescriptors.TryAdd(descriptor.PublishedFileId, descriptor);

									if (modDict.ContainsKey(descriptor.PublishedFileId))
									{
										modDict[descriptor.PublishedFileId].RemoteDescriptor = descriptor;
									}
								}
							}
						}
					}
				}
				);
			} else
			{
				onError.OnNext(response.ToString());
			}
		}

		private class WorkshopResponseHeader
		{
			[JsonProperty("response")]
			public WorkshopResponseMetadata Response { get; set; }
		}

		private class WorkshopResponseMetadata
		{
			[JsonProperty("result")]
			public int Result { get; set; }

			[JsonProperty("resultcount")]
			public int ResultCount { get; set; }

			[JsonProperty("publishedfiledetails")]
			public SteamWorkshopDescriptor[] PublishedFileDetails { get; set; }
		}

		#endregion Workshop

		#region Commands

		public void Run(int AppId)
		{
			platfomInterface.LaunchUrl($"steam://run/{AppId}");
		}

		#endregion Commands

		#region Platform

		public string LocateAppInstalationDirectory(int appID)
		{
			if (!File.Exists(platfomInterface.SteamConfigPath))
			{
				logger.Error($"config.vdf not found at {platfomInterface.SteamConfigPath}, please notify the developer");
				return null;
			}

			VProperty config = null;
			logger.Info($"Loading steam config from {platfomInterface.SteamConfigPath}");

			using (var stream = File.OpenRead(platfomInterface.SteamConfigPath))
			{
				using (var reader = new StreamReader(stream))
				{
					config = new VdfSerializer().Deserialize(reader);
				}
			}

			var steamProperties = config.Value.OfType<VProperty>().FirstOrDefault(p => p.Key == "Software")?.Value.OfType<VProperty>().FirstOrDefault(p => p.Key == "Valve")?.Value.OfType<VProperty>().FirstOrDefault(p => p.Key == "Steam");

			var customPaths = steamProperties?.Value.OfType<VProperty>().Where(p => p.Key.StartsWith("BaseInstallFolder_") && p.Value is VValue).Select(p => (p.Value as VValue).Value as string) ?? Enumerable.Empty<string>();

			var paths = new[] { platfomInterface.DefaultSteamInstallDir }.Concat(customPaths).Select(p => Path.Combine(p, "steamapps"));

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

		#endregion Platform
	}
}
