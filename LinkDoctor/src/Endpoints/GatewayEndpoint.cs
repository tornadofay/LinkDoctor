using LinkDoctor.src.Interfaces;
using LinkDoctor.src.Models;
using System.Net.NetworkInformation;

namespace LinkDoctor.src.Endpoints
{
    public class GatewayEndpoint : INetworkEndpoint
    {
        public string Name { get { return "Gateway Endpoint"; } }

        public ConnectionDiagnostics.ConnectionLayer Layer => ConnectionDiagnostics.ConnectionLayer.Gateway;

        public async Task<(bool, ConnectionDiagnostics)> DiagnoseConnectivityAsync()
        {
            var diagnostics = new ConnectionDiagnostics
            {
                ComponentStatus = new List<(string?, bool)>()
            };

            try
            {
                // Retrieve network interfaces with gateway
                var gatewayInterfaces = NetworkInterface.GetAllNetworkInterfaces()
                    .Select(n => n.GetIPProperties())
                    .Where(p => p.GatewayAddresses.Count > 0);

                if (!gatewayInterfaces.Any())
                {
                    diagnostics.DetailedErrorDescription = "No gateway interfaces found";
                    diagnostics.FailedLayer = Layer;
                    return (false, diagnostics);
                }

                foreach (var interfaceProperties in gatewayInterfaces)
                {
                    foreach (var gateway in interfaceProperties.GatewayAddresses)
                    {
                        // Check NAT traversal and gateway reachability
                        using (var ping = new Ping())
                        {
                            try
                            {
                                var pingResult = await ping.SendPingAsync(gateway.Address, 1000);

                                diagnostics.ComponentStatus.Add(($"Gateway {gateway.Address}",
                                    pingResult.Status == IPStatus.Success));

                                if (pingResult.Status != IPStatus.Success)
                                {
                                    diagnostics.DetailedErrorDescription =
                                        $"Gateway {gateway.Address} unreachable: {pingResult.Status}";
                                    diagnostics.FailedLayer = Layer;
                                    return (false, diagnostics);
                                }
                            }
                            catch (PingException)
                            {
                                diagnostics.ComponentStatus.Add(($"Gateway {gateway.Address}", false));
                                diagnostics.DetailedErrorDescription =
                                    $"Unable to ping gateway {gateway.Address}";
                                diagnostics.FailedLayer = Layer;
                                return (false, diagnostics);
                            }
                        }
                    }
                }

                return (true, diagnostics);
            }
            catch (Exception ex)
            {
                diagnostics.DetailedErrorDescription = $"Gateway diagnostic error: {ex.Message}";
                diagnostics.FailedLayer = Layer;
                return (false, diagnostics);
            }
        }
    }
}
