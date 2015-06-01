using System;
using System.Drawing;
using System.Drawing.Text;
using OpenTK;
using OpenTK.Graphics.OpenGL4;

namespace QuickFont
{
    public class GlFont
    {
        internal QFontData fontData;

        internal GlFont(QFontData fontData)
        {
            this.fontData = fontData;
        }

        /// <summary>
        ///     Initialise QFont from a System.Drawing.Font object
        /// </summary>
        /// <param name="font"></param>
        /// <param name="config"></param>
        public GlFont(Font font, QFontBuilderConfiguration config = null)
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
        public GlFont(string fontPath, float size, QFontBuilderConfiguration config,
            FontStyle style = FontStyle.Regular, Matrix4 currentProjectionMatrix = default(Matrix4))
        {
            Viewport? transToVp = null;
            float fontScale = 1f;
            if (config.TransformToCurrentOrthogProjection)
                transToVp = OrthogonalTransform(out fontScale, currentProjectionMatrix);

            using (Font font = QuickFont.GlFont.GetFont(fontPath, size, style, config == null ? 1 : config.SuperSampleLevels, fontScale))
            {
                InitialiseGlFont(font, config);
            }

            if (transToVp != null)
                Options.TransformToViewport = transToVp;
        }

        /// <summary>
        ///     Initialise QFont from a .qfont file
        /// </summary>
        /// <param name="qfontPath">The .qfont file to load</param>
        /// <param name="loaderConfig"></param>
        /// <param name="downSampleFactor"></param>
        /// <param name="currentProjectionMatrix">The current projection matrix to create a font pixel perfect, for.</param>
        public GlFont(string qfontPath, QFontConfiguration loaderConfig, float downSampleFactor = 1.0f, Matrix4 currentProjectionMatrix = default(Matrix4))
        {
            Viewport? transToVp = null;
            float fontScale = 1f;
            if (loaderConfig.TransformToCurrentOrthogProjection)
                transToVp = OrthogonalTransform(out fontScale, currentProjectionMatrix);

            InitialiseGlFont(null, new QFontBuilderConfiguration(loaderConfig), Builder.LoadQFontDataFromFile(qfontPath, downSampleFactor*fontScale, loaderConfig));
            ViewportHelper.CurrentViewport.ToString();

            if (transToVp != null)
                Options.TransformToViewport = transToVp;
        }

        internal QFontData FontData
        {
            set { fontData = value; }
            get { return fontData; }
        }

        private void InitialiseGlFont(Font font, QFontBuilderConfiguration config, QFontData data = null)
        {
           // if (_qFont.ProjectionMatrix == Matrix4.Zero) _qFont.ProjectionMatrix = Matrix4.Identity;

            fontData = data ?? BuildFont(font, config, null);

            //if (config.ShadowConfig != null)
            //    _qFont.Options.DropShadowActive = true;

            //NOTE This should be the only usage of InitialiseState() (I think).
            //TODO allow instance render states
            //InitialiseState();

        }

        /// <summary>
        ///     Returns a System.Drawing.Font object created from the specified font file
        /// </summary>
        /// <param name="fontPath">The path to the font file</param>
        /// <param name="size"></param>
        /// <param name="style"></param>
        /// <param name="superSampleLevels"></param>
        /// <param name="scale"></param>
        /// <returns></returns>
        private static Font GetFont(string fontPath, float size, FontStyle style, int superSampleLevels = 1, float scale = 1.0f)
        {
            var pfc = new PrivateFontCollection();
            pfc.AddFontFile(fontPath);
            FontFamily fontFamily = pfc.Families[0];

            if (!fontFamily.IsStyleAvailable(style))
                throw new ArgumentException("Font file: " + fontPath + " does not support style: " + style);

            return new Font(fontFamily, size*scale*superSampleLevels, style);
        }

        public static void CreateTextureFontFiles(Font font, string newFontName, QFontBuilderConfiguration config)
        {
            QFontData fontData = BuildFont(font, config, newFontName);
            Builder.SaveQFontDataToFile(fontData, newFontName);
        }

        public static void CreateTextureFontFiles(string fileName, float size, string newFontName, QFontBuilderConfiguration config, FontStyle style = FontStyle.Regular)
        {
            using (Font font = GetFont(fileName, size, style, config == null ? 1 : config.SuperSampleLevels))
            {
                CreateTextureFontFiles(font, newFontName, config);
            }
        }

        private static QFontData BuildFont(Font font, QFontBuilderConfiguration config, string saveName)
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
                    "Current projection matrix was not Orthogonal. Please ensure that you have set an orthogonal projection before attempting to create a font with the TransformToOrthogProjection flag set to true.",
                    "projectionMatrix");

            //var viewportTransform = new Viewport(left, top, right - left, bottom - top);
            Viewport viewportTransform = ViewportHelper.GetViewportFromOrthographicProjection(ref orthoProjMatrix);
            fontScale = Math.Abs(ViewportHelper.CurrentViewport.Value.Height / viewportTransform.Height);
            return viewportTransform;
        }

    }
}