/*
 * This code is derived from boilerpipe
 * 
 */

using System.Collections.Generic;
using NBoilerpipePortable;
using NBoilerpipePortable.Conditions;
using NBoilerpipePortable.Document;
using System.Linq;


namespace NBoilerpipePortable.Filters.Simple
{
	public class SurroundingToContentFilter : BoilerpipeFilter
	{
		private sealed class _TextBlockCondition_13 : TextBlockCondition
		{
			public _TextBlockCondition_13()
			{
			}

			public bool MeetsCondition(TextBlock tb)
			{
				return tb.GetLinkDensity() == 0 && tb.GetNumWords() > 6;
			}
		}

		public static readonly NBoilerpipePortable.Filters.Simple.SurroundingToContentFilter INSTANCE_TEXT
			 = new NBoilerpipePortable.Filters.Simple.SurroundingToContentFilter(new _TextBlockCondition_13
			());

		private readonly TextBlockCondition cond;

		public SurroundingToContentFilter(TextBlockCondition cond)
		{
			this.cond = cond;
		}

		/// <exception cref="NBoilerpipePortable.BoilerpipeProcessingException"></exception>
		public virtual bool Process(TextDocument doc)
		{
			IList<TextBlock> tbs = doc.GetTextBlocks();
			if (tbs.Count < 3)
			{
				return false;
			}
			TextBlock a = tbs[0];
			TextBlock b = tbs[1];
			TextBlock c;
			bool hasChanges = false;
            var it = tbs.Skip(2).GetEnumerator();
            it.MoveNext();
            for(;;)
            {
                c = it.Current;
                if (!b.IsContent() && a.IsContent() && c.IsContent() && cond.MeetsCondition(b))
                {
                    b.SetIsContent(true);
                    hasChanges = true;
                }
                a = c;
                if (!it.MoveNext())
                {
                    break;
                }
                b = it.Current;
            }
			return hasChanges;
		}
	}
}
