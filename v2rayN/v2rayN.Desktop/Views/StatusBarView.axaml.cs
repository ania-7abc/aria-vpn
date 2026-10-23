using DialogHostAvalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using v2rayN.Desktop.Common;

namespace v2rayN.Desktop.Views;

public partial class StatusBarView : ReactiveUserControl<StatusBarViewModel>
{
    private static Config _config;

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

        //spEnableTun.IsVisible = (Utils.IsWindows() || AppHandler.Instance.IsAdministrator);

        if (Utils.IsNonWindows() && cmbSystemProxy.Items.IsReadOnly == false)
        {
            cmbSystemProxy.Items.RemoveAt(cmbSystemProxy.Items.Count - 1);
        }

        cmbSystemProxy.IsVisible = true;
        proxyDust.Children.Clear();

        // Because this view has not yet been initialized when DispatcherRefreshIconInteraction is first called.
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
        if (proxyHost is null || proxyDust is null || cmbSystemProxy is null || ViewModel is null)
        {
            return;
        }

        var en = ViewModel.EnableTun;

        if (en)
        {
            var w = proxyHost.Bounds.Width;
            var h = proxyHost.Bounds.Height;
            if (w <= 1 || h <= 1)
            {
                Dispatcher.UIThread.Post(() =>
                {
                    if (ViewModel?.EnableTun == true && cmbSystemProxy is not null)
                    {
                        cmbSystemProxy.IsVisible = false;
                    }
                }, DispatcherPriority.Render);
                return;
            }

            proxyDust.Children.Clear();
            var rand = new Random();
            for (var i = 0; i < 22; i++)
            {
                var bw = rand.Next(8, 16);
                var bh = rand.Next(6, 10);
                var px = rand.Next(0, (int)Math.Max(1, w - bw));
                var py = rand.Next(0, (int)Math.Max(1, h - bh));
                var piece = new Border
                {
                    Width = bw,
                    Height = bh,
                    CornerRadius = new CornerRadius(rand.Next(4, 7)),
                    Background = new SolidColorBrush(Color.Parse("#8F8F9A")),
                    Opacity = 0.95,
                    RenderTransform = new TranslateTransform(px, py),
                    RenderTransformOrigin = new RelativePoint(0.5, 0.5, RelativeUnit.Relative),
                };
                Canvas.SetLeft(piece, px);
                Canvas.SetTop(piece, py);
                proxyDust.Children.Add(piece);
                _ = Dust(piece, rand);
            }

            cmbSystemProxy.IsVisible = false;
            _ = Task.Delay(450).ContinueWith(_ => Dispatcher.UIThread.Post(() => proxyDust.Children.Clear(), DispatcherPriority.Background));
            return;
        }

        proxyDust.Children.Clear();
        cmbSystemProxy.Opacity = 0;
        cmbSystemProxy.RenderTransform = new ScaleTransform(0.96, 0.96);
        cmbSystemProxy.RenderTransformOrigin = new RelativePoint(0.5, 0.5, RelativeUnit.Relative);
        cmbSystemProxy.IsVisible = true;

        var anim = new Animation
        {
            Duration = TimeSpan.FromMilliseconds(280),
            Easing = new CubicEaseOut(),
            FillMode = FillMode.Forward,
            Children =
            {
                new KeyFrame { Cue = new Cue(0d), Setters = { new Setter(OpacityProperty, 0.0), new Setter(ScaleTransform.ScaleXProperty, 0.96), new Setter(ScaleTransform.ScaleYProperty, 0.96) } },
                new KeyFrame { Cue = new Cue(1d), Setters = { new Setter(OpacityProperty, 1.0), new Setter(ScaleTransform.ScaleXProperty, 1.0), new Setter(ScaleTransform.ScaleYProperty, 1.0) } }
            }
        };
        _ = anim.RunAsync(cmbSystemProxy);
    }

    private async Task Dust(Border p, Random r)
    {
        try
        {
            var a1 = new Animation
            {
                Duration = TimeSpan.FromMilliseconds(380),
                Easing = new CubicEaseIn(),
                FillMode = FillMode.Forward,
                Children =
                {
                    new KeyFrame { Cue = new Cue(0d), Setters = { new Setter(ScaleTransform.ScaleXProperty, 1.0), new Setter(ScaleTransform.ScaleYProperty, 1.0), new Setter(OpacityProperty, 0.95) } },
                    new KeyFrame { Cue = new Cue(0.5d), Setters = { new Setter(ScaleTransform.ScaleXProperty, 0.55), new Setter(ScaleTransform.ScaleYProperty, 0.55), new Setter(OpacityProperty, 0.5) } },
                    new KeyFrame { Cue = new Cue(1d), Setters = { new Setter(ScaleTransform.ScaleXProperty, 0.16), new Setter(ScaleTransform.ScaleYProperty, 0.16), new Setter(OpacityProperty, 0.0) } }
                }
            };
            var a2 = new Animation
            {
                Duration = TimeSpan.FromMilliseconds(380),
                Easing = new CubicEaseOut(),
                FillMode = FillMode.Forward,
                Children =
                {
                    new KeyFrame { Cue = new Cue(0d), Setters = { new Setter(TranslateTransform.XProperty, Canvas.GetLeft(p)), new Setter(TranslateTransform.YProperty, Canvas.GetTop(p)) } },
                    new KeyFrame { Cue = new Cue(1d), Setters = { new Setter(TranslateTransform.XProperty, Canvas.GetLeft(p) + r.Next(-18, 18)), new Setter(TranslateTransform.YProperty, Canvas.GetTop(p) - r.Next(14, 28)) } }
                }
            };
            p.RenderTransform = new TransformGroup { Children = { new TranslateTransform(Canvas.GetLeft(p), Canvas.GetTop(p)), new ScaleTransform(1, 1) } };
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
