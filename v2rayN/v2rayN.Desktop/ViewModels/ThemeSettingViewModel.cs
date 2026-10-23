using Avalonia.Controls.Notifications;
using Avalonia.Controls.Primitives;
using AvaloniaEdit;
using Semi.Avalonia;
using v2rayN.Desktop.Common;

namespace v2rayN.Desktop.ViewModels;

public partial class ThemeSettingViewModel : MyReactiveObject
{
    private readonly IPlatformSettings? _platformSettings;

    [Reactive] public partial string CurrentTheme { get; set; }

    [Reactive] public partial int CurrentFontSize { get; set; }

    [Reactive] public partial string CurrentLanguage { get; set; }

    public ThemeSettingViewModel()
    {
        _config = AppManager.Instance.Config;

        NormalizeTheme();
        BindingUI();
        RestoreUI();

        _platformSettings = Application.Current?.PlatformSettings;
        if (_platformSettings is not null)
        {
            _platformSettings.ColorValuesChanged += OnPlatformColorValuesChanged;
        }
    }

    private static bool IsDarkSystemTheme()
    {
        return Application.Current?.PlatformSettings?.GetColorValues().ThemeVariant == PlatformThemeVariant.Dark;
    }

    private void OnPlatformColorValuesChanged(object? sender, PlatformColorValues e)
    {
        if (CurrentTheme is nameof(ETheme.AriaSystem) or nameof(ETheme.ContrastSystem))
        {
            Dispatcher.UIThread.Post(ModifyTheme);
        }
    }

    private void NormalizeTheme()
    {
        var theme = _config.UiItem.CurrentTheme;
        var normalized = theme switch
        {
            nameof(ETheme.FollowSystem) or nameof(ETheme.Dark) or nameof(ETheme.Light)
                or nameof(ETheme.AriaSystem) or nameof(ETheme.AriaLight) or nameof(ETheme.AriaDark)
                or nameof(ETheme.ContrastSystem) or nameof(ETheme.ContrastLight) or nameof(ETheme.ContrastDark) => theme,
            "Aria" => nameof(ETheme.AriaLight),
            "Midnight" => nameof(ETheme.AriaDark),
            "Graphite" => nameof(ETheme.ContrastDark),
            _ => nameof(ETheme.AriaSystem),
        };

        if (!string.Equals(_config.UiItem.CurrentTheme, normalized, StringComparison.Ordinal))
        {
            _config.UiItem.CurrentTheme = normalized;
            _ = ConfigHandler.SaveConfig(_config);
        }
    }

    private void RestoreUI()
    {
        ModifyTheme();
        ModifyFontFamily();
        ModifyFontSize();
    }

    private void BindingUI()
    {
        CurrentTheme = _config.UiItem.CurrentTheme;
        CurrentFontSize = _config.UiItem.CurrentFontSize;
        CurrentLanguage = _config.UiItem.CurrentLanguage;

        this.WhenAnyValue(x => x.CurrentTheme)
            .Where(y => y.IsNotEmpty())
            .SubscribeAsync(async _ =>
            {
                if (_config.UiItem.CurrentTheme != CurrentTheme)
                {
                    _config.UiItem.CurrentTheme = CurrentTheme;
                    ModifyTheme();
                    await ConfigHandler.SaveConfig(_config);
                }
            });

        this.WhenAnyValue(x => x.CurrentFontSize)
            .Where(y => y > 0)
            .SubscribeAsync(async _ =>
            {
                if (_config.UiItem.CurrentFontSize != CurrentFontSize && CurrentFontSize >= Global.MinFontSize)
                {
                    _config.UiItem.CurrentFontSize = CurrentFontSize;
                    ModifyFontSize();
                    await ConfigHandler.SaveConfig(_config);
                }
            });

        this.WhenAnyValue(x => x.CurrentLanguage)
            .Where(y => !y.IsNullOrEmpty())
            .SubscribeAsync(async _ =>
            {
                if (CurrentLanguage.IsNotEmpty() && _config.UiItem.CurrentLanguage != CurrentLanguage)
                {
                    _config.UiItem.CurrentLanguage = CurrentLanguage;
                    Thread.CurrentThread.CurrentUICulture = new(CurrentLanguage);
                    await ConfigHandler.SaveConfig(_config);
                    NoticeManager.Instance.Enqueue(ResUI.NeedRebootTips);
                }
            });
    }

