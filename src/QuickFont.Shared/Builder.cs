using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.IO;
using QuickFont.Configuration;

namespace QuickFont
{
    /// <summary>
    /// Class for building a Quick Font, given a Font
    /// and a configuration object.
    /// </summary>
    internal class Builder
    {
        private string _charSet;
        private QFontBuilderConfiguration _config;
        private IFont _font;

        public Builder(IFont font, QFontBuilderConfiguration config)
        {
            _charSet = config.CharSet;
            _config = config;
            _font = font;
            
        }

        private static Dictionary<char, QFontGlyph> CreateCharGlyphMapping(QFontGlyph[] glyphs)
        {
            var dict = new Dictionary<char, QFontGlyph>();
            for (int i = 0; i < glyphs.Length; i++)
                dict.Add(glyphs[i].Character, glyphs[i]);

            return dict;
        }

        //these do not affect the actual width of glyphs (we measure widths pixel-perfectly ourselves), but is used to detect whether a font is monospaced
        private List<SizeF> GetGlyphSizes(IFont font)
        {
            // We add padding to the returned sizes measured by MeasureString, because on some platforms (*cough*Mono*cough) this method
            // can return unreliable information. Without padding, this leads to some glyphs not fitting on the generated
            // texture page when we precisely measure their bounds
            // Hopefully a padding of 5 is enough, however may need to increase this?
            // For now we scale it with font size
            int padding = 5 + (int)(0.1 * font.Size);

            Bitmap bmp = new Bitmap(512, 512, PixelFormat.Format24bppRgb);
            Graphics graph = Graphics.FromImage(bmp);
            List<SizeF> sizes = new List<SizeF>();

            foreach (char t in _charSet)
            {
	            var charSize = font.MeasureString("" + t, graph);
	            sizes.Add(new SizeF(charSize.Width+padding, charSize.Height+padding));
            }

            graph.Dispose();
            bmp.Dispose();

            return sizes;
        }

		/// <summary>
		/// Returns the maximum width and maximum height from the list of sizes
		/// </summary>
		/// <param name="sizes"></param>
		/// <returns></returns>
        private SizeF GetMaxGlyphSize(List<SizeF> sizes)
        {
            SizeF maxSize = new SizeF(0f, 0f);
            for (int i = 0; i < _charSet.Length; i++)
            {
                if (sizes[i].Width > maxSize.Width)
                    maxSize.Width = sizes[i].Width;

                if (sizes[i].Height > maxSize.Height)
                    maxSize.Height = sizes[i].Height;
            }

            return maxSize;
        }

		/// <summary>
		/// Returns the minimum width and minimum height from the list of sizes
		/// </summary>
		/// <param name="sizes"></param>
		/// <returns></returns>
        private SizeF GetMinGlyphSize(List<SizeF> sizes)
        {
            SizeF minSize = new SizeF(float.MaxValue, float.MaxValue);
            for (int i = 0; i < _charSet.Length; i++)
            {
                if (sizes[i].Width < minSize.Width)
                    minSize.Width = sizes[i].Width;

                if (sizes[i].Height < minSize.Height)
                    minSize.Height = sizes[i].Height;
            }

            return minSize;
        }

        /// <summary>
        /// Returns true if all glyph widths are within 5% of each other
        /// </summary>
        /// <param name="sizes"></param>
        /// <returns></returns>
        private bool IsMonospaced(List<SizeF> sizes)
        {
            var min = GetMinGlyphSize(sizes);
            var max = GetMaxGlyphSize(sizes);

            if (max.Width - min.Width < max.Width * 0.05f)
                return true;

            return false;
        }

