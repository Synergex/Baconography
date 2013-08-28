/*
 * This code is derived from boilerpipe
 * 
 */

using NBoilerpipePortable;
using NBoilerpipePortable.Document;
using NBoilerpipePortable.Labels;


namespace NBoilerpipePortable.Filters.Simple
{
	/// <summary>Marks all blocks that contain a given label as "boilerplate".</summary>
	/// <remarks>Marks all blocks that contain a given label as "boilerplate".</remarks>
	/// <author>Christian Kohlsch√ºtter</author>
	public sealed class LabelToBoilerplateFilter : BoilerpipeFilter
	{
		public static readonly NBoilerpipePortable.Filters.Simple.LabelToBoilerplateFilter INSTANCE_STRICTLY_NOT_CONTENT
			 = new NBoilerpipePortable.Filters.Simple.LabelToBoilerplateFilter(DefaultLabels.STRICTLY_NOT_CONTENT
			);

		private string[] labels;

		public LabelToBoilerplateFilter(params string[] label)
		{
			this.labels = label;
		}

		/// <exception cref="NBoilerpipePortable.BoilerpipeProcessingException"></exception>
		public bool Process (TextDocument doc)
		{
			bool changes = false;
			foreach (TextBlock tb in doc.GetTextBlocks()) {
				if (tb.IsContent ()) {
					foreach (string label in labels) {
						if (tb.HasLabel (label)) {
							tb.SetIsContent (false);
							changes = true;
							goto BLOCK_LOOP_continue;
						}
					}
					BLOCK_LOOP_continue: {}
				}
			}
			return changes;
		}
	}
}
