using Avalonia.Data;
using v2rayN.Desktop.ViewModels;

namespace v2rayN.Desktop.Views;

/// <summary>
/// ThemeSettingView.xaml
/// </summary>
public partial class ThemeSettingView : ReactiveUserControl<ThemeSettingViewModel>
{
    private sealed record ThemeOption(string Value, string Display);

    private static readonly IReadOnlyList<ThemeOption> ThemeOptions =
    [
        new(nameof(ETheme.FollowSystem), "System"),
        new(nameof(ETheme.Dark), "Dark"),
        new(nameof(ETheme.Light), "Light"),
        new(nameof(ETheme.AriaSystem), "Aria System"),
        new(nameof(ETheme.AriaDark), "Aria Dark"),
        new(nameof(ETheme.AriaLight), "Aria Light"),
        new(nameof(ETheme.ContrastSystem), "Contrast System"),
        new(nameof(ETheme.ContrastDark), "Contrast Dark"),
        new(nameof(ETheme.ContrastLight), "Contrast Light"),
    ];

    public ThemeSettingView()
    {
        InitializeComponent();
        ViewModel = new ThemeSettingViewModel();

        cmbCurrentTheme.ItemsSource = ThemeOptions;
        cmbCurrentTheme.DisplayMemberBinding = new Binding(nameof(ThemeOption.Display));
        cmbCurrentTheme.SelectedValueBinding = new Binding(nameof(ThemeOption.Value));
        cmbCurrentFontSize.ItemsSource = Enumerable.Range(Global.MinFontSize, Global.MinFontSizeCount).ToList();
        cmbCurrentLanguage.ItemsSource = Global.LanguageOptions;
        cmbCurrentLanguage.DisplayMemberBinding = new Binding(nameof(LanguageOption.Display));
        cmbCurrentLanguage.SelectedValueBinding = new Binding(nameof(LanguageOption.Value));

        this.WhenActivated(disposables =>
        {
            this.Bind(ViewModel, vm => vm.CurrentTheme, v => v.cmbCurrentTheme.SelectedValue).DisposeWith(disposables);
            this.Bind(ViewModel, vm => vm.CurrentFontSize, v => v.cmbCurrentFontSize.SelectedValue).DisposeWith(disposables);
            this.Bind(ViewModel, vm => vm.CurrentLanguage, v => v.cmbCurrentLanguage.SelectedValue).DisposeWith(disposables);
        });
    }
}
