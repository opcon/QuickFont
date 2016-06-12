using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using OpenTK;

namespace QuickFont
{
    public class QFontDrawingPrimitive
    {
        private Vector3 _printOffset;
        private readonly QFont _font;
        private readonly IList<QVertex> _currentVertexRepr = new List<QVertex>();
        private readonly IList<QVertex> _shadowVertexRepr = new List<QVertex>();
#if DEBUG   // Keep copy of string for debug purposes, only
        private string _DisplayText_dbg = "<processedtext>";
#endif

        public QFontDrawingPrimitive(QFont font, QFontRenderOptions options)
        {
            _font = font;
            Options = options;
        }

        public QFontDrawingPrimitive(QFont font)
        {
            _font = font;
            Options = new QFontRenderOptions();
        }

        public Vector3 PrintOffset
        {
            get { return _printOffset; }
            set
            {
                _printOffset = value;
    //if (Font.FontData.dropShadowFont != null)
    //    Font.FontData.dropShadowFont.PrintOffset = value;
            }
        }

        public float LineSpacing
        {
            get { return (float) Math.Ceiling(Font.FontData.maxGlyphHeight*this.Options.LineSpacing); }
        }

        public bool IsMonospacingActive
        {
            get { return Font.FontData.IsMonospacingActive(this.Options); }
        }

        public float MonoSpaceWidth
        {
            get { return Font.FontData.GetMonoSpaceWidth(this.Options); }
        }

        public QFont Font
        {
            get { return _font; }
        }

        public QFontRenderOptions Options { get; private set; }

        public SizeF LastSize { get; private set; }

        internal IList<QVertex> CurrentVertexRepr
        {
            get { return _currentVertexRepr; }
        }

        internal IList<QVertex> ShadowVertexRepr { get { return _shadowVertexRepr; } }

        private void RenderDropShadow(float x, float y, char c, QFontGlyph nonShadowGlyph, QFont shadowFont, ref Rectangle clippingRectangle)
        {
            //note can cast drop shadow offset to int, but then you can't move the shadow smoothly...
            if (shadowFont != null && this.Options.DropShadowActive)
            {
                float xOffset = (_font.FontData.meanGlyphWidth*this.Options.DropShadowOffset.X + nonShadowGlyph.rect.Width*0.5f);
                float yOffset = (_font.FontData.meanGlyphWidth*this.Options.DropShadowOffset.Y + nonShadowGlyph.rect.Height*0.5f + nonShadowGlyph.yOffset);
                this.RenderGlyph(x + xOffset, y + yOffset, c, shadowFont, this.ShadowVertexRepr, clippingRectangle);
            }
        }
        
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

                float dv = Math.Abs((float)delta / (float)oldHeight);
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

                float dv = Math.Abs((float) delta/(float) oldHeight);
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

                float du = (float)delta / (float)oldWidth;

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

                float du = (float)delta / (float)oldWidth;

