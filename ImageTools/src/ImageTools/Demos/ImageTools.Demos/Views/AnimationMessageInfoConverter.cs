// ===============================================================================
// AnimationMessageInfoConverter.cs
// .NET Image Tools
// ===============================================================================
// Copyright (c) .NET Image Tools Development Group. 
// All rights reserved.
// ===============================================================================

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using ImageTools.Controls;

namespace ImageTools.Demos.Views
{
    /// <summary>
    /// This converter converts a chat message with icons to a wrap panel of textblocks and images.
    /// </summary>
    public class AnimationMessageInfoConverter : IValueConverter
    {
        private static Dictionary<string, string> _smileys = new Dictionary<string, string>();

        /// <summary>
        /// Initializes the <see cref="AnimationMessageInfoConverter"/> class.
        /// </summary>
        static AnimationMessageInfoConverter()
        {
            _smileys.Add("*COOL*", "/Images/SmileyCool.gif");
            _smileys.Add("*CURSING*", "/Images/SmileyCursing.gif");
            _smileys.Add("*DROOL*", "/Images/SmileyDrool.gif");
            _smileys.Add("*GRINS*", "/Images/SmileyGrins.gif");
            _smileys.Add("*HUH*", "/Images/SmileyHuh.gif");
            _smileys.Add("*JEDI*", "/Images/SmileyJedi.gif");
            _smileys.Add("*LOL*", "/Images/SmileyLol.gif");
            _smileys.Add("*LOVE*", "/Images/SmileyLove.gif");
        }

        /// <summary>
        /// Modifies the source data before passing it to the target for display in the UI.
        /// </summary>
        /// <param name="value">The source data being passed to the target.</param>
        /// <param name="targetType">The <see cref="T:System.Type"/> of data expected by the target dependency property.</param>
        /// <param name="parameter">An optional parameter to be used in the converter logic.</param>
        /// <param name="culture">The culture of the conversion.</param>
        /// <returns>
        /// The value to be passed to the target dependency property.
        /// </returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            AnimationMessageInfo message = value as AnimationMessageInfo;

            if (message != null)
            {
                WrapPanel wrapPanel = new WrapPanel();
                wrapPanel.Children.Add(new TextBlock { FontWeight = FontWeights.SemiBold, Text = "[" });
                wrapPanel.Children.Add(new TextBlock { FontWeight = FontWeights.SemiBold, Text = message.Created.ToString() });
                wrapPanel.Children.Add(new TextBlock { FontWeight = FontWeights.SemiBold, Text = "]" });

                wrapPanel.Children.Add(new TextBlock { Margin = new Thickness(4, 0, 0, 0), Text = message.User });
                wrapPanel.Children.Add(new TextBlock { Margin = new Thickness(4, 0, 4, 0), Text = ":"  });

                string messageString = message.Message;

                while (true)
                {
                    int index = int.MaxValue;
                    string smileyKey = null;

                    foreach (string key in _smileys.Keys)
                    {
                        int temp = messageString.IndexOf(key);

                        if (temp >= 0 && temp < index)
                        {
                            index = temp;

                            smileyKey = key;
                        }
                    }

                    if (index != int.MaxValue)
                    {
                        string subString = messageString.Substring(0, index);

                        foreach (string part in subString.Split(' '))
                        {
                            wrapPanel.Children.Add(new TextBlock { Text = part + " ", Foreground = message.MessageBrush });
                        }

                        wrapPanel.Children.Add(GetSmiley(_smileys[smileyKey]));

                        messageString = messageString.Substring(index + smileyKey.Length);
                    }
                    else
                    {
                        foreach (string part in messageString.Split(' '))
                        {
                            wrapPanel.Children.Add(new TextBlock { Text = part + " ", Foreground = message.MessageBrush });
                        }
                        break;
                    }
                }

                return wrapPanel;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Modifies the target data before passing it to the source object.  This method is called only in <see cref="F:System.Windows.Data.BindingMode.TwoWay"/> bindings.
        /// </summary>
        /// <param name="value">The target data being passed to the source.</param>
        /// <param name="targetType">The <see cref="T:System.Type"/> of data expected by the source object.</param>
        /// <param name="parameter">An optional parameter to be used in the converter logic.</param>
        /// <param name="culture">The culture of the conversion.</param>
        /// <returns>
        /// The value to be passed to the source object.
        /// </returns>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new System.NotImplementedException();
        }

        private static AnimatedImage GetSmiley(string path)
        {
            ExtendedImage image = new ExtendedImage();
            image.UriSource = new Uri(path, UriKind.Relative);

            AnimatedImage smiley = new AnimatedImage();
            smiley.Stretch = System.Windows.Media.Stretch.None;
            smiley.Source = image;
            smiley.VerticalAlignment = VerticalAlignment.Bottom;

            return smiley;
        }
    }
}
