using OpenTK;
using OpenTK.Graphics;
#if OPENGL_ES
using OpenTK.Graphics.ES20;
#else
using OpenTK.Graphics.OpenGL4;
#endif

namespace QuickFont
{
    /// <summary>
    /// Structure representing a viewport
    /// </summary>
    public struct Viewport
    {
        /// <summary>
        /// The x coordinate of the viewport
        /// </summary>
        public float X;

        /// <summary>
        /// The y coordinate of the viewport
        /// </summary>
        public float Y;

        /// <summary>
        /// The width of the viewport
        /// </summary>
        public float Width;

        /// <summary>
        /// The height of the viewport
        /// </summary>
        public float Height;

        /// <summary>
        /// Creates a new <see cref="Viewport"/>
        /// </summary>
        /// <param name="x">The x coordinate of the viewport</param>
        /// <param name="y">The y coordinate of the viewport</param>
        /// <param name="width">The width of the viewport</param>
        /// <param name="height">The height of the viewport</param>
        public Viewport(float x, float y, float width, float height) { X = x; Y = y; Width = width; Height = height; }
    }

    /// <summary>
    /// Helper methods for dealing with the viewport
    /// </summary>
    public static class ViewportHelper
    {
        /// <summary>
        /// The current viewport
        /// </summary>
        public static Viewport? CurrentViewport {
            get {

                if (_currentViewport == null)
                {
                    UpdateCurrentViewport();
                }

                return _currentViewport; 
            }
        }
        private static Viewport? _currentViewport;

        /// <summary>
        /// Update the current viewport
        /// </summary>
        public static void UpdateCurrentViewport()
        {
            GraphicsContext.Assert();
            int[] viewportInts = new int[4];

            GL.GetInteger(GetPName.Viewport, viewportInts);
            _currentViewport = new Viewport(viewportInts[0], viewportInts[1], viewportInts[2], viewportInts[3]);
        }


        /// <summary>
        /// Invalidate the stored viewpoint.
        /// Will be refreshed the next time it is requested
        /// </summary>
        public static void InvalidateViewport()
        {
            _currentViewport = null;
        }

        /// <summary>
        /// Returns true if the projection matrix is orthographic
        /// </summary>
        /// <param name="mat">The projection matrix to test</param>
        /// <returns>True if the projection matrix is orthographic</returns>
        public static bool IsOrthographicProjection(ref Matrix4 mat)
        {
            return !(mat.M12 != 0 || mat.M13 != 0 || mat.M14 != 0 || mat.M21 != 0 || mat.M23 != 0 || mat.M24 != 0 || mat.M31 != 0 || mat.M32 != 0 || mat.M34 != 0 || mat.M44 != 1f);
        }
    }
}
