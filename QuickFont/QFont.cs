using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Drawing.Text;
using System.Drawing.Imaging;
using System.Linq;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using OpenTK.Graphics;

namespace QuickFont
{
    public class QFont
    {
        //private QFontRenderOptions options = new QFontRenderOptions();
        private Stack<QFontRenderOptions> optionsStack = new Stack<QFontRenderOptions>();
        internal QFontData fontData;
        
        bool UsingVertexBuffers;
        public QVertexBuffer[] VertexBuffers = new QVertexBuffer[0];
        Vector3 PrintOffset;
        
        
        public QFontRenderOptions Options
        {
            get {

                if (optionsStack.Count == 0)
                {
                    optionsStack.Push(new QFontRenderOptions());
                }

                return optionsStack.Peek() ; 
            }
            private set { //not sure if we should even allow this...
                optionsStack.Pop();
                optionsStack.Push(value);
            }
        }


        #region Constructors and font builders

        private QFont() { }
        internal QFont(QFontData fontData) { this.fontData = fontData; }
        public QFont(Font font) : this(font, null) { }
        public QFont(Font font, QFontBuilderConfiguration config)
        {

            optionsStack.Push(new QFontRenderOptions());

            if (config == null)
                config = new QFontBuilderConfiguration();

            fontData = BuildFont(font, config, null);

            if (config.ShadowConfig != null)
                Options.DropShadowActive = true;

            if (config.UseVertexBuffer) 
                InitVBOs();
        }




        public QFont(string fileName, float size) : this(fileName, size, FontStyle.Regular, null) { }
        public QFont(string fileName, float size, FontStyle style) : this(fileName, size, style, null) { }
        public QFont(string fileName, float size, QFontBuilderConfiguration config) : this(fileName, size, FontStyle.Regular, config) { }
        public QFont(string fileName, float size, FontStyle style, QFontBuilderConfiguration config)
        {
            PrivateFontCollection pfc = new PrivateFontCollection();
            pfc.AddFontFile(fileName);
            var fontFamily = pfc.Families[0];

            if (!fontFamily.IsStyleAvailable(style))
                throw new ArgumentException("Font file: " + fileName + " does not support style: " +  style );

            if (config == null)
                config = new QFontBuilderConfiguration();

            TransformViewport? transToVp = null;
            float fontScale = 1f;
            if (config.TransformToCurrentOrthogProjection)
                transToVp = OrthogonalTransform(out fontScale);

            using(var font = new Font(fontFamily, size * fontScale * config.SuperSampleLevels, style)){
                fontData = BuildFont(font, config, null);
            }

            if (config.ShadowConfig != null)
                Options.DropShadowActive = true;
            if (transToVp != null)
                Options.TransformToViewport = transToVp;

            if(config.UseVertexBuffer)
                InitVBOs();
        }


        public static void CreateTextureFontFiles(Font font, string newFontName) { CreateTextureFontFiles(font, null); }
        public static void CreateTextureFontFiles(Font font, string newFontName, QFontBuilderConfiguration config)
        {
            var fontData = BuildFont(font, config, newFontName);
            Builder.SaveQFontDataToFile(fontData, newFontName);
        }

        



        public static void CreateTextureFontFiles(string fileName, float size, string newFontName) { CreateTextureFontFiles(fileName, size, FontStyle.Regular, null, newFontName); }
        public static void CreateTextureFontFiles(string fileName, float size, FontStyle style, string newFontName) { CreateTextureFontFiles(fileName, size, style, null, newFontName); }
        public static void CreateTextureFontFiles(string fileName, float size, QFontBuilderConfiguration config, string newFontName) { CreateTextureFontFiles(fileName, size, FontStyle.Regular, config, newFontName); }
        public static void CreateTextureFontFiles(string fileName, float size, FontStyle style, QFontBuilderConfiguration config, string newFontName)
        {
            PrivateFontCollection pfc = new PrivateFontCollection();
            pfc.AddFontFile(fileName);
            var fontFamily = pfc.Families[0];

            if (!fontFamily.IsStyleAvailable(style))
                throw new ArgumentException("Font file: " + fileName + " does not support style: " + style);

            QFontData fontData = null;
            if (config == null)
                config = new QFontBuilderConfiguration();


            using(var font = new Font(fontFamily, size * config.SuperSampleLevels, style)){
                fontData  = BuildFont(font, config, newFontName);
            }

            Builder.SaveQFontDataToFile(fontData, newFontName);
            
        }

