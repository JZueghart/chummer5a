using System.Text.RegularExpressions;

namespace Chummer.RegularExpressions;

public static partial class ExportCharacter
{
    [GeneratedRegex
        (
            "<mainmugshotbase64>[^\\s\\S]*</mainmugshotbase64>",
            RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.CultureInvariant
        )
    ]
    public static partial Regex MainMugshotReplacePattern();

    [GeneratedRegex
        (
            "<stringbase64>[^\\s\\S]*</stringbase64>",
            RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.CultureInvariant
        )
    ]
    public static partial Regex StringBase64ReplacePattern();

    [GeneratedRegex
        (
            "base64\": \"[^\\\"]*\",",
            RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.CultureInvariant
        )
    ]
    public static partial Regex Base64ReplacePattern();
}
