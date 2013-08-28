/*
 * This code is derived from boilerpipe
 * 
 */

using NBoilerpipePortable.Document;
using NBoilerpipePortable.Extractors;
using NBoilerpipePortable.Filters.Simple;


namespace NBoilerpipePortable.Extractors
{
	/// <summary>Marks everything as content.</summary>
	/// <remarks>Marks everything as content.</remarks>
	/// <author>Christian Kohlsch√ºtter</author>
	public sealed class KeepEverythingExtractor : ExtractorBase
	{
		public static readonly NBoilerpipePortable.Extractors.KeepEverythingExtractor INSTANCE = 
			new NBoilerpipePortable.Extractors.KeepEverythingExtractor();

		public KeepEverythingExtractor()
		{
		}

		/// <exception cref="NBoilerpipePortable.BoilerpipeProcessingException"></exception>
		public override bool Process(TextDocument doc)
		{
			return MarkEverythingContentFilter.INSTANCE.Process(doc);
		}
	}
}
