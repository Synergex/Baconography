/*
 * This code is derived from boilerpipe
 * 
 */

using NBoilerpipePortable.Document;
using NBoilerpipePortable.Labels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBoilerpipePortable.Filters.Heuristics
{

    /**
     * Marks all blocks as content that:
     * <ol>
     * <li>are on the same tag-level as very likely main content (usually the level of the largest block)</li>
     * <li>have a significant number of words, currently: at least 100</li>
     * </ol>
     *
     * @author Christian Kohlschütter
     */
    class LargeBlockSameTagLevelToContentFilter : BoilerpipeFilter
    {
        public static readonly NBoilerpipePortable.Filters.Heuristics.LargeBlockSameTagLevelToContentFilter INSTANCE = new
            NBoilerpipePortable.Filters.Heuristics.LargeBlockSameTagLevelToContentFilter();

        public bool Process(TextDocument doc)
        {
            var changes = false;

            int tagLevel = -1;
            foreach (var tb in doc.GetTextBlocks())
            {
                if (tb.IsContent() && tb.HasLabel(DefaultLabels.VERY_LIKELY_CONTENT))
                {
                    tagLevel = tb.GetTagLevel();
                    break;
                }

                if (tagLevel == -1)
                {
                    return false;
                }
            }

            foreach (var tb in doc.GetTextBlocks())
            {
                if (!tb.IsContent())
                {

                    if (tb.GetNumWords() >= 100 && tb.GetTagLevel() == tagLevel)
                    {
                        tb.SetIsContent(true);
                        changes = true;
                    }
                }
            }
            return changes;
        }
    }
}
