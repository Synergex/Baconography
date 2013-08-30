using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using NBoilerpipePortable.Parser;
using NBoilerpipePortable.Document;
using NBoilerpipePortable.Labels;
using NBoilerpipePortable.Util;
using Sharpen;
using System.Collections;
using NBoilerpipePortablePortable.Util;
using System.Linq;


namespace NBoilerpipePortable
{

    public class NBoilerpipeContentHandler : IContentHandler
    {
		enum Event
		{
			START_TAG,
			END_TAG,
			CHARACTERS,
			WHITESPACE
		}
		
		readonly IDictionary<string, TagAction> tagActions = DefaultTagActionMap.INSTANCE;
		string title = null;

		internal static readonly string ANCHOR_TEXT_START = "$\ue00a";
		internal static readonly string ANCHOR_TEXT_END = "\ue00a$";
		internal StringBuilder tokenBuilder = new StringBuilder();
		internal StringBuilder textBuilder = new StringBuilder();
		internal int inBody = 0;
		internal int inAnchor = 0;
		internal int inIgnorableElement = 0;
		internal int tagLevel = 0;
		internal int blockTagLevel = -1;
		internal bool sbLastWasWhitespace = false;

		int textElementIdx = 0;
		internal readonly IList<TextBlock> textBlocks = new List<TextBlock>();
		string lastStartTag = null;
		string lastEndTag = null;
		NBoilerpipeContentHandler.Event lastEvent;
		int offsetBlocks = 0;
		BitSet currentContainedTextElements = new BitSet();
		bool flush = false;
		bool inAnchorText = false;
		internal List<List<LabelAction>> labelStacks = new List<List<LabelAction>>();
		internal List<int?> fontSizeStack = new List<int?>();
		
		static readonly Sharpen.Pattern PAT_VALID_WORD_CHARACTER = Sharpen.Pattern
			.Compile ("[\\p{L}\\p{Nd}\\p{Nl}\\p{No}]");


        private bool IsHidden(HtmlAttributeCollection atts)
        {
            if (atts.Contains("class"))
            {
                return atts["class"].Value.Split(' ').Any(str => str == "hidden" || str.Contains("promo") || str.Contains("comment"));
            }
            else
                return false;
        }

        public void StartElement(HtmlNode node)
        {
            labelStacks.AddItem(null);
            TagAction ta = tagActions.Get(node.Name);
            if (ta != null)
            {
                if (ta.ChangesTagLevel())
                {
                    tagLevel++;
                }
                flush = ta.Start(this, node.Name, node.Attributes) | flush;
            }
            else
            {
                tagLevel++;
                flush = true;
            }

            if (IsHidden(node.Attributes))
                inIgnorableElement++;

            lastEvent = NBoilerpipeContentHandler.Event.START_TAG;
            lastStartTag = node.Name;
        }

        public void EndElement(HtmlNode node)
        {
            TagAction ta = tagActions.Get(node.Name);
            if (ta != null)
            {
                flush = ta.End(this, node.Name) | flush;
            }
            else
            {
                flush = true;
            }
            if (ta == null || ta.ChangesTagLevel())
            {
                tagLevel--;
            }
            if (flush)
            {
                FlushBlock();
            }

            if (inIgnorableElement > 0 && IsHidden(node.Attributes))
                inIgnorableElement--;

            lastEvent = NBoilerpipeContentHandler.Event.END_TAG;
            lastEndTag = node.Name;
            labelStacks.RemoveLast();
        }
		
