﻿// Released to the public domain. Use, modify and relicense at will.

using System;
using System.Diagnostics;
using System.Linq.Expressions;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Audio;
using OpenTK.Audio.OpenAL;
using OpenTK.Input;
using QuickFont;
using System.Drawing;
using QuickFont.Configuration;

namespace StarterKit
{
    class Game : GameWindow
    {
        QFont heading1;
        QFont heading2;
        QFont mainText;
        QFont codeText;
        QFont controlsText;
        QFont monoSpaced;
        private QFontDrawing drawing;
        private QFontDrawing controlsDrawing;

        private Stopwatch _stopwatch;
        private ProcessedText _processedText;
        private QFont _benchmarkResults;
        private Matrix4 _projectionMatrix;


        #region string constants

        private String modernQuickFontIntro =
            @"Welcome to ModernQuickFont. This is yet another fork of the original library QuickFont (which uses ordinary OpenGL). " +
            "This is actually a fork of opcon/QuickFont and thus (as the name implies) is a modern OpenGL implementation using VBOs and VAOs. " +
            "The difference to opcon/QuickFont therefore is a big Refactoring. The original (god like) QFont-class has been separated in 3 concerns: " +
            "Actual Font (save, create, hold texture) [QFont], Drawing-primitive (layout + vertex computations) [QFontDrawingPrimitive] "+
            "and a Drawing container (push stuff to OpenGL, draw) [QFontDrawing]. " +
            "Unfortunately or naturally the API has changed remarkably so that old code is nop longer compatible. "+
            "However the changes are not radical and can be adapted (some conceptual changes may surface) as you can see with thsi example. "+
            "This refactoring into three classes should make using QuickFont more flexible and more pleasing to use. " +
            "It lost of it's ease because you have to handle more classes. But your code will be more future proof and architected well.";
        String modernIntroCode = @"myFont = new QFont(""Fonts/HappySans.ttf"", 72, new QFontBuilderConfiguration(true));"+ Environment.NewLine +
            "drawing = new QFontDrawing();" + Environment.NewLine +
            @"drawing.Print(myFont, ""text"", pos, FontAlignment.Left, opts);" + Environment.NewLine +
            "for { drawing.Draw(); }";

        String introduction = @"Welcome to the QuickFont tutorial. All text in this tutorial (including headings!) is drawn with QuickFont, so it is also intended to showcase the library. :) If you want to get started immediately, you can skip the rest of this introduction by pressing [Right]. You can also press [Left] to go back to previous pages at any point" + Environment.NewLine + Environment.NewLine +
            "Why QuickFont? QuickFont is intended as a replacement (and improvement upon) OpenTK's TextPrinter library. My primary motivation for writing it was for practical reasons: I'm using OpenTK to write a game, and currently the most annoying bugs are all being caused by TextPrinter: it is slow, it is buggy, and no one wants to maintain it." + Environment.NewLine + Environment.NewLine +
            "I did consider simply fixing it, but then decided that it would be easier and more fun to write my own library from scratch. That is exactly what I've done." + Environment.NewLine + Environment.NewLine +
            "In fact it's turned out to be well worth it. It has only taken me a few days to write the library, and already it has quite a few really cool features which I will be using in my game.";

        String usingQuickFontIsSuperEasy = @"Using QuickFont is super easy. To load a font: ";
        String loadingAFont1 = "myFont = new QFont(\"HappySans.ttf\", 16);";
        String andPrintWithIt = @"...and to print with it: ";
        String printWithFont1 = "mainText.Begin();" + Environment.NewLine + "myFont.Print(\"Hello World!\")" + Environment.NewLine + "mainText.End();";
        String itIsAlsoEasyToMeasure = "It is also very easy to measure text: ";
        String measureText1 = "var bounds = myFont.Measure(\"Hello World\"); ";

        String oneOfTheFirstGotchas = "One of the first \"gotchas\" that I experienced with the old TextPrinter was having to manage a private font collection. Unlike TextPrinter, QuickFont does not need the private font collection (or Font object for that matter) to exist after construction. QuickFont works out everything it needs at load time, hence you can just pass it a file name, it will load the pfc internally and then chuck it away immediately. If you still prefer to manage a font collection yourself, and you simply want to create a QuickFont from a font object, that's fine: QuickFont has a constructor for this:  ";
        String loadingAFont2 = "myFont = new QFont(fontObject);";

