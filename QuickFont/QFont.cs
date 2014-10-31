using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Text;
using OpenTK;
using OpenTK.Graphics.OpenGL4;

namespace QuickFont
{
    public class QFont
    {
        //private QFontRenderOptions options = new QFontRenderOptions();
        private const string fragShaderSource = @"#version 430 core

uniform sampler2D tex_object;

in VS_OUT
{
	vec2 tc;
	vec4 colour;
} fs_in;

out vec4 colour;

void main(void)
{
	colour = texture(tex_object, fs_in.tc.st) * vec4(fs_in.colour);
    //colour = vec4(0., 0.5, 0., 1.0);
}";
        private const string vertShaderSource = @"#version 430 core

uniform mat4 proj_matrix;

in vec3 in_position;
in vec2 in_tc;
in vec4 in_colour;

out VS_OUT
{
	vec2 tc;
	vec4 colour;
} vs_out;

void main(void)
{
	vs_out.tc = in_tc;
	vs_out.colour = in_colour;
	gl_Position = proj_matrix * vec4(in_position, 1.); 
//    gl_Position = vec4(0.,0.,0.,1.);
}";

        private static SharedState _QFontSharedState;
        private readonly Stack<QFontRenderOptions> optionsStack = new Stack<QFontRenderOptions>();

        public QVertexArrayObject[] VertexArrayObjects = new QVertexArrayObject[0];
        private SharedState _instanceSharedState;

        private Vector3 _printOffset;
        private Matrix4 _projectionMatrix;
        internal QFontData fontData;

        public Vector3 PrintOffset
        {
            get { return _printOffset; }
            set
            {
                _printOffset = value;
                if (fontData.dropShadow != null)
                    fontData.dropShadow.PrintOffset = value;
            }
        }

        public QFontRenderOptions Options
        {
            get
            {
                if (optionsStack.Count == 0)
                {
                    optionsStack.Push(new QFontRenderOptions());
                }

                return optionsStack.Peek();
            }
            private set
            {
                //not sure if we should even allow this...
                optionsStack.Pop();
                optionsStack.Push(value);
            }
        }

        public static SharedState QFontSharedState
        {
            get { return _QFontSharedState; }
        }

        public SharedState InstanceSharedState
        {
            get { return _instanceSharedState ?? QFontSharedState; }
        }

        public Matrix4 ProjectionMatrix
        {
            get { return _projectionMatrix; }
            set { _projectionMatrix = value; }
        }

        public float LineSpacing
        {
            get { return (float) Math.Ceiling(fontData.maxGlyphHeight*Options.LineSpacing); }
        }

        public bool IsMonospacingActive
        {
            get { return fontData.IsMonospacingActive(Options); }
        }


        public float MonoSpaceWidth
        {
            get { return fontData.GetMonoSpaceWidth(Options); }
        }

        #region Constructors and font builders

        /// <summary>
        ///     Used for creating a dropshadow QFont object
        /// </summary>
        /// <param name="fontData"></param>
        internal QFont(QFontData fontData)
        {
            this.fontData = fontData;
        }

        /// <summary>
        ///     Initialise QFont from a System.Drawing.Font object
        /// </summary>
        /// <param name="font"></param>
        /// <param name="config"></param>
        public QFont(Font font, QFontBuilderConfiguration config = null)
        {
            InitialiseQFont(font, config);
        }

