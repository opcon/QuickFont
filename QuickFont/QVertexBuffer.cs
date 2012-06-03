using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK;
using System.Runtime.InteropServices;
using OpenTK.Graphics.OpenGL;
using System.Drawing;


namespace QuickFont
{
    public class QVertexBuffer : IDisposable
    {
        public int VertexCount = 0;

        int VboID;
        QVertex[] Vertices = new QVertex[1000];
        int TextureID;


        public QVertexBuffer(int textureID)
        {
            TextureID = textureID;

            GL.GenBuffers(1, out VboID);
        }

        public void Dispose()
        {
            GL.DeleteBuffers(1, ref VboID);
        }

        public void Reset()
        {
            VertexCount = 0;
        }

        public void AddVertex(Vector3 point, Vector3 normal, Vector2 textureCoord, int color)
        {
            if (VertexCount + 1 >= Vertices.Length)
            {
                var newArray = new QVertex[Vertices.Length * 2];
                Array.Copy(Vertices, newArray, VertexCount);
                Vertices = newArray;
            }

            Vertices[VertexCount].Set(point, normal, textureCoord, color);

            VertexCount++;
        }

        public void Load()
        {
            if (VertexCount == 0)
                return;

            GL.BindBuffer(BufferTarget.ArrayBuffer, VboID);
            
            GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(VertexCount * BlittableValueType.StrideOf(Vertices)), Vertices, BufferUsageHint.StaticDraw);

            int size;
            GL.GetBufferParameter(BufferTarget.ArrayBuffer, BufferParameterName.BufferSize, out size);
            
            if (VertexCount * BlittableValueType.StrideOf(Vertices) != size)
                throw new ApplicationException("Vertex data not uploaded correctly");
        }

        static ArrayCap[] DrawStates = new ArrayCap[] { ArrayCap.VertexArray, ArrayCap.NormalArray, ArrayCap.TextureCoordArray, ArrayCap.ColorArray };
        static EnableCap[] DrawCaps = new EnableCap[] { EnableCap.Texture2D, EnableCap.Blend };

        public void Draw()
        {
            if (VertexCount == 0)
                return;

            Helper.SafeGLEnable(DrawCaps, () =>
            {
                GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);

                Helper.SafeGLEnableClientStates(DrawStates, () =>
                {
                    GL.BindTexture(TextureTarget.Texture2D, TextureID);
                    GL.BindBuffer(BufferTarget.ArrayBuffer, VboID);

                    GL.VertexPointer(3, VertexPointerType.Float, BlittableValueType.StrideOf(Vertices), new IntPtr(0));
                    GL.NormalPointer(NormalPointerType.Float, BlittableValueType.StrideOf(Vertices), new IntPtr(12));
                    GL.TexCoordPointer(2, TexCoordPointerType.Float, BlittableValueType.StrideOf(Vertices), new IntPtr(24));
                    GL.ColorPointer(4, ColorPointerType.UnsignedByte, BlittableValueType.StrideOf(Vertices), new IntPtr(32)); 

                    // triangles because quads are depreciated in new opengl versions
                    GL.DrawArrays(BeginMode.Triangles, 0, VertexCount);
                });
            });
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct QVertex
    {
        public Vector3 Position;
        public Vector3 Normal;
        public Vector2 TextureCoord;
        public int VertexColor;

        internal void Set(Vector3 point, Vector3 normal, Vector2 textureCoord, int p)
        {
            Position = point;
            Normal = normal;
            TextureCoord = textureCoord;
            VertexColor = p;
        }
    }
}
