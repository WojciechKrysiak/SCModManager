using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SCModManager.Avalonia.SteamWorkshop
{
    public class SteamWorkshopDescriptor
    {
        // "publishedfileid": "922791015",
        [JsonProperty("publishedfileid")]
        public string PublishedFileId { get; set; }

        //		"result": 1,
        [JsonProperty("result")]
        public int Result { get; set; }

        //		"creator": "76561198002459966",
        [JsonProperty("creator")]
        public string Creator { get; set; }

        //		"creator_app_id": 281990,
        [JsonProperty("creator_app_id")]
        public int CreatorAppId { get; set; }

        //		"consumer_app_id": 281990,
        [JsonProperty("consumer_app_id")]
        public int ConsumerAppId { get; set; }

        //		"filename": "",
        [JsonProperty("filename")]
        public string Filename { get; set; }

        //		"file_size": 356730,
        [JsonProperty("file_size")]
        public int FileSize { get; set; }

        //		"file_url": "",
        [JsonProperty("file_url")]
        public string FileURL { get; set; }

        //		"hcontent_file": "2876523506020482522",
        [JsonProperty("hcontent_file")]
        public string HContentFile { get; set; }

        //		"preview_url": "https://steamuserimages-a.akamaihd.net/ugc/831322870602522449/5DEFE5FF58861D6EF121BCE5DC94F5D588827161/",
        [JsonProperty("preview_url")]
        public string PreviewURL { get; set; }

        //		"hcontent_preview": "831322870602522449",
        [JsonProperty("hcontent_preview")]
        public string HContentPreview { get; set; }

        //		"title": "Enhanced Gene Modding",
        [JsonProperty("title")]
        public string Title { get; set; }

        //		"description": "[h1][b]Enhanced Gene Modding[/b] by ParasiteX[/h1]\r\n\r\nThis mod will give you an extra menu that will pop-up after successfully gene modding a species, and will give you extra customization options.\r\n\r\n[b]These options include:[/b]\r\n[list][*]Rename species.\r\n[*]Change appearance of species.\r\n[*]Modify all leaders, armies and colonizers of same sub-species to new genemod species.\r\n[*]Set gene modded species as your new dominant species (Founder).\r\n[*]Convert a species you have gene modded into your dominant species, or a subspecies of them.\r\n[*]Convert a species into synthetics.\r\n[*]Designate a new homeworld for species within your empire.[/list]\r\n\r\nCurrently you can only change appearance of your species to the built-in and DLC exclusive species portraits.\r\nYou will need to have the DLC installed to be able to apply any of the DLC portraits.\r\n\r\nI also included the option to add a dummy trait. This is useful for when you need to merge two sub-species together that have identical traits and appearance, but different names.\r\nRead the in-game tooltip for more detailed instructions for how to use it. But in short, you need to add the dummy trait before you rename the species, if you plan to merge two subspecies together.\r\nAlso keep in mind that the traits have to be in the exact same order as the icons listed in the species menu.\r\n\r\nYou can only have one gene modding menu up at a time. This was made this way to avoid scripting conflicts.\r\nSo avoid genemodding other species while the window is still up.\r\n\r\nThe ability to convert a species into a subspecies of your dominant species requiers that you have the Genetic Resequencing tech from the Evolutionary Mastery Acsension Perk.\r\nConverting a species into synthetics requiers the Syntetic Evolution Perk as well as your dominant species converted to synths.\r\nYou can not convert species that are already a sub-species of your dominant species.\r\n\r\nShort delay of about 2 in-game days before gene modding menu pops up after uplifting a species.\r\nCustomization options when uplifting is limited to only renaming and changing apperance.\r\n\r\n[b]Compatibility:[/b]\r\nShould be very compatible with most mods without any major issues. It does not edit any vanilla files.\r\n\r\n[i]Not compatible with Ironman achievements[/i]\r\n\r\n[b]Translations:[/b]\r\n[url=http://steamcommunity.com/sharedfiles/filedetails/?edit=true&id=925635154]Russian [i]by Banana Pineapple[/i][/url]\r\n[url=https://steamcommunity.com/sharedfiles/filedetails/?id=927394708]Korean [i]by Ratori[/i][/url]\r\n\r\n[b]Possible Issues:[/b]\r\nEvery-time you change the appearance of a species, it will generate a new species entry that gets saved into memory. This can potentially lead to save and memory bloat, if you change appearance and genemod an excessive amount of times. The game does not delete old species entries. Even if there are no more pops of the species left in-game.\r\nBut if you're changing the appearance to match an already existing sub-species, with same name and traits. Then the game will automatically merge the two species together.\r\nSo in general, try to only apply an appearance when you’re absolutely sure about your choice.\r\n\r\n[b]Known Issues:[/b]\r\n[list][*]Shows a \"???\" in upper left corner of menu.\r\n[*]Can not change apperance of ruler you start the game with. Only workaround currently is to use the console command \"kill_ruler\" to replace the leader with one that doesnt have a pre-designed apperance.[/list] \r\n\r\n[b]Check out my other mods:[/b]\r\n[url=http://steamcommunity.com/sharedfiles/filedetails/?id=701739734]Color coded pop status icons[/url]",
        [JsonProperty("description")]
        public string Description { get; set; }

        //		"time_created": 1494387342,
        [JsonProperty("time_created")]
        public long TimeCreated { get; set; }

        //		"time_updated": 1494773883,
        [JsonProperty("time_updated")]
        public long TimeUpdate { get; set; }

        //		"visibility": 0,
        [JsonProperty("visibility")]
        public int Visibility { get; set; }

        //		"banned": 0,
        [JsonProperty("banned")]
        public int Banned { get; set; }

        //		"ban_reason": "",
        [JsonProperty("ban_reason")]
        public string BanReason { get; set; }

        //		"subscriptions": 11271,
        [JsonProperty("subscriptions")]
        public int Subscriptions { get; set; }

        //		"favorited": 528,
        [JsonProperty("favorited")]
        public int Favorited { get; set; }

        //		"lifetime_subscriptions": 13277,
        [JsonProperty("lifetime_subscriptions")]
        public int LifetimeSubscriptions { get; set; }

        //		"lifetime_favorited": 567,
        [JsonProperty("lifetime_favorited")]
        public int LifetimeFavorited { get; set; }

        //		"views": 46771,
        [JsonProperty("views")]
        public int Views { get; set; }

        //		"tags": [
        [JsonProperty("tags")]
        public SteamWorkshopTag[] Tags { get; set; }

        public List<KeyValuePair<string, string>> DisplayValues => new List<KeyValuePair<string,string>>
        {
         new KeyValuePair<string, string>("Title", Title),
         new KeyValuePair<string, string>("Tags", string.Join(",", Tags.Select(t => t.Tag))),
         new KeyValuePair<string, string>("Created",  new DateTime(1970,1,1,0,0,0,0, DateTimeKind.Utc).AddSeconds(TimeCreated).ToLocalTime().ToShortDateString()),
         new KeyValuePair<string, string>("Modified", new DateTime(1970,1,1,0,0,0,0, DateTimeKind.Utc).AddSeconds(TimeUpdate).ToLocalTime().ToShortDateString()),
         new KeyValuePair<string, string>("Subscriptions", LifetimeSubscriptions.ToString()),
         new KeyValuePair<string, string>("Favorited", Favorited.ToString())
        };

    }

    public class SteamWorkshopTag
    {
        [JsonProperty("tag")]
        public string Tag { get; set; }
    }

}
