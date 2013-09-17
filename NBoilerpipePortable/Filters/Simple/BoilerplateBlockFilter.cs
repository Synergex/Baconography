/*
 * This code is derived from boilerpipe
 * 
 */

using System.Collections.Generic;
using NBoilerpipePortable;
using NBoilerpipePortable.Document;
using NBoilerpipePortable.Filters.Simple;
using System.Linq;
using NBoilerpipePortable.Labels;


namespace NBoilerpipePortable.Filters.Simple
{
	/// <summary>
	/// Removes
	/// <see cref="NBoilerpipePortable.Document.TextBlock">NBoilerpipePortable.Document.TextBlock</see>
	/// s which have explicitly been marked as "not content".
	/// </summary>
	/// <author>Christian Kohlsch√ºtter</author>
	public sealed class BoilerplateBlockFilter : BoilerpipeFilter
	{
		public static readonly BoilerplateBlockFilter INSTANCE = new BoilerplateBlockFilter(null);
        public static readonly BoilerplateBlockFilter INSTANCE_KEEP_TITLE = new BoilerplateBlockFilter(DefaultLabels.TITLE);

        private BoilerplateBlockFilter(string labelToKeep)
        {
            this.labelToKeep = labelToKeep;
        }

        private readonly string labelToKeep;
		/// <exception cref="NBoilerpipePortable.BoilerpipeProcessingException"></exception>
		public bool Process(TextDocument doc)
		{
			IList<TextBlock> textBlocks = doc.GetTextBlocks();
            var removeMe = textBlocks.Where(tb => !tb.IsContent() && (labelToKeep == null || !tb.HasLabel(DefaultLabels.TITLE))).ToList();

            foreach (var tb in removeMe)
			{
                textBlocks.Remove(tb);
			}
            return removeMe.Count > 0;
		}

	}
}
