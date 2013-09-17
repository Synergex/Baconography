/*
 * This code is derived from boilerpipe
 * 
 */

using NBoilerpipePortable;
using NBoilerpipePortable.Document;


namespace NBoilerpipePortable.Filters.Simple
{
	/// <summary>Keeps only those content blocks which contain at least <em>k</em> words.
	/// 	</summary>
	/// <remarks>Keeps only those content blocks which contain at least <em>k</em> words.
	/// 	</remarks>
	/// <author>Christian Kohlsch√ºtter</author>
	public sealed class MinWordsFilter : BoilerpipeFilter
	{
		private readonly int minWords;

		public MinWordsFilter(int minWords)
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
				if (tb.GetNumWords() < minWords)
				{
					tb.SetIsContent(false);
					changes = true;
				}
			}
			return changes;
		}
	}
}
