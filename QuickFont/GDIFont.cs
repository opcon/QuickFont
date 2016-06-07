using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Text;
using System.Linq;
using System.Text;

namespace QuickFont
{
	public class GDIFont : IFont, IDisposable
	{
		private Font _font;
		private FontFamily _fontFamily;

		public float Size { get { return _font.Size; } }

		/// <summary>
		///     Creates a GDI+ Font from the specified font file
		/// </summary>
		/// <param name="fontPath">The path to the font file</param>
		/// <param name="size"></param>
		/// <param name="style"></param>
		/// <param name="superSampleLevels"></param>
		/// <param name="scale"></param>
		public GDIFont(string fontPath, float size, FontStyle style, int superSampleLevels = 1, float scale = 1.0f)
		{
			try
			{
				var pfc = new PrivateFontCollection();
				pfc.AddFontFile(fontPath);
				_fontFamily = pfc.Families[0];
			}
			catch (System.IO.FileNotFoundException ex)
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

		public Point DrawString(string s, Graphics graph, Brush color, int x, int y, float height)
		{
			graph.DrawString(s, _font, color, x, y);

			Debug.WriteLine(string.Format("Loading character {0}, y position is {1}", s, y));

			return Point.Empty;
		}

		public SizeF MeasureString(string s, Graphics graph)
		{
			return graph.MeasureString(s, _font);
		}

		public override string ToString()
		{
			return _font.ToString();
		}

		public void Dispose()
		{
			_font.Dispose();
			_fontFamily.Dispose();
		}
	}
}
