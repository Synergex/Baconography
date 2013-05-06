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
		bool viewportChanged = false;
		bool isMoving = false;
		double manipulationStart = 0;
		double manipulationEnd = 0;

		public FixedLongListSelector()
		{
			SelectionChanged += FixedLongListSelector_SelectionChanged;
			ManipulationStateChanged += listbox_ManipulationStateChanged;
			MouseMove += listbox_MouseMove;
			ItemRealized += OnViewportChanged;
			ItemUnrealized += OnViewportChanged;
			Compression += FixedLongListSelector_Compression;
		}

		void FixedLongListSelector_Compression(object sender, CompressionEventArgs e)
		{
			if (e.Type == CompressionType.Top)
				PulledDown = true;
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
			   typeof(Nullable<bool>),
			   typeof(FixedLongListSelector),
			   new PropertyMetadata(null, OnPulledDownChanged)
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
			selector.PulledDown = (Nullable<bool>)e.NewValue ?? false;
		}

		public bool PulledDown
		{
			get { return (Nullable<bool>)GetValue(PulledDownProperty) ?? false; }
			set { SetValue(PulledDownProperty, value); }
		}

		void OnViewportChanged(object sender, Microsoft.Phone.Controls.ItemRealizationEventArgs e)
		{
			viewportChanged = true;
		}

		void listbox_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
		{
			var pos = e.GetPosition(null);

			if (!isMoving)
				manipulationStart = pos.Y;
			else
				manipulationEnd = pos.Y;

			isMoving = true;
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
			}
			else if (ManipulationState == System.Windows.Controls.Primitives.ManipulationState.Animating)
			{
				var total = manipulationStart - manipulationEnd;

				if (!viewportChanged && Compression != null)
				{
					if (total < 0)
						Compression(this, new CompressionEventArgs(CompressionType.Top));
					else if (total > 0) // Explicitly exclude total == 0 case
						Compression(this, new CompressionEventArgs(CompressionType.Bottom));
				}
			}
		}


		public event OnCompression Compression;
	}

	public class CompressionEventArgs : EventArgs
	{
		public CompressionType Type { get; protected set; }

		public CompressionEventArgs(CompressionType type)
		{
			Type = type;
		}
	}

	public enum CompressionType { Top, Bottom, Left, Right };

	public delegate void OnCompression(object sender, CompressionEventArgs e);
}