        public static QFont FromQFontFile(string filePath) { return FromQFontFile(filePath, 1.0f, null); }
        public static QFont FromQFontFile(string filePath, QFontLoaderConfiguration loaderConfig) { return FromQFontFile(filePath, 1.0f, loaderConfig); }
        public static QFont FromQFontFile(string filePath, float downSampleFactor) { return FromQFontFile(filePath, downSampleFactor,null); }
        public static QFont FromQFontFile(string filePath, float downSampleFactor, QFontLoaderConfiguration loaderConfig)
        {



            if (loaderConfig == null)
                loaderConfig = new QFontLoaderConfiguration();

            TransformViewport? transToVp = null;
            float fontScale = 1f;
            if (loaderConfig.TransformToCurrentOrthogProjection)
                transToVp = OrthogonalTransform(out fontScale);
          
            QFont qfont = new QFont();
            qfont.fontData = Builder.LoadQFontDataFromFile(filePath, downSampleFactor * fontScale, loaderConfig);

            if (loaderConfig.ShadowConfig != null)
                qfont.Options.DropShadowActive = true;
            if (transToVp != null)
                qfont.Options.TransformToViewport = transToVp;

            return qfont;
        }
  
        private static QFontData BuildFont(Font font, QFontBuilderConfiguration config, string saveName){

            Builder builder = new Builder(font, config);
            return builder.BuildFontData(saveName);
        }



        #endregion




        /// <summary>
        /// When TransformToOrthogProjection is enabled, we need to get the current orthogonal transformation,
        /// the font scale, and ensure that the projection is actually orthogonal
        /// </summary>
        /// <param name="fontScale"></param>
        /// <param name="viewportTransform"></param>
        private static TransformViewport OrthogonalTransform(out float fontScale)
        {
            bool isOrthog;
            float left,right,bottom,top;
            ProjectionStack.GetCurrentOrthogProjection(out isOrthog,out left,out right,out bottom,out top);

            if (!isOrthog)
                throw new ArgumentOutOfRangeException("Current projection matrix was not Orthogonal. Please ensure that you have set an orthogonal projection before attempting to create a font with the TransformToOrthogProjection flag set to true.");
            
            var viewportTransform = new TransformViewport(left, top, right - left, bottom - top);
            fontScale = Math.Abs((float)ProjectionStack.CurrentViewport.Value.Height / viewportTransform.Height);
            return viewportTransform;
        }





        /// <summary>
        /// Pushes the specified QFont options onto the options stack
        /// </summary>
        /// <param name="newOptions"></param>
        public void PushOptions(QFontRenderOptions newOptions)
        {
            optionsStack.Push(newOptions);
        }

        /// <summary>
        /// Creates a clone of the current font options and pushes
        /// it onto the stack
        /// </summary>
        public void PushOptions()
        {
            PushOptions(Options.CreateClone());
        }

        public void PopOptions()
        {
            if (optionsStack.Count > 1)
            {
                optionsStack.Pop();
            }
            else
            {
                throw new Exception("Attempted to pop from options stack when there is only one Options object on the stack.");
            }
        }



        public float LineSpacing
        {
            get { return (float)Math.Ceiling(fontData.maxGlyphHeight * Options.LineSpacing); }
        }

        public bool IsMonospacingActive
        {
            get { return fontData.IsMonospacingActive(Options); }
        }


        public float MonoSpaceWidth
        {
            get { return fontData.GetMonoSpaceWidth(Options); }
        }



        private void RenderDropShadow(float x, float y, char c, QFontGlyph nonShadowGlyph)
        {
            //note can cast drop shadow offset to int, but then you can't move the shadow smoothly...
            if (fontData.dropShadow != null && Options.DropShadowActive)
            {
                //make sure fontdata font's options are synced with the actual options
                if (fontData.dropShadow.Options != Options)
                    fontData.dropShadow.Options = Options;

                fontData.dropShadow.RenderGlyph(
                    x + (fontData.meanGlyphWidth * Options.DropShadowOffset.X + nonShadowGlyph.rect.Width * 0.5f),
                    y + (fontData.meanGlyphWidth * Options.DropShadowOffset.Y + nonShadowGlyph.rect.Height * 0.5f + nonShadowGlyph.yOffset), c, true);
            }
        }

