using DialogHostAvalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Media.Transformation;
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
        });

        //spEnableTun.IsVisible = (Utils.IsWindows() || AppHandler.Instance.IsAdministrator);

        if (Utils.IsNonWindows() && cmbSystemProxy.Items.IsReadOnly == false)
        {
            cmbSystemProxy.Items.RemoveAt(cmbSystemProxy.Items.Count - 1);
        }

        this.WhenActivated(disposables =>
        {
            this.WhenAnyValue(x => x.ViewModel!.EnableTun)
                .Skip(1)
                .Subscribe(_ => Dispatcher.UIThread.Post(AnimateProxyVanish, DispatcherPriority.Render))
                .DisposeWith(disposables);
        });

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

private void AnimateProxyVanish()
    {
        if (proxyHost is null || proxyDust is null || cmbSystemProxy is null)
        {
            return;
        }

        if (proxyHost.Bounds.Width <= 0 || proxyHost.Bounds.Height <= 0)
        {
            return;
        }

        var targetVisible = ViewModel?.EnableTun != true;
        var random = new Random();
        proxyDust.Children.Clear();

        if (!targetVisible)
        {
            var count = 18;
            var width = proxyHost.Bounds.Width;
            var height = proxyHost.Bounds.Height;
            for (var i = 0; i < count; i++)
            {
                var rect = new Border
                {
                    Width = random.Next(6, 14),
                    Height = random.Next(6, 10),
                    CornerRadius = new CornerRadius(random.Next(4, 8)),
                    Background = Application.Current?.FindResource("SemiColorFill1") as IBrush ?? new SolidColorBrush(Color.FromRgb(180, 180, 190)),
                    Opacity = 0.9,
                    RenderTransform = new TranslateTransform(random.Next(0, (int)width), random.Next(0, (int)height)),
                    RenderTransformOrigin = new RelativePoint(0.5, 0.5, RelativeUnit.Relative),
                };

                var scale = new ScaleTransform(1.0, 1.0);
                rect.RenderTransform = new TransformGroup { Children = new Transforms { new TranslateTransform(random.Next(0, 100), random.Next(0, 30)), new ScaleTransform(1.0, 1.0) } };
                rect.RenderTransformOrigin = new RelativePoint(0.5, 0.5, RelativeUnit.Relative);

                proxyDust.Children.Add(rect);

                var x = Canvas.GetLeft(rect);
                Canvas.SetLeft(rect, random.Next(0, (int)Math.Max(1, width - rect.Width)));
                Canvas.SetTop(rect, random.Next(0, (int)Math.Max(1, height - rect.Height)));

                _ = AnimateDustParticle(rect, random, width, height);
            }

            cmbSystemProxy.IsVisible = false;
            Task.Delay(420).ContinueWith(_ => Dispatcher.UIThread.Post(() => { proxyDust.Children.Clear(); }, DispatcherPriority.Background));
            return;
        }

        cmbSystemProxy.Opacity = 0.0;
        cmbSystemProxy.IsVisible = true;

        var anim = new Animation
        {
            Duration = TimeSpan.FromMilliseconds(220),
            Easing = new CubicEaseOut(),
            FillMode = FillMode.Forward,
            Children =
            {
                new KeyFrame
                {
                    Cue = new Cue(0d),
                    Setters = { new Setter(OpacityProperty, 0.0), new Setter(ScaleTransform.ScaleXProperty, 0.96), new Setter(ScaleTransform.ScaleYProperty, 0.96) }
                },
                new KeyFrame
                {
                    Cue = new Cue(1d),
                    Setters = { new Setter(OpacityProperty, 1.0), new Setter(ScaleTransform.ScaleXProperty, 1.0), new Setter(ScaleTransform.ScaleYProperty, 1.0) }
                }
            }
        };

        if (cmbSystemProxy.RenderTransform is not ScaleTransform)
        {
            cmbSystemProxy.RenderTransform = new ScaleTransform(1, 1);
        }
        cmbSystemProxy.RenderTransformOrigin = new RelativePoint(0.5, 0.5, RelativeUnit.Relative);

        _ = anim.RunAsync(cmbSystemProxy);
    }

    private async Task AnimateDustParticle(Border rect, Random random, double width, double height)
    {
        try
        {
            var scaleAnim = new Animation
            {
                Duration = TimeSpan.FromMilliseconds(360),
                Easing = new CubicEaseIn(),
                FillMode = FillMode.Forward,
                Children =
                {
                    new KeyFrame { Cue = new Cue(0d), Setters = { new Setter(ScaleTransform.ScaleXProperty, 1.0), new Setter(ScaleTransform.ScaleYProperty, 1.0), new Setter(OpacityProperty, 0.9) } },
                    new KeyFrame { Cue = new Cue(0.4d), Setters = { new Setter(ScaleTransform.ScaleXProperty, 0.6), new Setter(ScaleTransform.ScaleYProperty, 0.6), new Setter(OpacityProperty, 0.6) } },
                    new KeyFrame { Cue = new Cue(1d), Setters = { new Setter(ScaleTransform.ScaleXProperty, 0.2), new Setter(ScaleTransform.ScaleYProperty, 0.2), new Setter(OpacityProperty, 0.0) } },
                }
            };

            var moveAnim = new Animation
            {
                Duration = TimeSpan.FromMilliseconds(360),
                Easing = new CubicEaseOut(),
                FillMode = FillMode.Forward,
                Children =
                {
                    new KeyFrame { Cue = new Cue(0d), Setters = { new Setter(TranslateTransform.XProperty, Canvas.GetLeft(rect)), new Setter(TranslateTransform.YProperty, Canvas.GetTop(rect)) } },
                    new KeyFrame { Cue = new Cue(1d), Setters = { new Setter(TranslateTransform.XProperty, Canvas.GetLeft(rect) + random.Next(-16, 16)), new Setter(TranslateTransform.YProperty, Canvas.GetTop(rect) - random.Next(10, 22)) } },
                }
            };

            if (rect.RenderTransform is TransformGroup tg)
            {
                if (tg.Children.Count < 2)
                {
                    tg.Children.Clear();
                    tg.Children.Add(new TranslateTransform());
                    tg.Children.Add(new ScaleTransform());
                }
                tg.Children[0] = new TranslateTransform(Canvas.GetLeft(rect), Canvas.GetTop(rect));
                tg.Children[1] = new ScaleTransform(1, 1);
                rect.RenderTransform = tg;
            }

            await Task.WhenAll(scaleAnim.RunAsync(rect), moveAnim.RunAsync(rect));
        }
        catch { }
    }
}
