using System.Reflection;
using System.Linq;
using System.Threading;
using System.Collections.Specialized;
using System.Collections.Generic;
using NBoilerpipePortable;
using NBoilerpipePortable.Document;


namespace Sharpen
{
    //using ICSharpCode.SharpZipLib.Zip.Compression;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Runtime.CompilerServices;
    using System.Text;
    using System.Text.RegularExpressions;

    public static class Extensions
    {
        private static readonly long EPOCH_TICKS;

        static Extensions()
        {
            DateTime time = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            EPOCH_TICKS = time.Ticks;
        }

        public static void Add<T>(this IList<T> list, int index, T item)
        {
            list.Insert(index, item);
        }

        public static StringBuilder AppendRange(this StringBuilder sb, string str, int start, int end)
        {
            return sb.Append(str, start, end - start);
        }

        public static StringBuilder Delete(this StringBuilder sb, int start, int end)
        {
            return sb.Remove(start, end - start);
        }

        public static void SetCharAt(this StringBuilder sb, int index, char c)
        {
            sb[index] = c;
        }

        public static int IndexOf(this StringBuilder sb, string str)
        {
            return sb.ToString().IndexOf(str);
        }

        public static int BitCount(int val)
        {
            uint num = (uint)val;
            int count = 0;
            for (int i = 0; i < 32; i++)
            {
                if ((num & 1) != 0)
                {
                    count++;
                }
                num >>= 1;
            }
            return count;
        }

        public static ICollection<KeyValuePair<T, U>> EntrySet<T, U>(this IDictionary<T, U> s)
        {
            return s;
        }

        //public static void Finish (this Inflater i)
        //{
        //}

        public static IList<T> SubList<T>(this IList<T> list, int start, int len)
        {
            List<T> sublist = new List<T>(len);
            for (int i = start; i < (start + len) && i < list.Count; i++)
            {
                sublist.Add(list[i - start]);
            }
            return sublist;
        }

        public static bool AddItem<T>(this IList<T> list, T item)
        {
            list.Add(item);
            return true;
        }

        public static bool AddItem<T>(this ICollection<T> list, T item)
        {
            list.Add(item);
            return true;
        }

        public static U Get<T, U>(this IDictionary<T, U> d, T key)
        {
            U val;
            d.TryGetValue(key, out val);
            return val;
        }

        public static U Put<T, U>(this IDictionary<T, U> d, T key, U value)
        {
            U old;
            d.TryGetValue(key, out old);
            d[key] = value;
            return old;
        }

        public static void PutAll<T, U>(this IDictionary<T, U> d, IDictionary<T, U> values)
        {
            foreach (KeyValuePair<T, U> val in values)
                d[val.Key] = val.Value;
        }

        

       

        public static bool IsEmpty<T>(this ICollection<T> col)
        {
            return (col.Count == 0);
        }

        public static bool IsEmpty<T>(this Stack<T> col)
        {
            return (col.Count == 0);
        }

        public static bool IsLower(this char c)
        {
            return char.IsLower(c);
        }

        public static bool IsUpper(this char c)
        {
            return char.IsUpper(c);
        }

        public static T Last<T>(this ICollection<T> col)
        {
            IList<T> list = col as IList<T>;
            if (list != null)
            {
                return list[list.Count - 1];
            }
            return col.Last<T>();
        }

        public static bool Matches(this string str, string regex)
        {
            Regex regex2 = new Regex(regex);
            return regex2.IsMatch(str);
        }

        public static DateTime CreateDate(long milliSecondsSinceEpoch)
        {
            long num = EPOCH_TICKS + (milliSecondsSinceEpoch * 10000);
            return new DateTime(num);
        }

        public static DateTimeOffset MillisToDateTimeOffset(long milliSecondsSinceEpoch, long offsetMinutes)
        {
            TimeSpan offset = TimeSpan.FromMinutes((double)offsetMinutes);
            long num = EPOCH_TICKS + (milliSecondsSinceEpoch * 10000);
            return new DateTimeOffset(num + offset.Ticks, offset);
        }

        public static T RemoveFirst<T>(this IList<T> list)
        {
            var first = list[0];
            list.RemoveAt(0);
            return first;
        }

        public static T RemoveLast<T>(this IList<T> list)
        {
            var last = list[list.Count - 1];
            list.RemoveAt(list.Count - 1);
            return last;
        }

