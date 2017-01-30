using System;
using System.Drawing;
using SharpFont;
using System.Diagnostics;
using System.IO;

namespace QuickFont
{
	/// <summary>
	/// An implementation of <see cref="IFont"/> that uses FreeType via
	/// SharpFont to load the font file. This implementation supports reading
	/// kerning information directly from the font file.
	/// </summary>
	public class FreeTypeFont : IFont
	{
		private Library _fontLibrary = new Library();
		private const uint DPI = 96;

		private Face _fontFace;

		private int _maxHorizontalBearyingY = 0;

		/// <summary>
		/// The size of the font
		/// </summary>
		public float Size { get; private set; }

		/// <summary>
		/// Whether the font has kerning information available, or if it needs
		/// to be calculated
		/// </summary>
		public bool HasKerningInformation { get { return _fontFace.HasKerning; } }

		/// <summary>
		/// Creates a new instace of FreeTypeFont
		/// </summary>
		/// <param name="fontPath">The path to the font file</param>
		/// <param name="size">Size of the font</param>
		/// <param name="style">Style of the font</param>
		/// <param name="superSampleLevels">Super sample levels</param>
		/// <param name="scale">Scale</param>
		/// <exception cref="ArgumentException"></exception>
		public FreeTypeFont(string fontPath, float size, FontStyle style, int superSampleLevels = 1, float scale = 1.0f)
		{
			// Check that the font exists
			if (!File.Exists(fontPath)) throw new ArgumentException("The specified font path does not exist", nameof(fontPath));

			StyleFlags fontStyle = StyleFlags.None;
			switch (style)
			{
				case FontStyle.Bold:
					fontStyle = StyleFlags.Bold;
					break;
				case FontStyle.Italic:
					fontStyle = StyleFlags.Italic;
					break;
				case FontStyle.Regular:
					fontStyle = StyleFlags.None;
					break;
				default:
					Debug.WriteLine("Invalid style flag chosen for FreeTypeFont: " + style);
					break;
			}

			LoadFontFace(fontPath, size, fontStyle, superSampleLevels, scale);
		}

		/// <summary>
		/// Creates a new instace of FreeTypeFont
		/// </summary>
		/// <param name="fontData">Contents of the font file</param>
		/// <param name="size">Size of the font</param>
		/// <param name="style">Style of the font</param>
		/// <param name="superSampleLevels">Super sample levels</param>
		/// <param name="scale">Scale</param>
		/// <exception cref="ArgumentException"></exception>
		public FreeTypeFont(byte[] fontData, float size, FontStyle style, int superSampleLevels = 1, float scale = 1.0f)
		{
			StyleFlags fontStyle = StyleFlags.None;
			switch (style)
			{
				case FontStyle.Bold:
					fontStyle = StyleFlags.Bold;
					break;
				case FontStyle.Italic:
					fontStyle = StyleFlags.Italic;
					break;
				case FontStyle.Regular:
					fontStyle = StyleFlags.None;
					break;
				default:
					Debug.WriteLine("Invalid style flag chosen for FreeTypeFont: " + style);
					break;
			}

			LoadFontFace(fontData, size, fontStyle, superSampleLevels, scale);
		}

		private void LoadFontFace(string fontPath, float size, StyleFlags fontStyle, int superSampleLevels, float scale)
		{
			// Get total number of faces in a font file
			var tempFace = _fontLibrary.NewFace(fontPath, -1);
			int numberOfFaces = tempFace.FaceCount;

			// Dispose of the temporary face
			tempFace.Dispose();
			tempFace = null;

			// Loop through to find the style we want
			for (int i = 0; i < numberOfFaces; i++)
			{
				tempFace = _fontLibrary.NewFace(fontPath, i);

				// If we've found the style, exit loop
				if (tempFace.StyleFlags == fontStyle)
					break;

				// Dispose temp face and keep searching
				tempFace.Dispose();
				tempFace = null;
			}

			// Use default font face if correct style not found
			if (tempFace == null)
			{
				Debug.WriteLine("Could not find correct face style in font: " + fontStyle);
				tempFace = _fontLibrary.NewFace(fontPath, 0);
			}

			// Set the face for this instance
			_fontFace = tempFace;

			// Set the size
			Size = size * scale * superSampleLevels;
			_fontFace.SetCharSize(0, Size, 0, DPI);
		}

