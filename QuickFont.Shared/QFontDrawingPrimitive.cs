using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using OpenTK;

namespace QuickFont
{
    /// <summary>
    /// Handles the vertex data for rendering text
    /// </summary>
    [DebuggerDisplay("Text = {_DisplayTest_dbg}")]
    public class QFontDrawingPrimitive
    {
#if DEBUG   // Keep copy of string for debug purposes, only
        private string _DisplayText_dbg = "<processedtext>";
#endif

        /// <summary>
        /// Creates a new instance of <see cref="QFontDrawingPrimitive"/> with
        /// the given <see cref="QFont"/> and <see cref="QFontRenderOptions"/>
        /// </summary>
        /// <param name="font">The <see cref="QFont"/></param>
        /// <param name="options">The <see cref="QFontRenderOptions"/></param>
        public QFontDrawingPrimitive(QFont font, QFontRenderOptions options)
        {
            Font = font;
            Options = options;
        }

        /// <summary>
        /// Creates a new instance of <see cref="QFontDrawingPrimitive"/> with
        /// the given <see cref="QFont"/> and the default <see cref="QFontRenderOptions"/>
        /// </summary>
        /// <param name="font"></param>
        public QFontDrawingPrimitive(QFont font)
        {
            Font = font;
            Options = new QFontRenderOptions();
        }

        /// <summary>
        /// The model-view matrix for this <see cref="QFontDrawingPrimitive"/>.
        /// Default value is the identity matrix
        /// </summary>
        public Matrix4 ModelViewMatrix = Matrix4.Identity;

        /// <summary>
        /// An offset that is added to all positions
        /// </summary>
        public Vector3 PrintOffset { get; set; }

        /// <summary>
        /// The linespacing used by the <see cref="QFont"/>
        /// </summary>
        public float LineSpacing
        {
            get { return (float) Math.Ceiling(Font.FontData.MaxGlyphHeight*Options.LineSpacing); }
        }

        /// <summary>
        /// Whether monospacing is active in the <see cref="QFont"/>
        /// </summary>
        public bool IsMonospacingActive
        {
            get { return Font.FontData.IsMonospacingActive(Options); }
        }

        /// <summary>
        /// The monospacing width
        /// </summary>
        public float MonoSpaceWidth
        {
            get { return Font.FontData.GetMonoSpaceWidth(Options); }
        }

        /// <summary>
        /// The <see cref="QFont"/> used by this instance
        /// </summary>
        public QFont Font { get; }

        /// <summary>
        /// The <see cref="QFontRenderOptions"/> of this instance
        /// </summary>
        public QFontRenderOptions Options { get; private set; }

        /// <summary>
        /// The size of the last text printed with this instance
        /// </summary>
        public SizeF LastSize { get; private set; }

        /// <summary>
        /// The current vertex list
        /// </summary>
        internal IList<QVertex> CurrentVertexRepr { get; } = new List<QVertex>();

        /// <summary>
        /// The current shadow vertex list
        /// </summary>
        internal IList<QVertex> ShadowVertexRepr { get; } = new List<QVertex>();

        /// <summary>
        /// Render a character's drop shadow
        /// </summary>
        /// <param name="x">The x coordinate</param>
        /// <param name="y">The y coordinate</param>
        /// <param name="c">The character to render</param>
        /// <param name="nonShadowGlyph">The non drop-shadowed glyph of the character</param>
        /// <param name="shadowFont">The drop shadow font</param>
        /// <param name="clippingRectangle">The clipping rectangle</param>
        private void RenderDropShadow(float x, float y, char c, QFontGlyph nonShadowGlyph, QFont shadowFont, ref Rectangle clippingRectangle)
        {
            //note can cast drop shadow offset to int, but then you can't move the shadow smoothly...
            if (shadowFont != null && Options.DropShadowActive)
            {
                float xOffset = (Font.FontData.MeanGlyphWidth*Options.DropShadowOffset.X + nonShadowGlyph.Rect.Width*0.5f);
                float yOffset = (Font.FontData.MeanGlyphWidth*Options.DropShadowOffset.Y + nonShadowGlyph.Rect.Height*0.5f + nonShadowGlyph.YOffset);
                RenderGlyph(x + xOffset, y + yOffset, c, shadowFont, ShadowVertexRepr, clippingRectangle);
            }
        }
        
