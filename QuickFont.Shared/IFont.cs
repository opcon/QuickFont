using System;
using System.Drawing;

namespace QuickFont
{
	/// <summary>
	/// Represents a font
	/// </summary>
	public interface IFont : IDisposable
	{
		/// <summary>
		/// Measures the given string and returns the size
		/// </summary>
		/// <param name="s">The string to measure</param>
		/// <param name="graph">The graphics surface to use for temporary purposes</param>
		/// <returns>The size of the given string</returns>
		SizeF MeasureString(string s, Graphics graph);

		/// <summary>
		/// The size of the font
		/// </summary>
		float Size { get; }

		/// <summary>
		/// Whether the font has kerning information available, or if it needs
		/// to be calculated
		/// </summary>
		bool HasKerningInformation { get; }

		/// <summary>
		/// Draws the given string at the specified location
		/// </summary>
		/// <param name="s">The string to draw</param>
		/// <param name="graph">The graphics surface to draw the string on to</param>
		/// <param name="color">The color of the text</param>
		/// <param name="x">The x position of the string</param>
		/// <param name="y">The y position of the string</param>
		/// <returns>Returns the offset of the glyph from the given x and y. Only non-zero with <see cref="FreeTypeFont"/></returns>
		Point DrawString(string s, Graphics graph, Brush color, int x, int y);

		/// <summary>
		/// Gets the kerning between the given characters, if the font supports it
		/// </summary>
		/// <param name="c1">The first character of the character pair</param>
		/// <param name="c2">The second character of the character pair</param>
		/// <returns>The horizontal kerning offset of the character pair</returns>
		int GetKerning(char c1, char c2);
	}
}
