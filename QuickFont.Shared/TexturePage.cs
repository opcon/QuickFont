using System;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

#if OPENGL_ES
using OpenTK.Graphics.ES20;
using PixelFormat = OpenTK.Graphics.ES20.PixelFormat;
#else
using OpenTK.Graphics.OpenGL4;
#endif

namespace QuickFont
{
    /// <summary>
    /// Represents a texture page
    /// </summary>
    class TexturePage : IDisposable
    {
        private int _textureID;

        /// <summary>
        /// The texture ID of this texture page
        /// </summary>
        public int TextureID
        {
            get { return _textureID; }
        }

        /// <summary>
        /// The width of this texture page
        /// </summary>
        public int Width { get; private set; }

        /// <summary>
        /// The height of this textur page
        /// </summary>
        public int Height { get; private set; }

        /// <summary>
        /// Creates a new instance of <see cref="TexturePage"/>
        /// </summary>
        /// <param name="filePath">The filepath to load as a bitmap</param>
        public TexturePage(string filePath)
        {
            var bitmap = new QBitmap(filePath);
            CreateTexture(bitmap.BitmapData);
            bitmap.Free();
        }

        /// <summary>
        /// Creates a new instance of <see cref="TexturePage"/>
        /// </summary>
        /// <param name="dataSource">The bitmap to use as a data source</param>
        public TexturePage(BitmapData dataSource)
        {
            CreateTexture(dataSource);
        }

        /// <summary>
        /// Creates an OpenGL texture
        /// </summary>
        /// <param name="dataSource">The data source to use for the texture</param>
        private void CreateTexture(BitmapData dataSource)
        {
            Width = dataSource.Width;
            Height = dataSource.Height;

            Helper.SafeGLEnable(EnableCap.Texture2D, () =>
            {
                GL.Hint(HintTarget.PerspectiveCorrectionHint, HintMode.Nicest);

                GL.GenTextures(1, out _textureID);
                GL.BindTexture(TextureTarget.Texture2D, TextureID);

                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToBorder);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToBorder);

                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);

#if OPENGL_ES
                var rawData = ConvertBgraToRgba(dataSource);
                GL.TexImage2D(TextureTarget2d.Texture2D, 0, TextureComponentCount.Rgba, Width, Height, 0,
                    PixelFormat.Rgba, PixelType.UnsignedByte, rawData);

                GL.GenerateMipmap(TextureTarget.Texture2D);
#else
                GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, Width, Height, 0,
                    OpenTK.Graphics.OpenGL4.PixelFormat.Bgra, PixelType.UnsignedByte, dataSource.Scan0);

                GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);
#endif
            });
        }

        /// <summary>
        /// Converts a BGRA bitmap to RGBA
        /// </summary>
        /// <param name="dataSource">The bitmap to convert</param>
        /// <returns>The converted bitmap bytes</returns>
        private static byte[] ConvertBgraToRgba(BitmapData dataSource)
        {
            var length = dataSource.Stride*dataSource.Height;

            var rawData = new byte[length];

            // Copy bitmap to byte[]
            Marshal.Copy(dataSource.Scan0, rawData, 0, length);

            for (var i = 0; i < rawData.Length; i = i + 4)
            {
                var temp1 = rawData[i];
                rawData[i] = rawData[i + 2];
                rawData[i + 2] = temp1;
            }
            return rawData;
        }

        /// <summary>
        /// Dispose resources owned by this instance
        /// </summary>
        public void Dispose()
        {
            GL.DeleteTexture(TextureID);
        }
    }
}
