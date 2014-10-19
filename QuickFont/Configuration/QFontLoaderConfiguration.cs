using System;
using System.Collections.Generic;
using System.Text;

namespace QuickFont
{
    /// <summary>
    /// The configuraiton used when loading a font from a qfont file.
    /// </summary>
    public class QFontLoaderConfiguration : QFontConfiguration
    {

        public QFontLoaderConfiguration() { }

        public QFontLoaderConfiguration(bool addDropShadow, bool TransformToOrthogProjection = false)
        {
            if (addDropShadow)
                this.ShadowConfig = new QFontShadowConfiguration();

            this.TransformToCurrentOrthogProjection = TransformToOrthogProjection;
        }

    }
}
