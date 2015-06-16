using System;
using System.Collections.Generic;
using System.Drawing;
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

        public QVertexArrayObject _vertexArrayObject;
        private SharedState _instanceSharedState;
        private readonly List<QFontDrawingPimitive> _glFontDrawingPimitives;
        private readonly bool _useDefaultBlendFunction;

        private Matrix4 _projectionMatrix;

        public QFontDrawing(bool useDefaultBlendFunction = true)
        {
            _useDefaultBlendFunction = useDefaultBlendFunction;
            _glFontDrawingPimitives =new List<QFontDrawingPimitive>();
            InitialiseState();
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

        public List<QFontDrawingPimitive> DrawingPimitiveses
        {
            get { return _glFontDrawingPimitives; }
        }

        /// <summary>
        ///     Initialises the static shared render state
        /// </summary>
        private static void InitialiseStaticState()
        {
            GL.Enable(EnableCap.Texture2D);

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
        private void InitialiseState()
        {
            if (QFontSharedState == null) InitialiseStaticState();
        }

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
        /// Helper method to reduce lines of code related to simple font drawing.
        /// Calls Begin(), then LoadVBOs(), then DrawVBOs(), then End()
        /// </summary>
        public void Draw()
        {
            GL.UseProgram(InstanceSharedState.ShaderVariables.ShaderProgram);
            if (_useDefaultBlendFunction)
            {
                GL.Enable(EnableCap.Blend);
                GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);
            }
            GL.UniformMatrix4(InstanceSharedState.ShaderVariables.MVPUniformLocation, false, ref _projectionMatrix);

            GL.Uniform1(InstanceSharedState.ShaderVariables.SamplerLocation, 0);
            GL.ActiveTexture(InstanceSharedState.DefaultTextureUnit);

            int start = 0;
            _vertexArrayObject.Bind();
            foreach (var primitive in _glFontDrawingPimitives)
            {
                var dpt = PrimitiveType.Triangles;
                GL.ActiveTexture(QFontSharedState.DefaultTextureUnit);

                // Use DrawArrays - first draw drop shadows, then draw actual font primitive
                if (primitive.ShadowVertexRepr.Count > 0)
                {
                    //int index = primitive.Font.FontData.CalculateMaxHeight();
                    GL.BindTexture(TextureTarget.Texture2D, primitive.Font.FontData.dropShadowFont.FontData.Pages[0].GLTexID);
                    GL.DrawArrays(dpt, start, primitive.ShadowVertexRepr.Count);
                    start += primitive.ShadowVertexRepr.Count;
                }

                GL.BindTexture(TextureTarget.Texture2D, primitive.Font.FontData.Pages[0].GLTexID);
                GL.DrawArrays(dpt, start, primitive.CurrentVertexRepr.Count);
                start += primitive.CurrentVertexRepr.Count;
            }

            GL.BindVertexArray(0);
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
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

        /// <summary>
        /// Refreshes the buffers with the current state of the 'font'drawing 
        /// as defined in DrawingPrimitives
        /// </summary>
        public void RefreshBuffers()
        {
            if (_vertexArrayObject == null)
            {
                _vertexArrayObject = new QVertexArrayObject(QFontSharedState);
            }

            _vertexArrayObject.Reset();

            foreach (var primitive in _glFontDrawingPimitives)
            {
                _vertexArrayObject.AddVertexes(primitive.ShadowVertexRepr);
                _vertexArrayObject.AddVertexes(primitive.CurrentVertexRepr);
            }
            _vertexArrayObject.Load();
        }

        public SizeF Print(QFont font, ProcessedText text, Vector3 position, QFontRenderOptions opt)
        {
            var dp = new QFontDrawingPimitive(font, opt);
            DrawingPimitiveses.Add(dp);
            return dp.Print(text, position, opt.ClippingRectangle);
        }

        public SizeF Print(QFont font, ProcessedText processedText, Vector3 position, Color? colour = null, Rectangle clippingRectangle = default(Rectangle))
        {
            var dp = new QFontDrawingPimitive(font);
            DrawingPimitiveses.Add(dp);
            if (colour.HasValue)
                return dp.Print(processedText, position, colour.Value);
            else
                return dp.Print(processedText, position);
        }

        public SizeF Print(QFont font, string text, Vector3 position, QFontAlignment alignment, QFontRenderOptions opt)
        {
            var dp = new QFontDrawingPimitive(font, opt);
            DrawingPimitiveses.Add(dp);
            return dp.Print(text, position, alignment, opt.ClippingRectangle);
        }

        public SizeF Print(QFont font, string text, Vector3 position, QFontAlignment alignment, Color? color = null, Rectangle clippingRectangle = default(Rectangle))
        {
            var dp = new QFontDrawingPimitive(font);
            DrawingPimitiveses.Add(dp);
            if( color.HasValue )
                return dp.Print(text, position, alignment, color.Value, clippingRectangle);
            return dp.Print(text, position, alignment, clippingRectangle);
        }

        public SizeF Print(QFont font, string text, Vector3 position, SizeF maxSize, QFontAlignment alignment, Rectangle clippingRectangle = default(Rectangle))
        {
            var dp = new QFontDrawingPimitive(font);
            DrawingPimitiveses.Add(dp);
            return dp.Print(text, position, maxSize, alignment, clippingRectangle);
        }

        public SizeF Print(QFont font, string text, Vector3 position, SizeF maxSize, QFontAlignment alignment, QFontRenderOptions opt )
        {
            var dp = new QFontDrawingPimitive(font, opt);
            DrawingPimitiveses.Add(dp);
            return dp.Print(text, position, maxSize, alignment, opt.ClippingRectangle);
        }

        #region IDisposable impl

        // Track whether Dispose has been called.
        private bool disposed;

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
                    //QFontDrawingPimitive.Font.FontData.Dispose();
                    _vertexArrayObject.Dispose();
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