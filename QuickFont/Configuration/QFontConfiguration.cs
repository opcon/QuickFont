namespace QuickFont.Configuration
{
    public class QFontConfiguration
    {
        public QFontShadowConfiguration ShadowConfig;
        public QFontKerningConfiguration KerningConfig = new QFontKerningConfiguration();

        /// <summary>
        /// Render the font pixel-prefectly at a size in units of the current orthogonal projection, independent of the viewport pixel size.
        /// </summary>
        public bool TransformToCurrentOrthogProjection;

        public QFontConfiguration() { }

        public QFontConfiguration(bool addDropShadow, bool transformToOrthogProjection = false)
        {
            if (addDropShadow)
                this.ShadowConfig = new QFontShadowConfiguration();

            this.TransformToCurrentOrthogProjection = transformToOrthogProjection;
        }
    }
}