        public void HandleText (HtmlTextNode node)
		{
			if (IsTag (node.Text))
				node.Text = "";
			
			char[] ch = HttpUtility.HtmlDecode (node.Text).ToCharArray ();
			int start = 0;
			int length = ch.Length;
			
			textElementIdx++;
			
			if (flush) {
				FlushBlock ();
				flush = false;
			}
			if (inIgnorableElement != 0) {
				return;
			}
			
			char c;
			bool startWhitespace = false;
			bool endWhitespace = false;
			if (length == 0) {
				return;
			}
			int end = start + length;
			for (int i = start; i < end; i++) {
				if (IsWhiteSpace (ch [i])) {
					ch [i] = ' ';
				}
			}
			while (start < end) {
				c = ch [start];
				if (c == ' ') {
					startWhitespace = true;
					start++;
					length--;
				} else {
					break;
				}
			}
			while (length > 0) {
				c = ch [start + length - 1];
				if (c == ' ') {
					endWhitespace = true;
					length--;
				} else {
					break;
				}
			}
			if (length == 0) {
				if (startWhitespace || endWhitespace) {
					if (!sbLastWasWhitespace) {
						textBuilder.Append (' ');
						tokenBuilder.Append (' ');
					}
					sbLastWasWhitespace = true;
				} else {
					sbLastWasWhitespace = false;
				}
				lastEvent = NBoilerpipeContentHandler.Event.WHITESPACE;
				return;
			}
			if (startWhitespace) {
				if (!sbLastWasWhitespace) {
					textBuilder.Append (' ');
					tokenBuilder.Append (' ');
				}
			}
			if (blockTagLevel == -1) {
				blockTagLevel = tagLevel;
			}
			textBuilder.Append (ch, start, length);
			tokenBuilder.Append (ch, start, length);
			if (endWhitespace) {
				textBuilder.Append (' ');
				tokenBuilder.Append (' ');
			}
			sbLastWasWhitespace = endWhitespace;
			lastEvent = NBoilerpipeContentHandler.Event.CHARACTERS;
			currentContainedTextElements.Add (textElementIdx);
		}
		
		bool IsTag (String text)
		{
			return (Regex.IsMatch (text, "</?[a-z][a-z0-9]*[^<>]*>", RegexOptions.IgnoreCase));
		}
		
		bool IsWhiteSpace (char ch)
		{
			if (ch == '\u00A0') return false;
			return char.IsWhiteSpace (ch);
		}
		
		public void FlushBlock ()
		{
			if (inBody == 0) {
				if (inBody == 0 && string.Compare("TITLE", lastStartTag, StringComparison.CurrentCultureIgnoreCase) == 0) 
					SetTitle (tokenBuilder.ToString ().Trim ());
				textBuilder.Length = 0;
				tokenBuilder.Length = 0;
				return;
			}

			int length = tokenBuilder.Length;
			if (length == 0) {
				return;
			} else if (length == 1) {
				if (sbLastWasWhitespace) {
					textBuilder.Length = 0;
					tokenBuilder.Length = 0;
					return;
				}
			}

			string[] tokens = UnicodeTokenizer.Tokenize (tokenBuilder);
			int numWords = 0;
			int numLinkedWords = 0;
			int numWrappedLines = 0;
			int currentLineLength = -1; // don't count the first space
			int maxLineLength = 80;
			int numTokens = 0;
			int numWordsCurrentLine = 0;

			foreach (string token in tokens) {
				if (token == ANCHOR_TEXT_START) {
					inAnchorText = true;
				} else {
					if (token == ANCHOR_TEXT_END) {
						inAnchorText = false;
					} else {
						if (IsWord (token)) {
							numTokens++;
							numWords++;
							numWordsCurrentLine++;
							
							if (inAnchorText) {
								numLinkedWords++;
							}
							int tokenLength = token.Length;
							currentLineLength += tokenLength + 1;
							if (currentLineLength > maxLineLength) {
								numWrappedLines++;
								currentLineLength = tokenLength;
								numWordsCurrentLine = 1;
							}
						} else {
							numTokens++;
						}
					}
				}
			}
			if (numTokens == 0) {
				return;
			}
			int numWordsInWrappedLines;
			if (numWrappedLines == 0) {
				numWordsInWrappedLines = numWords;
				numWrappedLines = 1;
			} else {
				numWordsInWrappedLines = numWords - numWordsCurrentLine;
			}
			TextBlock tb = new TextBlock (textBuilder.ToString ().Trim (), currentContainedTextElements
				, numWords, numLinkedWords, numWordsInWrappedLines, numWrappedLines, offsetBlocks
				);
			currentContainedTextElements = new BitSet ();
			offsetBlocks++;
			textBuilder.Length = 0;
			tokenBuilder.Length = 0;
			tb.SetTagLevel (blockTagLevel);
			AddTextBlock (tb);
			blockTagLevel = -1;
		}

		static bool IsWord (string token)
		{
			return PAT_VALID_WORD_CHARACTER.Matcher (token).Find ();
		}

        public TextDocument ToTextDocument()
        {
            return new TextDocument(title, textBlocks);
        }

