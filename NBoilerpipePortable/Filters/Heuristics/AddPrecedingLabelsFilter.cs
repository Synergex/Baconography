/*
 * This code is derived from boilerpipe
 * 
 */

using System.Collections.Generic;
using System.Linq;
using NBoilerpipePortable;
using NBoilerpipePortable.Document;


namespace NBoilerpipePortable.Filters.Heuristics
{
	/// <summary>Adds the labels of the preceding block to the current block, optionally adding a prefix.
	/// 	</summary>
	/// <remarks>Adds the labels of the preceding block to the current block, optionally adding a prefix.
	/// 	</remarks>
	/// <author>Christian Kohlsch√ºtter</author>
	public sealed class AddPrecedingLabelsFilter : BoilerpipeFilter
	{
		public static readonly NBoilerpipePortable.Filters.Heuristics.AddPrecedingLabelsFilter INSTANCE
			 = new NBoilerpipePortable.Filters.Heuristics.AddPrecedingLabelsFilter(string.Empty);

		public static readonly NBoilerpipePortable.Filters.Heuristics.AddPrecedingLabelsFilter INSTANCE_PRE
			 = new NBoilerpipePortable.Filters.Heuristics.AddPrecedingLabelsFilter("^");

		private readonly string labelPrefix;

		/// <summary>
		/// Creates a new
		/// <see cref="AddPrecedingLabelsFilter">AddPrecedingLabelsFilter</see>
		/// instance.
		/// </summary>
		/// <param name="maxBlocksDistance">The maximum distance in blocks.</param>
		/// <param name="contentOnly"></param>
		public AddPrecedingLabelsFilter(string labelPrefix)
		{
			this.labelPrefix = labelPrefix;
		}

		/// <exception cref="NBoilerpipePortable.BoilerpipeProcessingException"></exception>
		public bool Process(TextDocument doc)
		{
			IList<TextBlock> textBlocks = doc.GetTextBlocks();
			if (textBlocks.Count < 2)
			{
				return false;
			}
			bool changes = false;
			int remaining = textBlocks.Count;
			TextBlock blockBelow = null;
			TextBlock block;
            for (var it = textBlocks.Reverse().GetEnumerator(); it.MoveNext(); )
            {
                if (--remaining <= 0)
                {
                    break;
                }
                if (blockBelow == null)
                {

                    blockBelow = it.Current;
                    continue;
                }
                block = it.Current;
                ICollection<string> labels = block.GetLabels();
                if (labels != null && labels.Count != 0)
                {
                    foreach (string l in labels)
                    {
                        blockBelow.AddLabel(labelPrefix + l);
                    }
                    changes = true;
                }
                blockBelow = block;
            }
			return changes;
		}
	}
}
