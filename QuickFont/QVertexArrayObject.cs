using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using OpenTK;
using OpenTK.Graphics.ES20;

namespace QuickFont
{
    public class QVertexArrayObject : IDisposable
    {
        private const int InitialSize = 1000;
        private int _bufferSize;
        private int _bufferMaxVertexCount;
        public int VertexCount;
        
        private int VBOID;

        public readonly SharedState QFontSharedState;

        private List<QVertex> Vertices;
        private QVertex[] VertexArray;

        private static readonly int QVertexStride;

        static QVertexArrayObject()
        {
            QVertexStride = BlittableValueType.StrideOf(default(QVertex));
        }

        public QVertexArrayObject(SharedState state)
        {
            QFontSharedState = state;

            Vertices = new List<QVertex>(InitialSize);
            _bufferMaxVertexCount = InitialSize;
            _bufferSize = _bufferMaxVertexCount * QVertexStride;
            
            GL.UseProgram(QFontSharedState.ShaderVariables.ShaderProgram);

            GL.GenBuffers(1, out VBOID);
            GL.BindBuffer(BufferTarget.ArrayBuffer, VBOID);
            EnableAttributes();

            GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)_bufferSize, IntPtr.Zero, BufferUsageHint.StreamDraw );

            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
        }

        internal void AddVertexes(IList<QVertex> vertices)
        {
            VertexCount += vertices.Count;
            Vertices.AddRange(vertices);
        }

        public void AddVertex(Vector3 position, Vector2 textureCoord, Vector4 colour)
        {
            VertexCount++;
            Vertices.Add(new QVertex
            {
                Position = position,
                TextureCoord = textureCoord,
                VertexColor = colour
            });
        }

        public void Load()
        {
            if (VertexCount == 0)
                return;
            VertexArray = Vertices.ToArray();
            GL.BindBuffer(BufferTarget.ArrayBuffer, VBOID);
            EnableAttributes();

            if (VertexCount > _bufferMaxVertexCount)
            {
                while (VertexCount > _bufferMaxVertexCount)
                {
                    _bufferMaxVertexCount += InitialSize;
                    _bufferSize = _bufferMaxVertexCount*QVertexStride;
                }

                //reinitialise buffer with new _bufferSize
                GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr) _bufferSize, IntPtr.Zero, BufferUsageHint.StreamDraw);
            }
            GL.BufferSubData(BufferTarget.ArrayBuffer, IntPtr.Zero, (IntPtr) (VertexCount*QVertexStride), VertexArray);
        }

        public void Reset()
        {
            Vertices.Clear();
            VertexCount = 0;
        }

        public void Bind()
        {
            GL.BindBuffer(BufferTarget.ArrayBuffer, VBOID);
            EnableAttributes();
        }

        public void Dispose()
        {
            GL.DeleteBuffers(1, ref VBOID);
        }

        public void DisableAttributes()
        {
            GL.DisableVertexAttribArray(QFontSharedState.ShaderVariables.PositionCoordAttribLocation);
            GL.DisableVertexAttribArray(QFontSharedState.ShaderVariables.TextureCoordAttribLocation);
            GL.DisableVertexAttribArray(QFontSharedState.ShaderVariables.ColorCoordAttribLocation);
        }

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
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct QVertex
    {
        public Vector3 Position;
        public Vector2 TextureCoord;
        public Vector4 VertexColor;
    }
}
