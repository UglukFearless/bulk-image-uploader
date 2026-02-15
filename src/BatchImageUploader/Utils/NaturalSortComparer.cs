using System.Text.RegularExpressions;

namespace BatchImageUploader.Utils;

public class NaturalSortComparer : IComparer<string?>
{
    private static readonly Regex FirstNumberRegex = new(@"\d+", RegexOptions.Compiled);

    public int Compare(string? x, string? y)
    {
        if (x == null && y == null)
        {
            return 0;
        }

        if (x == null)
        {
            return -1;
        }

        if (y == null)
        {
            return 1;
        }

        var xNumber = ExtractFirstNumber(x);
        var yNumber = ExtractFirstNumber(y);

        if (xNumber.HasValue && yNumber.HasValue)
        {
            return xNumber.Value.CompareTo(yNumber.Value);
        }

        if (xNumber.HasValue)
        {
            return -1;
        }

        if (yNumber.HasValue)
        {
            return 1;
        }

        return string.Compare(x, y, StringComparison.Ordinal);
    }

    private static int? ExtractFirstNumber(string input)
    {
        var match = FirstNumberRegex.Match(input);
        if (match.Success && int.TryParse(match.Value, out var number))
        {
            return number;
        }

        return null;
    }
}
