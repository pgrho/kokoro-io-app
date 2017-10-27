﻿using System;
using System.IO;
using KokoroIO.XamarinForms.ViewModels;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace KokoroIO.XamarinForms.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class MessagesPage : ContentPage
    {
        public MessagesPage()
        {
            InitializeComponent();

            MessagingCenter.Subscribe<MessagesViewModel>(this, "LoadMessageFailed", lvm =>
            {
                DisplayAlert("kokoro.io", "Failed to load messages", "OK");
            });
            MessagingCenter.Subscribe<MessagesViewModel>(this, "PostMessageFailed", lvm =>
            {
                DisplayAlert("kokoro.io", "Failed to post a message", "OK");
            });

            MessagingCenter.Subscribe<MessagesViewModel>(this, "UploadImageFailed", lvm =>
            {
                DisplayAlert("kokoro.io", "Failed to upload an image", "OK");
            });
            MessagingCenter.Subscribe<MessagesViewModel>(this, "TakePhotoFailed", lvm =>
            {
                DisplayAlert("kokoro.io", "Failed to take a photo", "OK");
            });
        }

        private void ExpandableEditor_FilePasted(object sender, EventArgs<Stream> e)
        {
            var vm = BindingContext as MessagesViewModel;

            if (vm == null)
            {
                e.Data.Dispose();
                return;
            }

            vm.BeginUploadImage(e.Data);
        }

        private void ExpandableEditor_Unfocused(object sender, FocusEventArgs e)
        {
            var vm = BindingContext as MessagesViewModel;

            if (vm?.CandicateClicked > DateTime.Now.AddSeconds(-0.5))
            {
                e.VisualElement.Focus();
            }
        }
    }
}