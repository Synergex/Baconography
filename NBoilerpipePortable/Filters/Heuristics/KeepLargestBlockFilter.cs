/*
 * This code is derived from boilerpipe
 * 
 */

using System.Collections.Generic;
using NBoilerpipePortable;
using NBoilerpipePortable.Document;
using NBoilerpipePortable.Labels;
using System.Linq;


namespace NBoilerpipePortable.Filters.Heuristics
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
	/// Note that, by default, only TextBlocks marked as "content" are taken into consideration.
	/// </summary>
	/// <author>Christian Kohlsch√ºtter</author>
	public sealed class KeepLargestBlockFilter : BoilerpipeFilter
	{
		public static readonly NBoilerpipePortable.Filters.Heuristics.KeepLargestBlockFilter INSTANCE
			 = new NBoilerpipePortable.Filters.Heuristics.KeepLargestBlockFilter(false, 0);

		public static readonly NBoilerpipePortable.Filters.Heuristics.KeepLargestBlockFilter INSTANCE_EXPAND_TO_SAME_TAGLEVEL
			 = new NBoilerpipePortable.Filters.Heuristics.KeepLargestBlockFilter(true, 0);

        public static readonly NBoilerpipePortable.Filters.Heuristics.KeepLargestBlockFilter INSTANCE_EXPAND_TO_SAME_TAGLEVEL_MIN_WORDS
             = new NBoilerpipePortable.Filters.Heuristics.KeepLargestBlockFilter(true, 150);

		private readonly bool expandToSameLevelText;
        private readonly int minWords;

		public KeepLargestBlockFilter(bool expandToSameLevelText, int minWords)
		{
            this.minWords = minWords;
			this.expandToSameLevelText = expandToSameLevelText;
		}

		/// <exception cref="NBoilerpipePortable.BoilerpipeProcessingException"></exception>
		public bool Process(TextDocument doc)
		{
			IList<TextBlock> textBlocks = doc.GetTextBlocks();
			if (textBlocks.Count < 2)
			{
				return false;
			}
			int maxNumWords = -1;
			TextBlock largestBlock = null;
			int level = -1;
			int i = 0;
			int n = -1;
			foreach (TextBlock tb in textBlocks)
			{
				if (tb.IsContent())
				{
					int nw = tb.GetNumWords();
					if (nw > maxNumWords)
					{
						largestBlock = tb;
						maxNumWords = nw;
						n = i;
						if (expandToSameLevelText)
						{
							level = tb.GetTagLevel();
						}
					}
				}
				i++;
			}
			foreach (TextBlock tb in textBlocks)
			{
				if (tb == largestBlock)
				{
					tb.SetIsContent(true);
				}
				else
				{
					tb.SetIsContent(false);
					tb.AddLabel(DefaultLabels.MIGHT_BE_CONTENT);
				}
			}
			if (expandToSameLevelText && n != -1)
			{
                foreach (var tb in textBlocks.Take(n).Reverse())
                {
                    int tl = tb.GetTagLevel();
                    if (tl < level)
                    {
                        break;
                    }
                    else
                    {
                        if (tl == level)
                        {
                            if(tb.GetNumWords() >= minWords)
                                tb.SetIsContent(true);
                        }
                    }
                }

                foreach (var tb in textBlocks.Skip(n))
                {
                    int tl = tb.GetTagLevel();
                    if (tl < level)
                    {
                        break;
                    }
                    else
                    {
                        if (tl == level)
                        {
                            if (tb.GetNumWords() >= minWords)
                                tb.SetIsContent(true);
                        }
                    }
                }
			}
			return true;
		}
	}
}
