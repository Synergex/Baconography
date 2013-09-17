/*
 * This code is derived from boilerpipe
 * 
 */

using NBoilerpipePortable;
using NBoilerpipePortable.Document;
using NBoilerpipePortable.Filters.English;


namespace NBoilerpipePortable.Filters.English
{
	/// <summary>
	/// Keeps only those content blocks which contain at least k full-text words
	/// (measured by
	/// <see cref="HeuristicFilterBase.GetNumFullTextWords(NBoilerpipePortable.Document.TextBlock)
	/// 	">HeuristicFilterBase.GetNumFullTextWords(NBoilerpipePortable.Document.TextBlock)</see>
	/// ). k is 30 by default.
	/// </summary>
	/// <author>Christian Kohlsch√ºtter</author>
	public sealed class MinFulltextWordsFilter : HeuristicFilterBase, BoilerpipeFilter
	{
		public static readonly NBoilerpipePortable.Filters.English.MinFulltextWordsFilter DEFAULT_INSTANCE
			 = new NBoilerpipePortable.Filters.English.MinFulltextWordsFilter(30);

		private readonly int minWords;

		public static NBoilerpipePortable.Filters.English.MinFulltextWordsFilter GetDefaultInstance
			()
		{
			return DEFAULT_INSTANCE;
		}

		public MinFulltextWordsFilter(int minWords)
		{
			this.minWords = minWords;
		}

		/// <exception cref="NBoilerpipePortable.BoilerpipeProcessingException"></exception>
		public bool Process(TextDocument doc)
		{
			bool changes = false;
			foreach (TextBlock tb in doc.GetTextBlocks())
			{
				if (!tb.IsContent())
				{
					continue;
				}
				if (GetNumFullTextWords(tb) < minWords)
				{
					tb.SetIsContent(false);
					changes = true;
				}
			}
			return changes;
		}
	}
}
