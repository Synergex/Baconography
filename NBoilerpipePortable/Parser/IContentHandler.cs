using System;
using HtmlAgilityPack;

namespace NBoilerpipePortable.Parser
{
    public interface IContentHandler
    {
        void StartElement(HtmlNode node);
		void EndElement(HtmlNode node);
        void HandleText(HtmlTextNode node);
    }
}
