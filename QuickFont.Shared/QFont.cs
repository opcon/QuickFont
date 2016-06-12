using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Text;
using System.Linq;
using OpenTK;
using QuickFont.Configuration;

namespace QuickFont
{
    /// <summary>
    /// Meant to be the actual Font... a resource like <see cref="System.Drawing.Font"/>. Because it holds the textures (the fonts).
    /// </summary>
    [DebuggerDisplay("{FontName}")]
    public class QFont : IDisposable
    {
        private QFontData _fontData;
        private bool _disposed;
        private string _fontName;

        /// <summary>
        /// The maximum line height for the glyph set, unscaled
        /// </summary>
        public int MaxLineHeight {get { return _fontData.maxLineHeight; }}

        /// <summary>
        /// The maximum glyph height for the glyph set, unscaled
        /// </summary>
        public int MaxGlyphHeight {get { return _fontData.maxGlyphHeight; }}

        internal QFont(QFontData fontData)
        {
            this._fontData = fontData;
        }

        /// <summary>
        ///     Initialise QFont from a System.Drawing.Font object
        /// </summary>
        /// <param name="font"></param>
        /// <param name="config"></param>
        public QFont(IFont font, QFontBuilderConfiguration config = null)
        {
            InitialiseGlFont(font, config);
        }

        /// <summary>
        /// Initialise QFont from a font file
        /// </summary>
        /// <param name="fontPath">The font file to load</param>
        /// <param name="size">The size.</param>
        /// <param name="config">The configuration.</param>
        /// <param name="style">The style.</param>
        /// <param name="currentProjectionMatrix">The current projection matrix to create a font pixel perfect, for.</param>
        public QFont(string fontPath, float size, QFontBuilderConfiguration config,
            FontStyle style = FontStyle.Regular, Matrix4 currentProjectionMatrix = default(Matrix4))
        {
            Viewport? transToVp = null;
            float fontScale = 1f;
            if (config.TransformToCurrentOrthogProjection)
                transToVp = OrthogonalTransform(out fontScale, currentProjectionMatrix);

            using (IFont font = GetFont(fontPath, size, style, config == null ? 1 : config.SuperSampleLevels, fontScale))
            {
                _fontName = font.ToString();
                InitialiseGlFont(font, config);
            }

            // TODO: What to do with transToVp?  Property:Matrix4 and use in QFontDrawing?
            //if (transToVp != null)_fontData.Pages
   //    Options.TransformToViewport = transToVp;
        }

        /// <summary>
        ///     Initialise QFont from a .qfont file
        /// </summary>
        /// <param name="qfontPath">The .qfont file to load</param>
        /// <param name="loaderConfig"></param>
        /// <param name="downSampleFactor"></param>
        /// <param name="currentProjectionMatrix">The current projection matrix to create a font pixel perfect, for.</param>
        public QFont(string qfontPath, QFontConfiguration loaderConfig, float downSampleFactor = 1.0f, Matrix4 currentProjectionMatrix = default(Matrix4))
        {
            Viewport? transToVp = null;
            float fontScale = 1f;
            if (loaderConfig.TransformToCurrentOrthogProjection)
                transToVp = OrthogonalTransform(out fontScale, currentProjectionMatrix);

            InitialiseGlFont(null, new QFontBuilderConfiguration(loaderConfig), Builder.LoadQFontDataFromFile(qfontPath, downSampleFactor*fontScale, loaderConfig));
            _fontName = qfontPath;
            ViewportHelper.CurrentViewport.ToString();
            // TODO: What to do with transToVp?  Property:Matrix4 and use in QFontDrawing?
//if (transToVp != null)
//    Options.TransformToViewport = transToVp;
        }

        internal QFontData FontData
        {
            set { _fontData = value; }
            get { return _fontData; }
        }

        public string FontName
        {
            get { return _fontName; }
        }

        private void InitialiseGlFont(IFont font, QFontBuilderConfiguration config, QFontData data = null)
        {
            _fontData = data ?? BuildFont(font, config, null);
            
            // Check and fail if more than one texture was generated. The original implementation of QFont supported
            // this by choosing them as the come but this ModernOpenGl -implementation would be handycapped by 
            // allowing this degree of freedom. It is now possible to call DrawArrays for whole texts (requiring
            // shadows and text to be each 1 texture). This is quite efficient.
            // To cover it from another aspect: OpenGL 3.1 and more easily allow Textures of up to 8129² not
            // necessrily being base2 and square - this generousity should be hapily used and effiency be gained.
            // So there will be no implementation of VAO VBO based "Modern" OpenGL that is limited to 512 textures.
            // So this is a well takeable tradeoff
            if( _fontData.Pages.Length != 1 || (_fontData.dropShadowFont != null && _fontData.dropShadowFont.FontData.Pages.Length != 1))
            {
                throw new NotSupportedException("The implementation of QFontDrawing does not support multiple textures per Font/Shadow. " +
                                                "Thus this font can not be properly rendered in all cases. Reduce number of characters " +
                                                "or increase QFontBuilderConfiguration.MaxTexSize QFontShadowConfiguration.PageMaxTextureSize " +
                                                "to contain all characters/char-shadows in one Bitmap=>Texture.");
            }
        }

        public static void CreateTextureFontFiles(IFont font, string newFontName, QFontBuilderConfiguration config)
        {
            QFontData fontData = BuildFont(font, config, newFontName);
            Builder.SaveQFontDataToFile(fontData, newFontName);
        }

        public static void CreateTextureFontFiles(string fileName, float size, string newFontName, QFontBuilderConfiguration config, FontStyle style = FontStyle.Regular)
        {
            using (IFont font = GetFont(fileName, size, style, config == null ? 1 : config.SuperSampleLevels))
            {
                CreateTextureFontFiles(font, newFontName, config);
            }
        }

        private static IFont GetFont(string fontPath, float size, FontStyle style, int superSampleLevels = 1, float scale = 1.0f)
        {
            // If the font file exists load it using FreeTypeFont,
            // otherwise assume it references an internal font, and
            // load it using GDIFont (which will use the InstalledFontCollection)
            return System.IO.File.Exists(fontPath) ? 
                (new FreeTypeFont(fontPath, size, style, superSampleLevels, scale)) as IFont : 
                (new GDIFont(fontPath, size, style, superSampleLevels, scale)) as IFont;
        }

        private static QFontData BuildFont(IFont font, QFontBuilderConfiguration config, string saveName)
        {
            var builder = new Builder(font, config);
            return builder.BuildFontData(saveName);
        }

        /// <summary>
        ///     When TransformToOrthogProjection is enabled, we need to get the current orthogonal transformation,
        ///     the font scale, and ensure that the projection is actually orthogonal
        /// </summary>
        /// <param name="fontScale"></param>
        /// <param name="viewportTransform"></param>
        private Viewport OrthogonalTransform(out float fontScale, Matrix4 orthoProjMatrix)
        {
            if (!ViewportHelper.IsOrthographicProjection(ref orthoProjMatrix))
                throw new ArgumentOutOfRangeException(
                    "orthoProjMatrix",
                    "Current projection matrix was not Orthogonal. Please ensure that you have set an orthogonal projection before attempting to create a font with the TransformToOrthogProjection flag set to true.");

            //var viewportTransform = new Viewport(left, top, right - left, bottom - top);
            Viewport viewportTransform = ViewportHelper.GetViewportFromOrthographicProjection(ref orthoProjMatrix);
            fontScale = Math.Abs(ViewportHelper.CurrentViewport.Value.Height / viewportTransform.Height);
            return viewportTransform;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

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