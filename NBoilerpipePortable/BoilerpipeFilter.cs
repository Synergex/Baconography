/*
 * This code is derived from boilerpipe
 * 
 */

using NBoilerpipePortable;
using NBoilerpipePortable.Document;


namespace NBoilerpipePortable
{
	/// <summary>
	/// A generic
	/// <see cref="BoilerpipeFilter">BoilerpipeFilter</see>
	/// . Takes a
	/// <see cref="NBoilerpipePortable.Document.TextDocument">NBoilerpipePortable.Document.TextDocument</see>
	/// and
	/// processes it somehow.
	/// </summary>
	/// <author>Christian Kohlsch√ºtter</author>
	public interface BoilerpipeFilter
	{
		/// <summary>Processes the given document <code>doc</code>.</summary>
		/// <remarks>Processes the given document <code>doc</code>.</remarks>
		/// <param name="doc">
		/// The
		/// <see cref="NBoilerpipePortable.Document.TextDocument">NBoilerpipePortable.Document.TextDocument</see>
		/// that is to be processed.
		/// </param>
		/// <returns>
		/// <code>true</code> if changes have been made to the
		/// <see cref="NBoilerpipePortable.Document.TextDocument">NBoilerpipePortable.Document.TextDocument</see>
		/// .
		/// </returns>
		/// <exception cref="BoilerpipeProcessingException">BoilerpipeProcessingException</exception>
		/// <exception cref="NBoilerpipePortable.BoilerpipeProcessingException"></exception>
		bool Process(TextDocument doc);
	}
}
