namespace QuickFont.Configuration
{
	/// <summary>
	/// Shadow Type
	/// </summary>
    public enum ShadowType
    {
		/// <summary>
		/// Blur the font to create a shadow
		/// </summary>
        Blurred,
		/// <summary>
		/// Expand the font to create a shadow
		/// </summary>
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
        public int BlurRadius = 3;

        /// <summary>
        /// Number of blur passes. Caution: high values will greatly impact the 
        /// time it takes to build a font shadow
        /// </summary>
        public int BlurPasses = 2;

        /// <summary>
        /// The standard max width/height of 2D texture pages this OpenGl context wants to support
        /// 8129 sholud be a minimum. Exact value can be obtained with GL.GetInteger(GetPName.MaxTextureSize);
        /// page will automatically be cropped if there is extra space.
        /// </summary>
        public int PageMaxTextureSize = 4096;

        /// <summary>
        /// The margin (on all sides) around glyphs when rendered to
        /// their texture page. Note this is in addition to 3xblurRadius margin
        /// which is automatically added.
        /// </summary>
        public int GlyphMargin = 2;
    }
}
