using BaconographyPortable.Common;
using BaconographyPortable.Messages;
using BaconographyPortable.Model.Reddit;
using BaconographyPortable.Services;
using BaconographyPortable.ViewModel.Collections;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaconographyPortable.ViewModel
{
    public class MessageViewModel : ViewModelBase
    {
        IBaconProvider _baconProvider;
        IUserService _userService;
        IRedditService _redditService;
        INavigationService _navigationService;
        IDynamicViewLocator _dynamicViewLocator;
        TypedThing<Message> _message;

        public MessageViewModel(IBaconProvider baconProvider, Thing message)
        {
            _baconProvider = baconProvider;
            _message = new TypedThing<Message>(message);
            _userService = baconProvider.GetService<IUserService>();
            _redditService = baconProvider.GetService<IRedditService>();
            _navigationService = baconProvider.GetService<INavigationService>();
            _dynamicViewLocator = baconProvider.GetService<IDynamicViewLocator>();

            if (message.Data is CommentMessage)
            {
                var commentMessage = new TypedThing<CommentMessage>(message);
                if (!String.IsNullOrEmpty(commentMessage.Data.Subject))
                {
                    if (commentMessage.Data.LinkTitle.Contains("post"))
                    {
                        isPostReply = true;
                    }
                    else
                    {
                        isPostReply = false;
                    }
                    _message.Data.Subject = commentMessage.Data.LinkTitle;
                }
            }
        }

        bool isPostReply = false;

        public string Author { get { return _message.Data.Author; } }
        public string Body { get { return _message.Data.Body; } }
        public string Context { get { return _message.Data.Context; } }
        public DateTime Created { get { return _message.Data.Created; } }
        public DateTime CreatedUTC { get { return _message.Data.CreatedUTC; } }
        public string Id { get { return _message.Data.Id; } }
        public string Name { get { return _message.Data.Name; } }
        public string ParentId { get { return _message.Data.ParentId; } }
        public MessageViewModel Parent { get; set; }
        public string Recipient { get { return _message.Data.Destination; } }
        public string Replies { get { return _message.Data.Replies; } }
        public string Subject { get { return _message.Data.Subject; } }
        public string Subreddit { get { return _message.Data.Subreddit; } }

        private string _preview;
        public string Preview
        {
            get
            {
                if (String.IsNullOrEmpty(_preview))
                {
                    _preview = Body;
                    bool flag = false;
                    var newline = _preview.IndexOf('\r');
                    if (newline <= 0)
                        newline = _preview.IndexOf('\n');
                    if (newline > 0)
                    {
                        _preview = _preview.Substring(0, newline - 1);
                        flag = true;
                    }
                    if (Body.Length > 35)
                    {
                        _preview = Body.Substring(0, 35);
                        flag = true;                        
                    }
                    if (flag)
                        _preview += "...";
                }
                return _preview;
            }
        }
        public bool IsNew { get { return _message.Data.New; } }
        public bool IsCommentReply
        {
            get
            {
                return _message.Data.WasComment && !isPostReply;
            }
        }
        public bool IsPostReply
        {
            get
            {
                return _message.Data.WasComment && isPostReply;
            }
        }
        public bool IsUserMention
        {
            get
            {
                return false;
            }
        }
    }
}