        /// <summary>
        /// Scissor test a rectangle
        /// </summary>
        /// <param name="x">The x coordinate of the rectangle</param>
        /// <param name="y">The y coordinate of the rectangle</param>
        /// <param name="width">Th width of the rectangle</param>
        /// <param name="height">The height of the rectangle</param>
        /// <param name="u1">The u1 texture coordinate</param>
        /// <param name="v1">The v1 texture coordinate</param>
        /// <param name="u2">The u2 texture coordinate</param>
        /// <param name="v2">The v2 texture coordinate</param>
        /// <param name="clipRectangle">The clipping rectangle</param>
        /// <returns>Whether the rectangle is completely clipped</returns>
        private bool ScissorsTest(ref float x, ref float y, ref float width, ref float height, ref float u1, ref float v1, ref float u2, ref float v2, Rectangle clipRectangle)
        {
            float cRectY = clipRectangle.Y;
            if (y > cRectY + clipRectangle.Height)
            {
                float oldHeight = height;
                float delta = Math.Abs(y - (cRectY + clipRectangle.Height));
                y = cRectY + clipRectangle.Height;
                height -= delta;

                if (height <= 0)
                {
                    return true;
                }

                float dv = Math.Abs(delta / oldHeight);
                v1 += dv * (v2 - v1);
            }

            if ((y - height) < (cRectY))
            {
                float oldHeight = height;
                float delta = Math.Abs(cRectY - (y - height));

                height -= delta;

                if (height <= 0)
                {
                    return true;
                }

                float dv = Math.Abs(delta/oldHeight);
                v2 -= dv*(v2 - v1);
            }

            if (x < clipRectangle.X)
            {
                float oldWidth = width;
                float delta = clipRectangle.X - x;
                x = clipRectangle.X;
                width -= delta;

                if (width <= 0)
                {
                    return true;
                }

                float du = delta / oldWidth;

                u1 += du * (u2 - u1);
            }

            if ((x + width) > (clipRectangle.X + clipRectangle.Width))
            {
                float oldWidth = width;
                float delta = (x + width) - (clipRectangle.X + clipRectangle.Width);

                width -= delta;

                if (width <= 0)
                {
                    return true;
                }

                float du = delta / oldWidth;

                u2 -= du * (u2 - u1);
            }
            return false;
        }