		/// <summary>
		/// Creates the initial font bitmap. This is simply a long thin strip of all glyphs in a row
		/// </summary>
		/// <param name="font">The <see cref="IFont"/> object to build the initial bitmap from</param>
		/// <param name="maxSize">The maximum glyph size of the font</param>
		/// <param name="initialMargin">The initial bitmap margin (used for all four sides)</param>
		/// <param name="glyphs">A collection of <see cref="QFontGlyph"/>s corresponding to the initial bitmap</param>
		/// <param name="renderHint">The font rendering hint to use</param>
		/// <returns></returns>
        private Bitmap CreateInitialBitmap(IFont font, SizeF maxSize, int initialMargin, out QFontGlyph[] glyphs, TextGenerationRenderHint renderHint)
        {
            glyphs = new QFontGlyph[_charSet.Length];

            int spacing = (int)Math.Ceiling(maxSize.Width) + 2 * initialMargin;
            Bitmap bmp = new Bitmap(spacing * _charSet.Length, (int)Math.Ceiling(maxSize.Height) + 2 * initialMargin, PixelFormat.Format24bppRgb);
            Graphics graph = Graphics.FromImage(bmp);

            switch(renderHint){
                case TextGenerationRenderHint.SizeDependent: 
                    graph.TextRenderingHint = font.Size <= 12.0f  ? TextRenderingHint.ClearTypeGridFit : TextRenderingHint.AntiAlias; 
                    break;
                case TextGenerationRenderHint.AntiAlias: 
                    graph.TextRenderingHint = TextRenderingHint.AntiAlias; 
                    break;
                case TextGenerationRenderHint.AntiAliasGridFit: 
                    graph.TextRenderingHint = TextRenderingHint.AntiAliasGridFit; 
                    break;
                case TextGenerationRenderHint.ClearTypeGridFit:
                    graph.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
                    break;
                case TextGenerationRenderHint.SystemDefault:
                    graph.TextRenderingHint = TextRenderingHint.SystemDefault;
                    break;
            }

			// enable high quality graphics
			graph.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
			graph.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
			graph.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceOver;

            int xOffset = initialMargin;
            for (int i = 0; i < _charSet.Length; i++)
            {
				var offset = font.DrawString("" + _charSet[i], graph, Brushes.White, xOffset, initialMargin);
                var charSize = font.MeasureString("" + _charSet[i], graph);
                glyphs[i] = new QFontGlyph(0, new Rectangle(xOffset - initialMargin + offset.X, initialMargin + offset.Y, (int)charSize.Width + initialMargin * 2, (int)charSize.Height + initialMargin * 2), 0, _charSet[i]);
                xOffset += (int)charSize.Width + initialMargin * 2;
            }

            graph.Flush();
            graph.Dispose();

            return bmp;
        }

        private delegate bool EmptyDel(BitmapData data, int x, int y);

		/// <summary>
		/// Retargets a given glyph rectangle inwards, to find the minimum bounding box
		/// Assumes the current bounding box is larger than the minimum
		/// </summary>
		/// <param name="bitmapData">The bitmap containing the glyph</param>
		/// <param name="glyph">The <see cref="QFontGlyph"/> to retarget</param>
		/// <param name="setYOffset">Whether to update the y offset of the glyph or not</param>
		/// <param name="alphaTolerance">The alpha tolerance</param>
        private static void RetargetGlyphRectangleInwards(BitmapData bitmapData, QFontGlyph glyph, bool setYOffset, byte alphaTolerance)
        {
            int startX, endX;
            int startY, endY;

            var rect = glyph.Rect;

            EmptyDel emptyPix;

            if (bitmapData.PixelFormat == PixelFormat.Format32bppArgb)
                emptyPix = delegate(BitmapData data, int x, int y) { return QBitmap.EmptyAlphaPixel(data, x, y, alphaTolerance); };
            else
                emptyPix = delegate(BitmapData data, int x, int y) { return QBitmap.EmptyPixel(data, x, y); };

			for (startX = rect.X; startX < bitmapData.Width; startX++)
				for (int j = rect.Y; j <= rect.Y + rect.Height; j++)
					if (!emptyPix(bitmapData, startX, j))
						goto Done1;
			Done1:

			for (endX = rect.X + rect.Width; endX >= 0; endX--)
				for (int j = rect.Y; j <= rect.Y + rect.Height; j++)
					if (!emptyPix(bitmapData, endX, j))
						goto Done2;
			Done2:

			for (startY = rect.Y; startY < bitmapData.Height; startY++)
				for (int i = startX; i <= endX; i++)
					if (!emptyPix(bitmapData, i, startY))
						goto Done3;
                            
			Done3:

			for (endY = rect.Y + rect.Height; endY >= 0; endY--)
				for (int i = startX; i <= endX; i++)
					if (!emptyPix(bitmapData, i, endY))
						goto Done4;
			Done4:

			if (endY < startY)
                startY = endY = rect.Y;

            if (endX < startX)
                startX = endX = rect.X;

            glyph.Rect = new Rectangle(startX, startY, endX - startX + 1, endY - startY + 1);

            if (setYOffset)
                glyph.YOffset = glyph.Rect.Y;
        }

