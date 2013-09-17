/*
 * This code is derived from boilerpipe
 * 
 */

using NBoilerpipePortable.Document;


namespace NBoilerpipePortable.Labels
{
	/// <summary>
	/// Helps adding labels to
	/// <see cref="NBoilerpipePortable.Document.TextBlock">NBoilerpipePortable.Document.TextBlock</see>
	/// s.
	/// </summary>
	/// <author>Christian Kohlsch√ºtter</author>
	/// <seealso cref="ConditionalLabelAction">ConditionalLabelAction</seealso>
	public class LabelAction
	{
		protected internal readonly string[] labels;

		public LabelAction(params string[] labels)
		{
			this.labels = labels;
		}

		public virtual void AddTo(TextBlock tb)
		{
			AddLabelsTo(tb);
		}

		protected internal void AddLabelsTo(TextBlock tb)
		{
			tb.AddLabels(labels);
		}

		public override string ToString()
		{
			return base.ToString() + "{" + labels + "}";
		}
	}
}
