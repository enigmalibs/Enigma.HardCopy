using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Enigma.HardCopy.Recover.Resources;

namespace Enigma.HardCopy.Recover.ViewModels;

/// <summary>
/// Writes a list of code indexes the way a person reads one off a phone screen: <c>3, 5–8, 11</c>.
/// </summary>
/// <remarks>
/// A phone's width holds a few dozen characters, and a backup missing a run of forty codes would otherwise list
/// forty numbers. Runs collapse into ranges, and past a handful of ranges the rest is summarised as a count — a
/// user hunting for pages can act on the first few; the rest is noise until those are found.
/// </remarks>
public static class IndexRanges
{
    /// <summary>Formats ascending, distinct indexes as ranges.</summary>
    /// <param name="ascending">The indexes, ascending and without duplicates.</param>
    /// <param name="maxRanges">How many ranges to write before summarising the rest as a count.</param>
    /// <returns>The text; empty when <paramref name="ascending"/> is.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="ascending"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxRanges"/> is not positive.</exception>
    public static string Format(IReadOnlyList<int> ascending, int maxRanges)
    {
        ArgumentNullException.ThrowIfNull(ascending);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxRanges);

        StringBuilder text = new();
        int ranges = 0;
        int index = 0;
        while (index < ascending.Count)
        {
            int first = ascending[index];
            int last = first;
            while (index + 1 < ascending.Count && ascending[index + 1] == last + 1)
            {
                index++;
                last = ascending[index];
            }

            if (ranges == maxRanges)
            {
                int remaining = ascending.Count - CountUpTo(ascending, first);

                return string.Format(CultureInfo.CurrentCulture, Strings.MissingOverflowFormat, text, remaining);
            }

            if (ranges > 0)
            {
                text.Append(", ");
            }

            text.Append(first.ToString(CultureInfo.CurrentCulture));
            if (last == first + 1)
            {
                // Two in a row read better as a pair than as a range: "4, 5" rather than "4–5".
                text.Append(", ").Append(last.ToString(CultureInfo.CurrentCulture));
            }
            else if (last > first)
            {
                text.Append('–').Append(last.ToString(CultureInfo.CurrentCulture));
            }

            ranges++;
            index++;
        }

        return text.ToString();
    }

    /// <summary>How many of the indexes come before <paramref name="value"/>.</summary>
    private static int CountUpTo(IReadOnlyList<int> ascending, int value)
    {
        int count = 0;
        while (count < ascending.Count && ascending[count] < value)
        {
            count++;
        }

        return count;
    }
}
