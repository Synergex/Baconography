/*
 * This code is derived from boilerpipe
 * 
 */

using System.Collections.Generic;
using NBoilerpipePortable;
using NBoilerpipePortable.Document;
using NBoilerpipePortable.Filters.English;


namespace NBoilerpipePortable.Filters.English
{
	/// <summary>
	/// Classifies
	/// <see cref="NBoilerpipePortable.Document.TextBlock">NBoilerpipePortable.Document.TextBlock</see>
	/// s as content/not-content through rules that have
	/// been determined using the C4.8 machine learning algorithm, as described in the
	/// paper "Boilerplate Detection using Shallow Text Features", particularly using
	/// text densities and link densities.
	/// </summary>
	/// <author>Christian Kohlsch√ºtter</author>
	public class DensityRulesClassifier : BoilerpipeFilter
	{
		public static readonly DensityRulesClassifier INSTANCE = new DensityRulesClassifier
			();

		/// <summary>Returns the singleton instance for RulebasedBoilerpipeClassifier.</summary>
		/// <remarks>Returns the singleton instance for RulebasedBoilerpipeClassifier.</remarks>
		public static DensityRulesClassifier GetInstance()
		{
			return INSTANCE;
		}

		/// <exception cref="NBoilerpipePortable.BoilerpipeProcessingException"></exception>
		public virtual bool Process(TextDocument doc)
		{
			IList<TextBlock> textBlocks = doc.GetTextBlocks();
			bool hasChanges = false;
            var it = textBlocks.GetEnumerator();
			if (!it.MoveNext())
			{
				return false;
			}
			TextBlock prevBlock = TextBlock.EMPTY_START;
			TextBlock currentBlock = it.Current;
            TextBlock nextBlock = it.MoveNext() ? it.Current : TextBlock.EMPTY_START;
			hasChanges = Classify(prevBlock, currentBlock, nextBlock) | hasChanges;
			if (nextBlock != TextBlock.EMPTY_START)
			{
                while (it.MoveNext())
				{
					prevBlock = currentBlock;
					currentBlock = nextBlock;
                    nextBlock = it.Current;
					hasChanges = Classify(prevBlock, currentBlock, nextBlock) | hasChanges;
				}
				prevBlock = currentBlock;
				currentBlock = nextBlock;
				nextBlock = TextBlock.EMPTY_START;
				hasChanges = Classify(prevBlock, currentBlock, nextBlock) | hasChanges;
			}
			return hasChanges;
		}

		protected internal virtual bool Classify(TextBlock prev, TextBlock curr, TextBlock
			 next)
		{
			bool isContent;
			if (curr.GetLinkDensity() <= 0.333333)
			{
				if (prev.GetLinkDensity() <= 0.555556)
				{
					if (curr.GetTextDensity() <= 9)
					{
						if (next.GetTextDensity() <= 10)
						{
							if (prev.GetTextDensity() <= 4)
							{
								isContent = false;
							}
							else
							{
								isContent = true;
							}
						}
						else
						{
							isContent = true;
						}
					}
					else
					{
						if (next.GetTextDensity() == 0)
						{
							isContent = false;
						}
						else
						{
							isContent = true;
						}
					}
				}
				else
				{
					if (next.GetTextDensity() <= 11)
					{
						isContent = false;
					}
					else
					{
						isContent = true;
					}
				}
			}
			else
			{
				isContent = false;
			}
			return curr.SetIsContent(isContent);
		}
	}
}
