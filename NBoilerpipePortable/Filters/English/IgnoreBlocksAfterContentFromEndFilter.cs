/*
 * This code is derived from boilerpipe
 * 
 */

using System.Collections.Generic;
using NBoilerpipePortable;
using NBoilerpipePortable.Document;
using NBoilerpipePortable.Filters.English;
using NBoilerpipePortable.Labels;
using System.Linq;


namespace NBoilerpipePortable.Filters.English
{
	/// <summary>
	/// Marks all blocks as "non-content" that occur after blocks that have been
	/// marked
	/// <see cref="NBoilerpipePortable.Labels.DefaultLabels.INDICATES_END_OF_TEXT">NBoilerpipePortable.Labels.DefaultLabels.INDICATES_END_OF_TEXT
	/// 	</see>
	/// , and after any content block.
	/// This filter can be used in conjunction with an upstream
	/// <see cref="TerminatingBlocksFinder">TerminatingBlocksFinder</see>
	/// .
	/// </summary>
	/// <author>Christian Kohlsch√ºtter</author>
	/// <seealso cref="TerminatingBlocksFinder">TerminatingBlocksFinder</seealso>
	public sealed class IgnoreBlocksAfterContentFromEndFilter : HeuristicFilterBase, 
		BoilerpipeFilter
	{
		public static readonly NBoilerpipePortable.Filters.English.IgnoreBlocksAfterContentFromEndFilter
			 INSTANCE = new NBoilerpipePortable.Filters.English.IgnoreBlocksAfterContentFromEndFilter
			();

		public IgnoreBlocksAfterContentFromEndFilter()
		{
		}

		/// <exception cref="NBoilerpipePortable.BoilerpipeProcessingException"></exception>
		public bool Process(TextDocument doc)
		{
			bool changes = false;
			int words = 0;
			IList<TextBlock> blocks = doc.GetTextBlocks();
			if (blocks.Count > 0)
			{
				foreach(var tb in blocks.Reverse())
				{
					if (tb.HasLabel(DefaultLabels.INDICATES_END_OF_TEXT))
					{
						tb.AddLabel(DefaultLabels.STRICTLY_NOT_CONTENT);
						tb.RemoveLabel(DefaultLabels.MIGHT_BE_CONTENT);
						tb.SetIsContent(false);
						changes = true;
					}
					else
					{
						if (tb.IsContent())
						{
							words += tb.GetNumWords();
							if (words > 200)
							{
								break;
							}
						}
					}
				}
			}
			return changes;
		}
	}
}
