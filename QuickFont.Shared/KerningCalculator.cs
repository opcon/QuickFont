using System;
using System.Collections.Generic;
using QuickFont.Configuration;

namespace QuickFont
{
    /// <summary>
    /// Static methods for calculating kerning
    /// </summary>
    static class KerningCalculator
    {
        private struct XLimits
        {
            public int Min;
            public int Max;
        }

        /// <summary>
        /// Calculate the kerning between two glyphs
        /// </summary>
        /// <param name="g1">The first glyph</param>
        /// <param name="g2">The second glyph</param>
        /// <param name="lim1">The first glyph limits</param>
        /// <param name="lim2">The second glyph limits</param>
        /// <param name="config">The kerning configuration to use</param>
        /// <param name="font">The glyph's <see cref="IFont"/></param>
        /// <returns>The x coordinate kerning offset</returns>
        private static int Kerning(QFontGlyph g1, QFontGlyph g2, XLimits[] lim1, XLimits[] lim2, QFontKerningConfiguration config, IFont font)
        {
			// Use kerning information from the font if it exists
			if (font != null && font.HasKerningInformation) return font.GetKerning(g1.Character, g2.Character);

            // Otherwise, calculate our own kerning
            int yOffset1 = g1.YOffset;
            int yOffset2 = g2.YOffset;

            int startY = Math.Max(yOffset1, yOffset2);
            int endY = Math.Min(g1.Rect.Height + yOffset1, g2.Rect.Height + yOffset2);

            int w1 = g1.Rect.Width;

            int worstCase = w1;

            //TODO - offset startY, endY by yOffset1 so that lim1[j-yOffset1] can be written as lim1[j], will need another var for yOffset2

            for (int j = startY; j < endY; j++)
                worstCase = Math.Min(worstCase, w1 - lim1[j-yOffset1].Max + lim2[j-yOffset2].Min);

            worstCase = Math.Min(worstCase, g1.Rect.Width);
            worstCase = Math.Min(worstCase, g2.Rect.Width);

            //modify by character kerning rules
            CharacterKerningRule kerningRule = config.GetOverridingCharacterKerningRuleForPair(""+g1.Character + g2.Character);

            switch (kerningRule)
            {
                case CharacterKerningRule.Zero:
                    return 1;
                case CharacterKerningRule.NotMoreThanHalf:
                    return 1 - (int)Math.Min(Math.Min(g1.Rect.Width,g2.Rect.Width)*0.5f, worstCase);
            }

            return 1 - worstCase;
        }

        /// <summary>
        /// Calculates the kerning values for the given character set
        /// </summary>
        /// <param name="charSet">The character set to calculate kerning values for</param>
        /// <param name="glyphs">The glyphs used for kerning</param>
        /// <param name="bitmapPages">The bitmap pages of the glyphs</param>
        /// <param name="config">The kerning configuration</param>
        /// <param name="font">The <see cref="IFont"/> used to create the glyphs and bitmaps</param>
        /// <returns>A <see cref="Dictionary{TKey,TValue}"/> mapping of every glyph pair to a kerning amount</returns>
        public static Dictionary<string, int> CalculateKerning(char[] charSet, QFontGlyph[] glyphs, List<QBitmap> bitmapPages, QFontKerningConfiguration config, IFont font = null)
        {
            var kerningPairs = new Dictionary<string, int>();

            //we start by computing the index of the first and last non-empty pixel in each row of each glyph
            XLimits[][] limits = new XLimits[charSet.Length][];
            int maxHeight = 0;
            for (int n = 0; n < charSet.Length; n++)
            {
                var rect = glyphs[n].Rect;
                var page = bitmapPages[glyphs[n].Page];

                limits[n] = new XLimits[rect.Height+1];

                maxHeight = Math.Max(rect.Height, maxHeight);

                int yStart = rect.Y;
                int yEnd = rect.Y + rect.Height;
                int xStart = rect.X;
                int xEnd = rect.X + rect.Width;

                for (int j = yStart; j <= yEnd; j++)
                {
                    int last = xStart;

                    bool yetToFindFirst = true;
                    for (int i = xStart; i <= xEnd; i++)
                    {
                        if (!QBitmap.EmptyAlphaPixel(page.BitmapData, i, j,config.AlphaEmptyPixelTolerance))
                        {

                            if (yetToFindFirst)
                            {
                                limits[n][j - yStart].Min = i - xStart;
                                yetToFindFirst = false;
                            }
                            last = i;
                        }
                    }

                    limits[n][j - yStart].Max = last - xStart;

                    if (yetToFindFirst)
                        limits[n][j - yStart].Min = xEnd - 1;
                }
            }

            //we now bring up each row to the max (or min) of it's two adjacent rows, this is to stop glyphs sliding together too closely
            var tmp = new XLimits[maxHeight+1];

            for (int n = 0; n < charSet.Length; n++)
            {
                //clear tmp 
                for (int j = 0; j < limits[n].Length; j++)
                    tmp[j] = limits[n][j];

                for (int j = 0; j < limits[n].Length; j++)
                {
                    if(j != 0){
                        tmp[j].Min = Math.Min(limits[n][j - 1].Min, tmp[j].Min);
                        tmp[j].Max = Math.Max(limits[n][j - 1].Max, tmp[j].Max);
                    }

                    if (j != limits[n].Length - 1)
                    {
                        tmp[j].Min = Math.Min(limits[n][j + 1].Min, tmp[j].Min);
                        tmp[j].Max = Math.Max(limits[n][j + 1].Max, tmp[j].Max);
                    }
                    
                }

                for (int j = 0; j < limits[n].Length; j++)
                    limits[n][j] = tmp[j];
            }

            // For each character in the character set, 
            // combine it with every other character and add it to the kerning pair dictionary
            for (int i = 0; i < charSet.Length; i++)
                for (int j = 0; j < charSet.Length; j++)
                    kerningPairs.Add("" + charSet[i] + charSet[j], Kerning(glyphs[i], glyphs[j], limits[i], limits[j],config, font));

            return kerningPairs;
        }
    }
}
