using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using System.Windows.Shapes;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Controls;

namespace BaconographyWP8.View
{
	public partial class PivotCountIndicator : UserControl
	{

		/// <summary>
		/// Public ItemsCount property of type DependencyProperty
		/// </summary>
		public static readonly DependencyProperty ItemsCountProperty =
			DependencyProperty.Register("ItemsCount",
			typeof(int),
			typeof(PivotCountIndicator),
			new PropertyMetadata(OnItemsCountChanged));

		/// <summary>
		/// Public SelectedPivotIndex property of type DependencyProperty
		/// </summary>
		public static readonly DependencyProperty SelectedPivotIndexProperty =
			DependencyProperty.Register("SelectedPivotIndex",
			typeof(int),
			typeof(PivotCountIndicator),
			new PropertyMetadata(OnPivotIndexChanged));

		/// <summary>
		/// Constructor
		/// </summary>
		public PivotCountIndicator()
		{
			InitializeComponent();
		}

		/// <summary>
		/// Gets or sets number of pivot items
		/// </summary>
		public int ItemsCount
		{
			set { SetValue(ItemsCountProperty, value); }
			get { return (int)GetValue(ItemsCountProperty); }
		}

		/// <summary>
		/// Gets or sets index of selected pivot item
		/// </summary>
		public int SelectedPivotIndex
		{
			set { SetValue(SelectedPivotIndexProperty, value); }
			get { return (int)GetValue(SelectedPivotIndexProperty); }
		}

		/// <summary>
		/// OnItemsCountChanged property-changed handler
		/// </summary>
		private static void OnItemsCountChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
		{
			(obj as PivotCountIndicator).SetCircles();
		}

		/// <summary>
		/// OnPivotIndexChanged property-changed handler
		/// </summary>
		private static void OnPivotIndexChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
		{
			(obj as PivotCountIndicator).AccentCircle();
		}

		/// <summary>
		/// Draws circles.
		/// </summary>
		private void SetCircles()
		{
			ellipsesPanel.Children.Clear();
			for (int i = 0; i < this.ItemsCount; i++)
			{
				Ellipse ellipse = new Ellipse() { Height = 10, Width = 10, Margin = new Thickness(2,0,0,0) };
				ellipsesPanel.Children.Add(ellipse);
			}
			this.AccentCircle();
		}

		/// <summary>
		/// Accents selected pivot item circle.
		/// </summary>
		private void AccentCircle()
		{
			int i = 0;
			foreach (var item in ellipsesPanel.Children)
			{
				if (item is Ellipse)
				{
					Ellipse ellipse = (Ellipse)item;
					if (i == this.SelectedPivotIndex)
						ellipse.Fill = (SolidColorBrush)Application.Current.Resources["PhoneForegroundBrush"];
					else
						ellipse.Fill = (SolidColorBrush)Application.Current.Resources["PhoneDisabledBrush"];
					i++;
				}
			}
		}
	}
}
