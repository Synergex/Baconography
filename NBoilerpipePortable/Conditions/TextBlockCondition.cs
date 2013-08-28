/*
 * This code is derived from boilerpipe
 * 
 */

using NBoilerpipePortable.Conditions;
using NBoilerpipePortable.Document;


namespace NBoilerpipePortable.Conditions
{
	/// <summary>
	/// Evaluates whether a given
	/// <see cref="NBoilerpipePortable.Document.TextBlock">NBoilerpipePortable.Document.TextBlock</see>
	/// meets a certain condition.
	/// Useful in combination with
	/// <see cref="NBoilerpipePortable.Labels.ConditionalLabelAction">NBoilerpipePortable.Labels.ConditionalLabelAction
	/// 	</see>
	/// .
	/// </summary>
	/// <author>Christian Kohlschuetter</author>
	public interface TextBlockCondition
	{
		/// <summary>
		/// Returns <code>true</code> iff the given
		/// <see cref="NBoilerpipePortable.Document.TextBlock">NBoilerpipePortable.Document.TextBlock</see>
		/// tb meets the defined condition.
		/// </summary>
		/// <param name="tb"></param>
		/// <returns><code><true&lt;/code> iff the condition is met.</returns>
		bool MeetsCondition(TextBlock tb);
	}
}
