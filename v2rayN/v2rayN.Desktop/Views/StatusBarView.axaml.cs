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

        if (en)
        {
            var w = proxyHost.Bounds.Width;
            var h = proxyHost.Bounds.Height;
            if (w <= 2 || h <= 2)
            {
                w = cmbSystemProxy.Bounds.Width > 0 ? cmbSystemProxy.Bounds.Width : 140;
                h = cmbSystemProxy.Bounds.Height > 0 ? cmbSystemProxy.Bounds.Height : 32;
            }

            proxyDust.Children.Clear();
            proxyDust.Width = w;
            proxyDust.Height = h;
            Canvas.SetLeft(proxyDust, 0);
            Canvas.SetTop(proxyDust, 0);

            var rand = new Random();
            var steps = 36;
            for (var s = 0; s < steps; s++)
            {
                var piece = new Border
                {
                    Height = Math.Max(2, h / (double)steps),
                    Width = w,
                    Background = new SolidColorBrush(Color.FromRgb(142, 142, 152)),
                    Opacity = 1.0,
                    RenderTransformOrigin = new RelativePoint(0.5, 0.5, RelativeUnit.Relative),
                };
                var top = s * (h / steps);
                Canvas.SetLeft(piece, 0);
                Canvas.SetTop(piece, top);
                proxyDust.Children.Add(piece);
                _ = SliceFade(piece, rand, top, w, s, steps);
            }

            _ = Task.Delay(520).ContinueWith(_ => Dispatcher.UIThread.Post(() =>
            {
                if (cmbSystemProxy is not null && ViewModel?.EnableTun == true)
                {
                    cmbSystemProxy.IsVisible = false;
                }
                proxyDust.Children.Clear();
                _animatingProxy = false;
            }, DispatcherPriority.Background));
            return;
        }

        proxyDust.Children.Clear();
        cmbSystemProxy.Opacity = 0;
        cmbSystemProxy.RenderTransform = new ScaleTransform(0.98, 0.98);
        cmbSystemProxy.RenderTransformOrigin = new RelativePoint(0.5, 0.5, RelativeUnit.Relative);
        cmbSystemProxy.IsVisible = true;

        var anim = new Animation
        {
            Duration = TimeSpan.FromMilliseconds(260),
            Easing = new CubicEaseOut(),
            FillMode = FillMode.Forward,
            Children =
            {
                new KeyFrame { Cue = new Cue(0d), Setters = { new Setter(OpacityProperty, 0.0), new Setter(ScaleTransform.ScaleXProperty, 0.98), new Setter(ScaleTransform.ScaleYProperty, 0.98) } },
                new KeyFrame { Cue = new Cue(1d), Setters = { new Setter(OpacityProperty, 1.0), new Setter(ScaleTransform.ScaleXProperty, 1.0), new Setter(ScaleTransform.ScaleYProperty, 1.0) } }
            }
        };
        anim.RunAsync(cmbSystemProxy).ContinueWith(_ => Dispatcher.UIThread.Post(() => _animatingProxy = false));
    }

    private async Task SliceFade(Border p, Random r, double top0, double w, int s, int steps)
    {
        try
        {
            var delay = TimeSpan.FromMilliseconds(s * 10);
            await Task.Delay(delay);
            var a1 = new Animation
            {
                Duration = TimeSpan.FromMilliseconds(380),
                Easing = new CubicEaseIn(),
                FillMode = FillMode.Forward,
                Children =
                {
                    new KeyFrame { Cue = new Cue(0d), Setters = { new Setter(ScaleTransform.ScaleXProperty, 1.0), new Setter(ScaleTransform.ScaleYProperty, 1.0), new Setter(OpacityProperty, 1.0) } },
                    new KeyFrame { Cue = new Cue(0.4d), Setters = { new Setter(ScaleTransform.ScaleXProperty, 0.7), new Setter(ScaleTransform.ScaleYProperty, 0.6), new Setter(OpacityProperty, 0.7) } },
                    new KeyFrame { Cue = new Cue(1d), Setters = { new Setter(ScaleTransform.ScaleXProperty, 0.25), new Setter(ScaleTransform.ScaleYProperty, 0.25), new Setter(OpacityProperty, 0.0) } }
                }
            };
            var a2 = new Animation
            {
                Duration = TimeSpan.FromMilliseconds(380),
                Easing = new CubicEaseOut(),
                FillMode = FillMode.Forward,
                Children =
                {
                    new KeyFrame { Cue = new Cue(0d), Setters = { new Setter(TranslateTransform.XProperty, 0.0), new Setter(TranslateTransform.YProperty, top0) } },
                    new KeyFrame { Cue = new Cue(1d), Setters = { new Setter(TranslateTransform.XProperty, r.Next(-10, 10)), new Setter(TranslateTransform.YProperty, top0 - r.Next(16, 34)) } }
                }
            };
            p.RenderTransform = new TransformGroup { Children = { new TranslateTransform(0, top0), new ScaleTransform(1, 1) } };
            p.RenderTransformOrigin = new RelativePoint(0.5, 0.5, RelativeUnit.Relative);
            await Task.WhenAll(a1.RunAsync(p), a2.RunAsync(p));
        }
        catch { }
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
