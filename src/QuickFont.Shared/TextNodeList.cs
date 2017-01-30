using System;
using System.Collections;
using System.Drawing;
using System.Text;

namespace QuickFont
{
    /// <summary>
    /// The Text Node Type
    /// </summary>
    enum TextNodeType
    {
        /// <summary>
        /// Word
        /// </summary>
        Word,

        /// <summary>
        /// Line Break
        /// </summary>
        LineBreak,

        /// <summary>
        /// Space
        /// </summary>
        Space
    }

    /// <summary>
    /// A Text Node
    /// </summary>
    class TextNode
    {
        /// <summary>
        /// Text node type
        /// </summary>
        public TextNodeType Type;

        /// <summary>
        /// Text node text
        /// </summary>
        public string Text;

        /// <summary>
        /// The length of this text node (in pixels, without tweaks)
        /// </summary>
        public float Length;

        /// <summary>
        /// The length tweaks of this text node (in pixels, teaks for justification)
        /// </summary>
        public float LengthTweak;

        /// <summary>
        /// The height of this text node
        /// </summary>
        public float Height;

        /// <summary>
        /// The modified length of this text node
        /// </summary>
        public float ModifiedLength
        {
            get { return Length + LengthTweak; }
        }

        /// <summary>
        /// Creates a new instance of <see cref="TextNode"/>
        /// </summary>
        /// <param name="type">The text node type</param>
        /// <param name="text">The text node text</param>
        public TextNode(TextNodeType type, string text){
            Type = type;
            Text = text;
        }

        /// <summary>
        /// The next text node
        /// </summary>
        public TextNode Next;

        /// <summary>
        /// The previous text node
        /// </summary>
        public TextNode Previous;

    }

    /// <summary>
    /// Class to hide TextNodeList and related classes from 
    /// user whilst allowing a textNodeList to be passed around.
    /// </summary>
    public class ProcessedText
    {
        internal TextNodeList TextNodeList;
        internal SizeF MaxSize;
        internal QFontAlignment Alignment;
    }

    /// <summary>
    /// A doubly linked list of text nodes
    /// </summary>
    class TextNodeList : IEnumerable
    {
        /// <summary>
        /// The head of the text node linked list
        /// </summary>
        public TextNode Head;

        /// <summary>
        /// The tail of the text node linked list
        /// </summary>
        public TextNode Tail;

        /// <summary>
        /// Builds a doubly linked list of text nodes from the given input string
        /// </summary>
        /// <param name="text"></param>
        public TextNodeList(string text)
        {
            #region parse text

            text = text.Replace("\r\n", "\r");

            bool wordInProgress = false;
            StringBuilder currentWord = new StringBuilder();

            foreach (char t in text)
            {
                if (t == '\r' || t == '\n' || t == ' ')
                {
                    if (wordInProgress)
                    {
                        Add(new TextNode(TextNodeType.Word, currentWord.ToString()));
                        wordInProgress = false;
                    }

                    if (t == '\r' || t == '\n')
                        Add(new TextNode(TextNodeType.LineBreak, null));
                    else if (t == ' ')
                        Add(new TextNode(TextNodeType.Space, null));

                }
                else
                {
                    if (!wordInProgress)
                    {
                        wordInProgress = true;
                        currentWord = new StringBuilder();
                    }

                    currentWord.Append(t);
                }
            }

            if (wordInProgress)
                Add(new TextNode(TextNodeType.Word, currentWord.ToString()));

            #endregion
        }

        /// <summary>
        /// Measures each text node using the specified font data and render options
        /// </summary>
        /// <param name="fontData">The font data to use for measuring</param>
        /// <param name="options">The render options</param>
        public void MeasureNodes(QFontData fontData, QFontRenderOptions options){
            
            foreach(TextNode node in this){
                if(Math.Abs(node.Length) < float.Epsilon)
                    node.Length = MeasureTextNodeLength(node,fontData,options);
            }
        }

        /// <summary>
        /// Measures the length of a text node
        /// </summary>
        /// <param name="node">The text node to measure</param>
        /// <param name="fontData">The font data to use for measuring</param>
        /// <param name="options">The render options</param>
        /// <returns>The length of the text node</returns>
        private float MeasureTextNodeLength(TextNode node, QFontData fontData, QFontRenderOptions options)
        {

            bool monospaced = fontData.IsMonospacingActive(options);
            float monospaceWidth = fontData.GetMonoSpaceWidth(options);

            if (node.Type == TextNodeType.Space)
            {
                if (monospaced)
                    return monospaceWidth;

                return (float)Math.Ceiling(fontData.MeanGlyphWidth * options.WordSpacing);
            }

            float length = 0f;
            float height = 0f;
            if (node.Type == TextNodeType.Word)
            {
                
                for (int i = 0; i < node.Text.Length; i++)
                {
                    char c = node.Text[i];
                    if (fontData.CharSetMapping.ContainsKey(c))
                    {
                        var glyph = fontData.CharSetMapping[c];
                        if (monospaced)
                            length += monospaceWidth;
                        else
                            length += (float)Math.Ceiling(fontData.CharSetMapping[c].Rect.Width + fontData.MeanGlyphWidth * options.CharacterSpacing + fontData.GetKerningPairCorrection(i, node.Text, node));
                        height = Math.Max(height, glyph.YOffset + glyph.Rect.Height);
                    }
                }
            }
            node.Height = height;
            return length;
        }

