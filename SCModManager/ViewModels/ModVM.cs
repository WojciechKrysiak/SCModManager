using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using PDXModLib.ModData;
using ReactiveUI;
using SCModManager.SteamWorkshop;

namespace SCModManager.ViewModels
{
    public class ModVM : ReactiveObject
    {
        private SteamWorkshopDescriptor _remoteDescriptor;
        private bool _selected;

        public ModVM(ModConflictDescriptor modConflict, bool selected)
        {
            ModConflict = modConflict;
            Selected = selected;
        }

        public ModConflictDescriptor ModConflict { get; }

        public ImageSource Image { get; private set; }

        public Mod Mod => ModConflict.Mod;

        public string Id => Mod.Id;
        public string Name => Mod.Name;

        public bool ParseError => Mod.ParseError;

        public int ConflictCount => ModConflict.ConflictingMods.Count();

        public SteamWorkshopDescriptor RemoteDescriptor
        {
            get { return _remoteDescriptor; }
            set { this.RaiseAndSetIfChanged(ref _remoteDescriptor, value); }
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
    }
}
