/*
 * This code is derived from boilerpipe
 * 
 */

using NBoilerpipePortable.Labels;


namespace NBoilerpipePortable.Labels
{
	/// <summary>
	/// Some pre-defined labels which can be used in conjunction with
	/// <see cref="NBoilerpipePortable.Document.TextBlock.AddLabel(string)">NBoilerpipePortable.Document.TextBlock.AddLabel(string)
	/// 	</see>
	/// and
	/// <see cref="NBoilerpipePortable.Document.TextBlock.HasLabel(string)">NBoilerpipePortable.Document.TextBlock.HasLabel(string)
	/// 	</see>
	/// .
	/// </summary>
	/// <author>Christian Kohlsch√ºtter</author>
	public sealed class DefaultLabels
	{
		public static readonly string TITLE = "de.l3s.boilerpipe/TITLE";
		public static readonly string ARTICLE_METADATA = "de.l3s.boilerpipe/ARTICLE_METADATA";
		public static readonly string INDICATES_END_OF_TEXT = "de.l3s.boilerpipe/INDICATES_END_OF_TEXT";
		public static readonly string MIGHT_BE_CONTENT = "de.l3s.boilerpipe/MIGHT_BE_CONTENT";
		public static readonly string STRICTLY_NOT_CONTENT = "de.l3s.boilerpipe/STRICTLY_NOT_CONTENT";
		public static readonly string HR = "de.l3s.boilerpipe/HR";
		public static readonly string MARKUP_PREFIX = "<";
        public static readonly string VERY_LIKELY_CONTENT = "de.l3s.boilerpipe/VERY_LIKELY_CONTENT";
	    public static readonly string LI = "de.l3s.boilerpipe/LI";
	    public static readonly string HEADING = "de.l3s.boilerpipe/HEADING";
	    public static readonly string H1 = "de.l3s.boilerpipe/H1";
	    public static readonly string H2 = "de.l3s.boilerpipe/H2";
	    public static readonly string H3 = "de.l3s.boilerpipe/H3";
    }
}
