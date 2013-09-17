/*
 * This code is derived from boilerpipe
 * 
 */

using NBoilerpipePortable.Document;
using NBoilerpipePortable.Extractors;
using NBoilerpipePortable.Filters.English;
using NBoilerpipePortable.Filters.Heuristics;


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
	public sealed class LargestContentExtractor : ExtractorBase
	{
		public static readonly NBoilerpipePortable.Extractors.LargestContentExtractor INSTANCE = 
			new NBoilerpipePortable.Extractors.LargestContentExtractor();

		public LargestContentExtractor()
		{
		}

		/// <summary>
		/// Returns the singleton instance for
		/// <see cref="LargestContentExtractor">LargestContentExtractor</see>
		/// .
		/// </summary>
		public static NBoilerpipePortable.Extractors.LargestContentExtractor GetInstance()
		{
			return INSTANCE;
		}

		/// <exception cref="NBoilerpipePortable.BoilerpipeProcessingException"></exception>
		public override bool Process(TextDocument doc)
		{
			return NumWordsRulesClassifier.INSTANCE.Process(doc) | BlockProximityFusion.MAX_DISTANCE_1
				.Process(doc) | KeepLargestBlockFilter.INSTANCE.Process(doc);
		}
	}
}
