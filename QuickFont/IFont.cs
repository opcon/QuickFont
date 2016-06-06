using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;

namespace QuickFont
{
	public interface IFont : IDisposable
	{
		string ToString();

		SizeF MeasureString(string s, Graphics graph);

		float Size { get; }

		void DrawString(string s, Graphics graph, Brush color, int x, int y);
	}
}
