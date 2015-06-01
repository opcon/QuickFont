using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Text;
using OpenTK;
using OpenTK.Graphics.OpenGL4;

namespace QuickFont
{
    /// <summary>
    /// This class represents a 'Drawing' on screen made off of Font renderings. eg. GlFontDrawingPrimitive
    /// </summary>
    public class QFontDrawing
    {
        //private QFontRenderOptions options = new QFontRenderOptions();
        private const string fragShaderSource = @"#version 130

uniform sampler2D tex_object;

in vec2 tc;
in vec4 colour;

out vec4 fragColour;

void main(void)
{
	fragColour = texture(tex_object, tc) * vec4(colour);
}";
        private const string vertShaderSource = @"#version 130

uniform mat4 proj_matrix;

in vec3 in_position;
in vec2 in_tc;
in vec4 in_colour;

out vec2 tc;
out vec4 colour;

void main(void)
{
	tc = in_tc;
	colour = in_colour;
	gl_Position = proj_matrix * vec4(in_position, 1.); 
}";

        private static SharedState _QFontSharedState;
        private readonly Stack<QFontRenderOptions> optionsStack = new Stack<QFontRenderOptions>();

        public QVertexArrayObject[] VertexArrayObjects = new QVertexArrayObject[0];
        private SharedState _instanceSharedState;

        private Matrix4 _projectionMatrix;

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
            get { return QFont._QFontSharedState; }
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


        public GlFontDrawingPimitive GlFontDrawingPimitive
        {
            get { return _glFontDrawingPimitive; }
        }

        #region Constructors and font builders

        /// <summary>
        ///     Used for creating a dropshadow QFont object
        /// </summary>
        /// <param name="fontData"></param>
        internal QFont(QFontData fontData)
        {
            _glFontDrawingPimitive = new GlFontDrawingPimitive(new GlFont(fontData));
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

            using (Font font = QuickFont.GlFont.GetFont(fontPath, size, style, config == null ? 1 : config.SuperSampleLevels, fontScale))
            {
                _glFontDrawingPimitive = new GlFontDrawingPimitive(this);
                GlFontDrawingPimitive.GlFont = new GlFont(font, config);
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
            _glFontDrawingPimitive = new GlFontDrawingPimitive(new GlFont(qfontPath, loaderConfig, downSampleFactor, projectionMatrix), this);
            _projectionMatrix = projectionMatrix;
            Viewport? transToVp = null;
            float fontScale = 1f;
            if (loaderConfig.TransformToCurrentOrthogProjection)
                transToVp = OrthogonalTransform(out fontScale);

            if (transToVp != null)
                Options.TransformToViewport = transToVp;
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

            //Now we have all the information, time to create the immutable shared state object
            var shaderVariables = new ShaderVariables(prog, mvpLoc, tcLoc, posLoc, samplerLoc, colLoc);
            var sharedState = new SharedState(TextureUnit.Texture0, shaderVariables);

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
                if (GlFontDrawingPimitive.GlFont.FontData.dropShadow != null)
                    GlFontDrawingPimitive.GlFont.FontData.dropShadow._instanceSharedState = state;
            }
        }

        #endregion

        /// <summary>
        ///     When TransformToOrthogProjection is enabled, we need to get the current orthogonal transformation,
        ///     the font scale, and ensure that the projection is actually orthogonal
        /// </summary>
        /// <param name="fontScale"></param>
        /// <param name="viewportTransform"></param>
         [Obsolete]
        private Viewport OrthogonalTransform(out float fontScale)
        {
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

        /// <summary>
        /// Helper method to reduce lines of code related to simple font drawing.
        /// Calls Begin(), then LoadVBOs(), then DrawVBOs(), then End()
        /// </summary>
        public void Draw()
        {
            Begin();
            LoadVBOs();
            DrawVBOs();
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
            VertexArrayObjects = new QVertexArrayObject[GlFontDrawingPimitive.GlFont.FontData.Pages.Length];

            for (int i = 0; i < VertexArrayObjects.Length; i++)
            {
                int textureID = GlFontDrawingPimitive.GlFont.FontData.Pages[i].GLTexID;
                VertexArrayObjects[i] = new QVertexArrayObject(QFontSharedState, textureID);
            }

            if (GlFontDrawingPimitive.GlFont.FontData.dropShadow != null)
                GlFontDrawingPimitive.GlFont.FontData.dropShadow.InitVBOs();
        }

        public void ResetVBOs()
        {
            foreach (QVertexArrayObject buffer in VertexArrayObjects)
                buffer.Reset();

            if (GlFontDrawingPimitive.GlFont.FontData.dropShadow != null)
                GlFontDrawingPimitive.GlFont.FontData.dropShadow.ResetVBOs();
        }

        public void LoadVBOs()
        {
            foreach (QVertexArrayObject buffer in VertexArrayObjects)
                buffer.Load();

            if (GlFontDrawingPimitive.GlFont.FontData.dropShadow != null)
                GlFontDrawingPimitive.GlFont.FontData.dropShadow.LoadVBOs();
        }

        public void DrawVBOs()
        {
            GL.UseProgram(InstanceSharedState.ShaderVariables.ShaderProgram);
            GL.Uniform1(InstanceSharedState.ShaderVariables.SamplerLocation, 0);
            GL.ActiveTexture(InstanceSharedState.DefaultTextureUnit);

            if (GlFontDrawingPimitive.GlFont.FontData.dropShadow != null)
                GlFont.FontData.dropShadow.GlFontDrawingPimitive.GlFont.DrawVBOs();

            foreach (QVertexArrayObject buffer in VertexArrayObjects)
                buffer.Draw();
        }

        #region IDisposable impl

        // Track whether Dispose has been called.
        private bool disposed;
        private readonly GlFontDrawingPimitive _glFontDrawingPimitive;

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// Do not make this method virtual. A derived class should not be able to override this method
        /// </summary>
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
                    GlFontDrawingPimitive.GlFont.FontData.Dispose();
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
        public SharedState(TextureUnit defaultTextureUnit, ShaderVariables shaderVariables)
        {
            DefaultTextureUnit = defaultTextureUnit;
            ShaderVariables = shaderVariables;
        }

        public TextureUnit DefaultTextureUnit { get; private set; }
        public ShaderVariables ShaderVariables { get; private set; }
    }
}