/*
 * This code is derived from boilerpipe
 * 
 */

using System.Collections.Generic;
using System.Linq;
using NBoilerpipePortable;
using NBoilerpipePortable.Document;
using NBoilerpipePortable.Labels;


namespace NBoilerpipePortable.Filters.Heuristics
{
	public sealed class ContentFusion : BoilerpipeFilter
	{
		public static readonly NBoilerpipePortable.Filters.Heuristics.ContentFusion INSTANCE = new 
			NBoilerpipePortable.Filters.Heuristics.ContentFusion();

		/// <summary>
		/// Creates a new
		/// <see cref="ContentFusion">ContentFusion</see>
		/// instance.
		/// </summary>
		public ContentFusion()
		{
		}

		/// <exception cref="NBoilerpipePortable.BoilerpipeProcessingException"></exception>
		public bool Process(TextDocument doc)
		{
            IList<TextBlock> textBlocks = doc.GetTextBlocks();
            bool changes = false;
            if (textBlocks.Count < 2)
            {
                return false;
            }
            TextBlock b1 = textBlocks[0];

            do
            {
                foreach (var b2 in new List<TextBlock>(textBlocks.Skip(1)))
                {
                    if (b1.IsContent() && b2.GetLinkDensity() < 0.56 && !b2.HasLabel(DefaultLabels
						    .STRICTLY_NOT_CONTENT))
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
            }
            while (changes);
            return true;
		}
	}
}
