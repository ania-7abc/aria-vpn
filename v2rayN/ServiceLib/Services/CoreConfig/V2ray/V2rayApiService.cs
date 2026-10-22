namespace ServiceLib.Services.CoreConfig;

public partial class CoreConfigV2rayService
{
    /// <summary>
    /// Enables the local gRPC API of the Xray core.
    /// The HandlerService lets AriaVPN add outbounds at runtime, the RoutingService
    /// lets it point the balancer to the selected outbound. Together they allow the
    /// automatic server selection to switch servers without restarting the core.
    /// </summary>
    private void GenApi()
    {
        var port = AppManager.Instance.ApiPort;
        _coreConfig.api = new Api4Ray
        {
            tag = "api",
            listen = $"{Global.Loopback}:{port}",
            services = ["HandlerService", "RoutingService"],
        };

        _coreConfig.inbounds.Add(new Inbounds4Ray
        {
            tag = "api",
            listen = Global.Loopback,
            port = port,
            protocol = "dokodemo-door",
            settings = new Inboundsettings4Ray
            {
                address = Global.Loopback,
            },
        });

        _coreConfig.routing.rules.Insert(0, new RulesItem4Ray
        {
            type = "field",
            inboundTag = ["api"],
            outboundTag = "api",
        });
    }
}
