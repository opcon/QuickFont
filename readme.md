###ModernQuickFont
A Modern OpenGL implementation of the VBO advanced [opcon/QuickFont](https://github.com/opcon/QuickFont) library.
Original Library [QFont](http://www.opentk.com/project/QuickFont)

Welcome to ModernQuickFont. This is yet another fork of the original library QuickFont (which uses ordinary OpenGL).
This is actually a fork of opcon/QuickFont and thus (as the name implies) is a modern OpenGL implementation using VBOs and VAOs.
The difference to opcon/QuickFont therefore is a big Refactoring. The original (god like) QFont-class has been separated in 3 concerns:
Actual Font (save, create, hold texture) [QFont], Drawing-primitive (layout + vertex computations) [QFontDrawingPrimitive]
and a Drawing container (push stuff to OpenGL, draw) [QFontDrawing].
Unfortunately or naturally the API has changed remarkably so that old code is nop longer compatible.
However the changes are not radical and can be adapted (some conceptual changes may surface) as you can see with thsi example.
This refactoring into three classes should make using QuickFont more flexible and more pleasing to use.
It lost of it's ease because you have to handle more classes. But your code will be more future proof and architected well.

##What's new
- [x] Version changed to 3.0
- [x] QFont in it's form is history
- [x] QFont is the new Font ressource
- [x] QFontDrawingPrimitve layouts everything
- [x] QFontDrawing is the drawing container that actually draws (composed off primitives!)
- [x] special care for quadratic and small texture sizes removed (OpenGL does not need this rescriction Texture size 8129 should be normal)
- [x] Also because the new way of holding everything in one VAO requires one texture per QFont (shadows another one) otherwise it can not be implemented efficiently
- [x] therefore changed defaults for Texture default sizes to 4096. (QFontBuilderConfiguration, QFontShadowConfiguration)
- [x] Added support for other than latin scripts to have an adequately populated character set. 
- [x] Removed more legacy stuff.
- [x] Updated Example to work again. Left text alone just added a new Page 0.

###Todo
- [x] Maybe extract all Print methods in a static class to leave QFontDrawingPrimitive more basic.
- [x] Right to Left text flow support (arabic, hebrew)
- [x] Unicode zero spacing eg. combining character support
- [x] On-the-fly character addition (If a character can not be found, add it, regenerate the font)


##Code
So how would the code look like, now?

In some OnLoad() method create your QFont and your QFontDrawing
```C#
_myFont = new QFont("Fonts/HappySans.ttf", 72, new QFontBuilderConfiguration(true));
_myFont2 = new QFont("basics.qfont", new QFontBuilderConfiguration(true));
_drawing = new QFontDrawing();
```

On Event (to create screen) call some print methods or create Drawing primitives by themselves.
Add them to the drawing.
```C#
_drawing.DrawingPimitiveses.Clear();
_drawing.Print(_myFont, "text1", pos, FontAlignment.Left);

// draw with options
var textOpts = new QFontRenderOptions() 
    { 
	Colour = Color.FromArgb(new Color4(0.8f, 0.1f, 0.1f, 1.0f).ToArgb()), 
	DropShadowActive = true 
	};
SizeF size = _drawing.Print(_myFont, "text2", pos2, FontAlignment.Left, textOpts);
	
var dp = new QFontDrawingPimitive(_myFont2);
size = dp.Print(text, new Vector3(bounds.X, Height - yOffset, 0), new SizeF(maxWidth, float.MaxValue), alignment);
drawing.DrawingPimitiveses.Add(dp);	
	
// after all changes do update buffer data and extend it's size if needed.	
_drawing.RefreshBuffers();

```

Then in your paint-loop do:
```C#
_drawing.ProjectionMatrix = proj;
_drawing.Draw();
SwapBuffers();
```

At the end of the program dispose your own resources:
```C#
protected virtual void Dispose(bool disposing)
{
	_drawing.Dispose();
	_myFont.Dispose();
	_myFont2.Dispose();
}
```


###Please not this API is not backwards compatible with all previous QuickFont releases therefore new Version 3


