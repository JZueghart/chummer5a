using System.Text.RegularExpressions;

namespace Chummer.RegularExpressions;

public static partial class ParameterAttributeExpressions
{
    [GeneratedRegex
        (
            @"FixedValues\(([^)]*)\)",
            RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.CultureInvariant
        )
    ]
    public static partial Regex FixedValuesPattern();

    [GeneratedRegex
        (
            @"\[([^\]]*)\]",
            RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.CultureInvariant
        )
    ]
    public static partial Regex SquareBracketsPattern();
}
