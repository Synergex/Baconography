/*
 * This code is derived from boilerpipe
 * 
 */

using NBoilerpipePortable;
using NBoilerpipePortable.Document;
using NBoilerpipePortable.Labels;


namespace NBoilerpipePortable.Filters.Heuristics
{
	public class ArticleMetadataFilter : BoilerpipeFilter
	{
		private static readonly Sharpen.Pattern[] PATTERNS_SHORT = new Sharpen.Pattern[] 
			{ Sharpen.Pattern.Compile("^[0-9 \\,\\./]*\\b(Jan|Feb|Mar|Apr|May|Jun|Jul|Aug|Sep|Oct|Nov|Dec|January|February|March|April|May|June|July|August|September|October|November|December)?\\b[0-9 \\,\\:apm\\./]*([CPSDMGET]{2,3})?$"
			), Sharpen.Pattern.Compile("^[Bb]y ") };

		public static readonly NBoilerpipePortable.Filters.Heuristics.ArticleMetadataFilter INSTANCE
			 = new NBoilerpipePortable.Filters.Heuristics.ArticleMetadataFilter();

		public ArticleMetadataFilter()
		{
		}

		/// <exception cref="NBoilerpipePortable.BoilerpipeProcessingException"></exception>
		public virtual bool Process(TextDocument doc)
		{
			bool changed = false;
			foreach (TextBlock tb in doc.GetTextBlocks())
			{
				if (tb.GetNumWords() > 10)
				{
					continue;
				}
				string text = tb.GetText();
				foreach (Sharpen.Pattern p in PATTERNS_SHORT)
				{
					if (p.Matcher(text).Find())
					{
						changed = true;
						tb.SetIsContent(true);
						tb.AddLabel(DefaultLabels.ARTICLE_METADATA);
					}
				}
			}
			return changed;
		}
	}
}
