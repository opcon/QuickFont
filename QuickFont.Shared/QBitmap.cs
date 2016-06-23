using System;
using System.Drawing;
using System.Drawing.Imaging;

namespace QuickFont
{
    /// <summary>
    /// The <see cref="QBitmap"/> class. Used for font bitmaps
    /// </summary>
    public class QBitmap
    {
        /// <summary>
        /// The <see cref="QBitmap"/>'s Bitmap
        /// </summary>
        public Bitmap Bitmap;

        /// <summary>
        /// The <see cref="QBitmap"/>'s BitmapData
        /// </summary>
        public BitmapData BitmapData;

        /// <summary>
        /// Creates a new instance of the <see cref="QBitmap"/> class from the 
        /// given file path
        /// </summary>
        /// <param name="filePath">The file to load the bitmap from</param>
        public QBitmap(string filePath)
        {
            LockBits(new Bitmap(filePath));
        }

        /// <summary>
        /// Creates a new instance of the <see cref="QBitmap"/> class from an 
        /// existing bitmap
        /// </summary>
        /// <param name="bitmap">The existing bitmap to use</param>
        public QBitmap(Bitmap bitmap)
        {
            LockBits(bitmap);
        }

        /// <summary>
        /// Lock the bitmap bits
        /// </summary>
        /// <param name="bitmap">The bitmap to lock</param>
        private void LockBits(Bitmap bitmap)
        {
            // Save a reference to the bitmap
            Bitmap = bitmap;

            // Lock the bitmap bits
            BitmapData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadWrite, bitmap.PixelFormat);
        }

        /// <summary>
        /// Clear the bitmap to the specified RGBA values
        /// </summary>
        /// <param name="r">The red value</param>
        /// <param name="g">The green value</param>
        /// <param name="b">The blue value</param>
        /// <param name="a">The alpha value</param>
        public void Clear32(byte r, byte g, byte b, byte a)
        {
            unsafe
            {
                byte* sourcePtr = (byte*) (BitmapData.Scan0);

                for (int i = 0; i < BitmapData.Height; i++)
                {
                    for (int j = 0; j < BitmapData.Width; j++)
                    {
                        *(sourcePtr) = b;
                        *(sourcePtr + 1) = g;
                        *(sourcePtr + 2) = r;
                        *(sourcePtr + 3) = a;

                        sourcePtr += 4;
                    }
                    sourcePtr += BitmapData.Stride - BitmapData.Width*4; //move to the end of the line (past unused space)
                }
            }
        }

        /// <summary>
        /// Returns true if the given pixel is empty (i.e. black)
        /// </summary>
        /// <param name="bitmapData">The bitmap data</param>
        /// <param name="px">The pixel x coordinate</param>
        /// <param name="py">The pixel y coordinate</param>
        public static unsafe bool EmptyPixel(BitmapData bitmapData, int px, int py)
        {
            if (px < 0 || py < 0 || px >= bitmapData.Width || py >= bitmapData.Height) return true;
            byte* addr = (byte*) (bitmapData.Scan0) + bitmapData.Stride*py + px*3;
            return (*addr == 0 && *(addr + 1) == 0 && *(addr + 2) == 0);

        }

        /// <summary>
        /// Returns true if the given pixel is empty (i.e. alpha is zero)
        /// </summary>
        /// <param name="bitmapData">The bitmap data</param>
        /// <param name="px">The pixel x coordinate</param>
        /// <param name="py">The pixel y coordinate</param>
        /// <param name="alphaEmptyPixelTolerance">The pixel alpha tolerance below which to consider the pixel empty</param>
        public static unsafe bool EmptyAlphaPixel(BitmapData bitmapData, int px, int py, byte alphaEmptyPixelTolerance)
        {
            if (px < 0 || py < 0 || px >= bitmapData.Width || py >= bitmapData.Height) return true;
            byte* addr = (byte*) (bitmapData.Scan0) + bitmapData.Stride*py + px*4;
            return (*(addr + 3) <= alphaEmptyPixelTolerance);

        }

