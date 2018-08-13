using Avalonia.Media.Imaging;
using Avalonia.Threading;
using PDXModLib.ModData;
using ReactiveUI;
using SCModManager.Avalonia.SteamWorkshop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace SCModManager.Avalonia.ViewModels
{
	public class ModVM : ReactiveObject
	{
		private SteamWorkshopDescriptor _remoteDescriptor;
		private bool _selected;
		private int _conflictCount;
		private bool _hasConflictWithSelected;
		private IBitmap _remoteImage;
		bool isLoading;

		public ModVM(ModConflictDescriptor modConflict, bool selected)
		{
			ModConflict = modConflict;
			Selected = selected;
			_conflictCount = ModConflict.ConflictingMods.Count();
		}

		public ModConflictDescriptor ModConflict { get; }

		public Mod Mod => ModConflict.Mod;

		public string Id => Mod.Id;
		public string Name => Mod.Name;

		public bool ParseError => Mod.ParseError;

		public int ConflictCount => _conflictCount;

		public SteamWorkshopDescriptor RemoteDescriptor
		{
			get { return _remoteDescriptor; }
			set
			{
				this.RaiseAndSetIfChanged(ref _remoteDescriptor, value);
				this.RaisePropertyChanged(nameof(Image));
			}
		}

		public IBitmap Image
		{
			get {
				if (_remoteImage == null && !isLoading && _remoteDescriptor != null) 
				{
					isLoading = true;
					
					Task.Run(async () => await DownloadImage(_remoteDescriptor?.PreviewURL));
				}
				return _remoteImage;
			}
			set
			{
				isLoading = false;
				this.RaiseAndSetIfChanged(ref _remoteImage, value);
			}
		}


		public IEnumerable<KeyValuePair<string, string>> DisplayValues
		{
			get
			{
				var retval = new Dictionary<string, string>();

				retval.Add("Title", RemoteDescriptor?.Title ?? Mod.Name);
				if (RemoteDescriptor != null)
				{
					retval.Add("Tags", string.Join(",", RemoteDescriptor.Tags?.Select(t => t.Tag) ?? new string[0]));
					retval.Add("Created", new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc).AddSeconds(RemoteDescriptor.TimeCreated).ToLocalTime().ToShortDateString());
					retval.Add("Modified", new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc).AddSeconds(RemoteDescriptor.TimeUpdate).ToLocalTime().ToShortDateString());
					retval.Add("Subscriptions", RemoteDescriptor.LifetimeSubscriptions.ToString());
					retval.Add("Favorited", RemoteDescriptor.Favorited.ToString());
				}
				else
				{
					retval.Add("Tags", string.Join(",", Mod.Tags));
				}

				retval.Add("Supported version", Mod.SupportedVersion.ToString());

				return retval;
			}
		}

		public bool Selected
		{
			get { return _selected; }
			set { this.RaiseAndSetIfChanged(ref _selected, value); }
		}

		public void UpdateModFilter(Func<Mod, bool> filter)
		{
			this.RaiseAndSetIfChanged(ref _conflictCount, ModConflict.ConflictingMods.Count(filter),
				nameof(ConflictCount));
		}

		public bool HasConflictWithSelected
		{
			get { return _hasConflictWithSelected; }
			set { this.RaiseAndSetIfChanged(ref _hasConflictWithSelected, value); }
		}

		private async Task DownloadImage(string url)
		{
			if (string.IsNullOrEmpty(url) ||
				!(url.StartsWith("http://") || url.StartsWith("https://")))
				return;

			using (var client = new HttpClient())
			{
				var result = await client.GetAsync(url);

				if (result.IsSuccessStatusCode)
				{
					var stream = await result.Content.ReadAsStreamAsync();
					await Dispatcher.UIThread.InvokeAsync(() => Image = new Bitmap(stream));
				}
			}
		}
	}
}