		private void LoadFontFace(byte[] fontData, float size, StyleFlags fontStyle, int superSampleLevels, float scale)
		{
			// Get total number of faces in a font file
			var tempFace = _fontLibrary.NewMemoryFace(fontData, -1);
			int numberOfFaces = tempFace.FaceCount;

			// Dispose of the temporary face
			tempFace.Dispose();
			tempFace = null;

			// Loop through to find the style we want
			for (int i = 0; i < numberOfFaces; i++)
			{
				tempFace = _fontLibrary.NewMemoryFace(fontData, i);

				// If we've found the style, exit loop
				if (tempFace.StyleFlags == fontStyle)
					break;

				// Dispose temp face and keep searching
				tempFace.Dispose();
				tempFace = null;
			}

			// Use default font face if correct style not found
			if (tempFace == null)
			{
				Debug.WriteLine("Could not find correct face style in font: " + fontStyle);
				tempFace = _fontLibrary.NewMemoryFace(fontData, 0);
			}

			// Set the face for this instance
			_fontFace = tempFace;

			// Set the size
			Size = size * scale * superSampleLevels;
			_fontFace.SetCharSize(0, Size, 0, DPI);
		}

		/// <summary>Returns a string that represents the current object.</summary>
		/// <returns>A string that represents the current object.</returns>
		/// <filterpriority>2</filterpriority>
		public override string ToString()
		{
			return _fontFace.FamilyName ?? "";
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
			// Check we are only passed a single character
			if (s.Length > 1)
				throw new ArgumentOutOfRangeException(nameof(s), "Implementation currently only supports drawing individual characters");

			// Check the brush is a solid colour brush
			if (!(color is SolidBrush))
				throw new ArgumentException("Brush is required to be a SolidBrush (single, solid color)", nameof(color));

			var fontColor = ((SolidBrush)color).Color;

			// Load the glyph into the face's glyph slot
			LoadGlyph(s[0]);

			// Render the glyph
			_fontFace.Glyph.RenderGlyph(RenderMode.Normal);

			// If glyph rendered correctly, copy onto graphics
			if (_fontFace.Glyph.Bitmap.Width > 0)
			{
				var bitmap = _fontFace.Glyph.Bitmap.ToGdipBitmap(fontColor);
				int baseline = y + _maxHorizontalBearyingY;
				graph.DrawImageUnscaled(bitmap, x, (baseline - _fontFace.Glyph.Metrics.HorizontalBearingY.Ceiling()));
				return new Point(0, baseline - _fontFace.Glyph.Metrics.HorizontalBearingY.Ceiling() - 2 * y);
			}

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
			var c1Index = _fontFace.GetCharIndex(c1);
			var c2Index = _fontFace.GetCharIndex(c2);
			var kerning = _fontFace.GetKerning(c1Index, c2Index, KerningMode.Default);
			return kerning.X.Ceiling();
		}

		private void LoadGlyph(char c)
		{
			_fontFace.LoadGlyph(_fontFace.GetCharIndex(c), LoadFlags.Default, LoadTarget.Normal);
		}

		/// <summary>
		/// Measures the given string and returns the size
		/// </summary>
		/// <param name="s">The string to measure</param>
		/// <param name="graph">The graphics surface to use for temporary purposes</param>
		/// <returns>The size of the given string</returns>
		public SizeF MeasureString(string s, Graphics graph)
		{
			// Check we are only passed a single character
			if (s.Length > 1)
				throw new ArgumentOutOfRangeException(nameof(s), "Implementation currently only supports drawing individual characters");

			// Load the glyph into the face's glyph slot
			LoadGlyph(s[0]);

			// Get the glyph metrics
			var gMetrics = _fontFace.Glyph.Metrics;

			// Update max horizontal y bearing if needed
			var yBearing = (int)gMetrics.HorizontalBearingY;

			if (yBearing > _maxHorizontalBearyingY)
				_maxHorizontalBearyingY = yBearing;

			return new SizeF((float)(gMetrics.Width), (float)(gMetrics.Height));
		}

		private bool _disposedValue; // To detect redundant calls

		/// <summary>
		/// Dispose resources 
		/// </summary>
		/// <param name="disposing"></param>
		protected virtual void Dispose(bool disposing)
		{
			if (!_disposedValue)
			{
				if (disposing)
				{
					if (_fontFace != null)
					{
						_fontFace.Dispose();
						_fontFace = null;
					}
					if (_fontLibrary != null)
					{
						_fontLibrary.Dispose();
						_fontLibrary = null;
					}
				}
				_disposedValue = true;
			}
		}

		// This code added to correctly implement the disposable pattern.
		/// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.</summary>
		/// <filterpriority>2</filterpriority>
		public void Dispose()
		{
			// Do not change this code. Put cleanup code in Dispose(bool disposing) above.
			Dispose(true);
		}
	}
}
