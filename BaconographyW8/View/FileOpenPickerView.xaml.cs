using BaconographyPortable.Messages;
using GalaSoft.MvvmLight.Messaging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Networking.BackgroundTransfer;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Basic Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234237

namespace BaconographyW8.View
{
    /// <summary>
    /// A basic page that provides characteristics common to most applications.
    /// </summary>
    public sealed partial class FileOpenPickerView : BaconographyW8.Common.LayoutAwarePage
    {
        private Windows.Storage.Pickers.Provider.FileOpenPickerUI _fileOpenPickerUI;
        public FileOpenPickerView()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// Populates the page with content passed during navigation.  Any saved state is also
        /// provided when recreating a page from a prior session.
        /// </summary>
        /// <param name="navigationParameter">The parameter value passed to
        /// <see cref="Frame.Navigate(Type, Object)"/> when this page was initially requested.
        /// </param>
        /// <param name="pageState">A dictionary of state preserved by this page during an earlier
        /// session.  This will be null the first time a page is visited.</param>
        protected override void LoadState(Object navigationParameter, Dictionary<String, Object> pageState)
        {
        }

        /// <summary>
        /// Preserves state associated with this page in case the application is suspended or the
        /// page is discarded from the navigation cache.  Values must conform to the serialization
        /// requirements of <see cref="SuspensionManager.SessionState"/>.
        /// </summary>
        /// <param name="pageState">An empty dictionary to be populated with serializable state.</param>
        protected override void SaveState(Dictionary<String, Object> pageState)
        {
        }

        /// <summary>
        /// Invoked when another application wants to open files from this application.
        /// </summary>
        /// <param name="args">Activation data used to coordinate the process with Windows.</param>
        public void Activate(FileOpenPickerActivatedEventArgs args)
        {
            this._fileOpenPickerUI = args.FileOpenPickerUI;
            _fileOpenPickerUI.Closing += _fileOpenPickerUI_Closing;

            Messenger.Default.Register<PickerFileMessage>(this, pickerSelectionChanged);

            Window.Current.Content = this;
            Window.Current.Activate();
        }

        private async void pickerSelectionChanged(PickerFileMessage obj)
        {
            if (obj.Selected)
            {
                Uri uri = new Uri(obj.TargetUrl);
                string filename = Path.GetFileName(uri.LocalPath);

                var file = await ApplicationData.Current.TemporaryFolder.CreateFileAsync(filename, CreationCollisionOption.ReplaceExisting);
                var downloader = new BackgroundDownloader();
                var download = downloader.CreateDownload(
                    uri,
                    file);

                var res = await download.StartAsync();

                _fileOpenPickerUI.AddFile(obj.TargetUrl, file);
            }
            else if (_fileOpenPickerUI.ContainsFile(obj.TargetUrl))
            {
                _fileOpenPickerUI.RemoveFile(obj.TargetUrl);
            }

        }

        void _fileOpenPickerUI_Closing(Windows.Storage.Pickers.Provider.FileOpenPickerUI sender, Windows.Storage.Pickers.Provider.PickerClosingEventArgs args)
        {
            Messenger.Default.Unregister<PickerFileMessage>(this, pickerSelectionChanged);
        }
    }
}
