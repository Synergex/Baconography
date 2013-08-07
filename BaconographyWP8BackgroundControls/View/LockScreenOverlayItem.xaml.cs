using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using BaconographyWP8.ViewModel;

namespace BaconographyWP8BackgroundControls.View
{
    public partial class LockScreenOverlayItem : UserControl
    {
        public LockScreenOverlayItem()
        {
            InitializeComponent();
        }

        public LockScreenOverlayItem(LockScreenMessage lockScreenMessage)
        {
            InitializeComponent();
            glyph.Text = lockScreenMessage.Glyph;
            displayText.Text = lockScreenMessage.DisplayText;
        }
    }
}
