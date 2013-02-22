using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace BaconographyWP8.Common
{
	public class FixedLongListSelector : Microsoft.Phone.Controls.LongListSelector
    {
		public FixedLongListSelector()
        {
			SelectionChanged += FixedLongListSelector_SelectionChanged;
        }

        void FixedLongListSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SelectedItem = base.SelectedItem;
        }

        public static readonly DependencyProperty SelectedItemProperty =
            DependencyProperty.Register(
                "SelectedItem",
                typeof(object),
				typeof(FixedLongListSelector),
                new PropertyMetadata(null, OnSelectedItemChanged)
            );

        private static void OnSelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
			var selector = (FixedLongListSelector)d;
            selector.SelectedItem = e.NewValue;
        }

        public new object SelectedItem
        {
            get { return GetValue(SelectedItemProperty); }
            set { SetValue(SelectedItemProperty, value); }
        }
    }
}
