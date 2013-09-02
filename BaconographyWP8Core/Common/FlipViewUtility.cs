using BaconographyPortable.Common;
using BaconographyPortable.Services;
using BaconographyPortable.ViewModel;
using BaconographyWP8.View;
using BaconographyWP8Core.View;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Ioc;
using Microsoft.Phone.Controls;
using Microsoft.Practices.ServiceLocation;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace BaconographyWP8Core.Common
{
    class FlipViewUtility
    {
        static bool _flicking;
        private static LinkViewModel GetLinkViewModel(ViewModelBase viewModel)
        {
            if (viewModel is LinkedPictureViewModel)
                return ((LinkedPictureViewModel)viewModel).ParentLink;
            else if (viewModel is ReadableArticleViewModel)
                return ((ReadableArticleViewModel)viewModel).ParentLink;
            else if (viewModel is CommentsViewModel)
                return ((CommentsViewModel)viewModel).Link;
            else
                throw new ArgumentException();
        }

        private static Tuple<string, IEnumerable<Tuple<string, string>>, string> MakeSerializable(LinkedPictureViewModel vm)
        {
            return Tuple.Create(vm.LinkTitle, vm.Pictures.Select(linkedPicture => Tuple.Create(linkedPicture.Title, linkedPicture.Url)), vm.LinkId);
        }

        private static object MakeSerializable(ReadableArticleViewModel vm)
        {
            if (SimpleIoc.Default.IsRegistered<ReadableArticleViewModel>())
            {
                SimpleIoc.Default.Unregister<ReadableArticleViewModel>();
            }

            SimpleIoc.Default.Register<ReadableArticleViewModel>(() => vm);
            return null;
        }

        public static async void FlickHandler(object sender, FlickGestureEventArgs e, ViewModelBase currentViewModel, UIElement rootPage)
        {
            try
            {
                if (_flicking || currentViewModel == null)
                    return;

                if (e.Direction == System.Windows.Controls.Orientation.Vertical)
                {
                    //Up
                    if (e.VerticalVelocity < -1500)
                    {
                        _flicking = true;
                        try
                        {
                            using (ServiceLocator.Current.GetInstance<ISuspendableWorkQueue>().HighValueOperationToken)
                            {
                                var next = await StreamViewUtility.Next(GetLinkViewModel(currentViewModel), currentViewModel);
                                if (next != null)
                                {
                                    TransitionService.SetNavigationOutTransition(rootPage,
                                        new NavigationOutTransition()
                                        {
                                            Forward = new SlideTransition()
                                            {
                                                Mode = SlideTransitionMode.SlideUpFadeOut
                                            }
                                        }
                                    );
                                    if (next is LinkedPictureViewModel)
                                    {
                                        ServiceLocator.Current.GetInstance<INavigationService>().Navigate(typeof(LinkedPictureView), MakeSerializable(next as LinkedPictureViewModel));
                                    }
                                    else if (next is ReadableArticleViewModel)
                                    {
                                        ServiceLocator.Current.GetInstance<INavigationService>().Navigate(typeof(LinkedReadabilityView), MakeSerializable(next as ReadableArticleViewModel));
                                    }
                                }
                            }
                        }
                        finally
                        {
                            _flicking = false;
                        }


                    }
                    else if (e.VerticalVelocity > 1500) //Down
                    {
                        _flicking = true;
                        try
                        {
                            using (ServiceLocator.Current.GetInstance<ISuspendableWorkQueue>().HighValueOperationToken)
                            {
                                var previous = await StreamViewUtility.Previous(GetLinkViewModel(currentViewModel), currentViewModel);
                                if (previous is LinkedPictureViewModel)
                                {
                                    ServiceLocator.Current.GetInstance<INavigationService>().Navigate(typeof(LinkedPictureView), MakeSerializable(previous as LinkedPictureViewModel));
                                }
                                else if (previous is ReadableArticleViewModel)
                                {
                                    ServiceLocator.Current.GetInstance<INavigationService>().Navigate(typeof(LinkedReadabilityView), MakeSerializable(previous as ReadableArticleViewModel));
                                }
                            }
                        }
                        finally
                        {
                            _flicking = false;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }
        }
    }
}
