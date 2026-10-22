using System.Globalization;

namespace ServiceLib.Services;

/// <summary>
/// Automatic server selection.
/// Periodically measures the delay (and optionally the speed) of the available servers
/// and switches the active server to the best one according to the chosen strategy.
/// </summary>
public class AutoSelectService
{
    private static readonly Lazy<AutoSelectService> _instance = new(() => new());
    public static AutoSelectService Instance => _instance.Value;

    private const string _tag = "AutoSelectService";
    private Config? _config;
    private readonly SemaphoreSlim _runSemaphore = new(1, 1);
    private readonly Lock _stateLock = new();
    private CancellationTokenSource? _cts;
    private Task? _loopTask;

    public bool IsRunning { get; private set; }
    public bool IsBusy { get; private set; }
    public DateTime? LastRunTime { get; private set; }
    public string LastServerRemarks { get; private set; } = string.Empty;
    public int LastServerDelay { get; private set; }

    public void Init(Config config)
    {
        _config = config;
    }

    #region Loop

    /// <summary>
    /// Start the automatic selection if it is enabled in the settings.
    /// </summary>
    public void Start()
    {
        if (_config is null)
        {
            return;
        }

        if (_config.AutoSelectItem.Enabled)
        {
            StartLoop();
        }
    }

    public void StartLoop()
    {
        lock (_stateLock)
        {
            if (_loopTask is not null)
            {
                return;
            }

            _cts = new CancellationTokenSource();
            IsRunning = true;
            _loopTask = Task.Run(() => LoopAsync(_cts.Token));
        }
    }

    public void StopLoop()
    {
        lock (_stateLock)
        {
            if (_cts is null)
            {
                return;
            }

            try
            {
                _cts.Cancel();
            }
            catch (Exception ex)
            {
                Logging.SaveLog(_tag, ex);
            }

            _cts.Dispose();
            _cts = null;
            _loopTask = null;
            IsRunning = false;
        }
    }

    /// <summary>
    /// Enable or disable the automatic selection.
    /// </summary>
    public async Task<bool> SetEnabledAsync(bool enabled)
    {
        if (_config is null)
        {
            return false;
        }

        _config.AutoSelectItem.Enabled = enabled;
        await ConfigHandler.SaveConfig(_config);

        if (enabled)
        {
            StartLoop();
        }
        else
        {
            StopLoop();
        }

        AppEvents.AutoSelectStateChanged.Publish(RxVoid.Default);
        return true;
    }

    private async Task LoopAsync(CancellationToken ct)
    {
        try
        {
            //Give the application some time to start before the first measurement.
            await Task.Delay(TimeSpan.FromSeconds(15), ct);

            if (_config?.AutoSelectItem.SelectOnStartup == true)
            {
                await RunAsync(false);
            }

            while (!ct.IsCancellationRequested)
            {
                var minutes = Math.Clamp(_config?.AutoSelectItem.IntervalMinutes ?? 10, 1, 1440);
                await Task.Delay(TimeSpan.FromMinutes(minutes), ct);

                if (ct.IsCancellationRequested)
                {
                    break;
                }

                await RunAsync(false);
            }
        }
        catch (OperationCanceledException)
        {
            //Ignored
        }
        catch (Exception ex)
        {
            Logging.SaveLog(_tag, ex);
        }
    }

    #endregion Loop

    #region Selection

    /// <summary>
    /// Measure the servers and switch to the best one.
    /// </summary>
    /// <param name="manual">true - triggered by the user, false - by the timer.</param>
    public async Task<AutoSelectResult?> RunAsync(bool manual)
    {
        if (_config is null)
        {
            return null;
        }

        if (!await _runSemaphore.WaitAsync(0))
        {
            return null;
        }

        try
        {
            IsBusy = true;
            Logging.SaveLog($"{_tag}: run started, manual={manual}");
            AppEvents.AutoSelectStateChanged.Publish(RxVoid.Default);

            var option = _config.AutoSelectItem;
            var candidates = await GetCandidates(option.AllSubscriptions);
            if (candidates.Count <= 0)
            {
                if (manual)
                {
                    NoticeManager.Instance.Enqueue(ResUI.AutoSelectNoServers);
                }

                return null;
            }

            var delays = new ConcurrentDictionary<string, int>();
            var speeds = new ConcurrentDictionary<string, double>();

            var speedtestService = new SpeedtestService(_config, async result =>
            {
                if (result.IndexId.IsNullOrEmpty())
                {
                    return;
                }

                //Ignore the progress messages ("Speedtesting") and failed measurements (-1).
                if (result.Delay.IsNotEmpty()
                    && int.TryParse(result.Delay, NumberStyles.Integer, CultureInfo.InvariantCulture, out var delay)
                    && delay >= 0)
                {
                    delays[result.IndexId] = delay;
                }

                if (result.Speed.IsNotEmpty() && double.TryParse(result.Speed, NumberStyles.Float, CultureInfo.InvariantCulture, out var speed) && speed > 0)
                {
                    speeds[result.IndexId] = speed;
                }
            });

            await speedtestService.RunLoop(option.TestType, candidates);

            var result = await PickBestAsync(candidates, delays, speeds, option);

            Logging.SaveLog($"{_tag}: measured {delays.Count} of {candidates.Count} servers " +
                $"[{string.Join(", ", delays.Select(t => $"{t.Key}={t.Value}ms"))}]" +
                (result is null ? ", no server selected" : $", selected {result.Remarks} ({result.Delay} ms), switched={result.Switched}"));

            LastRunTime = DateTime.Now;
            AppEvents.AutoSelectStateChanged.Publish(RxVoid.Default);

            if (result is null)
            {
                if (manual)
                {
                    NoticeManager.Instance.Enqueue(ResUI.AutoSelectNoAvailableServer);
                }

                return null;
            }

            if (result.Switched && result.IndexId.IsNotEmpty())
            {
                await ConfigHandler.SetDefaultServerIndex(_config, result.IndexId);
                AppEvents.AutoSelectServerChangedRequested.Publish(result.IndexId);
            }

            LastServerRemarks = result.Remarks;
            LastServerDelay = result.Delay;

            if (manual)
            {
                var msg = result.Switched
                    ? string.Format(ResUI.AutoSelectSwitchedTo, result.Remarks, result.Delay)
                    : string.Format(ResUI.AutoSelectKeptCurrent, result.Remarks, result.Delay);
                NoticeManager.Instance.Enqueue(msg);
            }
            else if (result.Switched)
            {
                NoticeManager.Instance.Enqueue(string.Format(ResUI.AutoSelectSwitchedTo, result.Remarks, result.Delay));
            }

            return result;
        }
        catch (Exception ex)
        {
            Logging.SaveLog(_tag, ex);
            if (manual)
            {
                NoticeManager.Instance.Enqueue(ResUI.OperationFailed);
            }

            return null;
        }
        finally
        {
            IsBusy = false;
            AppEvents.AutoSelectStateChanged.Publish(RxVoid.Default);
            _runSemaphore.Release();
        }
    }

