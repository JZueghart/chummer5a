using System.Text.RegularExpressions;

namespace Chummer.RegularExpressions;

public static partial class Weapons
{
    [GeneratedRegex
        (
        "^[0-9]*[0-9]*x",
        RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.CultureInvariant
        )
    ]
    public static partial Regex AmmoCapacityFirstPattern();

    [GeneratedRegex
        (
            @"(?<=\))(x[0-9]*[0-9]*$)*",
            RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.CultureInvariant
        )
    ]
    public static partial Regex AmmoCapacitySecondPattern();
}