                u2 -= du * (u2 - u1);
            }
            return false;
        }

        /// <summary>
        /// Renders the glyph at the position given.
        /// </summary>
        /// <param name="x">The x.</param>
        /// <param name="y">The y.</param>
        /// <param name="c">The character to print.</param>
        internal void RenderGlyph(float x, float y, char c, QFont font, IList<QVertex> store, Rectangle clippingRectangle)
        {
            QFontGlyph glyph = font.FontData.CharSetMapping[c];

            //note: it's not immediately obvious, but this combined with the paramteters to 
            //RenderGlyph for the shadow mean that we render the shadow centrally (despite it being a different size)
            //under the glyph
            if (font.FontData.isDropShadow)
            {
                x -= (int) (glyph.rect.Width*0.5f);
                y -= (int) (glyph.rect.Height*0.5f + glyph.yOffset);
            }
            else
            {
                RenderDropShadow(x, y, c, glyph, font.FontData.dropShadowFont, ref clippingRectangle);
            }

            y = -y;

            TexturePage sheet = font.FontData.Pages[glyph.page];

            float tx1 = (float)(glyph.rect.X) / sheet.Width;
            float ty1 = (float)(glyph.rect.Y) / sheet.Height;
            float tx2 = (float)(glyph.rect.X + glyph.rect.Width) / sheet.Width;
            float ty2 = (float)(glyph.rect.Y + glyph.rect.Height) / sheet.Height;

            float vx = x + PrintOffset.X;
            float vy = y - glyph.yOffset + PrintOffset.Y;
            float vwidth = glyph.rect.Width;
            float vheight = glyph.rect.Height;

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
            if (font.FontData.isDropShadow)
                color = this.Options.DropShadowColour;
            else
                color = this.Options.Colour;

            Vector4 colour = Helper.ToVector4(color);

            store.Add(new QVertex() { Position = v1, TextureCoord = tv1, VertexColor = colour });
            store.Add(new QVertex() { Position = v2, TextureCoord = tv2, VertexColor = colour });
            store.Add(new QVertex() { Position = v3, TextureCoord = tv3, VertexColor = colour });

            store.Add(new QVertex() { Position = v1, TextureCoord = tv1, VertexColor = colour });
            store.Add(new QVertex() { Position = v3, TextureCoord = tv3, VertexColor = colour });
            store.Add(new QVertex() { Position = v4, TextureCoord = tv4, VertexColor = colour });
        }


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
                        xOffset += (float) Math.Ceiling(_font.FontData.meanGlyphWidth*this.Options.WordSpacing);
                    }
                        //normal character
                    else if (_font.FontData.CharSetMapping.ContainsKey(c))
                    {
                        QFontGlyph glyph = _font.FontData.CharSetMapping[c];
                        xOffset +=
                            (float)
                            Math.Ceiling(glyph.rect.Width + _font.FontData.meanGlyphWidth * this.Options.CharacterSpacing + 
                                _font.FontData.GetKerningPairCorrection(i, text, null));
                    }
                }
            }
            return xOffset;
        }

        private Vector2 TransformPositionToViewport(Vector2 input)
        {
            Viewport? v2 = this.Options.TransformToViewport;
            if (v2 == null)
            {
                return input;
            }
            Viewport? v1 = ViewportHelper.CurrentViewport;

            float X, Y;

            Debug.Assert(v1 != null, "v1 != null");
            X = (input.X - v2.Value.X)*(v1.Value.Width/v2.Value.Width);
            Y = (input.Y - v2.Value.Y)*(v1.Value.Height/v2.Value.Height);

            return new Vector2(X, Y);
        }

        private static float TransformWidthToViewport(float input, QFontRenderOptions options)
        {
            Viewport? v2 = options.TransformToViewport;
            if (v2 == null)
            {
                return input;
            }
            Viewport? v1 = ViewportHelper.CurrentViewport;

            Debug.Assert(v1 != null, "v1 != null");
            return input*(v1.Value.Width/v2.Value.Width);
        }

        private SizeF TransformMeasureFromViewport(SizeF input)
        {
            Viewport? v2 = this.Options.TransformToViewport;
            if (v2 == null)
            {
                return input;
            }
            Viewport? v1 = ViewportHelper.CurrentViewport;

            float X, Y;

            Debug.Assert(v1 != null, "v1 != null");
            X = input.Width*(v2.Value.Width/v1.Value.Width);
            Y = input.Height*(v2.Value.Height/v1.Value.Height);

            return new SizeF(X, Y);
        }

        private Vector2 LockToPixel(Vector2 input)
        {
            if (this.Options.LockToPixel)
            {
                float r = this.Options.LockToPixelRatio;
                return new Vector2((1 - r)*input.X + r*((int) Math.Round(input.X)),
                                   (1 - r)*input.Y + r*((int) Math.Round(input.Y)));
            }
            return input;
        }

        private Vector3 TransformToViewport(Vector3 input)
        {
            return new Vector3(LockToPixel(TransformPositionToViewport(input.Xy))) {Z = input.Z};
        }

        public SizeF Print(string text, Vector3 position, QFontAlignment alignment, Rectangle clippingRectangle = default(Rectangle))
        {
            PrintOffset = TransformToViewport(position);
            return PrintOrMeasure(text, alignment, false, clippingRectangle);
        }

        public SizeF Print(string text, Vector3 position, QFontAlignment alignment, Color color, Rectangle clippingRectangle = default(Rectangle))
        {
            this.Options.Colour = color;
            PrintOffset = TransformToViewport(position);
            return PrintOrMeasure(text, alignment, false, clippingRectangle);
        }

        public SizeF Print(string text, Vector3 position, SizeF maxSize, QFontAlignment alignment, Rectangle clippingRectangle = default(Rectangle))
        {
            ProcessedText processedText = ProcessText(_font, Options, text, maxSize, alignment);
            return Print(processedText, TransformToViewport(position), clippingRectangle);
        }

        public SizeF Print(string text, Vector3 position, SizeF maxSize, QFontAlignment alignment, Color colour, Rectangle clippingRectangle = default(Rectangle))
        {
            ProcessedText processedText = ProcessText(_font, Options, text, maxSize, alignment);
            return Print(processedText, TransformToViewport(position), colour, clippingRectangle);
        }

        public SizeF Print(ProcessedText processedText, Vector3 position, Rectangle clippingRectangle = default(Rectangle))
        {
            PrintOffset = TransformToViewport(position);
            return PrintOrMeasure(processedText, false, clippingRectangle);
        }

        public SizeF Print(ProcessedText processedText, Vector3 position, Color colour, Rectangle clippingRectangle = default(Rectangle))
        {
            this.Options.Colour = colour;
            PrintOffset = TransformToViewport(position);
            return PrintOrMeasure(processedText, false, clippingRectangle);
        }

        public SizeF Measure(string text, QFontAlignment alignment = QFontAlignment.Left)
        {
            return TransformMeasureFromViewport(PrintOrMeasure(text, alignment, true));
        }

        public SizeF Measure(string text, float maxWidth, QFontAlignment alignment)
        {
            return Measure(text, new SizeF(maxWidth, -1), alignment);
        }

        /// <summary>
        ///     Measures the actual width and height of the block of text.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="bounds"></param>
        /// <param name="alignment"></param>
        /// <returns></returns>
        public SizeF Measure(string text, SizeF maxSize, QFontAlignment alignment)
        {
            ProcessedText processedText = ProcessText(_font, Options, text, maxSize, alignment);
            return Measure(processedText);
        }

        /// <summary>
        ///     Measures the actual width and height of the block of text
        /// </summary>
        /// <param name="processedText"></param>
        /// <returns></returns>
        public SizeF Measure(ProcessedText processedText)
        {
            return TransformMeasureFromViewport(PrintOrMeasure(processedText, true));
        }

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
                    //maxCharHeight = maxCharHeight - LineSpacing;
                    //if (maxCharHeight < 0) maxCharHeight = 0;
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
                    if (c != ' ' && _font.FontData.CharSetMapping.ContainsKey(c))
                    {
                        if (!measureOnly)
                            RenderGlyph(xOffset, yOffset, c, _font, CurrentVertexRepr, clippingRectangle);
                    }

                    if (IsMonospacingActive)
                        xOffset += MonoSpaceWidth;
                    else
                    {
                        if (c == ' ')
                            xOffset += (float)Math.Ceiling(_font.FontData.meanGlyphWidth * this.Options.WordSpacing);
                            //normal character
                        else if (_font.FontData.CharSetMapping.ContainsKey(c))
                        {
                            QFontGlyph glyph = _font.FontData.CharSetMapping[c];
                            xOffset +=
                                (float)
                                Math.Ceiling(glyph.rect.Width + _font.FontData.meanGlyphWidth * this.Options.CharacterSpacing + _font.FontData.GetKerningPairCorrection(i, text, null));
                            maxCharHeight = Math.Max(maxCharHeight, glyph.rect.Height + glyph.yOffset);
                        }
                    }

                    maxXpos = Math.Max(xOffset, maxXpos);
                }
            }

            if (minXPos != float.MaxValue)
                maxWidth = maxXpos - minXPos;

            maxCharHeight = maxCharHeight - LineSpacing;
            if (maxCharHeight < 0) maxCharHeight = 0;
            LastSize = new SizeF(maxWidth, yOffset + LineSpacing);
            return LastSize;
        }

        private SizeF PrintOrMeasure(ProcessedText processedText, bool measureOnly, Rectangle clippingRectangle = default(Rectangle))
        {
            // init values we'll return
            float maxMeasuredWidth = 0f;
            float maxCharHeight = 0f;

            const float xPos = 0f;
            const float yPos = 0f;

            float xOffset = xPos;
            float yOffset = yPos;

            //make sure fontdata font's options are synced with the actual options
            ////if (_font.FontData.dropShadowFont != null && _font.FontData.dropShadowFont.Options != this.Options)
            ////{
            ////    _font.FontData.dropShadowFont.Options = this.Options;
            ////}

            float maxWidth = processedText.maxSize.Width;
            QFontAlignment alignment = processedText.alignment;


            //TODO - use these instead of translate when rendering by position (at some point)

            TextNodeList nodeList = processedText.textNodeList;
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
                    if (this.Options.WordWrap && SkipTrailingSpace(node, length, maxWidth) && atLeastOneNodeCosumedOnLine)
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
                    else if (this.Options.WordWrap)
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
                    if (processedText.maxSize.Height > 0 &&
                        yOffset + LineSpacing - yPos >= processedText.maxSize.Height)
                        break;
                    //maxCharHeight = maxCharHeight - LineSpacing;
                    //if (maxCharHeight < 0) maxCharHeight = 0;
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
                if (_font.FontData.CharSetMapping.ContainsKey(c))
                {
                    QFontGlyph glyph = _font.FontData.CharSetMapping[c];

                    RenderGlyph(x, y, c, _font, CurrentVertexRepr, clippingRectangle);

                    if (IsMonospacingActive)
                        x += MonoSpaceWidth;
                    else
                        x +=
                            (int)
                            Math.Ceiling(glyph.rect.Width + _font.FontData.meanGlyphWidth * this.Options.CharacterSpacing + _font.FontData.GetKerningPairCorrection(i, node.Text, node));

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
        ///     Computes the length of the next line, and whether the line is valid for
        ///     justification.
        /// </summary>
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
                                (extraLength + length - targetLength)*this.Options.JustifyContractionPenalty <
                                (targetLength - length) &&
                                ((targetLength - (length + extraLength + 1))/targetLength > -this.Options.JustifyCapContract);

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
                        if (totalPixels/targetLength < -this.Options.JustifyCapContract)
                            totalPixels = (int) (-this.Options.JustifyCapContract*targetLength);
                    }
                    else
                    {
                        if (totalPixels/targetLength > this.Options.JustifyCapExpand)
                            totalPixels = (int) (this.Options.JustifyCapExpand*targetLength);
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
                                (int) (totalPixels*this.Options.JustifyCharacterWeightForContract*charGaps/spaceGaps);
                        else
                            charPixels = (int) (totalPixels*this.Options.JustifyCharacterWeightForExpand*charGaps/spaceGaps);


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
        /// <param name="node"></param>
        /// <param name="lengthSoFar"></param>
        /// <param name="boundWidth"></param>
        /// <returns></returns>
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
        /// <param name="text"></param>
        /// <param name="bounds"></param>
        /// <returns></returns>
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
            processedText.textNodeList = nodeList;
            processedText.maxSize = maxSize;
            processedText.alignment = alignment;


            return processedText;
        }
    }
}