/*
 * This code is derived from boilerpipe
 * 
 */

using NBoilerpipePortable.Extractors;


namespace NBoilerpipePortable.Extractors
{
	/// <summary>
	/// Provides quick access to common
	/// <see cref="NBoilerpipePortable.BoilerpipeExtractor">NBoilerpipePortable.BoilerpipeExtractor</see>
	/// s.
	/// </summary>
	/// <author>Christian Kohlsch√ºtter</author>
	public sealed class CommonExtractors
	{
		public CommonExtractors()
		{
		}

		/// <summary>Works very well for most types of Article-like HTML.</summary>
		/// <remarks>Works very well for most types of Article-like HTML.</remarks>
		public static readonly ArticleExtractor ARTICLE_EXTRACTOR = ArticleExtractor.INSTANCE;

		/// <summary>
		/// Usually worse than
		/// <see cref="ArticleExtractor">ArticleExtractor</see>
		/// , but simpler/no heuristics.
		/// </summary>
		public static readonly DefaultExtractor DEFAULT_EXTRACTOR = DefaultExtractor.INSTANCE;

		/// <summary>
		/// Like
		/// <see cref="DefaultExtractor">DefaultExtractor</see>
		/// , but keeps the largest text block only.
		/// </summary>
		public static readonly LargestContentExtractor LARGEST_CONTENT_EXTRACTOR = LargestContentExtractor
			.INSTANCE;

		/// <summary>Trained on krdwrd Canola (different definition of "boilerplate").</summary>
		/// <remarks>
		/// Trained on krdwrd Canola (different definition of "boilerplate"). You may
		/// give it a try.
		/// </remarks>
		public static readonly CanolaExtractor CANOLA_EXTRACTOR = CanolaExtractor.INSTANCE;

		/// <summary>Dummy Extractor; should return the input text.</summary>
		/// <remarks>
		/// Dummy Extractor; should return the input text. Use this to double-check
		/// that your problem is within a particular
		/// <see cref="NBoilerpipePortable.BoilerpipeExtractor">NBoilerpipePortable.BoilerpipeExtractor</see>
		/// , or
		/// somewhere else.
		/// </remarks>
		public static readonly KeepEverythingExtractor KEEP_EVERYTHING_EXTRACTOR = KeepEverythingExtractor
			.INSTANCE;
	}
}