        String whenPrintingText = "When printing text, you can specify" + Environment.NewLine +
                                  "an alignment. Unbounded text can" + Environment.NewLine + 
                                  "be left-aligned, right-aligned " + Environment.NewLine +
                                  "or centered. You specify the " + Environment.NewLine + 
                                  "alignment as follows: ";

        String printWithFont2 = "myFont.Print(\"Hello World!\",QFontAlignment.Right)";

        String righAlignedText = "Right-aligned text will appear" + Environment.NewLine +
                                 "to the left of the original" + Environment.NewLine +
                                 "position, given by this red line.";

        String centredTextAsYou = "Centred text, as you would expect, is centred" + Environment.NewLine +
                                  "around the current position. The default alignment" + Environment.NewLine +
                                  "is Left. As you can see, you can include " + Environment.NewLine +
                                  "line-breaks in unbounded text.";

        String ofCourseItsNot = "Of course, it's not much fun having to insert your own line-breaks. A much better option is to simply specify the bounds of your text, and then let QuickFont decide where the line-breaks should go for you. You do this by specifying maxWidth. " + Environment.NewLine + Environment.NewLine +
                               "You can still specify line-breaks for new paragraphs. For example, this is all written using a single print. QuickFont is also clever enough to spot where it might have accidentally inserted a line-break just before you have explicitly included one in the text. In this case, it will make sure that it does not insert a redundant line-break. :)" + Environment.NewLine + Environment.NewLine +
                               "Another really cool feature of QuickFont, as you may have guessed already, is that it supports justified bounded text. It was quite tricky to get it all working pixel-perfectly, but as you can see, the results are pretty good. The precise justification settings are configurable in myFont.Options." + Environment.NewLine + Environment.NewLine +
                               "You can press the [Up] and [Down] arrow keys to change the alignment on this block of bounded text. You can also press the [Enter] key to test some serious bounding! Note that the bound height is always ignored.";

        String anotherCoolFeature = "QuickFont works by using the System.Drawing to render to a bitmap, and then measuring and targeting each glyph before packing them into another bitmap which is then turned into an OpenGL texture. So essentially all fonts are \"texture\" fonts. However, QuickFont also allows you to get at the bitmaps before they are turned into OpenGL textures, save them to png file(s), modify them and then load (and retarget/remeasure) them again as QFonts. Sound complicated? - Don't worry, it's really easy. :)" + Environment.NewLine + Environment.NewLine +
                                    "Firstly, you need to create your new silhouette files from an existing font. You only want to call this code once, as calling it again will overwrite your modified .png, so take care. :) ";

        String textureFontCode1 = "QFont.CreateTextureFontFiles(\"HappySans.ttf\",16,\"myTextureFont\");";

        String thisWillHaveCreated = "This will have created two files: \"myTextureFont.qfont\" and \"myTextureFont.png\" (or possibly multiple png files if your font is very large - I will explain how to configure this later). The next step is to actually texture your font. The png file(s) contain packed font silhouettes, perfect for layer effects in programs such as photoshop or GIMP. I suggest locking the alpha channel first, because QuickFont will complain if you merge two glyphs. You can enlarge glyphs at this stage, and QuickFont will automatically retarget each glyph when you next load the texture; however, it will fail if glyphs are merged...    ";

        String ifYouDoIntend = "...if you do intend to increase the size of the glyphs, then you can configure the silhouette texture to be generated with larger glyph margins to avoid glyphs merging. Here, I've also configured the texture sheet size a bit larger because the font is large and I want it all on one sheet for convenience: ";

        String textureFontCode2 = "QFontBuilderConfiguration config = new QFontBuilderConfiguration();" + Environment.NewLine +
            "config.GlyphMargin = 6;" + Environment.NewLine +
            "config.PageWidth = 1024;" + Environment.NewLine +
            "config.PageHeight = 1024;" + Environment.NewLine +
            "QFont.CreateTextureFontFiles(\"HappySans.ttf\",48,config,\"myTextureFont\");";

        String actuallyTexturing = "Actually texturing the glyphs is really going to come down to how skilled you are in photoshop, and how good the original font was that you chose as a silhouette. To give you an idea: this very cool looking font I'm using for headings only took me 3 minutes to texture in photoshop because I did it with layer affects that did all glyphs at once. :)" + Environment.NewLine + Environment.NewLine +
            "Anyway, once you've finished texturing your font, save the png file. Now you can load the font and write with it just like any other font!";