    private async Task<List<ProfileItem>> GetCandidates(bool allSubscriptions)
    {
        var subid = allSubscriptions ? string.Empty : _config?.SubIndexId ?? string.Empty;
        var items = await AppManager.Instance.ProfileItems(subid);
        if (items is null)
        {
            return [];
        }

        return items
            .Where(t => t is not null
                && !t.IndexId.IsNullOrEmpty()
                && t.ConfigType != EConfigType.Custom
                && t.ConfigType != EConfigType.Outbound
                && (t.ConfigType.IsComplexType() || t.Port > 0))
            .ToList();
    }

    private async Task<AutoSelectResult?> PickBestAsync(List<ProfileItem> candidates,
        ConcurrentDictionary<string, int> delays,
        ConcurrentDictionary<string, double> speeds,
        AutoSelectItem option)
    {
        return SelectBestServer(candidates, delays, speeds, option, _config?.IndexId ?? string.Empty);
    }

    /// <summary>
    /// Pure selection logic: picks the best server from the measured candidates.
    /// </summary>
    /// <param name="candidates">All servers that took part in the measurement.</param>
    /// <param name="delays">Measured delays in milliseconds, indexed by IndexId.</param>
    /// <param name="speeds">Measured speeds, indexed by IndexId.</param>
    /// <param name="option">The current settings.</param>
    /// <param name="currentIndexId">IndexId of the currently active server.</param>
    public static AutoSelectResult? SelectBestServer(
        List<ProfileItem> candidates,
        IReadOnlyDictionary<string, int> delays,
        IReadOnlyDictionary<string, double> speeds,
        AutoSelectItem option,
        string currentIndexId)
    {
        var measured = candidates
            .Select(t => new
            {
                Item = t,
                Delay = delays.GetValueOrDefault(t.IndexId, -1),
                Speed = speeds.GetValueOrDefault(t.IndexId, 0),
            })
            .ToList();

        var reachable = measured.Where(t => t.Delay >= 0).ToList();

        //The current server may be out of the measured list (e.g. it belongs to another group).
        var current = measured.FirstOrDefault(t => t.Item.IndexId == currentIndexId);

        if (option.MaxDelay > 0)
        {
            var fast = reachable.Where(t => t.Delay <= option.MaxDelay).ToList();
            if (fast.Count > 0)
            {
                reachable = fast;
            }
        }

        if (reachable.Count <= 0)
        {
            return null;
        }

        var selected = option.Strategy switch
        {
            EAutoSelectStrategy.Fastest => reachable
                .OrderByDescending(t => t.Speed)
                .ThenBy(t => t.Delay)
                .First(),
            EAutoSelectStrategy.Random => reachable[Random.Shared.Next(reachable.Count)],
            EAutoSelectStrategy.Fallback => current is not null && current.Delay >= 0
                ? current
                : reachable.First(),
            _ => reachable.OrderBy(t => t.Delay).First(),
        };

        //Do not switch if the current server is good enough.
        if (option.OnlySwitchIfBetter
            && current is not null
            && current.Delay >= 0
            && selected.Item.IndexId != current.Item.IndexId)
        {
            var improvement = current.Delay - selected.Delay;
            if (improvement <= option.SwitchTolerance)
            {
                return new AutoSelectResult
                {
                    IndexId = current.Item.IndexId,
                    Remarks = current.Item.Remarks,
                    Delay = current.Delay,
                    Switched = false,
                };
            }
        }

        return new AutoSelectResult
        {
            IndexId = selected.Item.IndexId,
            Remarks = selected.Item.Remarks,
            Delay = selected.Delay,
            Switched = selected.Item.IndexId != currentIndexId,
        };
    }

    #endregion Selection
}

/// <summary>
/// The outcome of an automatic selection run.
/// </summary>
public class AutoSelectResult
{
    public string? IndexId { get; set; }
    public string? Remarks { get; set; }
    public int Delay { get; set; }
    public bool Switched { get; set; }
}