        protected void AddTextBlock (TextBlock tb)
		{
			foreach (int l in fontSizeStack) {
				tb.AddLabels ("font-" + l);
				break;
			}
			
			foreach (List<LabelAction> labels in labelStacks) {
				if (labels != null) {
					foreach (LabelAction label in labels) {
						label.AddTo (tb);
					}
				}
			}
			textBlocks.Add (tb);
		}
		
		
		public void AddWhitespaceIfNecessary ()
		{
			if (!sbLastWasWhitespace) {
				tokenBuilder.Append (' ');
				textBuilder.Append (' ');
				sbLastWasWhitespace = true;
			}
		}
		
		public void AddLabelAction (LabelAction la)
		{
			List<LabelAction> labelStack = labelStacks.Last();
			if (labelStack == null) {
				labelStack = new List<LabelAction> ();
				labelStacks.RemoveLast ();
				labelStacks.AddItem (labelStack);
			}
			labelStack.AddItem (la);
		}
		
		public void SetTitle (string s)
		{
			if (s == null || s.Length == 0) {
				return;
			}
			title = s;
		}


    }
}

namespace Sharpen
{
    /*
     * A BitSet to replace java.util.BitSet.
     * Primary differences are that most set operators return new sets
     * as opposed to oring and anding "in place".  Further, a number of
     * operations were added.  I cannot contain a BitSet because there
     * is no way to access the internal bits (which I need for speed)
     * and, because it is final, I cannot subclass to add functionality.
     * Consider defining set degree.  Without access to the bits, I must
     * call a method n times to test the ith bit...ack!
     *
     * Also seems like or() from util is wrong when size of incoming set is bigger
     * than this.bits.length.
     *
     * @author Terence Parr
     * @author <br><a href="mailto:pete@yamuna.demon.co.uk">Pete Wells</a>
     */

    public class BitSet : ICloneable
    {
        protected internal const int BITS = 64; // number of bits / long
        protected internal const int NIBBLE = 4;
        protected internal const int LOG_BITS = 6; // 2^6 == 64

        /*
         * We will often need to do a mod operator (i mod nbits).  Its
         * turns out that, for powers of two, this mod operation is
         * same as (i & (nbits-1)).  Since mod is slow, we use a
         * precomputed mod mask to do the mod instead.
         */
        protected internal static readonly int MOD_MASK = BITS - 1;

        /* The actual data bits */
        protected internal long[] dataBits;

        /* Construct a bitset of size one word (64 bits) */
        public BitSet()
            : this(BITS)
        {
        }

        /* Construction from a static array of longs */
        public BitSet(long[] bits_)
        {
            dataBits = bits_;
        }

        /*
         * Construct a bitset given the size
         * @param nbits The size of the bitset in bits
         */
        public BitSet(int nbits)
        {
            dataBits = new long[((nbits - 1) >> LOG_BITS) + 1];
        }

        /* OR this element into this set (grow as necessary to accommodate) */
        public virtual void Add(int el)
        {
            int n = wordNumber(el);
            if (n >= dataBits.Length)
            {
                GrowToInclude(el);
            }
            dataBits[n] |= BitMask(el);
        }

        public virtual BitSet And(BitSet a)
        {
            BitSet s = (BitSet)this.Clone();
            s.AndInPlace(a);
            return s;
        }

        public virtual void AndInPlace(BitSet a)
        {
            int min = (int)(Math.Min(dataBits.Length, a.dataBits.Length));
            for (int i = min - 1; i >= 0; i--)
            {
                dataBits[i] &= a.dataBits[i];
            }
            // clear all bits in this not present in a (if this bigger than a).
            for (int i = min; i < dataBits.Length; i++)
            {
                dataBits[i] = 0;
            }
        }

        private static long BitMask(int bitNumber)
        {
            int bitPosition = bitNumber & MOD_MASK; // bitNumber mod BITS
            return 1L << bitPosition;
        }

        public virtual void Clear()
        {
            for (int i = dataBits.Length - 1; i >= 0; i--)
            {
                dataBits[i] = 0;
            }
        }

        public virtual void Clear(int el)
        {
            int n = wordNumber(el);
            if (n >= dataBits.Length)
            {
                // grow as necessary to accommodate
                GrowToInclude(el);
            }
            dataBits[n] &= ~BitMask(el);
        }

