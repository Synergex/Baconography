/*
 * This code is derived from boilerpipe
 * 
 */

using System;


namespace NBoilerpipePortable
{
	/// <summary>Exception for signaling failure in the processing pipeline.</summary>
	/// <remarks>Exception for signaling failure in the processing pipeline.</remarks>
	/// <author>Christian Kohlsch√ºtter</author>
	
	public class BoilerpipeProcessingException : Exception
	{
		private const long serialVersionUID = 1L;

		public BoilerpipeProcessingException() : base()
		{
		}

		public BoilerpipeProcessingException(string message, Exception cause) : base(message
			, cause)
		{
		}

		public BoilerpipeProcessingException(string message) : base(message)
		{
		}
	}
}
