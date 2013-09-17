/*
 * This code is derived from boilerpipe
 * 
 */

using System.Collections.Generic;
using NBoilerpipePortable;
using NBoilerpipePortable.Document;
using NBoilerpipePortable.Labels;
using Sharpen;
using System.Text.RegularExpressions;


namespace NBoilerpipePortable.Filters.Heuristics
{
    /// <summary>
    /// Marks
    /// <see cref="NBoilerpipePortable.Document.TextBlock">NBoilerpipePortable.Document.TextBlock</see>
    /// s which contain parts of the HTML
    /// <code>&lt;TITLE&gt;</code> tag, using some heuristics which are quite
    /// specific to the news domain.
    /// </summary>
    /// <author>Christian Kohlsch√ºtter</author>
    public sealed class DocumentTitleMatchClassifier : BoilerpipeFilter
    {
        private readonly ICollection<string> potentialTitles;

        public DocumentTitleMatchClassifier(string title)
        {
            if (title == null)
            {
                this.potentialTitles = null;
            }
            else
            {
                title = title.Replace('\u00a0', ' ');
			    title = title.Replace("'", "");
                title = title.Trim().ToLower();
                if (title.Length == 0)
                {
                    this.potentialTitles = null;
                }
                else
                {
                    this.potentialTitles = new HashSet<string>();
                    potentialTitles.Add(title);
                    string p;
                    p = GetLongestPart(title, "[ ]*[\\|»|-][ ]*");
                    if (p != null)
                    {
                        potentialTitles.Add(p);
                    }
                    p = GetLongestPart(title, "[ ]*[\\|»|:][ ]*");
                    if (p != null)
                    {
                        potentialTitles.Add(p);
                    }
                    p = GetLongestPart(title, "[ ]*[\\|»|:\\(\\)][ ]*");
                    if (p != null)
                    {
                        potentialTitles.Add(p);
                    }
                    p = GetLongestPart(title, "[ ]*[\\|»|:\\(\\)\\-][ ]*");
                    if (p != null)
                    {
                        potentialTitles.Add(p);
                    }
                    p = GetLongestPart(title, "[ ]*[\\|»|,|:\\(\\)\\-][ ]*");
                    if (p != null)
                    {
                        potentialTitles.Add(p);
                    }

                    AddPotentialTitles(potentialTitles as HashSet<string>, title, "[ ]+[\\|][ ]+", 4);
                    AddPotentialTitles(potentialTitles as HashSet<string>, title, "[ ]+[\\-][ ]+", 4);

                    potentialTitles.Add(new Regex(" - [^\\-]+$").Replace(title, "", 1));
                    potentialTitles.Add(new Regex("^[^\\-]+ - ").Replace(title, "", 1));
                }
            }
        }

        public ICollection<string> GetPotentialTitles()
        {
            return potentialTitles;
        }

        private void AddPotentialTitles(HashSet<string> potentialTitles, string title, string pattern, int minWords)
        {
            var parts = title.Split(pattern);
            if (parts.Length == 1)
            {
                return;
            }
            for (int i = 0; i < parts.Length; i++)
            {
                var p = parts[i];
                if (p.Contains(".com"))
                {
                    continue;
                }
                int numWords = p.Split("[\b ]+").Length;
                if (numWords >= minWords)
                {
                    potentialTitles.Add(p);
                }
            }
        }

        private string GetLongestPart(string title, string pattern)
        {
            string[] parts = title.Split(pattern);
            if (parts.Length == 1)
            {
                return null;
            }
            int longestNumWords = 0;
            string longestPart = string.Empty;
            for (int i = 0; i < parts.Length; i++)
            {
                string p = parts[i];
                if (p.Contains(".com"))
                {
                    continue;
                }
                int numWords = p.Split("[\b ]+").Length;
                if (numWords > longestNumWords || p.Length > longestPart.Length)
                {
                    longestNumWords = numWords;
                    longestPart = p;
                }
            }
            if (longestPart.Length == 0)
            {
                return null;
            }
            else
            {
                return longestPart.Trim();
            }
        }

        /// <exception cref="NBoilerpipePortable.BoilerpipeProcessingException"></exception>
        public bool Process(TextDocument doc)
        {
            if (potentialTitles == null)
            {
                return false;
            }
            bool changes = false;
            foreach (TextBlock tb in doc.GetTextBlocks())
            {
                string text = tb.GetText();
                text = text.Replace('\u00a0', ' ');
                text = text.Replace("'", "");
                text = text.Trim().ToLower();
                foreach (string candidate in potentialTitles)
                {
                    if (candidate.Equals(text))
                    {
                        tb.AddLabel(DefaultLabels.TITLE);
                        changes = true;
                    }
                }
            }
            return changes;
        }
    }
}
