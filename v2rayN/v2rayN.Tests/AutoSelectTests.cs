using ServiceLib.Enums;
using ServiceLib.Models.Configs;
using ServiceLib.Models.Entities;
using ServiceLib.Services;

namespace AriaVPN.Tests;

/// <summary>
/// Scenarios for the automatic server selection strategies.
/// </summary>
internal static class AutoSelectTests
{
    public static void Run()
    {
        LeastPingPicksTheFastest();
        FastestPicksTheHighestSpeed();
        FallbackKeepsCurrentServer();
        RandomPicksReachableServer();
        UnreachableServersAreIgnored();
        ZeroDelayIsReachable();
        MaxDelayFiltersSlowServers();
        OnlySwitchIfBetterKeepsCurrentServer();
        ImprovementThresholdAllowsSwitch();
        ContinuousModeAlwaysFollowsTheBest();
        SwitchIsReportedWhenServerChanges();
    }

    private static void LeastPingPicksTheFastest()
    {
        var option = new AutoSelectItem { Strategy = EAutoSelectStrategy.LeastPing };
        var result = AutoSelectService.SelectBestServer(
            Profiles("a", "b", "c"),
            Delays(("a", 120), ("b", 30), ("c", 70)),
            Speeds(),
            option,
            "");

        Check("LeastPing picks the lowest delay", result is not null && result.IndexId == "b");
    }

    private static void FastestPicksTheHighestSpeed()
    {
        var option = new AutoSelectItem { Strategy = EAutoSelectStrategy.Fastest };
        var result = AutoSelectService.SelectBestServer(
            Profiles("a", "b", "c"),
            Delays(("a", 120), ("b", 30), ("c", 70)),
            Speeds(("a", 10.5), ("b", 20.0), ("c", 15.0)),
            option,
            "");

        Check("Fastest picks the highest speed", result is not null && result.IndexId == "b");
    }

    private static void FallbackKeepsCurrentServer()
    {
        var option = new AutoSelectItem { Strategy = EAutoSelectStrategy.Fallback };
        var result = AutoSelectService.SelectBestServer(
            Profiles("a", "b"),
            Delays(("a", 120), ("b", 30)),
            Speeds(),
            option,
            "a");

        Check("Fallback keeps the current server", result is not null && result.IndexId == "a" && !result.Switched);
    }

    private static void RandomPicksReachableServer()
    {
        var option = new AutoSelectItem { Strategy = EAutoSelectStrategy.Random };
        var result = AutoSelectService.SelectBestServer(
            Profiles("a", "b", "c"),
            Delays(("a", 120), ("b", -1), ("c", 70)),
            Speeds(),
            option,
            "");

        Check("Random picks a reachable server", result is not null && (result.IndexId == "a" || result.IndexId == "c"));
    }

    private static void UnreachableServersAreIgnored()
    {
        var option = new AutoSelectItem { Strategy = EAutoSelectStrategy.LeastPing };
        var result = AutoSelectService.SelectBestServer(
            Profiles("a", "b"),
            Delays(("a", -1), ("b", 45)),
            Speeds(),
            option,
            "");

        Check("Unreachable servers are ignored", result is not null && result.IndexId == "b");
    }

    private static void ZeroDelayIsReachable()
    {
        var option = new AutoSelectItem { Strategy = EAutoSelectStrategy.LeastPing };
        var result = AutoSelectService.SelectBestServer(
            Profiles("a", "b"),
            Delays(("a", -1), ("b", 0)),
            Speeds(),
            option,
            "");

        Check("A 0 ms server is treated as reachable", result is not null && result.IndexId == "b");
    }

    private static void MaxDelayFiltersSlowServers()
    {
        var option = new AutoSelectItem { Strategy = EAutoSelectStrategy.LeastPing, MaxDelay = 100 };
        var result = AutoSelectService.SelectBestServer(
            Profiles("a", "b"),
            Delays(("a", 120), ("b", 30)),
            Speeds(),
            option,
            "");

        Check("MaxDelay filters out slow servers", result is not null && result.IndexId == "b");
    }

    private static void OnlySwitchIfBetterKeepsCurrentServer()
    {
        var option = new AutoSelectItem
        {
            Strategy = EAutoSelectStrategy.LeastPing,
            OnlySwitchIfBetter = true,
            SwitchTolerance = 50,
        };
        var result = AutoSelectService.SelectBestServer(
            Profiles("a", "b"),
            Delays(("a", 100), ("b", 80)),
            Speeds(),
            option,
            "a");

        Check("OnlySwitchIfBetter keeps the current server", result is not null && result.IndexId == "a" && !result.Switched);
    }

    private static void ImprovementThresholdAllowsSwitch()
    {
        var option = new AutoSelectItem
        {
            Strategy = EAutoSelectStrategy.LeastPing,
            OnlySwitchIfBetter = true,
            SwitchTolerance = 50,
        };
        var result = AutoSelectService.SelectBestServer(
            Profiles("a", "b"),
            Delays(("a", 300), ("b", 100)),
            Speeds(),
            option,
            "a");

        Check("A clearly better server is selected", result is not null && result.IndexId == "b" && result.Switched);
    }

    private static void ContinuousModeAlwaysFollowsTheBest()
    {
        var option = new AutoSelectItem
        {
            Strategy = EAutoSelectStrategy.LeastPing,
            OnlySwitchIfBetter = true,
            SwitchTolerance = 50,
            ContinuousMode = true,
        };
        var result = AutoSelectService.SelectBestServer(
            Profiles("a", "b"),
            Delays(("a", 100), ("b", 90)),
            Speeds(),
            option,
            "a");

        Check("Continuous mode follows even a slightly better server", result is not null && result.IndexId == "b" && result.Switched);
    }

    private static void SwitchIsReportedWhenServerChanges()
    {
        var option = new AutoSelectItem { Strategy = EAutoSelectStrategy.LeastPing };
        var result = AutoSelectService.SelectBestServer(
            Profiles("a", "b"),
            Delays(("a", 300), ("b", 100)),
            Speeds(),
            option,
            "a");

        Check("Switch flag is set when the server changes", result is not null && result.Switched);
    }

    private static void Check(string name, bool ok)
    {
        Program.Check(name, ok);
    }

    private static List<ProfileItem> Profiles(params string[] ids)
    {
        return ids.Select(id => new ProfileItem
        {
            IndexId = id,
            Remarks = $"server-{id}",
            Address = "127.0.0.1",
            Port = 1080,
            ConfigType = EConfigType.SOCKS,
        }).ToList();
    }

    private static Dictionary<string, int> Delays(params (string id, int delay)[] values)
    {
        return values.ToDictionary(v => v.id, v => v.delay);
    }

    private static Dictionary<string, double> Speeds(params (string id, double speed)[] values)
    {
        return values.ToDictionary(v => v.id, v => v.speed);
    }
}
