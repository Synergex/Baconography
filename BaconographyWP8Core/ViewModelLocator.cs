﻿using BaconographyPortable.Services;
using BaconographyPortable.ViewModel;
using BaconographyWP8.Converters;
using BaconographyWP8.PlatformServices;
using BaconographyWP8.ViewModel;
using BaconographyWP8Core.ViewModel;
using GalaSoft.MvvmLight.Ioc;
using Microsoft.Practices.ServiceLocation;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel;

namespace BaconographyWP8
{
    public class ViewModelLocator
    {
        private static IBaconProvider _baconProvider;
        public static void Initialize(IBaconProvider baconProvider)
        {
            if (_baconProvider == null)
            {
                _baconProvider = baconProvider;

                //ensure we exist
				ServiceLocator.Current.GetInstance<MainPageViewModel>();
                ServiceLocator.Current.GetInstance<RedditViewModel>();
                ServiceLocator.Current.GetInstance<LinkedWebViewModel>();
				ServiceLocator.Current.GetInstance<LoginPageViewModel>();
                ServiceLocator.Current.GetInstance<FileOpenPickerViewModel>();
                ServiceLocator.Current.GetInstance<SearchResultsViewModel>();
                ServiceLocator.Current.GetInstance<ContentPreferencesViewModel>();
                ServiceLocator.Current.GetInstance<RedditPickerViewModel>();
                ServiceLocator.Current.GetInstance<SearchQueryViewModel>();
                ServiceLocator.Current.GetInstance<MessagesViewModel>();
                ServiceLocator.Current.GetInstance<CaptchaViewModel>();
                ServiceLocator.Current.GetInstance<LockScreenViewModel>();
                ServiceLocator.Current.GetInstance<SubredditPickerViewModel>();
                SimpleIoc.Default.Register<IDynamicViewLocator>(() => _baconProvider.GetService<IDynamicViewLocator>());
            }
        }

        static ViewModelLocator()
        {
			try
			{
				ServiceLocator.SetLocatorProvider(() => SimpleIoc.Default);

				SimpleIoc.Default.Register<IBaconProvider>(() => _baconProvider);

				SimpleIoc.Default.Register<RedditViewModel>();
				SimpleIoc.Default.Register<LoginPageViewModel>();
				SimpleIoc.Default.Register<LoadIndicatorViewModel>();
				SimpleIoc.Default.Register<LinkedWebViewModel>();
				SimpleIoc.Default.Register<SubredditsViewModel>();
				SimpleIoc.Default.Register<SubredditSelectorViewModel>();
				SimpleIoc.Default.Register<AboutUserViewModel>();
				SimpleIoc.Default.Register<FileOpenPickerViewModel>();
				SimpleIoc.Default.Register<SearchResultsViewModel>();
				SimpleIoc.Default.Register<ContentPreferencesViewModel>();
				SimpleIoc.Default.Register<RedditPickerViewModel>();
				SimpleIoc.Default.Register<MainPageViewModel>();
				SimpleIoc.Default.Register<SearchQueryViewModel>();
				SimpleIoc.Default.Register<VisitedLinkConverter>();
				SimpleIoc.Default.Register<VisitedMainLinkConverter>();
				SimpleIoc.Default.Register<PreviewDataConverter>();
				SimpleIoc.Default.Register<ReplyViewModel>();
                SimpleIoc.Default.Register<MessagesViewModel>();
                SimpleIoc.Default.Register<CaptchaViewModel>();
                SimpleIoc.Default.Register<LockScreenViewModel>();
                SimpleIoc.Default.Register<ComposePostViewModel>();
                SimpleIoc.Default.Register<SubredditPickerViewModel>();
                SimpleIoc.Default.Register<CombinedSearchViewModel>();

				if (DesignerProperties.IsInDesignTool)
				{
                    var baconProvider = new BaconProvider(new Tuple<Type, Object>[] { new Tuple<Type, Object>(typeof(IDynamicViewLocator), new DynamicViewLocator()) });
					baconProvider.Initialize(null).Wait();
					Initialize(baconProvider);
				}
			}
			catch
			{
				System.Diagnostics.Debug.WriteLine("uhh, something happend, ignore it");
			}
        }