    private void ModifyTheme()
    {
        var app = Application.Current;
        if (app is not null)
        {
            var isDark = IsDarkSystemTheme();
            app.RequestedThemeVariant = CurrentTheme switch
            {
                nameof(ETheme.FollowSystem) => ThemeVariant.Default,
                nameof(ETheme.Dark) => ThemeVariant.Dark,
                nameof(ETheme.Light) => ThemeVariant.Light,
                nameof(ETheme.Aquatic) => SemiTheme.Aquatic,
                nameof(ETheme.Desert) => SemiTheme.Desert,
                nameof(ETheme.Dusk) => SemiTheme.Dusk,
                nameof(ETheme.NightSky) => SemiTheme.NightSky,
                nameof(ETheme.AriaSystem) => isDark ? AriaTheme.AriaDark : AriaTheme.AriaLight,
                nameof(ETheme.AriaLight) => AriaTheme.AriaLight,
                nameof(ETheme.AriaDark) => AriaTheme.AriaDark,
                nameof(ETheme.ContrastSystem) => isDark ? AriaTheme.ContrastDark : AriaTheme.ContrastLight,
                nameof(ETheme.ContrastLight) => AriaTheme.ContrastLight,
                nameof(ETheme.ContrastDark) => AriaTheme.ContrastDark,
                _ => ThemeVariant.Default,
            };
        }
    }

    private void ModifyFontSize()
    {
        double size = CurrentFontSize;
        if (size < Global.MinFontSize)
        {
            return;
        }

        Style style = new(x => Selectors.Or(
            x.OfType<Button>(),
            x.OfType<TextBox>(),
            x.OfType<TextBlock>(),
            x.OfType<SelectableTextBlock>(),
            x.OfType<Menu>(),
            x.OfType<ContextMenu>(),
            x.OfType<DataGridRow>(),
            x.OfType<ListBoxItem>(),
            x.OfType<HeaderedContentControl>(),
            x.OfType<TextEditor>()
        ));
        style.Add(new Setter()
        {
            Property = TemplatedControl.FontSizeProperty,
            Value = size,
        });
        Application.Current?.Styles.Add(style);

        ModifyFontSizeEx(size);
    }

    private void ModifyFontSizeEx(double size)
    {
        //DataGrid
        var rowHeight = 20 + (size / 2);
        var style = new Style(x => x.OfType<DataGrid>());
        style.Add(new Setter(DataGrid.RowHeightProperty, rowHeight));
        Application.Current?.Styles.Add(style);
    }

    private void ModifyFontFamily()
    {
        var currentFontFamily = _config.UiItem.CurrentFontFamily;
        if (currentFontFamily.IsNullOrEmpty())
        {
            return;
        }

        try
        {
            Style style = new(x => Selectors.Or(
                x.OfType<Button>(),
                x.OfType<TextBox>(),
                x.OfType<TextBlock>(),
                x.OfType<SelectableTextBlock>(),
                x.OfType<Menu>(),
                x.OfType<ContextMenu>(),
                x.OfType<DataGridRow>(),
                x.OfType<ListBoxItem>(),
                x.OfType<HeaderedContentControl>(),
                x.OfType<WindowNotificationManager>(),
                x.OfType<TextEditor>()
            ));
            style.Add(new Setter()
            {
                Property = TemplatedControl.FontFamilyProperty,
                Value = new FontFamily(currentFontFamily),
            });
            Application.Current?.Styles.Add(style);
        }
        catch (Exception ex)
        {
            Logging.SaveLog("ModifyFontFamily", ex);
        }
    }
}
