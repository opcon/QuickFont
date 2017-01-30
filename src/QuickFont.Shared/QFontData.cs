using System;
using System.Collections.Generic;
using System.Drawing;

namespace QuickFont
{
    /// <summary>
    /// This class holds all the necessary font data
    /// </summary>
    class QFontData
    {
        /// <summary>
        /// Mapping from a pair of characters to a pixel offset
        /// </summary>
        public Dictionary<string, int> KerningPairs;

        /// <summary>
        /// List of texture pages
        /// </summary>
        public TexturePage[] Pages;

        /// <summary>
        /// Mapping from character to glyph index
        /// </summary>
        public Dictionary<char, QFontGlyph> CharSetMapping; 

        /// <summary>
        /// The average glyph width
        /// </summary>
        public float MeanGlyphWidth;

        /// <summary>
        /// The maximum glyph height
        /// </summary>
        public int MaxGlyphHeight;

        /// <summary>
        /// The maximum line height
        /// </summary>
        public int MaxLineHeight;

        /// <summary>
        /// Null if no dropShadowFont is available
        /// </summary>
        public QFont DropShadowFont;

        /// <summary>
        /// true if this font is dropShadowFont
        /// </summary>
        public bool IsDropShadow;

        /// <summary>
        /// Whether the original font (from ttf) was detected to be monospaced
        /// </summary>
        public bool NaturallyMonospaced = false;

        /// <summary>
        /// Whether this font is being rendered as monospaced
        /// </summary>
        /// <param name="options">The render options</param>
        /// <returns>True if the font is rendered as monospaced</returns>
        public bool IsMonospacingActive(QFontRenderOptions options)
        {
            return (options.Monospacing == QFontMonospacing.Natural && NaturallyMonospaced) || options.Monospacing == QFontMonospacing.Yes; 
        }

        /// <summary>
        /// Returns the monospace width
        /// </summary>
        /// <param name="options">The font rendering options</param>
        /// <returns>The monospace width</returns>
        public float GetMonoSpaceWidth(QFontRenderOptions options)
        {
            return (float)Math.Ceiling(1 + (1 + options.CharacterSpacing) * MeanGlyphWidth);
        }

        /// <summary>
        /// Serialize this <see cref="QFontData"/> to a collection of <see cref="string"/>s
        /// </summary>
        /// <returns>The serialized <see cref="QFontData"/></returns>
        public List<string> Serialize()
        {
            var data = new List<string>();

            data.Add("" + Pages.Length);
            data.Add("" + CharSetMapping.Count);

            foreach (var glyphChar in CharSetMapping)
            {
                var chr = glyphChar.Key;
                var glyph = glyphChar.Value;

                data.Add("" + chr + " " + 
                    glyph.Page + " " +
                    glyph.Rect.X + " " +
                    glyph.Rect.Y + " " +
                    glyph.Rect.Width + " " +
                    glyph.Rect.Height + " " +
                    glyph.YOffset);
            }
            return data;
        }

        /// <summary>
        /// Deserialize a <see cref="QFontData"/> object, given the serialized data
        /// </summary>
        /// <param name="input">The serialized data</param>
        /// <param name="pageCount">The number of texture pages</param>
        /// <param name="charSet">The character set supported by this <see cref="QFontData"/></param>
        public void Deserialize(List<string> input, out int pageCount, out char[] charSet)
        {
            CharSetMapping = new Dictionary<char, QFontGlyph>();
            var charSetList = new List<char>();
            try
            {
                pageCount = int.Parse(input[0]);
                int glyphCount = int.Parse(input[1]);

                for (int i = 0; i < glyphCount; i++)
                {
                    var vals = input[2 + i].Split(' ');
                    var glyph = new QFontGlyph(int.Parse(vals[1]), new Rectangle(int.Parse(vals[2]), int.Parse(vals[3]), int.Parse(vals[4]), int.Parse(vals[5])), int.Parse(vals[6]), vals[0][0]);

                    CharSetMapping.Add(vals[0][0], glyph);
                    charSetList.Add(vals[0][0]);
                }
            }
            catch (Exception e)
            {
                throw new Exception("Failed to parse qfont file. Invalid format.",e);
            }

            charSet = charSetList.ToArray();
        }

        /// <summary>
        /// Calculate the mean width of all glyphs in this <see cref="QFontData"/>
        /// </summary>
        public void CalculateMeanWidth()
        {
            MeanGlyphWidth = 0f;
            foreach (var glyph in CharSetMapping)
                MeanGlyphWidth += glyph.Value.Rect.Width;

            MeanGlyphWidth /= CharSetMapping.Count;
        }

        /// <summary>
        /// Calculate the mean height of all glyphs in this <see cref="QFontData"/>
        /// </summary>
        public void CalculateMaxHeight()
        {
            MaxGlyphHeight = 0;
            MaxLineHeight = 0;
            foreach (var glyph in CharSetMapping)
            {
                MaxGlyphHeight = Math.Max(glyph.Value.Rect.Height, MaxGlyphHeight);
                MaxLineHeight = Math.Max(glyph.Value.Rect.Height + glyph.Value.YOffset, MaxLineHeight);
            }
        }

        /// <summary>
        /// Returns the kerning length correction for the character at the given index in the given string.
        /// Also, if the text is part of a textNode list, the nextNode is given so that the following 
        /// node can be checked incase of two adjacent word nodes.
        /// </summary>
        /// <param name="index">The character index into the string</param>
        /// <param name="text">The string of text containing the character to kern</param>
        /// <param name="textNode">The next text node</param>
        /// <returns>The kerning correction for the character pair</returns>
        public int GetKerningPairCorrection(int index, string text, TextNode textNode)
        {
            if (KerningPairs == null)
                return 0;

            var chars = new char[2];

            if (index + 1 == text.Length)
            {
                if (textNode != null && textNode.Next != null && textNode.Next.Type == TextNodeType.Word)
                    chars[1] = textNode.Next.Text[0];
                else
                    return 0;
            }
            else
            {
                chars[1] = text[index + 1];
            }

            chars[0] = text[index];

            var str = new string(chars);

            if (KerningPairs.ContainsKey(str))
                return KerningPairs[str];

            return 0;
        }

        /// <summary>
        /// Frees all resources used by this <see cref="QFontData"/> object
        /// </summary>
        public void Dispose()
        {
            // release all textures
            foreach (var page in Pages)
                page.Dispose();

            // and also all sub-fonts if there
            DropShadowFont?.Dispose();
        }
    }
}
