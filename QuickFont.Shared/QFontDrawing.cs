using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Linq;
using OpenTK;
#if OPENGL_ES
using OpenTK.Graphics.ES20;
#else
using OpenTK.Graphics.OpenGL4;
#endif

namespace QuickFont
{
    /// <summary>
    /// <see cref="QFontDrawing"/> manages a collection of <see cref="QFontDrawingPrimitive"/>'s
    /// and handles printing text to the screen
    /// </summary>
    public class QFontDrawing : IDisposable
    {
        private const string SHADER_VERSION_STRING130 = "#version 130\n\n";
        private const string SHADER_VERSION_STRING140 = "#version 140\n\n";
        private const string SHADER_VERSION_STRING150 = "#version 150\n\n";

        private static QFontSharedState _sharedState;

        /// <summary>
        /// The <see cref="QVertexArrayObject"/> used by this <see cref="QFontDrawing"/>
        /// </summary>
        public QVertexArrayObject VertexArrayObject;

        private QFontSharedState _instanceSharedState;
        private readonly List<QFontDrawingPrimitive> _glFontDrawingPrimitives;
        private readonly bool _useDefaultBlendFunction;

        private Matrix4 _projectionMatrix;

        /// <summary>
        /// Creates a new instance of <see cref="QFontDrawing"/>
        /// </summary>
        /// <param name="useDefaultBlendFunction">Whether to use the default blend function</param>
        /// <param name="state">The QFontSharedState of this object. If null, will use the static state</param>
        public QFontDrawing(bool useDefaultBlendFunction = true, QFontSharedState state = null)
        {
            _useDefaultBlendFunction = useDefaultBlendFunction;
            _glFontDrawingPrimitives = new List<QFontDrawingPrimitive>();
            InitialiseState(state);
        }

        /// <summary>
        /// The static shared state for all <see cref="QFontDrawing"/> objects
        /// </summary>
        public static QFontSharedState SharedState
        {
            get { return _sharedState; }
        }

        /// <summary>
        /// Returns the instance shared state if it exists, otherwise
        /// returns the static shared state
        /// </summary>
        public QFontSharedState InstanceSharedState
        {
            get { return _instanceSharedState ?? SharedState; }
        }

        /// <summary>
        /// The projection matrix used for text rendering
        /// </summary>
        public Matrix4 ProjectionMatrix
        {
            get { return _projectionMatrix; }
            set { _projectionMatrix = value; }
        }

        /// <summary>
        /// A list of <see cref="QFontDrawingPrimitive"/>'s that will be drawn
        /// when the <see cref="Draw"/> method is called
        /// </summary>
        public List<QFontDrawingPrimitive> DrawingPrimitives
        {
            get { return _glFontDrawingPrimitives; }
        }

        /// <summary>
        /// Load shader string from resource
        /// </summary>
        /// <param name="path">filename of Shader</param>
        /// <returns>The loaded shader</returns>
        private static string LoadShaderFromResource(string path)
        {
            var assembly = Assembly.GetExecutingAssembly();
            path = assembly.GetManifestResourceNames().Where(f => f.EndsWith(path)).First();

            var resourceStream =
                assembly.GetManifestResourceStream(path);
            if (resourceStream == null)
                throw new AccessViolationException("Error loading shader resource");

            string result;
            using (var sr = new StreamReader(resourceStream))
            {
                result = sr.ReadToEnd();
            }

            return result;
        }

