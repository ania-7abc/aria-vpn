using Avalonia.Styling;

namespace v2rayN.Desktop.Common;

/// <summary>
/// Custom AriaVPN color variants. They inherit the Semi light/dark resources and
/// only override the surface, text and accent tokens.
/// </summary>
public static class AriaTheme
{
    public static readonly ThemeVariant Light = new("AriaLight", ThemeVariant.Light);
    public static readonly ThemeVariant Midnight = new("AriaMidnight", ThemeVariant.Dark);
    public static readonly ThemeVariant Graphite = new("AriaGraphite", ThemeVariant.Dark);
}
