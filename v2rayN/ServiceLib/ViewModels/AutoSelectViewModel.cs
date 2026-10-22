namespace ServiceLib.ViewModels;

public partial class AutoSelectViewModel : MyReactiveObject
{
    public ReactiveCommand<RxVoid, RxVoid> SelectBestCmd { get; }
    public ReactiveCommand<RxVoid, RxVoid> SettingCmd { get; }

    private bool _syncingState;

    [Reactive]
    public partial bool Enabled { get; set; }

    [Reactive]
    public partial bool ContinuousMode { get; set; }

    [Reactive]
    public partial bool IsBusy { get; set; }

    [Reactive]
    public partial string StatusText { get; set; }

    [Reactive]
    public partial string CurrentServerText { get; set; }

    [Reactive]
    public partial string DetailText { get; set; }

    [Reactive]
    public partial string StrategyText { get; set; }

    public AutoSelectViewModel()
    {
        _config = AppManager.Instance.Config;

        SelectBestCmd = ReactiveCommand.CreateFromTask(async () =>
        {
            await SelectBestAsync();
        });

        SettingCmd = ReactiveCommand.CreateFromTask(async () =>
        {
            await SettingAsync();
        });

        this.WhenAnyValue(x => x.Enabled)
            .DistinctUntilChanged()
            .Skip(1)
            .SubscribeAsync(async enabled =>
            {
                if (!_syncingState)
                {
                    await ApplyEnabledAsync(enabled);
                }
            });

        this.WhenAnyValue(x => x.ContinuousMode)
            .DistinctUntilChanged()
            .Skip(1)
            .SubscribeAsync(async continuous =>
            {
                if (!_syncingState)
                {
                    await ApplyContinuousAsync(continuous);
                }
            });

        AppEvents.AutoSelectStateChanged
            .AsObservable()
            .ObserveOn(RxSchedulers.MainThreadScheduler)
            .Subscribe(_ => RefreshStatus());

        _ = Init();
    }

    private async Task Init()
    {
        Enabled = _config?.AutoSelectItem?.Enabled ?? false;
        IsBusy = AutoSelectService.Instance.IsBusy;
        RefreshStatus();
        await Task.CompletedTask;
    }

    #region Actions

    private async Task ApplyEnabledAsync(bool enabled)
    {
        try
        {
            await AutoSelectService.Instance.SetEnabledAsync(enabled);
            NoticeManager.Instance.Enqueue(enabled ? ResUI.AutoSelectEnabled : ResUI.AutoSelectDisabled);
        }
        catch (Exception ex)
        {
            Logging.SaveLog("AutoSelectViewModel", ex);
            Enabled = !enabled;
        }

        RefreshStatus();
    }

    private async Task ApplyContinuousAsync(bool continuous)
    {
        try
        {
            var option = _config?.AutoSelectItem;
            if (option is null)
            {
                return;
            }

            option.ContinuousMode = continuous;
            await ConfigHandler.SaveConfig(_config!);
            if (option.Enabled)
            {
                AutoSelectService.Instance.RestartLoop();
            }

            AppEvents.AutoSelectStateChanged.Publish(RxVoid.Default);
            NoticeManager.Instance.Enqueue(continuous ? ResUI.AutoSelectContinuousOn : ResUI.AutoSelectContinuousOff);
        }
        catch (Exception ex)
        {
            Logging.SaveLog("AutoSelectViewModel", ex);
            ContinuousMode = !continuous;
        }

        RefreshStatus();
    }

    private async Task SelectBestAsync()
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        try
        {
            await AutoSelectService.Instance.RunAsync(true);
        }
        catch (Exception ex)
        {
            Logging.SaveLog("AutoSelectViewModel", ex);
        }
        finally
        {
            IsBusy = false;
            RefreshStatus();
        }
    }

    private async Task SettingAsync()
    {
        var viewModel = new AutoSelectSettingViewModel();
        var ret = await AppManager.Instance.WindowDialog.ShowDialogAsync(viewModel);
        if (ret == true)
        {
            AutoSelectService.Instance.RestartLoop();
            RefreshStatus();
        }
    }

    public void RefreshStatus()
    {
        var option = _config?.AutoSelectItem;
        if (option is null)
        {
            return;
        }

        _syncingState = true;
        try
        {
            Enabled = option.Enabled;
            ContinuousMode = option.ContinuousMode;
        }
        finally
        {
            _syncingState = false;
        }

        IsBusy = AutoSelectService.Instance.IsBusy;

        StrategyText = option.Strategy switch
        {
            EAutoSelectStrategy.Fastest => ResUI.AutoSelectStrategyFastest,
            EAutoSelectStrategy.Random => ResUI.AutoSelectStrategyRandom,
            EAutoSelectStrategy.Fallback => ResUI.AutoSelectStrategyFallback,
            _ => ResUI.AutoSelectStrategyLeastPing,
        };

        StatusText = !option.Enabled
            ? ResUI.AutoSelectStatusDisabled
            : option.ContinuousMode
                ? string.Format(ResUI.AutoSelectStatusContinuous, option.ContinuousIntervalSeconds)
                : string.Format(ResUI.AutoSelectStatusEnabled, option.IntervalMinutes);

        var service = AutoSelectService.Instance;
        if (service.LastRunTime is not null)
        {
            CurrentServerText = string.Format(ResUI.AutoSelectLastResult, service.LastServerRemarks, service.LastServerDelay);
            DetailText = string.Format(ResUI.AutoSelectLastRun, service.LastRunTime.Value.ToString("HH:mm:ss"));
        }
        else
        {
            CurrentServerText = ResUI.AutoSelectNoResultYet;
            DetailText = string.Empty;
        }
    }

    #endregion Actions
}
