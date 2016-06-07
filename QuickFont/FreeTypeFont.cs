using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using SharpFont;
using System.Diagnostics;
using SharpFont.TrueType;

namespace QuickFont
{
	class FreeTypeFont : IFont
	{
		private Library _fontLibrary = new Library();
		private const uint DPI = 96;

		private Face _fontFace;
		private float _fontSize;

		private int _maxHorizontalBearyingY = 0;

		public FreeTypeFont(string fontPath, float size, FontStyle style, int superSampleLevels = 1, float scale = 1.0f)
		{
			StyleFlags _fontStyle = StyleFlags.None;
			switch (style)
			{
				case FontStyle.Bold:
					_fontStyle = StyleFlags.Bold;
					break;
				case FontStyle.Italic:
					_fontStyle = StyleFlags.Italic;
					break;
				case FontStyle.Regular:
					_fontStyle = StyleFlags.None;
					break;
				case FontStyle.Underline:
				case FontStyle.Strikeout:
				default:
					Debug.WriteLine("Invalid style flag chosen for FreeTypeFont: " + style);
					break;
			}

			LoadFontFace(fontPath, size, _fontStyle, superSampleLevels, scale);
		}

		private void LoadFontFace(string fontPath, float size, StyleFlags _fontStyle, int superSampleLevels, float scale)
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
				if (tempFace.StyleFlags == _fontStyle)
					break;

				// Dispose temp face and keep searching
				tempFace.Dispose();
				tempFace = null;
			}

			// Use default font face if correct style not found
			if (tempFace == null)
			{
				Debug.WriteLine("Could not find correct face style in font: " + _fontStyle);
				tempFace = _fontLibrary.NewFace(fontPath, 0);
			}

			// Set the face for this instance
			_fontFace = tempFace;

			// Set the size
			_fontSize = size * scale * superSampleLevels;
			_fontFace.SetCharSize(0, _fontSize, 0, DPI);
		}

		public override string ToString()
		{
			return _fontFace.FamilyName ?? "";
		}

		public float Size { get { return _fontSize; } }
			
		public Point DrawString(string s, Graphics graph, Brush color, int x, int y, float height)
		{
			// Check we are only passed a single character
			if (s.Length > 1)
				throw new ArgumentOutOfRangeException("s", "Implementation currently only supports drawing individual characters");

			// Check the brush is a solid colour brush
			if (!(color is SolidBrush))
				throw new ArgumentException("color", "Brush is required to be a SolidBrush (single, solid color)");

			var fontColor = (color as SolidBrush).Color;

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
				return new Point(0, baseline - _fontFace.Glyph.Metrics.HorizontalBearingY.Ceiling() - 2*y);
			}

			return Point.Empty;
		}

		private void LoadGlyph(char c)
		{
			_fontFace.LoadGlyph(_fontFace.GetCharIndex(c), LoadFlags.Default, LoadTarget.Normal);
		}

		public SizeF MeasureString(string s, Graphics graph)
		{
			// Check we are only passed a single character
			if (s.Length > 1)
				throw new ArgumentOutOfRangeException("s", "Implementation currently only supports drawing individual characters");

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

		private bool disposedValue = false; // To detect redundant calls

		protected virtual void Dispose(bool disposing)
		{
			if (!disposedValue)
			{
				if (disposing)
				{
					if (_fontFace != null)
					{
						_fontFace.Dispose();
						_fontFace = null;
					}
				}

				// TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
				// TODO: set large fields to null.

				disposedValue = true;
			}
		}

		// TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
		// ~FreeTypeFont() {
		//   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
		//   Dispose(false);
		// }

		// This code added to correctly implement the disposable pattern.
		public void Dispose()
		{
			// Do not change this code. Put cleanup code in Dispose(bool disposing) above.
			Dispose(true);
			// TODO: uncomment the following line if the finalizer is overridden above.
			// GC.SuppressFinalize(this);
		}
	}
}
