using Newtonsoft.Json;
using NLog;
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
	}

	internal class SteamIntegration : ISteamIntegration 
    {
		private ConcurrentBag<ModVM> pendingRequests = new ConcurrentBag<ModVM>();
		private ConcurrentDictionary<string, SteamWorkshopDescriptor> downloadedDescriptors = new ConcurrentDictionary<string, SteamWorkshopDescriptor>();

		static string requestPath = "/ISteamRemoteStorage/GetPublishedFileDetails/v1/";

		readonly ILogger logger;

        readonly HttpClient client = new HttpClient()
        {
            BaseAddress = new Uri("https://api.steampowered.com/")
        };

        public SteamIntegration(ILogger logger)
        {
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

		public void GetDescriptor(ModVM mod)
		{
			if (string.IsNullOrWhiteSpace(mod.Mod.RemoteFileId))
				return;

			if (downloadedDescriptors.TryGetValue(mod.Mod.RemoteFileId, out var descriptor));
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
    }
}
