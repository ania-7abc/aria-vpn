namespace ServiceLib.ViewModels;

public partial class AutoSelectSettingViewModel : MyReactiveObject, ICloseable
{
    public event EventHandler? RequestClose;

    public ReactiveCommand<RxVoid, RxVoid> SaveCmd { get; }

    [Reactive]
    public partial bool Enabled { get; set; }

    [Reactive]
    public partial int IntervalMinutes { get; set; }

    [Reactive]
    public partial bool OnlySwitchIfBetter { get; set; }

    [Reactive]
    public partial decimal MaxDelay { get; set; }

    [Reactive]
    public partial decimal SwitchTolerance { get; set; }

    [Reactive]
    public partial bool AllSubscriptions { get; set; }

    [Reactive]
    public partial bool SelectOnStartup { get; set; }

    [Reactive]
    public partial bool ContinuousMode { get; set; }

    [Reactive]
    public partial AutoSelectStrategyItem? SelectedStrategy { get; set; }

    [Reactive]
    public partial AutoSelectTestTypeItem? SelectedTestType { get; set; }

    [Reactive]
    public partial AutoSelectIntervalItem? SelectedInterval { get; set; }

    [Reactive]
    public partial AutoSelectContinuousIntervalItem? SelectedContinuousInterval { get; set; }

    public List<AutoSelectStrategyItem> StrategyItems { get; }
    public List<AutoSelectTestTypeItem> TestTypeItems { get; }
    public List<AutoSelectIntervalItem> IntervalItems { get; }
    public List<AutoSelectContinuousIntervalItem> ContinuousIntervalItems { get; }

    public AutoSelectSettingViewModel()
    {
        _config = AppManager.Instance.Config;

        StrategyItems =
        [
            new() { Value = EAutoSelectStrategy.LeastPing, Name = ResUI.AutoSelectStrategyLeastPing },
            new() { Value = EAutoSelectStrategy.Fastest, Name = ResUI.AutoSelectStrategyFastest },
            new() { Value = EAutoSelectStrategy.Random, Name = ResUI.AutoSelectStrategyRandom },
            new() { Value = EAutoSelectStrategy.Fallback, Name = ResUI.AutoSelectStrategyFallback },
        ];

        TestTypeItems =
        [
            new() { Value = ESpeedActionType.Tcping, Name = ResUI.AutoSelectTestTypeTcping },
            new() { Value = ESpeedActionType.Realping, Name = ResUI.AutoSelectTestTypeRealping },
            new() { Value = ESpeedActionType.UdpTest, Name = ResUI.AutoSelectTestTypeUdpTest },
            new() { Value = ESpeedActionType.Mixedtest, Name = ResUI.AutoSelectTestTypeMixedtest },
        ];

        IntervalItems = new List<int> { 1, 2, 3, 5, 10, 15, 20, 30, 60, 120 }
            .Select(t => new AutoSelectIntervalItem { Value = t })
            .ToList();

        ContinuousIntervalItems = new List<int> { 15, 30, 60, 120, 300, 600 }
            .Select(t => new AutoSelectContinuousIntervalItem { Value = t })
            .ToList();

        SaveCmd = ReactiveCommand.CreateFromTask(async () =>
        {
            await SaveAsync();
        });

        var option = _config?.AutoSelectItem;
        if (option is not null)
        {
            Enabled = option.Enabled;
            OnlySwitchIfBetter = option.OnlySwitchIfBetter;
            MaxDelay = option.MaxDelay;
            SwitchTolerance = option.SwitchTolerance;
            AllSubscriptions = option.AllSubscriptions;
            SelectOnStartup = option.SelectOnStartup;
            ContinuousMode = option.ContinuousMode;
            IntervalMinutes = option.IntervalMinutes;

            SelectedStrategy = StrategyItems.FirstOrDefault(t => t.Value == option.Strategy) ?? StrategyItems.First();
            SelectedTestType = TestTypeItems.FirstOrDefault(t => t.Value == option.TestType) ?? TestTypeItems.First();
            SelectedInterval = IntervalItems.FirstOrDefault(t => t.Value == option.IntervalMinutes)
                               ?? new AutoSelectIntervalItem { Value = option.IntervalMinutes };
            SelectedContinuousInterval = ContinuousIntervalItems.FirstOrDefault(t => t.Value == option.ContinuousIntervalSeconds)
                                         ?? new AutoSelectContinuousIntervalItem { Value = option.ContinuousIntervalSeconds };
        }
    }

    private async Task SaveAsync()
    {
        if (_config is null)
        {
            return;
        }

        var option = _config.AutoSelectItem;
        option.Enabled = Enabled;
        option.OnlySwitchIfBetter = OnlySwitchIfBetter;
        option.MaxDelay = (int)Math.Clamp(Math.Round(MaxDelay), 0, 60000);
        option.SwitchTolerance = (int)Math.Clamp(Math.Round(SwitchTolerance), 0, 60000);
        option.AllSubscriptions = AllSubscriptions;
        option.SelectOnStartup = SelectOnStartup;
        option.ContinuousMode = ContinuousMode;
        option.IntervalMinutes = Math.Clamp(SelectedInterval?.Value ?? IntervalMinutes, 1, 1440);
        option.ContinuousIntervalSeconds = Math.Clamp(SelectedContinuousInterval?.Value ?? 60, 15, 3600);
        option.Strategy = SelectedStrategy?.Value ?? EAutoSelectStrategy.LeastPing;
        option.TestType = SelectedTestType?.Value ?? ESpeedActionType.Tcping;

        await ConfigHandler.SaveConfig(_config);

        //Apply the new settings immediately: restart the background loop.
        AutoSelectService.Instance.RestartLoop();
        AppEvents.AutoSelectStateChanged.Publish(RxVoid.Default);

        NoticeManager.Instance.Enqueue(ResUI.OperationSuccess);
        RequestClose?.Invoke(this, EventArgs.Empty);
    }
}

public class AutoSelectStrategyItem
{
    public EAutoSelectStrategy Value { get; set; }
    public string? Name { get; set; }
}

public class AutoSelectTestTypeItem
{
    public ESpeedActionType Value { get; set; }
    public string? Name { get; set; }
}

public class AutoSelectIntervalItem
{
    public int Value { get; set; }
    public string Name => $"{Value} min";
}

public class AutoSelectContinuousIntervalItem
{
    public int Value { get; set; }
    public string Name => $"{Value} s";
}
