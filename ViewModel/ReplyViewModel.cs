using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using Baconography.RedditAPI;
using Baconography.RedditAPI.Actions;
using Baconography.RedditAPI.Things;
using Baconography.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Baconography.ViewModel
{
    public class ReplyViewModel : ViewModelBase
    {
        Thing _replyTargetThing;
        IUsersService _userService;
        IRedditActionQueue _actionQueue;
        Action<Thing> _convertIntoUIReply;
        public ReplyViewModel(Thing replyTargetThing, IUsersService userService, IRedditActionQueue actionQueue, RelayCommand cancel, Action<Thing> convertIntoUIReply)
        {
            _convertIntoUIReply = convertIntoUIReply;
            _cancel = cancel;
            _actionQueue = actionQueue;
            _replyTargetThing = replyTargetThing;
            _userService = userService;
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

            var selectedText = startText.Substring(startPosition, endPosition - startPosition);

            string splitter = "\n";
            if(selectedText.Contains("\r\n"))
            {
                splitter = "\r\n";
            }

            var preText = startPosition == 0 ? "" : startText.Substring(0, startPosition);
            var postText = endPosition == startText.Length ? "" : startText.Substring(endPosition + 1);

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

        RelayCommand _addBold;
        public RelayCommand AddBold
        {
            get
            {
                if (_addBold == null)
                {
                    _addBold = new RelayCommand(() =>
                        {
                            var surroundedTextTpl = SurroundSelection(SelectionStart, SelectionStart + SelectionLength, ReplyBody, _boldFormattingString);
                            ReplyBody = surroundedTextTpl.Item3;
                            SelectionStart = surroundedTextTpl.Item1;
                            SelectionLength = surroundedTextTpl.Item2 - surroundedTextTpl.Item1;
                        });
                }
                return _addBold;
            }
        }

        private string _italicFormattingString = "*{0}*";

        RelayCommand _addItalic;
        public RelayCommand AddItalic
        {
            get
            {
                if (_addItalic == null)
                {
                    _addItalic = new RelayCommand(() =>
                    {
                        var surroundedTextTpl = SurroundSelection(SelectionStart, SelectionStart + SelectionLength, ReplyBody, _italicFormattingString);
                        ReplyBody = surroundedTextTpl.Item3;
                        SelectionStart = surroundedTextTpl.Item1;
                        SelectionLength = surroundedTextTpl.Item2 - surroundedTextTpl.Item1;
                    });
                }
                return _addItalic;
            }
        }

        private string _strikeFormattingString = "~~{0}~~";

        RelayCommand _addStrike;
        public RelayCommand AddStrike
        {
            get
            {
                if (_addStrike == null)
                {
                    _addStrike = new RelayCommand(() =>
                    {
                        var surroundedTextTpl = SurroundSelection(SelectionStart, SelectionStart + SelectionLength, ReplyBody, _strikeFormattingString);
                        ReplyBody = surroundedTextTpl.Item3;
                        SelectionStart = surroundedTextTpl.Item1;
                        SelectionLength = surroundedTextTpl.Item2 - surroundedTextTpl.Item1;
                    });
                }
                return _addStrike;
            }
        }

        private string _superFormattingString = "^{0}";

        RelayCommand _addSuper;
        public RelayCommand AddSuper
        {
            get
            {
                if (_addSuper == null)
                {
                    _addSuper = new RelayCommand(() =>
                    {
                        var surroundedTextTpl = SurroundSelection(SelectionStart, SelectionStart + SelectionLength, ReplyBody, _superFormattingString);
                        ReplyBody = surroundedTextTpl.Item3;
                        SelectionStart = surroundedTextTpl.Item1;
                        SelectionLength = surroundedTextTpl.Item2 - surroundedTextTpl.Item1;
                    });
                }
                return _addSuper;
            }
        }

        private string _linkFormattingString = "[{0}](the-url-goes-here)";

        RelayCommand _addLink;
        public RelayCommand AddLink
        {
            get
            {
                if (_addLink == null)
                {
                    _addLink = new RelayCommand(() =>
                    {
                        var surroundedTextTpl = SurroundSelection(SelectionStart, SelectionStart + SelectionLength, ReplyBody, _linkFormattingString);
                        ReplyBody = surroundedTextTpl.Item3;
                        SelectionStart = surroundedTextTpl.Item1;
                        SelectionLength = surroundedTextTpl.Item2 - surroundedTextTpl.Item1;
                    });
                }
                return _addLink;
            }
        }

        private string _quoteFormattingString = ">{0}";

        RelayCommand _addQuote;
        public RelayCommand AddQuote
        {
            get
            {
                if (_addQuote == null)
                {
                    _addQuote = new RelayCommand(() =>
                    {
                        var surroundedTextTpl = SurroundSelection(SelectionStart, SelectionStart + SelectionLength, ReplyBody, _quoteFormattingString);
                        ReplyBody = surroundedTextTpl.Item3;
                        SelectionStart = surroundedTextTpl.Item1;
                        SelectionLength = surroundedTextTpl.Item2 - surroundedTextTpl.Item1;
                    });
                }
                return _addQuote;
            }
        }

        private string _codeFormattingString = "    {0}";

        RelayCommand _addCode;
        public RelayCommand AddCode
        {
            get
            {
                if (_addCode == null)
                {
                    _addCode = new RelayCommand(() =>
                    {
                        var surroundedTextTpl = SurroundSelection(SelectionStart, SelectionStart + SelectionLength, ReplyBody, _codeFormattingString);
                        ReplyBody = surroundedTextTpl.Item3;
                        SelectionStart = surroundedTextTpl.Item1;
                        SelectionLength = surroundedTextTpl.Item2 - surroundedTextTpl.Item1;
                    });
                }
                return _addCode;
            }
        }

        private string _bulletFormattingString = "*{0}";

        RelayCommand _addBullets;
        public RelayCommand AddBullets
        {
            get
            {
                if (_addBullets == null)
                {
                    _addBullets = new RelayCommand(() =>
                    {
                        var surroundedTextTpl = SurroundSelection(SelectionStart, SelectionStart + SelectionLength, ReplyBody, _bulletFormattingString);
                        ReplyBody = surroundedTextTpl.Item3;
                        SelectionStart = surroundedTextTpl.Item1;
                        SelectionLength = surroundedTextTpl.Item2 - surroundedTextTpl.Item1;
                    });
                }
                return _addBullets;
            }
        }

        private string _numberFormattingString = "1. {0}";

        RelayCommand _addNumbers;
        public RelayCommand AddNumbers
        {
            get
            {
                if (_addNumbers == null)
                {
                    _addNumbers = new RelayCommand(() =>
                    {
                        var surroundedTextTpl = SurroundSelection(SelectionStart, SelectionStart + SelectionLength, ReplyBody, _numberFormattingString);
                        ReplyBody = surroundedTextTpl.Item3;
                        SelectionStart = surroundedTextTpl.Item1;
                        SelectionLength = surroundedTextTpl.Item2 - surroundedTextTpl.Item1;
                    });
                }
                return _addNumbers;
            }
        }

        RelayCommand _submit;
        public RelayCommand Submit
        {
            get
            {
                if (_submit == null)
                {
                    _submit = new RelayCommand(() =>
                    {
                        var commentAction = new AddComment { Content = ReplyBody, ParentId = ((dynamic)_replyTargetThing.Data).Name };
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
                        _actionQueue.AddAction(commentAction);
                    });
                }
                return _submit;
            }
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
