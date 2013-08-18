using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using BaconographyPortable.ViewModel;
using System.Windows.Media.Animation;
using System.Threading.Tasks;

namespace BaconographyWP8.View
{
    public partial class ExtendedLinkView : UserControl
    {
        public ExtendedLinkView()
        {
            InitializeComponent();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            var linkViewModel = DataContext as LinkViewModel;
            if (linkViewModel != null)
            {
                linkViewModel.PropertyChanged += linkViewModel_PropertyChanged;
                ExpandSB.Begin();
            }
        }

        async void linkViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "IsExtendedOptionsShown")
            {
                var linkViewModel = DataContext as LinkViewModel;
                if (linkViewModel != null)
                {
                    if (linkViewModel.IsExtendedOptionsShown)
                    {
                        Visibility = System.Windows.Visibility.Visible;
                        Height = 75;
                        ExpandSB.Begin();
                    }
                    else
                    {
                        CollapseSB.Begin();
                        await Task.Delay(150);
                        Height = 1;
                        await Task.Yield();
                        Visibility = System.Windows.Visibility.Collapsed;
                    }
                }
                
            }
        }
    }
}
