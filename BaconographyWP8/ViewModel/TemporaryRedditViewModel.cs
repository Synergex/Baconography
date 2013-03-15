using BaconographyPortable.Messages;
using BaconographyPortable.Services;
using BaconographyPortable.ViewModel;
using BaconographyWP8.Messages;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaconographyWP8.ViewModel
{
	public class TemporaryRedditViewModel : ViewModelBase
	{
		RedditViewModel _redditViewModel;
		public TemporaryRedditViewModel(IBaconProvider baconProvider)
		{
			_redditViewModel = new RedditViewModel(baconProvider);
		}

		public RedditViewModel RedditViewModel
		{
			get
			{
				return _redditViewModel;
			}
		}

	}
}
