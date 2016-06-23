using System.Diagnostics;
using System.Drawing;

namespace QuickFont
{
    /// <summary>
    /// A <see cref="QFontGlyph"/> that holds the glyph data
    /// </summary>
    [DebuggerDisplay("{Character} Pg:{Page}")]
    public class QFontGlyph
    {
        /// <summary>
        /// Which texture page the glyph is on
        /// </summary>
        public int Page; 
        
        /// <summary>
        /// The rectangle defining the glyphs position on the page
        /// </summary>
        public Rectangle Rect;
        
        /// <summary>
        /// How far the glyph would need to be vertically offset to be vertically in line with the tallest glyph in the set of all glyphs
        /// </summary>
        public int YOffset;

        /// <summary>
        /// Which character this glyph represents
        /// </summary>
        public char Character;

        /// <summary>
        /// Create a new <see cref="QFontGlyph"/> object
        /// </summary>
        /// <param name="page">The texture page this glyph is on</param>
        /// <param name="rect">The glyph rectangle</param>
        /// <param name="yOffset">The glyph y offset</param>
        /// <param name="character">The glyph character</param>
        public QFontGlyph(int page, Rectangle rect, int yOffset, char character)
        {
            Page = page;
            Rect = rect;
            YOffset = yOffset;
            Character = character;
        }
    }
}
