using DialogHostAvalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using v2rayN.Desktop.Common;

namespace v2rayN.Desktop.Views;

public partial class StatusBarView : ReactiveUserControl<StatusBarViewModel>
{
    private static Config _config;
    private bool _animatingProxy;

    public StatusBarView()
    {
        InitializeComponent();

        _config = AppManager.Instance.Config;

        txtRunningServerDisplay.Tapped += TxtRunningServerDisplay_Tapped;
        txtRunningInfoDisplay.Tapped += TxtRunningServerDisplay_Tapped;

        this.WhenActivated(disposables =>
        {
            //status bar
            this.OneWayBind(ViewModel, vm => vm.RunningServerDisplay, v => v.txtRunningServerDisplay.Text).DisposeWith(disposables);
            this.OneWayBind(ViewModel, vm => vm.RunningInfoDisplay, v => v.txtRunningInfoDisplay.Text).DisposeWith(disposables);
            this.OneWayBind(ViewModel, vm => vm.SpeedProxyDisplay, v => v.txtSpeedProxyDisplay.Text).DisposeWith(disposables);
            this.OneWayBind(ViewModel, vm => vm.SpeedDirectDisplay, v => v.txtSpeedDirectDisplay.Text).DisposeWith(disposables);

            this.Bind(ViewModel, vm => vm.SystemProxySelected, v => v.cmbSystemProxy.SelectedIndex).DisposeWith(disposables);
            this.Bind(ViewModel, vm => vm.SelectedRouting, v => v.cmbRoutings2.SelectedItem).DisposeWith(disposables);

            ViewModel.SetClipboardDataInteraction.RegisterHandler(async interaction =>
            {
                var strData = interaction.Input;
                await AvaUtils.SetClipboardData(this, strData);
                interaction.SetOutput(RxVoid.Default);
            }).DisposeWith(disposables);

            ViewModel.PasswordInputInteraction.RegisterHandler(async interaction =>
            {
                var result = await PasswordInputAsync();
                interaction.SetOutput(result);
            }).DisposeWith(disposables);

            ViewModel.DispatcherRefreshIconInteraction.RegisterHandler(interaction =>
            {
                Dispatcher.UIThread.Post(RefreshIcon, DispatcherPriority.Default);
                interaction.SetOutput(RxVoid.Default);
            }).DisposeWith(disposables);

            this.WhenAnyValue(x => x.ViewModel!.EnableTun)
                .Skip(1)
                .Subscribe(_ => Dispatcher.UIThread.Post(AnimateProxyVanish, DispatcherPriority.Render))
                .DisposeWith(disposables);
        });

        if (Utils.IsNonWindows() && cmbSystemProxy.Items.IsReadOnly == false)
        {
            cmbSystemProxy.Items.RemoveAt(cmbSystemProxy.Items.Count - 1);
        }

        cmbSystemProxy.IsVisible = true;
        cmbSystemProxy.Opacity = 1;
        proxyDust.Children.Clear();
        _animatingProxy = false;

        RefreshIcon();
    }

    private void TxtRunningServerDisplay_Tapped(object? sender, Avalonia.Input.TappedEventArgs e)
    {
        ViewModel?.TestServerAvailability();
    }

    private void BtnTun_Click(object? sender, RoutedEventArgs e)
    {
        if (ViewModel is not null)
        {
            ViewModel.EnableTun = !ViewModel.EnableTun;
        }
    }

    private void AnimateProxyVanish()
    {
        if (proxyHost is null || proxyDust is null || cmbSystemProxy is null || ViewModel is null || _animatingProxy)
        {
            return;
        }

        var en = ViewModel.EnableTun;
        _animatingProxy = true;
        proxyDust.Children.Clear();

        if (en)
        {
            cmbSystemProxy.IsVisible = true;
            cmbSystemProxy.Opacity = 0;
            cmbSystemProxy.RenderTransform = null;
            cmbSystemProxy.RenderTransformOrigin = new RelativePoint(0.5, 0.5, RelativeUnit.Relative);

            var anim = new Animation
            {
                Duration = TimeSpan.FromMilliseconds(160),
                Easing = new CubicEaseInOut(),
                FillMode = FillMode.Forward,
                Children =
                {
                    new KeyFrame { Cue = new Cue(0d), Setters = { new Setter(OpacityProperty, 0.0) } },
                    new KeyFrame { Cue = new Cue(1d), Setters = { new Setter(OpacityProperty, 1.0) } }
                }
            };
            anim.RunAsync(cmbSystemProxy).ContinueWith(_ => Dispatcher.UIThread.Post(() =>
            {
                if (ViewModel?.EnableTun == true && cmbSystemProxy is not null)
                {
                    cmbSystemProxy.IsVisible = false;
                    cmbSystemProxy.Opacity = 1;
                }
                _animatingProxy = false;
            }));
            return;
        }

        cmbSystemProxy.IsVisible = true;
        cmbSystemProxy.Opacity = 0;
        cmbSystemProxy.RenderTransform = null;
        cmbSystemProxy.RenderTransformOrigin = new RelativePoint(0.5, 0.5, RelativeUnit.Relative);

        var animIn = new Animation
        {
            Duration = TimeSpan.FromMilliseconds(180),
            Easing = new CubicEaseInOut(),
            FillMode = FillMode.Forward,
            Children =
            {
                new KeyFrame { Cue = new Cue(0d), Setters = { new Setter(OpacityProperty, 0.0) } },
                new KeyFrame { Cue = new Cue(1d), Setters = { new Setter(OpacityProperty, 1.0) } }
            }
        };
        animIn.RunAsync(cmbSystemProxy).ContinueWith(_ => Dispatcher.UIThread.Post(() =>
        {
            if (ViewModel?.EnableTun == false && cmbSystemProxy is not null)
            {
                cmbSystemProxy.Opacity = 1;
            }
            _animatingProxy = false;
        }));
    }

    private void RefreshIcon()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow.Icon = AvaUtils.GetAppIcon();
            var iconslist = TrayIcon.GetIcons(Application.Current);
            iconslist[0].Icon = AvaUtils.GetTrayIcon(_config.SystemProxyItem.SysProxyType);
            TrayIcon.SetIcons(Application.Current, iconslist);
        }
    }

    private async Task<string?> PasswordInputAsync()
    {
        var dialog = new SudoPasswordInputView();
        var obj = await DialogHost.Show(dialog);

        var password = obj?.ToString();
        if (password.IsNullOrEmpty())
        {
            ViewModel.EnableTun = false;
            return password;
        }

        AppManager.Instance.LinuxSudoPwd = password;
        return password;
    }
}
