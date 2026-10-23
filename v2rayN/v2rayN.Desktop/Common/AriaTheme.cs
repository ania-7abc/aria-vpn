using Avalonia.Styling;

namespace v2rayN.Desktop.Common;

/// <summary>
/// Custom AriaVPN color variants. They inherit the Semi light/dark resources and
/// only override the surface, text and accent tokens.
/// </summary>
public static class AriaTheme
{
    public static readonly ThemeVariant AriaLight = new("AriaLight", ThemeVariant.Light);
    public static readonly ThemeVariant AriaDark = new("AriaDark", ThemeVariant.Dark);
    public static readonly ThemeVariant ContrastLight = new("ContrastLight", ThemeVariant.Light);
    public static readonly ThemeVariant ContrastDark = new("ContrastDark", ThemeVariant.Dark);
}