        public void RenderGlyph(float x, float y, char c, bool isDropShadow)
        {
            var glyph = fontData.CharSetMapping[c];

            //note: it's not immediately obvious, but this combined with the paramteters to 
            //RenderGlyph for the shadow mean that we render the shadow centrally (despite it being a different size)
            //under the glyph
            if (isDropShadow) 
            {
                x -= (int)(glyph.rect.Width * 0.5f);
                y -= (int)(glyph.rect.Height * 0.5f + glyph.yOffset);
            }
            
            RenderDropShadow(x, y, c, glyph);

            TexturePage sheet = fontData.Pages[glyph.page];

            float tx1 = (float)(glyph.rect.X) / sheet.Width;
            float ty1 = (float)(glyph.rect.Y) / sheet.Height;
            float tx2 = (float)(glyph.rect.X + glyph.rect.Width) / sheet.Width;
            float ty2 = (float)(glyph.rect.Y + glyph.rect.Height) / sheet.Height;

            var tv1 = new Vector2(tx1, ty1);
            var tv2 = new Vector2(tx1, ty2);
            var tv3 = new Vector2(tx2, ty2);
            var tv4 = new Vector2(tx2, ty1);

            var v1 = PrintOffset + new Vector3(x, y + glyph.yOffset, 0);
            var v2 = PrintOffset + new Vector3(x, y + glyph.yOffset + glyph.rect.Height, 0);
            var v3 = PrintOffset + new Vector3(x + glyph.rect.Width, y + glyph.yOffset + glyph.rect.Height, 0);
            var v4 = PrintOffset + new Vector3(x + glyph.rect.Width, y + glyph.yOffset, 0);

            Color color = Options.Colour;
            if(isDropShadow)
                color = Color.FromArgb((int)(Options.DropShadowOpacity * 255f), Color.White);

            if (UsingVertexBuffers)
            {
                var normal = new Vector3(0, 0, -1);

                int argb = Helper.ToRgba(color);

                var vbo = VertexBuffers[glyph.page];

                vbo.AddVertex(v1, normal, tv1, argb);
                vbo.AddVertex(v2, normal, tv2, argb);
                vbo.AddVertex(v3, normal, tv3, argb);

                vbo.AddVertex(v1, normal, tv1, argb);
                vbo.AddVertex(v3, normal, tv3, argb);
                vbo.AddVertex(v4, normal, tv4, argb);
            }

            // else use immediate mode
            else
            {
                GL.Color4(color);
                GL.BindTexture(TextureTarget.Texture2D, sheet.GLTexID);

                GL.Begin(BeginMode.Quads);
                GL.TexCoord2(tv1); GL.Vertex3(v1);
                GL.TexCoord2(tv2); GL.Vertex3(v2);
                GL.TexCoord2(tv3); GL.Vertex3(v3);
                GL.TexCoord2(tv4); GL.Vertex3(v4);
                GL.End();
            }
        }


        


        private float MeasureNextlineLength(string text)
        {

            float xOffset = 0;
            
            for(int i=0; i < text.Length;i++)
            {
                char c = text[i];

                if (c == '\r' || c == '\n')
                {
                    break;
                }


                if (IsMonospacingActive)
                {
                    xOffset += MonoSpaceWidth;
                }
                else
                {
                    //space
                    if (c == ' ')
                    {
                        xOffset += (float)Math.Ceiling(fontData.meanGlyphWidth * Options.WordSpacing);
                    }
                    //normal character
                    else if (fontData.CharSetMapping.ContainsKey(c))
                    {
                        QFontGlyph glyph = fontData.CharSetMapping[c];
                        xOffset += (float)Math.Ceiling(glyph.rect.Width + fontData.meanGlyphWidth * Options.CharacterSpacing + fontData.GetKerningPairCorrection(i, text, null));
                    }
                }
            }
            return xOffset;
        }


        private Vector2 TransformPositionToViewport(Vector2 input)
        {

            
            var v2 = Options.TransformToViewport;
            if (v2 == null)
            {
                return input;
            }
            var v1 = ProjectionStack.CurrentViewport;

            float X, Y;

            X = (input.X - v2.Value.X) * ((float)v1.Value.Width / v2.Value.Width);
            Y = (input.Y - v2.Value.Y) * ((float)v1.Value.Height / v2.Value.Height);

            return new Vector2(X, Y);
        }

        private float TransformWidthToViewport(float input)
        {
            
            var v2 = Options.TransformToViewport;
            if (v2 == null)
            {
                return input;
            }
            var v1 = ProjectionStack.CurrentViewport;

            return input * ((float)v1.Value.Width / v2.Value.Width);
        }

