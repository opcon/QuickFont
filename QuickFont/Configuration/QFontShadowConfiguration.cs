using System;
using System.Collections.Generic;
using System.Text;

namespace QuickFont
{



    public enum ShadowType
    {
        Blurred,
        Expanded
    }

    /// <summary>
    /// The configuration used when building a font drop shadow.
    /// </summary>
    public class QFontShadowConfiguration
    {
        /// <summary>
        /// Scale in relation to the actual font glyphs
        /// </summary>
        public float Scale = 1.0f;


        /// <summary>
        /// if type is blurred then font is blurred with gaussian blur
        /// if type is expanded letter is expanded in every direction by given amount of pixels
        /// </summary>
        public ShadowType Type = ShadowType.Blurred;

        /// <summary>
        /// The blur radius. Caution: high values will greatly impact the 
        /// time it takes to build a font shadow
        /// </summary>
        public int blurRadius = 3;

        /// <summary>
        /// Number of blur passes. Caution: high values will greatly impact the 
        /// time it takes to build a font shadow
        /// </summary>
        public int blurPasses = 2;

        /// <summary>
        /// The standard width of texture pages (the page will
        /// automatically be cropped if there is extra space)
        /// </summary>
        public int PageWidth = 512;

        /// <summary>
        /// The standard height of texture pages (the page will
        /// automatically be cropped if there is extra space)
        /// </summary>
        public int PageHeight = 512;

        /// <summary>
        /// Whether to force texture pages to use a power of two.
        /// </summary>
        public bool ForcePowerOfTwo = true;

        /// <summary>
        /// The margin (on all sides) around glyphs when rendered to
        /// their texture page. Note this is in addition to 3xblurRadius margin
        /// which is automatically added.
        /// </summary>
        public int GlyphMargin = 2;

    }
}