		/// <summary>
		/// Retargets a given glyph rectangle outwards, to find the minimum bounding box
		/// Assumes the current bounding box is smaller than the minimum
		/// </summary>
		/// <param name="bitmapData">The bitmap containing the glyph</param>
		/// <param name="glyph">The <see cref="QFontGlyph"/> to retarget</param>
		/// <param name="setYOffset">Whether to update the y offset of the glyph or not</param>
		/// <param name="alphaTolerance">The alpha tolerance</param>
		private static void RetargetGlyphRectangleOutwards(BitmapData bitmapData, QFontGlyph glyph, bool setYOffset, byte alphaTolerance)
        {
            int startX,endX;
            int startY,endY;

            var rect = glyph.Rect;

            EmptyDel emptyPix;

            if (bitmapData.PixelFormat == PixelFormat.Format32bppArgb)
                emptyPix = delegate(BitmapData data, int x, int y) { return QBitmap.EmptyAlphaPixel(data, x, y, alphaTolerance); };
            else
                emptyPix = delegate(BitmapData data, int x, int y) { return QBitmap.EmptyPixel(data, x, y); };

			for (startX = rect.X; startX >= 0; startX--)
			{
				bool foundPix = false;
				for (int j = rect.Y; j <= rect.Y + rect.Height; j++)
				{
					if (!emptyPix(bitmapData, startX, j))
					{
						foundPix = true;
						break;
					}
				}

				if (!foundPix)
				{
					startX++;
					break;
				}
			}

			for (endX = rect.X + rect.Width; endX < bitmapData.Width; endX++)
			{
				bool foundPix = false;
				for (int j = rect.Y; j <= rect.Y + rect.Height; j++)
				{
					if (!emptyPix(bitmapData, endX, j))
					{
						foundPix = true;
						break; 
					}
				}

				if (!foundPix)
				{
					endX--;
					break;
				}
			}

			for (startY = rect.Y; startY >= 0; startY--)
			{
				bool foundPix = false;
				for (int i = startX; i <= endX; i++)
				{
					if (!emptyPix(bitmapData, i, startY))
					{
						foundPix = true;
						break;
					}
				}

				if (!foundPix)
				{
					startY++;
					break;
				}
			}

			for (endY = rect.Y + rect.Height; endY < bitmapData.Height; endY++)
			{
				bool foundPix = false;
				for (int i = startX; i <= endX; i++)
				{
					if (!emptyPix(bitmapData, i, endY))
					{
						foundPix = true;
						break;
					}
				}

				if (!foundPix)
				{
					endY--;
					break;
				}
			}

			glyph.Rect = new Rectangle(startX, startY, endX - startX + 1, endY - startY + 1);

            if (setYOffset)
                glyph.YOffset = glyph.Rect.Y;
        }

