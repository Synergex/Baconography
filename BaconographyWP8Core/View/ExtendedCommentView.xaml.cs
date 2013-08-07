using BaconographyPortable.Services;
using BaconographyPortable.ViewModel;
using GalaSoft.MvvmLight.Ioc;
using Microsoft.Practices.ServiceLocation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Controls;
using Windows.Foundation;
using Windows.Foundation.Collections;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace BaconographyWP8.View
{
    public sealed partial class ExtendedCommentView : UserControl
    {
        public ExtendedCommentView()
        {
            this.InitializeComponent();
        }

		private void ReplyButton_Tap(object sender, System.Windows.Input.GestureEventArgs e)
		{
			var vm = this.DataContext as CommentViewModel;
			vm.GotoReply.Execute(null);			
			var replyData = vm.ReplyData;
			if (SimpleIoc.Default.IsRegistered<ReplyViewModel>())
				SimpleIoc.Default.Unregister<ReplyViewModel>();
			SimpleIoc.Default.Register<ReplyViewModel>(() => replyData, true);
			var _navigationService = ServiceLocator.Current.GetInstance<INavigationService>();
			_navigationService.Navigate(typeof(ReplyViewPage), null);
		}
    }
}