        public static string ReplaceAll(this string str, string regex, string replacement)
        {
            Regex rgx = new Regex(regex);

            if (replacement.IndexOfAny(new char[] { '\\', '$' }) != -1)
            {
                // Back references not yet supported
                StringBuilder sb = new StringBuilder();
                for (int n = 0; n < replacement.Length; n++)
                {
                    char c = replacement[n];
                    if (c == '$')
                        throw new NotSupportedException("Back references not supported");
                    if (c == '\\')
                        c = replacement[++n];
                    sb.Append(c);
                }
                replacement = sb.ToString();
            }

            return rgx.Replace(str, replacement);
        }

        public static bool RegionMatches(this string str, bool ignoreCase, int toOffset, string other, int ooffset, int len)
        {
            if (toOffset < 0 || ooffset < 0 || toOffset + len > str.Length || ooffset + len > other.Length)
                return false;
            return string.Compare(str, toOffset, other, ooffset, len) == 0;
        }

        public static T Set<T>(this IList<T> list, int index, T item)
        {
            T old = list[index];
            list[index] = item;
            return old;
        }

        public static int Signum(long val)
        {
            if (val < 0)
            {
                return -1;
            }
            if (val > 0)
            {
                return 1;
            }
            return 0;
        }

        public static void RemoveAll<T, U>(this ICollection<T> col, ICollection<U> items) where U : T
        {
            foreach (var u in items)
                col.Remove(u);
        }

        public static bool ContainsAll<T, U>(this ICollection<T> col, ICollection<U> items) where U : T
        {
            foreach (var u in items)
                if (!col.Any(n => (object.ReferenceEquals(n, u)) || n.Equals(u)))
                    return false;
            return true;
        }

        public static bool Contains<T>(this ICollection<T> col, object item)
        {
            if (!(item is T))
                return false;
            return col.Any(n => (object.ReferenceEquals(n, item)) || n.Equals(item));
        }

        public static void Sort<T>(this IList<T> list)
        {
            List<T> sorted = new List<T>(list);
            sorted.Sort();
            for (int i = 0; i < list.Count; i++)
            {
                list[i] = sorted[i];
            }
        }

        public static void Sort<T>(this IList<T> list, IComparer<T> comparer)
        {
            List<T> sorted = new List<T>(list);
            sorted.Sort(comparer);
            for (int i = 0; i < list.Count; i++)
            {
                list[i] = sorted[i];
            }
        }

        public static string[] Split(this string str, string regex)
        {
            return str.Split(regex, 0);
        }

        public static string[] Split(this string str, string regex, int limit)
        {
            Regex rgx = new Regex(regex);
            List<string> list = new List<string>();
            int startIndex = 0;
            if (limit != 1)
            {
                int nm = 1;
                foreach (Match match in rgx.Matches(str))
                {
                    list.Add(str.Substring(startIndex, match.Index - startIndex));
                    startIndex = match.Index + match.Length;
                    if (limit > 0 && ++nm == limit)
                        break;
                }
            }
            if (startIndex < str.Length)
            {
                list.Add(str.Substring(startIndex));
            }
            if (limit >= 0)
            {
                int count = list.Count - 1;
                while ((count >= 0) && (list[count].Length == 0))
                {
                    count--;
                }
                list.RemoveRange(count + 1, (list.Count - count) - 1);
            }
            return list.ToArray();
        }

        public static bool StartsWith(this string str, string prefix, int offset)
        {
            return str.Substring(offset).StartsWith(prefix);
        }

        public static CharSequence SubSequence(this string str, int start, int end)
        {
            return (CharSequence)str.Substring(start, end);
        }

        public static char[] ToCharArray(this string str)
        {
            char[] destination = new char[str.Length];
            str.CopyTo(0, destination, 0, str.Length);
            return destination;
        }

    }
}

namespace NBoilerpipePortable.Filters.Simple
{
	/// <summary>
	/// Reverts the "isContent" flag for all
	/// <see cref="NBoilerpipePortable.Document.TextBlock">NBoilerpipePortable.Document.TextBlock</see>
	/// s
	/// </summary>
	/// <author>Christian Kohlsch√ºtter</author>
	public sealed class InvertedFilter : BoilerpipeFilter
	{
		public static readonly NBoilerpipePortable.Filters.Simple.InvertedFilter INSTANCE = new NBoilerpipePortable.Filters.Simple.InvertedFilter
			();

		public InvertedFilter()
		{
		}

		/// <exception cref="NBoilerpipePortable.BoilerpipeProcessingException"></exception>
		public bool Process(TextDocument doc)
		{
			IList<TextBlock> tbs = doc.GetTextBlocks();
			if (tbs.Count == 0)
			{
				return false;
			}
			foreach (TextBlock tb in tbs)
			{
				tb.SetIsContent(!tb.IsContent());
			}
			return true;
		}
	}
}
