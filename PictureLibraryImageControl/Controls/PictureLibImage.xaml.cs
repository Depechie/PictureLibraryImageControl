using System;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using PictureLibraryImageControl.Framework;

namespace PictureLibraryImageControl.Controls
{
    public sealed partial class PictureLibImage : UserControl
    {
        public static readonly AsyncLock _lock = new AsyncLock();

        public static readonly DependencyProperty PlaceholderProperty =
            DependencyProperty.Register("Placeholder", typeof(ImageSource), typeof(PictureLibImage),
            new PropertyMetadata(default(ImageSource)));

        public static readonly DependencyProperty SourceProperty =
            DependencyProperty.Register("Source", typeof(string), typeof(PictureLibImage),
            new PropertyMetadata(default(ImageSource), SourceChanged));

        public static readonly DependencyProperty FolderNameProperty =
            DependencyProperty.Register("FolderName", typeof(string), typeof(PictureLibImage),
                new PropertyMetadata(default(ImageSource)));

        public ImageSource Placeholder
        {
            get { return (ImageSource)GetValue(PlaceholderProperty); }
            set { SetValue(PlaceholderProperty, value); }
        }

        public string Source
        {
            get { return (string)GetValue(SourceProperty); }
            set { SetValue(SourceProperty, value); }
        }

        public string FolderName
        {
            get { return (string)GetValue(FolderNameProperty); }
            set { SetValue(FolderNameProperty, value); }
        }

        public PictureLibImage()
        {
            this.InitializeComponent();
        }

        private static async void SourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (PictureLibImage)d;
            var file = await StorageFile.GetFileFromPathAsync((string)e.NewValue);
            if (file != null)
            {
                using (var stream = await file.OpenAsync(FileAccessMode.Read))
                {
                    var bitmapImage = new BitmapImage();
                    await bitmapImage.SetSourceAsync(stream);

                    bitmapImage.ImageOpened += (sender, args) => control.LoadImage(bitmapImage);
                }
            }
        }

        private void LoadImage(ImageSource source)
        {
            ImageFadeOut.Completed += (s, e) =>
            {
                Image.Source = source;
                ImageFadeIn.Begin();
            };
            ImageFadeOut.Begin();
        }

        private async void OnImageTapped(object sender, TappedRoutedEventArgs e)
        {
            FileOpenPicker openPicker = new FileOpenPicker();
            openPicker.ViewMode = PickerViewMode.Thumbnail;
            openPicker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
            openPicker.FileTypeFilter.Add(".jpg");
            openPicker.FileTypeFilter.Add(".jpeg");
            openPicker.FileTypeFilter.Add(".png");
            StorageFile file = await openPicker.PickSingleFileAsync();
            if (file != null)
            {
                // Application now has read/write access to the picked file 
                using (await _lock.LockAsync())
                {
                    byte[] buffer = null;
                    using (var stream = await file.OpenAsync(Windows.Storage.FileAccessMode.Read))
                    {
                        var reader = new DataReader(stream.GetInputStreamAt(0));
                        buffer = new byte[stream.Size];
                        await reader.LoadAsync((uint)stream.Size);
                        reader.ReadBytes(buffer);
                    }

                    StorageFolder PosPicFolder = await KnownFolders.PicturesLibrary.CreateFolderAsync(this.FolderName, CreationCollisionOption.OpenIfExists);
                    StorageFile selectedPicture = await PosPicFolder.CreateFileAsync(file.Name, CreationCollisionOption.OpenIfExists);

                    await FileIO.WriteBytesAsync(selectedPicture, buffer);
                    this.Source = selectedPicture.Path;
                }
            }
        }
    }
}
