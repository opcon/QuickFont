using System;
using System.Diagnostics;
using System.Drawing;
using QuickFont.Configuration;

namespace QuickFont
{
    /// <summary>
    /// A font resource that holds the necessary data for rendering text
    /// </summary>
    [DebuggerDisplay("{FontName}")]
    public class QFont : IDisposable
    {
        private bool _disposed;

        /// <summary>
        /// The maximum line height for the glyph set, unscaled
        /// </summary>
        public int MaxLineHeight {get { return FontData.MaxLineHeight; }}

        /// <summary>
        /// The maximum glyph height for the glyph set, unscaled
        /// </summary>
        public int MaxGlyphHeight {get { return FontData.MaxGlyphHeight; }}

        /// <summary>
        /// The <see cref="QFontData"/> corresponding to this <see cref="QFont"/>
        /// </summary>
        internal QFontData FontData { get; private set; }

        /// <summary>
        /// The name of this <see cref="QFont"/>
        /// </summary>
        public string FontName { get; }

        internal QFont(QFontData fontData)
        {
            FontData = fontData;
        }

        /// <summary>
        ///     Initialise QFont from a <see cref="IFont"/> object
        /// </summary>
        /// <param name="font">The font object used to create this <see cref="QFont"/></param>
        /// <param name="config">The font builder configuration</param>
        public QFont(IFont font, QFontBuilderConfiguration config = null)
        {
            InitialiseQFont(font, config);
        }

        /// <summary>
        /// Initialise QFont from a font file
        /// </summary>
        /// <param name="fontPath">The font file to load</param>
        /// <param name="size">The size</param>
        /// <param name="config">The configuration</param>
        /// <param name="style">The style</param>
        public QFont(string fontPath, float size, QFontBuilderConfiguration config,
            FontStyle style = FontStyle.Regular)
        {
            float fontScale = 1f;

            using (IFont font = GetFont(fontPath, size, style, config?.SuperSampleLevels ?? 1, fontScale))
            {
                FontName = font.ToString();
                InitialiseQFont(font, config);
            }
        }

        /// <summary>
        /// Initialise QFont from memory
        /// </summary>
        /// <param name="fontData">Contents of the font file</param>
        /// <param name="size">The size</param>
        /// <param name="config">The configuration</param>
        /// <param name="style">The style</param>
        public QFont(byte[] fontData, float size, QFontBuilderConfiguration config,
            FontStyle style = FontStyle.Regular)
        {
            float fontScale = 1f;

            using (IFont font = new FreeTypeFont(fontData, size, style, config?.SuperSampleLevels ?? 1, fontScale))
            {
                FontName = font.ToString();
                InitialiseQFont(font, config);
            }
        }

        /// <summary>
        ///     Initialise QFont from a .qfont file
        /// </summary>
        /// <param name="qfontPath">The .qfont file to load</param>
        /// <param name="loaderConfig">The loader configuration</param>
        /// <param name="downSampleFactor">The downsampling factor</param>
        public QFont(string qfontPath, QFontConfiguration loaderConfig, float downSampleFactor = 1.0f)
        {
            float fontScale = 1f;

            InitialiseQFont(null, new QFontBuilderConfiguration(loaderConfig), Builder.LoadQFontDataFromFile(qfontPath, downSampleFactor*fontScale, loaderConfig));
            FontName = qfontPath;
        }

        /// <summary>
        /// Initialises the <see cref="QFont"/> using the specified <see cref="IFont"/>
        /// </summary>
        /// <param name="font">The <see cref="IFont"/> to use</param>
        /// <param name="config">The builder configuration</param>
        /// <param name="data">The <see cref="QFontData"/> to use</param>
        private void InitialiseQFont(IFont font, QFontBuilderConfiguration config, QFontData data = null)
        {
            FontData = data ?? BuildFont(font, config, null);
            
            // Check and fail if more than one texture was generated. The original implementation of QFont supported
            // this by choosing them as the come but this ModernOpenGl -implementation would be handycapped by 
            // allowing this degree of freedom. It is now possible to call DrawArrays for whole texts (requiring
            // shadows and text to be each 1 texture). This is quite efficient.
            // To cover it from another aspect: OpenGL 3.1 and more easily allow Textures of up to 8129² not
            // necessrily being base2 and square - this generousity should be hapily used and effiency be gained.
            // So there will be no implementation of VAO VBO based "Modern" OpenGL that is limited to 512 textures.
            // So this is a well takeable tradeoff
            if( FontData.Pages.Length != 1 || (FontData.DropShadowFont != null && FontData.DropShadowFont.FontData.Pages.Length != 1))
            {
                throw new NotSupportedException("The implementation of QFontDrawing does not support multiple textures per Font/Shadow. " +
                                                "Thus this font can not be properly rendered in all cases. Reduce number of characters " +
                                                "or increase QFontBuilderConfiguration.MaxTexSize QFontShadowConfiguration.PageMaxTextureSize " +
                                                "to contain all characters/char-shadows in one Bitmap=>Texture.");
            }
        }

