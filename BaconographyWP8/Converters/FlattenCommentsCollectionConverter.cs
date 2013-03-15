using BaconographyPortable.Common;
using BaconographyPortable.ViewModel;
using BaconographyPortable.ViewModel.Collections;
using GalaSoft.MvvmLight;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Data;

namespace BaconographyWP8.Converters
{
	public class FlattenCommentsCollectionConverter : IValueConverter
    {
		private ObservableCollection<ViewModelBase> Comments;
		private CommentViewModelCollection originalCollection;

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
			originalCollection = value as CommentViewModelCollection;
			originalCollection.CollectionChanged += Comment_CollectionChanged;
			Comments = new ObservableCollection<ViewModelBase>();
			foreach (var child in originalCollection)
			{
				VisitAddChildren(child);
				
			}

			return Comments;
        }

		private void VisitAddChildren(ViewModelBase vm, int index = -1)
		{
			if (index < 0)
				Comments.Add(vm);
			else
				Comments.Insert(index, vm);

			if (vm is CommentViewModel)
			{
				var comment = vm as CommentViewModel;
				if (comment.Replies != null)
				{
					comment.Replies.CollectionChanged += Comment_CollectionChanged;
					foreach (ViewModelBase child in comment.Replies)
					{
						VisitAddChildren(child, index < 0 ? -1 : index + 1);
					}
				}
			}
		}

		private void Comment_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			if (e.Action == NotifyCollectionChangedAction.Add)
			{
				if (sender == originalCollection)
				{
					foreach (ViewModelBase vm in e.NewItems)
					{
						VisitAddChildren(vm, Comments.Count);
					}
				}
				else
				{
					int index = 0;
					ObservableCollection<ViewModelBase> collection = sender as ObservableCollection<ViewModelBase>;

					// Find the previous element of the triggering collection
					CommentViewModel previousItem = null;

					if (collection.Count > 1)
						previousItem = collection[collection.Count - 2] as CommentViewModel;

					if (previousItem != null)
					{
						// If we have the previous item, find its last child
						var lastChild = GetLastChild(previousItem);
						index = Comments.IndexOf(lastChild);
					}
					else
					{
						// Otherwise, use our parent's index
						for (int i = Comments.Count - 1; i > 0; i--)
						{
							if (Comments[i] is CommentViewModel)
							{
								var comment = Comments[i] as CommentViewModel;
								if (comment.Replies == sender)
								{
									index = i;
									break;
								}
							}
						}
					}

					if (index > 0)
					{
						foreach (ViewModelBase vm in e.NewItems)
						{
							VisitAddChildren(vm, index + 1);
						}
					}
				}
			}
			else if (e.Action == NotifyCollectionChangedAction.Remove)
			{
				foreach (ViewModelBase oldItem in e.OldItems)
				{
					Comments.Remove(oldItem);
				}
			}
		}

		public CommentViewModel GetLastChild(CommentViewModel vm)
		{
			if (vm.Replies == null || vm.Replies.Count == 0)
				return vm;

			CommentViewModel lastChild = vm.Replies[vm.Replies.Count - 1] as CommentViewModel;
			return GetLastChild(lastChild);
		}

		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }

		
    }
    
}
