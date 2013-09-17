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
    class TrailingHeadlineToBoilerplateFilter : BoilerpipeFilter 
    {
	    public static readonly TrailingHeadlineToBoilerplateFilter INSTANCE = new TrailingHeadlineToBoilerplateFilter();
	
	    public bool Process(TextDocument doc)
        {
	    	var changes = false;
	
            foreach (var tb in doc.GetTextBlocks().Reverse())
            {
                if (tb.IsContent())
                {
                    if (tb.HasLabel(DefaultLabels.HEADING))
                    {
                        tb.SetIsContent(false);
                        changes = true;
                    }
                    else
                    {
                        break;
                    }
                }
            }
	        return changes;
	    }
	
	}
}
