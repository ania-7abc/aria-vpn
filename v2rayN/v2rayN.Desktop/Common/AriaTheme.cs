using Avalonia.Styling;

namespace v2rayN.Desktop.Common;

/// <summary>
/// Custom AriaVPN color variants. Aria variants use softly tinted indigo surfaces,
/// while Contrast variants use stronger text and border colors.
/// </summary>
public static class AriaTheme
{
    public static readonly ThemeVariant AriaLight = new("AriaLight", ThemeVariant.Light);
    public static readonly ThemeVariant AriaDark = new("AriaDark", ThemeVariant.Dark);
    public static readonly ThemeVariant ContrastLight = new("ContrastLight", ThemeVariant.Light);
    public static readonly ThemeVariant ContrastDark = new("ContrastDark", ThemeVariant.Dark);
}
