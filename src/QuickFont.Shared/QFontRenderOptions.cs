using System.Drawing;
using OpenTK;

namespace QuickFont
{
    /// <summary>
    /// Text Alignment
    /// </summary>
    public enum QFontAlignment
    {
        /// <summary>
        /// Left-aligned text
        /// </summary>
        Left=0,

        /// <summary>
        /// Right-aligned text
        /// </summary>
        Right,

        /// <summary>
        /// Centred text
        /// </summary>
        Centre,

        /// <summary>
        /// Justified text
        /// </summary>
        Justify
    }

    /// <summary>
    /// Monospace handling
    /// </summary>
    public enum QFontMonospacing
    {
        /// <summary>
        /// Monospace only if the font is monospaced
        /// </summary>
        Natural = 0,

        /// <summary>
        /// Force monospacing
        /// </summary>
        Yes,

        /// <summary>
        /// Don't monospace
        /// </summary>
        No
    }

    /// <summary>
    /// Contains options used for font rendering
    /// </summary>
    public class QFontRenderOptions
    {
        /// <summary>
        /// The font colour
        /// </summary>
        public Color Colour = Color.White;

        /// <summary>
        /// Spacing between characters in units of average glyph width
        /// </summary>
        public float CharacterSpacing = 0.05f;

        /// <summary>
        /// Spacing between words in units of average glyph width
        /// </summary>
        public float WordSpacing = 0.9f;

        /// <summary>
        /// Line spacing in units of max glyph width
        /// </summary>
        public float LineSpacing = 1.0f;

        /// <summary>
        /// Whether to draw a drop-shadow. Note: this requires
        /// the QFont to have been loaded with a drop shadow to
        /// take effect.
        /// </summary>
        public bool DropShadowActive;

        /// <summary>
        /// Offset of the shadow from the font glyphs in units of average glyph width
        /// </summary>
        public Vector2 DropShadowOffset = new Vector2(0.16f, 0.16f);

        /// <summary>
        /// Opacity of drop shadows
        /// </summary>
        public Color DropShadowColour = Color.FromArgb(128, Color.Black);

        /// <summary>
        /// Set the opacity of the drop shadow
        /// </summary>
        public float DropShadowOpacity
        {
            set
            {
                DropShadowColour = Color.FromArgb((byte)(value * 255), Color.Black);
            }
        }

        /// <summary>
        /// Whether to render the font in monospaced mode. If set to "Natural", then 
        /// monospacing will be used if the font loaded font was detected to be monospaced.
        /// </summary>
        public QFontMonospacing Monospacing = QFontMonospacing.Natural;

        /// <summary>
        /// This is intended as a means of rendering text pixel-perfectly at a 
        /// fixed display size (size on screen) independent of the screen resolution.
        /// 
        /// Ordinarily it is possible to render pixel-perfect text by calling 
        /// QFont.Begin() / QFont.End(); however, this means working in a coordinate 
        /// system corresponding to the current screen resolution. If the screen 
        /// resolution changes, then the display size of the font will change 
        /// accordingly which may not be desirable. Many games/applications prefer 
        /// to use a fixed orthog coordinate system that is independent of screen 
        /// resolution (e.g. 1000x1000) so that when the screen resolution changes,
        /// everything is still the same size on screen, it simply has higher
        /// definition - which is what this setting supports.
        /// 
        /// One option is simply not to call QFont.Begin() / QFont.End(). This
        /// works; however, it becomes impossible to assure that glyphs are
        /// rendered pixel-perfectly. Instead they will be scaled in hardware.
        /// In most cases this looks fine; however, if you are a perfectionist, 
        /// you may prefer to use this option to assure pixel perfection.
        /// 
        /// Setting this option does two things:
        /// 
        /// Rendering to a specified position is transformed
        /// Measurements are transformed
        /// 
        /// So for example, suppose the screen resolution is 1024x768, but you 
        /// wish to run orthog mode at 1000x1000.  If you set:
        /// 
        /// myFont.Options.TransformToViewport = new Viewport(0,0,1000,1000);
        ///
        /// Then, if you render at position 500,500:
        /// 
        /// QFont.Begin();
        /// myFont.Options.LockToPixel = true;
        /// myFont.Print("Hello",new Vector2(500,500));
        /// QFont.End();
        ///
        /// This will be printed pixel-pefectly at pixel position 512, 384.
        /// 
        /// Additionally the font will be measured in terms of your 500x500 
        /// coordinate system.
        /// 
        /// The only issue is that if you change the resolution, the size of your
        /// font will change. You can get around this by loading a font size
        /// that is proportional to the screen resolution. This makes sense:
        /// if you want a font to be rendered pixel-perfectly at a higher
        /// resolution, it will need to be a larger font. At present this 
        /// needs to be doen manually. E.g:
        /// 
        ///    float fontScale = (float)Height / 800;
        ///    compyFontSmall = new QFont("Data/comfy.ttf", 14 * fontScale);
        /// 
        /// 
        /// </summary>
        public Viewport? TransformToViewport;

