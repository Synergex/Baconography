using BaconographyPortable.ViewModel;
using BaconographyWP8.View;
using BaconographyWP8.ViewModel;
using GalaSoft.MvvmLight;
using Microsoft.Phone.Controls;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Windows.System.Threading;

namespace BaconographyWP8.Common
{
    public class RedditViewPivotControl : Pivot
    {

        RedditView MapViewModel(ViewModelBase viewModel)
        {
            return new RedditView();
        }

        int inflightLoadId = 0;
        protected override async void OnLoadingPivotItem(PivotItem item)
        {
            //since this is going to take a non trivial amount of time we need to prevent
            //any future loads from conflicting with what we're doing
            //by taking an always increasing id we can check aginst it prior to continuing
            //and implement a sort of cancel.

            //this has the added side effect of making super rapid transitions of the pivot nearly free
            //since no one pivot will be the current one for more then a few hundred milliseconds
            var loadIdAtStart = ++inflightLoadId;
            base.OnLoadingPivotItem(item);

            if (item.Content is RedditView)
            {
                ((RedditView)item.Content).LoadWithScroll();
                return;
            }

            var imageControl = item.Content as Image;

            if(imageControl != null)
                await Task.Delay(500);

            if (loadIdAtStart != inflightLoadId)
                return;
            
            var madeControl = MapViewModel(item.DataContext as ViewModelBase);

            if(imageControl != null)
                await Task.Delay(250);

            if (loadIdAtStart != inflightLoadId)
                return;

            madeControl.DataContext = item.DataContext as ViewModelBase;
            if (imageControl != null)
                await Task.Delay(100);

            if (loadIdAtStart != inflightLoadId)
                return;

            if(imageControl != null)
                imageControl.Source = null;

            item.Content = madeControl;
            madeControl.LoadWithScroll();
            
        }

        protected override void OnUnloadedPivotItem(PivotItemEventArgs e)
        {
            //if we didnt finish loading we dont need to make a new writable bitmap
            if (!(e.Item.Content is Image) && e.Item.Content is UIElement)
            {
                if (e.Item.Content is RedditView)
                {
                    ((RedditView)e.Item.Content).UnloadWithScroll();
                }
                WriteableBitmap bitmap = new WriteableBitmap(e.Item.Content as UIElement, null);
                e.Item.Content = new Image { Source = bitmap };
            }
            base.OnUnloadedPivotItem(e);
        }

    }
}
