/*
 * This code is derived from boilerpipe
 * 
 */

using NBoilerpipePortable.Document;
using NBoilerpipePortable.Extractors;
using NBoilerpipePortable.Filters.Heuristics;
using NBoilerpipePortable.Filters.Simple;


namespace NBoilerpipePortable.Extractors
{
	/// <summary>A full-text extractor which extracts the largest text component of a page.
	/// 	</summary>
	/// <remarks>
	/// A full-text extractor which extracts the largest text component of a page.
	/// For news articles, it may perform better than the
	/// <see cref="DefaultExtractor">DefaultExtractor</see>
	/// ,
	/// but usually worse than
	/// <see cref="ArticleExtractor">ArticleExtractor</see>
	/// .
	/// </remarks>
	/// <author>Christian Kohlsch√ºtter</author>
	public sealed class KeepEverythingWithMinKWordsExtractor : ExtractorBase
	{
		private readonly MinWordsFilter filter;

		public KeepEverythingWithMinKWordsExtractor(int kMin)
		{
			this.filter = new MinWordsFilter(kMin);
		}

		/// <exception cref="NBoilerpipePortable.BoilerpipeProcessingException"></exception>
		public override bool Process(TextDocument doc)
		{
			return SimpleBlockFusionProcessor.INSTANCE.Process(doc) | MarkEverythingContentFilter
				.INSTANCE.Process(doc) | filter.Process(doc);
		}
	}
}
