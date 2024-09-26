using System.Text.RegularExpressions;

namespace Chummer.RegularExpressions;

public static partial class General
{
    [GeneratedRegex
        (
            @"/<\/?[a-z][\s\S]*>/i",
            RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.CultureInvariant
        )
    ]
    public static partial Regex HtmlTagsPattern();

    [GeneratedRegex
        (
            @"\r\n|\n\r|\n|\r",
            RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.CultureInvariant
        )
    ]
    public static partial Regex LineEndingsPattern();

    [GeneratedRegex
        (
            @"\\r\\n|\\n\\r|\\n|\\r",
            RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.CultureInvariant
        )
    ]
    public static partial Regex EscapedLineEndingsPattern();

    [GeneratedRegex
        (
            @"\\([a-z]{1,32})(-?\d{1,10})?[ ]?|\\'([0-9a-f]{2})|\\([^a-z])|([{}])|[\r\n]+|(.)",
            RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.CultureInvariant
        )
    ]
    public static partial Regex RtfStripperPattern();

    /// <summary>
    /// Regex that indicates whether a given string is a match for text that cannot be saved in XML. Match == true.
    /// </summary>
    [GeneratedRegex
        (
            @"[\u0000-\u0008\u000B\u000C\u000E-\u001F]",
            RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.CultureInvariant
        )
    ]
    public static partial Regex InvalidUnicodeCharsPattern();

    [GeneratedRegex
        (
            @"^(\[([a-z])+\.xml\])",
            RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.CultureInvariant
        )
    ]
    public static partial Regex ExtraFileSpecifierPattern();
}