        /// <summary>
        /// Blits a block of a bitmap data from source to destination, using the luminance of the source to determine the 
        /// alpha of the target. Source must be 24-bit, target must be 32-bit.
        /// </summary>
        /// <param name="source">The source bitmap data</param>
        /// <param name="target">The target bitmap data</param>
        /// <param name="srcPx">The source rectangle x coordinate</param>
        /// <param name="srcPy">The source rectangle y coordinate</param>
        /// <param name="srcW">The source rectangle width</param>
        /// <param name="srcH">The source rectangle height</param>
        /// <param name="px">The destination rectangle x coordinate</param>
        /// <param name="py">The destination rectangle y coordinate</param>
        public static void BlitMask(BitmapData source, BitmapData target, int srcPx, int srcPy, int srcW, int srcH, int px, int py)
        {
            int sourceBpp = 3;
            int targetBpp = 4;

            var targetStartX = Math.Max(px, 0);
            var targetEndX = Math.Min(px + srcW, target.Width);

            var targetStartY = Math.Max(py, 0);
            var targetEndY = Math.Min(py + srcH, target.Height);

            var copyW = targetEndX - targetStartX;
            var copyH = targetEndY - targetStartY;

            if (copyW < 0)
            {
                return;
            }

            if (copyH < 0)
            {
                return;
            }

            int sourceStartX = srcPx + targetStartX - px;
            int sourceStartY = srcPy + targetStartY - py;


            unsafe
            {
                byte* sourcePtr = (byte*) (source.Scan0);
                byte* targetPtr = (byte*) (target.Scan0);


                byte* targetY = targetPtr + targetStartY*target.Stride;
                byte* sourceY = sourcePtr + sourceStartY*source.Stride;
                for (int y = 0; y < copyH; y++, targetY += target.Stride, sourceY += source.Stride)
                {

                    byte* targetOffset = targetY + targetStartX*targetBpp;
                    byte* sourceOffset = sourceY + sourceStartX*sourceBpp;
                    for (int x = 0; x < copyW; x++, targetOffset += targetBpp, sourceOffset += sourceBpp)
                    {
                        int lume = *(sourceOffset) + *(sourceOffset + 1) + *(sourceOffset + 2);

                        lume /= 3;

                        if (lume > 255)
                            lume = 255;

                        *(targetOffset + 3) = (byte) lume;

                    }

                }
            }
        }

        /// <summary>
        /// Blits from source to target. Both source and target must be 32-bit
        /// </summary>
        /// <param name="source">The source bitmap data</param>
        /// <param name="target">The destination bitmap data</param>
        /// <param name="sourceRect">The source rectangle</param>
        /// <param name="px">The destination rectangle x coordinate</param>
        /// <param name="py">The desination rectangle y coordinate</param>
        public static void Blit(BitmapData source, BitmapData target, Rectangle sourceRect, int px, int py)
        {
            Blit(source, target, sourceRect.X, sourceRect.Y, sourceRect.Width, sourceRect.Height, px, py);
        }

        /// <summary>
        /// Blits from source to target. Both source and target must be 32-bit
        /// </summary>
        /// <param name="source">The source bitmap data</param>
        /// <param name="target">The target bitmap data</param>
        /// <param name="srcPx">The source rectangle x coordinate</param>
        /// <param name="srcPy">The source rectangle y coordinate</param>
        /// <param name="srcW">The source rectangle width</param>
        /// <param name="srcH">The source rectangle height</param>
        /// <param name="destX">The destination rectangle x coordinate</param>
        /// <param name="destY">The destination rectangle y coordinate</param>
        public static void Blit(BitmapData source, BitmapData target, int srcPx, int srcPy, int srcW, int srcH, int destX, int destY)
        {
            int bpp = 4;

            var targetStartX = Math.Max(destX, 0);
            var targetEndX = Math.Min(destX + srcW, target.Width);

            var targetStartY = Math.Max(destY, 0);
            var targetEndY = Math.Min(destY + srcH, target.Height);

            var copyW = targetEndX - targetStartX;
            var copyH = targetEndY - targetStartY;

            if (copyW < 0)
            {
                return;
            }

            if (copyH < 0)
            {
                return;
            }

            int sourceStartX = srcPx + targetStartX - destX;
            int sourceStartY = srcPy + targetStartY - destY;


            unsafe
            {
                byte* sourcePtr = (byte*) (source.Scan0);
                byte* targetPtr = (byte*) (target.Scan0);


                byte* targetY = targetPtr + targetStartY*target.Stride;
                byte* sourceY = sourcePtr + sourceStartY*source.Stride;
                for (int y = 0; y < copyH; y++, targetY += target.Stride, sourceY += source.Stride)
                {

                    byte* targetOffset = targetY + targetStartX*bpp;
                    byte* sourceOffset = sourceY + sourceStartX*bpp;
                    for (int x = 0; x < copyW*bpp; x++, targetOffset ++, sourceOffset ++)
                        *(targetOffset) = *(sourceOffset);

                }
            }
        }


        /// <summary>
        /// Changes the specified pixel to the given RGBA values
        /// </summary>
        /// <param name="px">The pixel x coordinate</param>
        /// <param name="py">The pixel y coordinate</param>
        /// <param name="r">The new pixel R value</param>
        /// <param name="g">The new pixel G value</param>
        /// <param name="b">The new pixel B value</param>
        /// <param name="a">The new pixel A value</param>
        public unsafe void PutPixel32(int px, int py, byte r, byte g, byte b, byte a)
        {
            byte* addr = (byte*) (BitmapData.Scan0) + BitmapData.Stride*py + px*4;

            *addr = b;
            *(addr + 1) = g;
            *(addr + 2) = r;
            *(addr + 3) = a;
        }

