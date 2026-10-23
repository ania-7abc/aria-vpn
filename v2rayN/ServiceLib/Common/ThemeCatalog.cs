namespace ServiceLib.Common;

/// <summary>
/// Canonical theme names and their transitions.
/// Kept free of UI framework types so the rules can be reused and tested.
/// </summary>
public static class ThemeCatalog
{
    /// <summary>
    /// Themes offered by the theme picker, in display order.
    /// </summary>
    public static IReadOnlyList<string> SelectableThemes { get; } =
    [
        nameof(ETheme.FollowSystem),
        nameof(ETheme.Dark),
        nameof(ETheme.Light),
        nameof(ETheme.AriaSystem),
        nameof(ETheme.AriaDark),
        nameof(ETheme.AriaLight),
        nameof(ETheme.ContrastSystem),
        nameof(ETheme.ContrastDark),
        nameof(ETheme.ContrastLight),
    ];

    /// <summary>
    /// Converts stored or legacy theme names to a name from <see cref="SelectableThemes"/>.
    /// </summary>
    public static string Normalize(string? theme)
    {
        return theme switch
        {
            nameof(ETheme.FollowSystem) or nameof(ETheme.Dark) or nameof(ETheme.Light)
                or nameof(ETheme.AriaSystem) or nameof(ETheme.AriaLight) or nameof(ETheme.AriaDark)
                or nameof(ETheme.ContrastSystem) or nameof(ETheme.ContrastLight) or nameof(ETheme.ContrastDark) => theme,
            "Aria" => nameof(ETheme.AriaLight),
            "Midnight" => nameof(ETheme.AriaDark),
            "Graphite" => nameof(ETheme.ContrastDark),
            _ => nameof(ETheme.AriaSystem),
        };
    }

    /// <summary>
    /// Returns <c>true</c> for the Aria and Contrast variants that follow the OS theme.
    /// </summary>
    public static bool IsSystemTheme(string? theme)
    {
        return theme is nameof(ETheme.AriaSystem) or nameof(ETheme.ContrastSystem);
    }

    /// <summary>
    /// Resolves theme variants that depend on the OS theme. Other names are returned unchanged.
    /// </summary>
    public static string ResolveSystemTheme(string? theme, bool isDark)
    {
        return theme switch
        {
            nameof(ETheme.AriaSystem) => isDark ? nameof(ETheme.AriaDark) : nameof(ETheme.AriaLight),
            nameof(ETheme.ContrastSystem) => isDark ? nameof(ETheme.ContrastDark) : nameof(ETheme.ContrastLight),
            _ => theme ?? nameof(ETheme.FollowSystem),
        };
    }
}