        private SizeF TransformMeasureFromViewport(SizeF input)
        {
            
            var v2 = Options.TransformToViewport;
            if (v2 == null)
            {
                return input;
            }
            var v1 = ProjectionStack.CurrentViewport;

            float X, Y;

            X = input.Width * ((float)v2.Value.Width / v1.Value.Width);
            Y = input.Height * ((float)v2.Value.Height / v1.Value.Height);

            return new SizeF(X, Y);
        }

        private Vector2 LockToPixel(Vector2 input)
        {
            if (Options.LockToPixel)
            {
                float r = Options.LockToPixelRatio;
                return new Vector2((1 - r) * input.X + r * ((int)Math.Round(input.X)), (1 - r) * input.Y + r * ((int)Math.Round(input.Y)));
            }
            return input;
        }



        public void Print(ProcessedText processedText, Vector2 position)
        {
            position = TransformPositionToViewport(position);
            position = LockToPixel(position);

            GL.PushMatrix();
            GL.Translate(position.X,position.Y,0f);
            Print(processedText);
            GL.PopMatrix();
        }

        public void Print(string text, SizeF maxSize, QFontAlignment alignment, Vector2 position)
        {
            position = TransformPositionToViewport(position);
            position = LockToPixel(position);
            
            GL.PushMatrix();
            GL.Translate(position.X, position.Y, 0f);
            Print(text, maxSize, alignment);
            GL.PopMatrix();
        }

        public void Print(string text, Vector2 position, QFontAlignment alignment = QFontAlignment.Left)
        {
            position = TransformPositionToViewport(position);
            position = LockToPixel(position);

            GL.PushMatrix();
            GL.Translate(position.X, position.Y, 0f);
            Print(text, alignment);
            GL.PopMatrix();
        }

        public void Print(string text, QFontAlignment alignment = QFontAlignment.Left)
        {
            PrintOrMeasure(text, alignment, false);
        }

        public void PrintToVBO(string text, Vector3 position, Color color, QFontAlignment alignment = QFontAlignment.Left)
        {
            Options.Colour = color;
            PrintOffset = position;
            PrintOrMeasure(text, alignment, false);
        }

        public void PrintToVBO(string text, QFontAlignment alignment, Vector3 position, Color color, SizeF maxSize)
        {
            Options.Colour = color;
            PrintOffset = position;
            Print(text, maxSize, alignment);
        }

        public SizeF Measure(string text, QFontAlignment alignment = QFontAlignment.Left)
        {
            return TransformMeasureFromViewport(PrintOrMeasure(text, alignment, true));
        }

        /// <summary>
        /// Measures the actual width and height of the block of text.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="bounds"></param>
        /// <param name="alignment"></param>
        /// <returns></returns>
        public SizeF Measure(string text, SizeF maxSize, QFontAlignment alignment)
        {
            var processedText = ProcessText(text, maxSize, alignment);
            return Measure(processedText);
        }

        /// <summary>
        /// Measures the actual width and height of the block of text
        /// </summary>
        /// <param name="processedText"></param>
        /// <returns></returns>
        public SizeF Measure(ProcessedText processedText)
        {
            return TransformMeasureFromViewport(PrintOrMeasure(processedText, true));
        }


