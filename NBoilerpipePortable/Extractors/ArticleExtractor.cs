/*
 * This code is derived from boilerpipe
 * 
 */

using NBoilerpipePortable.Document;
using NBoilerpipePortable.Extractors;
using NBoilerpipePortable.Filters.English;
using NBoilerpipePortable.Filters.Heuristics;
using NBoilerpipePortable.Filters.Simple;


namespace NBoilerpipePortable.Extractors
{
	/// <summary>A full-text extractor which is tuned towards news articles.</summary>
	/// <remarks>
	/// A full-text extractor which is tuned towards news articles. In this scenario
	/// it achieves higher accuracy than
	/// <see cref="DefaultExtractor">DefaultExtractor</see>
	/// .
	/// </remarks>
	/// <author>Christian Kohlsch√ºtter</author>
	public sealed class ArticleExtractor : ExtractorBase
	{
		public static readonly ArticleExtractor INSTANCE = new ArticleExtractor();

		/// <summary>
		/// Returns the singleton instance for
		/// <see cref="ArticleExtractor">ArticleExtractor</see>
		/// .
		/// </summary>
		public static ArticleExtractor GetInstance()
		{
			return INSTANCE;
		}

		/// <exception cref="NBoilerpipePortable.BoilerpipeProcessingException"></exception>
		public override bool Process (TextDocument doc)
		{
			
			bool ret = TerminatingBlocksFinder.INSTANCE.Process (doc) 
				| new DocumentTitleMatchClassifier (doc.GetTitle ()).Process (doc) 
				| NumWordsRulesClassifier.INSTANCE.Process (doc) 
				| IgnoreBlocksAfterContentFilter.DEFAULT_INSTANCE.Process (doc) 
				| BlockProximityFusion.MAX_DISTANCE_1.Process (doc) 
				| BoilerplateBlockFilter.INSTANCE.Process (doc) 
				| BlockProximityFusion.MAX_DISTANCE_1_CONTENT_ONLY.Process (doc) 
				| KeepLargestBlockFilter.INSTANCE.Process (doc) 
				| ExpandTitleToContentFilter.INSTANCE.Process (doc);
			
			return ret;
		}
	}
}