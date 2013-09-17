/*
 * This code is derived from boilerpipe
 * 
 */

using NBoilerpipePortable.Document;


namespace NBoilerpipePortable.Estimators
{
	/// <summary>
	/// Estimates the "goodness" of a
	/// <see cref="NBoilerpipePortable.BoilerpipeExtractor">NBoilerpipePortable.BoilerpipeExtractor</see>
	/// on a given document.
	/// </summary>
	/// <author>Christian Kohlsch√ºtter</author>
	public sealed class SimpleEstimator
	{
		/// <summary>
		/// Returns the singleton instance of
		/// <see cref="SimpleEstimator">SimpleEstimator</see>
		/// </summary>
		public static readonly NBoilerpipePortable.Estimators.SimpleEstimator INSTANCE = new NBoilerpipePortable.Estimators.SimpleEstimator
			();

		public SimpleEstimator()
		{
		}

		/// <summary>
		/// Given the statistics of the document before and after applying the
		/// <see cref="NBoilerpipePortable.BoilerpipeExtractor">NBoilerpipePortable.BoilerpipeExtractor</see>
		/// ,
		/// can we regard the extraction quality (too) low?
		/// Works well with
		/// <see cref="NBoilerpipePortable.Extractors.DefaultExtractor">NBoilerpipePortable.Extractors.DefaultExtractor
		/// 	</see>
		/// ,
		/// <see cref="NBoilerpipePortable.Extractors.ArticleExtractor">NBoilerpipePortable.Extractors.ArticleExtractor
		/// 	</see>
		/// and others.
		/// </summary>
		/// <param name="dsBefore"></param>
		/// <param name="dsAfter"></param>
		/// <returns>true if low quality is to be expected.</returns>
		public bool IsLowQuality(TextDocumentStatistics dsBefore, TextDocumentStatistics 
			dsAfter)
		{
			if (dsBefore.GetNumWords() < 90 || dsAfter.GetNumWords() < 70)
			{
				return true;
			}
			if (dsAfter.AvgNumWords() < 25)
			{
				return true;
			}
			return false;
		}
	}
}