        public PreviewDataConverter PreviewData
        {
            get
            {
                return ServiceLocator.Current.GetInstance<PreviewDataConverter>();
            }
        }

        public VisitedLinkConverter VisitedLink
        {
            get
            {
                return ServiceLocator.Current.GetInstance<VisitedLinkConverter>();
            }
        }

        public VisitedMainLinkConverter VisitedMainLink
        {
            get
            {
                return ServiceLocator.Current.GetInstance<VisitedMainLinkConverter>();
            }
        }

        public RedditViewModel Reddit
        {
            get
            {
                return new RedditViewModel(_baconProvider);
            }
        }

		public MainPageViewModel MainPage
		{
			get
			{
				return ServiceLocator.Current.GetInstance<MainPageViewModel>();
			}
		}

        public CommentsViewModel Comments
        {
            get
            {
                return new CommentsViewModel(_baconProvider);
            }
        }

        public LoadIndicatorViewModel LoadIndicator
        {
            get
            {
                return ServiceLocator.Current.GetInstance<LoadIndicatorViewModel>();
            }
        }

        public LinkedWebViewModel LinkedWeb
        {
            get
            {
                return ServiceLocator.Current.GetInstance<LinkedWebViewModel>();
            }
        }

		public LoginPageViewModel Login
        {
            get
            {
                return ServiceLocator.Current.GetInstance<LoginPageViewModel>();
            }
        }

        public SubredditsViewModel Subreddits
        {
            get
            {
                return ServiceLocator.Current.GetInstance<SubredditsViewModel>();
            }
        }

		public SubredditSelectorViewModel SubredditSelector
		{
			get
			{
				return ServiceLocator.Current.GetInstance<SubredditSelectorViewModel>();
			}
		}

        public AboutUserViewModel UserDetails
        {
            get
            {
                return new AboutUserViewModel(_baconProvider);
            }
        }

        public FileOpenPickerViewModel FileOpenPicker
        {
            get
            {
                return ServiceLocator.Current.GetInstance<FileOpenPickerViewModel>();
            }
        }

        public SearchResultsViewModel SearchResults
        {
            get
            {
                return ServiceLocator.Current.GetInstance<SearchResultsViewModel>();
            }
        }

        public SearchQueryViewModel SearchQuery
        {
            get
            {
                return ServiceLocator.Current.GetInstance<SearchQueryViewModel>();
            }
        }

        public CombinedSearchViewModel Search
        {
            get
            {
                return ServiceLocator.Current.GetInstance<CombinedSearchViewModel>();
            }
        }

        public ContentPreferencesViewModel ContentPreferences
        {
            get
            {
                return ServiceLocator.Current.GetInstance<ContentPreferencesViewModel>();
            }
        }

        public RedditPickerViewModel RedditPicker
        {
            get
            {
                return ServiceLocator.Current.GetInstance<RedditPickerViewModel>();
            }
        }

		public ReplyViewModel Reply
		{
			get
			{
				return ServiceLocator.Current.GetInstance<ReplyViewModel>();
			}
		}

        public MessagesViewModel Messages
        {
            get
            {
                return ServiceLocator.Current.GetInstance<MessagesViewModel>();
            }
        }

        public CaptchaViewModel Captcha
        {
            get
            {
                return CaptchaViewModel.GetInstance(_baconProvider);
            }
        }

        public LockScreenViewModel LockScreen
        {
            get
            {
                return ServiceLocator.Current.GetInstance<LockScreenViewModel>();
            }
        }

        public PreviewLockScreenViewModel PreviewLockScreen
        {
            get
            {
                return new PreviewLockScreenViewModel();
            }
        }

        public SubredditPickerViewModel SubredditPicker
        {
            get
            {
                return ServiceLocator.Current.GetInstance<SubredditPickerViewModel>();
            }
        }

        public ComposePostViewModel Submit
        {
            get
            {
                return ServiceLocator.Current.GetInstance<ComposePostViewModel>();
            }
        }
    }
}