        private SizeF PrintOrMeasure(string text, QFontAlignment alignment, bool measureOnly)
        {
            float maxWidth = 0f;
            float xOffset = 0f;
            float yOffset = 0f;

            var caps = new EnableCap[] { };
            if(!UsingVertexBuffers)
                caps = new EnableCap[] { EnableCap.Texture2D, EnableCap.Blend };

            Helper.SafeGLEnable(caps, () =>
            {
                float maxXpos = float.MinValue;
                float minXPos = float.MaxValue;

                if (!UsingVertexBuffers)
                {
                    GL.Color4(1.0f, 1.0f, 1.0f, 1.0f);

                    if (Options.UseDefaultBlendFunction)
                        GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);
                }
                
                text = text.Replace("\r\n", "\r");

                if (alignment == QFontAlignment.Right)
                    xOffset -= MeasureNextlineLength(text);
                else if (alignment == QFontAlignment.Centre)
                    xOffset -= (int)(0.5f * MeasureNextlineLength(text));

                for (int i = 0; i < text.Length; i++)
                {
                    char c = text[i];


                    //newline
                    if (c == '\r' || c == '\n')
                    {
                        yOffset += LineSpacing;
                        xOffset = 0f;

                        if (alignment == QFontAlignment.Right)
                            xOffset -= MeasureNextlineLength(text.Substring(i + 1));
                        else if (alignment == QFontAlignment.Centre)
                            xOffset -= (int)(0.5f * MeasureNextlineLength(text.Substring(i + 1)));

                    }
                    else
                    {

                        minXPos = Math.Min(xOffset, minXPos);

                        //normal character
                        if (c != ' ' && fontData.CharSetMapping.ContainsKey(c))
                        {
                            QFontGlyph glyph = fontData.CharSetMapping[c];
                            if (!measureOnly)
                                RenderGlyph(xOffset, yOffset, c, false);
                        }


                        if (IsMonospacingActive)
                            xOffset += MonoSpaceWidth;
                        else
                        {
                            if (c == ' ')
                                xOffset += (float)Math.Ceiling(fontData.meanGlyphWidth * Options.WordSpacing);
                            //normal character
                            else if (fontData.CharSetMapping.ContainsKey(c))
                            {
                                QFontGlyph glyph = fontData.CharSetMapping[c];
                                xOffset += (float)Math.Ceiling(glyph.rect.Width + fontData.meanGlyphWidth * Options.CharacterSpacing + fontData.GetKerningPairCorrection(i, text, null));
                            }
                        }

                        maxXpos = Math.Max(xOffset, maxXpos);
                    }
                }

                if (minXPos != float.MaxValue)
                    maxWidth = maxXpos - minXPos;
            });

            return new SizeF(maxWidth, yOffset + LineSpacing);
        }








        private void RenderWord(float x, float y, TextNode node)
        {

            if (node.Type != TextNodeType.Word)
                return;

            int charGaps = node.Text.Length - 1;
            bool isCrumbleWord = CrumbledWord(node);
            if (isCrumbleWord)
                charGaps++;

            int pixelsPerGap = 0;
            int leftOverPixels = 0;

            if (charGaps != 0)
            {
                pixelsPerGap = (int)node.LengthTweak / charGaps;
                leftOverPixels = (int)node.LengthTweak - pixelsPerGap * charGaps;
            }

            for(int i = 0; i < node.Text.Length; i++){
                char c = node.Text[i];
                if(fontData.CharSetMapping.ContainsKey(c)){
                    var glyph = fontData.CharSetMapping[c];

                    RenderGlyph(x,y,c, false);


                    if (IsMonospacingActive)
                        x += MonoSpaceWidth;
                    else
                        x += (int)Math.Ceiling(glyph.rect.Width + fontData.meanGlyphWidth * Options.CharacterSpacing + fontData.GetKerningPairCorrection(i, node.Text, node));

                    x += pixelsPerGap;
                    if (leftOverPixels > 0)
                    {
                        x += 1.0f;
                        leftOverPixels--;
                    }
                    else if (leftOverPixels < 0)
                    {
                        x -= 1.0f;
                        leftOverPixels++;
                    }


                }
            }
        }






        /// <summary>
        /// Computes the length of the next line, and whether the line is valid for
        /// justification.
        /// </summary>
        /// <param name="node"></param>
        /// <param name="maxLength"></param>
        /// <param name="justifable"></param>
        /// <returns></returns>
        private float TextNodeLineLength(TextNode node, float maxLength)
        {

            if (node == null)
                return 0;

            bool atLeastOneNodeCosumedOnLine = false;
            float length = 0;
            for (; node != null; node = node.Next)
            {

                if (node.Type == TextNodeType.LineBreak)
                    break;

                if (SkipTrailingSpace(node, length, maxLength) && atLeastOneNodeCosumedOnLine)
                    break;

                if (length + node.Length <= maxLength || !atLeastOneNodeCosumedOnLine)
                {
                    atLeastOneNodeCosumedOnLine = true;
                    length += node.Length;
                }
                else
                {
                    break;
                }


            }
            return length;
        }


        private bool CrumbledWord(TextNode node)
        {
            return (node.Type == TextNodeType.Word && node.Next != null && node.Next.Type == TextNodeType.Word);  
        }