        /// <summary>
        /// Returns the RGBA values of the specified pixel
        /// </summary>
        /// <param name="px">The pixel x coordinate</param>
        /// <param name="py">The pixel y coordinate</param>
        /// <param name="r">The pixel's R value</param>
        /// <param name="g">The pixel's G value</param>
        /// <param name="b">The pixel's B value</param>
        /// <param name="a">The pixel's A value</param>
        public unsafe void GetPixel32(int px, int py, ref byte r, ref byte g, ref byte b, ref byte a)
        {
            byte* addr = (byte*) (BitmapData.Scan0) + BitmapData.Stride*py + px*4;

            b = *addr;
            g = *(addr + 1);
            r = *(addr + 2);
            a = *(addr + 3);
        }

        /// <summary>
        /// Change the alpha value of the specified pixel
        /// </summary>
        /// <param name="px">The pixel x coordinate</param>
        /// <param name="py">The pixel y coordinate</param>
        /// <param name="a">The new pixel A value</param>
        public unsafe void PutAlpha32(int px, int py, byte a)
        {
            *((byte*) (BitmapData.Scan0) + BitmapData.Stride*py + px*4 + 3) = a;
        }

        /// <summary>
        /// Return the alpha value of the specified pixel
        /// </summary>
        /// <param name="px">The pixel x coordinate</param>
        /// <param name="py">The pixel y coordinate</param>
        /// <param name="a">The pixel's A value</param>
        public unsafe void GetAlpha32(int px, int py, ref byte a)
        {
            a = *((byte*) (BitmapData.Scan0) + BitmapData.Stride*py + px*4 + 3);
        }

        /// <summary>
        /// Downscale the 32-bit <see cref="Bitmap"/> of this <see cref="QBitmap"/> to the specified width and height
        /// </summary>
        /// <param name="newWidth">The new width of the bitmap</param>
        /// <param name="newHeight">The new height of the bitmap</param>
        public void DownScale32(int newWidth, int newHeight)
        {
            QBitmap newBitmap = new QBitmap(new Bitmap(newWidth, newHeight, Bitmap.PixelFormat));

            if (Bitmap.PixelFormat != PixelFormat.Format32bppArgb)
                throw new Exception("DownsScale32 only works on 32 bit images");

            float xscale = (float) BitmapData.Width/newWidth;
            float yscale = (float) BitmapData.Height/newHeight;

            byte r = 0, g = 0, b = 0, a = 0;
            float summedR;
            float summedG;
            float summedB;
            float summedA;

            int left, right, top, bottom; //the area of old pixels covered by the new bitmap


            float targetStartX, targetEndX;
            float targetStartY, targetEndY;

            float leftF, rightF, topF, bottomF; //edges of new pixel in old pixel coords
            float weight;
            float weightScale = xscale*yscale;
            float totalColourWeight;

            for (int m = 0; m < newHeight; m++)
            {
                for (int n = 0; n < newWidth; n++)
                {

                    leftF = n*xscale;
                    rightF = (n + 1)*xscale;

                    topF = m*yscale;
                    bottomF = (m + 1)*yscale;

                    left = (int) leftF;
                    right = (int) rightF;

                    top = (int) topF;
                    bottom = (int) bottomF;

                    if (left < 0) left = 0;
                    if (top < 0) top = 0;
                    if (right >= BitmapData.Width) right = BitmapData.Width - 1;
                    if (bottom >= BitmapData.Height) bottom = BitmapData.Height - 1;

                    summedR = 0f;
                    summedG = 0f;
                    summedB = 0f;
                    summedA = 0f;
                    totalColourWeight = 0f;

                    for (int j = top; j <= bottom; j++)
                    {
                        for (int i = left; i <= right; i++)
                        {
                            targetStartX = Math.Max(leftF, i);
                            targetEndX = Math.Min(rightF, i + 1);

                            targetStartY = Math.Max(topF, j);
                            targetEndY = Math.Min(bottomF, j + 1);

                            weight = (targetEndX - targetStartX)*(targetEndY - targetStartY);

                            GetPixel32(i, j, ref r, ref g, ref b, ref a);

                            summedA += weight*a;

                            if (a != 0)
                            {
                                summedR += weight*r;
                                summedG += weight*g;
                                summedB += weight*b;
                                totalColourWeight += weight;
                            }

                        }
                    }

                    summedR /= totalColourWeight;
                    summedG /= totalColourWeight;
                    summedB /= totalColourWeight;
                    summedA /= weightScale;

                    if (summedR < 0) summedR = 0f;
                    if (summedG < 0) summedG = 0f;
                    if (summedB < 0) summedB = 0f;
                    if (summedA < 0) summedA = 0f;

                    if (summedR >= 256) summedR = 255;
                    if (summedG >= 256) summedG = 255;
                    if (summedB >= 256) summedB = 255;
                    if (summedA >= 256) summedA = 255;

                    newBitmap.PutPixel32(n, m, (byte) summedR, (byte) summedG, (byte) summedB, (byte) summedA);
                }

            }


            Free();

            Bitmap = newBitmap.Bitmap;
            BitmapData = newBitmap.BitmapData;
        }

