/*
 * 
 * An XmlReader implementation for loading SGML (including HTML) converting it
 * to well formed XML, by adding missing quotes, empty attribute values, ignoring
 * duplicate attributes, case folding on tag names, adding missing closing tags
 * based on SGML DTD information, and so on.
 *
 * Copyright (c) 2002 Microsoft Corporation. All rights reserved. (Chris Lovett)
 *
 */

/*
 * 
 * Copyright (c) 2007-2013 MindTouch. All rights reserved.
 * www.mindtouch.com  oss@mindtouch.com
 *
 * For community documentation and downloads visit wiki.developer.mindtouch.com;
 * please review the licensing section.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 * 
 *     http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 *
 */

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using System.Xml;

namespace Sgml
{
    /// <summary>
    /// SGML is case insensitive, so here you can choose between converting
    /// to lower case or upper case tags.  "None" means that the case is left
    /// alone, except that end tags will be folded to match the start tags.
    /// </summary>
    public enum CaseFolding
    {
        /// <summary>
        /// Do not convert case, except for converting end tags to match start tags.
        /// </summary>
        None,

        /// <summary>
        /// Convert tags to upper case.
        /// </summary>
        ToUpper,

        /// <summary>
        /// Convert tags to lower case.
        /// </summary>
        ToLower
    }

    /// <summary>
    /// This stack maintains a high water mark for allocated objects so the client
    /// can reuse the objects in the stack to reduce memory allocations, this is
    /// used to maintain current state of the parser for element stack, and attributes
    /// in each element.
    /// </summary>
    internal class HWStack
    {
        private object[] m_items;
        private int m_size;
        private int m_count;
        private int m_growth;

        /// <summary>
        /// Initialises a new instance of the HWStack class.
        /// </summary>
        /// <param name="growth">The amount to grow the stack space by, if more space is needed on the stack.</param>
        public HWStack(int growth)
        {
            this.m_growth = growth;
        }

        /// <summary>
        /// The number of items currently in the stack.
        /// </summary>
        public int Count
        {
            get
            {
                return this.m_count;
            }
            set
            {
                this.m_count = value;
            }
        }

        /// <summary>
        /// The size (capacity) of the stack.
        /// </summary>
        [SuppressMessage("Microsoft.Performance", "CA1811", Justification = "Kept for potential future usage.")]
        public int Size
        {
            get
            {
                return this.m_size;
            }
        }

        /// <summary>
        /// Returns the item at the requested index or null if index is out of bounds
        /// </summary>
        /// <param name="i">The index of the item to retrieve.</param>
        /// <returns>The item at the requested index or null if index is out of bounds.</returns>
        public object this[int i]
        {
            get
            {
                return (i >= 0 && i < this.m_size) ? m_items[i] : null;
            }
            set
            {
                this.m_items[i] = value;
            }
        }

        /// <summary>
        /// Removes and returns the item at the top of the stack
        /// </summary>
        /// <returns>The item at the top of the stack.</returns>
        public object Pop()
        {
            this.m_count--;
            if (this.m_count > 0)
            {
                return m_items[this.m_count - 1];
            }

            return null;
        }

        /// <summary>
        /// Pushes a new slot at the top of the stack.
        /// </summary>
        /// <returns>The object at the top of the stack.</returns>
        /// <remarks>
        /// This method tries to reuse a slot, if it returns null then
        /// the user has to call the other Push method.
        /// </remarks>
        public object Push()
        {
            if (this.m_count == this.m_size)
            {
                int newsize = this.m_size + this.m_growth;
                object[] newarray = new object[newsize];
                if (this.m_items != null)
                    Array.Copy(this.m_items, newarray, this.m_size);

                this.m_size = newsize;
                this.m_items = newarray;
            }
            return m_items[this.m_count++];
        }

        /// <summary>
        /// Remove a specific item from the stack.
        /// </summary>
        /// <param name="i">The index of the item to remove.</param>
        [SuppressMessage("Microsoft.Performance", "CA1811", Justification = "Kept for potential future usage.")]
        public void RemoveAt(int i)
        {
            this.m_items[i] = null;
            Array.Copy(this.m_items, i + 1, this.m_items, i, this.m_count - i - 1);
            this.m_count--;
        }
    }

    /// <summary>
    /// This class represents an attribute.  The AttDef is assigned
    /// from a validation process, and is used to provide default values.
    /// </summary>
    internal class Attribute
    {
        internal string Name;    // the atomized name.
        internal AttDef DtdType; // the AttDef of the attribute from the SGML DTD.
        internal char QuoteChar; // the quote character used for the attribute value.
        private string m_literalValue; // the attribute value

        /// <summary>
        /// Attribute objects are reused during parsing to reduce memory allocations, 
        /// hence the Reset method.
        /// </summary>
        public void Reset(string name, string value, char quote)
        {
            this.Name = name;
            this.m_literalValue = value;
            this.QuoteChar = quote;
            this.DtdType = null;
        }

        public string Value
        {
            get
            {
                if (this.m_literalValue != null) 
                    return this.m_literalValue;
                if (this.DtdType != null) 
                    return this.DtdType.Default;
                return null;
            }
/*            set
            {
                this.m_literalValue = value;
            }*/
        }

        public bool IsDefault
        {
            get
            {
                return (this.m_literalValue == null);
            }
        }
    }    

    /// <summary>
    /// This class models an XML node, an array of elements in scope is maintained while parsing
    /// for validation purposes, and these Node objects are reused to reduce object allocation,
    /// hence the reset method.  
    /// </summary>
    internal class Node
    {
        internal XmlNodeType NodeType;
        internal string Value;
        internal XmlSpace Space;
        internal string XmlLang;
        internal bool IsEmpty;        
        internal string Name;
        internal ElementDecl DtdType; // the DTD type found via validation
        internal State CurrentState;
        internal bool Simulated; // tag was injected into result stream.
        HWStack attributes = new HWStack(10);

        /// <summary>
        /// Attribute objects are reused during parsing to reduce memory allocations, 
        /// hence the Reset method. 
        /// </summary>
        public void Reset(string name, XmlNodeType nt, string value) {           
            this.Value = value;
            this.Name = name;
            this.NodeType = nt;
            this.Space = XmlSpace.None;
            this.XmlLang= null;
            this.IsEmpty = true;
            this.attributes.Count = 0;
            this.DtdType = null;
        }