        String textureFontCode3 = "myTexureFont = QFont.FromQFontFile(\"myTextureFont.qfont\");";

        String asIhaveleant = "As I have learnt, trying to create drop-shadows as part of the glyphs themselves gives very poor results because the glyphs become larger than usual and the font looks poor when printed. To do drop-shadows properly, they need to be rendered separately underneath each glyph. This is what QuickFont does. In fact it does a lot more: it will generate the drop-shadow textures for you. It's super-easy to create a font with a drop-shadow: ";
        String dropShadowCode1 = "myFont = new QFont(\"HappySans.ttf\", 16, new QFontBuilderConfiguration(true));";
        String thisWorksFine = "This works fine for texture fonts too: ";
        String dropShadowCode2 = "myTexureFont = QFont.FromQFontFile(\"myTextureFont.qfont\", new QFontLoaderConfiguration(true));";
        String onceAFont = "Once a font has been loaded with a drop-shadow, it will automatically be rendered with a shadow. However, you can turn this off or customise the drop-shadow in myFont.options when rendering (I am rotating the drop shadow here, which looks kind of cool but is now giving me a headache xD). I've turned drop-shadows on for this font on this page; however, they are very subtle because the font is so tiny. If you want the shadow to be more visible for tiny fonts like this, you could modify the DropShadowConfiguration object passed into the font constructor to blur the shadow less severely during creation. ";

        String thereAreActually = "There are actually a lot more interesting config values and neat things that QuickFont does. Now that I look back at it, it's a bit crazy that I got this all done in a few days, but this tutorial is getting a bit tedious to write and I'm dying to get on with making my game, so I'm going to leave it at this. " + Environment.NewLine + Environment.NewLine +
            "I suppose I should also mention that there are almost certainly going to be a few bugs. Let me know if you find any and I will get them fixed asap. :) " + Environment.NewLine + Environment.NewLine +
            "I should probably also say something about the code: it's not unit tested and it probably would need a good few hours of refactoring before it would be clean enough to be respectable. I will do this at some point. Also, feel free to berate me if I'm severely breaking any conventions. I'm a programmer by profession and really should know better. ;)" + Environment.NewLine + Environment.NewLine +
            "With regard to features: I'm probably not going to add many more to this library. It really is intended for rendering cool-looking text quickly for things like games. If you want highly formatted text, for example, then it probably isn't the right tool. I hope you find it useful; I know I already do! :P" + Environment.NewLine + Environment.NewLine +
            "A tiny disclaimer: all of QuickFont is written from scratch apart from ~100 lines I stole from TextPrinter for setting the correct perspective. Obviously the example itself is just a hacked around version of the original example that comes with OpenTK.";

        String hereIsSomeMono = "Here is some mononspaced text.  Monospaced fonts will automatically be rendered in monospace mode; however, you can render monospaced fonts ordinarily " +
                                "or ordinary fonts in monospace mode using the render option:";
        String monoCode1 = " myFont.Options.Monospacing = QFontMonospacing.Yes; ";
        String theDefaultMono = "The default value for this is QFontMonospacing.Natural which simply means that if the underlying font was monospaced, then use monospacing. ";


        String mono =           " **   **   **   *  *   **  " + Environment.NewLine +                  
                                " * * * *  *  *  ** *  *  * " + Environment.NewLine +                
                                " *  *  *  *  *  * **  *  * " + Environment.NewLine +             
                                " *     *   **   *  *   **  ";

        private string preProcessed = "Text can be preprocessed which improves rendering time. This text is preprocessed.";
        private string nonPreProcessed = "Text can be preprocessed which improves rendering time. This text is not preprocessed.";
        #endregion

        int currentDemoPage = 0;
        int lastPage = 10;
        private int frameCount = 0;

        private string _benchResult="";

        QFontAlignment cycleAlignment = QFontAlignment.Left;

        /// <summary>Creates a 800x600 window with the specified title.</summary>
        public Game()
            : base(800, 600, GraphicsMode.Default, "OpenTK Quick Start Sample")
        {
            VSync = VSyncMode.Off;
            this.WindowBorder = WindowBorder.Fixed;
        }

