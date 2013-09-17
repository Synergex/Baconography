/*
 * This code is derived from boilerpipe
 * 
 */

using System.Collections.Generic;
using NBoilerpipePortable;
using NBoilerpipePortable.Document;
using NBoilerpipePortable.Filters.English;
using NBoilerpipePortable.Labels;


namespace NBoilerpipePortable.Filters.English
{
	/// <summary>
	/// Keeps the largest
	/// <see cref="NBoilerpipePortable.Document.TextBlock">NBoilerpipePortable.Document.TextBlock</see>
	/// only (by the number of words). In case of
	/// more than one block with the same number of words, the first block is chosen.
	/// All discarded blocks are marked "not content" and flagged as
	/// <see cref="NBoilerpipePortable.Labels.DefaultLabels.MIGHT_BE_CONTENT">NBoilerpipePortable.Labels.DefaultLabels.MIGHT_BE_CONTENT
	/// 	</see>
	/// .
	/// As opposed to
	/// <see cref="NBoilerpipePortable.Filters.Heuristics.KeepLargestBlockFilter">NBoilerpipePortable.Filters.Heuristics.KeepLargestBlockFilter
	/// 	</see>
	/// , the number of words are
	/// computed using
	/// <see cref="HeuristicFilterBase.GetNumFullTextWords(NBoilerpipePortable.Document.TextBlock)
	/// 	">HeuristicFilterBase.GetNumFullTextWords(NBoilerpipePortable.Document.TextBlock)</see>
	/// , which only counts
	/// words that occur in text elements with at least 9 words and are thus believed to be full text.
	/// NOTE: Without language-specific fine-tuning (i.e., running the default instance), this filter
	/// may lead to suboptimal results. You better use
	/// <see cref="NBoilerpipePortable.Filters.Heuristics.KeepLargestBlockFilter">NBoilerpipePortable.Filters.Heuristics.KeepLargestBlockFilter
	/// 	</see>
	/// instead, which
	/// works at the level of number-of-words instead of text densities.
	/// </summary>
	/// <author>Christian Kohlsch√ºtter</author>
	public sealed class KeepLargestFulltextBlockFilter : HeuristicFilterBase, BoilerpipeFilter
	{
		public static readonly KeepLargestFulltextBlockFilter INSTANCE = new KeepLargestFulltextBlockFilter
			();

		/// <exception cref="NBoilerpipePortable.BoilerpipeProcessingException"></exception>
		public bool Process(TextDocument doc)
		{
			IList<TextBlock> textBlocks = doc.GetTextBlocks();
			if (textBlocks.Count < 2)
			{
				return false;
			}
			int max = -1;
			TextBlock largestBlock = null;
			int index = 0;
			foreach (TextBlock tb in textBlocks)
			{
				if (!tb.IsContent())
				{
					continue;
				}
				int numWords = GetNumFullTextWords(tb);
				if (numWords > max)
				{
					largestBlock = tb;
					max = numWords;
				}
				index++;
			}
			if (largestBlock == null)
			{
				return false;
			}
			foreach (TextBlock tb_1 in textBlocks)
			{
				if (tb_1 == largestBlock)
				{
					tb_1.SetIsContent(true);
                    tb_1.AddLabel(DefaultLabels.VERY_LIKELY_CONTENT);
				}
				else
				{
					tb_1.SetIsContent(false);
					tb_1.AddLabel(DefaultLabels.MIGHT_BE_CONTENT);
				}
			}
			return true;
		}
	}
}
