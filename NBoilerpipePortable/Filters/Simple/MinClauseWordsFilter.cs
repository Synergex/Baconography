/*
 * This code is derived from boilerpipe
 * 
 */

using NBoilerpipePortable;
using NBoilerpipePortable.Document;
using Sharpen;


namespace NBoilerpipePortable.Filters.Simple
{
	/// <summary>
	/// Keeps only blocks that have at least one segment fragment ("clause") with at
	/// least <em>k</em> words (default: 5).
	/// </summary>
	/// <remarks>
	/// Keeps only blocks that have at least one segment fragment ("clause") with at
	/// least <em>k</em> words (default: 5).
	/// NOTE: You might consider using the
	/// <see cref="SplitParagraphBlocksFilter">SplitParagraphBlocksFilter</see>
	/// upstream.
	/// </remarks>
	/// <author>Christian Kohlsch√ºtter</author>
	/// <seealso cref="SplitParagraphBlocksFilter">SplitParagraphBlocksFilter</seealso>
	public sealed class MinClauseWordsFilter : BoilerpipeFilter
	{
		public static readonly NBoilerpipePortable.Filters.Simple.MinClauseWordsFilter INSTANCE = 
			new NBoilerpipePortable.Filters.Simple.MinClauseWordsFilter(5, false);

		private int minWords;

		private readonly bool acceptClausesWithoutDelimiter;

		public MinClauseWordsFilter(int minWords) : this(minWords, false)
		{
		}

		public MinClauseWordsFilter(int minWords, bool acceptClausesWithoutDelimiter)
		{
			this.minWords = minWords;
			this.acceptClausesWithoutDelimiter = acceptClausesWithoutDelimiter;
		}

		private readonly Sharpen.Pattern PAT_CLAUSE_DELIMITER = Sharpen.Pattern.Compile("[\\p{L}\\d][\\,\\.\\:\\;\\!\\?]+([ \\n\\r]+|$)"
			);

		private readonly Sharpen.Pattern PAT_WHITESPACE = Sharpen.Pattern.Compile("[ \\n\\r]+"
			);

		/// <exception cref="NBoilerpipePortable.BoilerpipeProcessingException"></exception>
		public bool Process(TextDocument doc)
		{
			bool changes = false;
			foreach (TextBlock tb in doc.GetTextBlocks())
			{
				if (!tb.IsContent())
				{
					continue;
				}
				string text = tb.GetText();
				Matcher m = PAT_CLAUSE_DELIMITER.Matcher(text);
				bool found = m.Find();
				int start = 0;
				int end;
				bool hasClause = false;
				while (found)
				{
					end = m.Start() + 1;
					hasClause = IsClause(text.SubSequence(start, end));
					start = m.End();
					if (hasClause)
					{
						break;
					}
					found = m.Find();
				}
				end = text.Length;
				// since clauses should *always end* with a delimiter, we normally
				// don't consider text without one
				if (acceptClausesWithoutDelimiter)
				{
					hasClause |= IsClause(text.SubSequence(start, end));
				}
				if (!hasClause)
				{
					tb.SetIsContent(false);
					changes = true;
				}
			}
			// System.err.println("IS NOT CONTENT: " + text);
			return changes;
		}

		private bool IsClause(CharSequence text)
		{
			var m = PAT_WHITESPACE.Matcher(text.ToString());
			int n = 1;
			while (m.Find())
			{
				n++;
				if (n >= minWords)
				{
					return true;
				}
			}
			return n >= minWords;
		}
	}
}

namespace Sharpen
{
    using System;
    using System.Text.RegularExpressions;

    public class Matcher
    {
        private int current;
        private MatchCollection matches;
        private Regex regex;
        private string str;

        internal Matcher(Regex regex, string str)
        {
            this.regex = regex;
            this.str = str;
        }

        public int End()
        {
            if ((matches == null) || (current >= matches.Count))
            {
                throw new InvalidOperationException();
            }
            return (matches[current].Index + matches[current].Length);
        }

        public bool Find()
        {
            if (matches == null)
            {
                matches = regex.Matches(str);
                current = 0;
            }
            return (current < matches.Count);
        }

        public bool Find(int index)
        {
            matches = regex.Matches(str, index);
            current = 0;
            return (matches.Count > 0);
        }

        public string Group(int n)
        {
            if ((matches == null) || (current >= matches.Count))
            {
                throw new InvalidOperationException();
            }
            Group grp = matches[current].Groups[n];
            return grp.Success ? grp.Value : null;
        }

        public bool Matches()
        {
            matches = null;
            return Find();
        }

        public string ReplaceFirst(string txt)
        {
            return regex.Replace(str, txt, 1);
        }

        public string ReplaceAll(string txt)
        {
            return regex.Replace(str, txt, int.MaxValue);
        }

        public Matcher Reset(CharSequence str)
        {
            return Reset(str.ToString());
        }

        public Matcher Reset(string str)
        {
            matches = null;
            this.str = str;
            return this;
        }

        public int Start()
        {
            if ((matches == null) || (current >= matches.Count))
            {
                throw new InvalidOperationException();
            }
            return matches[current].Index;
        }
    }
}
