using System.Globalization;
using System.Text.RegularExpressions;

namespace AriaVPN.Tests;

/// <summary>
/// Guards the visual hierarchy of the custom theme palettes in App.axaml.
/// </summary>
internal static class ThemePaletteTests
{
    private static readonly string[] NonContrastThemes = ["Light", "Dark", "AriaLight", "AriaDark"];

    public static void Run()
    {
        var xaml = ReadAppXaml();
        if (xaml is null)
        {
            Program.Check("App.axaml is available to palette tests", false);
            return;
        }

        foreach (var theme in NonContrastThemes)
        {
            var block = FindThemeBlock(xaml, theme);
            Program.Check($"{theme} defines a palette", block is not null);
            if (block is null)
            {
                continue;
            }

            CheckBackgroundSteps(theme, block);
            CheckTextContrast(theme, block);
        }

        CheckContrastPalettes(xaml);
    }

    private static void CheckBackgroundSteps(string theme, string block)
    {
        var levels = ReadBackgrounds(block);
        Program.Check($"{theme} defines five background levels", levels.Count == 5);
        if (levels.Count != 5)
        {
            return;
        }

        var brightness = levels.OrderBy(level => level.Key).Select(level => Brightness(level.Value)).ToArray();
        var cardStep = brightness[1] - brightness[0];
        Program.Check($"{theme} card is subtly lighter than the window", cardStep is >= 0.02 and <= 0.10);

        var isDark = theme is "Dark" or "AriaDark";
        var deeperLevelsStep = isDark
            ? brightness[2] > brightness[1] && brightness[3] > brightness[2] && brightness[4] > brightness[3]
            : brightness[2] < brightness[1] && brightness[3] < brightness[2] && brightness[4] < brightness[3];
        Program.Check($"{theme} deeper surfaces step away from the card", deeperLevelsStep);
    }

    private static void CheckTextContrast(string theme, string block)
    {
        var background = ReadColor(block, "SemiColorBackground0");
        var text = ReadColor(block, "SemiColorText0");
        Program.Check($"{theme} has a readable primary text color",
            background is not null && text is not null && Contrast(background, text) >= 7);
    }

    private static void CheckContrastPalettes(string xaml)
    {
        CheckContrastPalette(xaml, "ContrastLight", "#FFFFFF", "#000000");
        CheckContrastPalette(xaml, "ContrastDark", "#000000", "#FFFFFF");
    }

    private static void CheckContrastPalette(string xaml, string theme, string expectedBackground, string expectedBorder)
    {
        var block = FindThemeBlock(xaml, theme);
        if (block is null)
        {
            Program.Check($"{theme} defines a palette", false);
            return;
        }

        var background = ReadColor(block, "SemiColorBackground0");
        var border = ReadColor(block, "SemiColorBorder");
        Program.Check($"{theme} keeps its high-contrast background", background == expectedBackground);
        Program.Check($"{theme} keeps strong borders", border == expectedBorder);
    }

    private static string? ReadAppXaml()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, "v2rayN", "v2rayN.Desktop", "App.axaml");
            if (File.Exists(candidate))
            {
                return File.ReadAllText(candidate);
            }

            directory = directory.Parent;
        }

        return null;
    }

    private static string? FindThemeBlock(string xaml, string theme)
    {
        var key = theme is "Light" or "Dark"
            ? $"x:Key=\"{theme}\""
            : $"x:Key=\"{{x:Static common:AriaTheme.{theme}}}\"";
        var start = xaml.IndexOf(key, StringComparison.Ordinal);
        if (start < 0)
        {
            return null;
        }

        var end = xaml.IndexOf("</ResourceDictionary>", start, StringComparison.Ordinal);
        return end < 0 ? null : xaml[start..end];
    }

    private static Dictionary<int, string> ReadBackgrounds(string block)
    {
        var result = new Dictionary<int, string>();
        foreach (Match match in Regex.Matches(block, "x:Key=\"SemiColorBackground(?<level>[0-4])\"\\s+Color=\"(?<color>#[0-9A-Fa-f]{6})\""))
        {
            var level = int.Parse(match.Groups["level"].Value, CultureInfo.InvariantCulture);
            result[level] = match.Groups["color"].Value.ToUpperInvariant();
        }

        return result;
    }

    private static string? ReadColor(string block, string key)
    {
        var match = Regex.Match(block, $"x:Key=\"{Regex.Escape(key)}\"\\s+Color=\"(?<color>#[0-9A-Fa-f]{{6}})\"");
        return match.Success ? match.Groups["color"].Value.ToUpperInvariant() : null;
    }

    private static double Brightness(string color)
    {
        var (r, g, b) = ParseRgb(color);
        return 0.299 * r + 0.587 * g + 0.114 * b;
    }

    private static double Contrast(string first, string second)
    {
        var light = Luminance(first);
        var dark = Luminance(second);
        if (light < dark)
        {
            (light, dark) = (dark, light);
        }

        return (light + 0.05) / (dark + 0.05);
    }

    private static double Luminance(string color)
    {
        var (r, g, b) = ParseRgb(color);
        return 0.2126 * Linear(r) + 0.7152 * Linear(g) + 0.0722 * Linear(b);
    }

    private static (double R, double G, double B) ParseRgb(string color)
    {
        return (
            int.Parse(color.Substring(1, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture) / 255.0,
            int.Parse(color.Substring(3, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture) / 255.0,
            int.Parse(color.Substring(5, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture) / 255.0);
    }

    private static double Linear(double channel)
    {
        return channel <= 0.04045 ? channel / 12.92 : Math.Pow((channel + 0.055) / 1.055, 2.4);
    }
}
