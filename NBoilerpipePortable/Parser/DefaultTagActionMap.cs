/*
 * This code is derived from boilerpipe
 * 
 */


using NBoilerpipePortable.Labels;
namespace NBoilerpipePortable.Parser
{
	/// <summary>
	/// Default
	/// <see cref="TagAction">TagAction</see>
	/// s. Seem to work well.
	/// </summary>
	/// <seealso cref="TagActionMap">TagActionMap</seealso>
	
	public class DefaultTagActionMap : TagActionMap
	{
		private const long serialVersionUID = 1L;

		public static readonly TagActionMap INSTANCE = new NBoilerpipePortable.Parser.DefaultTagActionMap();

		public DefaultTagActionMap()
		{
			SetTagAction("STYLE", CommonTagActions.TA_IGNORABLE_ELEMENT);
			SetTagAction("SCRIPT", CommonTagActions.TA_IGNORABLE_ELEMENT);
			SetTagAction("OPTION", CommonTagActions.TA_IGNORABLE_ELEMENT);
			SetTagAction("OBJECT", CommonTagActions.TA_IGNORABLE_ELEMENT);
			SetTagAction("EMBED", CommonTagActions.TA_IGNORABLE_ELEMENT);
			SetTagAction("APPLET", CommonTagActions.TA_IGNORABLE_ELEMENT);
			SetTagAction("LINK", CommonTagActions.TA_IGNORABLE_ELEMENT);
			SetTagAction("A", CommonTagActions.TA_ANCHOR_TEXT);
			SetTagAction("BODY", CommonTagActions.TA_BODY);
			SetTagAction("STRIKE", CommonTagActions.TA_INLINE_NO_WHITESPACE);
			SetTagAction("U", CommonTagActions.TA_INLINE_NO_WHITESPACE);
			SetTagAction("B", CommonTagActions.TA_INLINE_NO_WHITESPACE);
			SetTagAction("I", CommonTagActions.TA_INLINE_NO_WHITESPACE);
			SetTagAction("EM", CommonTagActions.TA_INLINE_NO_WHITESPACE);
			SetTagAction("STRONG", CommonTagActions.TA_INLINE_NO_WHITESPACE);
			SetTagAction("SPAN", CommonTagActions.TA_INLINE_NO_WHITESPACE);
			// New in 1.1 (especially to improve extraction quality from Wikipedia etc.)
			SetTagAction("SUP", CommonTagActions.TA_INLINE_NO_WHITESPACE);
			// New in 1.2
			SetTagAction("CODE", CommonTagActions.TA_INLINE_NO_WHITESPACE);
			SetTagAction("TT", CommonTagActions.TA_INLINE_NO_WHITESPACE);
			SetTagAction("SUB", CommonTagActions.TA_INLINE_NO_WHITESPACE);
			SetTagAction("VAR", CommonTagActions.TA_INLINE_NO_WHITESPACE);
			SetTagAction("ABBR", CommonTagActions.TA_INLINE_WHITESPACE);
			SetTagAction("ACRONYM", CommonTagActions.TA_INLINE_WHITESPACE);
			SetTagAction("FONT", CommonTagActions.TA_INLINE_NO_WHITESPACE);
			// could also use TA_FONT 
			// added in 1.1.1
			SetTagAction("NOSCRIPT", CommonTagActions.TA_IGNORABLE_ELEMENT);
            SetTagAction("IMG", CommonTagActions.TA_IMG_ELEMENT);

            SetTagAction("LI", new CommonTagActions.BlockTagLabelAction(new LabelAction(DefaultLabels.LI)));
            SetTagAction("H1", new CommonTagActions.BlockTagLabelAction(new LabelAction(DefaultLabels.H1, DefaultLabels.HEADING)));
            SetTagAction("H2", new CommonTagActions.BlockTagLabelAction(new LabelAction(DefaultLabels.H2, DefaultLabels.HEADING)));
            SetTagAction("H3", new CommonTagActions.BlockTagLabelAction(new LabelAction(DefaultLabels.H3, DefaultLabels.HEADING)));
		}
	}
}
