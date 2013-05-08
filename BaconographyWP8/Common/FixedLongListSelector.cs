using BaconographyPortable.Messages;
using BaconographyPortable.ViewModel;
using GalaSoft.MvvmLight.Messaging;
using Microsoft.Phone.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace BaconographyWP8.Common
{
	public class FixedLongListSelector : Microsoft.Phone.Controls.LongListSelector
	{
		bool viewportChanged = false;
		bool isMoving = false;
		double manipulationStart = 0;
		double manipulationEnd = 0;

		public FixedLongListSelector()
		{
			SelectionChanged += FixedLongListSelector_SelectionChanged;
			ManipulationStateChanged += listbox_ManipulationStateChanged;
			MouseMove += listbox_MouseMove;
			Tap += FixedLongListSelector_Tap;
			ItemRealized += OnViewportChanged;
			ItemUnrealized += OnViewportChanged;
			Compression += FixedLongListSelector_Compression;
		}

		void FixedLongListSelector_Compression(object sender, CompressionEventArgs e)
		{
			if (e.Type == CompressionType.Top)
				PulledDown = true;
			else
				PulledDown = false;
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

		public static readonly DependencyProperty PulledDownProperty =
		   DependencyProperty.Register(
			   "PulledDown",
			   typeof(bool),
			   typeof(FixedLongListSelector),
			   new PropertyMetadata(false, OnPulledDownChanged)
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

		private static void OnPulledDownChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			var selector = (FixedLongListSelector)d;
			selector.PulledDown = (bool)e.NewValue;
		}

		public bool PulledDown
		{
			get { return (bool)GetValue(PulledDownProperty); }
			set { SetValue(PulledDownProperty, value); }
		}

		void OnViewportChanged(object sender, Microsoft.Phone.Controls.ItemRealizationEventArgs e)
		{
			viewportChanged = true;
		}

		void FixedLongListSelector_Tap(object sender, System.Windows.Input.GestureEventArgs e)
		{
			var pos = e.GetPosition(null);

			if (!isMoving)
				manipulationStart = pos.Y;
			else
				manipulationEnd = pos.Y;

			isMoving = true;
		}

		void listbox_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
		{
			var pos = e.GetPosition(null);

			if (!isMoving)
			{
				manipulationStart = pos.Y;
			}
			else
			{
				manipulationEnd = pos.Y;
				if (ManipulationState == System.Windows.Controls.Primitives.ManipulationState.Manipulating)
				{
					DoInterimManipulation();
				}
			}

			isMoving = true;
		}

		const int pullDownOffset = -115;
		void DoInterimManipulation()
		{
			var total = manipulationStart - manipulationEnd;
			var viewport = FindViewport(this);
			if (viewport != null)
			{
				if (viewport.Viewport.Top == 0)
				{
					if (total < pullDownOffset)
						Compression(this, new CompressionEventArgs(CompressionType.Top));
					else
						Compression(this, new CompressionEventArgs(CompressionType.None));
				}
				else
				{
					Compression(this, new CompressionEventArgs(CompressionType.None));
				}
			}
		}

		void listbox_ManipulationStateChanged(object sender, EventArgs e)
		{
			if (ManipulationState == System.Windows.Controls.Primitives.ManipulationState.Idle)
			{
				isMoving = false;
				viewportChanged = false;
			}
			else if (ManipulationState == System.Windows.Controls.Primitives.ManipulationState.Manipulating)
			{
				viewportChanged = false;
				DoInterimManipulation();
			}
			else if (ManipulationState == System.Windows.Controls.Primitives.ManipulationState.Animating)
			{
				if (PulledDown)
				{
					// User released, do refresh
					var redditVM = DataContext as RedditViewModel;
					var message = new RefreshSubredditMessage();
					if (redditVM != null)
					{
						message.Subreddit = redditVM.SelectedSubreddit;
						Messenger.Default.Send<RefreshSubredditMessage>(message);
					}
					Compression(this, new CompressionEventArgs(CompressionType.None));
				}
			}
		}


		public event OnCompression Compression;

		#region LLS Util

		private static ViewportControl FindViewport(DependencyObject parent)
		{
			var childCount = VisualTreeHelper.GetChildrenCount(parent);
			for (var i = 0; i < childCount; i++)
			{
				var elt = VisualTreeHelper.GetChild(parent, i);
				if (elt is ViewportControl) return (ViewportControl)elt;
				var result = FindViewport(elt);
				if (result != null) return result;
			}
			return null;
		}
		#endregion
	}

	public class CompressionEventArgs : EventArgs
	{
		public CompressionType Type { get; protected set; }

		public CompressionEventArgs(CompressionType type)
		{
			Type = type;
		}
	}

	public enum CompressionType { None, Top, Bottom, Left, Right };

	public delegate void OnCompression(object sender, CompressionEventArgs e);
}