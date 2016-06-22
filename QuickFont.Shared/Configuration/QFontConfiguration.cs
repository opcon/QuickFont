namespace QuickFont.Configuration
{
	/// <summary>
	/// Class to hold font configuration data
	/// </summary>
    public class QFontConfiguration
    {
		/// <summary>
		/// The shadow configuration
		/// </summary>
        public QFontShadowConfiguration ShadowConfig;

		/// <summary>
		/// The kerning configuration
		/// </summary>
        public QFontKerningConfiguration KerningConfig = new QFontKerningConfiguration();

        /// <summary>
        /// Render the font pixel-prefectly at a size in units of the current orthogonal projection, independent of the viewport pixel size.
        /// </summary>
        public bool TransformToCurrentOrthogProjection;

		/// <summary>
		/// Creates a new instance of <see cref="QFontConfiguration"/>
		/// </summary>
        public QFontConfiguration() { }

		/// <summary>
		/// Creates a new instance of <see cref="QFontConfiguration"/>
		/// </summary>
		/// <param name="addDropShadow">True to add a drop shadow to the font</param>
		/// <param name="transformToOrthogProjection">OBSOLETE</param>
        public QFontConfiguration(bool addDropShadow, bool transformToOrthogProjection = false)
        {
            if (addDropShadow)
				ShadowConfig = new QFontShadowConfiguration();

            TransformToCurrentOrthogProjection = transformToOrthogProjection;
        }
    }
}
