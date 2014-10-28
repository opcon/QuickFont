using System;
using System.Collections.Generic;
using System.Text;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using OpenTK.Graphics;


namespace QuickFont
{

    public struct Viewport
    {
        public int X, Y, Width, Height;
        public Viewport(int X, int Y, int Width, int Height) { this.X = X; this.Y = Y; this.Width = Width; this.Height = Height; }
    }

    public struct TransformViewport
    {
        public float X, Y, Width, Height;
        public TransformViewport(float X, float Y, float Width, float Height) { this.X = X; this.Y = Y; this.Width = Width; this.Height = Height; }
    }

    public class ProjectionStack
    {
        public static ProjectionStack DefaultStack { get; private set; }


        //The currently set viewport
        public Viewport? CurrentViewport {
            get {

                if (currentViewport == null)
                {
                    UpdateCurrentViewport();
                }

                return currentViewport; 
            }
        }
        private Viewport? currentViewport = null;

        static ProjectionStack()
        {
            DefaultStack = new ProjectionStack();
        }

        public void UpdateCurrentViewport()
        {
            GraphicsContext.Assert();
            Viewport viewport = new Viewport();
            GL.GetInteger(GetPName.Viewport, out viewport.X);
            currentViewport = viewport;
        }


        public void InvalidateViewport()
        {
            currentViewport = null;
        }


        public void GetCurrentOrthogProjection(out bool isOrthog, out float left, out float right, out float bottom, out float top)
        {
            Matrix4 matrix = new Matrix4();
            GL.GetFloat(GetPName.ProjectionMatrix, out matrix.Row0.X);

            if (matrix.M11 == 0 || matrix.M22 == 0)
            {
                isOrthog = false;
                left = right = bottom = top = 0;
                return;
            }

            left = -(1f + matrix.M41) / (matrix.M11);
            right = (1f - matrix.M41) / (matrix.M11);

            bottom = -(1 + matrix.M42) / (matrix.M22);
            top = (1 - matrix.M42) / (matrix.M22);

            isOrthog =
                matrix.M12 == 0 && matrix.M13 == 0 && matrix.M14 == 0 &&
                matrix.M21 == 0 && matrix.M23 == 0 && matrix.M24 == 0 &&
                matrix.M31 == 0 && matrix.M32 == 0 && matrix.M34 == 0 &&
                matrix.M44 == 1f;
        }


        public void Begin()
        {

            GraphicsContext.Assert();

            Viewport currentVp = (Viewport)CurrentViewport;

            GL.MatrixMode(MatrixMode.Projection);
            GL.PushMatrix(); //push projection matrix
            GL.LoadIdentity();
            GL.Ortho(currentVp.X, currentVp.Width, currentVp.Height, currentVp.Y, -1.0, 1.0);

            GL.MatrixMode(MatrixMode.Modelview);
            GL.PushMatrix();  //push modelview matrix
            GL.LoadIdentity();

        }

        public void End()
        {
            GraphicsContext.Assert();

            GL.MatrixMode(MatrixMode.Modelview);
            GL.PopMatrix(); //pop modelview

            GL.MatrixMode(MatrixMode.Projection);
            GL.PopMatrix(); //pop projection

            GL.MatrixMode(MatrixMode.Modelview);
        }

        public Matrix4 GetOrtho()
        {
            var currentVp = (Viewport) CurrentViewport;
            //return Matrix4.CreateScale(0.4f);
            var mat = Matrix4.CreateOrthographicOffCenter((float) currentVp.X, (float) currentVp.Width,
                (float) currentVp.Y, (float) currentVp.Height, -1.0f, 1.0f);
            mat = Matrix4.Mult(Matrix4.CreateScale(1, -1.0f, 1), mat);
            //mat = Matrix4.CreateOrthographic((float)currentVp.Width, (float)currentVp.Height, -100.0f, 200.0f);
            //mat = Matrix4.Mult(Matrix4.CreateScale(200f), mat);
            return mat;
            //return Matrix4.CreateOrthographic((float)currentVp.Width, currentVp.Height, -1.0f, 1.0f);
        }






    }
}