        /// <summary>
        /// Computes the length of the next line, and whether the line is valid for
        /// justification.
        /// </summary>
        private void JustifyLine(TextNode node, float targetLength)
        {
  
            bool justifiable = false;

            if (node == null)
                return;

            var headNode = node; //keep track of the head node


            //start by finding the length of the block of text that we know will actually fit:

            int charGaps = 0;
            int spaceGaps = 0;

            bool atLeastOneNodeCosumedOnLine = false;
            float length = 0;
            var expandEndNode = node; //the node at the end of the smaller list (before adding additional word)
            for (; node != null; node = node.Next)
            {

                

                if (node.Type == TextNodeType.LineBreak)
                    break;

                if (SkipTrailingSpace(node, length, targetLength) && atLeastOneNodeCosumedOnLine)
                {
                    justifiable = true;
                    break;
                }

                if (length + node.Length < targetLength || !atLeastOneNodeCosumedOnLine)
                {

                    expandEndNode = node;

                    if (node.Type == TextNodeType.Space)
                        spaceGaps++;

                    if (node.Type == TextNodeType.Word)
                    {
                        charGaps += (node.Text.Length - 1);

                        //word was part of a crumbled word, so there's an extra char cap between the two words
                        if (CrumbledWord(node))
                            charGaps++;

                    }

                    atLeastOneNodeCosumedOnLine = true;
                    length += node.Length;
                }
                else
                {
                    justifiable = true;
                    break;
                }

                

            }


            //now we check how much additional length is added by adding an additional word to the line
            float extraLength = 0f;
            int extraSpaceGaps = 0;
            int extraCharGaps = 0;
            bool contractPossible = false;
            TextNode contractEndNode = null;
            for (node = expandEndNode.Next; node != null; node = node.Next)
            {
                

                if (node.Type == TextNodeType.LineBreak)
                    break;

                if (node.Type == TextNodeType.Space)
                {
                    extraLength += node.Length;
                    extraSpaceGaps++;
                } 
                else if (node.Type == TextNodeType.Word)
                {
                    contractEndNode = node;
                    contractPossible = true;
                    extraLength += node.Length;
                    extraCharGaps += (node.Text.Length - 1);
                    break;
                }
            }



            if (justifiable)
            {

                //last part of this condition is to ensure that the full contraction is possible (it is all or nothing with contractions, since it looks really bad if we don't manage the full)
                bool contract = contractPossible && (extraLength + length - targetLength) * Options.JustifyContractionPenalty < (targetLength - length) &&
                    ((targetLength - (length + extraLength + 1)) / targetLength > -Options.JustifyCapContract); 

                if((!contract && length < targetLength) || (contract && length + extraLength > targetLength))  //calculate padding pixels per word and char
                {

                    if (contract)
                    {
                        length += extraLength + 1; 
                        charGaps += extraCharGaps;
                        spaceGaps += extraSpaceGaps;
                    }

                    

                    int totalPixels = (int)(targetLength - length); //the total number of pixels that need to be added to line to justify it
                    int spacePixels = 0; //number of pixels to spread out amongst spaces
                    int charPixels = 0; //number of pixels to spread out amongst char gaps





                    if (contract)
                    {

                        if (totalPixels / targetLength < -Options.JustifyCapContract)
                            totalPixels = (int)(-Options.JustifyCapContract * targetLength);
                    }
                    else
                    {
                        if (totalPixels / targetLength > Options.JustifyCapExpand)
                            totalPixels = (int)(Options.JustifyCapExpand * targetLength);
                    }


                    //work out how to spread pixles between character gaps and word spaces
                    if (charGaps == 0)
                    {
                        spacePixels = totalPixels;
                    }
                    else if (spaceGaps == 0)
                    {
                        charPixels = totalPixels;
                    }
                    else
                    {

                        if(contract)
                            charPixels = (int)(totalPixels * Options.JustifyCharacterWeightForContract * charGaps / spaceGaps);
                        else 
                            charPixels = (int)(totalPixels * Options.JustifyCharacterWeightForExpand * charGaps / spaceGaps);

         
                        if ((!contract && charPixels > totalPixels) ||
                            (contract && charPixels < totalPixels) )
                            charPixels = totalPixels;

                        spacePixels = totalPixels - charPixels;
                    }


                    int pixelsPerChar = 0;  //minimum number of pixels to add per char
                    int leftOverCharPixels = 0; //number of pixels remaining to only add for some chars

                    if (charGaps != 0)
                    {
                        pixelsPerChar = charPixels / charGaps;
                        leftOverCharPixels = charPixels - pixelsPerChar * charGaps;
                    }


                    int pixelsPerSpace = 0; //minimum number of pixels to add per space
                    int leftOverSpacePixels = 0; //number of pixels remaining to only add for some spaces

                    if (spaceGaps != 0)
                    {
                        pixelsPerSpace = spacePixels / spaceGaps;
                        leftOverSpacePixels = spacePixels - pixelsPerSpace * spaceGaps;
                    }

                    //now actually iterate over all nodes and set tweaked length
                    for (node = headNode; node != null; node = node.Next)
                    {

                        if (node.Type == TextNodeType.Space)
                        {
                            node.LengthTweak = pixelsPerSpace;
                            if (leftOverSpacePixels > 0)
                            {
                                node.LengthTweak += 1;
                                leftOverSpacePixels--;
                            }
                            else if (leftOverSpacePixels < 0)
                            {
                                node.LengthTweak -= 1;
                                leftOverSpacePixels++;
                            }


                        }
                        else if (node.Type == TextNodeType.Word)
                        {
                            int cGaps = (node.Text.Length - 1);
                            if (CrumbledWord(node))
                                cGaps++;

                            node.LengthTweak = cGaps * pixelsPerChar;


                            if (leftOverCharPixels >= cGaps)
                            {
                                node.LengthTweak += cGaps;
                                leftOverCharPixels -= cGaps;
                            }
                            else if (leftOverCharPixels <= -cGaps)
                            {
                                node.LengthTweak -= cGaps;
                                leftOverCharPixels += cGaps;
                            } 
                            else  
                            {
                                node.LengthTweak += leftOverCharPixels;
                                leftOverCharPixels = 0;
                            }
                        }

                        if ((!contract && node == expandEndNode) || (contract && node == contractEndNode))
                            break;

                    }

                }

            }


        }


