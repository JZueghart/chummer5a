using System.Text.RegularExpressions;

namespace Chummer.RegularExpressions;

public static partial class TranslateExceptionTelemetryProcessor
{
    [GeneratedRegex
        (
            @"\\{([0-9]+)\}",
            RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.CultureInvariant
        )
    ]
    public static partial Regex FirstReplacePattern();

    [GeneratedRegex
        (
            @"{([0-9]+)}",
            RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.CultureInvariant
        )
    ]
    public static partial Regex SecondReplacePattern();
}
