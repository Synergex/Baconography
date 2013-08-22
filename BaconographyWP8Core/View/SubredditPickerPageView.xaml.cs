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
using System.Windows.Data;
using BaconographyWP8.Common;
using BaconographyPortable.Model.Reddit;
using BaconographyWP8Core;
using BaconographyWP8Core.ViewModel;

namespace BaconographyWP8.View
{
    [ViewUri("/BaconographyWP8Core;component/View/SubredditPickerPageView.xaml")]
    public partial class SubredditPickerPageView : PhoneApplicationPage
    {
        SubredditPickerViewModel _spvm;

        public SubredditPickerPageView()
        {
            InitializeComponent();

            _spvm = DataContext as SubredditPickerViewModel;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            manualBox.Text = "";

            base.OnNavigatedTo(e);
        }

        private void UnpinFromSelected(object sender, System.Windows.Input.GestureEventArgs e)
        {
            var button = sender as Button;
            var context = button.DataContext as TypedSubreddit;

            if (_spvm != null)
            {
                var match = _spvm.SelectedSubreddits.FirstOrDefault<TypedSubreddit>(thing => thing.DisplayName == context.DisplayName);
                if (match != null)
                {
                    _spvm.SelectedSubreddits.Remove(match);
                }
            }
        }

        private void MarkUnmark(object sender, System.Windows.Input.GestureEventArgs e)
        {
            var button = sender as Button;
            var subredditVM = button.DataContext as AboutSubredditViewModel;
            if (subredditVM != null)
            {
                if (_spvm != null)
                {
                    var match = _spvm.SelectedSubreddits.FirstOrDefault<TypedSubreddit>(thing => thing.DisplayName == subredditVM.Thing.Data.DisplayName);
                    if (match != null)
                    {
                        subredditVM.Pinned = false;
                        _spvm.SelectedSubreddits.Remove(match);
                    }
                    else
                    {
                        subredditVM.Pinned = true;
                        _spvm.SelectedSubreddits.Add(new TypedSubreddit(subredditVM.Thing));
                    }
                }
            }
        }

        const int _offsetKnob = 7;
        private object newListLastItem;

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
                        var viewModel = DataContext as SubredditPickerViewModel;
                        if (viewModel != null && viewModel.Subreddits.HasMoreItems)
                            viewModel.Subreddits.LoadMoreItemsAsync(30);
                    }
                }
            }

            var subredditVM = newListLastItem as AboutSubredditViewModel;
            if (subredditVM != null)
            {
                if (_spvm != null)
                {
                    var match = _spvm.SelectedSubreddits.FirstOrDefault<TypedSubreddit>(thing => thing.DisplayName == subredditVM.Thing.Data.DisplayName);
                    if (match != null)
                    {
                        subredditVM.Pinned = true;
                    }
                    else
                    {
                        subredditVM.Pinned = false;
                    }
                }
            }
        }

        private void manualBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                this.Focus();
                var ssvm = this.DataContext as SubredditSelectorViewModel;
                if (ssvm != null)
                    ssvm.PinSubreddit.Execute(ssvm);
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
            //((TextBox)sender).Focus();
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
    }
}