        /// <summary>
        /// Checks whether to skip trailing space on line because the next word does not
        /// fit.
        /// 
        /// We only check one space - the assumption is that if there is more than one,
        /// it is a deliberate attempt to insert spaces.
        /// </summary>
        /// <param name="node"></param>
        /// <param name="lengthSoFar"></param>
        /// <param name="boundWidth"></param>
        /// <returns></returns>
        private bool SkipTrailingSpace(TextNode node, float lengthSoFar, float boundWidth)
        {

            if (node.Type == TextNodeType.Space && node.Next != null && node.Next.Type == TextNodeType.Word && node.ModifiedLength + node.Next.ModifiedLength + lengthSoFar > boundWidth)
            {
                return true;
            }

            return false;

        }





        /// <summary>
        /// Prints text inside the given bounds.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="bounds"></param>
        /// <param name="alignment"></param>
        public void Print(string text, SizeF maxSize, QFontAlignment alignment)
        {
            var processedText = ProcessText(text, maxSize, alignment);
            Print(processedText);
        }





        /// <summary>
        /// Creates node list object associated with the text.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="bounds"></param>
        /// <returns></returns>
        public ProcessedText ProcessText(string text, SizeF maxSize, QFontAlignment alignment)
        {
            //TODO: bring justify and alignment calculations in here

            maxSize.Width = TransformWidthToViewport(maxSize.Width);

            var nodeList = new TextNodeList(text);
            nodeList.MeasureNodes(fontData, Options);

            //we "crumble" words that are two long so that that can be split up
            var nodesToCrumble = new List<TextNode>();
            foreach (TextNode node in nodeList)
                if ((!Options.WordWrap || node.Length >= maxSize.Width) && node.Type == TextNodeType.Word)
                    nodesToCrumble.Add(node);

            foreach (var node in nodesToCrumble)
                nodeList.Crumble(node, 1);

            //need to measure crumbled words
            nodeList.MeasureNodes(fontData, Options);


            var processedText = new ProcessedText();
            processedText.textNodeList = nodeList;
            processedText.maxSize = maxSize;
            processedText.alignment = alignment;


            return processedText;
        }




        /// <summary>
        /// Prints text as previously processed with a boundary and alignment.
        /// </summary>
        /// <param name="processedText"></param>
        public void Print(ProcessedText processedText)
        {
            PrintOrMeasure(processedText, false);
        }