		/// <summary>
		/// Generates the final bitmap sheet for the font
		/// </summary>
		/// <param name="sourceGlyphs">A collection of <see cref="QFontGlyph"/>s. These are written to the final bitmap</param>
		/// <param name="sourceBitmaps"> The source bitmaps for the font (initial bitmap)</param>
		/// <param name="destSheetWidth">The destination bitmap width</param>
		/// <param name="destSheetHeight">The destination bitmap height</param>
		/// <param name="destGlyphs">A collection of <see cref="QFontGlyph"/>s that are placed on the final bitmap sheet</param>
		/// <param name="destMargin">The margin for the final bitmap sheet</param>
		/// <returns>A collection of <see cref="QBitmap"/>s. These are the final bitmap sheets</returns>
        private static List<QBitmap> GenerateBitmapSheetsAndRepack(QFontGlyph[] sourceGlyphs, BitmapData[] sourceBitmaps, int destSheetWidth, int destSheetHeight, out QFontGlyph[] destGlyphs, int destMargin)
        {
            var pages = new List<QBitmap>();
            destGlyphs = new QFontGlyph[sourceGlyphs.Length];

            QBitmap currentPage = null;

            int maxY = 0;
            foreach (var glph in sourceGlyphs)
                maxY = Math.Max(glph.Rect.Height, maxY);

            int finalPageIndex = 0;
            int finalPageRequiredWidth = 0;
            int finalPageRequiredHeight = 0;
            
			// We loop through the whole process twice. The first time is to determine
			// the size of the final page, so that we can crop it in advance
            for (int k = 0; k < 2; k++)
            {
				// Whether this is the pre-processing step
                bool pre = k == 0; 

                int xPos = 0;
                int yPos = 0;
                int maxYInRow = 0;
                int totalTries = 0;

				// Loop through all the glyphs
                for (int i = 0; i < sourceGlyphs.Length; i++)
                {
					// If this is the second stage and we don't already have a bitmap page, create one
                    if(!pre && currentPage == null){

                        if (finalPageIndex == pages.Count)
                        {
                            int width = Math.Min(destSheetWidth, finalPageRequiredWidth);
                            int height = Math.Min(destSheetHeight, finalPageRequiredHeight);

                            currentPage = new QBitmap(new Bitmap(width, height, PixelFormat.Format32bppArgb));
                            currentPage.Clear32(255, 255, 255, 0); //clear to white, but totally transparent
                        }
                        else
                        {
                            currentPage = new QBitmap(new Bitmap(destSheetWidth, destSheetHeight, PixelFormat.Format32bppArgb));
                            currentPage.Clear32(255, 255, 255, 0); //clear to white, but totally transparent
                        }
                        pages.Add(currentPage);
                    }

					// Keep track of the number of times we've tried to fit the font onto the texture page
                    totalTries++;

                    if (totalTries > 10 * sourceGlyphs.Length)
                        throw new Exception("Failed to fit font into texture pages");

                    var rect = sourceGlyphs[i].Rect;

					// If we can fit the glyph onto the page, place it
                    if (xPos + rect.Width + 2 * destMargin <= destSheetWidth && yPos + rect.Height + 2 * destMargin <= destSheetHeight)
                    {
                        if (!pre)
                        {
                            // Add to page
                            if(sourceBitmaps[sourceGlyphs[i].Page].PixelFormat == PixelFormat.Format32bppArgb)
                                QBitmap.Blit(sourceBitmaps[sourceGlyphs[i].Page], currentPage.BitmapData, rect.X, rect.Y, rect.Width, rect.Height, xPos + destMargin, yPos + destMargin);
                            else
                                QBitmap.BlitMask(sourceBitmaps[sourceGlyphs[i].Page], currentPage.BitmapData, rect.X, rect.Y, rect.Width, rect.Height, xPos + destMargin, yPos + destMargin);

							// Add to destination glyph collection
                            destGlyphs[i] = new QFontGlyph(pages.Count - 1, new Rectangle(xPos + destMargin, yPos + destMargin, rect.Width, rect.Height), sourceGlyphs[i].YOffset, sourceGlyphs[i].Character);
                        }
                        else
                        {
							// Update the final dimensions
                            finalPageRequiredWidth = Math.Max(finalPageRequiredWidth, xPos + rect.Width + 2 * destMargin);
                            finalPageRequiredHeight = Math.Max(finalPageRequiredHeight, yPos + rect.Height + 2 * destMargin);
                        }

						// Update the current x position
                        xPos += rect.Width + 2 * destMargin;

						// Update the maximum row height so far
                        maxYInRow = Math.Max(maxYInRow, rect.Height);

                        continue;
                    }

					// If we reach this, haven't been able to fit glyph onto row
					// Move down one row and try again
                    if (xPos + rect.Width + 2 * destMargin > destSheetWidth)
                    {
						// Retry the current glyph on the next row
                        i--;

						// Change coordinates to next row
                        yPos += maxYInRow + 2 * destMargin;
                        xPos = 0;

						// Is the next row off the bitmap sheet?
                        if (yPos + maxY + 2 * destMargin > destSheetHeight)
                        {
							// Reset y position
                            yPos = 0;

                            if (!pre)
                            {
								// If this is not the second stage, reset the currentPage
								// This will create a new one on next loop
                                currentPage = null;
                            }
                            else
                            {
								// If this is the pre-processing stage, update
								// the finalPageIndex. Reset width and height
								// since we clearly need one full page and extra
                                finalPageRequiredWidth = 0;
                                finalPageRequiredHeight = 0;
                                finalPageIndex++;
                            }
                        }
                    }
                }

            }

            return pages;
        }

