using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace QuickFont.Configuration
{
	/// <summary>
	/// Specifies the quality of rendering for fonts
	/// Only affects GDIFonts
	/// </summary>
    public enum TextGenerationRenderHint
    {
        /// <summary>
        /// Use AntiAliasGridFit when rendering the ttf character set to create the QFont texture
        /// </summary>
        AntiAliasGridFit,
        /// <summary>
        /// Use AntiAlias when rendering the ttf character set to create the QFont texture
        /// </summary>
        AntiAlias,
        /// <summary>
        /// Use ClearTypeGridFit if the font is smaller than 12, otherwise use AntiAlias
        /// </summary>
        SizeDependent,
        /// <summary>
        /// Use ClearTypeGridFit when rendering the ttf character set to create the QFont texture
        /// </summary>
        ClearTypeGridFit,
        /// <summary>
        /// Use SystemDefault when rendering the ttf character set to create the QFont texture
        /// </summary>
        SystemDefault
    }

	/// <summary>
	/// Flags which represent a character set for a language
	/// </summary>
    [Flags]
    public enum CharacterSet
    {
        /// <summary>
        ///  "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890.:,;'\"(!?)+-*/=_{}[]@~#\\&gt;&lt;|^%$£&amp;€°µ"
        /// </summary>
        BasicSet = 0,
        /// <summary>
        ///  "«»‹›"
        /// </summary>
        FrenchQuotes = 1 << 0,
        /// <summary>
        ///  "¡¿"
        /// </summary>
        SpanishQuestEx = 1 << 1,
        /// <summary>
        ///  "АБВГДЕЖЗИЙКЛМНОПРСТУФХЦЧШЩЪЫЬЭЮЯабвгдежзийклмнопрстуфхцчшщъыьэюяљњќћџЉЊЌЋЏ"
        /// </summary>
        CyrillicSet = 1 << 2,
        /// <summary>
        ///  "ÀŠŽŸžÁÂÃÄÅÆÇÈÉÊËÌÍÎÏÐÑÒÓÔÕÖØÙÚÛÜÝÞßàáâãäåæçèéêëìíîïðñòóôõöøùúûüýþÿ"
        /// </summary>
        ExtendedLatin = 1 << 3,
        /// <summary>
        /// "ΈΉΊΌΎΏΐΑΒΓΔΕΖΗΘΙΚΛΜΝΞΟΠΡΣΤΥΦΧΨΩΪΫάέήίΰαβγδεζηθικλμνξοπρςστυφχψωϊϋόύώ"
        /// </summary>
        GreekAlphabet = 1 << 4,
        /// <summary>
        /// "ıİŞ"
        /// </summary>
        TurkishI = 1 << 5,
        /// <summary>
        /// "אבגדהוזחטיכךלמםנןסעפףצץקרשת"
        /// </summary>
        HebrewAlphabet = 1 << 6,
        /// <summary>
        /// "ںکگپچژڈ¯؛ہءآأؤإئابةتثجحخدذرزسشصض×طظعغـفقكàلâمنهوçèéêëىيîï؟"
        /// </summary>
        ArabicAlphabet = 1 << 7,
        /// <summary>
        /// "กขฃคฅฆงจฉชซฌญฎฏฐฑฒณดตถทธนบปผฝพฟภมยรฤลฦวศษสหฬอฮฯะัาำิีึืฺุู฿เแโใไๅๆ็่้๊๋์ํ๎๏๐๑๒๓๔๕๖๗๘๙๚๛"
        /// </summary>
        ThaiKhmerAlphabet = 1 << 8,
        /// <summary>
        /// "ぁあぃいぅうぇえぉおかがきぎくぐけげこごさざしじすずせぜそぞただちぢっつづてでとどなにぬねのはばぱひびぴふぶぷへべぺほぼぽまみむめもゃやゅゆょよらりるれろゎわゐゑをんゔゕゖ゗゘゙゛゜ゝゞゟ"
        /// </summary>
        Hiragana = 1 << 9,
        /// <summary>
        /// "㆐㆑㆒㆓㆔㆕㆖㆗㆘㆙㆚㆛㆜㆝㆞㆟"
        /// </summary>
        JapDigits = 1 << 10,
        /// <summary>
        /// "「」"
        /// </summary>
        AsianQuotes = 1 << 11,
        /// <summary>
        /// "⽇⽉" 
        /// </summary>
        EssentialKanji = 1 << 12,
        /// <summary>
        /// "゠ァアィイゥウェエォオカガキギクグケゲコゴサザシジスズセゼソゾタダチヂッツヅテデトドナニヌネノハバパヒビピフブプヘベペホボポマミムメモャヤュユョヨラリルレロヮワヰヱヲンヴヵヶヷヸヹヺ・ーヽヾヿ"
        /// </summary>
        Katakana = 1 << 13,

        //Some common combinations of character sets.

        /// <summary>
        /// Includes the BasicSet, ExtendedLatin, FrenchQuotes and SpanishQuestEx
        /// </summary>
        General = BasicSet | ExtendedLatin | FrenchQuotes | SpanishQuestEx,
        /// <summary>
        /// Includes the BasicSet, CyrillicSet and FrenchQuotes
        /// </summary>
        Cyrillic = BasicSet | CyrillicSet | FrenchQuotes,
        /// <summary>
        /// Includes the BasicSet, GreekAlphabet and FrenchQuotes
        /// </summary>
        Greek = BasicSet | GreekAlphabet | FrenchQuotes,
        /// <summary>
        /// Includes the BasicSet, ExtendedLatin and TurkishI
        /// </summary>
        Turkish = BasicSet | ExtendedLatin | TurkishI,
        /// <summary>
        /// Includes the BasicSet and HebrewAlphabet
        /// </summary>
        Hebrew = BasicSet | HebrewAlphabet,
        /// <summary>
        /// Includes the BasicSet, ArabicAlphabet and FrenchQuotes
        /// </summary>
        Arabic = BasicSet | ArabicAlphabet | FrenchQuotes,
        /// <summary>
        /// Includes the BasicSet, Hiragana, Katakana, AsianQuotes, JapDigits and EssentialKanji
        /// </summary>
        Japanese = BasicSet | Hiragana | Katakana | AsianQuotes | JapDigits | EssentialKanji,
        /// <summary>
        /// Includes the BasicSet, ThaiKhmerAlphabet and FrenchQuotes
        /// </summary>
        Thai = BasicSet | ThaiKhmerAlphabet | FrenchQuotes

    }

    /// <summary>
    /// What settings to use when building the font
    /// </summary>
    public class QFontBuilderConfiguration : QFontConfiguration
    {
	    private const string BASIC_SET = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890.:,;'\"(!?)+-*/=_{}[]@~#\\<>|^%$£&€°µ";
	    private const string FRENCH_QUOTES = "«»‹›";
	    private const string SPANISH_QEST_EX = "¡¿";
	    private const string CYRILLIC_SET = "АБВГДЕЖЗИЙКЛМНОПРСТУФХЦЧШЩЪЫЬЭЮЯабвгдежзийклмнопрстуфхцчшщъыьэюяљњќћџЉЊЌЋЏ";
	    private const string EXTENDED_LATIN = "ÀŠŽŸžÁÂÃÄÅÆÇÈÉÊËÌÍÎÏÐÑÒÓÔÕÖØÙÚÛÜÝÞßàáâãäåæçèéêëìíîïðñòóôõöøùúûüýþÿ";
	    private const string GREEK_ALPHABET = "ΈΉΊΌΎΏΐΑΒΓΔΕΖΗΘΙΚΛΜΝΞΟΠΡΣΤΥΦΧΨΩΪΫάέήίΰαβγδεζηθικλμνξοπρςστυφχψωϊϋόύώ";
	    private const string TURKISH_I = "ıİŞ";
	    private const string HEBREW_ALPHABET = "אבגדהוזחטיכךלמםנןסעפףצץקרשת";
	    private const string ARABIC_ALPHABET = "ںکگپچژڈ¯؛ہءآأؤإئابةتثجحخدذرزسشصض×طظعغـفقكàلâمنهوçèéêëىيîï؟";
	    private const string THAI_KHMER_ALPHABET = "กขฃคฅฆงจฉชซฌญฎฏฐฑฒณดตถทธนบปผฝพฟภมยรฤลฦวศษสหฬอฮฯะัาำิีึืฺุู฿เแโใไๅๆ็่้๊๋์ํ๎๏๐๑๒๓๔๕๖๗๘๙๚๛";
	    private const string HIRAGANA = "ぁあぃいぅうぇえぉおかがきぎくぐけげこごさざしじすずせぜそぞただちぢっつづてでとどなにぬねのはばぱひびぴふぶぷへべぺほぼぽまみむめもゃやゅゆょよらりるれろゎわゐゑをんゔゕゖ゗゘゙゛゜ゝゞゟ";
	    private const string JAP_DIGITS = "㆐㆑㆒㆓㆔㆕㆖㆗㆘㆙㆚㆛㆜㆝㆞㆟";
	    private const string ASIAN_QUOTES = "「」";
	    private const string ESSENTIAL_KANJI = "⽇⽉";
	    private const string KATAKANA = "゠ァアィイゥウェエォオカガキギクグケゲコゴサザシジスズセゼソゾタダチヂッツヅテデトドナニヌネノハバパヒビピフブプヘベペホボポマミムメモャヤュユョヨラリルレロヮワヰヱヲンヴヵヶヷヸヹヺ・ーヽヾヿ";

        /// <summary>
        /// Whether to use super sampling when building font texture pages
        /// 
        /// 
        /// </summary>
        public int SuperSampleLevels = 1;

        /// <summary>
        /// The standard max width/height of 2D texture pages this OpenGl context wants to support
        /// 8129 sholud be a minimum. Exact value can be obtained with GL.GetInteger(GetPName.MaxTextureSize);
        /// page will automatically be cropped if there is extra space.
        /// </summary>
        public int PageMaxTextureSize = 4096;

        /// <summary>
        /// The margin (on all sides) around glyphs when rendered to
        /// their texture page
        /// </summary>
        public int GlyphMargin = 2;
       
        /// <summary>
        /// Set of characters to support
        /// </summary>
        public string CharSet = BuildCharacterSet(FigureOutBestCharacterSet());

        private CharacterSet _characters = CharacterSet.BasicSet;

		/// <summary>
		/// The character set for this builder configuration
		/// </summary>
        public CharacterSet Characters
        {
            get { return _characters; }
            set
            {
                _characters = value;
                CharSet = BuildCharacterSet(_characters);
            }
        }

        /// <summary>
        /// Which render hint to use when rendering the ttf character set to create the QFont texture
        /// </summary>
        public TextGenerationRenderHint TextGenerationRenderHint = TextGenerationRenderHint.SizeDependent;

		/// <summary>
		/// Creates a default configuration
		/// </summary>
        public QFontBuilderConfiguration() { }

		/// <summary>
		/// Creates a new <see cref="QFontBuilderConfiguration"/>
		/// </summary>
		/// <param name="addDropShadow">True to add drop shadow to the font</param>
		/// <param name="transformToOrthogProjection">OBSOLETE</param>
        public QFontBuilderConfiguration(bool addDropShadow, bool transformToOrthogProjection = false) 
            : base(addDropShadow, transformToOrthogProjection)
        {
        }

		/// <summary>
		/// Creates a new <see cref="QFontBuilderConfiguration"/>
		/// </summary>
		/// <param name="fontConfiguration">The existing font configuration to use as a base</param>
        public QFontBuilderConfiguration(QFontConfiguration fontConfiguration)
        {
            ShadowConfig = fontConfiguration.ShadowConfig;
            KerningConfig = fontConfiguration.KerningConfig;
            TransformToCurrentOrthogProjection = fontConfiguration.TransformToCurrentOrthogProjection;
        }

        private static string BuildCharacterSet(CharacterSet set)
        {
            var characterSetValues = Enum.GetValues(typeof(CharacterSet));
            var result = "";
            foreach (CharacterSet value in characterSetValues)
            {
	            if (!set.HasFlag(value)) continue;
	            switch (value)
	            {
		            case CharacterSet.BasicSet:
			            result += BASIC_SET;
			            break;
		            case CharacterSet.FrenchQuotes:
			            result += FRENCH_QUOTES;
			            break;
		            case CharacterSet.SpanishQuestEx:
			            result += SPANISH_QEST_EX;
			            break;
		            case CharacterSet.CyrillicSet:
			            result += CYRILLIC_SET;
			            break;
		            case CharacterSet.ExtendedLatin:
			            result += EXTENDED_LATIN;
			            break;
		            case CharacterSet.GreekAlphabet:
			            result += GREEK_ALPHABET;
			            break;
		            case CharacterSet.TurkishI:
			            result += TURKISH_I;
			            break;
		            case CharacterSet.HebrewAlphabet:
			            result += HEBREW_ALPHABET;
			            break;
		            case CharacterSet.ArabicAlphabet:
			            result += ARABIC_ALPHABET;
			            break;
		            case CharacterSet.ThaiKhmerAlphabet:
			            result += THAI_KHMER_ALPHABET;
			            break;
		            case CharacterSet.Hiragana:
			            result += HIRAGANA;
			            break;
		            case CharacterSet.JapDigits:
			            result += JAP_DIGITS;
			            break;
		            case CharacterSet.AsianQuotes:
			            result += ASIAN_QUOTES;
			            break;
		            case CharacterSet.EssentialKanji:
			            result += ESSENTIAL_KANJI;
			            break;
		            case CharacterSet.Katakana:
			            result += KATAKANA;
			            break;
	            }
            }
	        var hset = new HashSet<char>();
            foreach (var c in result)
            {
                hset.Add(c);
            }
            result = new string(hset.ToArray());
            return result;
        }

        /// <summary>
        /// Figures the out best character set to be assigned to charSet.
        /// Depends on current active culture. To be more general we'd use
        /// the code page and distinct the most used and practically usable
        /// cultures in terms of their characters in use. Obviuously traditional
        /// chinese can not be supported well. Due to Texture size limits and even worse due to the
        /// Kerning infos (which are n² size if n is number of characters).
        /// </summary>
        /// <returns></returns>
        private static CharacterSet FigureOutBestCharacterSet()
        {
            // he : 1255, de/en=1252, arab =ANSICodePage = 1256, bg/ru=1251, ko=949, ja =932, cz=1250, thai =847
            var textInfo = CultureInfo.CurrentCulture.TextInfo;
            switch (textInfo.ANSICodePage)
            {
                case 1251: //stands for cyrillic writing systems like bu, ru, uk
                    return CharacterSet.Cyrillic;
                case 1257: //stands for baltic
                case 1252: //stands for western european writing systems as fr,es,de,nl,se... and most others
                    return CharacterSet.General;
                case 1253: //stands for greek and greek writing cultures.
                    return CharacterSet.Greek;
                case 1254: //stands for turkish etc.
                    return CharacterSet.Turkish;
                case 1255: // stands for hebrew (he) adds hebrew characters  (well, right-to left reading order is not supported TODO)
                    return CharacterSet.Hebrew;
                case 1256: // stands for arabic writing cultures as north arfica and near east inc. persia 
                    // but it does not work properly becaues right-to left reading order is not supported by qfont. TODO
                    // Note this is not really supported since arabic has zero space combindig characters that are not supported (or are they?)
                    return CharacterSet.Arabic;
                case 932: // stands for japanese - add hiragana and katakana characters plus some essential kanji
                    return CharacterSet.Japanese;
                case 874: // stands for thai
                    // Note this is not really supported since thai has zero space combindig characters that are not supported (or are they?)
                    return CharacterSet.Thai;
            }
            return CharacterSet.BasicSet;
        }
    }
}
