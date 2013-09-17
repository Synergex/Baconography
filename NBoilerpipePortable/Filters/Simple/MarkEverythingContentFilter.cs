/*
 * This code is derived from boilerpipe
 * 
 */

using NBoilerpipePortable;
using NBoilerpipePortable.Document;


namespace NBoilerpipePortable.Filters.Simple
{
	/// <summary>Marks all blocks as content.</summary>
	/// <remarks>Marks all blocks as content.</remarks>
	/// <author>Christian Kohlsch√ºtter</author>
	public sealed class MarkEverythingContentFilter : BoilerpipeFilter
	{
		public static readonly NBoilerpipePortable.Filters.Simple.MarkEverythingContentFilter INSTANCE
			 = new NBoilerpipePortable.Filters.Simple.MarkEverythingContentFilter();

		public MarkEverythingContentFilter()
		{
		}

		/// <exception cref="NBoilerpipePortable.BoilerpipeProcessingException"></exception>
		public bool Process(TextDocument doc)
		{
			bool changes = false;
			foreach (TextBlock tb in doc.GetTextBlocks())
			{
				if (!tb.IsContent())
				{
					tb.SetIsContent(true);
					changes = true;
				}
			}
			return changes;
		}
	}
}
