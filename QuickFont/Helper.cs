using System;
using System.Collections.Generic;
using System.Drawing;
using OpenTK;
using OpenTK.Graphics.ES20;

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
        /// <param name="cap"></param>
        /// <param name="code"></param>
        public static void SafeGLEnable(EnableCap[] caps, Action code)
        {
            bool[] m_previouslyEnabled = new bool[caps.Length];

            for (int i = 0; i < caps.Length; i++)
            {
                if (GL.IsEnabled(caps[i]))
                    m_previouslyEnabled[i] = true;
                else 
                    GL.Enable(caps[i]);
            }

            code();

            for (int i = 0; i < caps.Length; i++)
            {
                if (!m_previouslyEnabled[i])
                    GL.Disable(caps[i]);
            }
        }

        public static int ToRgba(Color color)
        {
            return color.A << 24 | color.B << 16 | color.G << 8 | color.R;
        }

        public static Vector4 ToVector4(Color color)
        {
            return new Vector4{X = (float)color.R / byte.MaxValue, Y = (float)color.G / byte.MaxValue, Z = (float)color.B / byte.MaxValue, W = (float)color.A / byte.MaxValue};
        }
    }
}
