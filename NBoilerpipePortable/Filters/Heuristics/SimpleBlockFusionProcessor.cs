/*
 * This code is derived from boilerpipe
 * 
 */

using System.Collections.Generic;
using NBoilerpipePortable;
using NBoilerpipePortable.Document;
using NBoilerpipePortable.Filters.Heuristics;
using System.Linq;


namespace NBoilerpipePortable.Filters.Heuristics
{
	/// <summary>Merges two subsequent blocks if their text densities are equal.</summary>
	/// <remarks>Merges two subsequent blocks if their text densities are equal.</remarks>
	/// <author>Christian Kohlsch√ºtter</author>
	public class SimpleBlockFusionProcessor : BoilerpipeFilter
	{
		public static readonly SimpleBlockFusionProcessor INSTANCE = new SimpleBlockFusionProcessor
			();

		/// <summary>Returns the singleton instance for BlockFusionProcessor.</summary>
		/// <remarks>Returns the singleton instance for BlockFusionProcessor.</remarks>
		public static SimpleBlockFusionProcessor GetInstance()
		{
			return INSTANCE;
		}

		/// <exception cref="NBoilerpipePortable.BoilerpipeProcessingException"></exception>
		public virtual bool Process(TextDocument doc)
		{
			IList<TextBlock> textBlocks = doc.GetTextBlocks();
			bool changes = false;
			if (textBlocks.Count < 2)
			{
				return false;
			}
			TextBlock b1 = textBlocks[0];
			foreach (var b2 in new List<TextBlock>(textBlocks.Skip(1)) )
			{
				bool similar = (b1.GetTextDensity() == b2.GetTextDensity());
				if (similar)
				{
					b1.MergeNext(b2);
                    textBlocks.Remove(b2);
					changes = true;
				}
				else
				{
					b1 = b2;
				}
			}
			return changes;
		}
	}
}
