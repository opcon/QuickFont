// Released to the public domain. Use, modify and relicense at will.

using System;

using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Audio;
using OpenTK.Audio.OpenAL;
using OpenTK.Input;
using QuickFont;
using System.Drawing;

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


        #region string constants


        String introduction = @"Welcome to the QuickFont tutorial. All text in this tutorial (including headings!) is drawn with QuickFont, so it is also intended to showcase the library. :) If you want to get started immediately, you can skip the rest of this introduction by pressing [Right]. You can also press [Left] to go back to previous pages at any point" + Environment.NewLine + Environment.NewLine +
            "Why QuickFont? QuickFont is intended as a replacement (and improvement upon) OpenTK's TextPrinter library. My primary motivation for writing it was for practical reasons: I'm using OpenTK to write a game, and currently the most annoying bugs are all being caused by TextPrinter: it is slow, it is buggy, and no one wants to maintain it." + Environment.NewLine + Environment.NewLine +
            "I did consider simply fixing it, but then decided that it would be easier and more fun to write my own library from scratch. That is exactly what I've done." + Environment.NewLine + Environment.NewLine +
            "In fact it's turned out to be well worth it. It has only taken me a few days to write the library, and already it has quite a few really cool features which I will be using in my game.";

        String usingQuickFontIsSuperEasy = @"Using QuickFont is super easy. To load a font: ";
        String loadingAFont1 = "myFont = new QFont(\"HappySans.ttf\", 16);";
        String andPrintWithIt = @"...and to print with it: ";
        String printWithFont1 = "QFont.Begin();" + Environment.NewLine + "myFont.Print(\"Hello World!\")" + Environment.NewLine + "QFont.End();";
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
        #endregion




        int currentDemoPage = 1;
        int lastPage = 9;

        QFontAlignment cycleAlignment = QFontAlignment.Left;

        /// <summary>Creates a 800x600 window with the specified title.</summary>
        public Game()
            : base(800, 600, GraphicsMode.Default, "OpenTK Quick Start Sample")
        {
            VSync = VSyncMode.On;
            this.WindowBorder = WindowBorder.Fixed;
        }





        private void KeyDown(object sender, KeyboardKeyEventArgs keyEventArgs)
        {
  
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

                    }
                    break;

                case Key.Up:
                    {
                        if (currentDemoPage == 4)
                        {
                            if(cycleAlignment == QFontAlignment.Justify)
                                cycleAlignment = QFontAlignment.Left;
                            else 
                                cycleAlignment++;    
                        }


                    }
                    break;


                case Key.Down:
                    {
                        if (currentDemoPage == 4)
                        {
                            if(cycleAlignment == QFontAlignment.Left)
                                cycleAlignment = QFontAlignment.Justify;
                            else 
                                cycleAlignment--;    
                        }


                    }
                    break;
                case Key.F9:
                    
                    break;

            }

            if (currentDemoPage > lastPage)
                currentDemoPage = lastPage;

            if (currentDemoPage < 1)
                currentDemoPage = 1;

        }



        /// <summary>Load resources here.</summary>
        /// <param name="e">Not used.</param>
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            this.Keyboard.KeyDown += KeyDown;


            /*
            QFontBuilderConfiguration config = new QFontBuilderConfiguration();
            config.PageWidth = 1024;
            config.PageHeight = 1024;
            config.GlyphMargin = 6;
            QFont.CreateTextureFontFiles("BURNSTOW.TTF", 120, config, "metalFont");
            */


            heading2 = QFont.FromQFontFile("woodenFont.qfont", 1.0f, new QFontLoaderConfiguration(true));

            var builderConfig = new QFontBuilderConfiguration(true);
            builderConfig.ShadowConfig.blurRadius = 1; //reduce blur radius because font is very small
            builderConfig.TextGenerationRenderHint = TextGenerationRenderHint.ClearTypeGridFit; //best render hint for this font
            mainText = new QFont("Fonts/times.ttf", 14, builderConfig);

          
            heading1 = new QFont("Fonts/HappySans.ttf", 72, new QFontBuilderConfiguration(true));
          

            controlsText = new QFont("Fonts/HappySans.ttf", 32, new QFontBuilderConfiguration(true));


            codeText = new QFont("Fonts/Comfortaa-Regular.ttf", 12,FontStyle.Regular);

            heading1.Options.Colour = new Color4(0.2f, 0.2f, 0.2f, 1.0f);
            mainText.Options.Colour = new Color4(0.1f, 0.1f, 0.1f, 1.0f);
            mainText.Options.DropShadowActive = false;
            codeText.Options.Colour = new Color4(0.0f, 0.0f, 0.4f, 1.0f);

            QFontBuilderConfiguration config2 = new QFontBuilderConfiguration();
            config2.SuperSampleLevels = 1;
         //   font = new QFont("Fonts/times.ttf", 16,config2);
         //   font.Options.Colour = new Color4(0.1f, 0.1f, 0.1f, 1.0f);
         //   font.Options.CharacterSpacing = 0.1f;


            monoSpaced = new QFont("Fonts/Anonymous.ttf", 10);
            monoSpaced.Options.Colour = new Color4(0.1f, 0.1f, 0.1f, 1.0f);

            Console.WriteLine(" Monospaced : " + monoSpaced.IsMonospacingActive);


            GL.ClearColor(1.0f, 1.0f, 1.0f, 0.0f);
            GL.Disable(EnableCap.DepthTest);

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

            Matrix4 projection = Matrix4.CreatePerspectiveFieldOfView((float)Math.PI / 4, Width / (float)Height, 1.0f, 64.0f);
            GL.MatrixMode(MatrixMode.Projection);
            GL.LoadMatrix(ref projection);
        }

        double cnt;
        double boundsAnimationCnt = 1.0f;

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

            GL.Disable(EnableCap.Texture2D);
            GL.Color4(1.0f, 0f, 0f, 1.0f);


            float maxWidth = bounds.Width;

            float height = font.Measure(text, maxWidth, alignment).Height;

            GL.Begin(BeginMode.LineLoop);
                GL.Vertex3(bounds.X, bounds.Y, 0f);
                GL.Vertex3(bounds.X + bounds.Width, bounds.Y, 0f);
                GL.Vertex3(bounds.X + bounds.Width, bounds.Y + height, 0f);
                GL.Vertex3(bounds.X, bounds.Y + height, 0f);
            GL.End();

            font.Print(text, maxWidth, alignment, new Vector2(bounds.X,bounds.Y));

            yOffset += height;

        }

        
       //some helpers



        private void PrintComment(string comment, ref float yOffset)
        {
            PrintComment(mainText, comment, QFontAlignment.Justify, ref yOffset);
        }

        private void PrintComment(QFont font, string comment,QFontAlignment alignment, ref float yOffset){


            GL.PushMatrix();
                yOffset += 20;
                GL.Translate(30f, yOffset, 0f);
                font.Print(comment, Width - 60, alignment);
                yOffset += font.Measure(comment, Width - 60, alignment).Height;
            GL.PopMatrix();

        }



        private void PrintCommentWithLine(string comment, QFontAlignment alignment, float xOffset, ref float yOffset)
        {
            PrintCommentWithLine(mainText, comment, alignment, xOffset, ref yOffset);
        }

        private void PrintCommentWithLine(QFont font, string comment, QFontAlignment alignment, float xOffset, ref float yOffset)
        {


            GL.PushMatrix();
            yOffset += 20;
            GL.Translate((int)xOffset, yOffset, 0f);
            font.Print(comment, alignment);
            var bounds = font.Measure(comment, Width-60, alignment);


            GL.Disable(EnableCap.Texture2D);
            GL.Begin(BeginMode.Lines);
                GL.Color4(1.0f, 0f, 0f, 1f); GL.Vertex2(0f, 0f);
                GL.Color4(1.0f, 0f, 0f, 1f); GL.Vertex2(0f, bounds.Height + 20f);
            GL.End();

            yOffset += bounds.Height;

            GL.PopMatrix();

        }

        
        private void PrintCode(string code, ref float yOffset){

            GL.PushMatrix();
                yOffset += 20;
                GL.Translate(50f, yOffset, 0f);
                codeText.Print(code, Width - 50, QFontAlignment.Left);
                yOffset += codeText.Measure(code, Width - 50, QFontAlignment.Left).Height;
            GL.PopMatrix();

        }



        /// <summary>
        /// Called when it is time to render the next frame. Add your rendering code here.
        /// </summary>
        /// <param name="e">Contains timing information.</param>
        protected override void OnRenderFrame(FrameEventArgs e)
        {
            base.OnRenderFrame(e);

            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            Matrix4 modelview = Matrix4.LookAt(Vector3.Zero, Vector3.UnitZ, Vector3.UnitY);
            GL.MatrixMode(MatrixMode.Modelview);
            GL.LoadMatrix(ref modelview);


            QFont.Begin();
            GL.Begin(BeginMode.Quads);
 

            GL.Color3(1.0f, 1.0f, 1.0); GL.Vertex2(0, 0);
            GL.Color3(0.9f, 0.9f, 0.9f); GL.Vertex2(0, Height);
            GL.Color3(0.9f, 0.9f, 0.9f); GL.Vertex2(Width, Height);
            GL.Color3(0.9f, 0.9f, 0.9f); GL.Vertex2(Width, 0);
                
     
  
            GL.End();
            QFont.End();



            switch (currentDemoPage)
            {

                case 1:
                    {
                        float yOffset = 0;

                        QFont.Begin();

                            GL.PushMatrix();
                                GL.Translate(Width * 0.5f, yOffset, 0f);
                                heading1.Print("QuickFont", QFontAlignment.Centre);
                                yOffset += heading1.Measure("QuickFont").Height;
                            GL.PopMatrix();


                            GL.PushMatrix();
                                GL.Translate(20f, yOffset, 0f);
                                heading2.Print("Introduction", QFontAlignment.Left);
                                yOffset += heading2.Measure("Introduction").Height;
                            GL.PopMatrix();


                            GL.PushMatrix();
                                GL.Translate(30f, yOffset + 20, 0f);
                                mainText.Print(introduction, Width - 60, QFontAlignment.Justify);
                            GL.PopMatrix();


                        QFont.End();
                        
                    }
                    break;




                case 2:
                    {

                        float yOffset = 20;

                        QFont.Begin();


                            GL.PushMatrix();
                                GL.Translate(20f, yOffset, 0f);
                                heading2.Print("Easy as ABC!", QFontAlignment.Left);
                                yOffset += heading2.Measure("Easy as ABC!").Height;
                            GL.PopMatrix();


                            PrintComment(usingQuickFontIsSuperEasy, ref yOffset);
                            PrintCode(loadingAFont1, ref yOffset);

                            PrintComment(andPrintWithIt, ref yOffset);
                            PrintCode(printWithFont1, ref yOffset);

                            PrintComment(itIsAlsoEasyToMeasure, ref yOffset);
                            PrintCode(measureText1, ref yOffset);

                            PrintComment(oneOfTheFirstGotchas, ref yOffset);
                            PrintCode(loadingAFont2, ref yOffset);

                        QFont.End();
           
                    }
                    break;

                case 3:
                    {

                        float yOffset = 20;

                        QFont.Begin();


                        GL.PushMatrix();
                        GL.Translate(20f, yOffset, 0f);
                        heading2.Print("Alignment", QFontAlignment.Left);
                        yOffset += heading2.Measure("Easy as ABC!").Height;
                        GL.PopMatrix();

                        PrintCommentWithLine(whenPrintingText, QFontAlignment.Left, Width * 0.5f, ref yOffset);
                        PrintCode(printWithFont2, ref yOffset);


                        PrintCommentWithLine(righAlignedText, QFontAlignment.Right, Width * 0.5f, ref yOffset);
                        yOffset += 10f;

                        PrintCommentWithLine(centredTextAsYou, QFontAlignment.Centre, Width * 0.5f, ref yOffset);
   
                        QFont.End();

                    }
                    break;



                case 4:
                    {

                        float yOffset = 20;

                        QFont.Begin();


                        GL.PushMatrix();
                        GL.Translate(20f, yOffset, 0f);
                        heading2.Print("Bounds and Justify", QFontAlignment.Left);
                        yOffset += heading2.Measure("Easy as ABC!").Height;
                        GL.PopMatrix();



                        GL.PushMatrix();
                        yOffset += 20;
                        GL.Translate((int)(Width * 0.5), yOffset, 0f);
                        controlsText.Print("Press [Up], [Down] or [Enter]!", QFontAlignment.Centre);
                        yOffset += controlsText.Measure("[]").Height;
                        GL.PopMatrix();


            
                        float boundShrink = (int) (350* (1- Math.Cos(boundsAnimationCnt * Math.PI * 2)));

                        yOffset += 15; ;
                        PrintWithBounds(mainText, ofCourseItsNot, new RectangleF(30f + boundShrink*0.5f, yOffset, Width - 60 - boundShrink, 350f), cycleAlignment, ref yOffset);


                        string printWithBounds = "myFont.Print(text,400f,QFontAlignment." + cycleAlignment + ");";
                        yOffset += 15f;
                        PrintCode(printWithBounds, ref yOffset);



                        QFont.End();

                    }
                    break;






                case 5:
                    {

                        float yOffset = 20;

                        QFont.Begin();


                        GL.PushMatrix();
                        GL.Translate(20f, yOffset, 0f);
                        heading2.Print("Your own Texture Fonts", QFontAlignment.Left);
                        yOffset += heading2.Measure("T").Height;
                        GL.PopMatrix();


                        PrintComment(anotherCoolFeature, ref yOffset);
                        PrintCode(textureFontCode1, ref yOffset);
                        PrintComment(thisWillHaveCreated, ref yOffset);
                
                        

                        QFont.End();

                    }
                    break;


                case 6:
                    {

                        float yOffset = 20;

                        QFont.Begin();


                        GL.PushMatrix();
                        GL.Translate(20f, yOffset, 0f);
                        heading2.Print("Your own Texture Fonts", QFontAlignment.Left);
                        yOffset += heading2.Measure("T").Height;
                        GL.PopMatrix();


                        PrintComment(ifYouDoIntend, ref yOffset);
                        PrintCode(textureFontCode2, ref yOffset);
                        PrintComment(actuallyTexturing, ref yOffset);
                        PrintCode(textureFontCode3, ref yOffset);


                        QFont.End();

                    }
                    break;



                case 7:
                    {

                        float yOffset = 20;

                        QFont.Begin();


                        heading2.Options.DropShadowOffset = new Vector2(0.1f + 0.2f * (float)Math.Sin(cnt), 0.1f + 0.2f * (float)Math.Cos(cnt));

                        GL.PushMatrix();
                        GL.Translate(20f, yOffset, 0f);
                        heading2.Print("Drop Shadows", QFontAlignment.Left);
                        yOffset += heading2.Measure("T").Height;
                        GL.PopMatrix();

                        heading2.Options.DropShadowOffset = new Vector2(0.16f, 0.16f); //back to default

                        mainText.Options.DropShadowActive = true;
                        mainText.Options.DropShadowOpacity = 0.7f;
                        mainText.Options.DropShadowOffset = new Vector2(0.1f + 0.2f * (float)Math.Sin(cnt), 0.1f + 0.2f * (float)Math.Cos(cnt));



                        PrintComment(asIhaveleant, ref yOffset);
                        PrintCode(dropShadowCode1, ref yOffset);
                        PrintComment(thisWorksFine, ref yOffset);
                        PrintCode(dropShadowCode2, ref yOffset);
                        PrintComment(onceAFont, ref yOffset);


                        mainText.Options.DropShadowActive = false;

                        QFont.End();

                    }
                    break;




                case 8:
                    {

                        float yOffset = 20;

                        QFont.Begin();



                        monoSpaced.Options.CharacterSpacing = 0.05f;

                        GL.PushMatrix();
                        GL.Translate(20f, yOffset, 0f);
                        heading2.Print("Monospaced Fonts", QFontAlignment.Left);
                        yOffset += heading2.Measure("T").Height;
                        GL.PopMatrix();


                        PrintComment(monoSpaced, hereIsSomeMono, QFontAlignment.Left, ref yOffset);
                        PrintCode(monoCode1, ref yOffset);
                        PrintComment(monoSpaced, theDefaultMono, QFontAlignment.Left, ref yOffset);
   
                        PrintCommentWithLine(monoSpaced, mono, QFontAlignment.Left, Width * 0.5f, ref yOffset);
                        yOffset += 2f;
                        PrintCommentWithLine(monoSpaced, mono, QFontAlignment.Right, Width * 0.5f, ref yOffset);
                        yOffset += 2f;
                        PrintCommentWithLine(monoSpaced, mono, QFontAlignment.Centre, Width * 0.5f, ref yOffset);
                        yOffset += 2f;

                        monoSpaced.Options.CharacterSpacing = 0.5f;
                        PrintComment(monoSpaced, "As usual, you can adjust character spacing with myFont.Options.CharacterSpacing.", QFontAlignment.Left, ref yOffset);


                        QFont.End();

                    }
                    break;


                case 9:
                    {

                        float yOffset = 20;

                        QFont.Begin();


                        GL.PushMatrix();
                        GL.Translate(20f, yOffset, 0f);
                        heading2.Print("In Conclusion", QFontAlignment.Left);
                        yOffset += heading2.Measure("T").Height;
                        GL.PopMatrix();


                        PrintComment(thereAreActually, ref yOffset);
        

      

                        QFont.End();

                    }
                    break;



            }




            QFont.Begin();
            

            if (currentDemoPage != lastPage)
            {
                GL.PushMatrix();
                GL.Translate(Width - 10 - 16 * (float)(1 + Math.Sin(cnt * 4)), Height - controlsText.Measure("P").Height - 10f, 0f);
                controlsText.Options.Colour = new Color4(0.8f, 0.1f, 0.1f, 1.0f);
                controlsText.Print("Press [Right] ->", QFontAlignment.Right);
                GL.PopMatrix();
            }


            if (currentDemoPage != 1)
            {

                GL.PushMatrix();
                GL.Translate(10 + 16 * (float)(1 + Math.Sin(cnt * 4)), Height - controlsText.Measure("P").Height - 10f, 0f);
                controlsText.Options.Colour = new Color4(0.8f, 0.1f, 0.1f, 1.0f);
                controlsText.Print("<- Press [Left]", QFontAlignment.Left);
                GL.PopMatrix();
            }

            
            QFont.End();



            GL.Disable(EnableCap.Texture2D);

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