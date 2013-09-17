using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using System.Windows.Data;
using System.Windows.Input;

namespace BaconographyWP8Core.View
{
    public enum ExtendedAppMenuState
    {
        Extended,
        Collapsed
    }

    public partial class ExtendedAppBar : UserControl
    {
        public ExtendedAppBar()
        {
            InitializeComponent();
            MenuState = _staticMenuState;
        }




        public double Opacity
        {
            get { return (double)GetValue(OpacityProperty); }
            set { SetValue(OpacityProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Opacity.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty OpacityProperty =
            DependencyProperty.Register("Opacity", typeof(double), typeof(ExtendedAppBar), new PropertyMetadata(0.66));

        

        public string Text
        {
            get { return (string)GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Text.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register("Text", typeof(string), typeof(ExtendedAppBar), new PropertyMetadata(""));

        private static ExtendedAppMenuState _staticMenuState;
        public ExtendedAppMenuState MenuState
        {
            get { return (ExtendedAppMenuState)GetValue(MenuStateProperty); }
            set { SetValue(MenuStateProperty, value); }
        }

        // Using a DependencyProperty as the backing store for MenuState.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty MenuStateProperty =
            DependencyProperty.Register("MenuState", typeof(ExtendedAppMenuState), typeof(ExtendedAppBar), new PropertyMetadata(ExtendedAppMenuState.Extended, OnMenuStateChanged));

        public string LastButtonSymbol
        {
            get { return (string)GetValue(LastButtonSymbolProperty); }
            set { SetValue(LastButtonSymbolProperty, value); }
        }

        // Using a DependencyProperty as the backing store for LastButtonSymbol.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty LastButtonSymbolProperty =
            DependencyProperty.Register("LastButtonSymbol", typeof(string), typeof(ExtendedAppBar), new PropertyMetadata(""));


        public ICommand LastButtonCommand
        {
            get { return (ICommand)GetValue(LastButtonCommandProperty); }
            set { SetValue(LastButtonCommandProperty, value); }
        }

        // Using a DependencyProperty as the backing store for LastButtonCommand.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty LastButtonCommandProperty =
            DependencyProperty.Register("LastButtonCommand", typeof(ICommand), typeof(ExtendedAppBar), new PropertyMetadata(null));



        public string LastButtonText
        {
            get { return (string)GetValue(LastButtonTextProperty); }
            set { SetValue(LastButtonTextProperty, value); }
        }

        // Using a DependencyProperty as the backing store for LastButtonText.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty LastButtonTextProperty =
            DependencyProperty.Register("LastButtonText", typeof(string), typeof(ExtendedAppBar), new PropertyMetadata(""));

        

        private static void OnMenuStateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var thisp = d as ExtendedAppBar;
            var newState = (ExtendedAppMenuState)e.NewValue;
            switch (newState)
            {
                case ExtendedAppMenuState.Collapsed:
                    // Animate to Collapsed
                    thisp.caption.TextWrapping = System.Windows.TextWrapping.NoWrap;
                    thisp.caption.TextTrimming = System.Windows.TextTrimming.WordEllipsis;
                    thisp.trayButtons.Visibility = System.Windows.Visibility.Collapsed;
                    break;
                case ExtendedAppMenuState.Extended:
                    // Animate to Extended
                    thisp.caption.TextWrapping = System.Windows.TextWrapping.Wrap;
                    thisp.caption.TextTrimming = System.Windows.TextTrimming.None;
                    thisp.trayButtons.Visibility = System.Windows.Visibility.Visible;
                    break;
            }
            _staticMenuState = newState;
        }

        private void CaptionHitbox_ManipulationStarted(object sender, ManipulationStartedEventArgs e)
        {
            switch (MenuState)
            {
                case ExtendedAppMenuState.Extended:
                    // Animate to Collapsed
                    MenuState = ExtendedAppMenuState.Collapsed;
                    break;
                case ExtendedAppMenuState.Collapsed:
                    // Animate to Extended
                    MenuState = ExtendedAppMenuState.Extended;
                    break;
            }
        }

        
    }
}
