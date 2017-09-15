using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KokoroIO.XamarinForms.Helpers;
using Shipwreck.KokoroIO;
using Xamarin.Forms;
using XDevice = Xamarin.Forms.Device;

namespace KokoroIO.XamarinForms.ViewModels
{
    public sealed class ApplicationViewModel
    {
        internal ApplicationViewModel(Client client, Profile me)
        {
            Client = client;
            OpenUrlCommand = new Command(OpenUrl);

            client.MessageCreated += Client_MessageCreated;
            client.MessageUpdated += Client_MessageUpdated;
        }

        internal Client Client { get; }

        #region Rooms

        private bool _RoomsLoaded;

        private ObservableRangeCollection<RoomViewModel> _Rooms;

        public ObservableRangeCollection<RoomViewModel> Rooms
            => InitRooms()._Rooms;

        private ApplicationViewModel InitRooms()
        {
            if (_Rooms == null)
            {
                _Rooms = new ObservableRangeCollection<RoomViewModel>();
            }

            if (!_RoomsLoaded)
            {
                _RoomsLoaded = true;
                LoadRooms();
            }

            return this;
        }

        private async void LoadRooms()
        {
            var rooms = await Client.GetRoomsAsync();
            foreach (var r in rooms.OrderBy(e => (int)e.Kind).ThenBy(e => e.ChannelName).ThenBy(e => e.Id))
            {
                var rvm = new RoomViewModel(this, r);

                _Rooms.Add(rvm);
            }

            if (rooms.Any())
            {
                try
                {
                    await Client.ConnectAsync();
                    await Client.SubscribeAsync(rooms);
                }
                catch { }
            }
        }

        #endregion Rooms

        #region Profiles

        private readonly Dictionary<string, ProfileViewModel> _Profiles = new Dictionary<string, ProfileViewModel>();

        internal ProfileViewModel GetProfile(Message model)
        {
            var key = model.Profile.Id + '\0' + model.Avatar + '\0' + model.DisplayName;

            if (!_Profiles.TryGetValue(key, out var p))
            {
                p = new ProfileViewModel(model.Profile.Id, model.Avatar, model.DisplayName);
                _Profiles[key] = p;
            }
            return p;
        }

        internal ProfileViewModel GetProfile(Profile model)
        {
            var key = model.Id + '\0' + model.Avatar + '\0' + model.DisplayName;

            if (!_Profiles.TryGetValue(key, out var p))
            {
                p = new ProfileViewModel(model);
                _Profiles[key] = p;
            }
            return p;
        }

        #endregion Profiles

        public Command OpenUrlCommand { get; }

        private void OpenUrl(object url)
        {
            var u = url as Uri ?? (url is string s ? new Uri(s) : null);

            if (u != null)
            {
                XDevice.OpenUri(u);
            }
        }

        public async Task ConnectAsync()
        {
            // TODO: disable push notification

            await Client.ConnectAsync();
            await Client.SubscribeAsync(Rooms.Select(r => r.Id));
        }

        public async Task CloseAsync()
        {
            await Client.CloseAsync();

            // TODO: enable push notification
        }

        private void Client_MessageCreated(object sender, EventArgs<Message> e)
        {
            var rvm = _Rooms?.FirstOrDefault(r => r.Id == e.Data.Room.Id);
            if (rvm != null)
            {
                rvm.UnreadCount++;
                try
                {
                    DependencyService.Get<IAudioService>()?.PlayNotification();
                }
                catch { }
            }
        }

        private void Client_MessageUpdated(object sender, EventArgs<Message> e)
        {
            var mp = _Rooms?.FirstOrDefault(r => r.Id == e.Data.Room.Id)?.MessagesPage;
            if (mp != null)
            {
                mp.UpdateMessage(e.Data);
            }
        }
    }
}