using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Xml.Linq;

namespace BaconographyWP8.Common
{
    public class DataTemplateSelector : ContentControl
    {
		public DataTemplateSelector()
			: base()
		{

		}

        protected override void OnContentChanged(object oldContent, object newContent)
        {
            base.OnContentChanged(oldContent, newContent);
            if (newContent == null)
                ContentTemplate = null;

			ContentTemplate = SelectTemplate(newContent, this);
        }

		public DataTemplate SelectTemplate(object item, DependencyObject container)
		{
			return SelectTemplateCore(item, container);
		}

		protected virtual DataTemplate SelectTemplateCore(object item, DependencyObject container)
		{
			return null;
		}
    }
}
