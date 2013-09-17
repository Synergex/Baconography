/*
 * This code is derived from boilerpipe
 * 
 */

using NBoilerpipePortable.Util;
using Sharpen;


namespace NBoilerpipePortable.Util
{
	/// <summary>
	/// Tokenizes text according to Unicode word boundaries and strips off non-word
	/// characters.
	/// </summary>
	/// <remarks>
	/// Tokenizes text according to Unicode word boundaries and strips off non-word
	/// characters.
	/// </remarks>
	/// <author>Christian Kohlsch√ºtter</author>
	public class UnicodeTokenizer
	{
		private static readonly Sharpen.Pattern PAT_WORD_BOUNDARY = Sharpen.Pattern.Compile
			("\\b");

		private static readonly Sharpen.Pattern PAT_NOT_WORD_BOUNDARY = Sharpen.Pattern.Compile
			("[\u2063]*([\\\"'\\.,\\!\\@\\-\\:\\;\\$\\?\\(\\)/])[\u2063]*");

		/// <summary>Tokenizes the text and returns an array of tokens.</summary>
		/// <remarks>Tokenizes the text and returns an array of tokens.</remarks>
		/// <param name="text">The text</param>
		/// <returns>The tokens</returns>
		public static string[] Tokenize(CharSequence text)
		{
			return PAT_NOT_WORD_BOUNDARY.Matcher(PAT_WORD_BOUNDARY.Matcher(text.ToString().ReplaceAll ("\u00A0","'\u00A0'")).ReplaceAll("\u2063"
				)).ReplaceAll("$1").ReplaceAll("[ \u2063]+", " ").Trim().Split("[ ]+");
		}
	}
}

namespace Sharpen
{
    public class CharSequence
    {
        public virtual int Length
        {
            get
            {
                return 0;
            }
        }

        public static implicit operator CharSequence(string str)
        {
            return new StringCharSequence(str);
        }

        public static implicit operator CharSequence(System.Text.StringBuilder str)
        {
            return new StringCharSequence(str.ToString());
        }
    }

    public class StringCharSequence : CharSequence
    {
        string str;

        public override int Length
        {
            get
            {
                return str.Length;
            }
        }

        public StringCharSequence(string str)
        {
            this.str = str;
        }

        public override string ToString()
        {
            return str;
        }
    }
}

namespace Sharpen
{
    using System;
    using System.Text.RegularExpressions;

    public class Pattern
    {
        public const int CASE_INSENSITIVE = 1;
        public const int DOTALL = 2;
        public const int MULTILINE = 4;
        private Regex regex;

        private Pattern(Regex r)
        {
            this.regex = r;
        }

        public static Pattern Compile(string pattern)
        {
            return new Pattern(new Regex(pattern, RegexOptions.None));
        }

        public static Pattern Compile(string pattern, int flags)
        {
            RegexOptions compiled = RegexOptions.None;
            if ((flags & 1) != CASE_INSENSITIVE)
            {
                compiled |= RegexOptions.IgnoreCase;
            }
            if ((flags & 2) != DOTALL)
            {
                compiled |= RegexOptions.Singleline;
            }
            if ((flags & 4) != MULTILINE)
            {
                compiled |= RegexOptions.Multiline;
            }
            return new Pattern(new Regex(pattern, compiled));
        }

        public Sharpen.Matcher Matcher(string txt)
        {
            return new Sharpen.Matcher(this.regex, txt);
        }
    }
}