        public virtual object Clone()
        {
            BitSet s;
            try
            {
                s = new BitSet();
                s.dataBits = new long[dataBits.Length];
                Array.Copy(dataBits, 0, s.dataBits, 0, dataBits.Length);
            }
            catch //(System.Exception e)
            {
                throw new System.Exception();
            }
            return s;
        }

        public virtual int Degree()
        {
            int deg = 0;
            for (int i = dataBits.Length - 1; i >= 0; i--)
            {
                long word = dataBits[i];
                if (word != 0L)
                {
                    for (int bit = BITS - 1; bit >= 0; bit--)
                    {
                        if ((word & (1L << bit)) != 0)
                        {
                            deg++;
                        }
                    }
                }
            }
            return deg;
        }

        override public int GetHashCode()
        {
            return dataBits.GetHashCode();
        }

        /* Code "inherited" from java.util.BitSet */
        override public bool Equals(object obj)
        {
            if ((obj != null) && (obj is BitSet))
            {
                BitSet bset = (BitSet)obj;

                int n = (int)(System.Math.Min(dataBits.Length, bset.dataBits.Length));
                for (int i = n; i-- > 0; )
                {
                    if (dataBits[i] != bset.dataBits[i])
                    {
                        return false;
                    }
                }
                if (dataBits.Length > n)
                {
                    for (int i = (int)(dataBits.Length); i-- > n; )
                    {
                        if (dataBits[i] != 0)
                        {
                            return false;
                        }
                    }
                }
                else if (bset.dataBits.Length > n)
                {
                    for (int i = (int)(bset.dataBits.Length); i-- > n; )
                    {
                        if (bset.dataBits[i] != 0)
                        {
                            return false;
                        }
                    }
                }
                return true;
            }
            return false;
        }

        /*
         * Grows the set to a larger number of bits.
         * @param bit element that must fit in set
         */
        public virtual void GrowToInclude(int bit)
        {
            int newSize = (int)(System.Math.Max(dataBits.Length << 1, numWordsToHold(bit)));
            long[] newbits = new long[newSize];
            Array.Copy(dataBits, 0, newbits, 0, dataBits.Length);
            dataBits = newbits;
        }

        public virtual bool Member(int el)
        {
            int n = wordNumber(el);
            if (n >= dataBits.Length)
                return false;
            return (dataBits[n] & BitMask(el)) != 0;
        }

        public virtual bool Nil()
        {
            for (int i = dataBits.Length - 1; i >= 0; i--)
            {
                if (dataBits[i] != 0)
                    return false;
            }
            return true;
        }

        public virtual BitSet Not()
        {
            BitSet s = (BitSet)this.Clone();
            s.NotInPlace();
            return s;
        }

        public virtual void NotInPlace()
        {
            for (int i = dataBits.Length - 1; i >= 0; i--)
            {
                dataBits[i] = ~dataBits[i];
            }
        }

        /* Complement bits in the range 0..maxBit. */
        public virtual void NotInPlace(int maxBit)
        {
            NotInPlace(0, maxBit);
        }

        /* Complement bits in the range minBit..maxBit.*/
        public virtual void NotInPlace(int minBit, int maxBit)
        {
            // Make sure that we have room for maxBit
            GrowToInclude(maxBit);
            for (int i = minBit; i <= maxBit; i++)
            {
                int n = wordNumber(i);
                dataBits[n] ^= BitMask(i);
            }
        }

        private int numWordsToHold(int el)
        {
            return (el >> LOG_BITS) + 1;
        }

        public static BitSet of(int el)
        {
            BitSet s = new BitSet(el + 1);
            s.Add(el);
            return s;
        }

        /* Return this | a in a new set. */
        public virtual BitSet Or(BitSet a)
        {
            BitSet s = (BitSet)this.Clone();
            s.OrInPlace(a);
            return s;
        }

        public virtual void OrInPlace(BitSet a)
        {
            // If this is smaller than a, grow this first
            if (a.dataBits.Length > dataBits.Length)
            {
                setSize((int)(a.dataBits.Length));
            }
            int min = (int)(System.Math.Min(dataBits.Length, a.dataBits.Length));
            for (int i = min - 1; i >= 0; i--)
            {
                dataBits[i] |= a.dataBits[i];
            }
        }

        /* Remove this element from this set. */
        public virtual void Remove(int el)
        {
            int n = wordNumber(el);
            if (n >= dataBits.Length)
            {
                GrowToInclude(el);
            }
            dataBits[n] &= ~BitMask(el);
        }

