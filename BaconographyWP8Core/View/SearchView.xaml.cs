using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using BaconographyWP8.Common;
using BaconographyPortable.ViewModel;
using System.Windows.Data;

namespace BaconographyWP8Core.View
{
    [ViewUri("/BaconographyWP8Core;component/View/SearchView.xaml")]
    public partial class SearchView : PhoneApplicationPage
    {
        const int _offsetKnob = 7;
        private object newListLastItem;

        public SearchView()
        {
            InitializeComponent();
        }

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            if (e.NavigationMode == NavigationMode.New && e.Uri.ToString() == "/BaconographyWP8Core;component/MainPage.xaml" && e.IsCancelable)
                e.Cancel = true;
            else
            {
                if (e.NavigationMode == NavigationMode.Back && DataContext is CombinedSearchViewModel)
                {
                    ((CombinedSearchViewModel)DataContext).Query = "";
                }
                base.OnNavigatingFrom(e);
            }
        }

        private void manualBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                this.Focus();
            }
            else
            {
                BindingExpression bindingExpression = ((TextBox)sender).GetBindingExpression(TextBox.TextProperty);
                if (bindingExpression != null)
                {
                    bindingExpression.UpdateSource();
                }
            }
        }

        //this bit of unpleasantry is needed to prevent the input box from getting defocused when an item gets added to the collection
        bool _disableFocusHack = false;
        bool _needToHackFocus = false;
        TextBox _manualBox = null;
        private void manualBox_LostFocus(object sender, RoutedEventArgs e)
        {
            _manualBox = sender as TextBox;
            if (_disableFocusHack)
                _disableFocusHack = false;
            else
            {
                _needToHackFocus = true;
            }
        }

        private void manualBox_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            _disableFocusHack = true;
            _needToHackFocus = false;
        }

        private void FixedLongListSelector_GotFocus(object sender, RoutedEventArgs e)
        {
            if (!_disableFocusHack && _needToHackFocus)
            {
                _needToHackFocus = false;
                _manualBox.Focus();
            }
        }

        void newList_ItemRealized(object sender, ItemRealizationEventArgs e)
        {
            newListLastItem = e.Container.Content;
            var linksView = sender as FixedLongListSelector;
            if (linksView.ItemsSource != null && linksView.ItemsSource.Count >= _offsetKnob)
            {
                if (e.ItemKind == LongListSelectorItemKind.Item)
                {
                    if ((e.Container.Content).Equals(linksView.ItemsSource[linksView.ItemsSource.Count - _offsetKnob]))
                    {
                        var viewModel = DataContext as CombinedSearchViewModel;
                        if (viewModel != null && viewModel.SearchResults.HasMoreItems)
                            viewModel.SearchResults.LoadMoreItemsAsync(30);
                    }
                }
            }
        }
    }
}