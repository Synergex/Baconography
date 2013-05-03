// ===============================================================================
// AnimationMode.cs
// .NET Image Tools
// ===============================================================================
// Copyright (c) .NET Image Tools Development Group. 
// All rights reserved.
// ===============================================================================

namespace ImageTools.Controls
{
    /// <summary>
    /// Defines how the image should be animated.
    /// </summary>
    public enum AnimationMode
    {
        /// <summary>
        /// The image should not be animated.
        /// </summary>
        None,
        /// <summary>
        /// The Animation should always be played once.
        /// </summary>
        PlayOnce,
        /// <summary>
        /// Animation should always be repeated. If the end of the animation
        /// is reached, the animation will start again.
        /// </summary>
        Repeat
    }
}
