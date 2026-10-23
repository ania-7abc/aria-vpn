# AriaVPN — development notes

AriaVPN is a fork of [v2rayN](https://github.com/2dust/v2rayN) (GPL-3.0,
Avalonia / .NET 10). The `.deb` package is called `aria-vpn`.

Status: the feature is implemented and tested (unit tests + an integration run
in the real GUI); the `.deb` package is built by `package-aria-vpn.sh`.

## Environment

* .NET SDK 10 (`dotnet-install.sh` is enough, no root required).
* If the network has broken IPv6, set `DOTNET_SYSTEM_NET_DISABLEIPV6=1`,
  otherwise NuGet restore can hang on SYN-SENT.

```bash
export DOTNET_ROOT="$HOME/.dotnet"
export PATH="$DOTNET_ROOT:$PATH"
export DOTNET_SYSTEM_NET_DISABLEIPV6=1
export DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_NOLOGO=1
```

## Commands

```bash
# build the GUI
cd v2rayN
dotnet build v2rayN.Desktop/v2rayN.Desktop.csproj -c Release

# self-tests: selection logic, theme catalog, palette hierarchy
dotnet run --project v2rayN.Tests/AriaVPN.Tests.csproj -c Release

# publish + build the .deb
cd ..
./package-aria-vpn.sh 7.25.17 --cores /opt/AriaVPN/bin

# rebuild only the .deb from the existing publish output
./package-aria-vpn.sh 7.25.17 --no-publish --cores /opt/AriaVPN/bin
```

## Auto selection

### Configuration

`AutoSelectItem` in `ServiceLib/Models/Configs/ConfigItems.cs`:

| Field | Meaning |
|---|---|
| `Enabled` | the loop is running |
| `IntervalMinutes` | pause between measurements (1–1440) |
| `TestType` | `Tcping`, `Realping`, `UdpTest` or `Mixedtest` |
| `Strategy` | `LeastPing`, `Fastest`, `Random`, `Fallback` |
| `OnlySwitchIfBetter` | keep the current server unless the new one is better by `SwitchTolerance` ms |
| `MaxDelay` | ignore servers slower than this (0 = no limit) |
| `AllSubscriptions` | measure all subscriptions or only the current one |
| `SelectOnStartup` | run a measurement right after start-up |
| `ContinuousMode` | keep the best server active: re-check every `ContinuousIntervalSeconds` and switch through the Xray API only |
| `ContinuousIntervalSeconds` | pause between measurements in the continuous mode (15–3600) |

Defaults and sanitizing live in `ConfigHandler.LoadConfig`.

### Service

`ServiceLib/Services/AutoSelectService.cs`:

* `Init(config)`, `StartLoop()`, `StopLoop()`, `SetEnabledAsync(bool)`.
* The loop waits 15 seconds, then runs a measurement and repeats every
  `IntervalMinutes`, or every `ContinuousIntervalSeconds` when
  `ContinuousMode` is enabled. `RestartLoop()` applies changed settings at once.
* In the continuous mode the candidate list is filtered with
  `CoreManager.CanHotSwitch()`, so only servers that can be activated through
  the Xray API are measured. The improvement threshold is ignored there
  (`SwitchTolerance` is treated as 0) and automatic switch notifications are
  suppressed, because the mode may switch quite often.
* Measurements use the existing `SpeedtestService.RunLoop(testType, profiles)`.
* `SelectBestServer(candidates, delays, speeds, option, currentIndexId)` is a
  pure static function — all selection rules live there and are covered by the
  tests.
* On a change it calls `ConfigHandler.SetDefaultServerIndex()` and raises
  `AppEvents.AutoSelectServerChangedRequested`; `MainWindowViewModel` then
  refreshes the server list and asks `CoreManager.TryHotSwitchOutboundAsync()`
  to switch the running Xray core. When the hot switch is not available it calls
  `Reload()`, which restarts the core with the new server.
* Every run writes one log line with all measured delays and the decision.

### UI

* `AutoSelectView.axaml` — a card in the top bar of the main window: toggle,
  continuous-mode toggle, status, current server with its delay, "select the
  best now" and "settings".
* `AutoSelectSettingWindow.axaml` — grouped settings: schedule (regular and
  continuous interval), selection strategy and measurement limits.
* `MainWindow.axaml` has an "Auto server selection" menu with the same toggles,
  "select the best now" and the settings dialog.
* The tray menu (`App.axaml`) offers enabling auto selection, toggling the
  continuous mode and selecting the best server now. The tray tooltip shows the
  running server and the auto selection state.
* The main menu no longer has the upstream promotion/update entries; manual
  group/chain/custom-outbound creation lives under "Advanced".
* Strings live in `ServiceLib/Resx/ResUI.resx`; the UI is English-only.

## Branding

* `AssemblyName=AriaVPN`, the executable is `AriaVPN`.
* `Global.AppName = "AriaVPN"` — used by window captions, the tray tooltip,
  autostart entries and the log header.
* `ServiceLib/Global.cs`: `AvaAssets = "avares://AriaVPN/Assets/"`.
  This is critical: the `avares` URI contains the assembly name, so after the
  rename the old `avares://v2rayN/...` stops resolving and the app crashes with
  `Could not create glyphTypeface. Font family: Inter`.
