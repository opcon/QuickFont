using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace QuickFont.Configuration
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
        public const string BasicSet = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890.:,;'\"(!?)+-*/=_{}[]@~#\\<>|^%$£&€°µ";
        public const string FrenchQuotes = "«»‹›";
        public const string SpanishQestEx = "¡¿";
        public const string CyrillicSet = "АБВГДЕЖЗИЙКЛМНОПРСТУФХЦЧШЩЪЫЬЭЮЯабвгдежзийклмнопрстуфхцчшщъыьэюяљњќћџЉЊЌЋЏ";
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
        public string charSet = BuildCharacterSet(FigureOutBestCharacterSet());

        private CharacterSet _characters = CharacterSet.BasicSet;

        public CharacterSet Characters
        {
            get { return _characters; }
            set
            {
                _characters = value;
                charSet = BuildCharacterSet(_characters);
            }
        }

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

        private static string BuildCharacterSet(CharacterSet set)
        {
            var characterSetValues = Enum.GetValues(typeof(CharacterSet));
            string result = "";
            foreach (CharacterSet value in characterSetValues)
            {
                if (set.HasFlag(value))
                {
                    switch (value)
                    {
                        case CharacterSet.BasicSet:
                            result += BasicSet;
                            break;
                        case CharacterSet.FrenchQuotes:
                            result += FrenchQuotes;
                            break;
                        case CharacterSet.SpanishQuestEx:
                            result += SpanishQestEx;
                            break;
                        case CharacterSet.CyrillicSet:
                            result += CyrillicSet;
                            break;
                        case CharacterSet.ExtendedLatin:
                            result += ExtendedLatin;
                            break;
                        case CharacterSet.GreekAlphabet:
                            result += GreekAlphabet;
                            break;
                        case CharacterSet.TurkishI:
                            result += TurkishI;
                            break;
                        case CharacterSet.HebrewAlphabet:
                            result += HebrewAlphabet;
                            break;
                        case CharacterSet.ArabicAlphabet:
                            result += ArabicAlphabet;
                            break;
                        case CharacterSet.ThaiKhmerAlphabet:
                            result += ThaiKhmerAlphabet;
                            break;
                        case CharacterSet.Hiragana:
                            result += Hiragana;
                            break;
                        case CharacterSet.JapDigits:
                            result += JapDigits;
                            break;
                        case CharacterSet.AsianQuotes:
                            result += AsianQuotes;
                            break;
                        case CharacterSet.EssentialKanji:
                            result += EssentialKanji;
                            break;
                        case CharacterSet.Katakana:
                            result += Katakana;
                            break;
                    }
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
        static CharacterSet FigureOutBestCharacterSet()
        {
            // he : 1255, de/en=1252, arab =ANSICodePage = 1256, bg/ru=1251, ko=949, ja =932, cz=1250, thai =847
            TextInfo textInfo = CultureInfo.CurrentCulture.TextInfo;
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
                // TODO : add hindi, malayalam and telugu
            }
            return CharacterSet.BasicSet;
        }
    }
}
