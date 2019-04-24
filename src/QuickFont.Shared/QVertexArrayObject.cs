using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using OpenTK;
#if OPENGL_ES
using OpenTK.Graphics.ES20;
#else
using OpenTK.Graphics.OpenGL4;
#endif

namespace QuickFont
{
    /// <summary>
    /// A wrapper around OpenGL's vertex array objects
    /// </summary>
    public class QVertexArrayObject : IDisposable
    {
        private const int INITIAL_SIZE = 1000;

        private int _bufferSize;
        private int _bufferMaxVertexCount;

        /// <summary>
        /// The number of vertices stored in the vertex array buffer
        /// </summary>
        public int VertexCount;
#if !OPENGL_ES
        /// <summary>
        /// The vertex array object ID
        /// </summary>
        private int _VAOID;
#endif

        /// <summary>
        /// The vertex buffer object ID
        /// </summary>
        private int _VBOID;

        /// <summary>
        /// The shared state of this <see cref="QVertexArrayObject"/>
        /// </summary>
        public readonly QFontSharedState QFontSharedState;

        private List<QVertex> _vertices;
        private QVertex[] _vertexArray;

        private static readonly int QVertexStride;

        /// <summary>
        /// Default static constructor. Initializes the <see cref="QVertexStride"/> field
        /// </summary>
        static QVertexArrayObject()
        {
            QVertexStride = BlittableValueType.StrideOf(default(QVertex));
        }

        /// <summary>
        /// Creates a new instance of <see cref="QVertexArrayObject"/>
        /// </summary>
        /// <param name="state">The <see cref="QFontSharedState"/> to use</param>
        public QVertexArrayObject(QFontSharedState state)
        {
            QFontSharedState = state;

            _vertices = new List<QVertex>(INITIAL_SIZE);
            _bufferMaxVertexCount = INITIAL_SIZE;
            _bufferSize = _bufferMaxVertexCount * QVertexStride;

#if !OPENGL_ES
            _VAOID = GL.GenVertexArray();
#endif

            GL.UseProgram(QFontSharedState.ShaderVariables.ShaderProgram);

#if !OPENGL_ES
            GL.BindVertexArray(_VAOID);
#endif

            GL.GenBuffers(1, out _VBOID);
            GL.BindBuffer(BufferTarget.ArrayBuffer, _VBOID);
            EnableAttributes();

            GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)_bufferSize, IntPtr.Zero, BufferUsageHint.StreamDraw );

            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);

#if !OPENGL_ES
            GL.BindVertexArray(0);
#endif
        }

        /// <summary>
        /// Add verticies to the vertex collection
        /// </summary>
        /// <param name="vertices">The verticies to add</param>
        internal void AddVertexes(IList<QVertex> vertices)
        {
            VertexCount += vertices.Count;
            _vertices.AddRange(vertices);
        }

        /// <summary>
        /// Adds a single vertex to the vertex collection
        /// </summary>
        /// <param name="position">The vertex position</param>
        /// <param name="textureCoord">The vertex texture coordinate</param>
        /// <param name="colour">The vertex colour</param>
        public void AddVertex(Vector3 position, Vector2 textureCoord, Vector4 colour)
        {
            VertexCount++;
            _vertices.Add(new QVertex
            {
                Position = position,
                TextureCoord = textureCoord,
                VertexColor = colour
            });
        }

        /// <summary>
        /// Loads the current vertex collection into the VBO
        /// </summary>
        public void Load()
        {
            if (VertexCount == 0)
                return;
            _vertexArray = _vertices.ToArray();
            GL.BindBuffer(BufferTarget.ArrayBuffer, _VBOID);

#if OPENGL_ES
            EnableAttributes();
#else
            GL.BindVertexArray(_VAOID);
#endif

            if (VertexCount > _bufferMaxVertexCount)
            {
                while (VertexCount > _bufferMaxVertexCount)
                {
                    _bufferMaxVertexCount += INITIAL_SIZE;
                    _bufferSize = _bufferMaxVertexCount*QVertexStride;
                }

                //reinitialise buffer with new _bufferSize
                GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr) _bufferSize, IntPtr.Zero, BufferUsageHint.StreamDraw);
            }
            GL.BufferSubData(BufferTarget.ArrayBuffer, IntPtr.Zero, (IntPtr) (VertexCount*QVertexStride), _vertexArray);
        }

        /// <summary>
        /// Clears the vertex collection
        /// </summary>
        public void Reset()
        {
            _vertices.Clear();
            VertexCount = 0;
        }

        /// <summary>
        /// Binds the vertex array object and vertex buffer object
        /// </summary>
        public void Bind()
        {
#if OPENGL_ES
            GL.BindBuffer(BufferTarget.ArrayBuffer, _VBOID);
            EnableAttributes();
#else
            GL.BindVertexArray(_VAOID);
#endif
        }


        /// <summary>
        /// Disable the vertex attribute arrays
        /// </summary>
        public void DisableAttributes()
        {
            GL.DisableVertexAttribArray(QFontSharedState.ShaderVariables.PositionCoordAttribLocation);
            GL.DisableVertexAttribArray(QFontSharedState.ShaderVariables.TextureCoordAttribLocation);
            GL.DisableVertexAttribArray(QFontSharedState.ShaderVariables.ColorCoordAttribLocation);
        }

        /// <summary>
        /// Enable the vertex attribute arrays
        /// </summary>
        private void EnableAttributes()
        {
            int stride = QVertexStride;
            GL.EnableVertexAttribArray(QFontSharedState.ShaderVariables.PositionCoordAttribLocation);
            GL.EnableVertexAttribArray(QFontSharedState.ShaderVariables.TextureCoordAttribLocation);
            GL.EnableVertexAttribArray(QFontSharedState.ShaderVariables.ColorCoordAttribLocation);
            GL.VertexAttribPointer(QFontSharedState.ShaderVariables.PositionCoordAttribLocation, 3,
                VertexAttribPointerType.Float, false, stride, IntPtr.Zero);
            GL.VertexAttribPointer(QFontSharedState.ShaderVariables.TextureCoordAttribLocation, 2, VertexAttribPointerType.Float,
                false,
                stride, new IntPtr(3*sizeof (float)));
            GL.VertexAttribPointer(QFontSharedState.ShaderVariables.ColorCoordAttribLocation, 4, VertexAttribPointerType.Float,
                false, stride, new IntPtr(5*sizeof (float)));
        }

        private bool _disposedValue; // To detect redundant calls

        /// <summary>
        /// Disposes resources used by <see cref="QVertexArrayObject"/>
        /// </summary>
        /// <param name="disposing">Whether to dispose managed resources as well as unmanaged</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    GL.DeleteBuffers(1, ref _VBOID);
#if !OPENGL_ES
                    GL.DeleteVertexArrays(1, ref _VAOID);
#endif

                    if (_vertices != null)
                    {
                        _vertices.Clear();
                        _vertices = null;
                    }
                }

                _vertexArray = null;

                _disposedValue = true;
            }
        }

        /// <summary>
        /// Disposes resources used by <see cref="QVertexArrayObject"/>
        /// </summary>
        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
        }
    }

    /// <summary>
    /// Vertex data structure
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct QVertex
    {
        public Vector3 Position;
        public Vector2 TextureCoord;
        public Vector4 VertexColor;
    }
}