        /// <summary>
        ///     Initializes the static shared render state using builtin shaders
        /// </summary>
        private static void InitialiseStaticState()
        {
            // Enable 2D textures
            GL.Enable(EnableCap.Texture2D);

            //Create vertex and fragment shaders
            int vert = GL.CreateShader(ShaderType.VertexShader);
            int frag = GL.CreateShader(ShaderType.FragmentShader);

            //Check shaders were created succesfully
            if (vert == -1 || frag == -1)
                throw new Exception(string.Format("Error creating shader name for {0}", vert == -1 ? (frag == -1 ? "vert and frag shaders" : "vert shader") : "frag shader"));

            // We try to compile the shaders with ever increasing version numbers (up to 1.50)
            // This fixes a bug on MaxOSX where the Core profile only supports shaders >= 1.50 (or sometimes 1.40)
            var versions = new[] { SHADER_VERSION_STRING130, SHADER_VERSION_STRING140, SHADER_VERSION_STRING150 };

            // Holds the compilation status of the shaders
            int vertCompileStatus = 0;
            int fragCompileStatus = 0;

            foreach (var version in versions)
            {
#if OPENGL_ES
                GL.ShaderSource(vert, LoadShaderFromResource("simple_es.vs"));
                GL.ShaderSource(frag, LoadShaderFromResource("simple_es.fs"));
#else
                GL.ShaderSource(vert, version + LoadShaderFromResource("simple.vs"));
                GL.ShaderSource(frag, version + LoadShaderFromResource("simple.fs"));
#endif
                
                GL.CompileShader(vert);
                GL.CompileShader(frag);

                GL.GetShader(vert, ShaderParameter.CompileStatus, out vertCompileStatus);
                GL.GetShader(frag, ShaderParameter.CompileStatus, out fragCompileStatus);

                // Check shaders were compiled correctly
                // If they have, we break out of the foreach loop as we have found the minimum supported glsl version
                if (vertCompileStatus != 0 && fragCompileStatus != 0)
                    break;

                // Otherwise continue with the loop
            }

            // Check that we ended up with a compiled shader at the end of all this
            // These will only be 0 if all compilations with different versions failed,
            // since we break out of the version loop as soon as one compiles
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
            int projLoc = GL.GetUniformLocation(prog, "proj_matrix");
            int mvLoc = GL.GetUniformLocation(prog, "modelview_matrix");
            int samplerLoc = GL.GetUniformLocation(prog, "tex_object");
            int posLoc = GL.GetAttribLocation(prog, "in_position");
            int tcLoc = GL.GetAttribLocation(prog, "in_tc");
            int colLoc = GL.GetAttribLocation(prog, "in_colour");

            //Now we have all the information, time to create the shared state object
            var shaderVariables = new ShaderLocations
            {
                ShaderProgram = prog,
                ProjectionMatrixUniformLocation = projLoc,
                ModelViewMatrixUniformLocation = mvLoc,
                TextureCoordAttribLocation = tcLoc,
                PositionCoordAttribLocation = posLoc,
                SamplerLocation = samplerLoc,
                ColorCoordAttribLocation = colLoc,

            };
            var sharedState = new QFontSharedState(TextureUnit.Texture0, shaderVariables);

            _sharedState = sharedState;
        }

        /// <summary>
        ///     Initialises the instance render state
        /// </summary>
        /// <param name="state">
        ///     If state is null, this method will instead initialise the static state, which is returned when no
        ///     instance state is set
        /// </param>
        private void InitialiseState(QFontSharedState state)
        {
            if (state != null) _instanceSharedState = state;
            else if (SharedState == null) InitialiseStaticState();
        }

        /// <summary>
        /// Draws the text stored in this drawing
        /// </summary>
        public void Draw()
        {
            GL.UseProgram(InstanceSharedState.ShaderVariables.ShaderProgram);
            if (_useDefaultBlendFunction)
            {
                GL.Enable(EnableCap.Blend);
                GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
            }
            GL.UniformMatrix4(InstanceSharedState.ShaderVariables.ProjectionMatrixUniformLocation, false, ref _projectionMatrix);

            GL.Uniform1(InstanceSharedState.ShaderVariables.SamplerLocation, 0);
            GL.ActiveTexture(InstanceSharedState.DefaultTextureUnit);

            int start = 0;
            VertexArrayObject.Bind();
            foreach (var primitive in _glFontDrawingPrimitives)
            {
                GL.UniformMatrix4(InstanceSharedState.ShaderVariables.ModelViewMatrixUniformLocation, false, ref primitive.ModelViewMatrix);
                var dpt = PrimitiveType.Triangles;
                GL.ActiveTexture(SharedState.DefaultTextureUnit);

                // Use DrawArrays - first draw drop shadows, then draw actual font primitive
                if (primitive.ShadowVertexRepr.Count > 0)
                {
                    //int index = primitive.Font.FontData.CalculateMaxHeight();
                    GL.BindTexture(TextureTarget.Texture2D, primitive.Font.FontData.DropShadowFont.FontData.Pages[0].TextureID);
                    GL.DrawArrays(dpt, start, primitive.ShadowVertexRepr.Count);
                    start += primitive.ShadowVertexRepr.Count;
                }

                GL.BindTexture(TextureTarget.Texture2D, primitive.Font.FontData.Pages[0].TextureID);
                GL.DrawArrays(dpt, start, primitive.CurrentVertexRepr.Count);
                start += primitive.CurrentVertexRepr.Count;
            }

#if !OPENGL_ES
            GL.BindVertexArray(0);
#endif
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
        }