        /// <summary>
        /// Create the texture font files and save them to the specified file name
        /// </summary>
        /// <param name="font">The <see cref="IFont"/> object that is used to build the font</param>
        /// <param name="newFontName">The font file name</param>
        /// <param name="config">The builder configuration</param>
        public static void CreateTextureFontFiles(IFont font, string newFontName, QFontBuilderConfiguration config)
        {
            QFontData fontData = BuildFont(font, config, newFontName);
            Builder.SaveQFontDataToFile(fontData, newFontName);
        }

        /// <summary>
        /// Create the texture font files from the specified font file
        /// </summary>
        /// <param name="fileName">The font file to use to build the font</param>
        /// <param name="size">The font size</param>
        /// <param name="newFontName">The generated texture font file name</param>
        /// <param name="config">The builder configuration</param>
        /// <param name="style">The desired font style</param>
        public static void CreateTextureFontFiles(string fileName, float size, string newFontName, QFontBuilderConfiguration config, FontStyle style = FontStyle.Regular)
        {
            using (IFont font = GetFont(fileName, size, style, config?.SuperSampleLevels ?? 1))
            {
                CreateTextureFontFiles(font, newFontName, config);
            }
        }

        /// <summary>
        /// Returns an <see cref="IFont"/> object for the given parameters. Handles
        /// whether font file exists in <see cref="System.Drawing.Text.InstalledFontCollection"/> or not
        /// </summary>
        /// <param name="fontPath">The font path or font name</param>
        /// <param name="size">The desired font size</param>
        /// <param name="style">The desired font style</param>
        /// <param name="superSampleLevels">The desired supersample levels</param>
        /// <param name="scale">The desired font scale</param>
        /// <returns>The created <see cref="IFont"/></returns>
        private static IFont GetFont(string fontPath, float size, FontStyle style, int superSampleLevels = 1, float scale = 1.0f)
        {
            // If the font file exists load it using FreeTypeFont,
            // otherwise assume it references an internal font, and
            // load it using GDIFont (which will use the InstalledFontCollection)
            return System.IO.File.Exists(fontPath) ? 
                new FreeTypeFont(fontPath, size, style, superSampleLevels, scale) : 
                (new GDIFont(fontPath, size, style, superSampleLevels, scale)) as IFont;
        }

        /// <summary>
        /// Builds the <see cref="QFontData"/> for the given <see cref="IFont"/>
        /// </summary>
        /// <param name="font">The <see cref="IFont"/> to use to build the <see cref="QFontData"/></param>
        /// <param name="config">The builder configuration</param>
        /// <param name="saveName">
        /// The file name to save the font too. If null, the font is not saved
        /// </param>
        /// <returns>The build <see cref="QFontData"/></returns>
        private static QFontData BuildFont(IFont font, QFontBuilderConfiguration config, string saveName)
        {
            var builder = new Builder(font, config);
            return builder.BuildFontData(saveName);
        }

        /// <summary>
        /// Dispose the resources used by this <see cref="QFont"/> object
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Dispose the resources used by this <see cref="QFont"/> object
        /// </summary>
        /// <param name="disposing">Whether we are actually disposing</param>
        protected virtual void Dispose(bool disposing)
        {
            // Check to see if Dispose has already been called.
            if (!_disposed)
            {
                // If disposing equals true, dispose all managed
                // and unmanaged resources.
                if (disposing)
                {
                    //QFontDrawingPrimitive.Font.FontData.Dispose();
                    FontData.Dispose();
                }

                // Note disposing has been done.
                _disposed = true;
            }
        }

        /// <summary>
        /// Measures the specified text. Helper method delegating functionality.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <param name="maxSize">The maximum size.</param>
        /// <param name="alignment">The alignment.</param>
        /// <returns>Measured size</returns>
        public SizeF Measure(string text, SizeF maxSize, QFontAlignment alignment)
        {
            var test = new QFontDrawingPrimitive(this);
            return test.Measure(text, maxSize, alignment);
        }

        /// <summary>
        /// Measures the specified text. Helper method delegating functionality.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <param name="maxWidth">The maximum width.</param>
        /// <param name="alignment">The alignment.</param>
        /// <returns>
        /// Measured size.
        /// </returns>
        public SizeF Measure(string text, float maxWidth, QFontAlignment alignment)
        {
            var test = new QFontDrawingPrimitive(this);
            return test.Measure(text, maxWidth, alignment);
        }

        /// <summary>
        /// Measures the specified text. Helper method delegating functionality.
        /// </summary>
        /// <param name="processedText">The processed text.</param>
        /// <returns>
        /// Measured size.
        /// </returns>
        public SizeF Measure(ProcessedText processedText)
        {
            var test = new QFontDrawingPrimitive(this);
            return test.Measure(processedText);
        }

        /// <summary>
        /// Measures the specified text. Helper method delegating functionality.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <param name="alignment">The alignment.</param>
        /// <returns>
        /// Measured size.
        /// </returns>
        public SizeF Measure(string text, QFontAlignment alignment = QFontAlignment.Left)
        {
            var test = new QFontDrawingPrimitive(this);
            return test.Measure(text, alignment);
        }
    }
}
