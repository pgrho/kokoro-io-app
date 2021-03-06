using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using KokoroIO.XamarinForms.UWP.Renderers;
using KokoroIO.XamarinForms.Views;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Xamarin.Forms;
using Xamarin.Forms.Platform.UWP;

[assembly: ExportRenderer(typeof(ExpandableEditor), typeof(ExpandableEditorRenderer))]

namespace KokoroIO.XamarinForms.UWP.Renderers
{
    public sealed class ExpandableEditorRenderer : EditorRenderer
    {
        protected override void OnElementChanged(ElementChangedEventArgs<Editor> e)
        {
            base.OnElementChanged(e);

            if (Control != null)
            {
                Control.AllowDrop = true;
                Control.AddHandler(FormsTextBox.KeyDownEvent, (KeyEventHandler)Control_KeyDown, true);
                Control.TextChanged += Control_TextChanged;
                Control.SelectionChanged += Control_SelectionChanged;
                Control.DragEnter += Control_DragOver;
                Control.DragOver += Control_DragOver;
                Control.Drop += Control_Drop;

                Control.Paste += Control_Paste;
            }
        }

        private TextBlock _TextForMeasure;
        private int _PreviousLineCount;
        private double _LineHeight;

        private void Control_TextChanged(object sender, Windows.UI.Xaml.Controls.TextChangedEventArgs e)
        {
            _TextForMeasure = _TextForMeasure ?? new TextBlock()
            {
                TextWrapping = TextWrapping.Wrap
            };
            _TextForMeasure.FontFamily = Control.FontFamily;
            _TextForMeasure.FontSize = Control.FontSize;
            _TextForMeasure.FontStretch = Control.FontStretch;
            _TextForMeasure.FontStyle = Control.FontStyle;
            _TextForMeasure.FontWeight = Control.FontWeight;
            _TextForMeasure.Width = Math.Ceiling(Control.ActualWidth);
            _TextForMeasure.Padding = Control.Padding;
            if (_LineHeight == 0)
            {
                _TextForMeasure.Text = "M";
                _TextForMeasure.Measure(new Windows.Foundation.Size(Control.ActualWidth, Window.Current.Bounds.Height));
                _LineHeight = Math.Ceiling(_TextForMeasure.ActualHeight * 1.1);
            }
            _TextForMeasure.Text = Control.Text;
            _TextForMeasure.Measure(new Windows.Foundation.Size(Control.ActualWidth, Window.Current.Bounds.Height));

            var ee = Element as ExpandableEditor;
            var lc = Math.Min((int)Math.Round(_TextForMeasure.ActualHeight / _LineHeight), ee?.MaxLines > 0 ? ee.MaxLines : int.MaxValue);
            if (lc != _PreviousLineCount)
            {
                _PreviousLineCount = lc;
                Control.Height = _LineHeight * lc;
                ee?.InvalidateMeasure();
            }
        }

        private void Control_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Enter
                && Xamarin.Forms.Device.Idiom == TargetIdiom.Desktop)
            {
                var shift = CoreWindow.GetForCurrentThread().GetKeyState(VirtualKey.Shift);
                if ((shift & CoreVirtualKeyStates.Down) != CoreVirtualKeyStates.Down)
                {
                    var cmd = (Element as ExpandableEditor)?.PostCommand;

                    if (cmd?.CanExecute(null) ?? false)
                    {
                        cmd.Execute(null);
                        e.Handled = true;
                    }
                }
            }
        }

        private void Control_SelectionChanged(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            var ee = Element as ExpandableEditor;
            if (ee != null)
            {
                ee.SelectionStart = Control.SelectionStart;
                ee.SelectionLength = Control.SelectionLength;
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
                        h(Element, new KokoroIO.EventArgs<Stream>(ras.AsStreamForRead()));
                    }
                }
            }
            catch { }
        }

        private void Control_Paste(object sender, Windows.UI.Xaml.Controls.TextControlPasteEventArgs e)
        {
            if (!e.Handled)
            {
                var dataPackageView = Clipboard.GetContent();

                if (dataPackageView != null)
                {
                    e.Handled = CanHandleData(dataPackageView);
                    OnDataPasted(dataPackageView);
                }
            }
        }

        private static bool CanHandleData(DataPackageView dataPackageView)
            => dataPackageView.Contains(StandardDataFormats.StorageItems)
            || dataPackageView.Contains(StandardDataFormats.Bitmap);

        private async void OnDataPasted(DataPackageView dataPackageView)
        {
            try
            {
                var h = (Element as ExpandableEditor)?._FilePasted;

                if (h != null)
                {
                    IRandomAccessStreamWithContentType ras = null;
                    if (dataPackageView.Contains(StandardDataFormats.StorageItems))
                    {
                        var sis = await dataPackageView.GetStorageItemsAsync();
                        var si = sis.OfType<StorageFile>().FirstOrDefault();

                        if (si != null)
                        {
                            ras = await si.OpenReadAsync();
                            var s = ras.AsStreamForRead();
                        }
                    }
                    else if (dataPackageView.Contains(StandardDataFormats.Bitmap))
                    {
                        var re = await dataPackageView.GetBitmapAsync();
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
            if (Control != null)
            {
                switch (e.PropertyName)
                {
                    case nameof(ExpandableEditor.SelectionStart):
                    case nameof(ExpandableEditor.SelectionLength):
                        if (Element is ExpandableEditor ee)
                        {
                            if (0 <= ee.SelectionStart
                                && ee.SelectionLength >= 0
                                && ee.SelectionStart + ee.SelectionLength < Control.Text.Length)
                            {
                                if (!(Control.PointerCaptures?.Count > 0)
                                    && (CoreWindow.GetForCurrentThread().GetKeyState(VirtualKey.Shift) & CoreVirtualKeyStates.Down) == CoreVirtualKeyStates.Down)
                                {
                                    Control.Select(ee.SelectionStart, ee.SelectionLength);
                                }
                            }
                        }
                        break;
                }
            }

            base.OnElementPropertyChanged(sender, e);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (Control != null)
                {
                    Control.RemoveHandler(FormsTextBox.KeyDownEvent, (KeyEventHandler)Control_KeyDown);
                    Control.TextChanged -= Control_TextChanged;
                    Control.SelectionChanged -= Control_SelectionChanged;
                    Control.DragEnter -= Control_DragOver;
                    Control.DragOver -= Control_DragOver;
                    Control.Drop -= Control_Drop;

                    Control.Paste -= Control_Paste;
                }
            }
            base.Dispose(disposing);
        }
    }
}