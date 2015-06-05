using System.Globalization;

namespace QuickFont
{
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
    /// What settings to use when building the font
    /// </summary>
    public class QFontBuilderConfiguration : QFontConfiguration
    {
        public const string BasicSet = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890.:,;'\"(!?)+-*/=_{}[]@~#\\<>|^%$£&€°µ";
        public const string FrenchQuotes = "«»‹›";
        public const string SpanishQestEx = "¡¿";
        public const string CyrillSet = "АБВГДЕЖЗИЙКЛМНОПРСТУФХЦЧШЩЪЫЬЭЮЯабвгдежзийклмнопрстуфхцчшщъыьэюяљњќћџЉЊЌЋЏ";
        public const string ExtendedLatin = "ÀŠŽŸžÁÂÃÄÅÆÇÈÉÊËÌÍÎÏÐÑÒÓÔÕÖØÙÚÛÜÝÞßàáâãäåæçèéêëìíîïðñòóôõöøùúûüýþÿ";
        public const string GreekAlphabet = "ΈΉΊΌΎΏΐΑΒΓΔΕΖΗΘΙΚΛΜΝΞΟΠΡΣΤΥΦΧΨΩΪΫάέήίΰαβγδεζηθικλμνξοπρςστυφχψωϊϋόύώ";
        public const string TurkishI = "ıİŞ";
        public const string HebrewAlphabet = "אבגדהוזחטיכךלמםנןסעפףצץקרשת";
        public const string ArabicAlphabet = "ںکگپچژڈ¯؛ہءآأؤإئابةتثجحخدذرزسشصض×طظعغـفقكàلâمنهوçèéêëىيîï؟";
        public const string ThaiKhmerAlphabet = "กขฃคฅฆงจฉชซฌญฎฏฐฑฒณดตถทธนบปผฝพฟภมยรฤลฦวศษสหฬอฮฯะัาำิีึืฺุู฿เแโใไๅๆ็่้๊๋์ํ๎๏๐๑๒๓๔๕๖๗๘๙๚๛";
        public const string Hiragana = "ぁあぃいぅうぇえぉおかがきぎくぐけげこごさざしじすずせぜそぞただちぢっつづてでとどなにぬねのはばぱひびぴふぶぷへべぺほぼぽまみむめもゃやゅゆょよらりるれろゎわゐゑをんゔゕゖ゗゘゙゛゜ゝゞゟ";
        public const string JapDigits = "㆐㆑㆒㆓㆔㆕㆖㆗㆘㆙㆚㆛㆜㆝㆞㆟";
        public const string AsianQuotes = "「」";
        public const string EssentialKanji = "⽇⽉";
        public const string Katakana = "゠ァアィイゥウェエォオカガキギクグケゲコゴサザシジスズセゼソゾタダチヂッツヅテデトドナニヌネノハバパヒビピフブプヘベペホボポマミムメモャヤュユョヨラリルレロヮワヰヱヲンヴヵヶヷヸヹヺ・ーヽヾヿ";

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
        public string charSet = FigureOutBestCharacterSet();

        /// <summary>
        /// Which render hint to use when rendering the ttf character set to create the QFont texture
        /// </summary>
        public TextGenerationRenderHint TextGenerationRenderHint = TextGenerationRenderHint.SizeDependent;

        public QFontBuilderConfiguration() { }

        public QFontBuilderConfiguration(bool addDropShadow, bool TransformToOrthogProjection = false) 
            : base(addDropShadow, TransformToOrthogProjection)
        {
        }

        public QFontBuilderConfiguration(QFontConfiguration fontConfiguration)
        {
            this.ShadowConfig = fontConfiguration.ShadowConfig;
            this.KerningConfig = fontConfiguration.KerningConfig;
            this.TransformToCurrentOrthogProjection = fontConfiguration.TransformToCurrentOrthogProjection;
        }

        /// <summary>
        /// Figures the out best character set to be assigned to charSet.
        /// Depends on current active culture. To be more general we'd use
        /// the code page and distinct the most used and practically usable
        /// cultures in terms of their characters in use. Obviuously traditional
        /// chinese can not be supported well. Due to Texture size limits and even wores due to the
        /// Kerning infos (which are n² size if n is number of characters).
        /// </summary>
        /// <returns></returns>
        static string FigureOutBestCharacterSet()
        {
            // he : 1255, de/en=1252, arab =ANSICodePage = 1256, bg/ru=1251, ko=949, ja =932, cz=1250, thai =847
            TextInfo textInfo = System.Globalization.CultureInfo.CurrentCulture.TextInfo;
            switch (textInfo.ANSICodePage)
            {
                case 1251: //stands for cyrillic writing systems like bu, ru, uk
                    return BasicSet + CyrillSet+FrenchQuotes;
                case 1257: //stands for baltic
                case 1252: //stands for western european writing systems as fr,es,de,nl,se... and most others
                    return BasicSet + ExtendedLatin + FrenchQuotes + SpanishQestEx;
                case 1253: //stands for greek and greek writing cultures.
                    return BasicSet + GreekAlphabet + FrenchQuotes;
                case 1254: //stands for turkish etc.
                    return BasicSet + ExtendedLatin + TurkishI;
                case 1255: // stands for hebrew (he) adds hebrew characters  (well, right-to left reading order is not supported TODO)
                    return BasicSet + HebrewAlphabet;
                case 1256: // stands for arabic writing cultures as north arfica and near east inc. persia 
                    // but it does not work properly becaues right-to left reading order is not supported by qfont. TODO
                    // Note this is not really supported since arabic has zero space combindig characters that are not supported (or are they?)
                    return BasicSet + ArabicAlphabet + FrenchQuotes;
                case 932: // stands for japanese - add hiragana and katakana characters plus some essential kanji
                    return BasicSet + Hiragana + Katakana + AsianQuotes + JapDigits + EssentialKanji;
                case 874: // stands for thai
                    // Note this is not really supported since thai has zero space combindig characters that are not supported (or are they?)
                    return BasicSet + ThaiKhmerAlphabet + FrenchQuotes;
                // TODO : add hindi, malayalam and telugu


            }
            return BasicSet;
        }
    }
}
