using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace KokoroIO.XamarinForms.ViewModels
{
    public sealed class MessagesViewModel : BaseViewModel
    {
        internal MessagesViewModel(ChannelViewModel channel)
        {
            Channel = channel;
            Title = channel.DisplayName;
            _IsArchiveBannerShown = channel.IsArchived;

            PrependCommand = new Command(BeginPrepend);
            AppendCommand = new Command(BeginAppend);
            RefreshCommand = new Command(BeginRefresh);

            Channel.PropertyChanged += Channel_PropertyChanged;
        }

        #region Channel

        public ChannelViewModel Channel { get; }

        private void Channel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(Channel.IsArchived):
                    OnPropertyChanged(nameof(CanPost));
                    break;
            }
        }

        private ObservableRangeCollection<ProfileViewModel> _Members;

        public ObservableRangeCollection<ProfileViewModel> Members
        {
            get
            {
                if (_Members == null)
                {
                    _Members = Channel.Members;
                    _Members.CollectionChanged += (_, __) => ProfileCandicates.UpdateResult();
                }
                return _Members;
            }
        }

        #endregion Channel

        public ApplicationViewModel Application => Channel.Application;

        public Command OpenUrlCommand => Application.OpenUrlCommand;

        #region Showing Messages

        private int? _MaxId;
        private int? _MinId;

        #region Messages

        private InsertableObservableRangeCollection<MessageInfo> _Messages;

        public ObservableRangeCollection<MessageInfo> Messages
        {
            get
            {
                if (_Messages == null)
                {
                    _Messages = new InsertableObservableRangeCollection<MessageInfo>();
                    BeginLoadMessages().GetHashCode();
                }
                return _Messages;
            }
        }

        #endregion Messages

        #region HasPrevious

        private bool _HasPrevious = true;

        public bool HasPrevious
        {
            get => _HasPrevious;
            private set => SetProperty(ref _HasPrevious, value);
        }

        #endregion HasPrevious

        #region HasNext

        private bool _HasNext = true;

        public bool HasNext
        {
            get => _HasNext;
            private set => SetProperty(ref _HasNext, value);
        }

        #endregion HasNext

        public void BeginPrepend()
            => BeginLoadMessages(true).GetHashCode();

        public void BeginAppend()
            => BeginLoadMessages(false).GetHashCode();

        // TODO: increase page size by depth
        private const int PAGE_SIZE = 30;

        public async void BeginRefresh()
        {
            ClearPopupCommand.Execute(null);

            if (IsBusy)
            {
                return;
            }

            try
            {
                IsBusy = true;

                Message[] messages;
                if (Messages.Count == 0)
                {
                    messages = await Application.GetMessagesAsync(Channel.Id, limit: PAGE_SIZE);
                }
                else if (Messages.Count <= PAGE_SIZE)
                {
                    messages = await Application.GetMessagesAsync(Channel.Id, limit: PAGE_SIZE, afterId: Messages.First().Id);
                }
                else
                {
                    var shown = Messages.Where(e => e.IsShown).ToList();
                    var center = shown.Any() ? shown.Skip(shown.Count >> 1).First() : Messages.Last();
                    var last = Messages.SkipWhile(e => e != center).Take(PAGE_SIZE >> 1).LastOrDefault() ?? Messages.Last();
                    var first = Messages.Reverse().SkipWhile(e => e != last).Take(PAGE_SIZE + 1).LastOrDefault() ?? Messages.First();

                    messages = await Application.GetMessagesAsync(Channel.Id, afterId: first.Id, limit: PAGE_SIZE);
                }

                MessagesLoaded = true;

                if (messages.Any())
                {
                    _MinId = Math.Min(messages.Min(m => m.Id), _MinId ?? int.MaxValue);
                    _MaxId = Math.Max(messages.Max(m => m.Id), _MaxId ?? int.MinValue);
                    InsertMessages(messages);
                }
                _SelectedMessageId = null;
            }
            catch (Exception ex)
            {
                ex.Error("Load message failed");

                MessagingCenter.Send(this, "LoadMessageFailed");
            }
            finally
            {
                IsBusy = false;
            }
        }

        internal bool MessagesLoaded { get; private set; }

        private async Task BeginLoadMessages(bool prepend = false)
        {
            if (IsBusy || (Channel.IsArchived && Channel.UnreadCount <= 0 && !prepend && _Messages?.Count > 0))
            {
                return;
            }

            try
            {
                IsBusy = true;

                // int? bid, aid;
                Message[] messages;

                if (Messages.Count == 0)
                {
                    // initlial loading

                    messages = null;

                    // TODO: get first message id from argument

                    var tid = _SelectedMessageId
                                ?? (Channel.ReadStateTrackingPolicy != ReadStateTrackingPolicy.ConsumeLast ? Channel.LatestReadMessageId : null);

                    if (tid > 0)
                    {
                        var afts = await Application.GetMessagesAsync(Channel.Id, PAGE_SIZE, afterId: tid);

                        if (afts.Any())
                        {
                            var befores = await Application.GetMessagesAsync(Channel.Id, PAGE_SIZE, beforeId: afts.Min(m => m.Id));

                            messages = befores.Any() ? befores.Concat(afts).ToArray() : afts;
                            HasNext = afts.Length == PAGE_SIZE;
                            HasPrevious = befores.Length == PAGE_SIZE;
                        }
                    }

                    if (messages == null)
                    {
                        messages = await Application.GetMessagesAsync(Channel.Id, PAGE_SIZE);
                    }
                }
                else if (prepend)
                {
                    if (!HasPrevious)
                    {
                        return;
                    }

                    messages = await Application.GetMessagesAsync(Channel.Id, PAGE_SIZE, beforeId: _MinId);
                    HasPrevious = messages.Length == PAGE_SIZE;
                }
                else
                {
                    messages = await Application.GetMessagesAsync(Channel.Id, PAGE_SIZE, afterId: _MaxId);

                    HasNext = messages.Length == PAGE_SIZE;
                }

                MessagesLoaded = true;

                if (messages.Any())
                {
                    _MinId = Math.Min(messages.Min(m => m.Id), _MinId ?? int.MaxValue);
                    _MaxId = Math.Max(messages.Max(m => m.Id), _MaxId ?? int.MinValue);
                    InsertMessages(messages);

                    if (_SelectedMessageId != null)
                    {
                        SelectedMessage = Messages.FirstOrDefault(m => m.Id == _SelectedMessageId) ?? _SelectedMessage;
                    }
                }
                _SelectedMessageId = null;
            }
            catch (Exception ex)
            {
                ex.Error("Load message failed");

                MessagingCenter.Send(this, "LoadMessageFailed");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void InsertMessages(Message[] messages)
        {
            var minId = messages.Min(m => m.Id);
            var maxId = messages.Max(m => m.Id);

            if (!_Messages.Any() || minId > _Messages.Last().Id)
            {
                var mvms = messages.OrderBy(m => m.Id).Select(m => new MessageInfo(this, m)).ToList();
                for (var i = 0; i < mvms.Count; i++)
                {
                    mvms[i].SetIsMerged(i == 0 ? _Messages.LastOrDefault() : mvms[i - 1]);
                }

                _Messages.AddRange(mvms);
            }
            else if (maxId < _Messages.First().Id)
            {
                var mvms = messages.OrderBy(m => m.Id).Select(m => new MessageInfo(this, m)).ToList();
                MessageInfo prev = null;
                for (var i = 0; i < mvms.Count; i++)
                {
                    mvms[i].SetIsMerged(prev);
                    prev = mvms[i];
                }
                _Messages[0].SetIsMerged(prev);
                _Messages.InsertRange(0, mvms);
            }
            else
            {
                foreach (var dm in Messages
                                    .Where(e => e.Id == null
                                        && e.IdempotentKey != null
                                        && messages.Any(am => am.IdempotentKey == e.IdempotentKey))
                                    .ToList())
                {
                    Messages.Remove(dm);
                }

                var i = 0;

                foreach (var m in messages.OrderBy(e => e.Id))
                {
                    var vm = new MessageInfo(this, m);

                    for (; ; i++)
                    {
                        var prev = _Messages[i];

                        if (m.Id < prev.Id)
                        {
                            vm.SetIsMerged(Messages.ElementAtOrDefault(i - 1));
                            _Messages.Insert(i, vm);
                            prev.SetIsMerged(vm);
                            break;
                        }
                        else if (m.Id == prev.Id)
                        {
                            prev.Update(m);
                            break;
                        }
                        else if (i + 1 >= _Messages.Count)
                        {
                            vm.SetIsMerged(_Messages.LastOrDefault());
                            _Messages.Add(vm);
                            break;
                        }
                        else
                        {
                            var next = _Messages[i + 1];

                            if (m.Id < next.Id)
                            {
                                vm.SetIsMerged(prev);
                                _Messages.Insert(i + 1, vm);
                                next.SetIsMerged(vm);

                                break;
                            }
                        }
                    }
                }
            }
        }

        internal void UpdateMessage(Message message)
            => _Messages?.FirstOrDefault(m => m.Id == message.Id)?.Update(message);

        public Command PrependCommand { get; }
        public Command AppendCommand { get; }
        public Command RefreshCommand { get; }

        #region SelectedMessage

        private MessageInfo _SelectedMessage;

        public MessageInfo SelectedMessage
        {
            get => _SelectedMessage;
            set => SetProperty(ref _SelectedMessage, value, onChanged: () => _SelectedMessageId = null);
        }

        private ObservableRangeCollection<CommandViewModel> _Commands;

        public ObservableRangeCollection<CommandViewModel> Commands
            => _Commands ?? (_Commands = new ObservableRangeCollection<CommandViewModel>());

        private int? _SelectedMessageId;

        public int? SelectedMessageId
        {
            get => _SelectedMessageId ?? _SelectedMessage?.Id;
            set
            {
                if (value == SelectedMessageId)
                {
                    return;
                }

                if (value == null)
                {
                    _SelectedMessageId = null;
                    SelectedMessage = null;
                }
                else
                {
                    var m = _Messages.FirstOrDefault(e => e.Id == value);
                    if (m == null)
                    {
                        _SelectedMessageId = value;
                        BeginLoadMessages().GetHashCode();
                    }
                    else
                    {
                        _SelectedMessageId = null;
                        SelectedMessage = m;
                    }
                }
            }
        }

        #endregion SelectedMessage

        #region SelectedProfile

        private ProfileViewModel _SelectedProfile;

        public ProfileViewModel SelectedProfile
        {
            get => _SelectedProfile;
            set => SetProperty(ref _SelectedProfile, value, onChanged: () => OnPropertyChanged(nameof(HasProfile)));
        }

        public bool HasProfile => _SelectedProfile != null;

        internal async void SelectProfile(string screenName, string profileId)
        {
            Members.GetHashCode();
            await Channel.LoadMembersTask;

            SelectedProfile = Members.FirstOrDefault(p => p.Id == profileId)
                            ?? Members.FirstOrDefault(p => p.ScreenName.Equals(screenName, StringComparison.OrdinalIgnoreCase));

            // TODO: load from API
        }

        private Command _ClearPopupCommand;

        public Command ClearPopupCommand
            => _ClearPopupCommand ?? (_ClearPopupCommand = new Command(() =>
            {
                SelectedProfile = null;
                IsImageHistoryVisible = false;
                _Commands?.Clear();
                PopupUrl = null;
            }));

        #endregion SelectedProfile

        #endregion Showing Messages

        #region Post Message

        #region NewMessage

        private string _NewMessage = string.Empty;

        public string NewMessage
        {
            get => _NewMessage;
            set => SetProperty(ref _NewMessage, value, onChanged: UpdateCandicates);
        }

        #endregion NewMessage

        #region Candicates

        #region SelectionStart

        private int _SelectionStart;

        public int SelectionStart
        {
            get => _SelectionStart;
            set => SetProperty(ref _SelectionStart, value, onChanged: UpdateCandicates);
        }

        #endregion SelectionStart

        #region SelectionLength

        private int _SelectionLength;

        public int SelectionLength
        {
            get => _SelectionLength;
            set => SetProperty(ref _SelectionLength, value, onChanged: UpdateCandicates);
        }

        private void UpdateCandicates()
        {
            ProfileCandicates.UpdateResult();
            ChannelCandicates.UpdateResult();
        }

        #endregion SelectionLength

        #region NewMessageFocused

        private bool _NewMessageFocused;

        public bool NewMessageFocused
        {
            get => _NewMessageFocused;
            set => SetProperty(ref _NewMessageFocused, value);
        }

        #endregion NewMessageFocused

        private MessagesProfileCandicates _ProfileCandicates;

        public MessagesProfileCandicates ProfileCandicates
            => _ProfileCandicates ?? (_ProfileCandicates = new MessagesProfileCandicates(this));

        private MessagesChannelCandicates _ChannelCandicates;

        public MessagesChannelCandicates ChannelCandicates
            => _ChannelCandicates ?? (_ChannelCandicates = new MessagesChannelCandicates(this));

        internal DateTime? CandicateClicked { get; set; }

        #endregion Candicates

        #region Post Message

        public bool CanPost
            => !Channel.IsArchived;

        #region PostCommand

        private Command _PostCommand;

        public Command PostCommand
            => _PostCommand ?? (_PostCommand = new Command(BeginPost));

        public async void BeginPost()
        {
            var m = _NewMessage;

            var succeeded = false;

            if (IsBusy || string.IsNullOrEmpty(m))
            {
                return;
            }

            MessageInfo tempMessage = null;
            try
            {
                IsBusy = true;
                tempMessage = new MessageInfo(this, m);
                NewMessage = string.Empty;
                var prev = Messages.LastOrDefault();
                tempMessage.SetIsMerged(prev);
                Messages.Add(tempMessage);
                await Application.PostMessageAsync(Channel.Id, m, _IsNsfw, expandEmbedContents: _ExpandsContents, idempotentKey: tempMessage.IdempotentKey.Value);
                succeeded = true;
                ExpandsContents = true;
            }
            catch (Exception ex)
            {
                if (tempMessage != null)
                {
                    Messages.Remove(tempMessage);

                    if (string.IsNullOrEmpty(NewMessage))
                    {
                        NewMessage = m;
                    }
                }

                ex.Error("Post message failed");

                MessagingCenter.Send(this, "PostMessageFailed");
            }
            finally
            {
                IsBusy = false;
            }

            if (succeeded)
            {
                BeginAppend();
                // TODO: scroll to new message

                // TODO: reset nsfw
            }
        }

        #endregion PostCommand

        #region UploadImageCommand

        public bool SupportsImageUpload
            => Application.MediaPicker?.IsPhotosSupported == true;

        private Command _UploadImageCommand;

        public Command UploadImageCommand
            => _UploadImageCommand ?? (_UploadImageCommand = new Command(p => BeginUploadImage(p as Stream)));

        public void BeginUploadImage(Stream data = null)
        {
            Application.BeginUpload(new UploadParameter(AppendMessage, error =>
            {
                IsBusy = false;

                data?.Dispose();

                if (error != null)
                {
                    MessagingCenter.Send(this, "UploadImageFailed");
                }
            }, onUploading: () => IsBusy = true, data: data));
        }

        #endregion UploadImageCommand

        #region TakePhotoCommand

        public bool SupportsTakePhoto
            => Application.MediaPicker?.IsPhotosSupported == true
                && Application.MediaPicker?.IsCameraAvailable == true;

        private Command _TakePhotoCommand;

        public Command TakePhotoCommand
            => _TakePhotoCommand ?? (_TakePhotoCommand = new Command(BeginTakePhoto));

        public void BeginTakePhoto()
        {
            Application.BeginUpload(new UploadParameter(AppendMessage, error =>
            {
                IsBusy = false;

                if (error != null)
                {
                    MessagingCenter.Send(this, "TakePhotoFailed");
                }
            }, onUploading: () => IsBusy = true, useCamera: true));
        }

        #endregion TakePhotoCommand

        #region IsNsfw

        private bool _IsNsfw; // TODo: user default value

        public bool IsNsfw
        {
            get => _IsNsfw;
            set => SetProperty(ref _IsNsfw, value);
        }

        #endregion IsNsfw

        #region ToggleNsfwCommand

        private Command _ToggleNsfwCommand;

        public Command ToggleNsfwCommand
            => _ToggleNsfwCommand ?? (_ToggleNsfwCommand = new Command(() => IsNsfw = !_IsNsfw));

        #endregion ToggleNsfwCommand

        #region IsNsfw

        private bool _ExpandsContents = true;

        public bool ExpandsContents
        {
            get => _ExpandsContents;
            set => SetProperty(ref _ExpandsContents, value);
        }

        #endregion IsNsfw

        #region ToggleNsfwCommand

        private Command _ToggleExpandsContentsCommand;

        public Command ToggleExpandsContentsCommand
            => _ToggleExpandsContentsCommand ?? (_ToggleExpandsContentsCommand = new Command(() => ExpandsContents = !ExpandsContents));

        #endregion ToggleNsfwCommand

        #endregion Post Message

        internal void AppendMessage(string content)
        {
            IsBusy = false;

            if (string.IsNullOrWhiteSpace(_NewMessage))
            {
                NewMessage = content;
            }
            else
            {
                NewMessage += " " + content;
            }
        }

        #endregion Post Message

        #region ShowUnreadCommand

        private Command _ShowUnreadCommand;

        public Command ShowUnreadCommand
            => _ShowUnreadCommand ?? (_ShowUnreadCommand = new Command(ShowUnread));

        public async void ShowUnread()
        {
            var msg = _Messages?.OrderBy(m => m.Id).LastOrDefault();
            await BeginLoadMessages();
            SelectedMessage = msg == null ? _Messages.FirstOrDefault()
                            : (_Messages.SkipWhile(m => m != msg).Skip(1).FirstOrDefault() ?? msg);
        }

        #endregion ShowUnreadCommand

        #region ClearArchiveBannerCommand

        private bool _IsArchiveBannerShown;

        public bool IsArchiveBannerShown
        {
            get => _IsArchiveBannerShown;
            private set => SetProperty(ref _IsArchiveBannerShown, value);
        }

        private Command _ClearArchiveBannerCommand;

        public Command ClearArchiveBannerCommand
            => _ClearArchiveBannerCommand ?? (_ClearArchiveBannerCommand = new Command(ClearArchiveBanner));

        public void ClearArchiveBanner()
            => IsArchiveBannerShown = false;

        #endregion ClearArchiveBannerCommand

        private string _PopupUrl;

        public string PopupUrl
        {
            get => _PopupUrl;
            private set => SetProperty(ref _PopupUrl, value);
        }

        public bool OpenUrl(Uri u)
        {
            // TODO: support dedicated urls

            //if (u.Host == "www.youtube.com"
            //    && u.AbsolutePath == "/watch")
            //{
            //    if (u.Query.ParseQueryString().TryGetValue("v", out var v))
            //    {
            //        PopupUrl = $"https://www.youtube.com/embed/{v}";

            //        return true;
            //    }
            //}

            return false;
        }

        private Command _ShowMenuCommand;

        public Command ShowMenuCommand
            => _ShowMenuCommand ?? (_ShowMenuCommand = new Command(() =>
            {
                Commands.ReplaceRange(new[]
                {
                    new CommandViewModel("Refresh", RefreshCommand),
                    new CommandViewModel("Channel Detail", ShowChannelCommand)
                });
            }));

        private Command _ShowChannelCommand;
        public Command ShowChannelCommand
            => _ShowChannelCommand ?? (_ShowChannelCommand = new Command(() =>
            {
                ClearPopupCommand.Execute(null);
                Channel.ShowDetailCommand.Execute(null);
            }));

        #region ImageHistory

        private Command _ShowImageHistoryCommand;

        public Command ShowImageHistoryCommand
            => _ShowImageHistoryCommand ?? (_ShowImageHistoryCommand = new Command(() => IsImageHistoryVisible = true));

        private ImageHistoryPopupViewModel _ImageHistory;
        public ImageHistoryPopupViewModel ImageHistory => _ImageHistory;

        private bool _IsImageHistoryVisible;

        public bool IsImageHistoryVisible
        {
            get => _IsImageHistoryVisible;
            set => SetProperty(ref _IsImageHistoryVisible, value, onChanged: () =>
            {
                _ImageHistory = _IsImageHistoryVisible ? new ImageHistoryPopupViewModel(this) : null;
                OnPropertyChanged(nameof(ImageHistory));
            });
        }

        #endregion ImageHistory
    }
}