        public Attribute AddAttribute(string name, string value, char quotechar, bool caseInsensitive) {
            Attribute a;
            // check for duplicates!
            for (int i = 0, n = this.attributes.Count; i < n; i++) {
                a = (Attribute)this.attributes[i];
                if (string.Equals(a.Name, name, caseInsensitive ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
                {
                    return null;
                }
            }
            // This code makes use of the high water mark for attribute objects,
            // and reuses exisint Attribute objects to avoid memory allocation.
            a = (Attribute)this.attributes.Push();
            if (a == null) {
                a = new Attribute();
                this.attributes[this.attributes.Count-1] = a;
            }
            a.Reset(name, value, quotechar);
            return a;
        }

        [SuppressMessage("Microsoft.Performance", "CA1811", Justification = "Kept for potential future usage.")]
        public void RemoveAttribute(string name)
        {
            for (int i = 0, n = this.attributes.Count; i < n; i++)
            {
                Attribute a  = (Attribute)this.attributes[i];
                if (string.Equals(a.Name, name, StringComparison.OrdinalIgnoreCase))
                {
                    this.attributes.RemoveAt(i);
                    return;
                }
            }
        }
        public void CopyAttributes(Node n) {
            for (int i = 0, len = n.attributes.Count; i < len; i++) {
                Attribute a = (Attribute)n.attributes[i];
                Attribute na = this.AddAttribute(a.Name, a.Value, a.QuoteChar, false);
                na.DtdType = a.DtdType;
            }
        }

        public int AttributeCount {
            get {
                return this.attributes.Count;
            }
        }

        public int GetAttribute(string name) {
            for (int i = 0, n = this.attributes.Count; i < n; i++) {
                Attribute a = (Attribute)this.attributes[i];
                if (string.Equals(a.Name, name, StringComparison.OrdinalIgnoreCase)) {
                    return i;
                }
            }
            return -1;
        }

        public Attribute GetAttribute(int i) {
            if (i>=0 && i<this.attributes.Count) {
                Attribute a = (Attribute)this.attributes[i];
                return a;
            }
            return null;
        }
    }

    internal enum State
    {
        Initial,    // The initial state (Read has not been called yet)
        Markup,     // Expecting text or markup
        EndTag,     // Positioned on an end tag
        Attr,       // Positioned on an attribute
        AttrValue,  // Positioned in an attribute value
        Text,       // Positioned on a Text node.
        PartialTag, // Positioned on a text node, and we have hit a start tag
        AutoClose,  // We are auto-closing tags (this is like State.EndTag), but end tag was generated
        CData,      // We are on a CDATA type node, eg. <scipt> where we have special parsing rules.
        PartialText,
        PseudoStartTag, // we pushed a pseudo-start tag, need to continue with previous start tag.
        Eof
    }


    /// <summary>
    /// SgmlReader is an XmlReader API over any SGML document (including built in 
    /// support for HTML).  
    /// </summary>
    public class SgmlReader : XmlReader
    {
        /// <summary>
        /// The value returned when a namespace is queried and none has been specified.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1705", Justification = "SgmlReader's standards for constants are different to Microsoft's and in line with older C++ style constants naming conventions.  Visually, constants using this style are more easily identifiable as such.")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1707", Justification = "SgmlReader's standards for constants are different to Microsoft's and in line with older C++ style constants naming conventions.  Visually, constants using this style are more easily identifiable as such.")]
        public const string UNDEFINED_NAMESPACE = "#unknown";

        private SgmlDtd m_dtd;
        private Entity m_current;
        private State m_state;
        private char m_partial;
        private string m_endTag;
        private HWStack m_stack;
        private Node m_node; // current node (except for attributes)
        // Attributes are handled separately using these members.
        private Attribute m_a;
        private int m_apos; // which attribute are we positioned on in the collection.
        private Uri m_baseUri;
        private StringBuilder m_sb;
        private StringBuilder m_name;
        private TextWriter m_log;
        private bool m_foundRoot;
        private bool m_ignoreDtd;

        // autoclose support
        private Node m_newnode;
        private int m_poptodepth;
        private int m_rootCount;
        private bool m_isHtml;
        private string m_rootElementName;

        private string m_href;
        private string m_errorLogFile;
        private Entity m_lastError;
        private string m_proxy;
        private TextReader m_inputStream;
        private string m_syslit;
        private string m_pubid;
        private string m_subset;
        private string m_docType;
        private CaseFolding m_folding = CaseFolding.None;
        private bool m_stripDocType = true;
        //private string m_startTag;
        private Dictionary<string, string> unknownNamespaces = new Dictionary<string,string>();

        /// <summary>
        /// Initialises a new instance of the SgmlReader class.
        /// </summary>
        public SgmlReader() {
            Init();
        }

        /// <summary>
        /// Initialises a new instance of the SgmlReader class with an existing <see cref="XmlNameTable"/>, which is NOT used.
        /// </summary>
        /// <param name="nt">The nametable to use.</param>
        public SgmlReader(XmlNameTable nt) {
            Init();
        }

        /// <summary>
        /// Specify the SgmlDtd object directly.  This allows you to cache the Dtd and share
        /// it across multipl SgmlReaders.  To load a DTD from a URL use the SystemLiteral property.
        /// </summary>
        public SgmlDtd Dtd
        {
            get
            {
                if (this.m_dtd == null)
                {
                    LazyLoadDtd(this.m_baseUri);
                }

                return this.m_dtd; 
            }
            set
            {
                this.m_dtd = value;
            }
        }

        private static readonly string htmldtd = "<!--\r\n    This HTML DTD is based on loose.dtd from the W3C, but it is even looser" +
    "\r\n    so as to allow for the types of real world messy HTML you find out on the\r" +
    "\n    web.  For example, allowing all kinds of content like <script> inside a <TD" +
    ">\r\n    and so forth.\r\n-->\r\n<!ENTITY % HTML.Version \"-//W3C//DTD HTML 4.01 Transi" +
    "tional//EN\"\r\n  -- Typical usage:\r\n\r\n    <!DOCTYPE HTML PUBLIC \"-//W3C//DTD HTML " +
    "4.01 Transitional//EN\"\r\n            \"http://www.w3.org/TR/html4/loose.dtd\">\r\n   " +
    " <html>\r\n    <head>\r\n    ...\r\n    </head>\r\n    <body>\r\n    ...\r\n    </body>\r\n   " +
    " </html>\r\n\r\n    The URI used as a system identifier with the public identifier a" +
    "llows\r\n    the user agent to download the DTD and entity sets as needed.\r\n\r\n    " +
    "The FPI for the Strict HTML 4.01 DTD is:\r\n\r\n        \"-//W3C//DTD HTML 4.01//EN\"\r" +
    "\n\r\n    This version of the strict DTD is:\r\n\r\n        http://www.w3.org/TR/1999/R" +
    "EC-html401-19991224/strict.dtd\r\n\r\n    Authors should use the Strict DTD unless t" +
    "hey need the\r\n    presentation control for user agents that don\'t (adequately)\r\n" +
    "    support style sheets.\r\n\r\n    If you are writing a document that includes fra" +
    "mes, use \r\n    the following FPI:\r\n\r\n        \"-//W3C//DTD HTML 4.01 Frameset//EN" +
    "\"\r\n\r\n    This version of the frameset DTD is:\r\n\r\n        http://www.w3.org/TR/19" +
    "99/REC-html401-19991224/frameset.dtd\r\n\r\n\r\n-->\r\n\r\n<!--================== Imported" +
    " Names ====================================-->\r\n<!-- Feature Switch for frameset" +
    " documents -->\r\n<!ENTITY % HTML.Frameset \"IGNORE\">\r\n\r\n<!ENTITY % ContentType \"CD" +
    "ATA\"\r\n    -- media type, as per [RFC2045]\r\n    -->\r\n\r\n<!ENTITY % ContentTypes \"C" +
    "DATA\"\r\n    -- comma-separated list of media types, as per [RFC2045]\r\n    -->\r\n\r\n" +
    "<!ENTITY % Charset \"CDATA\"\r\n    -- a character encoding, as per [RFC2045]\r\n    -" +
    "->\r\n\r\n<!ENTITY % Charsets \"CDATA\"\r\n    -- a space-separated list of character en" +
    "codings, as per [RFC2045]\r\n    -->\r\n\r\n<!ENTITY % LanguageCode \"NAME\"\r\n    -- a l" +
    "anguage code, as per [RFC1766]\r\n    -->\r\n\r\n<!ENTITY % Character \"CDATA\"\r\n    -- " +
    "a single character from [ISO10646] \r\n    -->\r\n\r\n<!ENTITY % LinkTypes \"CDATA\"\r\n  " +
    "  -- space-separated list of link types\r\n    -->\r\n\r\n<!ENTITY % MediaDesc \"CDATA\"" +
    "\r\n    -- single or comma-separated list of media descriptors\r\n    -->\r\n\r\n<!ENTIT" +
    "Y % URI \"CDATA\"\r\n    -- a Uniform Resource Identifier,\r\n       see [URI]\r\n    --" +
    ">\r\n\r\n<!ENTITY % Datetime \"CDATA\" -- date and time information. ISO date format -" +
    "->\r\n\r\n\r\n<!ENTITY % Script \"CDATA\" -- script expression -->\r\n\r\n<!ENTITY % StyleSh" +
    "eet \"CDATA\" -- style sheet data -->\r\n\r\n<!ENTITY % FrameTarget \"CDATA\" -- render " +
    "in this frame -->\r\n\r\n\r\n<!ENTITY % Text \"CDATA\">\r\n\r\n\r\n<!-- Parameter Entities -->" +
    "\r\n\r\n<!ENTITY % head.misc \"SCRIPT|STYLE|META|LINK|OBJECT\" -- repeatable head elem" +
    "ents -->\r\n\r\n<!ENTITY % heading \"H1|H2|H3|H4|H5|H6\">\r\n\r\n<!ENTITY % list \"UL | OL " +
    "|  DIR | MENU\">\r\n\r\n<!ENTITY % preformatted \"PRE\">\r\n\r\n<!ENTITY % Color \"CDATA\" --" +
    " a color using sRGB: #RRGGBB as Hex values -->\r\n\r\n<!-- There are also 16 widely " +
    "known color names with their sRGB values:\r\n\r\n    Black  = #000000    Green  = #0" +
    "08000\r\n    Silver = #C0C0C0    Lime   = #00FF00\r\n    Gray   = #808080    Olive  " +
    "= #808000\r\n    White  = #FFFFFF    Yellow = #FFFF00\r\n    Maroon = #800000    Nav" +
    "y   = #000080\r\n    Red    = #FF0000    Blue   = #0000FF\r\n    Purple = #800080   " +
    " Teal   = #008080\r\n    Fuchsia= #FF00FF    Aqua   = #00FFFF\r\n -->\r\n\r\n<!ENTITY % " +
    "bodycolors \"\r\n  bgcolor     %Color;        #IMPLIED  -- document background colo" +
    "r --\r\n  text        %Color;        #IMPLIED  -- document text color --\r\n  link  " +
    "      %Color;        #IMPLIED  -- color of links --\r\n  vlink       %Color;      " +
    "  #IMPLIED  -- color of visited links --\r\n  alink       %Color;        #IMPLIED " +
    " -- color of selected links --\r\n  \">\r\n\r\n<!--================ Character mnemonic " +
    "entities =========================-->\r\n\r\n<!-- Portions (C) International Organiz" +
    "ation for Standardization 1986\r\n     Permission to copy in any form is granted f" +
    "or use with\r\n     conforming SGML systems and applications as defined in\r\n     I" +
    "SO 8879, provided this notice is included in all copies.\r\n-->\r\n<!-- Character en" +
    "tity set. Typical invocation:\r\n     <!ENTITY % HTMLlat1 PUBLIC\r\n       \"-//W3C//" +
    "ENTITIES Latin 1//EN//HTML\">\r\n     %HTMLlat1;\r\n-->\r\n\r\n<!ENTITY nbsp   CDATA \"&#1" +
    "60;\" -- no-break space = non-breaking space,\r\n                                  " +
    "U+00A0 ISOnum -->\r\n<!ENTITY iexcl  CDATA \"&#161;\" -- inverted exclamation mark, " +
    "U+00A1 ISOnum -->\r\n<!ENTITY cent   CDATA \"&#162;\" -- cent sign, U+00A2 ISOnum --" +
    ">\r\n<!ENTITY pound  CDATA \"&#163;\" -- pound sign, U+00A3 ISOnum -->\r\n<!ENTITY cur" +
    "ren CDATA \"&#164;\" -- currency sign, U+00A4 ISOnum -->\r\n<!ENTITY yen    CDATA \"&" +
    "#165;\" -- yen sign = yuan sign, U+00A5 ISOnum -->\r\n<!ENTITY brvbar CDATA \"&#166;" +
    "\" -- broken bar = broken vertical bar,\r\n                                  U+00A6" +
    " ISOnum -->\r\n<!ENTITY sect   CDATA \"&#167;\" -- section sign, U+00A7 ISOnum -->\r\n" +
    "<!ENTITY uml    CDATA \"&#168;\" -- diaeresis = spacing diaeresis,\r\n              " +
    "                    U+00A8 ISOdia -->\r\n<!ENTITY copy   CDATA \"&#169;\" -- copyrig" +
    "ht sign, U+00A9 ISOnum -->\r\n<!ENTITY ordf   CDATA \"&#170;\" -- feminine ordinal i" +
    "ndicator, U+00AA ISOnum -->\r\n<!ENTITY laquo  CDATA \"&#171;\" -- left-pointing dou" +
    "ble angle quotation mark\r\n                                  = left pointing guil" +
    "lemet, U+00AB ISOnum -->\r\n<!ENTITY not    CDATA \"&#172;\" -- not sign, U+00AC ISO" +
    "num -->\r\n<!ENTITY shy    CDATA \"&#173;\" -- soft hyphen = discretionary hyphen,\r\n" +
    "                                  U+00AD ISOnum -->\r\n<!ENTITY reg    CDATA \"&#17" +
    "4;\" -- registered sign = registered trade mark sign,\r\n                          " +
    "        U+00AE ISOnum -->\r\n<!ENTITY macr   CDATA \"&#175;\" -- macron = spacing ma" +
    "cron = overline\r\n                                  = APL overbar, U+00AF ISOdia " +
    "-->\r\n<!ENTITY deg    CDATA \"&#176;\" -- degree sign, U+00B0 ISOnum -->\r\n<!ENTITY " +
    "plusmn CDATA \"&#177;\" -- plus-minus sign = plus-or-minus sign,\r\n                " +
    "                  U+00B1 ISOnum -->\r\n<!ENTITY sup2   CDATA \"&#178;\" -- superscri" +
    "pt two = superscript digit two\r\n                                  = squared, U+0" +
    "0B2 ISOnum -->\r\n<!ENTITY sup3   CDATA \"&#179;\" -- superscript three = superscrip" +
    "t digit three\r\n                                  = cubed, U+00B3 ISOnum -->\r\n<!E" +
    "NTITY acute  CDATA \"&#180;\" -- acute accent = spacing acute,\r\n                  " +
    "                U+00B4 ISOdia -->\r\n<!ENTITY micro  CDATA \"&#181;\" -- micro sign," +
    " U+00B5 ISOnum -->\r\n<!ENTITY para   CDATA \"&#182;\" -- pilcrow sign = paragraph s" +
    "ign,\r\n                                  U+00B6 ISOnum -->\r\n<!ENTITY middot CDATA" +
    " \"&#183;\" -- middle dot = Georgian comma\r\n                                  = Gr" +
    "eek middle dot, U+00B7 ISOnum -->\r\n<!ENTITY cedil  CDATA \"&#184;\" -- cedilla = s" +
    "pacing cedilla, U+00B8 ISOdia -->\r\n<!ENTITY sup1   CDATA \"&#185;\" -- superscript" +
    " one = superscript digit one,\r\n                                  U+00B9 ISOnum -" +
    "->\r\n<!ENTITY ordm   CDATA \"&#186;\" -- masculine ordinal indicator,\r\n            " +
    "                      U+00BA ISOnum -->\r\n<!ENTITY raquo  CDATA \"&#187;\" -- right" +
    "-pointing double angle quotation mark\r\n                                  = right" +
    " pointing guillemet, U+00BB ISOnum -->\r\n<!ENTITY frac14 CDATA \"&#188;\" -- vulgar" +
    " fraction one quarter\r\n                                  = fraction one quarter," +
    " U+00BC ISOnum -->\r\n<!ENTITY frac12 CDATA \"&#189;\" -- vulgar fraction one half\r\n" +
    "                                  = fraction one half, U+00BD ISOnum -->\r\n<!ENTI" +
    "TY frac34 CDATA \"&#190;\" -- vulgar fraction three quarters\r\n                    " +
    "              = fraction three quarters, U+00BE ISOnum -->\r\n<!ENTITY iquest CDAT" +
    "A \"&#191;\" -- inverted question mark\r\n                                  = turned" +
    " question mark, U+00BF ISOnum -->\r\n<!ENTITY Agrave CDATA \"&#192;\" -- latin capit" +
    "al letter A with grave\r\n                                  = latin capital letter" +
    " A grave,\r\n                                  U+00C0 ISOlat1 -->\r\n<!ENTITY Aacute" +
    " CDATA \"&#193;\" -- latin capital letter A with acute,\r\n                         " +
    "         U+00C1 ISOlat1 -->\r\n<!ENTITY Acirc  CDATA \"&#194;\" -- latin capital let" +
    "ter A with circumflex,\r\n                                  U+00C2 ISOlat1 -->\r\n<!" +
    "ENTITY Atilde CDATA \"&#195;\" -- latin capital letter A with tilde,\r\n            " +
    "                      U+00C3 ISOlat1 -->\r\n<!ENTITY Auml   CDATA \"&#196;\" -- lati" +
    "n capital letter A with diaeresis,\r\n                                  U+00C4 ISO" +
    "lat1 -->\r\n<!ENTITY Aring  CDATA \"&#197;\" -- latin capital letter A with ring abo" +
    "ve\r\n                                  = latin capital letter A ring,\r\n          " +
    "                        U+00C5 ISOlat1 -->\r\n<!ENTITY AElig  CDATA \"&#198;\" -- la" +
    "tin capital letter AE\r\n                                  = latin capital ligatur" +
    "e AE,\r\n                                  U+00C6 ISOlat1 -->\r\n<!ENTITY Ccedil CDA" +
    "TA \"&#199;\" -- latin capital letter C with cedilla,\r\n                           " +
    "       U+00C7 ISOlat1 -->\r\n<!ENTITY Egrave CDATA \"&#200;\" -- latin capital lette" +
    "r E with grave,\r\n                                  U+00C8 ISOlat1 -->\r\n<!ENTITY " +
    "Eacute CDATA \"&#201;\" -- latin capital letter E with acute,\r\n                   " +
    "               U+00C9 ISOlat1 -->\r\n<!ENTITY Ecirc  CDATA \"&#202;\" -- latin capit" +
    "al letter E with circumflex,\r\n                                  U+00CA ISOlat1 -" +
    "->\r\n<!ENTITY Euml   CDATA \"&#203;\" -- latin capital letter E with diaeresis,\r\n  " +
    "                                U+00CB ISOlat1 -->\r\n<!ENTITY Igrave CDATA \"&#204" +
    ";\" -- latin capital letter I with grave,\r\n                                  U+00" +
    "CC ISOlat1 -->\r\n<!ENTITY Iacute CDATA \"&#205;\" -- latin capital letter I with ac" +
    "ute,\r\n                                  U+00CD ISOlat1 -->\r\n<!ENTITY Icirc  CDAT" +
    "A \"&#206;\" -- latin capital letter I with circumflex,\r\n                         " +
    "         U+00CE ISOlat1 -->\r\n<!ENTITY Iuml   CDATA \"&#207;\" -- latin capital let" +
    "ter I with diaeresis,\r\n                                  U+00CF ISOlat1 -->\r\n<!E" +
    "NTITY ETH    CDATA \"&#208;\" -- latin capital letter ETH, U+00D0 ISOlat1 -->\r\n<!E" +
    "NTITY Ntilde CDATA \"&#209;\" -- latin capital letter N with tilde,\r\n             " +
    "                     U+00D1 ISOlat1 -->\r\n<!ENTITY Ograve CDATA \"&#210;\" -- latin" +
    " capital letter O with grave,\r\n                                  U+00D2 ISOlat1 " +
    "-->\r\n<!ENTITY Oacute CDATA \"&#211;\" -- latin capital letter O with acute,\r\n     " +
    "                             U+00D3 ISOlat1 -->\r\n<!ENTITY Ocirc  CDATA \"&#212;\" " +
    "-- latin capital letter O with circumflex,\r\n                                  U+" +
    "00D4 ISOlat1 -->\r\n<!ENTITY Otilde CDATA \"&#213;\" -- latin capital letter O with " +
    "tilde,\r\n                                  U+00D5 ISOlat1 -->\r\n<!ENTITY Ouml   CD" +
    "ATA \"&#214;\" -- latin capital letter O with diaeresis,\r\n                        " +
    "          U+00D6 ISOlat1 -->\r\n<!ENTITY times  CDATA \"&#215;\" -- multiplication s" +
    "ign, U+00D7 ISOnum -->\r\n<!ENTITY Oslash CDATA \"&#216;\" -- latin capital letter O" +
    " with stroke\r\n                                  = latin capital letter O slash,\r" +
    "\n                                  U+00D8 ISOlat1 -->\r\n<!ENTITY Ugrave CDATA \"&#" +
    "217;\" -- latin capital letter U with grave,\r\n                                  U" +
    "+00D9 ISOlat1 -->\r\n<!ENTITY Uacute CDATA \"&#218;\" -- latin capital letter U with" +
    " acute,\r\n                                  U+00DA ISOlat1 -->\r\n<!ENTITY Ucirc  C" +
    "DATA \"&#219;\" -- latin capital letter U with circumflex,\r\n                      " +
    "            U+00DB ISOlat1 -->\r\n<!ENTITY Uuml   CDATA \"&#220;\" -- latin capital " +
    "letter U with diaeresis,\r\n                                  U+00DC ISOlat1 -->\r\n" +
    "<!ENTITY Yacute CDATA \"&#221;\" -- latin capital letter Y with acute,\r\n          " +
    "                        U+00DD ISOlat1 -->\r\n<!ENTITY THORN  CDATA \"&#222;\" -- la" +
    "tin capital letter THORN,\r\n                                  U+00DE ISOlat1 -->\r" +
    "\n<!ENTITY szlig  CDATA \"&#223;\" -- latin small letter sharp s = ess-zed,\r\n      " +
    "                            U+00DF ISOlat1 -->\r\n<!ENTITY agrave CDATA \"&#224;\" -" +
    "- latin small letter a with grave\r\n                                  = latin sma" +
    "ll letter a grave,\r\n                                  U+00E0 ISOlat1 -->\r\n<!ENTI" +
    "TY aacute CDATA \"&#225;\" -- latin small letter a with acute,\r\n                  " +
    "                U+00E1 ISOlat1 -->\r\n<!ENTITY acirc  CDATA \"&#226;\" -- latin smal" +
    "l letter a with circumflex,\r\n                                  U+00E2 ISOlat1 --" +
    ">\r\n<!ENTITY atilde CDATA \"&#227;\" -- latin small letter a with tilde,\r\n         " +
    "                         U+00E3 ISOlat1 -->\r\n<!ENTITY auml   CDATA \"&#228;\" -- l" +
    "atin small letter a with diaeresis,\r\n                                  U+00E4 IS" +
    "Olat1 -->\r\n<!ENTITY aring  CDATA \"&#229;\" -- latin small letter a with ring abov" +
    "e\r\n                                  = latin small letter a ring,\r\n             " +
    "                     U+00E5 ISOlat1 -->\r\n<!ENTITY aelig  CDATA \"&#230;\" -- latin" +
    " small letter ae\r\n                                  = latin small ligature ae, U" +
    "+00E6 ISOlat1 -->\r\n<!ENTITY ccedil CDATA \"&#231;\" -- latin small letter c with c" +
    "edilla,\r\n                                  U+00E7 ISOlat1 -->\r\n<!ENTITY egrave C" +
    "DATA \"&#232;\" -- latin small letter e with grave,\r\n                             " +
    "     U+00E8 ISOlat1 -->\r\n<!ENTITY eacute CDATA \"&#233;\" -- latin small letter e " +
    "with acute,\r\n                                  U+00E9 ISOlat1 -->\r\n<!ENTITY ecir" +
    "c  CDATA \"&#234;\" -- latin small letter e with circumflex,\r\n                    " +
    "              U+00EA ISOlat1 -->\r\n<!ENTITY euml   CDATA \"&#235;\" -- latin small " +
    "letter e with diaeresis,\r\n                                  U+00EB ISOlat1 -->\r\n" +
    "<!ENTITY igrave CDATA \"&#236;\" -- latin small letter i with grave,\r\n            " +
    "                      U+00EC ISOlat1 -->\r\n<!ENTITY iacute CDATA \"&#237;\" -- lati" +
    "n small letter i with acute,\r\n                                  U+00ED ISOlat1 -" +
    "->\r\n<!ENTITY icirc  CDATA \"&#238;\" -- latin small letter i with circumflex,\r\n   " +
    "                               U+00EE ISOlat1 -->\r\n<!ENTITY iuml   CDATA \"&#239;" +
    "\" -- latin small letter i with diaeresis,\r\n                                  U+0" +
    "0EF ISOlat1 -->\r\n<!ENTITY eth    CDATA \"&#240;\" -- latin small letter eth, U+00F" +
    "0 ISOlat1 -->\r\n<!ENTITY ntilde CDATA \"&#241;\" -- latin small letter n with tilde" +
    ",\r\n                                  U+00F1 ISOlat1 -->\r\n<!ENTITY ograve CDATA \"" +
    "&#242;\" -- latin small letter o with grave,\r\n                                  U" +
    "+00F2 ISOlat1 -->\r\n<!ENTITY oacute CDATA \"&#243;\" -- latin small letter o with a" +
    "cute,\r\n                                  U+00F3 ISOlat1 -->\r\n<!ENTITY ocirc  CDA" +
    "TA \"&#244;\" -- latin small letter o with circumflex,\r\n                          " +
    "        U+00F4 ISOlat1 -->\r\n<!ENTITY otilde CDATA \"&#245;\" -- latin small letter" +
    " o with tilde,\r\n                                  U+00F5 ISOlat1 -->\r\n<!ENTITY o" +
    "uml   CDATA \"&#246;\" -- latin small letter o with diaeresis,\r\n                  " +
    "                U+00F6 ISOlat1 -->\r\n<!ENTITY divide CDATA \"&#247;\" -- division s" +
    "ign, U+00F7 ISOnum -->\r\n<!ENTITY oslash CDATA \"&#248;\" -- latin small letter o w" +
    "ith stroke,\r\n                                  = latin small letter o slash,\r\n  " +
    "                                U+00F8 ISOlat1 -->\r\n<!ENTITY ugrave CDATA \"&#249" +
    ";\" -- latin small letter u with grave,\r\n                                  U+00F9" +
    " ISOlat1 -->\r\n<!ENTITY uacute CDATA \"&#250;\" -- latin small letter u with acute," +
    "\r\n                                  U+00FA ISOlat1 -->\r\n<!ENTITY ucirc  CDATA \"&" +
    "#251;\" -- latin small letter u with circumflex,\r\n                               " +
    "   U+00FB ISOlat1 -->\r\n<!ENTITY uuml   CDATA \"&#252;\" -- latin small letter u wi" +
    "th diaeresis,\r\n                                  U+00FC ISOlat1 -->\r\n<!ENTITY ya" +
    "cute CDATA \"&#253;\" -- latin small letter y with acute,\r\n                       " +
    "           U+00FD ISOlat1 -->\r\n<!ENTITY thorn  CDATA \"&#254;\" -- latin small let" +
    "ter thorn,\r\n                                  U+00FE ISOlat1 -->\r\n<!ENTITY yuml " +
    "  CDATA \"&#255;\" -- latin small letter y with diaeresis,\r\n                      " +
    "            U+00FF ISOlat1 -->\r\n\r\n<!-- Mathematical, Greek and Symbolic characte" +
    "rs for HTML -->\r\n\r\n<!-- Character entity set. Typical invocation:\r\n     <!ENTITY" +
    " % HTMLsymbol PUBLIC\r\n       \"-//W3C//ENTITIES Symbols//EN//HTML\">\r\n     %HTMLsy" +
    "mbol; -->\r\n\r\n<!-- Portions (C) International Organization for Standardization 19" +
    "86:\r\n     Permission to copy in any form is granted for use with\r\n     conformin" +
    "g SGML systems and applications as defined in\r\n     ISO 8879, provided this noti" +
    "ce is included in all copies.\r\n-->\r\n\r\n<!-- Relevant ISO entity set is given unle" +
    "ss names are newly introduced.\r\n     New names (i.e., not in ISO 8879 list) do n" +
    "ot clash with any\r\n     existing ISO 8879 entity names. ISO 10646 character numb" +
    "ers\r\n     are given for each character, in hex. CDATA values are decimal\r\n     c" +
    "onversions of the ISO 10646 values and refer to the document\r\n     character set" +
    ". Names are ISO 10646 names. \r\n\r\n-->\r\n\r\n<!-- Latin Extended-B -->\r\n<!ENTITY fnof" +
    "     CDATA \"&#402;\" -- latin small f with hook = function\r\n                     " +
    "               = florin, U+0192 ISOtech -->\r\n\r\n<!-- Greek -->\r\n<!ENTITY Alpha   " +
    " CDATA \"&#913;\" -- greek capital letter alpha, U+0391 -->\r\n<!ENTITY Beta     CDA" +
    "TA \"&#914;\" -- greek capital letter beta, U+0392 -->\r\n<!ENTITY Gamma    CDATA \"&" +
    "#915;\" -- greek capital letter gamma,\r\n                                    U+039" +
    "3 ISOgrk3 -->\r\n<!ENTITY Delta    CDATA \"&#916;\" -- greek capital letter delta,\r\n" +
    "                                    U+0394 ISOgrk3 -->\r\n<!ENTITY Epsilon  CDATA " +
    "\"&#917;\" -- greek capital letter epsilon, U+0395 -->\r\n<!ENTITY Zeta     CDATA \"&" +
    "#918;\" -- greek capital letter zeta, U+0396 -->\r\n<!ENTITY Eta      CDATA \"&#919;" +
    "\" -- greek capital letter eta, U+0397 -->\r\n<!ENTITY Theta    CDATA \"&#920;\" -- g" +
    "reek capital letter theta,\r\n                                    U+0398 ISOgrk3 -" +
    "->\r\n<!ENTITY Iota     CDATA \"&#921;\" -- greek capital letter iota, U+0399 -->\r\n<" +
    "!ENTITY Kappa    CDATA \"&#922;\" -- greek capital letter kappa, U+039A -->\r\n<!ENT" +
    "ITY Lambda   CDATA \"&#923;\" -- greek capital letter lambda,\r\n                   " +
    "                 U+039B ISOgrk3 -->\r\n<!ENTITY Mu       CDATA \"&#924;\" -- greek c" +
    "apital letter mu, U+039C -->\r\n<!ENTITY Nu       CDATA \"&#925;\" -- greek capital " +
    "letter nu, U+039D -->\r\n<!ENTITY Xi       CDATA \"&#926;\" -- greek capital letter " +
    "xi, U+039E ISOgrk3 -->\r\n<!ENTITY Omicron  CDATA \"&#927;\" -- greek capital letter" +
    " omicron, U+039F -->\r\n<!ENTITY Pi       CDATA \"&#928;\" -- greek capital letter p" +
    "i, U+03A0 ISOgrk3 -->\r\n<!ENTITY Rho      CDATA \"&#929;\" -- greek capital letter " +
    "rho, U+03A1 -->\r\n<!-- there is no Sigmaf, and no U+03A2 character either -->\r\n<!" +
    "ENTITY Sigma    CDATA \"&#931;\" -- greek capital letter sigma,\r\n                 " +
    "                   U+03A3 ISOgrk3 -->\r\n<!ENTITY Tau      CDATA \"&#932;\" -- greek" +
    " capital letter tau, U+03A4 -->\r\n<!ENTITY Upsilon  CDATA \"&#933;\" -- greek capit" +
    "al letter upsilon,\r\n                                    U+03A5 ISOgrk3 -->\r\n<!EN" +
    "TITY Phi      CDATA \"&#934;\" -- greek capital letter phi,\r\n                     " +
    "               U+03A6 ISOgrk3 -->\r\n<!ENTITY Chi      CDATA \"&#935;\" -- greek cap" +
    "ital letter chi, U+03A7 -->\r\n<!ENTITY Psi      CDATA \"&#936;\" -- greek capital l" +
    "etter psi,\r\n                                    U+03A8 ISOgrk3 -->\r\n<!ENTITY Ome" +
    "ga    CDATA \"&#937;\" -- greek capital letter omega,\r\n                           " +
    "         U+03A9 ISOgrk3 -->\r\n\r\n<!ENTITY alpha    CDATA \"&#945;\" -- greek small l" +
    "etter alpha,\r\n                                    U+03B1 ISOgrk3 -->\r\n<!ENTITY b" +
    "eta     CDATA \"&#946;\" -- greek small letter beta, U+03B2 ISOgrk3 -->\r\n<!ENTITY " +
    "gamma    CDATA \"&#947;\" -- greek small letter gamma,\r\n                          " +
    "          U+03B3 ISOgrk3 -->\r\n<!ENTITY delta    CDATA \"&#948;\" -- greek small le" +
    "tter delta,\r\n                                    U+03B4 ISOgrk3 -->\r\n<!ENTITY ep" +
    "silon  CDATA \"&#949;\" -- greek small letter epsilon,\r\n                          " +
    "          U+03B5 ISOgrk3 -->\r\n<!ENTITY zeta     CDATA \"&#950;\" -- greek small le" +
    "tter zeta, U+03B6 ISOgrk3 -->\r\n<!ENTITY eta      CDATA \"&#951;\" -- greek small l" +
    "etter eta, U+03B7 ISOgrk3 -->\r\n<!ENTITY theta    CDATA \"&#952;\" -- greek small l" +
    "etter theta,\r\n                                    U+03B8 ISOgrk3 -->\r\n<!ENTITY i" +
    "ota     CDATA \"&#953;\" -- greek small letter iota, U+03B9 ISOgrk3 -->\r\n<!ENTITY " +
    "kappa    CDATA \"&#954;\" -- greek small letter kappa,\r\n                          " +
    "          U+03BA ISOgrk3 -->\r\n<!ENTITY lambda   CDATA \"&#955;\" -- greek small le" +
    "tter lambda,\r\n                                    U+03BB ISOgrk3 -->\r\n<!ENTITY m" +
    "u       CDATA \"&#956;\" -- greek small letter mu, U+03BC ISOgrk3 -->\r\n<!ENTITY nu" +
    "       CDATA \"&#957;\" -- greek small letter nu, U+03BD ISOgrk3 -->\r\n<!ENTITY xi " +
    "      CDATA \"&#958;\" -- greek small letter xi, U+03BE ISOgrk3 -->\r\n<!ENTITY omic" +
    "ron  CDATA \"&#959;\" -- greek small letter omicron, U+03BF NEW -->\r\n<!ENTITY pi  " +
    "     CDATA \"&#960;\" -- greek small letter pi, U+03C0 ISOgrk3 -->\r\n<!ENTITY rho  " +
    "    CDATA \"&#961;\" -- greek small letter rho, U+03C1 ISOgrk3 -->\r\n<!ENTITY sigma" +
    "f   CDATA \"&#962;\" -- greek small letter final sigma,\r\n                         " +
    "           U+03C2 ISOgrk3 -->\r\n<!ENTITY sigma    CDATA \"&#963;\" -- greek small l" +
    "etter sigma,\r\n                                    U+03C3 ISOgrk3 -->\r\n<!ENTITY t" +
    "au      CDATA \"&#964;\" -- greek small letter tau, U+03C4 ISOgrk3 -->\r\n<!ENTITY u" +
    "psilon  CDATA \"&#965;\" -- greek small letter upsilon,\r\n                         " +
    "           U+03C5 ISOgrk3 -->\r\n<!ENTITY phi      CDATA \"&#966;\" -- greek small l" +
    "etter phi, U+03C6 ISOgrk3 -->\r\n<!ENTITY chi      CDATA \"&#967;\" -- greek small l" +
    "etter chi, U+03C7 ISOgrk3 -->\r\n<!ENTITY psi      CDATA \"&#968;\" -- greek small l" +
    "etter psi, U+03C8 ISOgrk3 -->\r\n<!ENTITY omega    CDATA \"&#969;\" -- greek small l" +
    "etter omega,\r\n                                    U+03C9 ISOgrk3 -->\r\n<!ENTITY t" +
    "hetasym CDATA \"&#977;\" -- greek small letter theta symbol,\r\n                    " +
    "                U+03D1 NEW -->\r\n<!ENTITY upsih    CDATA \"&#978;\" -- greek upsilo" +
    "n with hook symbol,\r\n                                    U+03D2 NEW -->\r\n<!ENTIT" +
    "Y piv      CDATA \"&#982;\" -- greek pi symbol, U+03D6 ISOgrk3 -->\r\n\r\n<!-- General" +
    " Punctuation -->\r\n<!ENTITY bull     CDATA \"&#8226;\" -- bullet = black small circ" +
    "le,\r\n                                     U+2022 ISOpub  -->\r\n<!-- bullet is NOT" +
    " the same as bullet operator, U+2219 -->\r\n<!ENTITY hellip   CDATA \"&#8230;\" -- h" +
    "orizontal ellipsis = three dot leader,\r\n                                     U+2" +
    "026 ISOpub  -->\r\n<!ENTITY prime    CDATA \"&#8242;\" -- prime = minutes = feet, U+" +
    "2032 ISOtech -->\r\n<!ENTITY Prime    CDATA \"&#8243;\" -- double prime = seconds = " +
    "inches,\r\n                                     U+2033 ISOtech -->\r\n<!ENTITY oline" +
    "    CDATA \"&#8254;\" -- overline = spacing overscore,\r\n                          " +
    "           U+203E NEW -->\r\n<!ENTITY frasl    CDATA \"&#8260;\" -- fraction slash, " +
    "U+2044 NEW -->\r\n\r\n<!-- Letterlike Symbols -->\r\n<!ENTITY weierp   CDATA \"&#8472;\"" +
    " -- script capital P = power set\r\n                                     = Weierst" +
    "rass p, U+2118 ISOamso -->\r\n<!ENTITY image    CDATA \"&#8465;\" -- blackletter cap" +
    "ital I = imaginary part,\r\n                                     U+2111 ISOamso --" +
    ">\r\n<!ENTITY real     CDATA \"&#8476;\" -- blackletter capital R = real part symbol" +
    ",\r\n                                     U+211C ISOamso -->\r\n<!ENTITY trade    CD" +
    "ATA \"&#8482;\" -- trade mark sign, U+2122 ISOnum -->\r\n<!ENTITY alefsym  CDATA \"&#" +
    "8501;\" -- alef symbol = first transfinite cardinal,\r\n                           " +
    "          U+2135 NEW -->\r\n<!-- alef symbol is NOT the same as hebrew letter alef" +
    ",\r\n     U+05D0 although the same glyph could be used to depict both characters -" +
    "->\r\n\r\n<!-- Arrows -->\r\n<!ENTITY larr     CDATA \"&#8592;\" -- leftwards arrow, U+2" +
    "190 ISOnum -->\r\n<!ENTITY uarr     CDATA \"&#8593;\" -- upwards arrow, U+2191 ISOnu" +
    "m-->\r\n<!ENTITY rarr     CDATA \"&#8594;\" -- rightwards arrow, U+2192 ISOnum -->\r\n" +
    "<!ENTITY darr     CDATA \"&#8595;\" -- downwards arrow, U+2193 ISOnum -->\r\n<!ENTIT" +
    "Y harr     CDATA \"&#8596;\" -- left right arrow, U+2194 ISOamsa -->\r\n<!ENTITY cra" +
    "rr    CDATA \"&#8629;\" -- downwards arrow with corner leftwards\r\n                " +
    "                     = carriage return, U+21B5 NEW -->\r\n<!ENTITY lArr     CDATA " +
    "\"&#8656;\" -- leftwards double arrow, U+21D0 ISOtech -->\r\n<!-- ISO 10646 does not" +
    " say that lArr is the same as the \'is implied by\' arrow\r\n    but also does not h" +
    "ave any other character for that function. So ? lArr can\r\n    be used for \'is im" +
    "plied by\' as ISOtech suggests -->\r\n<!ENTITY uArr     CDATA \"&#8657;\" -- upwards " +
    "double arrow, U+21D1 ISOamsa -->\r\n<!ENTITY rArr     CDATA \"&#8658;\" -- rightward" +
    "s double arrow,\r\n                                     U+21D2 ISOtech -->\r\n<!-- I" +
    "SO 10646 does not say this is the \'implies\' character but does not have \r\n     a" +
    "nother character with this function so ?\r\n     rArr can be used for \'implies\' as" +
    " ISOtech suggests -->\r\n<!ENTITY dArr     CDATA \"&#8659;\" -- downwards double arr" +
    "ow, U+21D3 ISOamsa -->\r\n<!ENTITY hArr     CDATA \"&#8660;\" -- left right double a" +
    "rrow,\r\n                                     U+21D4 ISOamsa -->\r\n\r\n<!-- Mathemati" +
    "cal Operators -->\r\n<!ENTITY forall   CDATA \"&#8704;\" -- for all, U+2200 ISOtech " +
    "-->\r\n<!ENTITY part     CDATA \"&#8706;\" -- partial differential, U+2202 ISOtech  " +
    "-->\r\n<!ENTITY exist    CDATA \"&#8707;\" -- there exists, U+2203 ISOtech -->\r\n<!EN" +
    "TITY empty    CDATA \"&#8709;\" -- empty set = null set = diameter,\r\n             " +
    "                        U+2205 ISOamso -->\r\n<!ENTITY nabla    CDATA \"&#8711;\" --" +
    " nabla = backward difference,\r\n                                     U+2207 ISOte" +
    "ch -->\r\n<!ENTITY isin     CDATA \"&#8712;\" -- element of, U+2208 ISOtech -->\r\n<!E" +
    "NTITY notin    CDATA \"&#8713;\" -- not an element of, U+2209 ISOtech -->\r\n<!ENTIT" +
    "Y ni       CDATA \"&#8715;\" -- contains as member, U+220B ISOtech -->\r\n<!-- shoul" +
    "d there be a more memorable name than \'ni\'? -->\r\n<!ENTITY prod     CDATA \"&#8719" +
    ";\" -- n-ary product = product sign,\r\n                                     U+220F" +
    " ISOamsb -->\r\n<!-- prod is NOT the same character as U+03A0 \'greek capital lette" +
    "r pi\' though\r\n     the same glyph might be used for both -->\r\n<!ENTITY sum      " +
    "CDATA \"&#8721;\" -- n-ary sumation, U+2211 ISOamsb -->\r\n<!-- sum is NOT the same " +
    "character as U+03A3 \'greek capital letter sigma\'\r\n     though the same glyph mig" +
    "ht be used for both -->\r\n<!ENTITY minus    CDATA \"&#8722;\" -- minus sign, U+2212" +
    " ISOtech -->\r\n<!ENTITY lowast   CDATA \"&#8727;\" -- asterisk operator, U+2217 ISO" +
    "tech -->\r\n<!ENTITY radic    CDATA \"&#8730;\" -- square root = radical sign,\r\n    " +
    "                                 U+221A ISOtech -->\r\n<!ENTITY prop     CDATA \"&#" +
    "8733;\" -- proportional to, U+221D ISOtech -->\r\n<!ENTITY infin    CDATA \"&#8734;\"" +
    " -- infinity, U+221E ISOtech -->\r\n<!ENTITY ang      CDATA \"&#8736;\" -- angle, U+" +
    "2220 ISOamso -->\r\n<!ENTITY and      CDATA \"&#8743;\" -- logical and = wedge, U+22" +
    "27 ISOtech -->\r\n<!ENTITY or       CDATA \"&#8744;\" -- logical or = vee, U+2228 IS" +
    "Otech -->\r\n<!ENTITY cap      CDATA \"&#8745;\" -- intersection = cap, U+2229 ISOte" +
    "ch -->\r\n<!ENTITY cup      CDATA \"&#8746;\" -- union = cup, U+222A ISOtech -->\r\n<!" +
    "ENTITY int      CDATA \"&#8747;\" -- integral, U+222B ISOtech -->\r\n<!ENTITY there4" +
    "   CDATA \"&#8756;\" -- therefore, U+2234 ISOtech -->\r\n<!ENTITY sim      CDATA \"&#" +
    "8764;\" -- tilde operator = varies with = similar to,\r\n                          " +
    "           U+223C ISOtech -->\r\n<!-- tilde operator is NOT the same character as " +
    "the tilde, U+007E,\r\n     although the same glyph might be used to represent both" +
    "  -->\r\n<!ENTITY cong     CDATA \"&#8773;\" -- approximately equal to, U+2245 ISOte" +
    "ch -->\r\n<!ENTITY asymp    CDATA \"&#8776;\" -- almost equal to = asymptotic to,\r\n " +
    "                                    U+2248 ISOamsr -->\r\n<!ENTITY ne       CDATA " +
    "\"&#8800;\" -- not equal to, U+2260 ISOtech -->\r\n<!ENTITY equiv    CDATA \"&#8801;\"" +
    " -- identical to, U+2261 ISOtech -->\r\n<!ENTITY le       CDATA \"&#8804;\" -- less-" +
    "than or equal to, U+2264 ISOtech -->\r\n<!ENTITY ge       CDATA \"&#8805;\" -- great" +
    "er-than or equal to,\r\n                                     U+2265 ISOtech -->\r\n<" +
    "!ENTITY sub      CDATA \"&#8834;\" -- subset of, U+2282 ISOtech -->\r\n<!ENTITY sup " +
    "     CDATA \"&#8835;\" -- superset of, U+2283 ISOtech -->\r\n<!-- note that nsup, \'n" +
    "ot a superset of, U+2283\' is not covered by the Symbol \r\n     font encoding and " +
    "is not included. Should it be, for symmetry?\r\n     It is in ISOamsn  --> \r\n<!ENT" +
    "ITY nsub     CDATA \"&#8836;\" -- not a subset of, U+2284 ISOamsn -->\r\n<!ENTITY su" +
    "be     CDATA \"&#8838;\" -- subset of or equal to, U+2286 ISOtech -->\r\n<!ENTITY su" +
    "pe     CDATA \"&#8839;\" -- superset of or equal to,\r\n                            " +
    "         U+2287 ISOtech -->\r\n<!ENTITY oplus    CDATA \"&#8853;\" -- circled plus =" +
    " direct sum,\r\n                                     U+2295 ISOamsb -->\r\n<!ENTITY " +
    "otimes   CDATA \"&#8855;\" -- circled times = vector product,\r\n                   " +
    "                  U+2297 ISOamsb -->\r\n<!ENTITY perp     CDATA \"&#8869;\" -- up ta" +
    "ck = orthogonal to = perpendicular,\r\n                                     U+22A5" +
    " ISOtech -->\r\n<!ENTITY sdot     CDATA \"&#8901;\" -- dot operator, U+22C5 ISOamsb " +
    "-->\r\n<!-- dot operator is NOT the same character as U+00B7 middle dot -->\r\n\r\n<!-" +
    "- Miscellaneous Technical -->\r\n<!ENTITY lceil    CDATA \"&#8968;\" -- left ceiling" +
    " = apl upstile,\r\n                                     U+2308 ISOamsc  -->\r\n<!ENT" +
    "ITY rceil    CDATA \"&#8969;\" -- right ceiling, U+2309 ISOamsc  -->\r\n<!ENTITY lfl" +
    "oor   CDATA \"&#8970;\" -- left floor = apl downstile,\r\n                          " +
    "           U+230A ISOamsc  -->\r\n<!ENTITY rfloor   CDATA \"&#8971;\" -- right floor" +
    ", U+230B ISOamsc  -->\r\n<!ENTITY lang     CDATA \"&#9001;\" -- left-pointing angle " +
    "bracket = bra,\r\n                                     U+2329 ISOtech -->\r\n<!-- la" +
    "ng is NOT the same character as U+003C \'less than\' \r\n     or U+2039 \'single left" +
    "-pointing angle quotation mark\' -->\r\n<!ENTITY rang     CDATA \"&#9002;\" -- right-" +
    "pointing angle bracket = ket,\r\n                                     U+232A ISOte" +
    "ch -->\r\n<!-- rang is NOT the same character as U+003E \'greater than\' \r\n     or U" +
    "+203A \'single right-pointing angle quotation mark\' -->\r\n\r\n<!-- Geometric Shapes " +
    "-->\r\n<!ENTITY loz      CDATA \"&#9674;\" -- lozenge, U+25CA ISOpub -->\r\n\r\n<!-- Mis" +
    "cellaneous Symbols -->\r\n<!ENTITY spades   CDATA \"&#9824;\" -- black spade suit, U" +
    "+2660 ISOpub -->\r\n<!-- black here seems to mean filled as opposed to hollow -->\r" +
    "\n<!ENTITY clubs    CDATA \"&#9827;\" -- black club suit = shamrock,\r\n             " +
    "                        U+2663 ISOpub -->\r\n<!ENTITY hearts   CDATA \"&#9829;\" -- " +
    "black heart suit = valentine,\r\n                                     U+2665 ISOpu" +
    "b -->\r\n<!ENTITY diams    CDATA \"&#9830;\" -- black diamond suit, U+2666 ISOpub --" +
    ">\r\n\r\n<!-- Special characters for HTML -->\r\n\r\n<!-- Character entity set. Typical " +
    "invocation:\r\n     <!ENTITY % HTMLspecial PUBLIC\r\n       \"-//W3C//ENTITIES Specia" +
    "l//EN//HTML\">\r\n     %HTMLspecial; -->\r\n\r\n<!-- Portions (C) International Organiz" +
    "ation for Standardization 1986:\r\n     Permission to copy in any form is granted " +
    "for use with\r\n     conforming SGML systems and applications as defined in\r\n     " +
    "ISO 8879, provided this notice is included in all copies.\r\n-->\r\n\r\n<!-- Relevant " +
    "ISO entity set is given unless names are newly introduced.\r\n     New names (i.e." +
    ", not in ISO 8879 list) do not clash with any\r\n     existing ISO 8879 entity nam" +
    "es. ISO 10646 character numbers\r\n     are given for each character, in hex. CDAT" +
    "A values are decimal\r\n     conversions of the ISO 10646 values and refer to the " +
    "document\r\n     character set. Names are ISO 10646 names. \r\n\r\n-->\r\n\r\n<!-- C0 Cont" +
    "rols and Basic Latin -->\r\n<!ENTITY quot    CDATA \"&#34;\"   -- quotation mark = A" +
    "PL quote,\r\n                                    U+0022 ISOnum -->\r\n<!ENTITY amp  " +
    "   CDATA \"&#38;\"   -- ampersand, U+0026 ISOnum -->\r\n<!ENTITY lt      CDATA \"&#60" +
    ";\"   -- less-than sign, U+003C ISOnum -->\r\n<!ENTITY gt      CDATA \"&#62;\"   -- g" +
    "reater-than sign, U+003E ISOnum -->\r\n\r\n<!-- XML-only entity -->\r\n<!ENTITY apos  " +
    "  CDATA \"&#39;\"   -- single quotation mark = APL quote -->\r\n\r\n<!-- Latin Extende" +
    "d-A -->\r\n<!ENTITY OElig   CDATA \"&#338;\"  -- latin capital ligature OE,\r\n       " +
    "                             U+0152 ISOlat2 -->\r\n<!ENTITY oelig   CDATA \"&#339;\"" +
    "  -- latin small ligature oe, U+0153 ISOlat2 -->\r\n<!-- ligature is a misnomer, t" +
    "his is a separate character in some languages -->\r\n<!ENTITY Scaron  CDATA \"&#352" +
    ";\"  -- latin capital letter S with caron,\r\n                                    U" +
    "+0160 ISOlat2 -->\r\n<!ENTITY scaron  CDATA \"&#353;\"  -- latin small letter s with" +
    " caron,\r\n                                    U+0161 ISOlat2 -->\r\n<!ENTITY Yuml  " +
    "  CDATA \"&#376;\"  -- latin capital letter Y with diaeresis,\r\n                   " +
    "                 U+0178 ISOlat2 -->\r\n\r\n<!-- Spacing Modifier Letters -->\r\n<!ENTI" +
    "TY circ    CDATA \"&#710;\"  -- modifier letter circumflex accent,\r\n              " +
    "                      U+02C6 ISOpub -->\r\n<!ENTITY tilde   CDATA \"&#732;\"  -- sma" +
    "ll tilde, U+02DC ISOdia -->\r\n\r\n<!-- General Punctuation -->\r\n<!ENTITY ensp    CD" +
    "ATA \"&#8194;\" -- en space, U+2002 ISOpub -->\r\n<!ENTITY emsp    CDATA \"&#8195;\" -" +
    "- em space, U+2003 ISOpub -->\r\n<!ENTITY thinsp  CDATA \"&#8201;\" -- thin space, U" +
    "+2009 ISOpub -->\r\n<!ENTITY zwnj    CDATA \"&#8204;\" -- zero width non-joiner,\r\n  " +
    "                                  U+200C NEW RFC 2070 -->\r\n<!ENTITY zwj     CDAT" +
    "A \"&#8205;\" -- zero width joiner, U+200D NEW RFC 2070 -->\r\n<!ENTITY lrm     CDAT" +
    "A \"&#8206;\" -- left-to-right mark, U+200E NEW RFC 2070 -->\r\n<!ENTITY rlm     CDA" +
    "TA \"&#8207;\" -- right-to-left mark, U+200F NEW RFC 2070 -->\r\n<!ENTITY ndash   CD" +
    "ATA \"&#8211;\" -- en dash, U+2013 ISOpub -->\r\n<!ENTITY mdash   CDATA \"&#8212;\" --" +
    " em dash, U+2014 ISOpub -->\r\n<!ENTITY lsquo   CDATA \"&#8216;\" -- left single quo" +
    "tation mark,\r\n                                    U+2018 ISOnum -->\r\n<!ENTITY rs" +
    "quo   CDATA \"&#8217;\" -- right single quotation mark,\r\n                         " +
    "           U+2019 ISOnum -->\r\n<!ENTITY sbquo   CDATA \"&#8218;\" -- single low-9 q" +
    "uotation mark, U+201A NEW -->\r\n<!ENTITY ldquo   CDATA \"&#8220;\" -- left double q" +
    "uotation mark,\r\n                                    U+201C ISOnum -->\r\n<!ENTITY " +
    "rdquo   CDATA \"&#8221;\" -- right double quotation mark,\r\n                       " +
    "             U+201D ISOnum -->\r\n<!ENTITY bdquo   CDATA \"&#8222;\" -- double low-9" +
    " quotation mark, U+201E NEW -->\r\n<!ENTITY dagger  CDATA \"&#8224;\" -- dagger, U+2" +
    "020 ISOpub -->\r\n<!ENTITY Dagger  CDATA \"&#8225;\" -- double dagger, U+2021 ISOpub" +
    " -->\r\n<!ENTITY permil  CDATA \"&#8240;\" -- per mille sign, U+2030 ISOtech -->\r\n<!" +
    "ENTITY lsaquo  CDATA \"&#8249;\" -- single left-pointing angle quotation mark,\r\n  " +
    "                                  U+2039 ISO proposed -->\r\n<!-- lsaquo is propos" +
    "ed but not yet ISO standardized -->\r\n<!ENTITY rsaquo  CDATA \"&#8250;\" -- single " +
    "right-pointing angle quotation mark,\r\n                                    U+203A" +
    " ISO proposed -->\r\n<!-- rsaquo is proposed but not yet ISO standardized -->\r\n<!E" +
    "NTITY euro   CDATA \"&#8364;\"  -- euro sign, U+20AC NEW -->\r\n\r\n<!--==============" +
    "===== Generic Attributes ===============================-->\r\n\r\n<!ENTITY % coreat" +
    "trs\r\n \"id          ID             #IMPLIED  -- document-wide unique id --\r\n  cla" +
    "ss       CDATA          #IMPLIED  -- space-separated list of classes --\r\n  style" +
    "       %StyleSheet;   #IMPLIED  -- associated style info --\r\n  title       %Text" +
    ";         #IMPLIED  -- advisory title --\"\r\n  >\r\n\r\n<!ENTITY % i18n\r\n \"lang       " +
    " %LanguageCode; #IMPLIED  -- language code --\r\n  dir         (ltr|rtl)      #IMP" +
    "LIED  -- direction for weak/neutral text --\"\r\n  >\r\n\r\n<!ENTITY % events\r\n \"onclic" +
    "k     %Script;       #IMPLIED  -- a pointer button was clicked --\r\n  ondblclick " +
    " %Script;       #IMPLIED  -- a pointer button was double clicked--\r\n  onmousedow" +
    "n %Script;       #IMPLIED  -- a pointer button was pressed down --\r\n  onmouseup " +
    "  %Script;       #IMPLIED  -- a pointer button was released --\r\n  onmouseover %S" +
    "cript;       #IMPLIED  -- a pointer was moved onto --\r\n  onmousemove %Script;   " +
    "    #IMPLIED  -- a pointer was moved within --\r\n  onmouseout  %Script;       #IM" +
    "PLIED  -- a pointer was moved away --\r\n  onkeypress  %Script;       #IMPLIED  --" +
    " a key was pressed and released --\r\n  onkeydown   %Script;       #IMPLIED  -- a " +
    "key was pressed down --\r\n  onkeyup     %Script;       #IMPLIED  -- a key was rel" +
    "eased --\"\r\n  >\r\n\r\n<!-- Reserved Feature Switch -->\r\n<!ENTITY % HTML.Reserved \"IG" +
    "NORE\">\r\n\r\n<!-- The following attributes are reserved for possible future use -->" +
    "\r\n<![ %HTML.Reserved; [\r\n<!ENTITY % reserved\r\n \"datasrc     %URI;          #IMPL" +
    "IED  -- a single or tabular Data Source --\r\n  datafld     CDATA          #IMPLIE" +
    "D  -- the property or column name --\r\n  dataformatas (plaintext|html) plaintext " +
    "-- text or html --\"\r\n  >\r\n]]>\r\n\r\n<!ENTITY % reserved \"\">\r\n\r\n<!ENTITY % attrs \"%c" +
    "oreattrs; %i18n; %events;\">\r\n\r\n<!ENTITY % align \"align (left|center|right|justif" +
    "y)  #IMPLIED\"\r\n                   -- default is left for ltr paragraphs, right f" +
    "or rtl --\r\n  >\r\n\r\n<!--=================== Text Markup ==========================" +
    "============-->\r\n\r\n<!ENTITY % fontstyle\r\n \"TT | I | B | U | S | STRIKE | BIG | S" +
    "MALL\">\r\n\r\n<!ENTITY % phrase \"EM | STRONG | DFN | CODE |\r\n                   SAMP" +
    " | KBD | VAR | CITE | ABBR | ACRONYM\" >\r\n\r\n<!ENTITY % special\r\n   \"A | IMG | APP" +
    "LET | OBJECT | FONT | BASEFONT | BR | SCRIPT |\r\n    MAP | Q | SUB | SUP | SPAN |" +
    " BDO | IFRAME\">\r\n\r\n<!ENTITY % formctrl \"INPUT | SELECT | TEXTAREA | LABEL | BUTT" +
    "ON\">\r\n\r\n<!-- %inline; covers inline or \"text-level\" elements -->\r\n<!ENTITY % inl" +
    "ine \"#PCDATA | %fontstyle; | %phrase; | %special; | %formctrl;\">\r\n\r\n<!ELEMENT (%" +
    "fontstyle;|%phrase;) - - (%inline;)*>\r\n<!ATTLIST (%fontstyle;|%phrase;)\r\n  %attr" +
    "s;                              -- %coreattrs, %i18n, %events --\r\n  >\r\n\r\n<!ELEME" +
    "NT (SUB|SUP) - - (%inline;)*    -- subscript, superscript -->\r\n<!ATTLIST (SUB|SU" +
    "P)\r\n  %attrs;                              -- %coreattrs, %i18n, %events --\r\n  >" +
    "\r\n\r\n<!ELEMENT SPAN - - (%inline;)*         -- generic language/style container -" +
    "->\r\n<!ATTLIST SPAN\r\n  %attrs;                              -- %coreattrs, %i18n," +
    " %events --\r\n  %reserved;\t\t\t       -- reserved for possible future use --\r\n  >\r\n" +
    "\r\n<!ELEMENT BDO - - (%inline;)*          -- I18N BiDi over-ride -->\r\n<!ATTLIST B" +
    "DO\r\n  %coreattrs;                          -- id, class, style, title --\r\n  lang" +
    "        %LanguageCode; #IMPLIED  -- language code --\r\n  dir         (ltr|rtl)   " +
    "   #REQUIRED -- directionality --\r\n  >\r\n\r\n<!ELEMENT BASEFONT - O EMPTY          " +
    " -- base font size -->\r\n<!ATTLIST BASEFONT\r\n  id          ID             #IMPLIE" +
    "D  -- document-wide unique id --\r\n  size        CDATA          #REQUIRED -- base" +
    " font size for FONT elements --\r\n  color       %Color;        #IMPLIED  -- text " +
    "color --\r\n  face        CDATA          #IMPLIED  -- comma-separated list of font" +
    " names --\r\n  >\r\n\r\n<!ELEMENT FONT - - (%inline;)*         -- local change to font" +
    " -->\r\n<!ATTLIST FONT\r\n  %coreattrs;                          -- id, class, style" +
    ", title --\r\n  %i18n;\t\t               -- lang, dir --\r\n  size        CDATA       " +
    "   #IMPLIED  -- [+|-]nn e.g. size=\"+1\", size=\"4\" --\r\n  color       %Color;      " +
    "  #IMPLIED  -- text color --\r\n  face        CDATA          #IMPLIED  -- comma-se" +
    "parated list of font names --\r\n  >\r\n\r\n<!ELEMENT BR - O EMPTY                 -- " +
    "forced line break -->\r\n<!ATTLIST BR\r\n  %coreattrs;                          -- i" +
    "d, class, style, title --\r\n  clear       (left|all|right|none) none -- control o" +
    "f text flow --\r\n  >\r\n\r\n<!--================== HTML content models ==============" +
    "=================-->\r\n\r\n<!--\r\n    HTML has two basic content models:\r\n\r\n        " +
    "%inline;     character level elements and text strings\r\n        %block;      blo" +
    "ck-like elements e.g. paragraphs and lists\r\n-->\r\n\r\n<!ENTITY % block\r\n     \"P | %" +
    "heading; | %list; | %preformatted; | DL | DIV | CENTER |\r\n      NOSCRIPT | NOFRA" +
    "MES | BLOCKQUOTE | FORM | ISINDEX | HR |\r\n      TABLE | FIELDSET | ADDRESS\">\r\n\r\n" +
    "<!ENTITY % flow \"%block; | %inline;\">\r\n\r\n<!--=================== Document Body =" +
    "===================================-->\r\n\r\n<!ELEMENT BODY O O (%flow;)* +(INS|DEL" +
    ") -- document body -->\r\n<!ATTLIST BODY\r\n  %attrs;                              -" +
    "- %coreattrs, %i18n, %events --\r\n  onload          %Script;   #IMPLIED  -- the d" +
    "ocument has been loaded --\r\n  onunload        %Script;   #IMPLIED  -- the docume" +
    "nt has been removed --\r\n  background      %URI;      #IMPLIED  -- texture tile f" +
    "or document\r\n                                          background --\r\n  %bodycol" +
    "ors;                         -- bgcolor, text, link, vlink, alink --\r\n  >\r\n\r\n<!E" +
    "LEMENT ADDRESS - - ((%inline;)|P)*  -- information on author -->\r\n<!ATTLIST ADDR" +
    "ESS\r\n  %attrs;                              -- %coreattrs, %i18n, %events --\r\n  " +
    ">\r\n\r\n<!ELEMENT DIV - - (%flow;)*            -- generic language/style container " +
    "-->\r\n<!ATTLIST DIV\r\n  %attrs;                              -- %coreattrs, %i18n," +
    " %events --\r\n  %align;                              -- align, text alignment --\r" +
    "\n  %reserved;                           -- reserved for possible future use --\r\n" +
    "  >\r\n\r\n<!ELEMENT CENTER - - (%flow;)*         -- shorthand for DIV align=center " +
    "-->\r\n<!ATTLIST CENTER\r\n  %attrs;                              -- %coreattrs, %i1" +
    "8n, %events --\r\n  >\r\n\r\n<!--================== The Anchor Element ===============" +
    "=================-->\r\n\r\n<!ENTITY % Shape \"(rect|circle|poly|default)\">\r\n<!ENTITY" +
    " % Coords \"CDATA\" -- comma-separated list of lengths -->\r\n\r\n<!ELEMENT A - - (%in" +
    "line;)* -(A)       -- anchor -->\r\n<!ATTLIST A\r\n  %attrs;                        " +
    "      -- %coreattrs, %i18n, %events --\r\n  charset     %Charset;      #IMPLIED  -" +
    "- char encoding of linked resource --\r\n  type        %ContentType;  #IMPLIED  --" +
    " advisory content type --\r\n  name        CDATA          #IMPLIED  -- named link " +
    "end --\r\n  href        %URI;          #IMPLIED  -- URI for linked resource --\r\n  " +
    "hreflang    %LanguageCode; #IMPLIED  -- language code --\r\n  target      %FrameTa" +
    "rget;  #IMPLIED  -- render in this frame --\r\n  rel         %LinkTypes;    #IMPLI" +
    "ED  -- forward link types --\r\n  rev         %LinkTypes;    #IMPLIED  -- reverse " +
    "link types --\r\n  accesskey   %Character;    #IMPLIED  -- accessibility key chara" +
    "cter --\r\n  shape       %Shape;        rect      -- for use with client-side imag" +
    "e maps --\r\n  coords      %Coords;       #IMPLIED  -- for use with client-side im" +
    "age maps --\r\n  tabindex    NUMBER         #IMPLIED  -- position in tabbing order" +
    " --\r\n  onfocus     %Script;       #IMPLIED  -- the element got the focus --\r\n  o" +
    "nblur      %Script;       #IMPLIED  -- the element lost the focus --\r\n  >\r\n\r\n<!-" +
    "-================== Client-side image maps ============================-->\r\n\r\n<!" +
    "-- These can be placed in the same document or grouped in a\r\n     separate docum" +
    "ent although this isn\'t yet widely supported -->\r\n\r\n<!ELEMENT MAP - - ((%block;)" +
    " | AREA)+ -- client-side image map -->\r\n<!ATTLIST MAP\r\n  %attrs;                " +
    "              -- %coreattrs, %i18n, %events --\r\n  name        CDATA          #RE" +
    "QUIRED -- for reference by usemap --\r\n  >\r\n\r\n<!ELEMENT AREA - O EMPTY           " +
    "    -- client-side image map area -->\r\n<!ATTLIST AREA\r\n  %attrs;                " +
    "              -- %coreattrs, %i18n, %events --\r\n  shape       %Shape;        rec" +
    "t      -- controls interpretation of coords --\r\n  coords      %Coords;       #IM" +
    "PLIED  -- comma-separated list of lengths --\r\n  href        %URI;          #IMPL" +
    "IED  -- URI for linked resource --\r\n  target      %FrameTarget;  #IMPLIED  -- re" +
    "nder in this frame --\r\n  nohref      (nohref)       #IMPLIED  -- this region has" +
    " no action --\r\n  alt         %Text;         #REQUIRED -- short description --\r\n " +
    " tabindex    NUMBER         #IMPLIED  -- position in tabbing order --\r\n  accessk" +
    "ey   %Character;    #IMPLIED  -- accessibility key character --\r\n  onfocus     %" +
    "Script;       #IMPLIED  -- the element got the focus --\r\n  onblur      %Script; " +
    "      #IMPLIED  -- the element lost the focus --\r\n  >\r\n\r\n<!--================== " +
    "The LINK Element ==================================-->\r\n\r\n<!--\r\n  Relationship v" +
    "alues can be used in principle:\r\n\r\n   a) for document specific toolbars/menus wh" +
    "en used\r\n      with the LINK element in document head e.g.\r\n        start, conte" +
    "nts, previous, next, index, end, help\r\n   b) to link to a separate style sheet (" +
    "rel=stylesheet)\r\n   c) to make a link to a script (rel=script)\r\n   d) by stylesh" +
    "eets to control how collections of\r\n      html nodes are rendered into printed d" +
    "ocuments\r\n   e) to make a link to a printable version of this document\r\n      e." +
    "g. a postscript or pdf version (rel=alternate media=print)\r\n-->\r\n\r\n<!ELEMENT LIN" +
    "K - O EMPTY               -- a media-independent link -->\r\n<!ATTLIST LINK\r\n  %at" +
    "trs;                              -- %coreattrs, %i18n, %events --\r\n  charset   " +
    "  %Charset;      #IMPLIED  -- char encoding of linked resource --\r\n  href       " +
    " %URI;          #IMPLIED  -- URI for linked resource --\r\n  hreflang    %Language" +
    "Code; #IMPLIED  -- language code --\r\n  type        %ContentType;  #IMPLIED  -- a" +
    "dvisory content type --\r\n  rel         %LinkTypes;    #IMPLIED  -- forward link " +
    "types --\r\n  rev         %LinkTypes;    #IMPLIED  -- reverse link types --\r\n  med" +
    "ia       %MediaDesc;    #IMPLIED  -- for rendering on these media --\r\n  target  " +
    "    %FrameTarget;  #IMPLIED  -- render in this frame --\r\n  >\r\n\r\n<!--============" +
    "======= Images ===========================================-->\r\n\r\n<!-- Length def" +
    "ined in strict DTD for cellpadding/cellspacing -->\r\n<!ENTITY % Length \"CDATA\" --" +
    " nn for pixels or nn% for percentage length -->\r\n<!ENTITY % MultiLength \"CDATA\" " +
    "-- pixel, percentage, or relative -->\r\n\r\n<![ %HTML.Frameset; [\r\n<!ENTITY % Multi" +
    "Lengths \"CDATA\" -- comma-separated list of MultiLength -->\r\n]]>\r\n\r\n<!ENTITY % Pi" +
    "xels \"CDATA\" -- integer representing length in pixels -->\r\n\r\n<!ENTITY % IAlign \"" +
    "(top|middle|bottom|left|right)\" -- center? -->\r\n\r\n<!-- To avoid problems with te" +
    "xt-only UAs as well as \r\n   to make image content understandable and navigable \r" +
    "\n   to users of non-visual UAs, you need to provide\r\n   a description with ALT, " +
    "and avoid server-side image maps -->\r\n<!ELEMENT IMG - O EMPTY                -- " +
    "Embedded image -->\r\n<!ATTLIST IMG\r\n  %attrs;                              -- %co" +
    "reattrs, %i18n, %events --\r\n  src         %URI;          #REQUIRED -- URI of ima" +
    "ge to embed --\r\n  alt         %Text;         #REQUIRED -- short description --\r\n" +
    "  longdesc    %URI;          #IMPLIED  -- link to long description\r\n            " +
    "                              (complements alt) --\r\n  name        CDATA         " +
    " #IMPLIED  -- name of image for scripting --\r\n  height      %Length;       #IMPL" +
    "IED  -- override height --\r\n  width       %Length;       #IMPLIED  -- override w" +
    "idth --\r\n  usemap      %URI;          #IMPLIED  -- use client-side image map --\r" +
    "\n  ismap       (ismap)        #IMPLIED  -- use server-side image map --\r\n  align" +
    "       %IAlign;       #IMPLIED  -- vertical or horizontal alignment --\r\n  border" +
    "      %Pixels;       #IMPLIED  -- link border width --\r\n  hspace      %Pixels;  " +
    "     #IMPLIED  -- horizontal gutter --\r\n  vspace      %Pixels;       #IMPLIED  -" +
    "- vertical gutter --\r\n  >\r\n\r\n<!-- USEMAP points to a MAP element which may be in" +
    " this document\r\n  or an external document, although the latter is not widely sup" +
    "ported -->\r\n\r\n<!--==================== OBJECT ==================================" +
    "====-->\r\n<!--\r\n  OBJECT is used to embed objects as part of HTML pages \r\n  PARAM" +
    " elements should precede other content. SGML mixed content\r\n  model technicality" +
    " precludes specifying this formally ...\r\n-->\r\n\r\n<!ELEMENT OBJECT - - (PARAM | %f" +
    "low;)*\r\n -- generic embedded object -->\r\n<!ATTLIST OBJECT\r\n  %attrs;            " +
    "                  -- %coreattrs, %i18n, %events --\r\n  declare     (declare)     " +
    " #IMPLIED  -- declare but don\'t instantiate flag --\r\n  classid     %URI;        " +
    "  #IMPLIED  -- identifies an implementation --\r\n  codebase    %URI;          #IM" +
    "PLIED  -- base URI for classid, data, archive--\r\n  data        %URI;          #I" +
    "MPLIED  -- reference to object\'s data --\r\n  type        %ContentType;  #IMPLIED " +
    " -- content type for data --\r\n  codetype    %ContentType;  #IMPLIED  -- content " +
    "type for code --\r\n  archive     CDATA          #IMPLIED  -- space-separated list" +
    " of URIs --\r\n  standby     %Text;         #IMPLIED  -- message to show while loa" +
    "ding --\r\n  height      %Length;       #IMPLIED  -- override height --\r\n  width  " +
    "     %Length;       #IMPLIED  -- override width --\r\n  usemap      %URI;         " +
    " #IMPLIED  -- use client-side image map --\r\n  name        CDATA          #IMPLIE" +
    "D  -- submit as part of form --\r\n  tabindex    NUMBER         #IMPLIED  -- posit" +
    "ion in tabbing order --\r\n  align       %IAlign;       #IMPLIED  -- vertical or h" +
    "orizontal alignment --\r\n  border      %Pixels;       #IMPLIED  -- link border wi" +
    "dth --\r\n  hspace      %Pixels;       #IMPLIED  -- horizontal gutter --\r\n  vspace" +
    "      %Pixels;       #IMPLIED  -- vertical gutter --\r\n  %reserved;              " +
    "             -- reserved for possible future use --\r\n  >\r\n\r\n<!ELEMENT PARAM - O " +
    "EMPTY              -- named property value -->\r\n<!ATTLIST PARAM\r\n  id          I" +
    "D             #IMPLIED  -- document-wide unique id --\r\n  name        CDATA      " +
    "    #REQUIRED -- property name --\r\n  value       CDATA          #IMPLIED  -- pro" +
    "perty value --\r\n  valuetype   (DATA|REF|OBJECT) DATA   -- How to interpret value" +
    " --\r\n  type        %ContentType;  #IMPLIED  -- content type for value\r\n         " +
    "                                 when valuetype=ref --\r\n  >\r\n\r\n<!--=============" +
    "====== Java APPLET ==================================-->\r\n<!--\r\n  One of code or" +
    " object attributes must be present.\r\n  Place PARAM elements before other content" +
    ".\r\n-->\r\n<!ELEMENT APPLET - - (PARAM | %flow;)* -- Java applet -->\r\n<!ATTLIST APP" +
    "LET\r\n  %coreattrs;                          -- id, class, style, title --\r\n  cod" +
    "ebase    %URI;          #IMPLIED  -- optional base URI for applet --\r\n  archive " +
    "    CDATA          #IMPLIED  -- comma-separated archive list --\r\n  code        C" +
    "DATA          #IMPLIED  -- applet class file --\r\n  object      CDATA          #I" +
    "MPLIED  -- serialized applet file --\r\n  alt         %Text;         #IMPLIED  -- " +
    "short description --\r\n  name        CDATA          #IMPLIED  -- allows applets t" +
    "o find each other --\r\n  width       %Length;       #REQUIRED -- initial width --" +
    "\r\n  height      %Length;       #REQUIRED -- initial height --\r\n  align       %IA" +
    "lign;       #IMPLIED  -- vertical or horizontal alignment --\r\n  hspace      %Pix" +
    "els;       #IMPLIED  -- horizontal gutter --\r\n  vspace      %Pixels;       #IMPL" +
    "IED  -- vertical gutter --\r\n  >\r\n\r\n<!--=================== Horizontal Rule =====" +
    "=============================-->\r\n\r\n<!ELEMENT HR - O EMPTY -- horizontal rule --" +
    ">\r\n<!ATTLIST HR\r\n  %attrs;                              -- %coreattrs, %i18n, %e" +
    "vents --\r\n  align       (left|center|right) #IMPLIED\r\n  noshade     (noshade)   " +
    "   #IMPLIED\r\n  size        %Pixels;       #IMPLIED\r\n  width       %Length;      " +
    " #IMPLIED\r\n  >\r\n\r\n<!--=================== Paragraphs ===========================" +
    "============-->\r\n\r\n<!ELEMENT P - O (%inline;)*            -- paragraph -->\r\n<!AT" +
    "TLIST P\r\n  %attrs;                              -- %coreattrs, %i18n, %events --" +
    "\r\n  %align;                              -- align, text alignment --\r\n  >\r\n\r\n<!-" +
    "-=================== Headings =========================================-->\r\n\r\n<!" +
    "--\r\n  There are six levels of headings from H1 (the most important)\r\n  to H6 (th" +
    "e least important).\r\n-->\r\n\r\n<!ELEMENT (%heading;)  - - (%inline;)* -- heading --" +
    ">\r\n<!ATTLIST (%heading;)\r\n  %attrs;                              -- %coreattrs, " +
    "%i18n, %events --\r\n  %align;                              -- align, text alignme" +
    "nt --\r\n  >\r\n\r\n<!--=================== Preformatted Text ========================" +
    "========-->\r\n\r\n<!-- excludes markup for images and changes in font size -->\r\n<!E" +
    "NTITY % pre.exclusion \"IMG|OBJECT|APPLET|BIG|SMALL|SUB|SUP|FONT|BASEFONT\">\r\n\r\n<!" +
    "ELEMENT PRE - - (%inline;)* -(%pre.exclusion;) -- preformatted text -->\r\n<!ATTLI" +
    "ST PRE\r\n  %attrs;                              -- %coreattrs, %i18n, %events --\r" +
    "\n  width       NUMBER         #IMPLIED\r\n  >\r\n\r\n<!--===================== Inline " +
    "Quotes ==================================-->\r\n\r\n<!ELEMENT Q - - (%inline;)*     " +
    "       -- short inline quotation -->\r\n<!ATTLIST Q\r\n  %attrs;                    " +
    "          -- %coreattrs, %i18n, %events --\r\n  cite        %URI;          #IMPLIE" +
    "D  -- URI for source document or msg --\r\n  >\r\n\r\n<!--=================== Block-li" +
    "ke Quotes ================================-->\r\n\r\n<!ELEMENT BLOCKQUOTE - - (%flow" +
    ";)*     -- long quotation -->\r\n<!ATTLIST BLOCKQUOTE\r\n  %attrs;                  " +
    "            -- %coreattrs, %i18n, %events --\r\n  cite        %URI;          #IMPL" +
    "IED  -- URI for source document or msg --\r\n  >\r\n\r\n<!--=================== Insert" +
    "ed/Deleted Text ============================-->\r\n\r\n\r\n<!-- INS/DEL are handled by" +
    " inclusion on BODY -->\r\n<!ELEMENT (INS|DEL) - - (%flow;)*      -- inserted text," +
    " deleted text -->\r\n<!ATTLIST (INS|DEL)\r\n  %attrs;                              -" +
    "- %coreattrs, %i18n, %events --\r\n  cite        %URI;          #IMPLIED  -- info " +
    "on reason for change --\r\n  datetime    %Datetime;     #IMPLIED  -- date and time" +
    " of change --\r\n  >\r\n\r\n<!--=================== Lists ============================" +
    "================-->\r\n\r\n<!-- definition lists - DT for term, DD for its definitio" +
    "n -->\r\n\r\n<!ELEMENT DL - - (DT|DD)+              -- definition list -->\r\n<!ATTLIS" +
    "T DL\r\n  %attrs;                              -- %coreattrs, %i18n, %events --\r\n " +
    " compact     (compact)      #IMPLIED  -- reduced interitem spacing --\r\n  >\r\n\r\n<!" +
    "ELEMENT DT - O (%inline;)*           -- definition term -->\r\n<!ELEMENT DD - O (%" +
    "flow;)*             -- definition description -->\r\n<!ATTLIST (DT|DD)\r\n  %attrs; " +
    "                             -- %coreattrs, %i18n, %events --\r\n  >\r\n\r\n<!-- Order" +
    "ed lists (OL) Numbering style\r\n\r\n    1   arablic numbers     1, 2, 3, ...\r\n    a" +
    "   lower alpha         a, b, c, ...\r\n    A   upper alpha         A, B, C, ...\r\n " +
    "   i   lower roman         i, ii, iii, ...\r\n    I   upper roman         I, II, I" +
    "II, ...\r\n\r\n    The style is applied to the sequence number which by default\r\n   " +
    " is reset to 1 for the first list item in an ordered list.\r\n\r\n    This can\'t be " +
    "expressed directly in SGML due to case folding.\r\n-->\r\n\r\n<!ENTITY % OLStyle \"CDAT" +
    "A\"      -- constrained to: \"(1|a|A|i|I)\" -->\r\n\r\n<!ELEMENT OL - - (LI)+          " +
    "       -- ordered list -->\r\n<!ATTLIST OL\r\n  %attrs;                             " +
    " -- %coreattrs, %i18n, %events --\r\n  type        %OLStyle;      #IMPLIED  -- num" +
    "bering style --\r\n  compact     (compact)      #IMPLIED  -- reduced interitem spa" +
    "cing --\r\n  start       NUMBER         #IMPLIED  -- starting sequence number --\r\n" +
    "  >\r\n\r\n<!-- Unordered Lists (UL) bullet styles -->\r\n<!ENTITY % ULStyle \"(disc|sq" +
    "uare|circle)\">\r\n\r\n<!ELEMENT UL - - (LI)+                 -- unordered list -->\r\n" +
    "<!ATTLIST UL\r\n  %attrs;                              -- %coreattrs, %i18n, %even" +
    "ts --\r\n  type        %ULStyle;      #IMPLIED  -- bullet style --\r\n  compact     " +
    "(compact)      #IMPLIED  -- reduced interitem spacing --\r\n  >\r\n\r\n<!ELEMENT (DIR|" +
    "MENU) - - (LI)+ -(%block;) -- directory list, menu list -->\r\n<!ATTLIST DIR\r\n  %a" +
    "ttrs;                              -- %coreattrs, %i18n, %events --\r\n  compact  " +
    "   (compact)      #IMPLIED -- reduced interitem spacing --\r\n  >\r\n<!ATTLIST MENU\r" +
    "\n  %attrs;                              -- %coreattrs, %i18n, %events --\r\n  comp" +
    "act     (compact)      #IMPLIED -- reduced interitem spacing --\r\n  >\r\n\r\n<!ENTITY" +
    " % LIStyle \"CDATA\" -- constrained to: \"(%ULStyle;|%OLStyle;)\" -->\r\n\r\n<!ELEMENT L" +
    "I - O (%flow;)*             -- list item -->\r\n<!ATTLIST LI\r\n  %attrs;           " +
    "                   -- %coreattrs, %i18n, %events --\r\n  type        %LIStyle;    " +
    "  #IMPLIED  -- list item style --\r\n  value       NUMBER         #IMPLIED  -- res" +
    "et sequence number --\r\n  >\r\n\r\n<!--================ Forms =======================" +
    "========================-->\r\n<!ELEMENT FORM - - (%flow;)* -(FORM)   -- interacti" +
    "ve form -->\r\n<!ATTLIST FORM\r\n  %attrs;                              -- %coreattr" +
    "s, %i18n, %events --\r\n  action      %URI;          #REQUIRED -- server-side form" +
    " handler --\r\n  method      (GET|POST)     GET       -- HTTP method used to submi" +
    "t the form--\r\n  enctype     %ContentType;  \"application/x-www-form-urlencoded\"\r\n" +
    "  accept      %ContentTypes; #IMPLIED  -- list of MIME types for file upload --\r" +
    "\n  name        CDATA          #IMPLIED  -- name of form for scripting --\r\n  onsu" +
    "bmit    %Script;       #IMPLIED  -- the form was submitted --\r\n  onreset     %Sc" +
    "ript;       #IMPLIED  -- the form was reset --\r\n  target      %FrameTarget;  #IM" +
    "PLIED  -- render in this frame --\r\n  accept-charset %Charsets;  #IMPLIED  -- lis" +
    "t of supported charsets --\r\n  >\r\n\r\n<!-- Each label must not contain more than ON" +
    "E field -->\r\n<!ELEMENT LABEL - - (%inline;)* -(LABEL) -- form field label text -" +
    "->\r\n<!ATTLIST LABEL\r\n  %attrs;                              -- %coreattrs, %i18n" +
    ", %events --\r\n  for         IDREF          #IMPLIED  -- matches field ID value -" +
    "-\r\n  accesskey   %Character;    #IMPLIED  -- accessibility key character --\r\n  o" +
    "nfocus     %Script;       #IMPLIED  -- the element got the focus --\r\n  onblur   " +
    "   %Script;       #IMPLIED  -- the element lost the focus --\r\n  >\r\n\r\n<!ENTITY % " +
    "InputType\r\n  \"(TEXT | PASSWORD | CHECKBOX |\r\n    RADIO | SUBMIT | RESET |\r\n    F" +
    "ILE | HIDDEN | IMAGE | BUTTON)\"\r\n   >\r\n\r\n<!-- attribute name required for all bu" +
    "t submit and reset -->\r\n<!ELEMENT INPUT - O EMPTY              -- form control -" +
    "->\r\n<!ATTLIST INPUT\r\n  %attrs;                              -- %coreattrs, %i18n" +
    ", %events --\r\n  type        %InputType;    TEXT      -- what kind of widget is n" +
    "eeded --\r\n  name        CDATA          #IMPLIED  -- submit as part of form --\r\n " +
    " value       CDATA          #IMPLIED  -- Specify for radio buttons and checkboxe" +
    "s --\r\n  checked     (checked)      #IMPLIED  -- for radio buttons and check boxe" +
    "s --\r\n  disabled    (disabled)     #IMPLIED  -- unavailable in this context --\r\n" +
    "  readonly    (readonly)     #IMPLIED  -- for text and passwd --\r\n  size        " +
    "CDATA          #IMPLIED  -- specific to each type of field --\r\n  maxlength   NUM" +
    "BER         #IMPLIED  -- max chars for text fields --\r\n  src         %URI;      " +
    "    #IMPLIED  -- for fields with images --\r\n  alt         CDATA          #IMPLIE" +
    "D  -- short description --\r\n  usemap      %URI;          #IMPLIED  -- use client" +
    "-side image map --\r\n  ismap       (ismap)        #IMPLIED  -- use server-side im" +
    "age map --\r\n  tabindex    NUMBER         #IMPLIED  -- position in tabbing order " +
    "--\r\n  accesskey   %Character;    #IMPLIED  -- accessibility key character --\r\n  " +
    "onfocus     %Script;       #IMPLIED  -- the element got the focus --\r\n  onblur  " +
    "    %Script;       #IMPLIED  -- the element lost the focus --\r\n  onselect    %Sc" +
    "ript;       #IMPLIED  -- some text was selected --\r\n  onchange    %Script;      " +
    " #IMPLIED  -- the element value was changed --\r\n  accept      %ContentTypes; #IM" +
    "PLIED  -- list of MIME types for file upload --\r\n  align       %IAlign;       #I" +
    "MPLIED  -- vertical or horizontal alignment --\r\n  %reserved;                    " +
    "       -- reserved for possible future use --\r\n  >\r\n\r\n<!ELEMENT SELECT - - (OPTG" +
    "ROUP|OPTION)+ -- option selector -->\r\n<!ATTLIST SELECT\r\n  %attrs;               " +
    "               -- %coreattrs, %i18n, %events --\r\n  name        CDATA          #I" +
    "MPLIED  -- field name --\r\n  size        NUMBER         #IMPLIED  -- rows visible" +
    " --\r\n  multiple    (multiple)     #IMPLIED  -- default is single selection --\r\n " +
    " disabled    (disabled)     #IMPLIED  -- unavailable in this context --\r\n  tabin" +
    "dex    NUMBER         #IMPLIED  -- position in tabbing order --\r\n  onfocus     %" +
    "Script;       #IMPLIED  -- the element got the focus --\r\n  onblur      %Script; " +
    "      #IMPLIED  -- the element lost the focus --\r\n  onchange    %Script;       #" +
    "IMPLIED  -- the element value was changed --\r\n  %reserved;                      " +
    "     -- reserved for possible future use --\r\n  >\r\n\r\n<!ELEMENT OPTGROUP - - (OPTI" +
    "ON)+ -- option group -->\r\n<!ATTLIST OPTGROUP\r\n  %attrs;                         " +
    "     -- %coreattrs, %i18n, %events --\r\n  disabled    (disabled)     #IMPLIED  --" +
    " unavailable in this context --\r\n  label       %Text;         #REQUIRED -- for u" +
    "se in hierarchical menus --\r\n  >\r\n\r\n<!ELEMENT OPTION - O (#PCDATA)         -- se" +
    "lectable choice -->\r\n<!ATTLIST OPTION\r\n  %attrs;                              --" +
    " %coreattrs, %i18n, %events --\r\n  selected    (selected)     #IMPLIED\r\n  disable" +
    "d    (disabled)     #IMPLIED  -- unavailable in this context --\r\n  label       %" +
    "Text;         #IMPLIED  -- for use in hierarchical menus --\r\n  value       CDATA" +
    "          #IMPLIED  -- defaults to element content --\r\n  >\r\n\r\n<!ELEMENT TEXTAREA" +
    " - - (#PCDATA)       -- multi-line text field -->\r\n<!ATTLIST TEXTAREA\r\n  %attrs;" +
    "                              -- %coreattrs, %i18n, %events --\r\n  name        CD" +
    "ATA          #IMPLIED\r\n  rows        NUMBER         #REQUIRED\r\n  cols        NUM" +
    "BER         #REQUIRED\r\n  disabled    (disabled)     #IMPLIED  -- unavailable in " +
    "this context --\r\n  readonly    (readonly)     #IMPLIED\r\n  tabindex    NUMBER    " +
    "     #IMPLIED  -- position in tabbing order --\r\n  accesskey   %Character;    #IM" +
    "PLIED  -- accessibility key character --\r\n  onfocus     %Script;       #IMPLIED " +
    " -- the element got the focus --\r\n  onblur      %Script;       #IMPLIED  -- the " +
    "element lost the focus --\r\n  onselect    %Script;       #IMPLIED  -- some text w" +
    "as selected --\r\n  onchange    %Script;       #IMPLIED  -- the element value was " +
    "changed --\r\n  %reserved;                           -- reserved for possible futu" +
    "re use --\r\n  >\r\n\r\n<!--\r\n  #PCDATA is to solve the mixed content problem,\r\n  per " +
    "specification only whitespace is allowed there!\r\n -->\r\n<!ELEMENT FIELDSET - - (#" +
    "PCDATA,LEGEND,(%flow;)*) -- form control group -->\r\n<!ATTLIST FIELDSET\r\n  %attrs" +
    ";                              -- %coreattrs, %i18n, %events --\r\n  >\r\n\r\n<!ELEMEN" +
    "T LEGEND - - (%inline;)*       -- fieldset legend -->\r\n<!ENTITY % LAlign \"(top|b" +
    "ottom|left|right)\">\r\n\r\n<!ATTLIST LEGEND\r\n  %attrs;                              " +
    "-- %coreattrs, %i18n, %events --\r\n  accesskey   %Character;    #IMPLIED  -- acce" +
    "ssibility key character --\r\n  align       %LAlign;       #IMPLIED  -- relative t" +
    "o fieldset --\r\n  >\r\n\r\n<!ELEMENT BUTTON - -\r\n     (%flow;)* -(A|%formctrl;|FORM|I" +
    "SINDEX|FIELDSET|IFRAME)\r\n     -- push button -->\r\n<!ATTLIST BUTTON\r\n  %attrs;   " +
    "                           -- %coreattrs, %i18n, %events --\r\n  name        CDATA" +
    "          #IMPLIED\r\n  value       CDATA          #IMPLIED  -- sent to server whe" +
    "n submitted --\r\n  type        (button|submit|reset) submit -- for use as form bu" +
    "tton --\r\n  disabled    (disabled)     #IMPLIED  -- unavailable in this context -" +
    "-\r\n  tabindex    NUMBER         #IMPLIED  -- position in tabbing order --\r\n  acc" +
    "esskey   %Character;    #IMPLIED  -- accessibility key character --\r\n  onfocus  " +
    "   %Script;       #IMPLIED  -- the element got the focus --\r\n  onblur      %Scri" +
    "pt;       #IMPLIED  -- the element lost the focus --\r\n  %reserved;              " +
    "             -- reserved for possible future use --\r\n  >\r\n\r\n<!--================" +
    "======= Tables =======================================-->\r\n\r\n<!-- IETF HTML tabl" +
    "e standard, see [RFC1942] -->\r\n\r\n<!--\r\n The BORDER attribute sets the thickness " +
    "of the frame around the\r\n table. The default units are screen pixels.\r\n\r\n The FR" +
    "AME attribute specifies which parts of the frame around\r\n the table should be re" +
    "ndered. The values are not the same as\r\n CALS to avoid a name clash with the VAL" +
    "IGN attribute.\r\n\r\n The value \"border\" is included for backwards compatibility wi" +
    "th\r\n <TABLE BORDER> which yields frame=border and border=implied\r\n For <TABLE BO" +
    "RDER=1> you get border=1 and frame=implied. In this\r\n case, it is appropriate to" +
    " treat this as frame=border for backwards\r\n compatibility with deployed browsers" +
    ".\r\n-->\r\n<!ENTITY % TFrame \"(void|above|below|hsides|lhs|rhs|vsides|box|border)\">" +
    "\r\n\r\n<!--\r\n The RULES attribute defines which rules to draw between cells:\r\n\r\n If" +
    " RULES is absent then assume:\r\n     \"none\" if BORDER is absent or BORDER=0 other" +
    "wise \"all\"\r\n-->\r\n\r\n<!ENTITY % TRules \"(none | groups | rows | cols | all)\">\r\n  \r" +
    "\n<!-- horizontal placement of table relative to document -->\r\n<!ENTITY % TAlign " +
    "\"(left|center|right)\">\r\n\r\n<!-- horizontal alignment attributes for cell contents" +
    " -->\r\n<!ENTITY % cellhalign\r\n  \"align      (left|center|right|justify|char) #IMP" +
    "LIED\r\n   char       %Character;    #IMPLIED  -- alignment char, e.g. char=\':\' --" +
    "\r\n   charoff    %Length;       #IMPLIED  -- offset for alignment char --\"\r\n  >\r\n" +
    "\r\n<!-- vertical alignment attributes for cell contents -->\r\n<!ENTITY % cellvalig" +
    "n\r\n  \"valign     (top|middle|bottom|baseline) #IMPLIED\"\r\n  >\r\n\r\n<!ELEMENT TABLE " +
    "- -\r\n     (CAPTION?, (COL*|COLGROUP*), THEAD?, TFOOT?, TBODY+)>\r\n<!ELEMENT CAPTI" +
    "ON  - - (%inline;)*     -- table caption -->\r\n<!ELEMENT THEAD    - O (TR|%flow;)" +
    "+           -- table header -->\r\n<!ELEMENT TFOOT    - O (TR|%flow;)+           -" +
    "- table footer -->\r\n<!ELEMENT TBODY    O O (TR|%flow;)+           -- table body " +
    "-->\r\n<!ELEMENT COLGROUP - O (COL|%flow;)*          -- table column group -->\r\n<!" +
    "ELEMENT COL      - O EMPTY           -- table column -->\r\n<!ELEMENT TR       - O" +
    " (TH|TD|%flow;)+        -- table row -->\r\n<!ELEMENT (TH|TD)  - O (%flow;)*      " +
    " -- table header cell, table data cell-->\r\n\r\n<!ATTLIST TABLE                    " +
    "    -- table element --\r\n  %attrs;                              -- %coreattrs, %" +
    "i18n, %events --\r\n  summary     %Text;         #IMPLIED  -- purpose/structure fo" +
    "r speech output--\r\n  width       %Length;       #IMPLIED  -- table width --\r\n  b" +
    "order      %Pixels;       #IMPLIED  -- controls frame width around table --\r\n  f" +
    "rame       %TFrame;       #IMPLIED  -- which parts of frame to render --\r\n  rule" +
    "s       %TRules;       #IMPLIED  -- rulings between rows and cols --\r\n  cellspac" +
    "ing %Length;       #IMPLIED  -- spacing between cells --\r\n  cellpadding %Length;" +
    "       #IMPLIED  -- spacing within cells --\r\n  align       %TAlign;       #IMPLI" +
    "ED  -- table position relative to window --\r\n  bgcolor     %Color;        #IMPLI" +
    "ED  -- background color for cells --\r\n  %reserved;                           -- " +
    "reserved for possible future use --\r\n  datapagesize CDATA         #IMPLIED  -- r" +
    "eserved for possible future use --\r\n  >\r\n\r\n<!ENTITY % CAlign \"(top|bottom|left|r" +
    "ight)\">\r\n\r\n<!ATTLIST CAPTION\r\n  %attrs;                              -- %coreatt" +
    "rs, %i18n, %events --\r\n  align       %CAlign;       #IMPLIED  -- relative to tab" +
    "le --\r\n  >\r\n\r\n<!--\r\nCOLGROUP groups a set of COL elements. It allows you to grou" +
    "p\r\nseveral semantically related columns together.\r\n-->\r\n<!ATTLIST COLGROUP\r\n  %a" +
    "ttrs;                              -- %coreattrs, %i18n, %events --\r\n  span     " +
    "   NUMBER         1         -- default number of columns in group --\r\n  width   " +
    "    %MultiLength;  #IMPLIED  -- default width for enclosed COLs --\r\n  %cellhalig" +
    "n;                         -- horizontal alignment in cells --\r\n  %cellvalign;  " +
    "                       -- vertical alignment in cells --\r\n  >\r\n\r\n<!--\r\n COL elem" +
    "ents define the alignment properties for cells in\r\n one or more columns.\r\n\r\n The" +
    " WIDTH attribute specifies the width of the columns, e.g.\r\n\r\n     width=64      " +
    "  width in screen pixels\r\n     width=0.5*      relative width of 0.5\r\n\r\n The SPA" +
    "N attribute causes the attributes of one\r\n COL element to apply to more than one" +
    " column.\r\n-->\r\n<!ATTLIST COL                          -- column groups and prope" +
    "rties --\r\n  %attrs;                              -- %coreattrs, %i18n, %events -" +
    "-\r\n  span        NUMBER         1         -- COL attributes affect N columns --\r" +
    "\n  width       %MultiLength;  #IMPLIED  -- column width specification --\r\n  %cel" +
    "lhalign;                         -- horizontal alignment in cells --\r\n  %cellval" +
    "ign;                         -- vertical alignment in cells --\r\n  >\r\n\r\n<!--\r\n   " +
    " Use THEAD to duplicate headers when breaking table\r\n    across page boundaries," +
    " or for static headers when\r\n    TBODY sections are rendered in scrolling panel." +
    "\r\n\r\n    Use TFOOT to duplicate footers when breaking table\r\n    across page boun" +
    "daries, or for static footers when\r\n    TBODY sections are rendered in scrolling" +
    " panel.\r\n\r\n    Use multiple TBODY sections when rules are needed\r\n    between gr" +
    "oups of table rows.\r\n-->\r\n<!ATTLIST (THEAD|TBODY|TFOOT)          -- table sectio" +
    "n --\r\n  %attrs;                              -- %coreattrs, %i18n, %events --\r\n " +
    " %cellhalign;                         -- horizontal alignment in cells --\r\n  %ce" +
    "llvalign;                         -- vertical alignment in cells --\r\n  >\r\n\r\n<!AT" +
    "TLIST TR                           -- table row --\r\n  %attrs;                   " +
    "           -- %coreattrs, %i18n, %events --\r\n  %cellhalign;                     " +
    "    -- horizontal alignment in cells --\r\n  %cellvalign;                         " +
    "-- vertical alignment in cells --\r\n  bgcolor     %Color;        #IMPLIED  -- bac" +
    "kground color for row --\r\n  >\r\n\r\n\r\n<!-- Scope is simpler than headers attribute " +
    "for common tables -->\r\n<!ENTITY % Scope \"(row|col|rowgroup|colgroup)\">\r\n\r\n<!-- T" +
    "H is for headers, TD for data, but for cells acting as both use TD -->\r\n<!ATTLIS" +
    "T (TH|TD)                      -- header or data cell --\r\n  %attrs;             " +
    "                 -- %coreattrs, %i18n, %events --\r\n  abbr        %Text;         " +
    "#IMPLIED  -- abbreviation for header cell --\r\n  axis        CDATA          #IMPL" +
    "IED  -- comma-separated list of related headers--\r\n  headers     IDREFS         " +
    "#IMPLIED  -- list of id\'s for header cells --\r\n  scope       %Scope;        #IMP" +
    "LIED  -- scope covered by header cells --\r\n  rowspan     NUMBER         1       " +
    "  -- number of rows spanned by cell --\r\n  colspan     NUMBER         1         -" +
    "- number of cols spanned by cell --\r\n  %cellhalign;                         -- h" +
    "orizontal alignment in cells --\r\n  %cellvalign;                         -- verti" +
    "cal alignment in cells --\r\n  nowrap      (nowrap)       #IMPLIED  -- suppress wo" +
    "rd wrap --\r\n  bgcolor     %Color;        #IMPLIED  -- cell background color --\r\n" +
    "  width       %Length;       #IMPLIED  -- width for cell --\r\n  height      %Leng" +
    "th;       #IMPLIED  -- height for cell --\r\n  >\r\n\r\n<!--================== Documen" +
    "t Frames ===================================-->\r\n\r\n<!--\r\n  The content model for" +
    " HTML documents depends on whether the HEAD is\r\n  followed by a FRAMESET or BODY" +
    " element. The widespread omission of\r\n  the BODY start tag makes it impractical " +
    "to define the content model\r\n  without the use of a marked section.\r\n-->\r\n\r\n<![ " +
    "%HTML.Frameset; [\r\n<!ELEMENT FRAMESET - - ((FRAMESET|FRAME)+ & NOFRAMES?) -- win" +
    "dow subdivision-->\r\n<!ATTLIST FRAMESET\r\n  %coreattrs;                          -" +
    "- id, class, style, title --\r\n  rows        %MultiLengths; #IMPLIED  -- list of " +
    "lengths,\r\n                                          default: 100% (1 row) --\r\n  " +
    "cols        %MultiLengths; #IMPLIED  -- list of lengths,\r\n                      " +
    "                    default: 100% (1 col) --\r\n  onload      %Script;       #IMPL" +
    "IED  -- all the frames have been loaded  -- \r\n  onunload    %Script;       #IMPL" +
    "IED  -- all the frames have been removed -- \r\n  >\r\n]]>\r\n\r\n<![ %HTML.Frameset; [\r" +
    "\n<!-- reserved frame names start with \"_\" otherwise starts with letter -->\r\n<!EL" +
    "EMENT FRAME - O EMPTY              -- subwindow -->\r\n<!ATTLIST FRAME\r\n  %coreatt" +
    "rs;                          -- id, class, style, title --\r\n  longdesc    %URI; " +
    "         #IMPLIED  -- link to long description\r\n                                " +
    "          (complements title) --\r\n  name        CDATA          #IMPLIED  -- name" +
    " of frame for targetting --\r\n  src         %URI;          #IMPLIED  -- source of" +
    " frame content --\r\n  frameborder (1|0)          1         -- request frame borde" +
    "rs? --\r\n  marginwidth %Pixels;       #IMPLIED  -- margin widths in pixels --\r\n  " +
    "marginheight %Pixels;      #IMPLIED  -- margin height in pixels --\r\n  noresize  " +
    "  (noresize)     #IMPLIED  -- allow users to resize frames? --\r\n  scrolling   (y" +
    "es|no|auto)  auto      -- scrollbar or none --\r\n  >\r\n]]>\r\n\r\n<!ELEMENT IFRAME - -" +
    " (%flow;)*         -- inline subwindow -->\r\n<!ATTLIST IFRAME\r\n  %coreattrs;     " +
    "                     -- id, class, style, title --\r\n  longdesc    %URI;         " +
    " #IMPLIED  -- link to long description\r\n                                        " +
    "  (complements title) --\r\n  name        CDATA          #IMPLIED  -- name of fram" +
    "e for targetting --\r\n  src         %URI;          #IMPLIED  -- source of frame c" +
    "ontent --\r\n  frameborder (1|0)          1         -- request frame borders? --\r\n" +
    "  marginwidth %Pixels;       #IMPLIED  -- margin widths in pixels --\r\n  marginhe" +
    "ight %Pixels;      #IMPLIED  -- margin height in pixels --\r\n  scrolling   (yes|n" +
    "o|auto)  auto      -- scrollbar or none --\r\n  align       %IAlign;       #IMPLIE" +
    "D  -- vertical or horizontal alignment --\r\n  height      %Length;       #IMPLIED" +
    "  -- frame height --\r\n  width       %Length;       #IMPLIED  -- frame width --\r\n" +
    "  >\r\n\r\n<![ %HTML.Frameset; [\r\n<!ENTITY % noframes.content \"(BODY) -(NOFRAMES)\">\r" +
    "\n]]>\r\n\r\n<!ENTITY % noframes.content \"(%flow;)*\">\r\n\r\n<!ELEMENT NOFRAMES - - %nofr" +
    "ames.content;\r\n -- alternate content container for non frame-based rendering -->" +
    "\r\n<!ATTLIST NOFRAMES\r\n  %attrs;                              -- %coreattrs, %i18" +
    "n, %events --\r\n  >\r\n\r\n<!--================ Document Head =======================" +
    "================-->\r\n<!-- %head.misc; defined earlier on as \"SCRIPT|STYLE|META|L" +
    "INK|OBJECT\" -->\r\n<!ENTITY % head.content \"TITLE & ISINDEX? & BASE?\">\r\n\r\n<!ELEMEN" +
    "T HEAD O O (%head.content;) +(%head.misc;) -- document head -->\r\n<!ATTLIST HEAD\r" +
    "\n  %i18n;                               -- lang, dir --\r\n  profile     %URI;    " +
    "      #IMPLIED  -- named dictionary of meta info --\r\n  >\r\n\r\n<!-- The TITLE eleme" +
    "nt is not considered part of the flow of text.\r\n       It should be displayed, f" +
    "or example as the page header or\r\n       window title. Exactly one title is requ" +
    "ired per document.\r\n    -->\r\n<!ELEMENT TITLE - - (#PCDATA) -(%head.misc;) -- doc" +
    "ument title -->\r\n<!ATTLIST TITLE %i18n>\r\n\r\n<!ELEMENT ISINDEX - O EMPTY          " +
    "  -- single line prompt -->\r\n<!ATTLIST ISINDEX\r\n  %coreattrs;                   " +
    "       -- id, class, style, title --\r\n  %i18n;                               -- " +
    "lang, dir --\r\n  prompt      %Text;         #IMPLIED  -- prompt message -->\r\n\r\n<!" +
    "ELEMENT BASE - O EMPTY               -- document base URI -->\r\n<!ATTLIST BASE\r\n " +
    " href        %URI;          #IMPLIED  -- URI that acts as base URI --\r\n  target " +
    "     %FrameTarget;  #IMPLIED  -- render in this frame --\r\n  >\r\n\r\n<!ELEMENT META " +
    "- O EMPTY               -- generic metainformation -->\r\n<!ATTLIST META\r\n  %i18n;" +
    "                               -- lang, dir, for use with content --\r\n  http-equ" +
    "iv  NAME           #IMPLIED  -- HTTP response header name  --\r\n  name        NAM" +
    "E           #IMPLIED  -- metainformation name --\r\n  content     CDATA          #" +
    "REQUIRED -- associated information --\r\n  scheme      CDATA          #IMPLIED  --" +
    " select form of content --\r\n  >\r\n\r\n<!ELEMENT STYLE - - %StyleSheet        -- sty" +
    "le info -->\r\n<!ATTLIST STYLE\r\n  %i18n;                               -- lang, di" +
    "r, for use with title --\r\n  type        %ContentType;  #REQUIRED -- content type" +
    " of style language --\r\n  media       %MediaDesc;    #IMPLIED  -- designed for us" +
    "e with these media --\r\n  title       %Text;         #IMPLIED  -- advisory title " +
    "--\r\n  >\r\n\r\n<!ELEMENT SCRIPT - - %Script;          -- script statements -->\r\n<!AT" +
    "TLIST SCRIPT\r\n  charset     %Charset;      #IMPLIED  -- char encoding of linked " +
    "resource --\r\n  type        %ContentType;  #REQUIRED -- content type of script la" +
    "nguage --\r\n  language    CDATA          #IMPLIED  -- predefined script language " +
    "name --\r\n  src         %URI;          #IMPLIED  -- URI for an external script --" +
    "\r\n  defer       (defer)        #IMPLIED  -- UA may defer execution of script --\r" +
    "\n  event       CDATA          #IMPLIED  -- reserved for possible future use --\r\n" +
    "  for         %URI;          #IMPLIED  -- reserved for possible future use --\r\n " +
    " >\r\n\r\n<!ELEMENT NOSCRIPT - - (%flow;)*\r\n  -- alternate content container for non" +
    " script-based rendering -->\r\n<!ATTLIST NOSCRIPT\r\n  %attrs;                      " +
    "        -- %coreattrs, %i18n, %events --\r\n  >\r\n\r\n<!--================ Document S" +
    "tructure ==================================-->\r\n<!ENTITY % version \"version CDAT" +
    "A #FIXED \'%HTML.Version;\'\">\r\n\r\n<![ %HTML.Frameset; [\r\n<!ENTITY % html.content \"H" +
    "EAD, FRAMESET\">\r\n]]>\r\n\r\n<!ENTITY % html.content \"HEAD, BODY\">\r\n\r\n<!ELEMENT HTML " +
    "O O (%html.content;)    -- document root element -->\r\n<!ATTLIST HTML\r\n  %i18n;  " +
    "                             -- lang, dir --\r\n  %version;\r\n  >\r\n";

        private void LazyLoadDtd(Uri baseUri)
        {

            var sr = new StringReader(htmldtd);
            this.m_dtd = SgmlDtd.Parse(baseUri, "HTML", sr, null, this.m_proxy, null);

            if (this.m_dtd != null && this.m_dtd.Name != null)
            {
                switch(this.CaseFolding)
                {
                case CaseFolding.ToUpper:
                    this.m_rootElementName = this.m_dtd.Name.ToUpperInvariant();
                    break;
                case CaseFolding.ToLower:
                    this.m_rootElementName = this.m_dtd.Name.ToLowerInvariant();
                    break;
                default:
                    this.m_rootElementName = this.m_dtd.Name;
                    break;
                }

                this.m_isHtml = StringUtilities.EqualsIgnoreCase(this.m_dtd.Name, "html");
            }
        }

        /// <summary>
        /// The name of root element specified in the DOCTYPE tag.
        /// </summary>
        public string DocType
        {
            get
            {
                return this.m_docType;
            }
            set
            {
                this.m_docType = value;
            }
        }

        /// <summary>
        /// The root element of the document.
        /// </summary>
        public string RootElementName
        {
            get
            {
                return m_rootElementName;
            }
        }

        /// <summary>
        /// The PUBLIC identifier in the DOCTYPE tag
        /// </summary>
        public string PublicIdentifier
        {
            get
            {
                return this.m_pubid;
            }
            set
            {
                this.m_pubid = value;
            }
        }

        /// <summary>
        /// The SYSTEM literal in the DOCTYPE tag identifying the location of the DTD.
        /// </summary>
        public string SystemLiteral
        {
            get
            {
                return this.m_syslit;
            }
            set
            {
                this.m_syslit = value;
            }
        }

        /// <summary>
        /// The DTD internal subset in the DOCTYPE tag
        /// </summary>
        public string InternalSubset
        {
            get
            {
                return this.m_subset;
            }
            set
            {
                this.m_subset = value;
            }
        }

        /// <summary>
        /// The input stream containing SGML data to parse.
        /// You must specify this property or the Href property before calling Read().
        /// </summary>
        public TextReader InputStream
        {
            get
            {
                return this.m_inputStream;
            }
            set
            {
                this.m_inputStream = value;
                Init();
            }
        }

        /// <summary>
        /// Sometimes you need to specify a proxy server in order to load data via HTTP
        /// from outside the firewall.  For example: "itgproxy:80".
        /// </summary>
        public string WebProxy
        {
            get
            {
                return this.m_proxy;
            }
            set
            {
                this.m_proxy = value;
            }
        }

        /// <summary>
        /// The base Uri is used to resolve relative Uri's like the SystemLiteral and
        /// Href properties.  This is a method because BaseURI is a read-only
        /// property on the base XmlReader class.
        /// </summary>
        public void SetBaseUri(string uri)
        {
            this.m_baseUri = new Uri(uri);
        }

        /// <summary>
        /// Specify the location of the input SGML document as a URL.
        /// </summary>
        public string Href
        {
            get
            {
                return this.m_href;
            }
            set
            {
                this.m_href = value; 
                Init();
                if (this.m_baseUri == null)
                {
                    if (this.m_href.IndexOf("://") > 0)
                    {
                        this.m_baseUri = new Uri(this.m_href);
                    }
                }
            }
        }

        /// <summary>
        /// Whether to strip out the DOCTYPE tag from the output (default true)
        /// </summary>
        public bool StripDocType
        {
            get
            {
                return this.m_stripDocType;
            }
            set
            {
                this.m_stripDocType = value;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether to ignore any DTD reference.
        /// </summary>
        /// <value><c>true</c> if DTD references should be ignored; otherwise, <c>false</c>.</value>
        public bool IgnoreDtd
        {
            get { return m_ignoreDtd; }
            set { m_ignoreDtd = value; }
        }

        /// <summary>
        /// The case conversion behaviour while processing tags.
        /// </summary>
        public CaseFolding CaseFolding
        {
            get
            {
                return this.m_folding;
            }
            set
            {
                this.m_folding = value;
            }
        }

        /// <summary>
        /// DTD validation errors are written to this stream.
        /// </summary>
        public TextWriter ErrorLog
        {
            get
            {
                return this.m_log;
            }
            set
            {
                this.m_log = value;
            }
        }


        private void Log(string msg, params string[] args)
        {
            if (ErrorLog != null)
            {
                string err = string.Format(CultureInfo.CurrentUICulture, msg, args);
                if (this.m_lastError != this.m_current)
                {
                    err = err + "    " + this.m_current.Context();
                    this.m_lastError = this.m_current;
                    ErrorLog.WriteLine("### Error:" + err);
                }
                else
                {
                    string path = "";
                    if (this.m_current.ResolvedUri != null)
                    {
                        path = this.m_current.ResolvedUri.AbsolutePath;
                    }

                    ErrorLog.WriteLine("### Error in {0}#{1}, line {2}, position {3}: {4}", path, this.m_current.Name, this.m_current.Line, this.m_current.LinePosition, err);
                }
            }
        }

        private void Log(string msg, char ch)
        {
            Log(msg, ch.ToString());
        }

        private void Init()
        {
            this.m_state = State.Initial;
            this.m_stack = new HWStack(10);
            this.m_node = Push(null, XmlNodeType.Document, null);
            this.m_node.IsEmpty = false;
            this.m_sb = new StringBuilder();
            this.m_name = new StringBuilder();
            this.m_poptodepth = 0;
            this.m_current = null;
            this.m_partial = '\0';
            this.m_endTag = null;
            this.m_a = null;
            this.m_apos = 0;
            this.m_newnode = null;
            this.m_rootCount = 0;
            this.m_foundRoot = false;
            this.unknownNamespaces.Clear();
        }

        private Node Push(string name, XmlNodeType nt, string value)
        {
            Node result = (Node)this.m_stack.Push();
            if (result == null)
            {
                result = new Node();
                this.m_stack[this.m_stack.Count - 1] = result;
            }

            result.Reset(name, nt, value);
            this.m_node = result;
            return result;
        }

        private void SwapTopNodes()
        {
            int top = this.m_stack.Count - 1;
            if (top > 0)
            {
                Node n = (Node)this.m_stack[top - 1];
                this.m_stack[top - 1] = this.m_stack[top];
                this.m_stack[top] = n;
            }
        }

        private Node Push(Node n)
        {
            // we have to do a deep clone of the Node object because
            // it is reused in the stack.
            Node n2 = Push(n.Name, n.NodeType, n.Value);
            n2.DtdType = n.DtdType;
            n2.IsEmpty = n.IsEmpty;
            n2.Space = n.Space;
            n2.XmlLang = n.XmlLang;
            n2.CurrentState = n.CurrentState;
            n2.CopyAttributes(n);
            this.m_node = n2;
            return n2;
        }

        private void Pop()
        {
            if (this.m_stack.Count > 1)
            {
                this.m_node = (Node)this.m_stack.Pop();
            }
        }

        private Node Top()
        {
            int top = this.m_stack.Count - 1;
            if (top > 0)
            {
                return (Node)this.m_stack[top];
            }

            return null;
        }

        /// <summary>
        /// The node type of the node currently being parsed.
        /// </summary>
        public override XmlNodeType NodeType
        {
            get
            {
                if (this.m_state == State.Attr)
                {
                    return XmlNodeType.Attribute;
                }
                else if (this.m_state == State.AttrValue)
                {
                    return XmlNodeType.Text;
                }
                else if (this.m_state == State.EndTag || this.m_state == State.AutoClose)
                {
                    return XmlNodeType.EndElement;
                }

                return this.m_node.NodeType;
            }
        }

        /// <summary>
        /// The name of the current node, if currently positioned on a node or attribute.
        /// </summary>
        public override string Name
        {
            get
            {
                string result = null;
                if (this.m_state == State.Attr)
                {
                    result = XmlConvert.EncodeName(this.m_a.Name);
                }
                else if (this.m_state != State.AttrValue)
                {
                    result = this.m_node.Name;
                }

                return result;
            }
        }

        /// <summary>
        /// The local name of the current node, if currently positioned on a node or attribute.
        /// </summary>
        public override string LocalName
        {
            get
            {
                string result = Name;
                if (result != null)
                {
                    int colon = result.IndexOf(':');
                    if (colon != -1)
                    {
                        result = result.Substring(colon + 1);
                    }
                }
                return result;
            }
        }

        /// <summary>
        /// The namespace of the current node, if currently positioned on a node or attribute.
        /// </summary>
        /// <remarks>
        /// If not positioned on a node or attribute, <see cref="UNDEFINED_NAMESPACE"/> is returned.
        /// </remarks>
        [SuppressMessage("Microsoft.Performance", "CA1820", Justification="Cannot use IsNullOrEmpty in a switch statement and swapping the elegance of switch for a load of 'if's is not worth it.")]
        public override string NamespaceURI
        {
            get
            {
                // SGML has no namespaces, unless this turned out to be an xmlns attribute.
                if (this.m_state == State.Attr && string.Equals(this.m_a.Name, "xmlns", StringComparison.OrdinalIgnoreCase))
                {
                    return "http://www.w3.org/2000/xmlns/";
                }

                string prefix = Prefix;
                switch (Prefix)
                {
                case "xmlns":
                    return "http://www.w3.org/2000/xmlns/";
                case "xml":
                    return "http://www.w3.org/XML/1998/namespace";
                case null: // Should never occur since Prefix never returns null
                case "":
                    if (NodeType == XmlNodeType.Attribute)
                    {
                        // attributes without a prefix are never in any namespace
                        return string.Empty;
                    }
                    else if (NodeType == XmlNodeType.Element)
                    {
                        // check if a 'xmlns:prefix' attribute is defined
                        for (int i = this.m_stack.Count - 1; i > 0; --i)
                        {
                            Node node = this.m_stack[i] as Node;
                            if ((node != null) && (node.NodeType == XmlNodeType.Element))
                            {
                                int index = node.GetAttribute("xmlns");
                                if (index >= 0)
                                {
                                    string value = node.GetAttribute(index).Value;
                                    if (value != null)
                                    {
                                        return value;
                                    }
                                }
                            }
                        }
                    }

                    return string.Empty;
                default: {
                        string value;
                        if((NodeType == XmlNodeType.Attribute) || (NodeType == XmlNodeType.Element)) {

                            // check if a 'xmlns:prefix' attribute is defined
                            string key = "xmlns:" + prefix;
                            for(int i = this.m_stack.Count - 1; i > 0; --i) {
                                Node node = this.m_stack[i] as Node;
                                if((node != null) && (node.NodeType == XmlNodeType.Element)) {
                                    int index = node.GetAttribute(key);
                                    if(index >= 0) {
                                        value = node.GetAttribute(index).Value;
                                        if(value != null) {
                                            return value;
                                        }
                                    }
                                }
                            }
                        }

                        // check if we've seen this prefix before
                        if(!unknownNamespaces.TryGetValue(prefix, out value)) {
                            if(unknownNamespaces.Count > 0) {
                                value = UNDEFINED_NAMESPACE + unknownNamespaces.Count.ToString();
                            } else {
                                value = UNDEFINED_NAMESPACE;
                            }
                            unknownNamespaces[prefix] = value;
                        }
                        return value;
                    }
                }
            }
        }

        /// <summary>
        /// The prefix of the current node's name.
        /// </summary>
        public override string Prefix
        { 
            get
            {
                string result = Name;
                if (result != null)
                {
                    int colon = result.IndexOf(':');
                    if(colon != -1) {
                        result = result.Substring(0, colon);
                    } else {
                        result = string.Empty;
                    }
                }
                return result ?? string.Empty;
            }
        }

        /// <summary>
        /// Whether the current node has a value or not.
        /// </summary>
        public override bool HasValue
        { 
            get
            {
                if (this.m_state == State.Attr || this.m_state == State.AttrValue)
                {
                    return true;
                }

                return (this.m_node.Value != null);
            }
        }

        /// <summary>
        /// The value of the current node.
        /// </summary>
        public override string Value
        {
            get
            {
                if (this.m_state == State.Attr || this.m_state == State.AttrValue)
                {
                    return this.m_a.Value;
                }

                return this.m_node.Value;
            }
        }

        /// <summary>
        /// Gets the depth of the current node in the XML document.
        /// </summary>
        /// <value>The depth of the current node in the XML document.</value>
        public override int Depth
        { 
            get
            {
                if (this.m_state == State.Attr)
                {
                    return this.m_stack.Count;
                }
                else if (this.m_state == State.AttrValue)
                {
                    return this.m_stack.Count + 1;
                }

                return this.m_stack.Count - 1;
            }
        }

        /// <summary>
        /// Gets the base URI of the current node.
        /// </summary>
        /// <value>The base URI of the current node.</value>
        public override string BaseURI
        {
            get
            {
                return this.m_baseUri == null ? "" : this.m_baseUri.AbsoluteUri;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the current node is an empty element (for example, &lt;MyElement/&gt;).
        /// </summary>
        public override bool IsEmptyElement
        {
            get
            {
                if (this.m_state == State.Markup || this.m_state == State.Attr || this.m_state == State.AttrValue)
                {
                    return this.m_node.IsEmpty;
                }

                return false;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the current node is an attribute that was generated from the default value defined in the DTD or schema.
        /// </summary>
        /// <value>
        /// true if the current node is an attribute whose value was generated from the default value defined in the DTD or
        /// schema; false if the attribute value was explicitly set.
        /// </value>
        public override bool IsDefault
        {
            get
            {
                if (this.m_state == State.Attr || this.m_state == State.AttrValue)
                    return this.m_a.IsDefault;

                return false;
            }
        }

        /// <summary>
        /// Gets the quotation mark character used to enclose the value of an attribute node.
        /// </summary>
        /// <value>The quotation mark character (" or ') used to enclose the value of an attribute node.</value>
        /// <remarks>
        /// This property applies only to an attribute node.
        /// </remarks>
        public char QuoteChar
        {
            get
            {
                if (this.m_a != null)
                    return this.m_a.QuoteChar;

                return '\0';
            }
        }

        /// <summary>
        /// Gets the current xml:space scope.
        /// </summary>
        /// <value>One of the <see cref="XmlSpace"/> values. If no xml:space scope exists, this property defaults to XmlSpace.None.</value>
        public override XmlSpace XmlSpace
        {
            get
            {
                for (int i = this.m_stack.Count - 1; i > 1; i--)
                {
                    Node n = (Node)this.m_stack[i];
                    XmlSpace xs = n.Space;
                    if (xs != XmlSpace.None)
                        return xs;
                }

                return XmlSpace.None;
            }
        }

        /// <summary>
        /// Gets the current xml:lang scope.
        /// </summary>
        /// <value>The current xml:lang scope.</value>
        public override string XmlLang
        {
            get
            {
                for (int i = this.m_stack.Count - 1; i > 1; i--)
                {
                    Node n = (Node)this.m_stack[i];
                    string xmllang = n.XmlLang;
                    if (xmllang != null)
                        return xmllang;
                }

                return string.Empty;
            }
        }


        /// <summary>
        /// Gets the number of attributes on the current node.
        /// </summary>
        /// <value>The number of attributes on the current node.</value>
        public override int AttributeCount
        {
            get
            {
                if (this.m_state == State.Attr || this.m_state == State.AttrValue)
                    //For compatibility with mono
                    return this.m_node.AttributeCount;
                else if (this.m_node.NodeType == XmlNodeType.Element || this.m_node.NodeType == XmlNodeType.DocumentType)
                    return this.m_node.AttributeCount;
                else
                    return 0;
            }
        }

        /// <summary>
        /// Gets the value of an attribute with the specified <see cref="Name"/>.
        /// </summary>
        /// <param name="name">The name of the attribute to retrieve.</param>
        /// <returns>The value of the specified attribute. If the attribute is not found, a null reference (Nothing in Visual Basic) is returned. </returns>
        public override string GetAttribute(string name)
        {
            if (this.m_state != State.Attr && this.m_state != State.AttrValue)
            {
                int i = this.m_node.GetAttribute(name);
                if (i >= 0)
                    return GetAttribute(i);
            }

            return null;
        }

        /// <summary>
        /// Gets the value of the attribute with the specified <see cref="LocalName"/> and <see cref="NamespaceURI"/>.
        /// </summary>
        /// <param name="name">The local name of the attribute.</param>
        /// <param name="namespaceURI">The namespace URI of the attribute.</param>
        /// <returns>The value of the specified attribute. If the attribute is not found, a null reference (Nothing in Visual Basic) is returned. This method does not move the reader.</returns>
        public override string GetAttribute(string name, string namespaceURI)
        {
            return GetAttribute(name); // SGML has no namespaces.
        }

        /// <summary>
        /// Gets the value of the attribute with the specified index.
        /// </summary>
        /// <param name="i">The index of the attribute.</param>
        /// <returns>The value of the specified attribute. This method does not move the reader.</returns>
        public override string GetAttribute(int i)
        {
            if (this.m_state != State.Attr && this.m_state != State.AttrValue)
            {
                Attribute a = this.m_node.GetAttribute(i);
                if (a != null)
                    return a.Value;
            }

            throw new ArgumentOutOfRangeException("i");
        }

        /// <summary>
        /// Gets the value of the attribute with the specified index.
        /// </summary>
        /// <param name="i">The index of the attribute.</param>
        /// <returns>The value of the specified attribute. This method does not move the reader.</returns>
        public override string this[int i]
        {
            get
            {
                return GetAttribute(i);
            }
        }

        /// <summary>
        /// Gets the value of an attribute with the specified <see cref="Name"/>.
        /// </summary>
        /// <param name="name">The name of the attribute to retrieve.</param>
        /// <returns>The value of the specified attribute. If the attribute is not found, a null reference (Nothing in Visual Basic) is returned. </returns>
        public override string this[string name]
        { 
            get
            {
                return GetAttribute(name);
            }
        }

        /// <summary>
        /// Gets the value of the attribute with the specified <see cref="LocalName"/> and <see cref="NamespaceURI"/>.
        /// </summary>
        /// <param name="name">The local name of the attribute.</param>
        /// <param name="namespaceURI">The namespace URI of the attribute.</param>
        /// <returns>The value of the specified attribute. If the attribute is not found, a null reference (Nothing in Visual Basic) is returned. This method does not move the reader.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1023", Justification = "This design is that of Microsoft's XmlReader class and overriding its method is merely continuing the same design.")]
        public override string this[string name, string namespaceURI]
        { 
            get
            {
                return GetAttribute(name, namespaceURI);
            }
        }

        /// <summary>
        /// Moves to the atttribute with the specified <see cref="Name"/>.
        /// </summary>
        /// <param name="name">The qualified name of the attribute.</param>
        /// <returns>true if the attribute is found; otherwise, false. If false, the reader's position does not change.</returns>
        public override bool MoveToAttribute(string name)
        {
            int i = this.m_node.GetAttribute(name);
            if (i >= 0)
            {
                MoveToAttribute(i);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Moves to the attribute with the specified <see cref="LocalName"/> and <see cref="NamespaceURI"/>.
        /// </summary>
        /// <param name="name">The local name of the attribute.</param>
        /// <param name="ns">The namespace URI of the attribute.</param>
        /// <returns>true if the attribute is found; otherwise, false. If false, the reader's position does not change.</returns>
        public override bool MoveToAttribute(string name, string ns)
        {
            return MoveToAttribute(name);
        }

        /// <summary>
        /// Moves to the attribute with the specified index.
        /// </summary>
        /// <param name="i">The index of the attribute to move to.</param>
        public override void MoveToAttribute(int i)
        {
            Attribute a = this.m_node.GetAttribute(i);
            if (a != null)
            {
                this.m_apos = i;
                this.m_a = a; 
                //Make sure that AttrValue does not overwrite the preserved value
                if (this.m_state != State.Attr && this.m_state != State.AttrValue)
                {
                    this.m_node.CurrentState = this.m_state; //save current state.
                }

                this.m_state = State.Attr;
                return;
            }

            throw new ArgumentOutOfRangeException("i");
        }

        /// <summary>
        /// Moves to the first attribute.
        /// </summary>
        /// <returns></returns>
        public override bool MoveToFirstAttribute()
        {
            if (this.m_node.AttributeCount > 0)
            {
                MoveToAttribute(0);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Moves to the next attribute.
        /// </summary>
        /// <returns>true if there is a next attribute; false if there are no more attributes.</returns>
        /// <remarks>
        /// If the current node is an element node, this method is equivalent to <see cref="MoveToFirstAttribute"/>. If <see cref="MoveToNextAttribute"/> returns true,
        /// the reader moves to the next attribute; otherwise, the position of the reader does not change.
        /// </remarks>
        public override bool MoveToNextAttribute()
        {
            if (this.m_state != State.Attr && this.m_state != State.AttrValue)
            {
                return MoveToFirstAttribute();
            }
            else if (this.m_apos < this.m_node.AttributeCount - 1)
            {
                MoveToAttribute(this.m_apos + 1);
                return true;
            }
            else
                return false;
        }

        /// <summary>
        /// Moves to the element that contains the current attribute node.
        /// </summary>
        /// <returns>
        /// true if the reader is positioned on an attribute (the reader moves to the element that owns the attribute); false if the reader is not positioned
        /// on an attribute (the position of the reader does not change).
        /// </returns>
        public override bool MoveToElement()
        {
            if (this.m_state == State.Attr || this.m_state == State.AttrValue)
            {
                this.m_state = this.m_node.CurrentState;
                this.m_a = null;
                return true;
            }
            else
                return (this.m_node.NodeType == XmlNodeType.Element);
        }

        /// <summary>
        /// Gets whether the content is HTML or not.
        /// </summary>
        public bool IsHtml
        {
            get
            {
                return this.m_isHtml;
            }
        }

        /// <summary>
        /// Returns the encoding of the current entity.
        /// </summary>
        /// <returns>The encoding of the current entity.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1024", Justification = "This method to get the encoding does not simply read a value, but potentially causes significant processing of the input stream.")]
        public Encoding GetEncoding()
        {
            if (this.m_current == null)
            {
                OpenInput();
            }

            return this.m_current.Encoding;
        }

        private void OpenInput()
        {
            LazyLoadDtd(this.m_baseUri);

            if (this.Href != null)
            {
                this.m_current = new Entity("#document", null, this.m_href, this.m_proxy);
            }
            else if (this.m_inputStream != null)
            {
                this.m_current = new Entity("#document", null, this.m_inputStream, this.m_proxy);           
            }
            else
            {
                throw new InvalidOperationException("You must specify input either via Href or InputStream properties");
            }

            this.m_current.IsHtml = this.IsHtml;
            this.m_current.Open(null, this.m_baseUri);
            if (this.m_current.ResolvedUri != null)
                this.m_baseUri = this.m_current.ResolvedUri;

            if (this.m_current.IsHtml && this.m_dtd == null)
            {
                this.m_docType = "HTML";
                LazyLoadDtd(this.m_baseUri);
            }
        }

        /// <summary>
        /// Reads the next node from the stream.
        /// </summary>
        /// <returns>true if the next node was read successfully; false if there are no more nodes to read.</returns>
        public override bool Read()
        {
            if (m_current == null)
            {
                OpenInput();
            }

            if (m_node.Simulated)
            {
                // return the next node
                m_node.Simulated = false;
                this.m_node = Top();
                this.m_state = this.m_node.CurrentState;
                return true;
            }

            bool foundnode = false;
            while (!foundnode)
            {
                switch (this.m_state)
                {
                    case State.Initial:
                        this.m_state = State.Markup;
                        this.m_current.ReadChar();
                        goto case State.Markup;
                    case State.Eof:
                        if (this.m_current.Parent != null)
                        {
                            this.m_current.Close();
                            this.m_current = this.m_current.Parent;
                        }
                        else
                        {                           
                            return false;
                        }
                        break;
                    case State.EndTag:
                        if (string.Equals(this.m_endTag, this.m_node.Name, StringComparison.OrdinalIgnoreCase))
                        {
                            Pop(); // we're done!
                            this.m_state = State.Markup;
                            goto case State.Markup;
                        }                     
                        Pop(); // close one element
                        foundnode = true;// return another end element.
                        break;
                    case State.Markup:
                        if (this.m_node.IsEmpty)
                        {
                            Pop();
                        }
                        foundnode = ParseMarkup();
                        break;
                    case State.PartialTag:
                        Pop(); // remove text node.
                        this.m_state = State.Markup;
                        foundnode = ParseTag(this.m_partial);
                        break;
                    case State.PseudoStartTag:
                        foundnode = ParseStartTag('<');                        
                        break;
                    case State.AutoClose:
                        Pop(); // close next node.
                        if (this.m_stack.Count <= this.m_poptodepth)
                        {
                            this.m_state = State.Markup;
                            if (this.m_newnode != null)
                            {
                                Push(this.m_newnode); // now we're ready to start the new node.
                                this.m_newnode = null;
                                this.m_state = State.Markup;
                            }
                            else if (this.m_node.NodeType == XmlNodeType.Document)
                            {
                                this.m_state = State.Eof;
                                goto case State.Eof;
                            }
                        } 
                        foundnode = true;
                        break;
                    case State.CData:
                        foundnode = ParseCData();
                        break;
                    case State.Attr:
                        goto case State.AttrValue;
                    case State.AttrValue:
                        this.m_state = State.Markup;
                        goto case State.Markup;
                    case State.Text:
                        Pop();
                        goto case State.Markup;
                    case State.PartialText:
                        if (ParseText(this.m_current.Lastchar, false))
                        {
                            this.m_node.NodeType = XmlNodeType.Whitespace;
                        }

                        foundnode = true;
                        break;
                }

                if (foundnode && this.m_node.NodeType == XmlNodeType.Whitespace)
                {
                    // strip out whitespace (caller is probably pretty printing the XML).
                    foundnode = false;
                }
                if (!foundnode && this.m_state == State.Eof && this.m_stack.Count > 1)
                {
                    this.m_poptodepth = 1;
                    this.m_state = State.AutoClose;
                    this.m_node = Top();
                    return true;
                }
            }
            if (!m_foundRoot && (this.NodeType == XmlNodeType.Element ||
                    this.NodeType == XmlNodeType.Text ||
                    this.NodeType == XmlNodeType.CDATA))
            {
                m_foundRoot = true;
                if (this.IsHtml && (this.NodeType != XmlNodeType.Element ||
                    !string.Equals(this.LocalName, "html", StringComparison.OrdinalIgnoreCase)))
                {
                    // Simulate an HTML root element!
                    this.m_node.CurrentState = this.m_state;
                    Node root = Push("html", XmlNodeType.Element, null);
                    SwapTopNodes(); // make html the outer element.
                    this.m_node = root;
                    root.Simulated = true;
                    root.IsEmpty = false;
                    this.m_state = State.Markup;
                    //this.state = State.PseudoStartTag;
                    //this.startTag = name;
                }

                return true;
            }

            return true;
        }

        private bool ParseMarkup()
        {
            char ch = this.m_current.Lastchar;
            if (ch == '<')
            {
                ch = this.m_current.ReadChar();
                return ParseTag(ch);
            } 
            else if (ch != Entity.EOF)
            {
                if (this.m_node.DtdType != null && this.m_node.DtdType.ContentModel.DeclaredContent == DeclaredContent.CDATA)
                {
                    // e.g. SCRIPT or STYLE tags which contain unparsed character data.
                    this.m_partial = '\0';
                    this.m_state = State.CData;
                    return false;
                }
                else if (ParseText(ch, true))
                {
                    this.m_node.NodeType = XmlNodeType.Whitespace;
                }

                return true;
            }

            this.m_state = State.Eof;
            return false;
        }

        private const string declterm = " \t\r\n><";
        private bool ParseTag(char ch)
        {
            if (ch == '%')
            {
                return ParseAspNet();
            }
            else if (ch == '!')
            {
                ch = this.m_current.ReadChar();
                if (ch == '-')
                {
                    return ParseComment();
                }
                else if (ch == '[')
                {
                    return ParseConditionalBlock();
                }
                else if (ch != '_' && !char.IsLetter(ch))
                {
                    // perhaps it's one of those nasty office document hacks like '<![if ! ie ]>'
                    string value = this.m_current.ScanToEnd(this.m_sb, "Recovering", ">"); // skip it
                    Log("Ignoring invalid markup '<!"+value+">");
                    return false;
                }
                else
                {
                    string name = this.m_current.ScanToken(this.m_sb, SgmlReader.declterm, false);
                    if (string.Equals(name, "DOCTYPE", StringComparison.OrdinalIgnoreCase))
                    {
                        ParseDocType();

                        // In SGML DOCTYPE SYSTEM attribute is optional, but in XML it is required,
                        // therefore if there is no SYSTEM literal then add an empty one.
                        if (this.GetAttribute("SYSTEM") == null && this.GetAttribute("PUBLIC") != null)
                        {
                            this.m_node.AddAttribute("SYSTEM", "", '"', this.m_folding == CaseFolding.None);
                        }

                        if (m_stripDocType)
                        {
                            return false;
                        }
                        else
                        {
                            this.m_node.NodeType = XmlNodeType.DocumentType;
                            return true;
                        }
                    }
                    else
                    {
                        Log("Invalid declaration '<!{0}...'.  Expecting '<!DOCTYPE' only.", name);
                        this.m_current.ScanToEnd(null, "Recovering", ">"); // skip it
                        return false;
                    }
                }
            } 
            else if (ch == '?')
            {
                this.m_current.ReadChar();// consume the '?' character.
                return ParsePI();
            }
            else if (ch == '/')
            {
                return ParseEndTag();
            }
            else
            {
                return ParseStartTag(ch);
            }
        }

        private string ScanName(string terminators)
        {
            string name = this.m_current.ScanToken(this.m_sb, terminators, false);
            switch (this.m_folding)
            {
                case CaseFolding.ToUpper:
                    name = name.ToUpperInvariant();
                    break;
                case CaseFolding.ToLower:
                    name = name.ToLowerInvariant();
                    break;
            }
            return name;
        }

        private static bool VerifyName(string name)
        {
            try
            {
                XmlConvert.VerifyName(name);
                return true;
            }
            catch (XmlException)
            {
                return false;
            }
        }

        private const string tagterm = " \t\r\n=/><";
        private const string aterm = " \t\r\n='\"/>";
        private const string avterm = " \t\r\n>";
        private bool ParseStartTag(char ch)
        {
            string name = null;
            if (m_state != State.PseudoStartTag)
            {
                if (SgmlReader.tagterm.IndexOf(ch) >= 0)
                {
                    this.m_sb.Length = 0;
                    this.m_sb.Append('<');
                    this.m_state = State.PartialText;
                    return false;
                }

                name = ScanName(SgmlReader.tagterm);
            }
            else
            {
                // TODO: Changes by mindtouch mean that  this.startTag is never non-null.  The effects of this need checking.

                //name = this.startTag;
                m_state = State.Markup;
            }

            Node n = Push(name, XmlNodeType.Element, null);
            n.IsEmpty = false;
            Validate(n);
            ch = this.m_current.SkipWhitespace();
            while (ch != Entity.EOF && ch != '>')
            {
                if (ch == '/')
                {
                    n.IsEmpty = true;
                    ch = this.m_current.ReadChar();
                    if (ch != '>')
                    {
                        Log("Expected empty start tag '/>' sequence instead of '{0}'", ch);
                        this.m_current.ScanToEnd(null, "Recovering", ">");
                        return false;
                    }
                    break;
                } 
                else if (ch == '<')
                {
                    Log("Start tag '{0}' is missing '>'", name);
                    break;
                }

                string aname = ScanName(SgmlReader.aterm);
                ch = this.m_current.SkipWhitespace();
                if (string.Equals(aname, ",", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(aname, "=", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(aname, ":", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(aname, ";", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                string value = null;
                char quote = '\0';
                if (ch == '=' || ch == '"' || ch == '\'')
                {
                    if (ch == '=' )
                    {
                        this.m_current.ReadChar();
                        ch = this.m_current.SkipWhitespace();
                    }

                    if (ch == '\'' || ch == '\"')
                    {
                        quote = ch;
                        value = ScanLiteral(this.m_sb, ch);
                    }
                    else if (ch != '>')
                    {
                        string term = SgmlReader.avterm;
                        value = this.m_current.ScanToken(this.m_sb, term, false);
                    }
                }

                if (ValidAttributeName(aname))
                {
                    Attribute a = n.AddAttribute(aname, value ?? aname, quote, this.m_folding == CaseFolding.None);
                    if (a == null)
                    {
                        Log("Duplicate attribute '{0}' ignored", aname);
                    }
                    else
                    {
                        ValidateAttribute(n, a);
                    }
                }

                ch = this.m_current.SkipWhitespace();
            }

            if (ch == Entity.EOF)
            {
                this.m_current.Error("Unexpected EOF parsing start tag '{0}'", name);
            } 
            else if (ch == '>')
            {
                this.m_current.ReadChar(); // consume '>'
            }

            if (this.Depth == 1)
            {
                if (this.m_rootCount == 1)
                {
                    // Hmmm, we found another root level tag, soooo, the only
                    // thing we can do to keep this a valid XML document is stop
                    this.m_state = State.Eof;
                    return false;
                }
                this.m_rootCount++;
            }

            ValidateContent(n);
            return true;
        }

        private bool ParseEndTag()
        {
            this.m_state = State.EndTag;
            this.m_current.ReadChar(); // consume '/' char.
            string name = this.ScanName(SgmlReader.tagterm);
            char ch = this.m_current.SkipWhitespace();
            if (ch != '>')
            {
                Log("Expected empty start tag '/>' sequence instead of '{0}'", ch);
                this.m_current.ScanToEnd(null, "Recovering", ">");
            }

            this.m_current.ReadChar(); // consume '>'

            this.m_endTag = name;

            // Make sure there's a matching start tag for it.                        
            bool caseInsensitive = (this.m_folding == CaseFolding.None);
            this.m_node = (Node)this.m_stack[this.m_stack.Count - 1];
            for (int i = this.m_stack.Count - 1; i > 0; i--)
            {
                Node n = (Node)this.m_stack[i];
                if (string.Equals(n.Name, name, caseInsensitive ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
                {
                    this.m_endTag = n.Name;
                    return true;
                }
            }

            Log("No matching start tag for '</{0}>'", name);
            this.m_state = State.Markup;
            return false;
        }

        private bool ParseAspNet()
        {
            string value = "<%" + this.m_current.ScanToEnd(this.m_sb, "AspNet", "%>") + "%>";
            Push(null, XmlNodeType.CDATA, value);         
            return true;
        }

        private bool ParseComment()
        {
            char ch = this.m_current.ReadChar();
            if (ch != '-')
            {
                Log("Expecting comment '<!--' but found {0}", ch);
                this.m_current.ScanToEnd(null, "Comment", ">");
                return false;
            }

            string value = this.m_current.ScanToEnd(this.m_sb, "Comment", "-->");
            
            // Make sure it's a valid comment!
            int i = value.IndexOf("--");

            while (i >= 0)
            {
                int j = i + 2;
                while (j < value.Length && value[j] == '-')
                    j++;

                if (i > 0)
                {
                    value = value.Substring(0, i - 1) + "-" + value.Substring(j);
                } 
                else
                {
                    value = "-" + value.Substring(j);
                }

                i = value.IndexOf("--");
            }

            if (value.Length > 0 && value[value.Length - 1] == '-')
            {
                value += " "; // '-' cannot be last character
            }

            Push(null, XmlNodeType.Comment, value);         
            return true;
        }

        private const string cdataterm = "\t\r\n[]<>";
        private bool ParseConditionalBlock()
        {
            char ch = m_current.ReadChar(); // skip '['
            ch = m_current.SkipWhitespace();
            string name = m_current.ScanToken(m_sb, cdataterm, false);
            if (name.StartsWith("if "))
            {
                // 'downlevel-revealed' comment (another atrocity of the IE team)
                m_current.ScanToEnd(null, "CDATA", ">");
                return false;
            }
            else if (!string.Equals(name, "CDATA", StringComparison.OrdinalIgnoreCase))
            {
                Log("Expecting CDATA but found '{0}'", name);
                m_current.ScanToEnd(null, "CDATA", ">");
                return false;
            }
            else
            {
                ch = m_current.SkipWhitespace();
                if (ch != '[')
                {
                    Log("Expecting '[' but found '{0}'", ch);
                    m_current.ScanToEnd(null, "CDATA", ">");
                    return false;
                }

                string value = m_current.ScanToEnd(m_sb, "CDATA", "]]>");

                Push(null, XmlNodeType.CDATA, value);
                return true;
            }
        }

        private const string dtterm = " \t\r\n>";
        private void ParseDocType()
        {
            char ch = this.m_current.SkipWhitespace();
            string name = this.ScanName(SgmlReader.dtterm);
            Push(name, XmlNodeType.DocumentType, null);
            ch = this.m_current.SkipWhitespace();
            if (ch != '>')
            {
                string subset = "";
                string pubid = "";
                string syslit = "";

                if (ch != '[')
                {
                    string token = this.m_current.ScanToken(this.m_sb, SgmlReader.dtterm, false);
                    if (string.Equals(token, "PUBLIC", StringComparison.OrdinalIgnoreCase))
                    {
                        ch = this.m_current.SkipWhitespace();
                        if (ch == '\"' || ch == '\'')
                        {
                            pubid = this.m_current.ScanLiteral(this.m_sb, ch);
                            this.m_node.AddAttribute(token, pubid, ch, this.m_folding == CaseFolding.None);
                        }
                    } 
                    else if (!string.Equals(token, "SYSTEM", StringComparison.OrdinalIgnoreCase))
                    {
                        Log("Unexpected token in DOCTYPE '{0}'", token);
                        this.m_current.ScanToEnd(null, "DOCTYPE", ">");
                    }
                    ch = this.m_current.SkipWhitespace();
                    if (ch == '\"' || ch == '\'')
                    {
                        token = "SYSTEM";
                        syslit = this.m_current.ScanLiteral(this.m_sb, ch);
                        this.m_node.AddAttribute(token, syslit, ch, this.m_folding == CaseFolding.None);  
                    }
                    ch = this.m_current.SkipWhitespace();
                }

                if (ch == '[')
                {
                    subset = this.m_current.ScanToEnd(this.m_sb, "Internal Subset", "]");
                    this.m_node.Value = subset;
                }

                ch = this.m_current.SkipWhitespace();
                if (ch != '>')
                {
                    Log("Expecting end of DOCTYPE tag, but found '{0}'", ch);
                    this.m_current.ScanToEnd(null, "DOCTYPE", ">");
                }

                if (this.m_dtd != null && !string.Equals(this.m_dtd.Name, name, StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException("DTD does not match document type");
                }

                this.m_docType = name;
                this.m_pubid = pubid;
                this.m_syslit = syslit;
                this.m_subset = subset;
                LazyLoadDtd(this.m_current.ResolvedUri);
            }

            this.m_current.ReadChar();
        }

        private const string piterm = " \t\r\n?";
        private bool ParsePI()
        {
            string name = this.m_current.ScanToken(this.m_sb, SgmlReader.piterm, false);
            string value = null;
            if (this.m_current.Lastchar != '?')
            {
                // Notice this is not "?>".  This is because Office generates bogus PI's that end with "/>".
                value = this.m_current.ScanToEnd(this.m_sb, "Processing Instruction", ">");
                value = value.TrimEnd('/');
            }
            else
            {
                // error recovery.
                value = this.m_current.ScanToEnd(this.m_sb, "Processing Instruction", ">");
            }

            // check if the name has a prefix; if so, ignore it
            int colon = name.IndexOf(':');
            if(colon > 0) {
                name = name.Substring(colon + 1);
            }

            // skip xml declarations, since these are generated in the output instead.
            if (!string.Equals(name, "xml", StringComparison.OrdinalIgnoreCase))
            {
                Push(name, XmlNodeType.ProcessingInstruction, value);
                return true;
            }

            return false;
        }

        private bool ParseText(char ch, bool newtext)
        {
            bool ws = !newtext || this.m_current.IsWhitespace;
            if (newtext)
                this.m_sb.Length = 0;

            //this.sb.Append(ch);
            //ch = this.current.ReadChar();
            this.m_state = State.Text;
            while (ch != Entity.EOF)
            {
                if (ch == '<')
                {
                    ch = this.m_current.ReadChar();
                    if (ch == '/' || ch == '!' || ch == '?' || char.IsLetter(ch))
                    {
                        // Hit a tag, so return XmlNodeType.Text token
                        // and remember we partially started a new tag.
                        this.m_state = State.PartialTag;
                        this.m_partial = ch;
                        break;
                    } 
                    else
                    {
                        // not a tag, so just proceed.
                        this.m_sb.Append('<');
                        this.m_sb.Append(ch);
                        ws = false;
                        ch = this.m_current.ReadChar();
                    }
                } 
                else if (ch == '&')
                {
                    ExpandEntity(this.m_sb, '<');
                    ws = false;
                    ch = this.m_current.Lastchar;
                }
                else
                {
                    if (!this.m_current.IsWhitespace)
                        ws = false;
                    this.m_sb.Append(ch);
                    ch = this.m_current.ReadChar();
                }
            }

            string value = this.m_sb.ToString();
            Push(null, XmlNodeType.Text, value);
            return ws;
        }

        /// <summary>
        /// Consumes and returns a literal block of text, expanding entities as it does so.
        /// </summary>
        /// <param name="sb">The string builder to use.</param>
        /// <param name="quote">The delimiter for the literal.</param>
        /// <returns>The consumed literal.</returns>
        /// <remarks>
        /// This version is slightly different from <see cref="Entity.ScanLiteral"/> in that
        /// it also expands entities.
        /// </remarks>
        private string ScanLiteral(StringBuilder sb, char quote)
        {
            sb.Length = 0;
            char ch = this.m_current.ReadChar();
            while (ch != Entity.EOF && ch != quote && ch != '>')
            {
                if (ch == '&')
                {
                    ExpandEntity(sb, quote);
                    ch = this.m_current.Lastchar;
                }               
                else
                {
                    sb.Append(ch);
                    ch = this.m_current.ReadChar();
                }
            }
            if(ch == quote) {
                this.m_current.ReadChar(); // consume end quote.
            }
            return sb.ToString();
        }

        private bool ParseCData()
        {
            // Like ParseText(), only it doesn't allow elements in the content.  
            // It allows comments and processing instructions and text only and
            // text is not returned as text but CDATA (since it may contain angle brackets).
            // And initial whitespace is ignored.  It terminates when we hit the
            // end tag for the current CDATA node (e.g. </style>).
            bool ws = this.m_current.IsWhitespace;
            this.m_sb.Length = 0;
            char ch = this.m_current.Lastchar;
            if (this.m_partial != '\0')
            {
                Pop(); // pop the CDATA
                switch (this.m_partial)
                {
                    case '!':
                        this.m_partial = ' '; // and pop the comment next time around
                        return ParseComment();
                    case '?':
                        this.m_partial = ' '; // and pop the PI next time around
                        return ParsePI();
                    case '/':
                        this.m_state = State.EndTag;
                        return true;    // we are done!
                    case ' ':
                        break; // means we just needed to pop the Comment, PI or CDATA.
                }
            }            
            
            // if this.partial == '!' then parse the comment and return
            // if this.partial == '?' then parse the processing instruction and return.            
            while (ch != Entity.EOF)
            {
                if (ch == '<')
                {
                    ch = this.m_current.ReadChar();
                    if (ch == '!')
                    {
                        ch = this.m_current.ReadChar();
                        if (ch == '-')
                        {
                            // return what CDATA we have accumulated so far
                            // then parse the comment and return to here.
                            if (ws)
                            {
                                this.m_partial = ' '; // pop comment next time through
                                return ParseComment();
                            } 
                            else
                            {
                                // return what we've accumulated so far then come
                                // back in and parse the comment.
                                this.m_partial = '!';
                                break; 
                            }
#if FIX
                        } else if (ch == '['){
                            // We are about to wrap this node as a CDATA block because of it's
                            // type in the DTD, but since we found a CDATA block in the input
                            // we have to parse it as a CDATA block, otherwise we will attempt
                            // to output nested CDATA blocks which of course is illegal.
                            if (this.ParseConditionalBlock()){
                                this.partial = ' ';
                                return true;
                            }
#endif
                        }
                        else
                        {
                            // not a comment, so ignore it and continue on.
                            this.m_sb.Append('<');
                            this.m_sb.Append('!');
                            this.m_sb.Append(ch);
                            ws = false;
                        }
                    } 
                    else if (ch == '?')
                    {
                        // processing instruction.
                        this.m_current.ReadChar();// consume the '?' character.
                        if (ws)
                        {
                            this.m_partial = ' '; // pop PI next time through
                            return ParsePI();
                        } 
                        else
                        {
                            this.m_partial = '?';
                            break;
                        }
                    }
                    else if (ch == '/')
                    {
                        // see if this is the end tag for this CDATA node.
                        string temp = this.m_sb.ToString();
                        if (ParseEndTag() && string.Equals(this.m_endTag, this.m_node.Name, StringComparison.OrdinalIgnoreCase))
                        {
                            if (ws || string.IsNullOrEmpty(temp))
                            {
                                // we are done!
                                return true;
                            } 
                            else
                            {
                                // return CDATA text then the end tag
                                this.m_partial = '/';
                                this.m_sb.Length = 0; // restore buffer!
                                this.m_sb.Append(temp);
                                this.m_state = State.CData;
                                break;
                            }
                        } 
                        else
                        {
                            // wrong end tag, so continue on.
                            this.m_sb.Length = 0; // restore buffer!
                            this.m_sb.Append(temp);
                            this.m_sb.Append("</" + this.m_endTag + ">");
                            ws = false;

                            // NOTE (steveb): we have one character in the buffer that we need to process next
                            ch = this.m_current.Lastchar;
                            continue;
                        }
                    }
                    else
                    {
                        // must be just part of the CDATA block, so proceed.
                        this.m_sb.Append('<');
                        this.m_sb.Append(ch);
                        ws = false;
                    }
                } 
                else
                {
                    if (!this.m_current.IsWhitespace && ws)
                        ws = false;
                    this.m_sb.Append(ch);
                }

                ch = this.m_current.ReadChar();
            }

            // NOTE (steveb): check if we reached EOF, which means it's over
            if(ch == Entity.EOF) {
                this.m_state = State.Eof;
                return false;
            }

            string value = this.m_sb.ToString();

            // NOTE (steveb): replace any nested CDATA sections endings
            value = value.Replace("<![CDATA[", string.Empty);
            value = value.Replace("]]>", string.Empty);
            value = value.Replace("/**/", string.Empty);

            Push(null, XmlNodeType.CDATA, value);
            if (this.m_partial == '\0')
                this.m_partial = ' ';// force it to pop this CDATA next time in.

            return true;
        }

        private void ExpandEntity(StringBuilder sb, char terminator)
        {
            char ch = this.m_current.ReadChar();
            if (ch == '#')
            {
                string charent = this.m_current.ExpandCharEntity();
                sb.Append(charent);
                ch = this.m_current.Lastchar;
            } 
            else
            {
                this.m_name.Length = 0;
                while (ch != Entity.EOF &&
                    (char.IsLetter(ch) || ch == '_' || ch == '-') || ((this.m_name.Length > 0) && char.IsDigit(ch)))
                {
                    this.m_name.Append(ch);
                    ch = this.m_current.ReadChar();
                }
                string name = this.m_name.ToString();

                // TODO (steveb): don't lookup amp, gt, lt, quote
                switch(name) {
                case "amp":
                    sb.Append("&");
                    if(ch != terminator && ch != '&' && ch != Entity.EOF)
                        ch = this.m_current.ReadChar();
                    return;
                case "lt":
                    sb.Append("<");
                    if(ch != terminator && ch != '&' && ch != Entity.EOF)
                        ch = this.m_current.ReadChar();
                    return;
                case "gt":
                    sb.Append(">");
                    if(ch != terminator && ch != '&' && ch != Entity.EOF)
                        ch = this.m_current.ReadChar();
                    return;
                case "quot":
                    sb.Append("\"");
                    if(ch != terminator && ch != '&' && ch != Entity.EOF)
                        ch = this.m_current.ReadChar();
                    return;
                case "apos":
                    sb.Append("'");
                    if(ch != terminator && ch != '&' && ch != Entity.EOF)
                        ch = this.m_current.ReadChar();
                    return;
                }

                if (this.m_dtd != null && !string.IsNullOrEmpty(name))
                {
                    Entity e = (Entity)this.m_dtd.FindEntity(name);
                    if (e != null)
                    {
                        if (e.IsInternal)
                        {
                            sb.Append(e.Literal);
                            if (ch != terminator && ch != '&' && ch != Entity.EOF)
                                ch = this.m_current.ReadChar();

                            return;
                        } 
                        else
                        {
                            Entity ex = new Entity(name, e.PublicId, e.Uri, this.m_current.Proxy);
                            e.Open(this.m_current, new Uri(e.Uri));
                            this.m_current = ex;
                            this.m_current.ReadChar();
                            return;
                        }
                    } 
                    else
                    {
                        Log("Undefined entity '{0}'", name);
                    }
                }
                // Entity is not defined, so just keep it in with the rest of the
                // text.
                sb.Append("&");
                sb.Append(name);
                if(ch != terminator && ch != '&' && ch != Entity.EOF)
                {
                    sb.Append(ch);
                    ch = this.m_current.ReadChar();
                }
            }
        }

        /// <summary>
        /// Gets a value indicating whether the reader is positioned at the end of the stream.
        /// </summary>
        /// <value>true if the reader is positioned at the end of the stream; otherwise, false.</value>
        public override bool EOF
        {
            get
            {
                return this.m_state == State.Eof;
            }
        }

        /// <summary>
        /// Changes the <see cref="ReadState"/> to Closed.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (this.m_current != null)
            {
                this.m_current.Close();
                this.m_current = null;
            }

            if (this.m_log != null)
            {
                this.m_log.Dispose();
                this.m_log = null;
            }
            base.Dispose(disposing);
        }

        /// <summary>
        /// Gets the state of the reader.
        /// </summary>
        /// <value>One of the ReadState values.</value>
        public override ReadState ReadState
        {
            get
            {
                if (this.m_state == State.Initial)
                    return ReadState.Initial;
                else if (this.m_state == State.Eof)
                    return ReadState.EndOfFile;
                else
                    return ReadState.Interactive;
            }
        }

        /// <summary>
        /// Reads the contents of an element or text node as a string.
        /// </summary>
        /// <returns>The contents of the element or an empty string.</returns>
        public string ReadString()
        {
            if (this.m_node.NodeType == XmlNodeType.Element)
            {
                this.m_sb.Length = 0;
                while (Read())
                {
                    switch (this.NodeType)
                    {
                        case XmlNodeType.CDATA:
                        case XmlNodeType.SignificantWhitespace:
                        case XmlNodeType.Whitespace:
                        case XmlNodeType.Text:
                            this.m_sb.Append(this.m_node.Value);
                            break;
                        default:
                            return this.m_sb.ToString();
                    }
                }

                return this.m_sb.ToString();
            }

            return this.m_node.Value;
        }

        /// <summary>
        /// Reads all the content, including markup, as a string.
        /// </summary>
        /// <returns>
        /// All the XML content, including markup, in the current node. If the current node has no children,
        /// an empty string is returned. If the current node is neither an element nor attribute, an empty
        /// string is returned.
        /// </returns>
        public override string ReadInnerXml()
        {
            throw new NotImplementedException();
            //StringWriter sw = new StringWriter(CultureInfo.InvariantCulture);
            //XmlTextWriter xw = new XmlTextWriter(sw);
            //xw.Formatting = Formatting.Indented;
            //switch (this.NodeType)
            //{
            //    case XmlNodeType.Element:
            //        Read();
            //        while (!this.EOF && this.NodeType != XmlNodeType.EndElement)
            //        {
            //            xw.WriteNode(this, true);
            //        }
            //        Read(); // consume the end tag
            //        break;
            //    case XmlNodeType.Attribute:
            //        sw.Write(this.Value);
            //        break;
            //    default:
            //        // return empty string according to XmlReader spec.
            //        break;
            //}

            //xw.Close();
            //return sw.ToString();
        }

        /// <summary>
        /// Reads the content, including markup, representing this node and all its children.
        /// </summary>
        /// <returns>
        /// If the reader is positioned on an element or an attribute node, this method returns all the XML content, including markup, of the current node and all its children; otherwise, it returns an empty string.
        /// </returns>
        public override string ReadOuterXml()
        {
            throw new NotImplementedException();
            //StringWriter sw = new StringWriter(CultureInfo.InvariantCulture);
            //XmlTextWriter xw = new XmlTextWriter(sw);
            //xw.Formatting = Formatting.Indented;
            //xw.WriteNode(this, true);
            //xw.Close();
            //return sw.ToString();
        }

        /// <summary>
        /// Gets the XmlNameTable associated with this implementation.
        /// </summary>
        /// <value>The XmlNameTable enabling you to get the atomized version of a string within the node.</value>
        public override XmlNameTable NameTable
        {
            get
            {
                return null;
            }
        }

        /// <summary>
        /// Resolves a namespace prefix in the current element's scope.
        /// </summary>
        /// <param name="prefix">The prefix whose namespace URI you want to resolve. To match the default namespace, pass an empty string.</param>
        /// <returns>The namespace URI to which the prefix maps or a null reference (Nothing in Visual Basic) if no matching prefix is found.</returns>
        public override string LookupNamespace(string prefix)
        {
            return null; // there are no namespaces in SGML.
        }

        /// <summary>
        /// Resolves the entity reference for EntityReference nodes.
        /// </summary>
        /// <exception cref="InvalidOperationException">SgmlReader does not resolve or return entities.</exception>
        public override void ResolveEntity()
        {
            // We never return any entity reference nodes, so this should never be called.
            throw new InvalidOperationException("Not on an entity reference.");
        }

        /// <summary>
        /// Parses the attribute value into one or more Text, EntityReference, or EndEntity nodes.
        /// </summary>
        /// <returns>
        /// true if there are nodes to return. false if the reader is not positioned on an attribute node when the initial call is made or if all the
        /// attribute values have been read. An empty attribute, such as, misc="", returns true with a single node with a value of string.Empty.
        /// </returns>
        public override bool ReadAttributeValue()
        {
            if (this.m_state == State.Attr)
            {
                this.m_state = State.AttrValue;
                return true;
            }
            else if (this.m_state == State.AttrValue)
            {
                return false;
            }
            else
                throw new InvalidOperationException("Not on an attribute.");
        }   

        private void Validate(Node node)
        {
            if (this.m_dtd != null)
            {
                ElementDecl e = this.m_dtd.FindElement(node.Name);
                if (e != null)
                {
                    node.DtdType = e;
                    if (e.ContentModel.DeclaredContent == DeclaredContent.EMPTY)
                        node.IsEmpty = true;
                }
            }
        }

        private static void ValidateAttribute(Node node, Attribute a)
        {
            ElementDecl e = node.DtdType;
            if (e != null)
            {
                AttDef ad = e.FindAttribute(a.Name);
                if (ad != null)
                {
                    a.DtdType = ad;
                }
            }
        }

        private static bool ValidAttributeName(string name)
        {
            try
            {
                XmlConvert.VerifyNMTOKEN(name);
                int index = name.IndexOf(':');
                if (index >= 0)
                {
                    XmlConvert.VerifyNCName(name.Substring(index + 1));
                }

                return true;
            }
            catch (XmlException)
            {
                return false;
            }
            catch (ArgumentNullException)
            {
                // (steveb) this is probably a bug in XmlConvert.VerifyNCName when passing in an empty string
                return false;
            }
        }

        private void ValidateContent(Node node)
        {
            if (node.NodeType == XmlNodeType.Element)
            {
                if (!VerifyName(node.Name))
                {
                    Pop();
                    Push(null, XmlNodeType.Text, "<" + node.Name + ">");
                    return;
                }
            }

            if (this.m_dtd != null)
            {
                // See if this element is allowed inside the current element.
                // If it isn't, then auto-close elements until we find one
                // that it is allowed to be in.                                  
                string name = node.Name.ToUpperInvariant(); // DTD is in upper case
                int i = 0;
                int top = this.m_stack.Count - 2;
                if (node.DtdType != null) { 
                    // it is a known element, let's see if it's allowed in the
                    // current context.
                    for (i = top; i > 0; i--)
                    {
                        Node n = (Node)this.m_stack[i];
                        if (n.IsEmpty)
                            continue; // we'll have to pop this one
                        ElementDecl f = n.DtdType;
                        if (f != null)
                        {
                            if ((i == 2) && string.Equals(f.Name, "BODY", StringComparison.OrdinalIgnoreCase)) // NOTE (steveb): never close the BODY tag too early
                                break;
                            else if (string.Equals(f.Name, this.m_dtd.Name, StringComparison.OrdinalIgnoreCase))
                                break; // can't pop the root element.
                            else if (f.CanContain(name, this.m_dtd))
                            {
                                break;
                            }
                            else if (!f.EndTagOptional)
                            {
                                // If the end tag is not optional then we can't
                                // auto-close it.  We'll just have to live with the
                                // junk we've found and move on.
                                break;
                            }
                        } 
                        else
                        {
                            // Since we don't understand this tag anyway,
                            // we might as well allow this content!
                            break;
                        }
                    }
                }

                if (i == 0)
                {
                    // Tag was not found or is not allowed anywhere, ignore it and 
                    // continue on.
                    return;
                }
                else if (i < top)
                {
                    Node n = (Node)this.m_stack[top];
                    if (i == top - 1 && string.Equals(name, n.Name, StringComparison.OrdinalIgnoreCase))
                    {
                        // e.g. p not allowed inside p, not an interesting error.
                    }
                    else
                    {
#if DEBUG
                        string closing = "";
                        for (int k = top; k >= i+1; k--) {
                            if (closing != "") closing += ",";
                            Node n2 = (Node)this.m_stack[k];
                            closing += "<" + n2.Name + ">";
                        }
                        Log("Element '{0}' not allowed inside '{1}', closing {2}.", name, n.Name, closing);
#endif
                    }

                    this.m_state = State.AutoClose;
                    this.m_newnode = node;
                    Pop(); // save this new node until we pop the others
                    this.m_poptodepth = i + 1;
                }
            }
        }
    }
}