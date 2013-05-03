using BaconographyPortable.Model.Reddit;
using BaconographyPortable.Services;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaconographyPortable.ViewModel
{
    public class ReplyViewModel : ViewModelBase
    {
        Thing _replyTargetThing;
        IBaconProvider _baconProvider;
        IUserService _userService;
        IRedditService _redditService;
        Action<Thing> _convertIntoUIReply;
        public ReplyViewModel(IBaconProvider baconProvider, Thing replyTargetThing, RelayCommand cancel, Action<Thing> convertIntoUIReply)
        {
            _convertIntoUIReply = convertIntoUIReply;
            _cancel = cancel;
            _baconProvider = baconProvider;
            _redditService = _baconProvider.GetService<IRedditService>();
            _userService = _baconProvider.GetService<IUserService>();
            _replyTargetThing = replyTargetThing;
            
            var userServiceTask = _userService.GetUser();
            userServiceTask.Wait();

            if (string.IsNullOrWhiteSpace(userServiceTask.Result.Username))
            {
                IsLoggedIn = false;
                CommentingAs = string.Empty;
            }
            else
            {
                CommentingAs = userServiceTask.Result.Username;
                IsLoggedIn = true;
            }

            _addBold = new RelayCommand(AddBoldImpl);
            _addItalic = new RelayCommand(AddItalicImpl);
            _addStrike = new RelayCommand(AddStrikeImpl);
            _addSuper = new RelayCommand(AddSuperImpl);
            _addLink = new RelayCommand(AddLinkImpl);
            _addQuote = new RelayCommand(AddQuoteImpl);
            _addCode = new RelayCommand(AddCodeImpl);
            _addBullets = new RelayCommand(AddBulletsImpl);
            _addNumbers = new RelayCommand(AddNumbersImpl);
            _submit = new RelayCommand(SubmitImpl);

        }

        private int _selectionLength;
        public int SelectionLength
        {
            get
            {
                return _selectionLength;
            }
            set
            {
                _selectionLength = value;
                RaisePropertyChanged("SelectionLength");
            }
        }

        private int _selectionStart;
        public int SelectionStart
        {
            get
            {
                return _selectionStart;
            }
            set
            {
                _selectionStart = value;
                RaisePropertyChanged("SelectionStart");
            }
        }

        private bool _isLoggedIn;
        public bool IsLoggedIn
        {
            get
            {
                return _isLoggedIn;
            }
            set
            {
                _isLoggedIn = value;
                RaisePropertyChanged("IsLoggedIn");
            }
        }

        private string _commentingAs;
        public string CommentingAs
        {
            get
            {
                return _commentingAs;
            }
            set
            {
                _commentingAs = value;
                RaisePropertyChanged("CommentingAs");
            }
        }

        private string _replyBody;
        public string ReplyBody
        {
            get
            {
                return _replyBody;
            }
            set
            {
                _replyBody = value;
                RaisePropertyChanged("ReplyBody");
            }
        }

        private Tuple<int, int, string> SurroundSelection(int startPosition, int endPosition, string startText, string newTextFormat)
        {
            //split selection into multiple lines
            //for each line in the selection we apply its body to the newTextFormat string via string.format
            //if we only had a single line return the selection span as the modified position of just the original text
            //if we had multiple lines the selection span should be the entire replace string block

            if (string.IsNullOrEmpty(startText))
                startPosition = endPosition = 0;
            
            var selectedText = string.IsNullOrEmpty(startText) ? "" : startText.Substring(startPosition, endPosition - startPosition);

            string splitter = "\n";
            if (selectedText.Contains("\r\n"))
            {
                splitter = "\r\n";
            }

            var preText = (string.IsNullOrEmpty(startText) || startPosition == 0) ? "" : startText.Substring(0, startPosition);
            var postText = (string.IsNullOrEmpty(startText) || endPosition == startText.Length) ? "" : startText.Substring(endPosition + 1);

            var selectedTextLines = selectedText.Split(new string[] { splitter }, StringSplitOptions.None);
            if (selectedTextLines.Length > 1)
            {
                var formattedText = string.Join(splitter, selectedTextLines.Select(str => string.Format(newTextFormat, str)));
                var newText = preText + formattedText + postText;
                return Tuple.Create(startPosition, startPosition + formattedText.Length, newText);
            }
            else
            {
                var newText = preText + string.Format(newTextFormat, selectedText) + postText;
                var formatOffset = newTextFormat.IndexOf("{0}");
                return Tuple.Create(startPosition + formatOffset, endPosition + formatOffset, newText);
            }

        }

        private string _boldFormattingString = "**{0}**";
        private string _italicFormattingString = "*{0}*";
        private string _strikeFormattingString = "~~{0}~~";
        private string _superFormattingString = "^{0}";
        private string _linkFormattingString = "[{0}](the-url-goes-here)";
        private string _quoteFormattingString = ">{0}";
        private string _codeFormattingString = "    {0}";
        private string _bulletFormattingString = "*{0}";
        private string _numberFormattingString = "1. {0}";

        public RelayCommand AddBold { get { return _addBold; } }
        public RelayCommand AddItalic { get { return _addItalic; } }
        public RelayCommand AddStrike { get { return _addStrike; } }
        public RelayCommand AddSuper { get { return _addSuper; } }
        public RelayCommand AddLink { get { return _addLink; } }
        public RelayCommand AddQuote { get { return _addQuote; } }
        public RelayCommand AddCode { get { return _addCode; } }
        public RelayCommand AddBullets { get { return _addBullets; } }
        public RelayCommand AddNumbers { get { return _addNumbers; } }
        public RelayCommand Submit { get { return _submit; } }

        RelayCommand _addBold;
        RelayCommand _addItalic;
        RelayCommand _addStrike;
        RelayCommand _addSuper;
        RelayCommand _addLink;
        RelayCommand _addQuote;
        RelayCommand _addCode;
        RelayCommand _addBullets;
        RelayCommand _addNumbers;
        RelayCommand _submit;
        
        private void AddBoldImpl()
        {
            var surroundedTextTpl = SurroundSelection(SelectionStart, SelectionStart + SelectionLength, ReplyBody, _boldFormattingString);
            ReplyBody = surroundedTextTpl.Item3;
            SelectionStart = surroundedTextTpl.Item1;
            SelectionLength = surroundedTextTpl.Item2 - surroundedTextTpl.Item1;
        }

        private void AddItalicImpl()
        {
            var surroundedTextTpl = SurroundSelection(SelectionStart, SelectionStart + SelectionLength, ReplyBody, _italicFormattingString);
            ReplyBody = surroundedTextTpl.Item3;
            SelectionStart = surroundedTextTpl.Item1;
            SelectionLength = surroundedTextTpl.Item2 - surroundedTextTpl.Item1;
        }

        private void AddStrikeImpl()
        {
            var surroundedTextTpl = SurroundSelection(SelectionStart, SelectionStart + SelectionLength, ReplyBody, _strikeFormattingString);
            ReplyBody = surroundedTextTpl.Item3;
            SelectionStart = surroundedTextTpl.Item1;
            SelectionLength = surroundedTextTpl.Item2 - surroundedTextTpl.Item1;
        }

        private void AddSuperImpl()
        {
            var surroundedTextTpl = SurroundSelection(SelectionStart, SelectionStart + SelectionLength, ReplyBody, _superFormattingString);
            ReplyBody = surroundedTextTpl.Item3;
            SelectionStart = surroundedTextTpl.Item1;
            SelectionLength = surroundedTextTpl.Item2 - surroundedTextTpl.Item1;
        }

        private void AddLinkImpl()
        {
            var surroundedTextTpl = SurroundSelection(SelectionStart, SelectionStart + SelectionLength, ReplyBody, _linkFormattingString);
            ReplyBody = surroundedTextTpl.Item3;
            SelectionStart = surroundedTextTpl.Item1;
            SelectionLength = surroundedTextTpl.Item2 - surroundedTextTpl.Item1;
        }

        private void AddQuoteImpl()
        {
            var surroundedTextTpl = SurroundSelection(SelectionStart, SelectionStart + SelectionLength, ReplyBody, _quoteFormattingString);
            ReplyBody = surroundedTextTpl.Item3;
            SelectionStart = surroundedTextTpl.Item1;
            SelectionLength = surroundedTextTpl.Item2 - surroundedTextTpl.Item1;
        }

        private void AddCodeImpl()
        {
            var surroundedTextTpl = SurroundSelection(SelectionStart, SelectionStart + SelectionLength, ReplyBody, _codeFormattingString);
            ReplyBody = surroundedTextTpl.Item3;
            SelectionStart = surroundedTextTpl.Item1;
            SelectionLength = surroundedTextTpl.Item2 - surroundedTextTpl.Item1;
        }

        private void AddBulletsImpl()
        {
            var surroundedTextTpl = SurroundSelection(SelectionStart, SelectionStart + SelectionLength, ReplyBody, _bulletFormattingString);
            ReplyBody = surroundedTextTpl.Item3;
            SelectionStart = surroundedTextTpl.Item1;
            SelectionLength = surroundedTextTpl.Item2 - surroundedTextTpl.Item1;
        }

        private void AddNumbersImpl()
        {
            var surroundedTextTpl = SurroundSelection(SelectionStart, SelectionStart + SelectionLength, ReplyBody, _numberFormattingString);
            ReplyBody = surroundedTextTpl.Item3;
            SelectionStart = surroundedTextTpl.Item1;
            SelectionLength = surroundedTextTpl.Item2 - surroundedTextTpl.Item1;
        }

        private void SubmitImpl()
        {
            _redditService.AddComment(((dynamic)_replyTargetThing.Data).Name, ReplyBody);
            var theComment = new Thing
            {
                Kind = "t2",
                Data = new Comment
                {
                    Author = string.IsNullOrWhiteSpace(CommentingAs) ? "self" : CommentingAs,
                    Body = ReplyBody,
                    Likes = true,
                    Ups = 1,
                    ParentId = ((dynamic)_replyTargetThing.Data).Name,
                    Replies = new Listing { Data = new ListingData { Children = new List<Thing>() } },
                    Created = DateTime.Now,
                    CreatedUTC = DateTime.UtcNow
                }
            };
            _convertIntoUIReply(theComment);
            Cancel.Execute(null);
        }

        RelayCommand _cancel;
        public RelayCommand Cancel
        {
            get
            {
                return _cancel;
            }
        }
    }
}
