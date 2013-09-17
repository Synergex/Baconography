using BaconographyPortable.ViewModel;
using GalaSoft.MvvmLight;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaconographyPortable.Common
{
    public class NeverEndingRedditView
    {
        RedditViewModel _context;
        int _currentLinkPos;
        bool _forward;
        public NeverEndingRedditView(RedditViewModel context, int currentLinkPos, bool forward)
        {
            _context = context;
            _currentLinkPos = currentLinkPos;
            _forward = forward;
        }
        public async Task<ViewModelBase> Next()
        {
            if (_forward)
            {
                _currentLinkPos++;
                if (_context.Links.Count <= _currentLinkPos)
                {
                    await _context.Links.LoadMoreItemsAsync(100);
                }
            }
            else
                _currentLinkPos--;

            if (_context.Links.Count > _currentLinkPos && _currentLinkPos > 0)
                return _context.Links[_currentLinkPos];
            else
                return null;
        }
    }
}
