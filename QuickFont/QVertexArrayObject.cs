using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL4;

namespace QuickFont
{
    public class QVertexArrayObject : IDisposable
    {
        private const int InitialSize = 1000;
        private int _bufferSize;
        private int _bufferMaxVertexCount;
        public int VertexCount;

        private int VAOID;
        private int VBOID;
        private int _textureID;

        public readonly SharedState QFontSharedState;

        private List<QVertex> Vertices;
        private QVertex[] VertexArray;

        public QVertexArrayObject(SharedState state, int textureID)
        {
            QFontSharedState = state;
            _textureID = textureID;

            Vertices = new List<QVertex>(InitialSize);
            _bufferMaxVertexCount = InitialSize;
            _bufferSize = _bufferMaxVertexCount * BlittableValueType.StrideOf(default(QVertex));

            VAOID = GL.GenVertexArray();

            GL.UseProgram(QFontSharedState.ShaderVariables.ShaderProgram);
            GL.BindVertexArray(VAOID);

            GL.GenBuffers(1, out VBOID);
            GL.BindBuffer(BufferTarget.ArrayBuffer, VBOID);

            int stride = BlittableValueType.StrideOf(default(QVertex));
            GL.EnableVertexAttribArray(QFontSharedState.ShaderVariables.PositionCoordAttribLocation);
            GL.EnableVertexAttribArray(QFontSharedState.ShaderVariables.TextureCoordAttribLocation);
            GL.EnableVertexAttribArray(QFontSharedState.ShaderVariables.ColorCoordAttribLocation);
            GL.VertexAttribPointer(QFontSharedState.ShaderVariables.PositionCoordAttribLocation, 3, VertexAttribPointerType.Float, false, stride, IntPtr.Zero);
            GL.VertexAttribPointer(QFontSharedState.ShaderVariables.TextureCoordAttribLocation, 2, VertexAttribPointerType.Float, false,
                stride, new IntPtr(3 * sizeof(float)));
            GL.VertexAttribPointer(QFontSharedState.ShaderVariables.ColorCoordAttribLocation, 4, VertexAttribPointerType.Float, false, stride, new IntPtr(5 * sizeof(float)));

            GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)_bufferSize, IntPtr.Zero, BufferUsageHint.StreamDraw );

            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            GL.BindVertexArray(0);
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
            GL.BindVertexArray(VAOID);

            if (VertexCount > _bufferMaxVertexCount)
            {
                while (VertexCount > _bufferMaxVertexCount)
                {
                    _bufferMaxVertexCount += InitialSize;
                    _bufferSize = _bufferMaxVertexCount*BlittableValueType.StrideOf(default(QVertex));
                }
                GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(VertexCount * BlittableValueType.StrideOf(default(QVertex))), VertexArray,
                    BufferUsageHint.StreamDraw);
            }
            else
            {
                GL.BindBuffer(BufferTarget.ArrayBuffer, VBOID);
                GL.BufferSubData(BufferTarget.ArrayBuffer, IntPtr.Zero, (IntPtr) (VertexCount * BlittableValueType.StrideOf(default(QVertex))), VertexArray);
            }
        }

        public void Reset()
        {
            Vertices.Clear();
            VertexCount = 0;
        }

        public void Draw()
        {
            GL.BindVertexArray(VAOID);
            var dpt = PrimitiveType.Triangles;
            GL.Enable(EnableCap.Texture2D);
            GL.ActiveTexture(QFontSharedState.DefaultTextureUnit);
            GL.BindTexture(TextureTarget.Texture2D, _textureID);
            GL.BindBuffer(BufferTarget.ArrayBuffer, VBOID);

            GL.DrawArrays(dpt, 0, VertexCount);

            GL.BindVertexArray(0);
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
        }

        public void Dispose()
        {
            GL.DeleteBuffers(1, ref VBOID);
            GL.DeleteVertexArrays(1, ref VAOID);
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
