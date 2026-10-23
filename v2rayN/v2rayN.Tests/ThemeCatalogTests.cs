using ServiceLib.Common;
using ServiceLib.Enums;

namespace AriaVPN.Tests;

/// <summary>
/// Checks the theme names, legacy aliases and system-theme resolution.
/// </summary>
internal static class ThemeCatalogTests
{
    public static void Run()
    {
        SelectableThemesAreUnique();
        CanonicalThemesStayUnchanged();
        LegacyThemesAreMigrated();
        UnknownThemesFallBackToSystem();
        OnlyAriaAndContrastAreSystemThemes();
        SystemThemesResolveToLightAndDark();
        ExplicitThemesIgnoreTheSystemTheme();
        FollowSystemIgnoresTheSystemTheme();
    }

    private static void SelectableThemesAreUnique()
    {
        var themes = ThemeCatalog.SelectableThemes;
        Program.Check("Theme picker offers nine choices", themes.Count == 9);
        Program.Check("Theme picker has no duplicates", themes.Distinct(StringComparer.Ordinal).Count() == themes.Count);
        Program.Check("Theme picker excludes legacy names", !themes.Contains("Aria") && !themes.Contains("Midnight") && !themes.Contains("Graphite"));
    }

    private static void CanonicalThemesStayUnchanged()
    {
        var unchanged = ThemeCatalog.SelectableThemes.All(theme => ThemeCatalog.Normalize(theme) == theme);
        Program.Check("Canonical theme names stay unchanged", unchanged);
    }

    private static void LegacyThemesAreMigrated()
    {
        Program.Check("Legacy Aria maps to AriaLight", ThemeCatalog.Normalize("Aria") == nameof(ETheme.AriaLight));
        Program.Check("Legacy Midnight maps to AriaDark", ThemeCatalog.Normalize("Midnight") == nameof(ETheme.AriaDark));
        Program.Check("Legacy Graphite maps to ContrastDark", ThemeCatalog.Normalize("Graphite") == nameof(ETheme.ContrastDark));
    }

    private static void UnknownThemesFallBackToSystem()
    {
        Program.Check("Unknown theme falls back to AriaSystem", ThemeCatalog.Normalize("Solarized") == nameof(ETheme.AriaSystem));
        Program.Check("Empty theme falls back to AriaSystem", ThemeCatalog.Normalize("") == nameof(ETheme.AriaSystem));
        Program.Check("Null theme falls back to AriaSystem", ThemeCatalog.Normalize(null) == nameof(ETheme.AriaSystem));
    }

    private static void OnlyAriaAndContrastAreSystemThemes()
    {
        Program.Check("AriaSystem follows the system", ThemeCatalog.IsSystemTheme(nameof(ETheme.AriaSystem)));
        Program.Check("ContrastSystem follows the system", ThemeCatalog.IsSystemTheme(nameof(ETheme.ContrastSystem)));
        Program.Check("FollowSystem is not a custom variant", !ThemeCatalog.IsSystemTheme(nameof(ETheme.FollowSystem)));
        Program.Check("Explicit themes do not follow the system", !ThemeCatalog.IsSystemTheme(nameof(ETheme.Dark)));
    }

    private static void SystemThemesResolveToLightAndDark()
    {
        Program.Check("AriaSystem resolves to AriaDark", ThemeCatalog.ResolveSystemTheme(nameof(ETheme.AriaSystem), true) == nameof(ETheme.AriaDark));
        Program.Check("AriaSystem resolves to AriaLight", ThemeCatalog.ResolveSystemTheme(nameof(ETheme.AriaSystem), false) == nameof(ETheme.AriaLight));
        Program.Check("ContrastSystem resolves to ContrastDark", ThemeCatalog.ResolveSystemTheme(nameof(ETheme.ContrastSystem), true) == nameof(ETheme.ContrastDark));
        Program.Check("ContrastSystem resolves to ContrastLight", ThemeCatalog.ResolveSystemTheme(nameof(ETheme.ContrastSystem), false) == nameof(ETheme.ContrastLight));
    }

    private static void ExplicitThemesIgnoreTheSystemTheme()
    {
        var unchanged = new[] { nameof(ETheme.Dark), nameof(ETheme.Light), nameof(ETheme.AriaDark), nameof(ETheme.AriaLight), nameof(ETheme.ContrastDark), nameof(ETheme.ContrastLight) }
            .All(theme => ThemeCatalog.ResolveSystemTheme(theme, true) == theme && ThemeCatalog.ResolveSystemTheme(theme, false) == theme);
        Program.Check("Explicit themes ignore the system theme", unchanged);
    }

    private static void FollowSystemIgnoresTheSystemTheme()
    {
        Program.Check("FollowSystem keeps the platform default", ThemeCatalog.ResolveSystemTheme(nameof(ETheme.FollowSystem), true) == nameof(ETheme.FollowSystem));
    }
}