        /// <summary>
        /// Locks the position to a particular pixel, allowing the text to be rendered pixel-perfectly.
        /// You need to turn this off if you wish to move text around the screen smoothly by fractions 
        /// of a pixel.
        /// </summary>
        public bool LockToPixel;

        /// <summary>
        /// Only applies when LockToPixel is true:
        /// This is used to transition smoothly between being locked to pixels and not
        /// </summary>
        public float LockToPixelRatio = 1.0f;

        /// <summary>
        /// Whether to always set :
        /// GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.DstAlpha);
        /// before rendering text.
        /// 
        /// </summary>
        public bool UseDefaultBlendFunction = true;

        /// <summary>
        /// Wrap word to next line if max width hit
        /// </summary>
        public bool WordWrap = true;

        /// <summary>
        /// When a line of text is justified, space may be inserted between
        /// characters, and between words. 
        /// 
        /// This parameter determines how this choice is weighted:
        /// 
        /// 0.0f => word spacing only
        /// 1.0f => "fairly" distributed between both
        /// > 1.0 => in favour of character spacing
        /// 
        /// This applies to expansions only.
        /// 
        /// </summary>
        public float JustifyCharacterWeightForExpand
        {
            get { return _justifyCharWeightForExpand; }
            set { 

                _justifyCharWeightForExpand = MathHelper.Clamp(value, 0.0f, 1.0f);
            }
        }

        private float _justifyCharWeightForExpand = 0.5f;

        /// <summary>
        /// When a line of text is justified, space may be removed between
        /// characters, and between words. 
        /// 
        /// This parameter determines how this choice is weighted:
        /// 
        /// 0.0f => word spacing only
        /// 1.0f => "fairly" distributed between both
        /// > 1.0 => in favour of character spacing
        /// 
        /// This applies to contractions only.
        /// 
        /// </summary>
        public float JustifyCharacterWeightForContract
        {
            get { return _justifyCharWeightForContract; }
            set
            {
                _justifyCharWeightForContract = MathHelper.Clamp(value, 0.0f, 1.0f);
            }
        }

        private float _justifyCharWeightForContract = 0.2f;

        /// <summary>
        /// Total justification cap as a fraction of the boundary width.
        /// </summary>
        public float JustifyCapExpand = 0.5f;

        /// <summary>
        /// Total justification cap as a fraction of the boundary width.
        /// </summary>
        public float JustifyCapContract = 0.1f;

        /// <summary>
        /// By what factor justification is penalized for being negative.
        /// 
        /// (e.g. if this is set to 3, then a contraction will only happen
        /// over an expansion if it is 3 of fewer times smaller than the
        /// expansion).
        /// 
        /// 
        /// </summary>
        public float JustifyContractionPenalty = 2;

        /// <summary>
        /// The clipping rectangle to use for rendering
        /// </summary>
        public Rectangle ClippingRectangle = default(Rectangle);

        /// <summary>
        /// Creates a clone of the render options
        /// </summary>
        /// <returns>The clone of the render options</returns>
        public QFontRenderOptions CreateClone()
        {
            return new QFontRenderOptions
            {
                Colour = Colour,
                CharacterSpacing = CharacterSpacing,
                WordSpacing = WordSpacing,
                LineSpacing = LineSpacing,
                DropShadowActive = DropShadowActive,
                DropShadowOffset = DropShadowOffset,
                DropShadowColour = DropShadowColour,
                Monospacing = Monospacing,
                TransformToViewport = TransformToViewport,
                LockToPixel = LockToPixel,
                LockToPixelRatio = LockToPixelRatio,
                UseDefaultBlendFunction = UseDefaultBlendFunction,
                JustifyCharacterWeightForExpand = JustifyCharacterWeightForExpand,
                _justifyCharWeightForExpand = _justifyCharWeightForExpand,
                JustifyCharacterWeightForContract = JustifyCharacterWeightForContract,
                _justifyCharWeightForContract = _justifyCharWeightForContract,
                JustifyCapExpand = JustifyCapExpand,
                JustifyCapContract = JustifyCapContract,
                JustifyContractionPenalty = JustifyContractionPenalty,
                ClippingRectangle = ClippingRectangle
            };
        }
    }
}