        /// <summary>
        ///     Initialise QFont from a font file
        /// </summary>
        /// <param name="fontPath">The font file to load</param>
        /// <param name="size"></param>
        /// <param name="config"></param>
        /// <param name="style"></param>
        public QFont(string fontPath, float size, QFontBuilderConfiguration config,
            FontStyle style = FontStyle.Regular, Matrix4 projectionMatrix = default(Matrix4))
        {
            _projectionMatrix = projectionMatrix;
            Viewport? transToVp = null;
            float fontScale = 1f;
            if (config.TransformToCurrentOrthogProjection)
                transToVp = OrthogonalTransform(out fontScale);

            using (Font font = GetFont(fontPath, size, style, config == null ? 1 : config.SuperSampleLevels, fontScale))
            {
                InitialiseQFont(font, config);
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
        /// <param name="proj"></param>
        public QFont(string qfontPath, QFontConfiguration loaderConfig, float downSampleFactor = 1.0f, Matrix4 projectionMatrix = default(Matrix4))
        {
            _projectionMatrix = projectionMatrix;
            Viewport? transToVp = null;
            float fontScale = 1f;
            if (loaderConfig.TransformToCurrentOrthogProjection)
                transToVp = OrthogonalTransform(out fontScale);

            InitialiseQFont(null, new QFontBuilderConfiguration(loaderConfig), Builder.LoadQFontDataFromFile(qfontPath, downSampleFactor*fontScale, loaderConfig));
            ViewportHelper.CurrentViewport.ToString();

            if (transToVp != null)
                Options.TransformToViewport = transToVp;
        }

        private void InitialiseQFont(Font font, QFontBuilderConfiguration config, QFontData data = null)
        {
            ProjectionMatrix = Matrix4.Identity;

            fontData = data ?? BuildFont(font, config, null);

            if (config.ShadowConfig != null)
                Options.DropShadowActive = true;

            //NOTE This should be the only usage of InitialiseState() (I think).
            //TODO allow instance render states
            InitialiseState();

            //Always use VBOs
            InitVBOs();
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

        /// <summary>
        ///     Initialises the static shared render state
        /// </summary>
        private static void InitialiseStaticState()
        {
            //Create vertex and fragment shaders
            int vert = GL.CreateShader(ShaderType.VertexShader);
            int frag = GL.CreateShader(ShaderType.FragmentShader);

            //Check shaders were created succesfully
            if (vert == -1 || frag == -1)
                throw new Exception(string.Format("Error creating shader name for {0}", vert == -1 ? (frag == -1 ? "vert and frag shaders" : "vert shader") : "frag shader"));

            //Compile default (simple) shaders
            int vertCompileStatus;
            int fragCompileStatus;

            GL.ShaderSource(vert, vertShaderSource);
            GL.CompileShader(vert);
            GL.ShaderSource(frag, fragShaderSource);
            GL.CompileShader(frag);

            GL.GetShader(vert, ShaderParameter.CompileStatus, out vertCompileStatus);
            GL.GetShader(frag, ShaderParameter.CompileStatus, out fragCompileStatus);

            //Check shaders were compiled correctly
            //TODO Worth doing this rather than just checking the total program error log?
            if (vertCompileStatus == 0 || fragCompileStatus == 0)
            {
                string vertInfo = GL.GetShaderInfoLog(vert);
                string fragInfo = GL.GetShaderInfoLog(frag);
                throw new Exception(String.Format("Shaders were not compiled correctly. Info logs are\nVert:\n{0}\nFrag:\n{1}", vertInfo, fragInfo));
            }

            int prog;
            int progLinkStatus;

            prog = GL.CreateProgram();
            GL.AttachShader(prog, vert);
            GL.AttachShader(prog, frag);
            GL.LinkProgram(prog);

            //Check program was linked without errors 
            GL.GetProgram(prog, GetProgramParameterName.LinkStatus, out progLinkStatus);
            if (progLinkStatus == 0)
            {
                string programInfoLog = GL.GetProgramInfoLog(prog);
                throw new Exception(String.Format("Program was not linked correctly. Info log is\n{0}", programInfoLog));
            }

            //Detach then delete unneeded shaders
            GL.DetachShader(prog, vert);
            GL.DetachShader(prog, frag);
            GL.DeleteShader(vert);
            GL.DeleteShader(frag);

            //Retrieve shader attribute and uniform locations
            int mvpLoc = GL.GetUniformLocation(prog, "proj_matrix");
            int samplerLoc = GL.GetUniformLocation(prog, "tex_object");
            int posLoc = GL.GetAttribLocation(prog, "in_position");
            int tcLoc = GL.GetAttribLocation(prog, "in_tc");
            int colLoc = GL.GetAttribLocation(prog, "in_colour");

            int sampler = GL.GenSampler();
            GL.SamplerParameter(sampler, SamplerParameterName.TextureWrapS, (int) TextureWrapMode.ClampToBorder);
            GL.SamplerParameter(sampler, SamplerParameterName.TextureWrapT, (int) TextureWrapMode.ClampToBorder);
            GL.SamplerParameter(sampler, SamplerParameterName.TextureMinFilter, (int) TextureMinFilter.Linear);
            GL.SamplerParameter(sampler, SamplerParameterName.TextureMagFilter, (int) TextureMagFilter.Linear);

            //Now we have all the information, time to create the immutable shared state object
            var shaderVariables = new ShaderVariables(prog, mvpLoc, tcLoc, posLoc, samplerLoc, colLoc);
            var sharedState = new SharedState(TextureUnit.Texture0, shaderVariables, sampler);

            _QFontSharedState = sharedState;
        }

        /// <summary>
        ///     Initialises the instance render state
        /// </summary>
        /// <param name="state">
        ///     If state is null, this method will instead initialise the static state, which is returned when no
        ///     instance state is set
        /// </param>
        private void InitialiseState(SharedState state = null)
        {
            if (state == null)
            {
                if (QFontSharedState == null) InitialiseStaticState();
            }
            else
            {
                _instanceSharedState = state;
                if (fontData.dropShadow != null)
                    fontData.dropShadow._instanceSharedState = state;
            }
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

        #endregion

        /// <summary>
        ///     When TransformToOrthogProjection is enabled, we need to get the current orthogonal transformation,
        ///     the font scale, and ensure that the projection is actually orthogonal
        /// </summary>
        /// <param name="fontScale"></param>
        /// <param name="viewportTransform"></param>
        private Viewport OrthogonalTransform(out float fontScale)
        {
            //bool isOrthog;
            //float left, right, bottom, top;
            //ViewportHelper.GetCurrentOrthogProjection(out isOrthog, out left, out right, out bottom, out top);

            if (!ViewportHelper.IsOrthographicProjection(ref _projectionMatrix))
                throw new ArgumentOutOfRangeException(
                    "Current projection matrix was not Orthogonal. Please ensure that you have set an orthogonal projection before attempting to create a font with the TransformToOrthogProjection flag set to true.",
                    "projectionMatrix");

            //var viewportTransform = new Viewport(left, top, right - left, bottom - top);
            Viewport viewportTransform = ViewportHelper.GetViewportFromOrthographicProjection(ref _projectionMatrix);
            fontScale = Math.Abs(ViewportHelper.CurrentViewport.Value.Height/viewportTransform.Height);
            return viewportTransform;
        }

        /// <summary>
        ///     Pushes the specified QFont options onto the options stack
        /// </summary>
        /// <param name="newOptions"></param>
        public void PushOptions(QFontRenderOptions newOptions)
        {
            optionsStack.Push(newOptions);
        }

        /// <summary>
        ///     Creates a clone of the current font options and pushes
        ///     it onto the stack
        /// </summary>
        public void PushOptions()
        {
            PushOptions(Options.CreateClone());
        }

        public void PopOptions()
        {
            if (optionsStack.Count > 1)
            {
                optionsStack.Pop();
            }
            else
            {
                throw new Exception(
                    "Attempted to pop from options stack when there is only one Options object on the stack.");
            }
        }

        private void RenderDropShadow(float x, float y, char c, QFontGlyph nonShadowGlyph)
        {
            //note can cast drop shadow offset to int, but then you can't move the shadow smoothly...
            if (fontData.dropShadow != null && Options.DropShadowActive)
            {
                fontData.dropShadow.RenderGlyph(
                    x + (fontData.meanGlyphWidth*Options.DropShadowOffset.X + nonShadowGlyph.rect.Width*0.5f),
                    y +
                    (fontData.meanGlyphWidth*Options.DropShadowOffset.Y + nonShadowGlyph.rect.Height*0.5f +
                     nonShadowGlyph.yOffset), c);
            }
        }

        public void RenderGlyph(float x, float y, char c)
        {
            QFontGlyph glyph = fontData.CharSetMapping[c];

            //note: it's not immediately obvious, but this combined with the paramteters to 
            //RenderGlyph for the shadow mean that we render the shadow centrally (despite it being a different size)
            //under the glyph
            if (fontData.isDropShadow)
            {
                x -= (int) (glyph.rect.Width*0.5f);
                y -= (int) (glyph.rect.Height*0.5f + glyph.yOffset);
            }
            else
                RenderDropShadow(x, y, c, glyph);

            TexturePage sheet = fontData.Pages[glyph.page];

            float tx1 = (float) (glyph.rect.X)/sheet.Width;
            float ty1 = (float) (glyph.rect.Y)/sheet.Height;
            float tx2 = (float) (glyph.rect.X + glyph.rect.Width)/sheet.Width;
            float ty2 = (float) (glyph.rect.Y + glyph.rect.Height)/sheet.Height;

            var tv1 = new Vector2(tx1, ty1);
            var tv2 = new Vector2(tx1, ty2);
            var tv3 = new Vector2(tx2, ty2);
            var tv4 = new Vector2(tx2, ty1);

            Vector3 v1 = PrintOffset + new Vector3(x, y + glyph.yOffset, 0);
            Vector3 v2 = PrintOffset + new Vector3(x, y + glyph.yOffset + glyph.rect.Height, 0);
            Vector3 v3 = PrintOffset + new Vector3(x + glyph.rect.Width, y + glyph.yOffset + glyph.rect.Height, 0);
            Vector3 v4 = PrintOffset + new Vector3(x + glyph.rect.Width, y + glyph.yOffset, 0);

            Color color;
            if (fontData.isDropShadow)
                color = Options.DropShadowColour;
            else
                color = Options.Colour;

            var normal = new Vector3(0, 0, -1);

            int argb = Helper.ToRgba(color);

            QVertexArrayObject vbo = VertexArrayObjects[glyph.page];

            Vector4 colour = Helper.ToVector4(color);

            vbo.AddVertex(v1, tv1, colour);
            vbo.AddVertex(v2, tv2, colour);
            vbo.AddVertex(v3, tv3, colour);

            vbo.AddVertex(v1, tv1, colour);
            vbo.AddVertex(v3, tv3, colour);
            vbo.AddVertex(v4, tv4, colour);
        }

        private float MeasureNextlineLength(string text)
        {
            float xOffset = 0;

            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];

                if (c == '\r' || c == '\n')
                {
                    break;
                }


                if (IsMonospacingActive)
                {
                    xOffset += MonoSpaceWidth;
                }
                else
                {
                    //space
                    if (c == ' ')
                    {
                        xOffset += (float) Math.Ceiling(fontData.meanGlyphWidth*Options.WordSpacing);
                    }
                        //normal character
                    else if (fontData.CharSetMapping.ContainsKey(c))
                    {
                        QFontGlyph glyph = fontData.CharSetMapping[c];
                        xOffset +=
                            (float)
                                Math.Ceiling(glyph.rect.Width + fontData.meanGlyphWidth*Options.CharacterSpacing +
                                             fontData.GetKerningPairCorrection(i, text, null));
                    }
                }
            }
            return xOffset;
        }

        private Vector2 TransformPositionToViewport(Vector2 input)
        {
            Viewport? v2 = Options.TransformToViewport;
            if (v2 == null)
            {
                return input;
            }
            Viewport? v1 = ViewportHelper.CurrentViewport;

            float X, Y;

            Debug.Assert(v1 != null, "v1 != null");
            X = (input.X - v2.Value.X)*(v1.Value.Width/v2.Value.Width);
            Y = (input.Y - v2.Value.Y)*(v1.Value.Height/v2.Value.Height);

            return new Vector2(X, Y);
        }

        private float TransformWidthToViewport(float input)
        {
            Viewport? v2 = Options.TransformToViewport;
            if (v2 == null)
            {
                return input;
            }
            Viewport? v1 = ViewportHelper.CurrentViewport;

            Debug.Assert(v1 != null, "v1 != null");
            return input*(v1.Value.Width/v2.Value.Width);
        }

        private SizeF TransformMeasureFromViewport(SizeF input)
        {
            Viewport? v2 = Options.TransformToViewport;
            if (v2 == null)
            {
                return input;
            }
            Viewport? v1 = ViewportHelper.CurrentViewport;

            float X, Y;

            Debug.Assert(v1 != null, "v1 != null");
            X = input.Width*(v2.Value.Width/v1.Value.Width);
            Y = input.Height*(v2.Value.Height/v1.Value.Height);

            return new SizeF(X, Y);
        }

        private Vector2 LockToPixel(Vector2 input)
        {
            if (Options.LockToPixel)
            {
                float r = Options.LockToPixelRatio;
                return new Vector2((1 - r)*input.X + r*((int) Math.Round(input.X)),
                    (1 - r)*input.Y + r*((int) Math.Round(input.Y)));
            }
            return input;
        }

        private Vector3 TransformToViewport(Vector3 input)
        {
            return new Vector3(LockToPixel(TransformPositionToViewport(input.Xy))) {Z = input.Z};
        }

        public void PrintToVBO(string text, Vector3 position, SizeF maxSize, QFontAlignment alignment)
        {
            ProcessedText processedText = ProcessText(text, maxSize, alignment);
            PrintToVBO(processedText, TransformToViewport(position));
        }

        public void PrintToVBO(string text, Vector3 position, SizeF maxSize, QFontAlignment alignment, Color colour)
        {
            ProcessedText processedText = ProcessText(text, maxSize, alignment);
            PrintToVBO(processedText, TransformToViewport(position), colour);
        }

        public void PrintToVBO(ProcessedText processedText, Vector3 position)
        {
            PrintOffset = TransformToViewport(position);
            PrintOrMeasure(processedText, false);
        }

        public void PrintToVBO(ProcessedText processedText, Vector3 position, Color colour)
        {
            Options.Colour = colour;
            PrintOffset = TransformToViewport(position);
            PrintOrMeasure(processedText, false);
        }

        public void PrintToVBO(string text, Vector3 position, QFontAlignment alignment)
        {
            PrintOffset = TransformToViewport(position);
            PrintOrMeasure(text, alignment, false);
        }

        public void PrintToVBO(string text, Vector3 position, QFontAlignment alignment, Color color)
        {
            Options.Colour = color;
            PrintOffset = TransformToViewport(position);
            PrintOrMeasure(text, alignment, false);
        }

        public SizeF Measure(string text, QFontAlignment alignment = QFontAlignment.Left)
        {
            return TransformMeasureFromViewport(PrintOrMeasure(text, alignment, true));
        }

        public SizeF Measure(string text, float maxWidth, QFontAlignment alignment)
        {
            return Measure(text, new SizeF(maxWidth, -1), alignment);
        }

        /// <summary>
        ///     Measures the actual width and height of the block of text.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="bounds"></param>
        /// <param name="alignment"></param>
        /// <returns></returns>
        public SizeF Measure(string text, SizeF maxSize, QFontAlignment alignment)
        {
            ProcessedText processedText = ProcessText(text, maxSize, alignment);
            return Measure(processedText);
        }

        /// <summary>
        ///     Measures the actual width and height of the block of text
        /// </summary>
        /// <param name="processedText"></param>
        /// <returns></returns>
        public SizeF Measure(ProcessedText processedText)
        {
            return TransformMeasureFromViewport(PrintOrMeasure(processedText, true));
        }

        private SizeF PrintOrMeasure(string text, QFontAlignment alignment, bool measureOnly)
        {
            float maxWidth = 0f;
            float xOffset = 0f;
            float yOffset = 0f;

            var caps = new EnableCap[] {};

            //make sure fontdata font's options are synced with the actual options
            if (fontData.dropShadow != null && fontData.dropShadow.Options != Options)
            {
                fontData.dropShadow.Options = Options;
            }

            Helper.SafeGLEnable(caps, () =>
            {
                float maxXpos = float.MinValue;
                float minXPos = float.MaxValue;

                text = text.Replace("\r\n", "\r");

                if (alignment == QFontAlignment.Right)
                    xOffset -= MeasureNextlineLength(text);
                else if (alignment == QFontAlignment.Centre)
                    xOffset -= (int) (0.5f*MeasureNextlineLength(text));

                for (int i = 0; i < text.Length; i++)
                {
                    char c = text[i];

                    //newline
                    if (c == '\r' || c == '\n')
                    {
                        yOffset += LineSpacing;
                        xOffset = 0f;

                        if (alignment == QFontAlignment.Right)
                            xOffset -= MeasureNextlineLength(text.Substring(i + 1));
                        else if (alignment == QFontAlignment.Centre)
                            xOffset -= (int) (0.5f*MeasureNextlineLength(text.Substring(i + 1)));
                    }
                    else
                    {
                        minXPos = Math.Min(xOffset, minXPos);

                        //normal character
                        if (c != ' ' && fontData.CharSetMapping.ContainsKey(c))
                        {
                            QFontGlyph glyph = fontData.CharSetMapping[c];
                            if (!measureOnly)
                                RenderGlyph(xOffset, yOffset, c);
                        }

                        if (IsMonospacingActive)
                            xOffset += MonoSpaceWidth;
                        else
                        {
                            if (c == ' ')
                                xOffset += (float) Math.Ceiling(fontData.meanGlyphWidth*Options.WordSpacing);
                                //normal character
                            else if (fontData.CharSetMapping.ContainsKey(c))
                            {
                                QFontGlyph glyph = fontData.CharSetMapping[c];
                                xOffset +=
                                    (float)
                                        Math.Ceiling(glyph.rect.Width + fontData.meanGlyphWidth*Options.CharacterSpacing +
                                                     fontData.GetKerningPairCorrection(i, text, null));
                            }
                        }

                        maxXpos = Math.Max(xOffset, maxXpos);
                    }
                }

                if (minXPos != float.MaxValue)
                    maxWidth = maxXpos - minXPos;
            });

            return new SizeF(maxWidth, yOffset + LineSpacing);
        }

        private SizeF PrintOrMeasure(ProcessedText processedText, bool measureOnly)
        {
            // init values we'll return
            float maxMeasuredWidth = 0f;

            float xPos = 0f;
            float yPos = 0f;

            float xOffset = xPos;
            float yOffset = yPos;

            // determine what capacities we need
            var caps = new EnableCap[] {};

            //make sure fontdata font's options are synced with the actual options
            if (fontData.dropShadow != null && fontData.dropShadow.Options != Options)
            {
                fontData.dropShadow.Options = Options;
            }

            Helper.SafeGLEnable(caps, () =>
            {
                float maxWidth = processedText.maxSize.Width;
                QFontAlignment alignment = processedText.alignment;


                //TODO - use these instead of translate when rendering by position (at some point)

                TextNodeList nodeList = processedText.textNodeList;
                for (TextNode node = nodeList.Head; node != null; node = node.Next)
                    node.LengthTweak = 0f; //reset tweaks


                if (alignment == QFontAlignment.Right)
                    xOffset -= (float) Math.Ceiling(TextNodeLineLength(nodeList.Head, maxWidth) - maxWidth);
                else if (alignment == QFontAlignment.Centre)
                    xOffset -= (float) Math.Ceiling(0.5f*TextNodeLineLength(nodeList.Head, maxWidth));
                else if (alignment == QFontAlignment.Justify)
                    JustifyLine(nodeList.Head, maxWidth);


                bool atLeastOneNodeCosumedOnLine = false;
                float length = 0f;
                for (TextNode node = nodeList.Head; node != null; node = node.Next)
                {
                    bool newLine = false;

                    if (node.Type == TextNodeType.LineBreak)
                    {
                        newLine = true;
                    }
                    else
                    {
                        if (Options.WordWrap && SkipTrailingSpace(node, length, maxWidth) && atLeastOneNodeCosumedOnLine)
                        {
                            newLine = true;
                        }
                        else if (length + node.ModifiedLength <= maxWidth || !atLeastOneNodeCosumedOnLine)
                        {
                            atLeastOneNodeCosumedOnLine = true;

                            if (!measureOnly)
                                RenderWord(xOffset + length, yOffset, node);
                            length += node.ModifiedLength;

                            maxMeasuredWidth = Math.Max(length, maxMeasuredWidth);
                        }
                        else if (Options.WordWrap)
                        {
                            newLine = true;
                            if (node.Previous != null)
                                node = node.Previous;
                        }
                        else
                            continue; // continue so we still read line breaks even if reached max width
                    }

                    if (newLine)
                    {
                        if (processedText.maxSize.Height > 0 &&
                            yOffset + LineSpacing - yPos >= processedText.maxSize.Height)
                            break;

                        yOffset += LineSpacing;
                        xOffset = xPos;
                        length = 0f;
                        atLeastOneNodeCosumedOnLine = false;

                        if (node.Next != null)
                        {
                            if (alignment == QFontAlignment.Right)
                                xOffset -= (float) Math.Ceiling(TextNodeLineLength(node.Next, maxWidth) - maxWidth);
                            else if (alignment == QFontAlignment.Centre)
                                xOffset -= (float) Math.Ceiling(0.5f*TextNodeLineLength(node.Next, maxWidth));
                            else if (alignment == QFontAlignment.Justify)
                                JustifyLine(node.Next, maxWidth);
                        }
                    }
                }
            });

            return new SizeF(maxMeasuredWidth, yOffset + LineSpacing - yPos);
        }

        private void RenderWord(float x, float y, TextNode node)
        {
            if (node.Type != TextNodeType.Word)
                return;

            int charGaps = node.Text.Length - 1;
            bool isCrumbleWord = CrumbledWord(node);
            if (isCrumbleWord)
                charGaps++;

            int pixelsPerGap = 0;
            int leftOverPixels = 0;

            if (charGaps != 0)
            {
                pixelsPerGap = (int) node.LengthTweak/charGaps;
                leftOverPixels = (int) node.LengthTweak - pixelsPerGap*charGaps;
            }

            for (int i = 0; i < node.Text.Length; i++)
            {
                char c = node.Text[i];
                if (fontData.CharSetMapping.ContainsKey(c))
                {
                    QFontGlyph glyph = fontData.CharSetMapping[c];

                    RenderGlyph(x, y, c);


                    if (IsMonospacingActive)
                        x += MonoSpaceWidth;
                    else
                        x +=
                            (int)
                                Math.Ceiling(glyph.rect.Width + fontData.meanGlyphWidth*Options.CharacterSpacing +
                                             fontData.GetKerningPairCorrection(i, node.Text, node));

                    x += pixelsPerGap;
                    if (leftOverPixels > 0)
                    {
                        x += 1.0f;
                        leftOverPixels--;
                    }
                    else if (leftOverPixels < 0)
                    {
                        x -= 1.0f;
                        leftOverPixels++;
                    }
                }
            }
        }

        /// <summary>
        ///     Computes the length of the next line, and whether the line is valid for
        ///     justification.
        /// </summary>
        /// <param name="node"></param>
        /// <param name="maxLength"></param>
        /// <param name="justifable"></param>
        /// <returns></returns>
        private float TextNodeLineLength(TextNode node, float maxLength)
        {
            if (node == null)
                return 0;

            bool atLeastOneNodeCosumedOnLine = false;
            float length = 0;
            for (; node != null; node = node.Next)
            {
                if (node.Type == TextNodeType.LineBreak)
                    break;

                if (SkipTrailingSpace(node, length, maxLength) && atLeastOneNodeCosumedOnLine)
                    break;

                if (length + node.Length <= maxLength || !atLeastOneNodeCosumedOnLine)
                {
                    atLeastOneNodeCosumedOnLine = true;
                    length += node.Length;
                }
                else
                {
                    break;
                }
            }
            return length;
        }


        private bool CrumbledWord(TextNode node)
        {
            return (node.Type == TextNodeType.Word && node.Next != null && node.Next.Type == TextNodeType.Word);
        }

        /// <summary>
        ///     Computes the length of the next line, and whether the line is valid for
        ///     justification.
        /// </summary>
        private void JustifyLine(TextNode node, float targetLength)
        {
            bool justifiable = false;

            if (node == null)
                return;

            TextNode headNode = node; //keep track of the head node


            //start by finding the length of the block of text that we know will actually fit:

            int charGaps = 0;
            int spaceGaps = 0;

            bool atLeastOneNodeCosumedOnLine = false;
            float length = 0;
            TextNode expandEndNode = node; //the node at the end of the smaller list (before adding additional word)
            for (; node != null; node = node.Next)
            {
                if (node.Type == TextNodeType.LineBreak)
                    break;

                if (SkipTrailingSpace(node, length, targetLength) && atLeastOneNodeCosumedOnLine)
                {
                    justifiable = true;
                    break;
                }

                if (length + node.Length < targetLength || !atLeastOneNodeCosumedOnLine)
                {
                    expandEndNode = node;

                    if (node.Type == TextNodeType.Space)
                        spaceGaps++;

                    if (node.Type == TextNodeType.Word)
                    {
                        charGaps += (node.Text.Length - 1);

                        //word was part of a crumbled word, so there's an extra char cap between the two words
                        if (CrumbledWord(node))
                            charGaps++;
                    }

                    atLeastOneNodeCosumedOnLine = true;
                    length += node.Length;
                }
                else
                {
                    justifiable = true;
                    break;
                }
            }

            //now we check how much additional length is added by adding an additional word to the line
            float extraLength = 0f;
            int extraSpaceGaps = 0;
            int extraCharGaps = 0;
            bool contractPossible = false;
            TextNode contractEndNode = null;
            for (node = expandEndNode.Next; node != null; node = node.Next)
            {
                if (node.Type == TextNodeType.LineBreak)
                    break;

                if (node.Type == TextNodeType.Space)
                {
                    extraLength += node.Length;
                    extraSpaceGaps++;
                }
                else if (node.Type == TextNodeType.Word)
                {
                    contractEndNode = node;
                    contractPossible = true;
                    extraLength += node.Length;
                    extraCharGaps += (node.Text.Length - 1);
                    break;
                }
            }

            if (justifiable)
            {
                //last part of this condition is to ensure that the full contraction is possible (it is all or nothing with contractions, since it looks really bad if we don't manage the full)
                bool contract = contractPossible &&
                                (extraLength + length - targetLength)*Options.JustifyContractionPenalty <
                                (targetLength - length) &&
                                ((targetLength - (length + extraLength + 1))/targetLength > -Options.JustifyCapContract);

                if ((!contract && length < targetLength) || (contract && length + extraLength > targetLength))
                    //calculate padding pixels per word and char
                {
                    if (contract)
                    {
                        length += extraLength + 1;
                        charGaps += extraCharGaps;
                        spaceGaps += extraSpaceGaps;
                    }


                    var totalPixels = (int) (targetLength - length);
                    //the total number of pixels that need to be added to line to justify it
                    int spacePixels = 0; //number of pixels to spread out amongst spaces
                    int charPixels = 0; //number of pixels to spread out amongst char gaps


                    if (contract)
                    {
                        if (totalPixels/targetLength < -Options.JustifyCapContract)
                            totalPixels = (int) (-Options.JustifyCapContract*targetLength);
                    }
                    else
                    {
                        if (totalPixels/targetLength > Options.JustifyCapExpand)
                            totalPixels = (int) (Options.JustifyCapExpand*targetLength);
                    }

                    //work out how to spread pixles between character gaps and word spaces
                    if (charGaps == 0)
                    {
                        spacePixels = totalPixels;
                    }
                    else if (spaceGaps == 0)
                    {
                        charPixels = totalPixels;
                    }
                    else
                    {
                        if (contract)
                            charPixels =
                                (int) (totalPixels*Options.JustifyCharacterWeightForContract*charGaps/spaceGaps);
                        else
                            charPixels = (int) (totalPixels*Options.JustifyCharacterWeightForExpand*charGaps/spaceGaps);


                        if ((!contract && charPixels > totalPixels) ||
                            (contract && charPixels < totalPixels))
                            charPixels = totalPixels;

                        spacePixels = totalPixels - charPixels;
                    }


                    int pixelsPerChar = 0; //minimum number of pixels to add per char
                    int leftOverCharPixels = 0; //number of pixels remaining to only add for some chars

                    if (charGaps != 0)
                    {
                        pixelsPerChar = charPixels/charGaps;
                        leftOverCharPixels = charPixels - pixelsPerChar*charGaps;
                    }

                    int pixelsPerSpace = 0; //minimum number of pixels to add per space
                    int leftOverSpacePixels = 0; //number of pixels remaining to only add for some spaces

                    if (spaceGaps != 0)
                    {
                        pixelsPerSpace = spacePixels/spaceGaps;
                        leftOverSpacePixels = spacePixels - pixelsPerSpace*spaceGaps;
                    }

                    //now actually iterate over all nodes and set tweaked length
                    for (node = headNode; node != null; node = node.Next)
                    {
                        if (node.Type == TextNodeType.Space)
                        {
                            node.LengthTweak = pixelsPerSpace;
                            if (leftOverSpacePixels > 0)
                            {
                                node.LengthTweak += 1;
                                leftOverSpacePixels--;
                            }
                            else if (leftOverSpacePixels < 0)
                            {
                                node.LengthTweak -= 1;
                                leftOverSpacePixels++;
                            }
                        }
                        else if (node.Type == TextNodeType.Word)
                        {
                            int cGaps = (node.Text.Length - 1);
                            if (CrumbledWord(node))
                                cGaps++;

                            node.LengthTweak = cGaps*pixelsPerChar;


                            if (leftOverCharPixels >= cGaps)
                            {
                                node.LengthTweak += cGaps;
                                leftOverCharPixels -= cGaps;
                            }
                            else if (leftOverCharPixels <= -cGaps)
                            {
                                node.LengthTweak -= cGaps;
                                leftOverCharPixels += cGaps;
                            }
                            else
                            {
                                node.LengthTweak += leftOverCharPixels;
                                leftOverCharPixels = 0;
                            }
                        }

                        if ((!contract && node == expandEndNode) || (contract && node == contractEndNode))
                            break;
                    }
                }
            }
        }

        /// <summary>
        ///     Checks whether to skip trailing space on line because the next word does not
        ///     fit.
        ///     We only check one space - the assumption is that if there is more than one,
        ///     it is a deliberate attempt to insert spaces.
        /// </summary>
        /// <param name="node"></param>
        /// <param name="lengthSoFar"></param>
        /// <param name="boundWidth"></param>
        /// <returns></returns>
        private bool SkipTrailingSpace(TextNode node, float lengthSoFar, float boundWidth)
        {
            if (node.Type == TextNodeType.Space && node.Next != null && node.Next.Type == TextNodeType.Word &&
                node.ModifiedLength + node.Next.ModifiedLength + lengthSoFar > boundWidth)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        ///     Creates node list object associated with the text.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="bounds"></param>
        /// <returns></returns>
        public ProcessedText ProcessText(string text, SizeF maxSize, QFontAlignment alignment)
        {
            //TODO: bring justify and alignment calculations in here

            maxSize.Width = TransformWidthToViewport(maxSize.Width);

            var nodeList = new TextNodeList(text);
            nodeList.MeasureNodes(fontData, Options);

            //we "crumble" words that are two long so that that can be split up
            var nodesToCrumble = new List<TextNode>();
            foreach (TextNode node in nodeList)
                if ((!Options.WordWrap || node.Length >= maxSize.Width) && node.Type == TextNodeType.Word)
                    nodesToCrumble.Add(node);

            foreach (TextNode node in nodesToCrumble)
                nodeList.Crumble(node, 1);

            //need to measure crumbled words
            nodeList.MeasureNodes(fontData, Options);


            var processedText = new ProcessedText();
            processedText.textNodeList = nodeList;
            processedText.maxSize = maxSize;
            processedText.alignment = alignment;


            return processedText;
        }

        public void Begin()
        {
            GL.UseProgram(InstanceSharedState.ShaderVariables.ShaderProgram);
            if (Options.UseDefaultBlendFunction)
            {
                GL.Enable(EnableCap.Blend);
                GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);
            }
            GL.UniformMatrix4(InstanceSharedState.ShaderVariables.MVPUniformLocation, false, ref _projectionMatrix);
        }

        public void End()
        {
        }

        /// <summary>
        ///     Invalidates the internally cached viewport, causing it to be
        ///     reread the next time it is required. This should be called
        ///     if the viewport (is resized?) and text is to be rendered to the new
        ///     viewport.
        /// </summary>
        public static void RefreshViewport()
        {
            ViewportHelper.InvalidateViewport();
        }

        private void InitVBOs()
        {
            VertexArrayObjects = new QVertexArrayObject[fontData.Pages.Length];

            for (int i = 0; i < VertexArrayObjects.Length; i++)
            {
                int textureID = fontData.Pages[i].GLTexID;
                VertexArrayObjects[i] = new QVertexArrayObject(QFontSharedState, textureID);
            }

            if (fontData.dropShadow != null)
                fontData.dropShadow.InitVBOs();
        }

        public void ResetVBOs()
        {
            foreach (QVertexArrayObject buffer in VertexArrayObjects)
                buffer.Reset();

            if (fontData.dropShadow != null)
                fontData.dropShadow.ResetVBOs();
        }

        public void LoadVBOs()
        {
            foreach (QVertexArrayObject buffer in VertexArrayObjects)
                buffer.Load();

            if (fontData.dropShadow != null)
                fontData.dropShadow.LoadVBOs();
        }

        public void DrawVBOs()
        {
            GL.UseProgram(InstanceSharedState.ShaderVariables.ShaderProgram);
            GL.Uniform1(InstanceSharedState.ShaderVariables.SamplerLocation, 0);
            GL.ActiveTexture(InstanceSharedState.DefaultTextureUnit);
            GL.BindSampler(0, InstanceSharedState.SamplerID);

            if (fontData.dropShadow != null)
                fontData.dropShadow.DrawVBOs();

            foreach (QVertexArrayObject buffer in VertexArrayObjects)
                buffer.Draw();
        }

        #region IDisposable impl

        // Track whether Dispose has been called.
        private bool disposed;

        // Implement IDisposable.
        // Do not make this method virtual.
        // A derived class should not be able to override this method.
        public void Dispose()
        {
            Dispose(true);
            // This object will be cleaned up by the Dispose method.
            // Therefore, you should call GC.SupressFinalize to
            // take this object off the finalization queue
            // and prevent finalization code for this object
            // from executing a second time.
            GC.SuppressFinalize(this);
        }

        // Dispose(bool disposing) executes in two distinct scenarios.
        // If disposing equals true, the method has been called directly
        // or indirectly by a user's code. Managed and unmanaged resources
        // can be disposed.
        // If disposing equals false, the method has been called by the
        // runtime from inside the finalizer and you should not reference
        // other objects. Only unmanaged resources can be disposed.
        protected virtual void Dispose(bool disposing)
        {
            // Check to see if Dispose has already been called.
            if (!disposed)
            {
                // If disposing equals true, dispose all managed
                // and unmanaged resources.
                if (disposing)
                {
                    fontData.Dispose();
                    foreach (QVertexArrayObject buffer in VertexArrayObjects)
                        buffer.Dispose();
                }

                // Note disposing has been done.
                disposed = true;
            }
        }

        #endregion
    }

    public class ShaderVariables
    {
        public ShaderVariables(int shaderProgram, int mvpUniformLocation, int textureCoordAttribLocation, int positionCoordAttribLocation, int samplerLocation, int colorCoordAttribLocation)
        {
            ColorCoordAttribLocation = colorCoordAttribLocation;
            SamplerLocation = samplerLocation;
            PositionCoordAttribLocation = positionCoordAttribLocation;
            TextureCoordAttribLocation = textureCoordAttribLocation;
            MVPUniformLocation = mvpUniformLocation;
            ShaderProgram = shaderProgram;
        }

        public int ShaderProgram { get; private set; }
        public int MVPUniformLocation { get; private set; }
        public int TextureCoordAttribLocation { get; private set; }
        public int PositionCoordAttribLocation { get; private set; }
        public int SamplerLocation { get; private set; }
        public int ColorCoordAttribLocation { get; private set; }
    }

    public class SharedState
    {
        public SharedState(TextureUnit defaultTextureUnit, ShaderVariables shaderVariables, int samplerId)
        {
            DefaultTextureUnit = defaultTextureUnit;
            ShaderVariables = shaderVariables;
            SamplerID = samplerId;
        }

        public TextureUnit DefaultTextureUnit { get; private set; }
        public ShaderVariables ShaderVariables { get; private set; }
        public int SamplerID { get; private set; }
    }
}