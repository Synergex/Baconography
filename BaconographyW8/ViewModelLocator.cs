﻿using BaconographyPortable.Services;
using BaconographyPortable.ViewModel;
using BaconographyW8.Converters;
using BaconographyW8.PlatformServices;
using GalaSoft.MvvmLight.Ioc;
using Microsoft.Practices.ServiceLocation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel;

namespace BaconographyW8
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
                ServiceLocator.Current.GetInstance<RedditViewModel>();
                ServiceLocator.Current.GetInstance<LinkedWebViewModel>();
                ServiceLocator.Current.GetInstance<LoginViewModel>();
                ServiceLocator.Current.GetInstance<AboutUserViewModel>();
                ServiceLocator.Current.GetInstance<FileOpenPickerViewModel>();
                ServiceLocator.Current.GetInstance<SearchResultsViewModel>();
                ServiceLocator.Current.GetInstance<ContentPreferencesViewModel>();
                ServiceLocator.Current.GetInstance<RedditPickerViewModel>();
                ServiceLocator.Current.GetInstance<SearchQueryViewModel>();
                SimpleIoc.Default.Register<IDynamicViewLocator>(() => _baconProvider.GetService<IDynamicViewLocator>());
            }
        }

        static ViewModelLocator()
        {
            ServiceLocator.SetLocatorProvider(() => SimpleIoc.Default);

            SimpleIoc.Default.Register<IBaconProvider>(() => _baconProvider);

            SimpleIoc.Default.Register<RedditViewModel>();
            SimpleIoc.Default.Register<LoginViewModel>();
            SimpleIoc.Default.Register<LoadIndicatorViewModel>();
            SimpleIoc.Default.Register<LinkedWebViewModel>();
            SimpleIoc.Default.Register<SubredditsViewModel>();
            SimpleIoc.Default.Register<AboutUserViewModel>();
            SimpleIoc.Default.Register<FileOpenPickerViewModel>();
            SimpleIoc.Default.Register<SearchResultsViewModel>();
            SimpleIoc.Default.Register<ContentPreferencesViewModel>();
            SimpleIoc.Default.Register<RedditPickerViewModel>();
            SimpleIoc.Default.Register<SearchQueryViewModel>();
            SimpleIoc.Default.Register<VisitedLinkConverter>();
            SimpleIoc.Default.Register<VisitedMainLinkConverter>();
            SimpleIoc.Default.Register<PreviewDataConverter>();
            

            if (DesignMode.DesignModeEnabled)
            {
                var dynamicViewLocator = new DynamicViewLocator();
                var baconProvider = new BaconProvider(new Tuple<Type, Object>[] { new Tuple<Type, Object>(typeof(IDynamicViewLocator), new DynamicViewLocator()) });
                baconProvider.Initialize(null).Wait();
                Initialize(baconProvider);
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
                return ServiceLocator.Current.GetInstance<RedditViewModel>();
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

        public LoginViewModel Login
        {
            get
            {
                return ServiceLocator.Current.GetInstance<LoginViewModel>();
            }
        }

        public SubredditsViewModel Subreddits
        {
            get
            {
                return ServiceLocator.Current.GetInstance<SubredditsViewModel>();
            }
        }

        public AboutUserViewModel UserDetails
        {
            get
            {
                return ServiceLocator.Current.GetInstance<AboutUserViewModel>();
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
    }
}
