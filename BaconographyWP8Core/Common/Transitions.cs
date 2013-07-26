using Microsoft.Phone.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace BaconographyWP8.Common
{
    public class Transitions
    {

        /// <summary>
        /// Set the Turnstile transition for this UIElement
        /// </summary>
        /// <param name="element"></param>
        public static void UseMoveDownOutTransition(UIElement element)
        {
            TransitionService.SetNavigationOutTransition(element,
                new NavigationOutTransition()
                {
                    Backward = new SlideTransition()
                    {
                        Mode = SlideTransitionMode.SlideDownFadeOut
                    },
                    Forward = new SlideTransition()
                    {
                        Mode = SlideTransitionMode.SlideUpFadeIn
                    }
                }
            );
        }
        public static void UseMoveDownInTransition(UIElement element)
        {

            TransitionService.SetNavigationInTransition(element,
                new NavigationInTransition()
                {
                    Backward = new SlideTransition()
                    {
                        Mode = SlideTransitionMode.SlideDownFadeOut
                    },
                    Forward = new SlideTransition()
                    {
                        Mode = SlideTransitionMode.SlideUpFadeIn
                    }
                }
            );
        }
    }
}
