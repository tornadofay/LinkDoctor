using LinkDoctor.src.Interfaces;
using LinkDoctor.src.Models;
using System.Net.NetworkInformation;

namespace LinkDoctor.src.Endpoints
{
    public class LocalNetworkEndpoint : INetworkEndpoint
    {
        public string Name { get { return "Local Network Endpoint"; } }

        public ConnectionDiagnostics.ConnectionLayer Layer => ConnectionDiagnostics.ConnectionLayer.LocalNetwork;

        public async Task<(bool, ConnectionDiagnostics)> DiagnoseConnectivityAsync()
        {
            var diagnostics = new ConnectionDiagnostics
            {
                ComponentStatus = new List<(string?, bool)>()
            };

            try
            {
                // Check local network interfaces
                var networkInterfaces = NetworkInterface.GetAllNetworkInterfaces()
                    .Where(n => n.OperationalStatus == OperationalStatus.Up
                                && n.NetworkInterfaceType != NetworkInterfaceType.Loopback);

                if (!networkInterfaces.Any())
                {
                    diagnostics.DetailedErrorDescription = "No active network interfaces found";
                    diagnostics.FailedLayer = Layer;
                    return (false, diagnostics);
                }

                // Check default gateway
                var defaultGateway = NetworkInterface.GetAllNetworkInterfaces()
                    .Select(n => n.GetIPProperties())
                    .Where(p => p.GatewayAddresses.Count > 0)
                    .SelectMany(p => p.GatewayAddresses)
                    .FirstOrDefault();

                if (defaultGateway == null)
                {
                    diagnostics.DetailedErrorDescription = "No default gateway detected";
                    diagnostics.FailedLayer = Layer;
                    return (false, diagnostics);
                }

                // Verify DHCP configuration
                var dhcpEnabled = networkInterfaces.Any(n =>
                    n.GetIPProperties().GetIPv4Properties()?.IsDhcpEnabled ?? false);

                // Ping local gateway
                using (var ping = new Ping())
                {
                    var pingResult = await ping.SendPingAsync(defaultGateway.Address, 1000);

                    diagnostics.ComponentStatus.Add(("NetworkInterfaces", true));
                    diagnostics.ComponentStatus.Add(("DefaultGateway", pingResult.Status == IPStatus.Success));
                    diagnostics.ComponentStatus.Add(("DHCPConfiguration", dhcpEnabled));

                    if (pingResult.Status != IPStatus.Success)
                    {
                        diagnostics.DetailedErrorDescription = $"Unable to ping local gateway: {pingResult.Status}";
                        diagnostics.FailedLayer = Layer;
                        return (false, diagnostics);
                    }
                }

                return (true, diagnostics);
            }
            catch (Exception ex)
            {
                diagnostics.DetailedErrorDescription = $"Local network diagnostic error: {ex.Message}";
                diagnostics.FailedLayer = Layer;
                return (false, diagnostics);
            }
        }
    }
}
