﻿using KokoroIO.XamarinForms.Helpers;
using KokoroIO.XamarinForms.Models;
using Xamarin.Forms;

namespace KokoroIO.XamarinForms.ViewModels
{
    public sealed class UploaderInfo : ObservableObject
    {
        internal UploaderInfo(IImageUploader uploader, IUploaderInfoHost host = null)
        {
            Uploader = uploader;
            Host = host;
        }

        internal IUploaderInfoHost Host { get; }

        internal IImageUploader Uploader { get; }

        public string DisplayName => Uploader.DisplayName;

        #region LogoImageSource

        private ImageSource _LogoImageSource;

        public ImageSource LogoImageSource
        {
            get
            {
                if (_LogoImageSource == null)
                {
                    var resName = Uploader.LogoImage;

                    if (resName != null)
                    {
                        _LogoImageSource = ImageSource.FromStream(() => RH.GetManifestResourceStream(resName));
                    }
                }

                return _LogoImageSource;
            }
        }

        public bool HasLogoImage => Uploader.LogoImage != null;

        #endregion LogoImageSource

        private bool _IsSelected;

        public bool IsSelected
        {
            get => _IsSelected;
            set => SetProperty(ref _IsSelected, value, onChanged: () =>
            {
                if (Host != null)
                {
                    if (IsSelected)
                    {
                        Host.SelectedUploader = this;
                    }
                    else if (Host.SelectedUploader == this)
                    {
                        Host.SelectedUploader = null;
                    }
                }
            });
        }
    }
}