* Icons: `Assets/AriaVPN.ico`, `AriaVPN.png`, `AriaVPN.icns`.
* Tray icons `Assets/NotifyIcon1..4.ico` are colour-coded by the system-proxy
  state (grey = direct, green = proxy, blue = unchanged, amber = PAC) and use
  the same "A" mark as the application icon.
* The application does not update itself from the upstream v2rayN releases
  (`CoreInfoManager.IsCheckUpdateSupported`); updates are delivered through the
  package.

## Packaging

`package-aria-vpn.sh`:

* publishes a self-contained single-file binary for `linux-x64`;
* copies the cores (`xray`, `sing-box`, `srss`, geo files);
* installs to `/opt/AriaVPN`, adds `/usr/bin/aria-vpn`, a desktop entry and the
  icon;
* builds the package with `dpkg-deb`.

Because `/opt/AriaVPN` is owned by root, the launcher sets
`V2RAYN_LOCAL_APPLICATION_DATA_V2=1` when the directory is not writable. The
application then keeps its configuration, logs and cores in
`~/.local/share/AriaVPN` and copies `bin` there on the first run
(`CoreManager`).

## Implementation details worth knowing

* `SpeedtestService.RunLoop` reports results through a callback with string
  fields. For TCP ping the delay is in whole milliseconds and **0 is valid**
  (loopback); unreachable servers are `-1`.
* Progress messages such as `Speedtesting` must be dropped with
  `int.TryParse`, not `ToInt()` (which returns 0).
* `ProfileItem.CoreType` must match the core (`ECoreType.Xray = 2` for
  SOCKS/VMess/...), otherwise `GetTestBatchItem` skips the profile.
* `NoticeManager.Instance.Enqueue()` can be called from a background thread.
* Theme: Semi.Avalonia (`CardBorder`, `SimpleToggleSwitch`, `Classes="Success"`,
  `SemiIconBolt`).
* Notifications are shown in the bottom-right corner so they do not cover the
  auto selection card in the top bar.

### Hot server switch (Xray)

A regular reload stops the core and therefore drops every active connection. To
avoid that, the Xray config always contains a local gRPC API and a
`proxy-balancer` (also when there is only one outbound). On a server change
`CoreManager.TryHotSwitchOutboundAsync()`:

1. builds the outbound for the new profile and writes it to a temporary file;
2. calls `xray api ado --server=127.0.0.1:<ApiPort> <file>` to add the outbound
   with the stable tag `proxy-hot-<IndexId>` (adding the same tag again is
   skipped by `CoreManager`);
3. calls `xray api bo --server=127.0.0.1:<ApiPort> -b proxy-balancer
   proxy-hot-<IndexId>` to point the balancer at it.

New connections immediately use the new outbound, while connections that are
already established keep their old one. The API port is allocated next to the
local mixed port (`AppManager.ApiPort`).

The hot switch is skipped (and the core is restarted) for sing-box and other
non-Xray cores, custom/outbound/group nodes, fragment modes
(`EnableFragment`, `EnableFinalFragment`) and an enabled full config template,
because those cases rewrite or replace the generated config.

## Interface

* `Assets/GlobalStyles.axaml` is the single place for the visual system: cards
  use `Classes="card"` (15 px radius, flat `SemiColorBackground2` surface, no
  border), controls use 10 px radii, menus and popups use 12 px. Avoid the old
  `Theme="{DynamicResource CardBorder}"` in new views.
* Borders are reserved for window frames. The main window and dialogs keep the
  system frame; their content is built from borderless cards.
* Themes live in `ETheme`. Canonical names, legacy aliases and the System
  variants are handled by `ServiceLib/Common/ThemeCatalog.cs`, then mapped to
  Avalonia variants in `Views/ThemeSettingViewModel.ModifyTheme()`. The custom
  `AriaLight`, `AriaDark`, `ContrastLight` and `ContrastDark` variants are
  declared in `Common/AriaTheme.cs`; their palette overrides are in the
  `ThemeDictionaries` of `App.axaml`.

## Testing

* `v2rayN.Tests` — `AutoSelectTests` cover the `SelectBestServer` strategies,
  unreachable servers, a 0 ms server, `MaxDelay`, `OnlySwitchIfBetter`, the
  improvement threshold, the continuous mode and the "switched" flag.
  `ThemeCatalogTests` cover canonical names, legacy aliases and System-theme
  resolution. `ThemePaletteTests` guard the background hierarchy, text contrast
  and Contrast-theme borders in `App.axaml`.
* Integration test in the real GUI: three profiles (two live TCP listeners and
  one dead port), TCP ping, `LeastPing`. Expected log line:

  ```
  AutoSelectService: measured 2 of 3 servers [...], selected fast-1 (1 ms), switched=True
  ```

  The active server in the config changes and the Xray balancer is switched to
  the selected outbound without restarting the core (or the core is restarted
  when the hot switch is unavailable).

## Known limitations

* Core updates from the GUI are disabled in a packaged installation
  (`IsPackagedInstall()`); the cores are updated together with the package.
* `MaxDelay` and `SwitchTolerance` are edited as `decimal` in the settings and
  stored as `int`.
