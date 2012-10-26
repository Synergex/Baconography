using GalaSoft.MvvmLight.Messaging;
using Baconography.Messages;
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

// The File Open Picker Contract item template is documented at http://go.microsoft.com/fwlink/?LinkId=234239

namespace Baconography.View
{
    /// <summary>
    /// This page displays files owned by the application so that the user can grant another application
    /// access to them.
    /// </summary>
    public sealed partial class FileOpenPickerView : Baconography.Common.LayoutAwarePage
    {
        /// <summary>
        /// Files are added to or removed from the Windows UI to let Windows know what has been selected.
        /// </summary>
        private Windows.Storage.Pickers.Provider.FileOpenPickerUI _fileOpenPickerUI;

        public FileOpenPickerView()
        {
            this.InitializeComponent();
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
            else if(_fileOpenPickerUI.ContainsFile(obj.TargetUrl))
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