        private void KeyDown(object sender, KeyboardKeyEventArgs keyEventArgs)
        {
            int texmax = GL.GetInteger(GetPName.MaxTextureSize);
            switch (keyEventArgs.Key)
            {
                case Key.Space:
                case Key.Right:
                    currentDemoPage++;
                    break;

                case Key.BackSpace:
                case Key.Left:
                    currentDemoPage--;
                    break;

                case Key.Enter:
                {
                    if (currentDemoPage == 4)
                        boundsAnimationCnt = 0f;
                    break;
                }

                case Key.Up:
                {
                    if (currentDemoPage == 4)
                    {
                        if (cycleAlignment == QFontAlignment.Justify)
                            cycleAlignment = QFontAlignment.Left;
                        else
                            cycleAlignment++;
                    }
                    break;
                }

                case Key.Down:
                {
                    if (currentDemoPage == 4)
                    {
                        if (cycleAlignment == QFontAlignment.Left)
                            cycleAlignment = QFontAlignment.Justify;
                        else
                            cycleAlignment--;
                    }
                    break;
                }
                case Key.F9:
                    break;
            }

            if (currentDemoPage > lastPage)
                currentDemoPage = lastPage;
            if (currentDemoPage < 0)
                currentDemoPage = 0;
        }

        /// <summary>Load resources here.</summary>
        /// <param name="e">Not used.</param>
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            this.Keyboard.KeyDown += KeyDown;
            drawing = new QFontDrawing();
            controlsDrawing = new QFontDrawing();
            controlsTextOpts = new QFontRenderOptions() { Colour = Color.FromArgb(new Color4(0.8f, 0.1f, 0.1f, 1.0f).ToArgb()), DropShadowActive = true };

            heading2 = new QFont("woodenFont.qfont", new QFontConfiguration(addDropShadow: true), 1.0f);
            heading2Options = new QFontRenderOptions() { Colour = Color.White, DropShadowActive = true};

            var builderConfig = new QFontBuilderConfiguration(addDropShadow: true);
            builderConfig.ShadowConfig.blurRadius = 2; //reduce blur radius because font is very small
            builderConfig.ShadowConfig.blurPasses = 1;
            builderConfig.ShadowConfig.Type = ShadowType.Blurred;
            builderConfig.TextGenerationRenderHint = TextGenerationRenderHint.ClearTypeGridFit; //best render hint for this font
            builderConfig.Characters = CharacterSet.General | CharacterSet.Japanese | CharacterSet.Thai | CharacterSet.Cyrillic;
            mainText = new QFont("Fonts/times.ttf", 14, builderConfig);
            mainTextOptions = new QFontRenderOptions() { DropShadowActive = true, Colour = Color.White, WordSpacing = 0.5f};
 
            _benchmarkResults = new QFont("Fonts/times.ttf", 14, builderConfig);

            heading1 = new QFont("Fonts/HappySans.ttf", 72, new QFontBuilderConfiguration(true));

            controlsText = new QFont("Fonts/HappySans.ttf", 32, new QFontBuilderConfiguration(true));

            codeText = new QFont("Fonts/Comfortaa-Regular.ttf", 12, new QFontBuilderConfiguration());

            heading1Options = new QFontRenderOptions() { Colour = Color.FromArgb(new Color4(0.2f, 0.2f, 0.2f, 1.0f).ToArgb()), DropShadowActive = true};
            _processedText = QFontDrawingPrimitive.ProcessText(mainText, mainTextOptions, preProcessed, new SizeF(Width - 40, -1), QFontAlignment.Left);
            codeTextOptions = new QFontRenderOptions() { Colour = Color.FromArgb(new Color4(0.0f, 0.0f, 0.4f, 1.0f).ToArgb()) };

            monoSpaced = new QFont("Fonts/Anonymous.ttf", 10, new QFontBuilderConfiguration());
            monoSpacedOptions = new QFontRenderOptions() { Colour = Color.FromArgb(new Color4(0.1f, 0.1f, 0.1f, 1.0f).ToArgb()), DropShadowActive = true};

            GL.ClearColor(Color4.CornflowerBlue);
        }

