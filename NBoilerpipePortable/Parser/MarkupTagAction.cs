/*
 * This code is derived from boilerpipe
 * 
 */

using System.Collections.Generic;
using NBoilerpipePortable.Labels;

using HtmlAgilityPack;
using Sharpen;
using System.Linq;

namespace Sharpen
{
    static class Collections
    {
        public static T[] ToArray<T>(ICollection<T> list)
        {
            T[] array = new T[list.Count];
            list.CopyTo(array, 0);
            return array;
        }
        public static U[] ToArray<T, U>(ICollection<T> list, U[] res) where T : U
        {
            if (res.Length < list.Count)
                res = new U[list.Count];

            int n = 0;
            foreach (T t in list)
                res[n++] = t;

            if (res.Length > list.Count)
                res[list.Count] = default(T);
            return res;
        }
    }
}

namespace NBoilerpipePortable.Parser
{
	/// <summary>
	/// Assigns labels for element CSS classes and ids to the corresponding
	/// <see cref="NBoilerpipePortable.Document.TextBlock">NBoilerpipePortable.Document.TextBlock</see>
	/// . CSS classes are prefixed by
	/// <code>
	/// <see cref="NBoilerpipePortable.Labels.DefaultLabels.MARKUP_PREFIX">NBoilerpipePortable.Labels.DefaultLabels.MARKUP_PREFIX
	/// 	</see>
	/// .</code>, and IDs are prefixed by
	/// <code>
	/// <see cref="NBoilerpipePortable.Labels.DefaultLabels.MARKUP_PREFIX">NBoilerpipePortable.Labels.DefaultLabels.MARKUP_PREFIX
	/// 	</see>
	/// #</code>
	/// </summary>
	/// <author>Christian Kohlsch√ºtter</author>
	public sealed class MarkupTagAction : TagAction
	{
		private readonly bool isBlockLevel;

		private List<IList<string>> labelStack = new List<IList<string>>();

		public MarkupTagAction(bool isBlockLevel)
		{
			this.isBlockLevel = isBlockLevel;
		}

		private static readonly Sharpen.Pattern PAT_NUM = Sharpen.Pattern.Compile("[0-9]+"
			);

		/// <exception cref="Sharpen.SAXException"></exception>
		public bool Start (NBoilerpipeContentHandler instance, string localName, HtmlAttributeCollection atts)
		{
			IList<string> labels = new List<string> (5);
			labels.Add (DefaultLabels.MARKUP_PREFIX + localName);
			string classVal = atts ["class"].Value;
			if (classVal != null && classVal.Length > 0) {
				classVal = PAT_NUM.Matcher (classVal).ReplaceAll ("#");
				classVal = classVal.Trim ();
				string[] vals = classVal.Split ('[', ' ', ']', '+');
                labels.Add(DefaultLabels.MARKUP_PREFIX + "." + classVal.Replace(' ', '.'));
				if (vals.Length > 1) {
					foreach (string s in vals) {
                        labels.Add(DefaultLabels.MARKUP_PREFIX + "." + s);
					}
				}
			}
			var att = atts["id"];
			var id =  ( atts !=null) ? att.Name : "";
			if (id != null && id.Length > 0) {
				id = PAT_NUM.Matcher (id).ReplaceAll ("#");
                labels.Add(DefaultLabels.MARKUP_PREFIX + "#" + id);
			}
			ICollection<string> ancestors = GetAncestorLabels ();
			IList<string> labelsWithAncestors = new List<string> ((ancestors.Count + 1) * labels
				.Count);
			foreach (string l in labels) {
				foreach (string an in ancestors) {
                    labelsWithAncestors.Add(an);
                    labelsWithAncestors.Add(an + " " + l);
				}
                labelsWithAncestors.Add(l);
			}
			instance.AddLabelAction (new LabelAction (Sharpen.Collections.ToArray (labelsWithAncestors
				, new string[labelsWithAncestors.Count])));
			labelStack.AddItem (labels);
			return isBlockLevel;
		}

		/// <exception cref="Sharpen.SAXException"></exception>
		public bool End(NBoilerpipeContentHandler instance, string localName)
		{
			labelStack.RemoveLast();
			return isBlockLevel;
		}

		public bool ChangesTagLevel()
		{
			return isBlockLevel;
		}

		private ICollection<string> GetAncestorLabels()
		{
			return new HashSet<string>(labelStack.Where(lst => lst != null).SelectMany(lst => lst));
		}
	}
}