        private SizeF PrintOrMeasure(ProcessedText processedText, bool measureOnly)
        {
            // init values we'll return
            float maxMeasuredWidth = 0f;

            float xPos = 0f;
            float yPos = 0f;

            float xOffset = xPos;
            float yOffset = yPos;

            // determine what capacities we need
            var caps = new EnableCap[] { };

            if (!measureOnly && !UsingVertexBuffers)
            {
                GL.Color4(1.0f, 1.0f, 1.0f, 1.0f);

                caps = new EnableCap[] { EnableCap.Texture2D, EnableCap.Blend };
            }

            Helper.SafeGLEnable(caps, () =>
            {
                if (!measureOnly && !UsingVertexBuffers && Options.UseDefaultBlendFunction)
                    GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);

                float maxWidth = processedText.maxSize.Width;
                var alignment = processedText.alignment;


                //TODO - use these instead of translate when rendering by position (at some point)

                var nodeList = processedText.textNodeList;
                for (TextNode node = nodeList.Head; node != null; node = node.Next)
                    node.LengthTweak = 0f;  //reset tweaks


                if (alignment == QFontAlignment.Right)
                    xOffset -= (float)Math.Ceiling(TextNodeLineLength(nodeList.Head, maxWidth) - maxWidth);
                else if (alignment == QFontAlignment.Centre)
                    xOffset -= (float)Math.Ceiling(0.5f * TextNodeLineLength(nodeList.Head, maxWidth));
                else if (alignment == QFontAlignment.Justify)
                    JustifyLine(nodeList.Head, maxWidth);


                bool atLeastOneNodeCosumedOnLine = false;
                float length = 0f;
                for (TextNode node = nodeList.Head; node != null; node = node.Next)
                {
                    bool newLine = false;

                    if (node.Type == TextNodeType.LineBreak)
                    {
                        newLine = true;
                    }
                    else
                    {

                        if (Options.WordWrap && SkipTrailingSpace(node, length, maxWidth) && atLeastOneNodeCosumedOnLine)
                        {
                            newLine = true;
                        }
                        else if (length + node.ModifiedLength <= maxWidth || !atLeastOneNodeCosumedOnLine)
                        {
                            atLeastOneNodeCosumedOnLine = true;

                            if (!measureOnly)
                                RenderWord(xOffset + length, yOffset, node);
                            length += node.ModifiedLength;

                            maxMeasuredWidth = Math.Max(length, maxMeasuredWidth);

                        }
                        else if (Options.WordWrap)
                        {
                            newLine = true;
                            if (node.Previous != null)
                                node = node.Previous;
                        }
                        else
                            continue; // continue so we still read line breaks even if reached max width
                    }

                    if (newLine)
                    {
                        if (yOffset + LineSpacing - yPos >= processedText.maxSize.Height)
                            break;

                        yOffset += LineSpacing;
                        xOffset = xPos;
                        length = 0f;
                        atLeastOneNodeCosumedOnLine = false;

                        if (node.Next != null)
                        {
                            if (alignment == QFontAlignment.Right)
                                xOffset -= (float)Math.Ceiling(TextNodeLineLength(node.Next, maxWidth) - maxWidth);
                            else if (alignment == QFontAlignment.Centre)
                                xOffset -= (float)Math.Ceiling(0.5f * TextNodeLineLength(node.Next, maxWidth));
                            else if (alignment == QFontAlignment.Justify)
                                JustifyLine(node.Next, maxWidth);
                        }
                    }
                }
            });

            return new SizeF(maxMeasuredWidth, yOffset + LineSpacing - yPos);
        }


        /*
        public void Begin()
        {
            ProjectionStack.Begin();
        }

        public void End()
        {
            ProjectionStack.End();
        }*/


        public static void Begin()
        {
            ProjectionStack.Begin();
        }

        public static void End()
        {
            ProjectionStack.End();
        }

        /// <summary>
        /// Invalidates the internally cached viewport, causing it to be 
        /// reread the next time it is required. This should be called
        /// if the viewport and text is to be rendered to the new 
        /// viewport.
        /// </summary>
        public static void RefreshViewport()
        {
            ProjectionStack.InvalidateViewport();
        }

        private void InitVBOs()
        {
            UsingVertexBuffers = true;
            VertexBuffers = new QVertexBuffer[fontData.Pages.Length];

            for (int i = 0; i < VertexBuffers.Length; i++)
            {
                int textureID = fontData.Pages[i].GLTexID;
                VertexBuffers[i] = new QVertexBuffer(textureID);
            }
        }

        public void ResetVBOs()
        {
            foreach (var buffer in VertexBuffers)
                buffer.Reset();
        }

        public void LoadVBOs()
        {
            foreach (var buffer in VertexBuffers)
                buffer.Load();
        }

        public void DrawVBOs()
        {
            foreach (var buffer in VertexBuffers)
                buffer.Draw();
        }
    }
}