        /// <summary>
        /// Bind the 0'th shader and disable the attributes used
        /// </summary>
        public void DisableShader()
        {
            GL.UseProgram(0);
            VertexArrayObject.DisableAttributes();
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
            if (VertexArrayObject == null)
            {
                VertexArrayObject = new QVertexArrayObject(SharedState);
            }

            VertexArrayObject.Reset();

            foreach (var primitive in _glFontDrawingPrimitives)
            {
                VertexArrayObject.AddVertexes(primitive.ShadowVertexRepr);
                VertexArrayObject.AddVertexes(primitive.CurrentVertexRepr);
            }
            VertexArrayObject.Load();
        }

        /// <summary>
        /// Prints the specified text with the given render options
        /// </summary>
        /// <param name="font">The <see cref="QFont"/> to print the text with</param>
        /// <param name="text">The text to print</param>
        /// <param name="position">The position of the text</param>
        /// <param name="opt">The text render options</param>
        /// <returns>The size of the printed text</returns>
        public SizeF Print(QFont font, ProcessedText text, Vector3 position, QFontRenderOptions opt)
        {
            var dp = new QFontDrawingPrimitive(font, opt);
            DrawingPrimitives.Add(dp);
            return dp.Print(text, position, opt.ClippingRectangle);
        }

        /// <summary>
        /// Prints the specified text
        /// </summary>
        /// <param name="font">The <see cref="QFont"/> to print the text with</param>
        /// <param name="processedText">The processed text to print</param>
        /// <param name="position">The position of the text</param>
        /// <param name="colour">The colour of the text</param>
        /// <returns>The size of the printed text</returns>
        public SizeF Print(QFont font, ProcessedText processedText, Vector3 position, Color? colour = null)
        {
            var dp = new QFontDrawingPrimitive(font);
            DrawingPrimitives.Add(dp);
            return colour.HasValue ? dp.Print(processedText, position, colour.Value) : dp.Print(processedText, position);
        }

        /// <summary>
        /// Prints the specified text with the given alignment and render options
        /// </summary>
        /// <param name="font">The <see cref="QFont"/> to print the text with</param>
        /// <param name="text">The text to print</param>
        /// <param name="position">The position of the text</param>
        /// <param name="alignment">The alignment of the text</param>
        /// <param name="opt">The render options</param>
        /// <returns>The size of the printed text</returns>
        public SizeF Print(QFont font, string text, Vector3 position, QFontAlignment alignment, QFontRenderOptions opt)
        {
            var dp = new QFontDrawingPrimitive(font, opt);
            DrawingPrimitives.Add(dp);
            return dp.Print(text, position, alignment, opt.ClippingRectangle);
        }

        /// <summary>
        /// Prints the specified text with the given alignment, color and clipping rectangle
        /// </summary>
        /// <param name="font">The <see cref="QFont"/> to print the text with</param>
        /// <param name="text">The text to print</param>
        /// <param name="position">The position of the text</param>
        /// <param name="alignment">The alignment of the text</param>
        /// <param name="color">The colour of the text</param>
        /// <param name="clippingRectangle">The clipping rectangle to scissor test the text with</param>
        /// <returns>The size of the printed text</returns>
        public SizeF Print(QFont font, string text, Vector3 position, QFontAlignment alignment, Color? color = null, Rectangle clippingRectangle = default(Rectangle))
        {
            var dp = new QFontDrawingPrimitive(font);
            DrawingPrimitives.Add(dp);
            if( color.HasValue )
                return dp.Print(text, position, alignment, color.Value, clippingRectangle);
            return dp.Print(text, position, alignment, clippingRectangle);
        }