		/// <summary>
		/// Builds the font data
		/// </summary>
		/// <param name="saveName">The filename to save the font texture files too. If null, the texture files are not saved</param>
		/// <returns>A <see cref="QFontData"/></returns>
        public QFontData BuildFontData(string saveName = null)
        {
			// Check super sample level range
            if (_config.SuperSampleLevels <= 0 || _config.SuperSampleLevels > 8)
            {
                throw new ArgumentOutOfRangeException("SuperSampleLevels = [" + _config.SuperSampleLevels + "] is an unsupported value. Please use values in the range [1,8]"); 
            }

            int margin = 3; //margin in initial bitmap (don't bother to make configurable - likely to cause confusion
            int glyphMargin = _config.GlyphMargin * _config.SuperSampleLevels;

            QFontGlyph[] initialGlyphs;
            var sizes = GetGlyphSizes(_font);
            var maxSize = GetMaxGlyphSize(sizes);
            var initialBmp = CreateInitialBitmap(_font, maxSize, margin, out initialGlyphs,_config.TextGenerationRenderHint);

#if DEBUG 
			// print bitmap with bounds to debug it
			var debugBmp = initialBmp.Clone() as Bitmap;
			var graphics = Graphics.FromImage(debugBmp);
			var pen = new Pen(Color.Red, 1);

			foreach (var g in initialGlyphs)
			{
				graphics.DrawRectangle(pen, g.Rect);
			}

			graphics.Flush();
			graphics.Dispose();

			debugBmp.Save(_font + "-DEBUG.png", ImageFormat.Png);
#endif

            var initialBitmapData = initialBmp.LockBits(new Rectangle(0, 0, initialBmp.Width, initialBmp.Height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);

            int minYOffset = int.MaxValue;

			// Retarget each glyph rectangle to get minimum bounding box
            foreach (var glyph in initialGlyphs)
            {
                RetargetGlyphRectangleInwards(initialBitmapData, glyph, true, _config.KerningConfig.AlphaEmptyPixelTolerance);
                minYOffset = Math.Min(minYOffset,glyph.YOffset);
            }
            minYOffset--; //give one pixel of breathing room?

			// Update glyph y offsets
            foreach (var glyph in initialGlyphs)
                glyph.YOffset -= minYOffset;

			// Find the optimal page size for the font
            Size pagesize = GetOptimalPageSize(initialBmp.Width * _config.SuperSampleLevels, initialBmp.Height * _config.SuperSampleLevels, _config.PageMaxTextureSize);
            QFontGlyph[] glyphs;

			// Generate the final bitmap pages
            List<QBitmap> bitmapPages = GenerateBitmapSheetsAndRepack(initialGlyphs, new[] { initialBitmapData }, pagesize.Width, pagesize.Height, out glyphs, glyphMargin);

			// Clean up
            initialBmp.UnlockBits(initialBitmapData);
            initialBmp.Dispose();

			// Scale and retarget glyphs if needed
            if (_config.SuperSampleLevels != 1)
            {
                ScaleSheetsAndGlyphs(bitmapPages, glyphs, 1.0f / _config.SuperSampleLevels);
                RetargetAllGlyphs(bitmapPages, glyphs,_config.KerningConfig.AlphaEmptyPixelTolerance);
            }

            //create list of texture pages
            var pages = new List<TexturePage>();
            foreach (var page in bitmapPages)
                pages.Add(new TexturePage(page.BitmapData));


			// Build the QFontData
			var fontData = new QFontData
			{
				CharSetMapping = CreateCharGlyphMapping(glyphs),
				Pages = pages.ToArray()
			};
			fontData.CalculateMeanWidth();
            fontData.CalculateMaxHeight();
            fontData.KerningPairs = KerningCalculator.CalculateKerning(_charSet.ToCharArray(), glyphs, bitmapPages,_config.KerningConfig, _font);
            fontData.NaturallyMonospaced = IsMonospaced(sizes);

			// Save the font texture files if required
            if (saveName != null)
            {
                if (bitmapPages.Count == 1)
                {
                    bitmapPages[0].Bitmap.UnlockBits(bitmapPages[0].BitmapData);
                    bitmapPages[0].Bitmap.Save(saveName + ".png", ImageFormat.Png);
                    bitmapPages[0] = new QBitmap(bitmapPages[0].Bitmap);
                }
                else
                {
                    for (int i = 0; i < bitmapPages.Count; i++)
                    {
                        bitmapPages[i].Bitmap.UnlockBits(bitmapPages[i].BitmapData);
                        bitmapPages[i].Bitmap.Save(saveName + "_sheet_" + i + ".png", ImageFormat.Png);
                        bitmapPages[i] = new QBitmap(bitmapPages[i].Bitmap);
                    }
                }
            }

			// Build the font drop shadow if required
            if (_config.ShadowConfig != null)
                fontData.DropShadowFont = BuildDropShadow(bitmapPages, glyphs, _config.ShadowConfig, _charSet.ToCharArray(),_config.KerningConfig.AlphaEmptyPixelTolerance);

			// Clean up resources
            foreach (var page in bitmapPages)
                page.Free();

			// Check that no glyphs are overlapping
            var intercept = FirstIntercept(fontData.CharSetMapping);
            if (intercept != null)
                throw new Exception("Failed to create glyph set. Glyphs '" + intercept[0] + "' and '" + intercept[1] + "' were overlapping. This is could be due to an error in the font, or a bug in Graphics.MeasureString().");
            
            return fontData;
        }

		/// <summary>
		/// Returns the optimal page size
		/// </summary>
		/// <param name="width">The desired page width</param>
		/// <param name="height">The desired page height</param>
		/// <param name="pageMaxTextureSize">The max page texture size</param>
		/// <returns>The optimal page size</returns>
        private static Size GetOptimalPageSize(int width, int height, int pageMaxTextureSize)
        {
            int rows = (width/(pageMaxTextureSize))+1;
            return new Size(pageMaxTextureSize, rows*height);
        }

		/// <summary>
		/// Builds the drop shadow
		/// </summary>
		/// <param name="sourceFontSheets">The bitmap sheets from the source font</param>
		/// <param name="sourceFontGlyphs">The glyphs from the source font</param>
		/// <param name="shadowConfig">The shadow configuration</param>
		/// <param name="charSet">The char set to include in the shadow</param>
		/// <param name="alphaTolerance">The alpha tolerance</param>
		/// <returns>The shadow QFont</returns>
        private static QFont BuildDropShadow(List<QBitmap> sourceFontSheets, QFontGlyph[] sourceFontGlyphs, QFontShadowConfiguration shadowConfig, char[] charSet, byte alphaTolerance)
        {
            QFontGlyph[] newGlyphs;

            var sourceBitmapData = new List<BitmapData>();
            foreach(var sourceSheet in sourceFontSheets)
                sourceBitmapData.Add(sourceSheet.BitmapData);
            
            var bitmapSheets = GenerateBitmapSheetsAndRepack(sourceFontGlyphs, sourceBitmapData.ToArray(), shadowConfig.PageMaxTextureSize, shadowConfig.PageMaxTextureSize, out newGlyphs, shadowConfig.GlyphMargin + shadowConfig.BlurRadius*3);

            //scale up in case we wanted bigger/smaller shadows
            if (Math.Abs(shadowConfig.Scale - 1.0f) > float.Epsilon)
                ScaleSheetsAndGlyphs(bitmapSheets, newGlyphs, shadowConfig.Scale); //no point in retargeting yet, since we will do it after blur

            //whiten and blur
            foreach (var bitmapSheet in bitmapSheets)
            {
                bitmapSheet.Colour32(255, 255, 255);
                if (shadowConfig.Type == ShadowType.Blurred)
                    bitmapSheet.BlurAlpha(shadowConfig.BlurRadius, shadowConfig.BlurPasses);
                else
                    bitmapSheet.ExpandAlpha(shadowConfig.BlurRadius, shadowConfig.BlurPasses);
            }

            //retarget after blur and scale
            RetargetAllGlyphs(bitmapSheets, newGlyphs, alphaTolerance);

            //create list of texture pages
            var newTextureSheets = new List<TexturePage>();
            foreach (var page in bitmapSheets)
                newTextureSheets.Add(new TexturePage(page.BitmapData));

			var fontData = new QFontData {CharSetMapping = new Dictionary<char, QFontGlyph>()};

			for(int i = 0; i < charSet.Length; i++)
                fontData.CharSetMapping.Add(charSet[i],newGlyphs[i]);

            fontData.Pages = newTextureSheets.ToArray();
            fontData.CalculateMeanWidth();
            fontData.CalculateMaxHeight();

            foreach (var sheet in bitmapSheets)
                sheet.Free();

            fontData.IsDropShadow = true;
            return new QFont(fontData);
        }

		/// <summary>
		/// Scales the sheets and glyphs of a font by the specified amount
		/// </summary>
		/// <param name="pages">The pages to scale</param>
		/// <param name="glyphs">The glyphs to scale</param>
		/// <param name="scale">The amount to scale by</param>
        private static void ScaleSheetsAndGlyphs(List<QBitmap> pages, QFontGlyph[] glyphs, float scale)
        {
            foreach (var page in pages)
                page.DownScale32((int)(page.Bitmap.Width * scale), (int)(page.Bitmap.Height * scale));

            foreach (var glyph in glyphs)
            {
                glyph.Rect = new Rectangle((int)(glyph.Rect.X * scale), (int)(glyph.Rect.Y * scale), (int)(glyph.Rect.Width * scale), (int)(glyph.Rect.Height * scale));
                glyph.YOffset = (int)(glyph.YOffset * scale);
            }
        }

		/// <summary>
		/// Updates the glyph targeting - required after they have been scaled
		/// </summary>
		/// <param name="pages">The pages containing the glyphs</param>
		/// <param name="glyphs">The glyphs to retarget</param>
		/// <param name="alphaTolerance">The alpha tolerance</param>
        private static void RetargetAllGlyphs(List<QBitmap> pages, QFontGlyph[] glyphs, byte alphaTolerance)
        {
            foreach (var glyph in glyphs)
                RetargetGlyphRectangleOutwards(pages[glyph.Page].BitmapData, glyph, false, alphaTolerance);
        }

		/// <summary>
		/// Saves the <see cref="QFontData"/> to the specified file
		/// This is used for loading custom texture fonts
		/// </summary>
		/// <param name="data">The <see cref="QFontData"/> to save</param>
		/// <param name="filePath">The filepath</param>
        public static void SaveQFontDataToFile(QFontData data, string filePath)
        {
			// Serialize the font data
            var lines = data.Serialize();

			// Write it to the file
            StreamWriter writer = new StreamWriter(filePath + ".qfont");
            foreach (var line in lines)
                writer.WriteLine(line);
            
            writer.Close();
        }

		/// <summary>
		/// Creates an individual bitmap for each glyph
		/// </summary>
		/// <param name="sourceGlyphs">The source glyphs</param>
		/// <param name="sourceBitmaps">The source bitmaps</param>
		/// <param name="destGlyphs">The destination glyphs</param>
		/// <param name="destBitmaps">The destination bitmaps</param>
        public static void CreateBitmapPerGlyph(QFontGlyph[] sourceGlyphs, QBitmap[] sourceBitmaps, out QFontGlyph[]  destGlyphs, out QBitmap[] destBitmaps){
            destBitmaps = new QBitmap[sourceGlyphs.Length];
            destGlyphs = new QFontGlyph[sourceGlyphs.Length];
            for(int i = 0; i < sourceGlyphs.Length; i++){
                var sg = sourceGlyphs[i];
                destGlyphs[i] = new QFontGlyph(i,new Rectangle(0,0,sg.Rect.Width,sg.Rect.Height),sg.YOffset,sg.Character);
                destBitmaps[i] = new QBitmap(new Bitmap(sg.Rect.Width,sg.Rect.Height,PixelFormat.Format32bppArgb));
                QBitmap.Blit(sourceBitmaps[sg.Page].BitmapData,destBitmaps[i].BitmapData,sg.Rect,0,0);
            }
        }

		/// <summary>
		/// Loads a <see cref="QFontData"/> from the font data file
		/// </summary>
		/// <param name="filePath">The font data file to load from</param>
		/// <param name="downSampleFactor">Whether to downsample the font</param>
		/// <param name="loaderConfig">The font loader configuration</param>
		/// <returns>The loaded <see cref="QFontData"/></returns>
        public static QFontData LoadQFontDataFromFile(string filePath, float downSampleFactor, QFontConfiguration loaderConfig)
        {
            var lines = new List<string>();
            StreamReader reader = new StreamReader(filePath);
            string line;
            while((line = reader.ReadLine()) != null)
                lines.Add(line);
            reader.Close();

            var data = new QFontData();
            int pageCount;
            char[] charSet;
            data.Deserialize(lines, out pageCount, out charSet);

            string namePrefix = filePath.Replace(".qfont","").Replace(" ", "");
            
            var bitmapPages = new List<QBitmap>();

            if (pageCount == 1)
            {
                bitmapPages.Add(new QBitmap(namePrefix + ".png"));
            }
            else
            {
                for (int i = 0; i < pageCount; i++)
                    bitmapPages.Add(new QBitmap(namePrefix + "_sheet_" + i));
            }

            foreach (var glyph in data.CharSetMapping.Values)
                RetargetGlyphRectangleOutwards(bitmapPages[glyph.Page].BitmapData, glyph, false, loaderConfig.KerningConfig.AlphaEmptyPixelTolerance);
 
            var intercept = FirstIntercept(data.CharSetMapping);
            if (intercept != null)
            {
                throw new Exception("Failed to load font from file. Glyphs '" + intercept[0] + "' and '" + intercept[1] + "' were overlapping. If you are texturing your font without locking pixel opacity, then consider using a larger glyph margin. This can be done by setting QFontBuilderConfiguration myQfontBuilderConfig.GlyphMargin, and passing it into CreateTextureFontFiles.");
            }

            if (downSampleFactor > 1.0f)
            {
                foreach (var page in bitmapPages)
                    page.DownScale32((int)(page.Bitmap.Width * downSampleFactor), (int)(page.Bitmap.Height * downSampleFactor));

                foreach (var glyph in data.CharSetMapping.Values)
                {

                    glyph.Rect = new Rectangle((int)(glyph.Rect.X * downSampleFactor),
                                                (int)(glyph.Rect.Y * downSampleFactor),
                                                (int)(glyph.Rect.Width * downSampleFactor),
                                                (int)(glyph.Rect.Height * downSampleFactor));
                    glyph.YOffset = (int)(glyph.YOffset * downSampleFactor);
                }
            }
            else if (downSampleFactor < 1.0f )
            {
                // If we were simply to shrink the entire texture, then at some point we will make glyphs overlap, breaking the font.
                // For this reason it is necessary to copy every glyph to a separate bitmap, and then shrink each bitmap individually.
                QFontGlyph[] shrunkGlyphs;
                QBitmap[] shrunkBitmapsPerGlyph;
                CreateBitmapPerGlyph(Helper.ToArray(data.CharSetMapping.Values), bitmapPages.ToArray(), out shrunkGlyphs, out shrunkBitmapsPerGlyph);
                    
                //shrink each bitmap
                for (int i = 0; i < shrunkGlyphs.Length; i++)
                {   
                    var bmp = shrunkBitmapsPerGlyph[i];
                    bmp.DownScale32(Math.Max((int)(bmp.Bitmap.Width * downSampleFactor),1), Math.Max((int)(bmp.Bitmap.Height * downSampleFactor),1));
                    shrunkGlyphs[i].Rect = new Rectangle(0, 0, bmp.Bitmap.Width, bmp.Bitmap.Height);
                    shrunkGlyphs[i].YOffset = (int)(shrunkGlyphs[i].YOffset * downSampleFactor);
                }

                var shrunkBitmapData = new BitmapData[shrunkBitmapsPerGlyph.Length];
                for(int i = 0; i < shrunkBitmapsPerGlyph.Length; i ++ ){
                    shrunkBitmapData[i] = shrunkBitmapsPerGlyph[i].BitmapData;
                }

                //use roughly the same number of pages as before..
                int newWidth = (int)(bitmapPages[0].Bitmap.Width * (0.1f + downSampleFactor));
                int newHeight = (int)(bitmapPages[0].Bitmap.Height * (0.1f + downSampleFactor));

                //free old bitmap pages since we are about to chuck them away
                for (int i = 0; i < pageCount; i++)
                    bitmapPages[i].Free();

                QFontGlyph[] shrunkRepackedGlyphs;
                bitmapPages = GenerateBitmapSheetsAndRepack(shrunkGlyphs, shrunkBitmapData, newWidth, newHeight, out shrunkRepackedGlyphs, 4);
                data.CharSetMapping = CreateCharGlyphMapping(shrunkRepackedGlyphs);

                foreach (var bmp in shrunkBitmapsPerGlyph)
                    bmp.Free();

                pageCount = bitmapPages.Count;
            }

            data.Pages = new TexturePage[pageCount];
            for(int i = 0; i < pageCount; i ++ )
                data.Pages[i] = new TexturePage(bitmapPages[i].BitmapData);

            if (Math.Abs(downSampleFactor - 1.0f) > float.Epsilon)
            {
                foreach (var glyph in data.CharSetMapping.Values)
                    RetargetGlyphRectangleOutwards(bitmapPages[glyph.Page].BitmapData, glyph, false, loaderConfig.KerningConfig.AlphaEmptyPixelTolerance);

                intercept = FirstIntercept(data.CharSetMapping);
                if (intercept != null)
                {
                    throw new Exception("Failed to load font from file. Glyphs '" + intercept[0] + "' and '" + intercept[1] + "' were overlapping. This occurred only after resizing your texture font, implying that there is a bug in QFont. ");
                }
            }

            var glyphList = new List<QFontGlyph>();

            foreach (var c in charSet)
                glyphList.Add(data.CharSetMapping[c]);

            if (loaderConfig.ShadowConfig != null)
                data.DropShadowFont = BuildDropShadow(bitmapPages, glyphList.ToArray(), loaderConfig.ShadowConfig, Helper.ToArray(charSet),loaderConfig.KerningConfig.AlphaEmptyPixelTolerance);

            data.KerningPairs = KerningCalculator.CalculateKerning(Helper.ToArray(charSet), glyphList.ToArray(), bitmapPages, loaderConfig.KerningConfig);
            
            data.CalculateMeanWidth();
            data.CalculateMaxHeight();

            for (int i = 0; i < pageCount; i++)
                bitmapPages[i].Free();

            return data;
        }

		/// <summary>
		/// Find the first intercept between two glyph bounding boxes
		/// </summary>
		/// <param name="charSet">The character set to test</param>
		/// <returns>The overlapping characters</returns>
        private static char[] FirstIntercept(Dictionary<char,QFontGlyph> charSet)
        {
            char[] keys = Helper.ToArray(charSet.Keys);

            for (int i = 0; i < keys.Length; i++)
            {
                for (int j = i + 1; j < keys.Length; j++)
                {
                    if (charSet[keys[i]].Page == charSet[keys[j]].Page && RectangleIntersect(charSet[keys[i]].Rect, charSet[keys[j]].Rect))
                    {
                        return new[] { keys[i], keys[j] };
                    }
                }
            }
            return null;
        }

		/// <summary>
		/// Returns true if two rectangles intersect
		/// </summary>
		/// <param name="r1">The first rectangle</param>
		/// <param name="r2">The second rectangle</param>
		/// <returns>True if the rectangles intersect</returns>
        private static bool RectangleIntersect(Rectangle r1, Rectangle r2)
        {
            return (r1.X < r2.X + r2.Width && r1.X + r1.Width > r2.X &&
                    r1.Y < r2.Y + r2.Height && r1.Y + r1.Height > r2.Y);
        }

        /// <summary>
        /// Returns the power of 2 that is closest to x, but not smaller than x.
        /// </summary>
        public static int PowerOfTwo(int x)
        {
            int shifts = 0;
            uint val = (uint)x;

            if (x < 0)
                return 0;

            while (val > 0)
            {
                val = val >> 1;
                shifts++;
            }

            val = (uint)1 << (shifts - 1);
            if (val < x)
            {
                val = val << 1;
            }

            return (int)val;
        }
    }
}