        /// <summary>
        /// Renders the glyph at the position given.
        /// </summary>
        /// <param name="x">The x coordinate</param>
        /// <param name="y">The y coordinate</param>
        /// <param name="c">The character to print.</param>
        /// <param name="font">The font to print with</param>
        /// <param name="store">The collection of <see cref="QVertex"/>'s to add the vertices too</param>
        /// <param name="clippingRectangle">The clipping rectangle</param>
        internal void RenderGlyph(float x, float y, char c, QFont font, IList<QVertex> store, Rectangle clippingRectangle)
        {
            QFontGlyph glyph = font.FontData.CharSetMapping[c];

            //note: it's not immediately obvious, but this combined with the paramteters to 
            //RenderGlyph for the shadow mean that we render the shadow centrally (despite it being a different size)
            //under the glyph
            if (font.FontData.IsDropShadow)
            {
                x -= (int) (glyph.Rect.Width*0.5f);
                y -= (int) (glyph.Rect.Height*0.5f + glyph.YOffset);
            }
            else
            {
                RenderDropShadow(x, y, c, glyph, font.FontData.DropShadowFont, ref clippingRectangle);
            }

            y = -y;

            TexturePage sheet = font.FontData.Pages[glyph.Page];

            float tx1 = (float)(glyph.Rect.X) / sheet.Width;
            float ty1 = (float)(glyph.Rect.Y) / sheet.Height;
            float tx2 = (float)(glyph.Rect.X + glyph.Rect.Width) / sheet.Width;
            float ty2 = (float)(glyph.Rect.Y + glyph.Rect.Height) / sheet.Height;

            float vx = x + PrintOffset.X;
            float vy = y - glyph.YOffset + PrintOffset.Y;
            float vwidth = glyph.Rect.Width;
            float vheight = glyph.Rect.Height;

            // Don't draw anything if the glyph is completely clipped
            if (clippingRectangle != default(Rectangle) && ScissorsTest(ref vx, ref vy, ref vwidth, ref vheight, ref tx1, ref ty1, ref tx2, ref ty2, clippingRectangle)) return;

            var tv1 = new Vector2(tx1, ty1);
            var tv2 = new Vector2(tx1, ty2);
            var tv3 = new Vector2(tx2, ty2);
            var tv4 = new Vector2(tx2, ty1);

            Vector3 v1 = new Vector3(vx, vy, PrintOffset.Z);
            Vector3 v2 = new Vector3(vx, vy - vheight, PrintOffset.Z);
            Vector3 v3 = new Vector3(vx + vwidth, vy - vheight, PrintOffset.Z);
            Vector3 v4 = new Vector3(vx + vwidth, vy, PrintOffset.Z);

            Color color;
            if (font.FontData.IsDropShadow)
                color = Options.DropShadowColour;
            else
                color = Options.Colour;

            Vector4 colour = Helper.ToVector4(color);

            store.Add(new QVertex { Position = v1, TextureCoord = tv1, VertexColor = colour });
            store.Add(new QVertex { Position = v2, TextureCoord = tv2, VertexColor = colour });
            store.Add(new QVertex { Position = v3, TextureCoord = tv3, VertexColor = colour });

            store.Add(new QVertex { Position = v1, TextureCoord = tv1, VertexColor = colour });
            store.Add(new QVertex { Position = v3, TextureCoord = tv3, VertexColor = colour });
            store.Add(new QVertex { Position = v4, TextureCoord = tv4, VertexColor = colour });
        }