        /// <summary>
        /// Prints the specified text with the given maximum size, alignment and clipping rectangle
        /// </summary>
        /// <param name="font">The <see cref="QFont"/> to print the text with</param>
        /// <param name="text">The text to print</param>
        /// <param name="position">The position of the text</param>
        /// <param name="maxSize">The maximum bounding size of the text</param>
        /// <param name="alignment">The text alignment</param>
        /// <param name="clippingRectangle">The clipping rectangle to scissor test the text with</param>
        /// <returns>The size of the printed text</returns>
        public SizeF Print(QFont font, string text, Vector3 position, SizeF maxSize, QFontAlignment alignment, Rectangle clippingRectangle = default(Rectangle))
        {
            var dp = new QFontDrawingPrimitive(font);
            DrawingPrimitives.Add(dp);
            return dp.Print(text, position, maxSize, alignment, clippingRectangle);
        }

        /// <summary>
        /// Prints the specified text with the given maximum size, alignment and render options
        /// </summary>
        /// <param name="font">The <see cref="QFont"/> to print the text with</param>
        /// <param name="text">The text to print</param>
        /// <param name="position">The position of the text</param>
        /// <param name="maxSize">The maximum bounding size of the text</param>
        /// <param name="alignment">The text alignment</param>
        /// <param name="opt">The render options</param>
        /// <returns>The size of the printed text</returns>
        public SizeF Print(QFont font, string text, Vector3 position, SizeF maxSize, QFontAlignment alignment, QFontRenderOptions opt )
        {
            var dp = new QFontDrawingPrimitive(font, opt);
            DrawingPrimitives.Add(dp);
            return dp.Print(text, position, maxSize, alignment, opt.ClippingRectangle);
        }

        /// <summary>
        /// Track whether <see cref="Dispose()"/> has been called
        /// </summary>
        private bool _disposed;

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

        /// <summary>
        /// Handles disposing objects
        /// </summary>
        /// <param name="disposing"></param>
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
                    if (VertexArrayObject != null)
                    {
                        VertexArrayObject.Dispose();
                        VertexArrayObject = null;
                    }
                }

                // Note disposing has been done.
                _disposed = true;
            }
        }
    }


    /// <summary>
    /// Holds the shader state
    /// </summary>
    public class ShaderLocations
    {
        /// <summary>
        /// The shader program name
        /// </summary>
        public int ShaderProgram { get; set; }

        /// <summary>
        /// The Projection matrix uniform location
        /// </summary>
        public int ProjectionMatrixUniformLocation { get; set; }

        /// <summary>
        /// The Model-View matrix uniform location
        /// </summary>
        public int ModelViewMatrixUniformLocation { get; set; }

        /// <summary>
        /// The texture coordinate attribute location
        /// </summary>
        public int TextureCoordAttribLocation { get; set; }

        /// <summary>
        /// The position coordinate attribute location
        /// </summary>
        public int PositionCoordAttribLocation { get; set; }

        /// <summary>
        /// The texture sample location
        /// </summary>
        public int SamplerLocation { get; set; }

        /// <summary>
        /// The color coordinate attribute location
        /// </summary>
        public int ColorCoordAttribLocation { get; set; }
    }

    /// <summary>
    /// The shared state of the <see cref="QFontDrawing"/> object.
    /// This can be shared between different <see cref="QFontDrawing"/> objects
    /// </summary>
    public class QFontSharedState
    {
        /// <summary>
        /// Creates a new instance of <see cref="QFontSharedState"/>
        /// </summary>
        /// <param name="defaultTextureUnit">The default texture unit</param>
        /// <param name="shaderVariables">The shader variables</param>
        public QFontSharedState(TextureUnit defaultTextureUnit, ShaderLocations shaderVariables)
        {
            DefaultTextureUnit = defaultTextureUnit;
            ShaderVariables = shaderVariables;
        }

        /// <summary>
        /// The default texture unit of this shared state
        /// </summary>
        public TextureUnit DefaultTextureUnit { get; }

        /// <summary>
        /// The shader variables of this shared state
        /// </summary>
        public ShaderLocations ShaderVariables { get; }
    }

}