        /*
         * Sets the size of a set.
         * @param nwords how many words the new set should be
         */
        private void setSize(int nwords)
        {
            long[] newbits = new long[nwords];
            int n = (int)(System.Math.Min(nwords, dataBits.Length));
            Array.Copy(dataBits, 0, newbits, 0, n);
            dataBits = newbits;
        }

        public virtual int size()
        {
            return dataBits.Length << LOG_BITS; // num words * bits per word
        }

        /*
         * Return how much space is being used by the dataBits array not
         * how many actually have member bits on.
         */
        public virtual int LengthInLongWords()
        {
            return dataBits.Length;
        }

        /* Is this contained within a? */
        public virtual bool Subset(BitSet a)
        {
            if (a == null) //(a == null || !(a is BitSet))
                return false;
            return this.And(a).Equals(this);
        }

        /*
         * Subtract the elements of 'a' from 'this' in-place.
         * Basically, just turn off all bits of 'this' that are in 'a'.
         */
        public virtual void SubtractInPlace(BitSet a)
        {
            if (a == null)
                return;
            // for all words of 'a', turn off corresponding bits of 'this'
            for (int i = 0; i < dataBits.Length && i < a.dataBits.Length; i++)
            {
                dataBits[i] &= ~a.dataBits[i];
            }
        }

        public virtual int[] ToArray()
        {
            int[] elems = new int[Degree()];
            int en = 0;
            for (int i = 0; i < (dataBits.Length << LOG_BITS); i++)
            {
                if (Member(i))
                {
                    elems[en++] = i;
                }
            }
            return elems;
        }

        public virtual long[] ToPackedArray()
        {
            return dataBits;
        }

        override public string ToString()
        {
            return ToString(",");
        }

        /*
         * Transform a bit set into a string by formatting each element as an integer
         * @separator The string to put in between elements
         * @return A commma-separated list of values
         */
        public virtual string ToString(string separator)
        {
            string str = "";
            for (int i = 0; i < (dataBits.Length << LOG_BITS); i++)
            {
                if (Member(i))
                {
                    if (str.Length > 0)
                    {
                        str += separator;
                    }
                    str = str + i;
                }
            }
            return str;
        }

        /*
         * Create a string representation where instead of integer elements, the
         * ith element of vocabulary is displayed instead.  Vocabulary is a Vector
         * of Strings.
         * @separator The string to put in between elements
         * @return A commma-separated list of character constants.
         */
        public virtual string ToString(string separator, IList<object> vocabulary)
        {
            if (vocabulary == null)
            {
                return ToString(separator);
            }
            string str = "";
            for (int i = 0; i < (dataBits.Length << LOG_BITS); i++)
            {
                if (Member(i))
                {
                    if (str.Length > 0)
                    {
                        str += separator;
                    }
                    if (i >= vocabulary.Count)
                    {
                        str += "<bad element " + i + ">";
                    }
                    else if (vocabulary[i] == null)
                    {
                        str += "<" + i + ">";
                    }
                    else
                    {
                        str += (string)vocabulary[i];
                    }
                }
            }
            return str;
        }

        /*
         * Dump a comma-separated list of the words making up the bit set.
         * Split each 64 bit number into two more manageable 32 bit numbers.
         * This generates a comma-separated list of C++-like unsigned long constants.
         */
        public virtual string ToStringOfHalfWords()
        {
            string s = new string("".ToCharArray());
            for (int i = 0; i < dataBits.Length; i++)
            {
                if (i != 0)
                    s += ", ";
                long tmp = dataBits[i];
                tmp &= 0xFFFFFFFFL;
                s += (tmp + "UL");
                s += ", ";
                tmp = (uint)dataBits[i] >> 32;
                tmp &= 0xFFFFFFFFL;
                s += (tmp + "UL");
            }
            return s;
        }

        /*
         * Dump a comma-separated list of the words making up the bit set.
         * This generates a comma-separated list of Java-like long int constants.
         */
        public virtual string ToStringOfWords()
        {
            string s = new string("".ToCharArray());
            for (int i = 0; i < dataBits.Length; i++)
            {
                if (i != 0)
                    s += ", ";
                s += (dataBits[i] + "L");
            }
            return s;
        }

        private static int wordNumber(int bit)
        {
            return bit >> LOG_BITS; // bit / BITS
        }
    }
}
