using BaconographyPortable.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaconographyW8.PlatformServices
{
    public class MarkdownProcessor : IMarkdownProcessor
    {
        public MarkdownData Process(string markdown)
        {
            var processed = SnuDom.SnuDom.MarkdownToDOM(markdown);
            return new MarkdownData { MarkdownDom = processed };
        }
    }
}