        /// <summary>
        /// Splits a word into sub-words of size less than or equal to baseCaseSize 
        /// </summary>
        /// <param name="node"></param>
        /// <param name="baseCaseSize"></param>
        public void Crumble(TextNode node, int baseCaseSize){

            //base case
            if(node.Text.Length <= baseCaseSize )
                return;
 
            var left = SplitNode(node);
            var right = left.Next;

            Crumble(left,baseCaseSize);
            Crumble(right,baseCaseSize);

        }

        /// <summary>
        /// Splits a word node in two, adding both new nodes to the list in sequence.
        /// </summary>
        /// <param name="node"></param>
        /// <returns>The first new node</returns>
        public TextNode SplitNode(TextNode node)
        {
            if (node.Type != TextNodeType.Word)
                throw new Exception("Cannot slit text node of type: " + node.Type);

            int midPoint = node.Text.Length / 2;

            string newFirstHalf = node.Text.Substring(0, midPoint);
            string newSecondHalf = node.Text.Substring(midPoint, node.Text.Length - midPoint);

            TextNode newFirst = new TextNode(TextNodeType.Word, newFirstHalf);
            TextNode newSecond = new TextNode(TextNodeType.Word, newSecondHalf);
            newFirst.Next = newSecond;
            newSecond.Previous = newFirst;

            //node is head
            if (node.Previous == null)
                Head = newFirst;
            else
            {
                node.Previous.Next = newFirst;
                newFirst.Previous = node.Previous;
            }

            //node is tail
            if (node.Next == null)
                Tail = newSecond;
            else
            {
                node.Next.Previous = newSecond;
                newSecond.Next = node.Next;
            }

            return newFirst;
        }

        /// <summary>
        /// Adds a node to the head of the doubly linked list
        /// </summary>
        /// <param name="node">The node to add</param>
        public void Add(TextNode node)
        {
            //new node is head (and tail)
            if(Head == null){
                Head = node;
                Tail = node;
            } else {
                Tail.Next = node;
                node.Previous = Tail;
                Tail = node;
            }
        }

        /// <summary>
        /// Returns the string representation of this text node object
        /// </summary>
        /// <returns>The text representation</returns>
        public override string ToString()
        {
            StringBuilder builder = new StringBuilder();


           // for (var node = Head; node.Next != null; node = node.Next)

            foreach(TextNode node in this)
            {
                if (node.Type == TextNodeType.Space)
                    builder.Append(" ");
                if (node.Type == TextNodeType.LineBreak)
                    builder.Append(Environment.NewLine);
                if (node.Type == TextNodeType.Word)
                    builder.Append("" + node.Text + "");
            }

            return builder.ToString();
        }

        /// <summary>Returns an enumerator that iterates through a collection.</summary>
        /// <returns>An <see cref="T:System.Collections.IEnumerator" /> object that can be used to iterate through the collection.</returns>
        /// <filterpriority>2</filterpriority>
        public IEnumerator GetEnumerator()
        {
            return new TextNodeListEnumerator(this);
        }

        /// <summary>
        /// An enumerator for the text node list
        /// </summary>
        private sealed class TextNodeListEnumerator : IEnumerator
        {
            private TextNode _currentNode;
            private TextNodeList _targetList;

            public TextNodeListEnumerator(TextNodeList targetList)
            {
                _targetList = targetList;
            }

            /// <summary>Gets the current element in the collection.</summary>
            /// <returns>The current element in the collection.</returns>
            /// <filterpriority>2</filterpriority>
            public object Current
            {
                get { return _currentNode; }
            }

            /// <summary>Advances the enumerator to the next element of the collection.</summary>
            /// <returns>true if the enumerator was successfully advanced to the next element; false if the enumerator has passed the end of the collection.</returns>
            /// <exception cref="T:System.InvalidOperationException">The collection was modified after the enumerator was created. </exception>
            /// <filterpriority>2</filterpriority>
            public bool MoveNext()
            {
                if (_currentNode == null)
                    _currentNode = _targetList.Head;
                else
                    _currentNode = _currentNode.Next;
                return _currentNode != null;
            }

            /// <summary>Sets the enumerator to its initial position, which is before the first element in the collection.</summary>
            /// <exception cref="T:System.InvalidOperationException">The collection was modified after the enumerator was created. </exception>
            /// <filterpriority>2</filterpriority>
            public void Reset()
            {
                _currentNode = null;
            }
        }
    }
}
