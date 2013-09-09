using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaconographyPortable.Services
{
    public interface IMarkdownProcessor
    {
        MarkdownData Process(string markdown);
    }
    public class MarkdownData
    {
        //public List<Tuple<bool, string, string>> ProcessedMarkdownBlock { get; set; }
        public object MarkdownDom { get; set; }
    }
}