        /// <summary>
        /// Measures the length from the start of the text up to the next line
        /// or the end of the string, whichever comes first
        /// </summary>
        /// <param name="text">The text to measure</param>
        /// <returns>The length of the next line</returns>
        private float MeasureNextlineLength(string text)
        {
            float xOffset = 0;

            for (int i = 0; i < text.Length; i++)
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
                        xOffset += (float) Math.Ceiling(Font.FontData.MeanGlyphWidth*Options.WordSpacing);
                    }
                        //normal character
                    else if (Font.FontData.CharSetMapping.ContainsKey(c))
                    {
                        QFontGlyph glyph = Font.FontData.CharSetMapping[c];
                        xOffset +=
                            (float)
                            Math.Ceiling(glyph.Rect.Width + Font.FontData.MeanGlyphWidth * Options.CharacterSpacing + 
                                Font.FontData.GetKerningPairCorrection(i, text, null));
                    }
                }
            }
            return xOffset;
        }

        /// <summary>
        /// Transforms a given input position to the current viewport
        /// </summary>
        /// <param name="input">The untransformed position</param>
        /// <returns>The transformed position</returns>
        private Vector2 TransformPositionToViewport(Vector2 input)
        {
            Viewport? v2 = Options.TransformToViewport;
            if (v2 == null)
            {
                return input;
            }
            Viewport? v1 = ViewportHelper.CurrentViewport;

            Debug.Assert(v1 != null, "v1 != null");
            var x = (input.X - v2.Value.X)*(v1.GetValueOrDefault().Width/v2.Value.Width);
            var y = (input.Y - v2.Value.Y)*(v1.GetValueOrDefault().Height/v2.Value.Height);

            return new Vector2(x, y);
        }

        /// <summary>
        /// Transforms a given width to the current viewport
        /// </summary>
        /// <param name="input">The untransformed width</param>
        /// <param name="options">The render options</param>
        /// <returns>The transformed width</returns>
        private static float TransformWidthToViewport(float input, QFontRenderOptions options)
        {
            Viewport? v2 = options.TransformToViewport;
            if (v2 == null)
            {
                return input;
            }
            Viewport? v1 = ViewportHelper.CurrentViewport;

            Debug.Assert(v1 != null, "v1 != null");
            return input*(v1.GetValueOrDefault().Width/v2.Value.Width);
        }

        /// <summary>
        /// Transforms a given size to the current viewport
        /// </summary>
        /// <param name="input">The untransformed size</param>
        /// <returns>The transformed size</returns>
        private SizeF TransformMeasureFromViewport(SizeF input)
        {
            Viewport? v2 = Options.TransformToViewport;
            if (v2 == null)
            {
                return input;
            }
            Viewport? v1 = ViewportHelper.CurrentViewport;

            Debug.Assert(v1 != null, "v1 != null");
            var x = input.Width*(v2.Value.Width/v1.GetValueOrDefault().Width);
            var y = input.Height*(v2.Value.Height/v1.GetValueOrDefault().Height);

            return new SizeF(x, y);
        }

        /// <summary>
        /// Locks the position so that it lies exactly on a pixel
        /// </summary>
        /// <param name="input">The input position</param>
        /// <returns>The position locked to the nearest pixel</returns>
        private Vector2 LockToPixel(Vector2 input)
        {
            if (Options.LockToPixel)
            {
                float r = Options.LockToPixelRatio;
                return new Vector2((1 - r)*input.X + r*((int) Math.Round(input.X)),
                                   (1 - r)*input.Y + r*((int) Math.Round(input.Y)));
            }
            return input;
        }

        /// <summary>
        /// Transforms a vector to the current viewport
        /// </summary>
        /// <param name="input">The untransformed vector</param>
        /// <returns>The transformed vector</returns>
        private Vector3 TransformToViewport(Vector3 input)
        {
            return new Vector3(LockToPixel(TransformPositionToViewport(input.Xy))) {Z = input.Z};
        }

        /// <summary>
        /// Prints the specified text with the given alignment and clipping rectangle
        /// </summary>
        /// <param name="text">The text to print</param>
        /// <param name="position">The position of the text</param>
        /// <param name="alignment">The text alignment</param>
        /// <param name="clippingRectangle">The clipping rectangle to scissor test the text with</param>
        /// <returns>The size of the printed text</returns>
        public SizeF Print(string text, Vector3 position, QFontAlignment alignment, Rectangle clippingRectangle = default(Rectangle))
        {
            PrintOffset = TransformToViewport(position);
            return PrintOrMeasure(text, alignment, false, clippingRectangle);
        }

        /// <summary>
        /// Prints the specified text with the given alignment, color and clipping rectangle
        /// </summary>
        /// <param name="text">The text to print</param>
        /// <param name="position">The text position</param>
        /// <param name="alignment">The text alignment</param>
        /// <param name="color">The text color</param>
        /// <param name="clippingRectangle">The clipping rectangle to scissor test the text with</param>
        /// <returns>The size of the printed text</returns>
        public SizeF Print(string text, Vector3 position, QFontAlignment alignment, Color color, Rectangle clippingRectangle = default(Rectangle))
        {
            Options.Colour = color;
            PrintOffset = TransformToViewport(position);
            return PrintOrMeasure(text, alignment, false, clippingRectangle);
        }

        /// <summary>
        /// Prints the specified text with the given alignment, maximum size and clipping rectangle
        /// </summary>
        /// <param name="text">The text to print</param>
        /// <param name="position">The text position</param>
        /// <param name="maxSize">The maxmimum size of the printed text</param>
        /// <param name="alignment">The text alignment</param>
        /// <param name="clippingRectangle">The clipping rectangle to scissor test the text with</param>
        /// <returns>The size of the printed text</returns>
        public SizeF Print(string text, Vector3 position, SizeF maxSize, QFontAlignment alignment, Rectangle clippingRectangle = default(Rectangle))
        {
            ProcessedText processedText = ProcessText(Font, Options, text, maxSize, alignment);
            return Print(processedText, TransformToViewport(position), clippingRectangle);
        }

        /// <summary>
        /// Prints the specified text with the given alignment, maximum size, colour and clipping rectangle
        /// </summary>
        /// <param name="text">The text to print</param>
        /// <param name="position">The text position</param>
        /// <param name="maxSize">The maxmimum size of the printed text</param>
        /// <param name="alignment">The text alignment</param>
        /// <param name="colour">The text colour</param>
        /// <param name="clippingRectangle">The clipping rectangle to scissor test the text with</param>
        /// <returns>The size of the printed text</returns>
        public SizeF Print(string text, Vector3 position, SizeF maxSize, QFontAlignment alignment, Color colour, Rectangle clippingRectangle = default(Rectangle))
        {
            ProcessedText processedText = ProcessText(Font, Options, text, maxSize, alignment);
            return Print(processedText, TransformToViewport(position), colour, clippingRectangle);
        }

        /// <summary>
        /// Prints the specified processed text with the given clipping rectangle
        /// </summary>
        /// <param name="processedText">The processed text to print</param>
        /// <param name="position">The text position</param>
        /// <param name="clippingRectangle">The clipping rectangle to scissor test the text with</param>
        /// <returns>The size of the printed text</returns>
        public SizeF Print(ProcessedText processedText, Vector3 position, Rectangle clippingRectangle = default(Rectangle))
        {
            PrintOffset = TransformToViewport(position);
            return PrintOrMeasure(processedText, false, clippingRectangle);
        }

        /// <summary>
        /// Prints the specified processed text with the given color and clipping rectangle
        /// </summary>
        /// <param name="processedText">The processed text to print</param>
        /// <param name="position">The text position</param>
        /// <param name="colour">The text colour</param>
        /// <param name="clippingRectangle">The clipping rectangle to scissor test the text with</param>
        /// <returns>The size of the printed text</returns>
        public SizeF Print(ProcessedText processedText, Vector3 position, Color colour, Rectangle clippingRectangle = default(Rectangle))
        {
            Options.Colour = colour;
            PrintOffset = TransformToViewport(position);
            return PrintOrMeasure(processedText, false, clippingRectangle);
        }

        /// <summary>
        /// Measures the specified text with the given alignment
        /// </summary>
        /// <param name="text">The specified text</param>
        /// <param name="alignment">The text alignment</param>
        /// <returns>The size of the text</returns>
        public SizeF Measure(string text, QFontAlignment alignment = QFontAlignment.Left)
        {
            return TransformMeasureFromViewport(PrintOrMeasure(text, alignment, true));
        }

        /// <summary>
        /// Measures the specified text with the given alignment and a maximum width
        /// (no maximum height)
        /// </summary>
        /// <param name="text">The specified text</param>
        /// <param name="maxWidth">The maximum width of the text</param>
        /// <param name="alignment">The text alignment</param>
        /// <returns>The size of the text</returns>
        public SizeF Measure(string text, float maxWidth, QFontAlignment alignment)
        {
            return Measure(text, new SizeF(maxWidth, -1), alignment);
        }

        /// <summary>
        ///     Measures the actual width and height of the block of text.
        /// </summary>
        /// <param name="text">The text to measure</param>
        /// <param name="maxSize">The maximum size of the text</param>
        /// <param name="alignment">The text alignment</param>
        /// <returns>The size of the text</returns>
        public SizeF Measure(string text, SizeF maxSize, QFontAlignment alignment)
        {
            ProcessedText processedText = ProcessText(Font, Options, text, maxSize, alignment);
            return Measure(processedText);
        }

        /// <summary>
        ///     Measures the actual width and height of the block of text
        /// </summary>
        /// <param name="processedText">The processed text to measure</param>
        /// <returns>The size of the text</returns>
        public SizeF Measure(ProcessedText processedText)
        {
            return TransformMeasureFromViewport(PrintOrMeasure(processedText, true));
        }

        /// <summary>
        /// Print or measure the specified text
        /// </summary>
        /// <param name="text">The text to print or measure</param>
        /// <param name="alignment">The text alignment</param>
        /// <param name="measureOnly">Whether to only measure the text</param>
        /// <param name="clippingRectangle">The clipping rectangle to scissor test the text with</param>
        /// <returns>The size of the text</returns>
        private SizeF PrintOrMeasure(string text, QFontAlignment alignment, bool measureOnly, Rectangle clippingRectangle = default(Rectangle))
        {
            float maxWidth = 0f;
            float xOffset = 0f;
            float yOffset = 0f;

            float maxXpos = float.MinValue;
            float minXPos = float.MaxValue;

            text = text.Replace("\r\n", "\r");
#if DEBUG
            _DisplayText_dbg = text;
#endif
            if (alignment == QFontAlignment.Right)
                xOffset -= MeasureNextlineLength(text);
            else if (alignment == QFontAlignment.Centre)
                xOffset -= (int) (0.5f*MeasureNextlineLength(text));

            float maxCharHeight = 0;
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];

                //newline
                if (c == '\r' || c == '\n')
                {
                    yOffset += LineSpacing;
                    maxCharHeight = 0;
                    xOffset = 0f;

                    if (alignment == QFontAlignment.Right)
                        xOffset -= MeasureNextlineLength(text.Substring(i + 1));
                    else if (alignment == QFontAlignment.Centre)
                        xOffset -= (int) (0.5f*MeasureNextlineLength(text.Substring(i + 1)));
                }
                else
                {
                    minXPos = Math.Min(xOffset, minXPos);

                    //normal character
                    if (c != ' ' && Font.FontData.CharSetMapping.ContainsKey(c))
                    {
                        if (!measureOnly)
                            RenderGlyph(xOffset, yOffset, c, Font, CurrentVertexRepr, clippingRectangle);
                    }

                    if (IsMonospacingActive)
                        xOffset += MonoSpaceWidth;
                    else
                    {
                        if (c == ' ')
                            xOffset += (float)Math.Ceiling(Font.FontData.MeanGlyphWidth * Options.WordSpacing);
                            //normal character
                        else if (Font.FontData.CharSetMapping.ContainsKey(c))
                        {
                            QFontGlyph glyph = Font.FontData.CharSetMapping[c];
                            xOffset +=
                                (float)
                                Math.Ceiling(glyph.Rect.Width + Font.FontData.MeanGlyphWidth * Options.CharacterSpacing + Font.FontData.GetKerningPairCorrection(i, text, null));
                            maxCharHeight = Math.Max(maxCharHeight, glyph.Rect.Height + glyph.YOffset);
                        }
                    }

                    maxXpos = Math.Max(xOffset, maxXpos);
                }
            }

            if (Math.Abs(minXPos - float.MaxValue) > float.Epsilon)
                maxWidth = maxXpos - minXPos;

            LastSize = new SizeF(maxWidth, yOffset + LineSpacing);
            return LastSize;
        }

        /// <summary>
        /// Print or measure the specified processed text
        /// </summary>
        /// <param name="processedText">The processed text</param>
        /// <param name="measureOnly">Whether to only measure the text</param>
        /// <param name="clippingRectangle">The clipping rectangle to scissor test the text with</param>
        /// <returns>The size of the text</returns>
        private SizeF PrintOrMeasure(ProcessedText processedText, bool measureOnly, Rectangle clippingRectangle = default(Rectangle))
        {
            // init values we'll return
            float maxMeasuredWidth = 0f;
            float maxCharHeight = 0f;

            const float xPos = 0f;
            const float yPos = 0f;

            float xOffset = xPos;
            float yOffset = yPos;

            float maxWidth = processedText.MaxSize.Width;
            QFontAlignment alignment = processedText.Alignment;

            //TODO - use these instead of translate when rendering by position (at some point)

            TextNodeList nodeList = processedText.TextNodeList;
            for (TextNode node = nodeList.Head; node != null; node = node.Next)
                node.LengthTweak = 0f; //reset tweaks


            if (alignment == QFontAlignment.Right)
                xOffset -= (float) Math.Ceiling(TextNodeLineLength(nodeList.Head, maxWidth) - maxWidth);
            else if (alignment == QFontAlignment.Centre)
                xOffset -= (float) Math.Ceiling(0.5f*TextNodeLineLength(nodeList.Head, maxWidth));
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
                            RenderWord(xOffset + length, yOffset, node, ref clippingRectangle);
                        length += node.ModifiedLength;

                        maxCharHeight = Math.Max(maxCharHeight, node.Height);
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
                    if (processedText.MaxSize.Height > 0 &&
                        yOffset + LineSpacing - yPos >= processedText.MaxSize.Height)
                        break;
                    yOffset += LineSpacing;
                    xOffset = xPos;
                    length = 0f;
                    atLeastOneNodeCosumedOnLine = false;

                    if (node.Next != null)
                    {
                        if (alignment == QFontAlignment.Right)
                            xOffset -= (float) Math.Ceiling(TextNodeLineLength(node.Next, maxWidth) - maxWidth);
                        else if (alignment == QFontAlignment.Centre)
                            xOffset -= (float) Math.Ceiling(0.5f*TextNodeLineLength(node.Next, maxWidth));
                        else if (alignment == QFontAlignment.Justify)
                            JustifyLine(node.Next, maxWidth);
                    }
                }
            }

            LastSize = new SizeF(maxMeasuredWidth, yOffset + LineSpacing - yPos);
            return LastSize;
        }

        /// <summary>
        /// Renders a word (text node)
        /// </summary>
        /// <param name="x">The x coordinate of the word</param>
        /// <param name="y">The y coordinate of the word</param>
        /// <param name="node">The word to render</param>
        /// <param name="clippingRectangle">The clipping rectangle to scissor test the word with</param>
        private void RenderWord(float x, float y, TextNode node, ref Rectangle clippingRectangle)
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
                pixelsPerGap = (int) node.LengthTweak/charGaps;
                leftOverPixels = (int) node.LengthTweak - pixelsPerGap*charGaps;
            }

            for (int i = 0; i < node.Text.Length; i++)
            {
                char c = node.Text[i];
                if (Font.FontData.CharSetMapping.ContainsKey(c))
                {
                    QFontGlyph glyph = Font.FontData.CharSetMapping[c];

                    RenderGlyph(x, y, c, Font, CurrentVertexRepr, clippingRectangle);

                    if (IsMonospacingActive)
                        x += MonoSpaceWidth;
                    else
                        x +=
                            (int)
                            Math.Ceiling(glyph.Rect.Width + Font.FontData.MeanGlyphWidth * Options.CharacterSpacing + Font.FontData.GetKerningPairCorrection(i, node.Text, node));

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
        ///     Computes the length of the next line, and whether the line is valid for
        ///     justification.
        /// </summary>
        /// <param name="node">The starting text node</param>
        /// <param name="maxLength">The maximum length of the line</param>
        /// <returns>The length of the line</returns>
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

        /// <summary>
        /// Checks whether a textnode has been crumbled
        /// </summary>
        /// <param name="node">The starting node</param>
        /// <returns>Whether the textnode has been crumbled</returns>
        private bool CrumbledWord(TextNode node)
        {
            return (node.Type == TextNodeType.Word && node.Next != null && node.Next.Type == TextNodeType.Word);
        }

        /// <summary>
        ///     Computes the length of the next line, and whether the line is valid for
        ///     justification.
        /// </summary>
        /// <param name="node">The starting text node</param>
        /// <param name="targetLength">The target line length</param>
        private void JustifyLine(TextNode node, float targetLength)
        {
            bool justifiable = false;

            if (node == null)
                return;

            TextNode headNode = node; //keep track of the head node


            //start by finding the length of the block of text that we know will actually fit:

            int charGaps = 0;
            int spaceGaps = 0;

            bool atLeastOneNodeCosumedOnLine = false;
            float length = 0;
            TextNode expandEndNode = node; //the node at the end of the smaller list (before adding additional word)
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
                bool contract = contractPossible &&
                                (extraLength + length - targetLength)*Options.JustifyContractionPenalty <
                                (targetLength - length) &&
                                ((targetLength - (length + extraLength + 1))/targetLength > -Options.JustifyCapContract);

                if ((!contract && length < targetLength) || (contract && length + extraLength > targetLength))
                    //calculate padding pixels per word and char
                {
                    if (contract)
                    {
                        length += extraLength + 1;
                        charGaps += extraCharGaps;
                        spaceGaps += extraSpaceGaps;
                    }


                    var totalPixels = (int) (targetLength - length);
                    //the total number of pixels that need to be added to line to justify it
                    int spacePixels = 0; //number of pixels to spread out amongst spaces
                    int charPixels = 0; //number of pixels to spread out amongst char gaps


                    if (contract)
                    {
                        if (totalPixels/targetLength < -Options.JustifyCapContract)
                            totalPixels = (int) (-Options.JustifyCapContract*targetLength);
                    }
                    else
                    {
                        if (totalPixels/targetLength > Options.JustifyCapExpand)
                            totalPixels = (int) (Options.JustifyCapExpand*targetLength);
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
                        if (contract)
                            charPixels =
                                (int) (totalPixels*Options.JustifyCharacterWeightForContract*charGaps/spaceGaps);
                        else
                            charPixels = (int) (totalPixels*Options.JustifyCharacterWeightForExpand*charGaps/spaceGaps);


                        if ((!contract && charPixels > totalPixels) ||
                            (contract && charPixels < totalPixels))
                            charPixels = totalPixels;

                        spacePixels = totalPixels - charPixels;
                    }


                    int pixelsPerChar = 0; //minimum number of pixels to add per char
                    int leftOverCharPixels = 0; //number of pixels remaining to only add for some chars

                    if (charGaps != 0)
                    {
                        pixelsPerChar = charPixels/charGaps;
                        leftOverCharPixels = charPixels - pixelsPerChar*charGaps;
                    }

                    int pixelsPerSpace = 0; //minimum number of pixels to add per space
                    int leftOverSpacePixels = 0; //number of pixels remaining to only add for some spaces

                    if (spaceGaps != 0)
                    {
                        pixelsPerSpace = spacePixels/spaceGaps;
                        leftOverSpacePixels = spacePixels - pixelsPerSpace*spaceGaps;
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

                            node.LengthTweak = cGaps*pixelsPerChar;


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
        ///     Checks whether to skip trailing space on line because the next word does not
        ///     fit.
        ///     We only check one space - the assumption is that if there is more than one,
        ///     it is a deliberate attempt to insert spaces.
        /// </summary>
        /// <param name="node">The starting text node</param>
        /// <param name="lengthSoFar">The length of the line so far</param>
        /// <param name="boundWidth">The maximum width</param>
        /// <returns>Whether we can skip the trailing space</returns>
        private bool SkipTrailingSpace(TextNode node, float lengthSoFar, float boundWidth)
        {
            if (node.Type == TextNodeType.Space && node.Next != null && node.Next.Type == TextNodeType.Word &&
                node.ModifiedLength + node.Next.ModifiedLength + lengthSoFar > boundWidth)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        ///     Creates node list object associated with the text.
        /// </summary>
        /// <param name="options">The font render options</param>
        /// <param name="text">The text to process</param>
        /// <param name="font">The <see cref="QFont"/> to process the text with</param>
        /// <param name="maxSize">The maximum size of the processed text</param>
        /// <param name="alignment">The text alignment</param>
        /// <returns>The processed text</returns>
        public static ProcessedText ProcessText(QFont font, QFontRenderOptions options, string text, SizeF maxSize, QFontAlignment alignment)
        {
            //TODO: bring justify and alignment calculations in here
            maxSize.Width = TransformWidthToViewport(maxSize.Width, options);

            var nodeList = new TextNodeList(text);
            nodeList.MeasureNodes(font.FontData, options);

            //we "crumble" words that are two long so that that can be split up
            var nodesToCrumble = new List<TextNode>();
            foreach (TextNode node in nodeList)
                if ((!options.WordWrap || node.Length >= maxSize.Width) && node.Type == TextNodeType.Word)
                    nodesToCrumble.Add(node);

            foreach (TextNode node in nodesToCrumble)
                nodeList.Crumble(node, 1);

            //need to measure crumbled words
            nodeList.MeasureNodes(font.FontData, options);


            var processedText = new ProcessedText();
            processedText.TextNodeList = nodeList;
            processedText.MaxSize = maxSize;
            processedText.Alignment = alignment;


            return processedText;
        }
    }
}
