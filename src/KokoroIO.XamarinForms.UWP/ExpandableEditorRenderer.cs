using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using KokoroIO.XamarinForms.UWP;
using KokoroIO.XamarinForms.Views;
using Shipwreck.KokoroIO;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.Storage.Streams;
using Xamarin.Forms;
using Xamarin.Forms.Platform.UWP;

[assembly: ExportRenderer(typeof(ExpandableEditor), typeof(ExpandableEditorRenderer))]

namespace KokoroIO.XamarinForms.UWP
{
    public sealed class ExpandableEditorRenderer : EditorRenderer
    {
        protected override void OnElementChanged(ElementChangedEventArgs<Editor> e)
        {
            base.OnElementChanged(e);

            if (Control != null)
            {
                Control.PlaceholderText = (e.NewElement as ExpandableEditor)?.Placeholder ?? string.Empty;

                Control.AllowDrop = true;
                Control.DragEnter += Control_DragOver;
                Control.DragOver += Control_DragOver;
                Control.Drop += Control_Drop;
            }
        }

        private void Control_DragOver(object sender, Windows.UI.Xaml.DragEventArgs e)
        {
            if (e.Handled)
            {
                return;
            }
            if ((e.AllowedOperations & DataPackageOperation.Copy) == DataPackageOperation.Copy)
            {
                e.AcceptedOperation = DataPackageOperation.Copy;
                e.Handled = true;
            }
        }

        private async void Control_Drop(object sender, Windows.UI.Xaml.DragEventArgs e)
        {
            try
            {
                var h = (Element as ExpandableEditor)?._FilePasted;

                if (h != null)
                {
                    IRandomAccessStreamWithContentType ras = null;
                    if (e.DataView.Contains(StandardDataFormats.StorageItems))
                    {
                        var sis = await e.DataView.GetStorageItemsAsync();
                        var si = sis.OfType<StorageFile>().FirstOrDefault();

                        if (si != null)
                        {
                            ras = await si.OpenReadAsync();
                            var s = ras.AsStreamForRead();
                        }
                    }
                    else if (e.DataView.Contains(StandardDataFormats.Bitmap))
                    {
                        var re = await e.DataView.GetBitmapAsync();
                        ras = await re.OpenReadAsync();
                    }

                    if (ras != null)
                    {
                        // TODO: add handled to dispose stream
                        h(Element, new EventArgs<Stream>(ras.AsStreamForRead()));
                    }
                }
            }
            catch { }
        }

        protected override void OnElementPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ExpandableEditor.Placeholder))
            {
                if (Control != null)
                {
                    Control.PlaceholderText = (Element as ExpandableEditor)?.Placeholder ?? string.Empty;
                }
            }
            base.OnElementPropertyChanged(sender, e);
        }
    }
}