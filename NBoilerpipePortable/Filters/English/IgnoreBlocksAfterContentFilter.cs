/*
 * This code is derived from boilerpipe
 * 
 */

using NBoilerpipePortable;
using NBoilerpipePortable.Document;
using NBoilerpipePortable.Filters.English;
using NBoilerpipePortable.Labels;


namespace NBoilerpipePortable.Filters.English
{
	/// <summary>
	/// Marks all blocks as "non-content" that occur after blocks that have been
	/// marked
	/// <see cref="NBoilerpipePortable.Labels.DefaultLabels.INDICATES_END_OF_TEXT">NBoilerpipePortable.Labels.DefaultLabels.INDICATES_END_OF_TEXT
	/// 	</see>
	/// . These marks are ignored
	/// unless a minimum number of words in content blocks occur before this mark (default: 60).
	/// This can be used in conjunction with an upstream
	/// <see cref="TerminatingBlocksFinder">TerminatingBlocksFinder</see>
	/// .
	/// </summary>
	/// <author>Christian Kohlsch√ºtter</author>
	/// <seealso cref="TerminatingBlocksFinder">TerminatingBlocksFinder</seealso>
	public sealed class IgnoreBlocksAfterContentFilter : HeuristicFilterBase, BoilerpipeFilter
	{
		public static readonly NBoilerpipePortable.Filters.English.IgnoreBlocksAfterContentFilter
			 DEFAULT_INSTANCE = new NBoilerpipePortable.Filters.English.IgnoreBlocksAfterContentFilter
			(60);

		public static readonly NBoilerpipePortable.Filters.English.IgnoreBlocksAfterContentFilter
			 INSTANCE_200 = new NBoilerpipePortable.Filters.English.IgnoreBlocksAfterContentFilter(200
			);

		private readonly int minNumWords;

		/// <summary>Returns the singleton instance for DeleteBlocksAfterContentFilter.</summary>
		/// <remarks>Returns the singleton instance for DeleteBlocksAfterContentFilter.</remarks>
		public static NBoilerpipePortable.Filters.English.IgnoreBlocksAfterContentFilter GetDefaultInstance
			()
		{
			return DEFAULT_INSTANCE;
		}

		public IgnoreBlocksAfterContentFilter(int minNumWords)
		{
			this.minNumWords = minNumWords;
		}

		/// <exception cref="NBoilerpipePortable.BoilerpipeProcessingException"></exception>
		public bool Process(TextDocument doc)
		{
			bool changes = false;
			int numWords = 0;
			bool foundEndOfText = false;
			for (var it = doc.GetTextBlocks().GetEnumerator(); it.MoveNext(); )
			{
				TextBlock block = it.Current;
				bool endOfText = block.HasLabel(DefaultLabels.INDICATES_END_OF_TEXT);
				if (block.IsContent())
				{
					numWords += GetNumFullTextWords(block);
				}
				if (endOfText && numWords >= minNumWords)
				{
					foundEndOfText = true;
				}
				if (foundEndOfText)
				{
					changes = true;
					block.SetIsContent(false);
				}
			}
			return changes;
		}
	}
}
