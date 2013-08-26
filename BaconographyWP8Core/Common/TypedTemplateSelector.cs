using BaconographyWP8Core.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace BaconographyWP8.Common
{
    //from the blog post at http://www.wiredprairie.us/blog/index.php/archives/1705
    public class TypedTemplateSelector : DataTemplateSelector
    {
        private Dictionary<string, DataTemplate> _cachedDataTemplates;

        /// <summary>
        /// Fallback value for DataTemplate
        /// </summary>
        public string DefaultTemplateKey { get; set; }

        /// <summary>
        /// Cache search results for a type (defaults to Enabled)
        /// </summary>
        public bool IsCacheEnabled { get; set; }

        public TypedTemplateSelector()
        {
            IsCacheEnabled = true;
        }

        protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
        {
            // grab the Type name. Type will be searched as Type:NAME as shown below
            /*
                <DataTemplate x:Key="Type:SampleDataItem">
                    <Grid HorizontalAlignment="Left" Width="250" Height="250">
                        <TextBlock Text="{Binding Title}" />
                    </Grid>
                </DataTemplate>
             */
            string key = item != null ? string.Format("Type:{0}", item.GetType().Name.Split('.').Last()) : DefaultTemplateKey;
            DataTemplate dt = GetCachedDataTemplate(key);
            try
            {
                if (dt != null) { return dt; }

                // look at all parents (visual parents)
                FrameworkElement fe = container as FrameworkElement;
                while (fe != null)
                {
                    dt = FindTemplate(fe, key);
                    if (dt != null) { return dt; }
                    // if you were to just look at logical parents,
                    // you'd find that there isn't a Parent for Items set
                    fe = VisualTreeHelper.GetParent(fe) as FrameworkElement;
                }

                dt = FindTemplate(null, key);
                return dt;
            }
            finally
            {
                if (dt != null)
                {
                    AddCachedDataTemplate(key, dt);
                }
            }
        }

        private DataTemplate GetCachedDataTemplate(string key)
        {
            if (key == null)
                return null;

            if (!IsCacheEnabled) { return null; }
            VerifyCachedDataTemplateStorage();
            if (_cachedDataTemplates.ContainsKey(key))
            {
                return _cachedDataTemplates[key];
            }

            return null;
        }

        private void AddCachedDataTemplate(string key, DataTemplate dt)
        {
            if (!IsCacheEnabled) { return; }
            VerifyCachedDataTemplateStorage();
            _cachedDataTemplates[key] = dt;
        }

        /// <summary>
        /// Delay creates storage
        /// </summary>
        private void VerifyCachedDataTemplateStorage()
        {
            if (_cachedDataTemplates == null)
            {
                _cachedDataTemplates = new Dictionary<string, DataTemplate>();
            }

        }

        /// <summary>
        /// Returns a template
        /// </summary>
        /// <param name="source">Pass null to search entire app</param>
        /// <param name="key"></param>
        /// <returns></returns>
        private static DataTemplate FindTemplate(object source, string key)
        {
            if (key == null)
                return null;

            var fe = source as FrameworkElement;
            object obj;
            ResourceDictionary rd = fe != null ? fe.Resources : Styles.Resources;
            if (rd.Contains(key))
            {
				obj = rd[key];
                DataTemplate dt = obj as DataTemplate;
                if (dt != null)
                {
                    return dt;
                }
            }
            return null;

        }
    }
}
