/*
 * This code is derived from boilerpipe
 * 
 */

using NBoilerpipePortable;
using NBoilerpipePortable.Document;


namespace NBoilerpipePortable
{
	/// <summary>
	/// Something that can be represented as a
	/// <see cref="NBoilerpipePortable.Document.TextDocument">NBoilerpipePortable.Document.TextDocument</see>
	/// .
	/// </summary>
	public interface BoilerpipeDocumentSource
	{
		/// <exception cref="NBoilerpipePortable.BoilerpipeProcessingException"></exception>
		TextDocument ToTextDocument();
	}
}
