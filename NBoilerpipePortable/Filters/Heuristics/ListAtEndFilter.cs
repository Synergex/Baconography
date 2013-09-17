/*
 * This code is derived from boilerpipe
 * 
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NBoilerpipePortable.Document;
using NBoilerpipePortable.Labels;

namespace NBoilerpipePortable.Filters.Heuristics
{

    /**
	 * Marks nested list-item blocks after the end of the main content.
	 *
	 * @author Christian Kohlschütter
	 */
    class ListAtEndFilter : BoilerpipeFilter 
    {
		public static readonly ListAtEndFilter INSTANCE = new ListAtEndFilter();
	
		private ListAtEndFilter() {
		}
	
		public bool Process(TextDocument doc)
        {
			var changes = false;
	
			int tagLevel = Int32.MaxValue;
			foreach (var tb in doc.GetTextBlocks()) 
            {
				if (tb.IsContent() && tb.HasLabel(DefaultLabels.VERY_LIKELY_CONTENT)) 
                {
					tagLevel = tb.GetTagLevel();
				} 
                else 
                {
					if (tb.GetTagLevel() > tagLevel
						&& tb.HasLabel(DefaultLabels.MIGHT_BE_CONTENT)
						&& tb.HasLabel(DefaultLabels.LI)
						&& tb.GetLinkDensity() == 0) 
                    {
						tb.SetIsContent(true);
						changes = true;
					} 
                    else 
                    {
                        tagLevel = Int32.MaxValue;
					}
				}
			}
	
			return changes;
	
		}
	}
}