        /// <summary>
        /// Called when your window is resized. Set your viewport here. It is also
        /// a good place to set up your projection matrix (which probably changes
        /// along when the aspect ratio of your window).
        /// </summary>
        /// <param name="e">Not used.</param>
        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);

            GL.Viewport(ClientRectangle.X, ClientRectangle.Y, ClientRectangle.Width, ClientRectangle.Height);

            //_projectionMatrix = Matrix4.CreateOrthographicOffCenter(X, Width, Y, Height, -1.0f, 1.0f);
            _projectionMatrix = Matrix4.CreateOrthographicOffCenter(ClientRectangle.X, ClientRectangle.Width, ClientRectangle.Y, ClientRectangle.Height, -1.0f, 1.0f);
            //_projectionMatrix = Matrix4.Mult(Matrix4.CreateScale(1, -1.0f, 1), _projectionMatrix);
            //_projectionMatrix = Matrix4.Mult(Matrix4.CreateScale(0.5f), _projectionMatrix);
        }

        double cnt;
        double boundsAnimationCnt = 1.0f;
        private QFontRenderOptions monoSpacedOptions;
        private QFontRenderOptions codeTextOptions;
        private QFontRenderOptions mainTextOptions;
        private QFontRenderOptions heading1Options;
        private QFontRenderOptions heading2Options;
        private QFontRenderOptions controlsTextOpts;
        private int _previousPage = -1;

        /// <summary>
        /// Called when it is time to setup the next frame. Add you game logic here.
        /// </summary>
        /// <param name="e">Contains timing information for framerate independent logic.</param>
        protected override void OnUpdateFrame(FrameEventArgs e)
        {
            base.OnUpdateFrame(e);

            if (Keyboard[Key.Escape])
                Exit();

            cnt += e.Time;

            if (boundsAnimationCnt < 1.0f)
                boundsAnimationCnt += e.Time * 0.2f;
            else
                boundsAnimationCnt = 1.0f;
        }

        private void PrintWithBounds(QFont font, string text, RectangleF bounds, QFontAlignment alignment, ref float yOffset)
        {
            float maxWidth = bounds.Width;

            float height = font.Measure(text, new SizeF(maxWidth, -1), alignment).Height;

            //gl.begin(beginmode.lineloop);
            //gl.vertex3(bounds.x, bounds.y, 0f);
            //gl.vertex3(bounds.x + bounds.width, bounds.y, 0f);
            //gl.vertex3(bounds.x + bounds.width, bounds.y + height, 0f);
            //gl.vertex3(bounds.x, bounds.y + height, 0f);
            //gl.end();

            var dp = new QFontDrawingPrimitive(font);
            dp.Print(text, new Vector3(bounds.X, Height - yOffset, 0), new SizeF(maxWidth, float.MaxValue), alignment);
            drawing.DrawingPrimitives.Add(dp);

            yOffset += height;
        }       
       //some helpers



        private void PrintComment(string comment, ref float yOffset)
        {
            PrintComment(mainText, comment, QFontAlignment.Justify, ref yOffset, mainTextOptions);
        }

        private void PrintComment(QFont font, string comment, QFontAlignment alignment, ref float yOffset, QFontRenderOptions opts )
        {
            yOffset += 20;
            var pos = new Vector3(30f, Height - yOffset, 0f);
            var dp = new QFontDrawingPrimitive(font, opts ?? new QFontRenderOptions());
            dp.Print(comment, pos, new SizeF(Width - 60, -1), alignment);
            yOffset += dp.Measure(comment, new SizeF(Width - 60, -1), alignment).Height;
            drawing.DrawingPrimitives.Add(dp);
        }

        private void PrintCommentWithLine(string comment, QFontAlignment alignment, float xOffset, ref float yOffset)
        {
            PrintCommentWithLine(mainText, comment, alignment, xOffset, ref yOffset, mainTextOptions);
        }

        private void PrintCommentWithLine(QFont font, string comment, QFontAlignment alignment, float xOffset, ref float yOffset, QFontRenderOptions opts)
        {
            yOffset += 20;
            var dp = new QFontDrawingPrimitive(font, opts);
            //if (doSpacing)
            //    dp.Options.CharacterSpacing = 0.05f;
            dp.Print(comment, new Vector3(xOffset, Height - yOffset, 0f), new SizeF(Width - 60, -1), alignment);
            drawing.DrawingPrimitives.Add(dp);
            var bounds = font.Measure(comment, new SizeF(Width - 60, float.MaxValue), alignment);

            //GL.Disable(EnableCap.Texture2D);
            //GL.Begin(BeginMode.Lines);
            //GL.Color4(1.0f, 0f, 0f, 1f); GL.Vertex2(0f, 0f);
            //GL.Color4(1.0f, 0f, 0f, 1f); GL.Vertex2(0f, bounds.Height + 20f);
            //GL.End();

            yOffset += bounds.Height;
        }

        
        private void PrintCode(string code, ref float yOffset)
        {
            yOffset += 20;
            var pos = new Vector3(50f, Height - yOffset, 0f);
            drawing.Print(codeText, code, pos, new SizeF(Width - 50, -1), QFontAlignment.Left, codeTextOptions);
            yOffset += codeText.Measure(code, new SizeF(Width - 50, -1), QFontAlignment.Left).Height;
        }

        /// <summary>
        /// Called when it is time to render the next frame. Add your rendering code here.
        /// </summary>
        /// <param name="e">Contains timing information.</param>
        protected override void OnRenderFrame(FrameEventArgs e)
        {
            base.OnRenderFrame(e);

            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            drawing.ProjectionMatrix = _projectionMatrix;

            frameCount++;

            float yOffset = 0;

            //mainText.Begin();
            //GL.Begin(BeginMode.Quads);
 
            //GL.Color3(1.0f, 1.0f, 1.0); GL.Vertex2(0, 0);
            //GL.Color3(0.9f, 0.9f, 0.9f); GL.Vertex2(0, Height);
            //GL.Color3(0.9f, 0.9f, 0.9f); GL.Vertex2(Width, Height);
            //GL.Color3(0.9f, 0.9f, 0.9f); GL.Vertex2(Width, 0);
  
            //GL.End();
            //mainText.End();
            if (currentDemoPage != _previousPage)
            {
                _previousPage = currentDemoPage;
                // we have to rebuild the stuff
                drawing.DrawingPrimitives.Clear();
                            
                switch (currentDemoPage)
                {
                    case 0:
                        {
                            yOffset += drawing.Print(heading1, "ModernQuickFont",
                                                                     new Vector3((float) Width/2, Height, 0),
                                                                     QFontAlignment.Centre, heading1Options).Height;

                            yOffset += drawing.Print(heading2, "Introduction #0",
                                                                     new Vector3(20, Height - yOffset, 0),
                                                                     QFontAlignment.Left, heading2Options)
                                        .Height;

                            yOffset += drawing.Print(mainText, modernQuickFontIntro, new Vector3(30, Height - yOffset, 0),
                                                                 new SizeF(Width - 60f, -1), QFontAlignment.Justify, new Rectangle(60, Height-400, 200, 200)).Height;
                            
                            PrintCode(modernIntroCode, ref yOffset);
                            break;
                        }

                    case 1:
                        {
                            yOffset += drawing.Print(heading1, "QuickFont",
                                                                     new Vector3((float) Width/2, Height, 0),
                                                                     QFontAlignment.Centre, heading1Options).Height;

                            yOffset += drawing.Print(heading2, "Introduction",
                                                                     new Vector3(20, Height - yOffset, 0),
                                                                     QFontAlignment.Left, heading2Options)
                                        .Height;

                            yOffset += 20f;
                            drawing.Print(mainText, introduction, new Vector3(30, Height - yOffset, 0),
                                                                 new SizeF(Width - 60f, -1), QFontAlignment.Justify);
                            
                            break;
                        }

                    case 2:
                        {
                            yOffset += drawing.Print( heading2, "Easy as ABC!",
                                                                     new Vector3(20f, Height - yOffset, 0f),
                                                                     QFontAlignment.Left, heading2Options).Height;

                            PrintComment(usingQuickFontIsSuperEasy, ref yOffset);
                            PrintCode(loadingAFont1, ref yOffset);

                            PrintComment(andPrintWithIt, ref yOffset);
                            PrintCode(printWithFont1, ref yOffset);

                            PrintComment(itIsAlsoEasyToMeasure, ref yOffset);
                            PrintCode(measureText1, ref yOffset);

                            PrintComment(oneOfTheFirstGotchas, ref yOffset);
                            PrintCode(loadingAFont2, ref yOffset);

                            break;
                        }

                    case 3:
                        {
                            yOffset +=  drawing.Print(heading2 ,"Alignment", new Vector3(20f, Height - yOffset, 0f),
                                                                     QFontAlignment.Left, heading2Options).Height;

                            PrintCommentWithLine(whenPrintingText, QFontAlignment.Left, 20f, ref yOffset);
                            PrintCode(printWithFont2, ref yOffset);

                            PrintCommentWithLine(righAlignedText, QFontAlignment.Right, 20f, ref yOffset);
                            yOffset += 10f;

                            PrintCommentWithLine(centredTextAsYou, QFontAlignment.Centre, Width*0.5f, ref yOffset);

                            break;
                        }

                    case 4:
                        {
                            // in this stage force redraw and recreation of VBO every time: just divert last page
                            _previousPage = -1;
                            yOffset += drawing.Print(heading2, "Bounds and Justify",
                                                                     new Vector3(20f, Height - yOffset, 0f),
                                                                     QFontAlignment.Left, heading2Options).Height;

                            yOffset += 20;
                            yOffset +=  drawing.Print( controlsText, "Press [Up], [Down] or [Enter]!",
                                                                         new Vector3(Width*0.5f, Height - yOffset, 0f),
                                                                         QFontAlignment.Centre, controlsTextOpts).Height;

                            float boundShrink = (int) (350*(1 - Math.Cos(boundsAnimationCnt*Math.PI*2)));

                            yOffset += 15;
                            PrintWithBounds(mainText, ofCourseItsNot,
                                            new RectangleF(30f + boundShrink*0.5f, yOffset, Width - 60 - boundShrink,
                                                           350f), cycleAlignment, ref yOffset);

                            string printWithBounds = "myFont.Print(text, position, maxSize, QFontAlignment." +
                                                     cycleAlignment + ");";
                            yOffset += 15f;
                            PrintCode(printWithBounds, ref yOffset);

                            break;
                        }

                    case 5:
                        {
                            yOffset +=  drawing.Print(heading2, "Your own Texture Fonts",
                                                                     new Vector3(20f, Height - yOffset, 0f),
                                                                     QFontAlignment.Left, heading2Options).Height;

                            PrintComment(anotherCoolFeature, ref yOffset);
                            PrintCode(textureFontCode1, ref yOffset);
                            PrintComment(thisWillHaveCreated, ref yOffset);

                            break;
                        }

                    case 6:
                        {
                            yOffset +=  drawing.Print(heading2, "Your own Texture Fonts",
                                                                     new Vector3(20f, Height - yOffset, 0f),
                                                                     QFontAlignment.Left, heading2Options).Height;

                            PrintComment(ifYouDoIntend, ref yOffset);
                            PrintCode(textureFontCode2, ref yOffset);
                            PrintComment(actuallyTexturing, ref yOffset);
                            PrintCode(textureFontCode3, ref yOffset);

                            break;
                        }

                    case 7:
                        {
                            // in this stage force redraw and recreation of VBO every time: just divert last page
                            _previousPage = -1;

                            // store this primitive to remember
                            QFontDrawingPrimitive dp = new QFontDrawingPrimitive(heading2);
                            dp.Options.DropShadowActive = true;
                            dp.Options.DropShadowOffset = new Vector2(0.1f + 0.2f*(float) Math.Sin(cnt),
                                                                            0.1f + 0.2f*(float) Math.Cos(cnt));

                            yOffset += dp.Print("Drop Shadows",
                                                                     new Vector3(20f, Height - yOffset, 0f),
                                                                     QFontAlignment.Left).Height;
                            drawing.DrawingPrimitives.Add(dp);

                            PrintComment(asIhaveleant, ref yOffset);
                            PrintCode(dropShadowCode1, ref yOffset);
                            PrintComment(thisWorksFine, ref yOffset);
                            PrintCode(dropShadowCode2, ref yOffset);
                            PrintComment(onceAFont, ref yOffset);

                            //mainText.Options.DropShadowActive = false;
                            break;
                        }

                    case 8:
                        {
                            yOffset += drawing.Print(heading2, "Monospaced Fonts",
                                                                     new Vector3(20f, Height - yOffset, 0f),
                                                                     QFontAlignment.Left, heading2Options).Height;

                            QFontRenderOptions monoSpaceCondensed = monoSpacedOptions.CreateClone();
                            monoSpaceCondensed.CharacterSpacing = 0.05f;
                            PrintComment(monoSpaced, hereIsSomeMono, QFontAlignment.Left, ref yOffset, monoSpaceCondensed);
                            PrintCode(monoCode1, ref yOffset);
                            PrintComment(monoSpaced, theDefaultMono, QFontAlignment.Left, ref yOffset, monoSpaceCondensed);

                            PrintCommentWithLine(monoSpaced, mono, QFontAlignment.Left, 20f, ref yOffset, monoSpaceCondensed);
                            yOffset += 2f;
                            PrintCommentWithLine(monoSpaced, mono, QFontAlignment.Right, 20f, ref yOffset, monoSpaceCondensed);
                            yOffset += 2f;
                            PrintCommentWithLine(monoSpaced, mono, QFontAlignment.Centre, Width * 0.5f, ref yOffset, monoSpaceCondensed);
                            yOffset += 2f;

                            PrintComment(monoSpaced,
                                         "As usual, you can adjust character spacing with myPrimitive.Options.CharacterSpacing.",
                                         QFontAlignment.Left, ref yOffset, monoSpaceCondensed);

                            
                            break;
                        }

                    case 9:
                        {
                            // in this stage force redraw and recreation of VBO every time: just divert last page
                            _previousPage = -1;
                            yOffset += drawing.Print(heading2, "Preprocessed Text",
                                                                     new Vector3(20f, Height - yOffset, 0f),
                                                                     QFontAlignment.Left).Height;
                            yOffset += 20f;

                            _stopwatch = Stopwatch.StartNew();
                            yOffset +=  drawing.Print(mainText, _processedText,
                                                                     new Vector3(20, Height - yOffset, 0)).Height;
                            _stopwatch.Stop();
                            long preprocessed = _stopwatch.Elapsed.Ticks;

                            _stopwatch = Stopwatch.StartNew();
                            yOffset += drawing.Print( mainText,nonPreProcessed,
                                                                     new Vector3(20, Height - yOffset, 0),
                                                                     new SizeF(Width - 40f, -1), QFontAlignment.Left)
                                        .Height;
                            _stopwatch.Stop();
                            long notpreprocessed = _stopwatch.Elapsed.Ticks;

                            if (frameCount > 60)
                            {
                                _benchResult = string.Format(("{0}       {1}\nPreprocessed was {2} ticks faster"),
                                                             preprocessed,
                                                             notpreprocessed, notpreprocessed - preprocessed);
                                frameCount = 0;
                            }

                            drawing.Print(_benchmarkResults, _benchResult,
                                                                          new Vector3(Width*0.5f, Height - yOffset, 0),
                                                                          QFontAlignment.Centre, Color.White);
                            break;
                        }

                    case 10:
                        {
                            yOffset += drawing.Print(heading2, "In Conclusion",
                                                                     new Vector3(20f, Height - yOffset, 0f),
                                                                     QFontAlignment.Left, heading2Options).Height;

                            PrintComment(thereAreActually, ref yOffset);

                            break;
                        }
                }

                drawing.RefreshBuffers();

            }

            // Create controlsDrawing every time.. would be also good to vary ProjectionMatrix with * Matrix4.CreateTranslation() !
            // this would save buffer work for OpenGL
            controlsDrawing.DrawingPrimitives.Clear();
            controlsDrawing.ProjectionMatrix = _projectionMatrix;

            if (currentDemoPage != lastPage)
            {
                Vector3 pos = new Vector3(Width - 10 - 16 * (float)(1 + Math.Sin(cnt * 4)), 
                    controlsText.Measure("P").Height + 10f, 0f);
                controlsDrawing.Print(controlsText, "Press [Right] ->", pos, QFontAlignment.Right, controlsTextOpts);
            }

            if (currentDemoPage != 0)
            {
                var pos = new Vector3(10 + 16*(float) (1 + Math.Sin(cnt*4)), controlsText.Measure("P").Height + 10f, 0f);
                controlsDrawing.Print(controlsText, "<- Press [Left]", pos, QFontAlignment.Left, controlsTextOpts);
            }
            controlsDrawing.RefreshBuffers();
            controlsDrawing.Draw();

            drawing.Draw();
            SwapBuffers();
        }

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // The 'using' idiom guarantees proper resource cleanup.
            // We request 30 UpdateFrame events per second, and unlimited
            // RenderFrame events (as fast as the computer can handle).
            using (Game game = new Game())
            {
                game.Run(30.0);
            }
        }
    }
}