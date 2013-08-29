/*
 * This code is derived from boilerpipe
 * 
 */

using System;
using System.IO;
using NBoilerpipePortable;
using NBoilerpipePortable.Document;
using NBoilerpipePortable.Extractors;
using NBoilerpipePortable.Parser;
using System.Collections.Generic;


namespace NBoilerpipePortable.Extractors
{
	/// <summary>The base class of Extractors.</summary>
	/// <remarks>
	/// The base class of Extractors. Also provides some helper methods to quickly
	/// retrieve the text that remained after processing.
	/// </remarks>
	/// <author>Christian Kohlsch√ºtter</author>
	public abstract class ExtractorBase : BoilerpipeExtractor
	{
		/// <summary>Extracts text from the HTML code given as a String.</summary>
		/// <remarks>Extracts text from the HTML code given as a String.</remarks>
		/// <param name="html">The HTML code as a String.</param>
		/// <returns>The extracted text.</returns>
		/// <exception cref="NBoilerpipePortable.BoilerpipeProcessingException">NBoilerpipePortable.BoilerpipeProcessingException
		/// 	</exception>
		public virtual string GetText (string html)
		{
			try {

				NBoilerpipeHtmlParser parser = new NBoilerpipeHtmlParser (new NBoilerpipeContentHandler ());
				parser.Parse (html);
				return GetText (parser.ToTextDocument ());
			} catch (Exception e) {
				throw new BoilerpipeProcessingException (e.ToString());
			}
		}

		/// <summary>
		/// Extracts text from the given
		/// <see cref="NBoilerpipePortable.Document.TextDocument">NBoilerpipePortable.Document.TextDocument</see>
		/// object.
		/// </summary>
		/// <param name="doc">
		/// The
		/// <see cref="NBoilerpipePortable.Document.TextDocument">NBoilerpipePortable.Document.TextDocument</see>
		/// .
		/// </param>
		/// <returns>The extracted text.</returns>
		/// <exception cref="NBoilerpipePortable.BoilerpipeProcessingException">NBoilerpipePortable.BoilerpipeProcessingException
		/// 	</exception>
		public virtual string GetText(TextDocument doc)
		{
			Process(doc);
			return doc.GetContent();
		}

		public abstract bool Process(TextDocument arg1);

        public IEnumerable<Tuple<string, string>> GetTextAndImageBlocks(string html, Uri uri, out string title)
        {
            NBoilerpipeHtmlParser parser = new NBoilerpipeHtmlParser(new NBoilerpipeContentHandler());
            parser.Parse(html);
            var doc = parser.ToTextDocument();
            this.Process(doc);
            List<Tuple<string, string>> result = new List<Tuple<string, string>>();
            title = doc.GetTitle();
            foreach (var textblock in doc.GetTextBlocks())
            {
                var nearbyImage = textblock.NearbyImage;
                if (!string.IsNullOrEmpty(nearbyImage) && uri != null)
                {
                    try
                    {
                        Uri imageUri = new Uri(textblock.NearbyImage);
                        if (!imageUri.IsAbsoluteUri)
                        {
                            nearbyImage = new Uri(uri, nearbyImage).ToString();
                        }
                    }
                    catch
                    {
                        try
                        {
                            nearbyImage = new Uri(uri, nearbyImage).ToString();
                        }
                        catch
                        {
                            nearbyImage = null;
                        }
                    }
                }
                result.Add(Tuple.Create(textblock.GetText(), nearbyImage));
            }
            return result;
        }
	}
}
