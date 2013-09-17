/*
 * This code is derived from boilerpipe
 * 
 */

using NBoilerpipePortable;
using NBoilerpipePortable.Document;


namespace NBoilerpipePortable
{
	/// <summary>
	/// A source that returns
	/// <see cref="NBoilerpipePortable.Document.TextDocument">NBoilerpipePortable.Document.TextDocument</see>
	/// s.
	/// </summary>
	/// <author>Christian Kohlsch√ºtter</author>
	public interface BoilerpipeInput
	{
		/// <summary>
		/// Returns (somehow) a
		/// <see cref="NBoilerpipePortable.Document.TextDocument">NBoilerpipePortable.Document.TextDocument</see>
		/// .
		/// </summary>
		/// <returns>
		/// A
		/// <see cref="NBoilerpipePortable.Document.TextDocument">NBoilerpipePortable.Document.TextDocument</see>
		/// .
		/// </returns>
		/// <exception cref="BoilerpipeProcessingException">BoilerpipeProcessingException</exception>
		/// <exception cref="NBoilerpipePortable.BoilerpipeProcessingException"></exception>
		TextDocument GetTextDocument();
	}
}
