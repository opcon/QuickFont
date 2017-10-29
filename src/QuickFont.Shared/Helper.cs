using System;
using System.Collections.Generic;
using System.Drawing;
using OpenTK;
#if OPENGL_ES
using OpenTK.Graphics.ES20;
#else
using OpenTK.Graphics.OpenGL4;
#endif

namespace QuickFont
{
    /// <summary>
    /// A helper class containing some useful static methods
    /// </summary>
	internal static class Helper
    {
		/// <summary>
		/// Returns an array copy of <see cref="ICollection{T}"/>
		/// </summary>
		/// <typeparam name="T">The type of the collection item</typeparam>
		/// <param name="collection">The collection to copy</param>
		/// <returns>The <see cref="ICollection{T}"/> copied to an array</returns>
        public static T[] ToArray<T>(ICollection<T> collection)
        {
            T[] output = new T[collection.Count];
            collection.CopyTo(output, 0);
            return output;
        }

        /// <summary>
        /// Ensures that state is disabled
        /// </summary>
        /// <param name="cap"></param>
        /// <param name="code"></param>
        public static void SafeGLEnable(EnableCap cap, Action code)
        {
            bool enabled = GL.IsEnabled(cap);
            GL.Enable(cap);

            code();

            if (!enabled)
                GL.Disable(cap);
        }

        /// <summary>
        /// Ensures that multiple states are disabled
        /// </summary>
        /// <param name="caps"></param>
        /// <param name="code"></param>
        public static void SafeGLEnable(EnableCap[] caps, Action code)
        {
            bool[] previouslyEnabled = new bool[caps.Length];

            for (int i = 0; i < caps.Length; i++)
            {
                if (GL.IsEnabled(caps[i]))
                    previouslyEnabled[i] = true;
                else 
                    GL.Enable(caps[i]);
            }

            code();

            for (int i = 0; i < caps.Length; i++)
            {
                if (!previouslyEnabled[i])
                    GL.Disable(caps[i]);
            }
        }

		/// <summary>
		/// Converts the given <see cref="Color"/> to RGBA
		/// <para/>
		/// Colour bytes are converted with
		/// <c>color.A &lt;&lt; 24 | color.B &lt;&lt; 16 | color.G &lt;&lt; 8 | color.R</c>
		/// </summary>
		/// <param name="color">The <see cref="Color"/> to convert</param>
		/// <returns>The 32-bit RGBA values of the colour</returns>
		public static int ToRgba(Color color)
        {
            return color.A << 24 | color.B << 16 | color.G << 8 | color.R;
        }

		/// <summary>
		/// Converts the given <see cref="Color"/> to a <see cref="Vector4"/>,
		/// such that XYZW = RGBA.
		/// <para></para>
		/// Vector values are normalized between 0.0 and 1.0
		/// </summary>
		/// <param name="color">The <see cref="Color"/> to convert</param>
		/// <returns>The <see cref="Vector4"/> of the color</returns>
        public static Vector4 ToVector4(Color color)
        {
            return new Vector4{X = (float)color.R / byte.MaxValue, Y = (float)color.G / byte.MaxValue, Z = (float)color.B / byte.MaxValue, W = (float)color.A / byte.MaxValue};
        }
    }
}
