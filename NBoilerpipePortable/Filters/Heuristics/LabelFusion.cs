/*
 * This code is derived from boilerpipe
 * 
 */

using System.Collections.Generic;
using NBoilerpipePortable;
using NBoilerpipePortable.Document;
using NBoilerpipePortable.Labels;
using System.Linq;


namespace NBoilerpipePortable.Filters.Heuristics
{
	/// <summary>Fuses adjacent blocks if their labels are equal.</summary>
	/// <remarks>Fuses adjacent blocks if their labels are equal.</remarks>
	/// <author>Christian Kohlsch√ºtter</author>
	public sealed class LabelFusion : BoilerpipeFilter
	{
		public static readonly NBoilerpipePortable.Filters.Heuristics.LabelFusion INSTANCE = new 
			NBoilerpipePortable.Filters.Heuristics.LabelFusion(string.Empty);

		private readonly string labelPrefix;

		/// <summary>
		/// Creates a new
		/// <see cref="LabelFusion">LabelFusion</see>
		/// instance.
		/// </summary>
		/// <param name="maxBlocksDistance">The maximum distance in blocks.</param>
		/// <param name="contentOnly"></param>
		public LabelFusion(string labelPrefix)
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
			TextBlock prevBlock = textBlocks[0];
			foreach (var block in new List<TextBlock>(textBlocks.Skip(1)))
			{
				if (EqualLabels(prevBlock.GetLabels(), block.GetLabels()))
				{
					prevBlock.MergeNext(block);
                    textBlocks.Remove(block);
					changes = true;
				}
				else
				{
					prevBlock = block;
				}
			}
			return changes;
		}

		private bool EqualLabels(ICollection<string> labels, ICollection<string> labels2)
		{
			if (labels == null || labels2 == null)
			{
				return false;
			}
			return MarkupLabelsOnly(labels).Equals(MarkupLabelsOnly(labels2));
		}

		private ICollection<string> MarkupLabelsOnly(ICollection<string> set1)
		{
			return new HashSet<string>(set1.Where(str => !str.StartsWith(DefaultLabels.MARKUP_PREFIX)));
		}
	}
}
