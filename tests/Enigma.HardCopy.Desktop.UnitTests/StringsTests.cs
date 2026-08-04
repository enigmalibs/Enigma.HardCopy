using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Enigma.HardCopy.Desktop.Resources;
using Xunit;

namespace Enigma.HardCopy.Desktop.UnitTests;

/// <summary>
/// Guards the contract between <see cref="Strings"/> and <c>Strings.resx</c>: every property is a key, and a
/// key that is not there throws. Reading them all here turns a missing string from a blank label in the running
/// application into a failing build.
/// </summary>
public sealed class StringsTests
{
    private static IReadOnlyList<PropertyInfo> StringProperties { get; } = typeof(Strings)
        .GetProperties(BindingFlags.Public | BindingFlags.Static)
        .Where(property => property.PropertyType == typeof(string))
        .ToArray();

    [Fact]
    public void StringProperties_AreNotEmpty_SoTheReflectionIsActuallyCoveringSomething()
        => Assert.NotEmpty(StringProperties);

    [Fact]
    public void EveryString_ResolvesToText()
    {
        List<string> problems = [];
        foreach (PropertyInfo property in StringProperties)
        {
            try
            {
                if (property.GetValue(null) is not string value || string.IsNullOrWhiteSpace(value))
                {
                    problems.Add($"{property.Name} resolves to nothing.");
                }
                else if (string.Equals(value, property.Name, StringComparison.Ordinal))
                {
                    problems.Add($"{property.Name} resolves to its own key.");
                }
            }
            catch (TargetInvocationException ex)
            {
                problems.Add($"{property.Name}: {ex.InnerException?.Message}");
            }
        }

        Assert.True(problems.Count == 0, string.Join(Environment.NewLine, problems));
    }

    [Fact]
    public void EveryFormatString_CarriesAtLeastOnePlaceholder()
    {
        List<string> problems = [];
        foreach (PropertyInfo property in StringProperties.Where(p => p.Name.EndsWith("Format", StringComparison.Ordinal)))
        {
            // "{0" rather than "{0}", because an argument may carry a format specifier of its own — "{0:N0}".
            if (property.GetValue(null) is string value && !value.Contains("{0", StringComparison.Ordinal))
            {
                problems.Add($"{property.Name} has no {{0}}: \"{value}\"");
            }
        }

        Assert.True(problems.Count == 0, string.Join(Environment.NewLine, problems));
    }
}
