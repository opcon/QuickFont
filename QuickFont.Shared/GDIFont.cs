using System;
using System.Drawing;
using System.Drawing.Text;
using System.Linq;

namespace QuickFont
{
	/// <summary>
	/// An implementation of <see cref="IFont"/> that uses the GDI system to 
	/// load fonts (aka System.Drawing). This method does not work for loading
	/// custom fonts on Mono, and hence FreeType is required for those
	/// </summary>
	public sealed class GDIFont : IFont
	{
		private Font _font;
		private FontFamily _fontFamily;

		/// <summary>
		/// The size of the font
		/// </summary>
		public float Size { get { return _font.Size; } }

		/// <summary>
		/// Whether the font has kerning information available, or if it needs
		/// to be calculated
		/// </summary>
		public bool HasKerningInformation { get { return false; } }

		/// <summary>
		///     Creates a GDI+ Font from the specified font file
		/// </summary>
		/// <param name="fontPath">The path to the font file</param>
		/// <param name="size">The size of the font</param>
		/// <param name="style">The font style</param>
		/// <param name="superSampleLevels">The super sample level of the font</param>
		/// <param name="scale">The scale of the font</param>
		public GDIFont(string fontPath, float size, FontStyle style, int superSampleLevels = 1, float scale = 1.0f)
		{
			try
			{
				var pfc = new PrivateFontCollection();
				pfc.AddFontFile(fontPath);
				_fontFamily = pfc.Families[0];
			}
			catch (System.IO.FileNotFoundException)
			{
				//font file could not be found, check if it exists in the installed font collection
				var installed = new InstalledFontCollection();
				_fontFamily = installed.Families.FirstOrDefault(family => string.Equals(fontPath, family.Name));
				//if we can't find the font file at all, use the system default font
				if (_fontFamily == null) _fontFamily = SystemFonts.DefaultFont.FontFamily;
			}
			if (!_fontFamily.IsStyleAvailable(style))
				throw new ArgumentException("Font file: " + fontPath + " does not support style: " + style);

			_font = new Font(_fontFamily, size * scale * superSampleLevels, style);
		}

		/// <summary>
		/// Draws the given string at the specified location
		/// </summary>
		/// <param name="s">The string to draw</param>
		/// <param name="graph">The graphics surface to draw the string on to</param>
		/// <param name="color">The color of the text</param>
		/// <param name="x">The x position of the string</param>
		/// <param name="y">The y position of the string</param>
		/// <returns>Returns the offset of the glyph from the given x and y. Only non-zero with <see cref="FreeTypeFont"/></returns>
		public Point DrawString(string s, Graphics graph, Brush color, int x, int y)
		{
			graph.DrawString(s, _font, color, x, y);

			return Point.Empty;
		}

		/// <summary>
		/// Gets the kerning between the given characters, if the font supports it
		/// </summary>
		/// <param name="c1">The first character of the character pair</param>
		/// <param name="c2">The second character of the character pair</param>
		/// <returns>The horizontal kerning offset of the character pair</returns>
		public int GetKerning(char c1, char c2)
		{
			throw new NotImplementedException("Font kerning for GDI Fonts is not implemented. Should be calculated manually.");
		}

		/// <summary>
		/// Measures the given string and returns the size
		/// </summary>
		/// <param name="s">The string to measure</param>
		/// <param name="graph">The graphics surface to use for temporary purposes</param>
		/// <returns>The size of the given string</returns>
		public SizeF MeasureString(string s, Graphics graph)
		{
			return graph.MeasureString(s, _font);
		}

		/// <summary>Returns a string that represents the current object.</summary>
		/// <returns>A string that represents the current object.</returns>
		/// <filterpriority>2</filterpriority>
		public override string ToString()
		{
		    return _font.Name;
		}

		/// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.</summary>
		/// <filterpriority>2</filterpriority>
		public void Dispose()
		{
			_font.Dispose();
			_fontFamily.Dispose();
		}
	}
}