        /// <summary>
        /// Sets colour of the <see cref="QBitmap"/> without touching alpha values
        /// </summary>
        /// <param name="r">The new R value</param>
        /// <param name="g">The new G value</param>
        /// <param name="b">The new B value</param>
        public void Colour32(byte r, byte g, byte b)
        {
            unsafe
            {
                byte* addr;
                for (int i = 0; i < BitmapData.Width; i++)
                {
                    for (int j = 0; j < BitmapData.Height; j++)
                    {
                        addr = (byte*) (BitmapData.Scan0) + BitmapData.Stride*j + i*4;
                        *addr = b;
                        *(addr + 1) = g;
                        *(addr + 2) = r;
                    }
                }
            }
        }

        /// <summary>
        /// Expand the alpha values of this <see cref="QBitmap"/>
        /// </summary>
        /// <param name="radius">The expansion radius</param>
        /// <param name="passes">Number of expansion passes</param>
        public void ExpandAlpha(int radius, int passes)
        {
            QBitmap tmp = new QBitmap(new Bitmap(Bitmap.Width, Bitmap.Height, Bitmap.PixelFormat));

            byte a = 0;
            byte max;
            int xpos, ypos, x, y, kx, ky;
            int width = Bitmap.Width;
            int height = Bitmap.Height;

            for (int pass = 0; pass < passes; pass++)
            {

                //horizontal pass
                for (y = 0; y < height; y++)
                {
                    for (x = 0; x < width; x++)
                    {
                        max = 0;
                        for (kx = -radius; kx <= radius; kx++)
                        {
                            xpos = x + kx;
                            if (xpos >= 0 && xpos < width)
                            {
                                GetAlpha32(xpos, y, ref a);
                                if (a > max)
                                    max = a;
                            }
                        }

                        tmp.PutAlpha32(x, y, max);
                    }
                }

                //vertical pass
                for (x = 0; x < width; ++x)
                {
                    for (y = 0; y < height; ++y)
                    {
                        max = 0;
                        for (ky = -radius; ky <= radius; ky++)
                        {
                            ypos = y + ky;
                            if (ypos >= 0 && ypos < height)
                            {
                                tmp.GetAlpha32(x, ypos, ref a);
                                if (a > max)
                                    max = a;
                            }
                        }

                        PutAlpha32(x, y, max);

                    }
                }

            }

            tmp.Free();
        }

        /// <summary>
        /// Blur the alpha values of this <see cref="QBitmap"/>
        /// </summary>
        /// <param name="radius">The blur radius</param>
        /// <param name="passes">The blur passes</param>
        public void BlurAlpha(int radius, int passes)
        {
            QBitmap tmp = new QBitmap(new Bitmap(Bitmap.Width, Bitmap.Height, Bitmap.PixelFormat));

            byte a = 0;
            int summedA;
            int weight;
            int xpos, ypos, x, y, kx, ky;
            int width = Bitmap.Width;
            int height = Bitmap.Height;

            for (int pass = 0; pass < passes; pass++)
            {
                //horizontal pass
                for (y = 0; y < height; y++)
                {
                    for (x = 0; x < width; x++)
                    {
                        summedA = weight = 0;
                        for (kx = -radius; kx <= radius; kx++)
                        {
                            xpos = x + kx;
                            if (xpos >= 0 && xpos < width)
                            {
                                GetAlpha32(xpos, y, ref a);
                                summedA += a;
                                weight++;
                            }
                        }

                        summedA /= weight;
                        tmp.PutAlpha32(x, y, (byte) summedA);
                    }
                }

                //vertical pass
                for (x = 0; x < width; ++x)
                {
                    for (y = 0; y < height; ++y)
                    {
                        summedA = weight = 0;
                        for (ky = -radius; ky <= radius; ky++)
                        {
                            ypos = y + ky;
                            if (ypos >= 0 && ypos < height)
                            {
                                tmp.GetAlpha32(x, ypos, ref a);
                                summedA += a;
                                weight++;
                            }
                        }

                        summedA /= weight;

                        PutAlpha32(x, y, (byte) summedA);

                    }
                }

            }

            tmp.Free();

        }

        /// <summary>
        /// Frees the resources belonging to this <see cref="QBitmap"/>
        /// </summary>
        public void Free()
        {
            Bitmap.UnlockBits(BitmapData);
            Bitmap.Dispose();
        }

    }
}
