using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace BaconographyWP8.Common
{
	class Utility
	{
		public static SolidColorBrush GetColorFromHexa(string hexaColor)
		{
			return new SolidColorBrush(
				Color.FromArgb(
					Convert.ToByte(hexaColor.Substring(1, 2), 16),
					Convert.ToByte(hexaColor.Substring(3, 2), 16),
					Convert.ToByte(hexaColor.Substring(5, 2), 16),
					Convert.ToByte(hexaColor.Substring(7, 2), 16)
				)
			);
		}
	}
}
