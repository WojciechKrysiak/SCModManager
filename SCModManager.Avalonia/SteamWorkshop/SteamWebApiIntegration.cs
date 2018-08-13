using Newtonsoft.Json;
using NLog;
using SCModManager.Avalonia.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace SCModManager.Avalonia.SteamWorkshop
{
	static class SteamWebApiIntegration
    {
        static string requestPath = "/ISteamRemoteStorage/GetPublishedFileDetails/v1/";

        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        static readonly HttpClient client = new HttpClient()
        {
            BaseAddress = new Uri("https://api.steampowered.com/")
        };

        static SteamWebApiIntegration()
        {
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        public static async Task LoadModDescriptors(IEnumerable<ModVM> mods, Action<string> onError)
        {
            var modDict = mods.Where(m => !String.IsNullOrEmpty(m.Mod.RemoteFileId)).ToDictionary(m => m.Mod.RemoteFileId, m => m);

            if (!modDict.Any())
            {
                return;
            }

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
                Log.Log(LogLevel.Warn, ex);
                onError(ex.Message);
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
                onError(response.ToString());
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
