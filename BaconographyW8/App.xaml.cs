using BaconographyPortable.Messages;
using BaconographyPortable.Services;
using BaconographyPortable.ViewModel;
using BaconographyW8.Common;
using BaconographyW8.PlatformServices;
using BaconographyW8.View;
using BaconographyW8BackgroundTask;
using GalaSoft.MvvmLight.Messaging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.Background;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.ApplicationSettings;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Application template is documented at http://go.microsoft.com/fwlink/?LinkId=234227

namespace BaconographyW8
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    sealed partial class App : Application
    {
        private BaconProvider _baconProvider;
        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {

            this.InitializeComponent();
            this.Suspending += OnSuspending;


            var taskRegistered = false;

            foreach (var task in Windows.ApplicationModel.Background.BackgroundTaskRegistration.AllTasks)
            {
                if (task.Value.Name == "BaconographyW8BackgroundTaskLiveTileUpdater")
                {
                    taskRegistered = true;
                    break;
                }
            }

            if (!taskRegistered)
            {
                var builder = new BackgroundTaskBuilder();

                builder.Name = "BaconographyW8BackgroundTaskLiveTileUpdater";
                builder.TaskEntryPoint = "BaconographyW8BackgroundTask.LiveTileUpdater";
                builder.SetTrigger(new TimeTrigger(15, false));
                builder.Register();
            }
        }

        protected override void OnWindowCreated(WindowCreatedEventArgs args)
        {
            base.OnWindowCreated(args);
            SettingsPane.GetForCurrentView().CommandsRequested += App_CommandsRequested;
        }

        /// <summary>
        /// Invoked when the application is launched normally by the end user.  Other entry points
        /// will be used when the application is launched to open a specific file, to display
        /// search results, and so forth.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected override async void OnLaunched(LaunchActivatedEventArgs args)
        {
            Frame rootFrame = Window.Current.Content as Frame;

            // Do not repeat app initialization when the Window already has content,
            // just ensure that the window is active
            if (rootFrame == null)
            {
                // Create a Frame to act as the navigation context and navigate to the first page
                rootFrame = new Frame();

                if (args.PreviousExecutionState == ApplicationExecutionState.Terminated)
                {
                    //TODO: Load state from previously suspended application
                }

                // Place the frame in the current Window
                Window.Current.Content = rootFrame;
            }

            if (_baconProvider == null)
            {
                _baconProvider = new BaconProvider();
                _baconProvider.AddService(typeof(IDynamicViewLocator), new DynamicViewLocator());

                await _baconProvider.Initialize(rootFrame);

                ViewModelLocator.Initialize(_baconProvider);
            }
            else
            {
                await _baconProvider.Initialize(rootFrame);
            }

            if (rootFrame.Content == null)
            {
                // When the navigation stack isn't restored navigate to the first page,
                // configuring the new page by passing required information as a navigation
                // parameter
                if (!rootFrame.Navigate(_baconProvider.GetService<IDynamicViewLocator>().RedditView, args.Arguments))
                {
                    throw new Exception("Failed to create initial page");
                }
            }
            // Ensure the current window is active
            Window.Current.Activate();

            LiveTileUpdater ltu = new LiveTileUpdater();
            ltu.RunBody(_baconProvider);
        }

        /// <summary>
        /// Invoked when application execution is being suspended.  Application state is saved
        /// without knowing whether the application will be terminated or resumed with the contents
        /// of memory still intact.
        /// </summary>
        /// <param name="sender">The source of the suspend request.</param>
        /// <param name="e">Details about the suspend request.</param>
        private async void OnSuspending(object sender, SuspendingEventArgs e)
        {
            var deferral = e.SuspendingOperation.GetDeferral();

            await _baconProvider.GetService<IOfflineService>().Suspend();

            deferral.Complete();
        }

        private void App_CommandsRequested(SettingsPane sender, SettingsPaneCommandsRequestedEventArgs args)
        {
            var commands = new List<SettingsCommand>
            {
                new SettingsCommand("UserLogin", "Login", TriggerLogin),
                new SettingsCommand("ContentPreferences", "Content Preferences", TriggerContentPreferences),
                new SettingsCommand("About", "About", TriggerAbout),
                new SettingsCommand("PrivacyPolicy", "Privacy Policy", TriggerPrivacyPolicy),
            };

            //make sure we dont insert duplicates, I've seen apps that have problems with this but dont really know how/why
            foreach (var command in commands)
            {
                if (args.Request.ApplicationCommands.FirstOrDefault(existing => command.Label == existing.Label || command.Id == existing.Id) == null)
                    args.Request.ApplicationCommands.Add(command);
            }

        }

        private bool _isTypeToSearch = false;

        private void TriggerAbout(Windows.UI.Popups.IUICommand command)
        {
            var flyout = new SettingsFlyout();
            flyout.Content = new AboutView();
            flyout.HeaderText = "About";
            flyout.IsOpen = true;
            flyout.Closed += (e, sender) =>
            {
                Messenger.Default.Unregister<CloseSettingsMessage>(this);
                SetSearchKeyboard(_isTypeToSearch);
            };
            Messenger.Default.Register<CloseSettingsMessage>(this, (message) =>
            {
                flyout.IsOpen = false;
                SetSearchKeyboard(_isTypeToSearch);
            });

            _isTypeToSearch = GetSearchKeyboard();
            App.SetSearchKeyboard(false);
        }

        private void TriggerContentPreferences(Windows.UI.Popups.IUICommand command)
        {
            var flyout = new SettingsFlyout();
            flyout.Content = new ContentPreferencesView();
            flyout.HeaderText = "Content Preferences";
            flyout.IsOpen = true;
            flyout.Closed += async (e, sender) =>
            {
                Messenger.Default.Unregister<CloseSettingsMessage>(this);
                SetSearchKeyboard(_isTypeToSearch);
                await _baconProvider.GetService<ISettingsService>().Persist();
            };
            Messenger.Default.Register<CloseSettingsMessage>(this, async (message) =>
            {
                flyout.IsOpen = false;
                SetSearchKeyboard(_isTypeToSearch);
                await _baconProvider.GetService<ISettingsService>().Persist();
            });

            _isTypeToSearch = GetSearchKeyboard();
            App.SetSearchKeyboard(false);
        }

        private void TriggerLogin(Windows.UI.Popups.IUICommand command)
        {
            var flyout = new SettingsFlyout();
            flyout.Content = new LoginView();
            flyout.HeaderText = "Login";
            flyout.IsOpen = true;
            flyout.Closed += (e, sender) =>
            {
                Messenger.Default.Unregister<CloseSettingsMessage>(this);
                SetSearchKeyboard(_isTypeToSearch);
            };
            Messenger.Default.Register<CloseSettingsMessage>(this, (message) =>
            {
                flyout.IsOpen = false;
                SetSearchKeyboard(_isTypeToSearch);
            });

            _isTypeToSearch = GetSearchKeyboard();
            App.SetSearchKeyboard(false);
        }

        private async void TriggerPrivacyPolicy(Windows.UI.Popups.IUICommand command)
        {
            await Windows.System.Launcher.LaunchUriAsync(new Uri("https://github.com/Synergex/Baconography/wiki/PrivacyPolicy"));
        }

        internal static void SetSearchKeyboard(bool value)
        {
            try
            {
                //this needs to be guarded as the search pane can disappear on us if we're getting dumped out of/suspended
                var sp = Windows.ApplicationModel.Search.SearchPane.GetForCurrentView();
                if (sp != null)
                    sp.ShowOnKeyboardInput = value;
            }
            catch
            {
                //do nothing we were most likely shutting down
            }
        }

        internal static bool GetSearchKeyboard()
        {
            try
            {
                //this needs to be guarded as the search pane can disappear on us if we're getting dumped out of/suspended
                var sp = Windows.ApplicationModel.Search.SearchPane.GetForCurrentView();
                if (sp != null)
                    return sp.ShowOnKeyboardInput;
            }
            catch
            {
                //do nothing we were most likely shutting down
            }
            return false;
        }

        protected async override void OnSearchActivated(Windows.ApplicationModel.Activation.SearchActivatedEventArgs args)
        {
            // TODO: Register the Windows.ApplicationModel.Search.SearchPane.GetForCurrentView().QuerySubmitted
            // event in OnWindowCreated to speed up searches once the application is already running

            // If the Window isn't already using Frame navigation, insert our own Frame
            var previousContent = Window.Current.Content;
            var frame = previousContent as Frame;

            // If the app does not contain a top-level frame, it is possible that this 
            // is the initial launch of the app. Typically this method and OnLaunched 
            // in App.xaml.cs can call a common method.
            if (frame == null)
            {
                // Create a Frame to act as the navigation context and associate it with
                // a SuspensionManager key
                frame = new Frame();
                SuspensionManager.RegisterFrame(frame, "AppFrame");

                if (args.PreviousExecutionState == ApplicationExecutionState.Terminated)
                {
                    // Restore the saved session state only when appropriate
                    try
                    {
                        await SuspensionManager.RestoreAsync();
                    }
                    catch (SuspensionManagerException)
                    {
                        //Something went wrong restoring state.
                        //Assume there is no state and continue
                    }
                }
            }

            frame.Navigate(typeof(SearchResultsView), args.QueryText);

            Window.Current.Content = frame;

            // Ensure the current window is active
            Window.Current.Activate();
        }

        /// <summary>
        /// Invoked when the application is activated to display a file open picker.
        /// </summary>
        /// <param name="args">Details about the activation request.</param>
        protected override async void OnFileOpenPickerActivated(Windows.ApplicationModel.Activation.FileOpenPickerActivatedEventArgs args)
        {

            if (_baconProvider == null)
            {
                _baconProvider = new BaconProvider();
                _baconProvider.AddService(typeof(IDynamicViewLocator), new DynamicViewLocator());

                await _baconProvider.Initialize(null);

                ViewModelLocator.Initialize(_baconProvider);
            }

            var fileOpenPickerPage = new FileOpenPickerView();
            fileOpenPickerPage.Activate(args);
        }
    }
}
