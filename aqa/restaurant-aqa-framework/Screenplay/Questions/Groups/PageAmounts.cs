using System.Globalization;
using System.Text.RegularExpressions;

namespace Restaurant_Aqa_Framework.Screenplay.Questions.Groups;

/// <summary>Reads the last "$12.34" amount out of a piece of page text.</summary>
public static partial class PageAmounts
{
    public static decimal? LastAmount(string text)
    {
        var matches = AmountPattern().Matches(text);
        return matches.Count == 0
            ? null
            : decimal.Parse(matches[^1].Groups[1].Value.Replace(",", "", StringComparison.Ordinal), CultureInfo.InvariantCulture);
    }

    [GeneratedRegex(@"\$\s?([\d,]+\.\d{2})")]
    private static partial Regex AmountPattern();
}
