using System;
using System.Collections.Generic;
using System.Text;
using OpenTK.Graphics.OpenGL;

namespace QuickFont
{
    class Helper
    {
        public static T[] ToArray<T>(ICollection<T> collection)
        {
            T[] output = new T[collection.Count];
            collection.CopyTo(output, 0);
            return output;
        }

        /// <summary>
        /// Ensures GL.End() is called
        /// </summary>
        /// <param name="mode"></param>
        /// <param name="code"></param>
        public static void SafeGLBegin(BeginMode mode, Action code)
        {
            GL.Begin(mode);

            code();

            GL.End();
        }

        /// <summary>
        /// Ensures that state is disabled
        /// </summary>
        /// <param name="cap"></param>
        /// <param name="code"></param>
        public static void SafeGLEnable(EnableCap cap, Action code)
        {
            GL.Enable(cap);

            code();

            GL.Disable(cap);
        }

    }
}
