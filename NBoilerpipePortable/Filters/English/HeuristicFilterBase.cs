/*
 * This code is derived from boilerpipe
 * 
 */

using NBoilerpipePortable.Document;
using NBoilerpipePortable.Filters.English;


namespace NBoilerpipePortable.Filters.English
{
	/// <summary>Base class for some heuristics that are used by boilerpipe filters.</summary>
	/// <remarks>Base class for some heuristics that are used by boilerpipe filters.</remarks>
	/// <author>Christian Kohlsch√ºtter</author>
	public abstract class HeuristicFilterBase
	{
		protected internal static int GetNumFullTextWords(TextBlock tb)
		{
			return GetNumFullTextWords(tb, 9);
		}

		protected internal static int GetNumFullTextWords(TextBlock tb, float minTextDensity
			)
		{
			if (tb.GetTextDensity() >= minTextDensity)
			{
				return tb.GetNumWords();
			}
			else
			{
				return 0;
			}
		}
	}
}
