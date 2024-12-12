using LinkDoctor.src.Interfaces;
using LinkDoctor.src.Models;
using System.Net.NetworkInformation;

namespace LinkDoctor.src.Endpoints
{
    public class UplinkEndpoint : INetworkEndpoint
    {
        public string Name { get { return "Uplink Endpoint"; } }

        public ConnectionDiagnostics.ConnectionLayer Layer => ConnectionDiagnostics.ConnectionLayer.InternetUplink;

        public async Task<(bool, ConnectionDiagnostics)> DiagnoseConnectivityAsync()
        {
            var diagnostics = new ConnectionDiagnostics
            {
                ComponentStatus = new List<(string?, bool)>()
            };

            try
            {
                // Retrieve public IP and validate internet connectivity
                var publicIp = await GetPublicIPAddressAsync();

                // Test connectivity to multiple well-known endpoints
                var testEndpoints = new[]
                {
                "8.8.8.8",     // Google DNS
                "1.1.1.1",     // Cloudflare DNS
                "9.9.9.9"      // Quad9 DNS
            };

                bool uplinkSuccessful = false;
                foreach (var endpoint in testEndpoints)
                {
                    using (var ping = new Ping())
                    {
                        try
                        {
                            var pingResult = await ping.SendPingAsync(endpoint, 2000);

                            diagnostics.ComponentStatus.Add(($"Uplink via {endpoint}",
                                pingResult.Status == IPStatus.Success));

                            if (pingResult.Status == IPStatus.Success)
                            {
                                uplinkSuccessful = true;
                                break;
                            }
                        }
                        catch (PingException)
                        {
                            diagnostics.ComponentStatus.Add(($"Uplink via {endpoint}", false));
                        }
                    }
                }

                if (!uplinkSuccessful)
                {
                    diagnostics.DetailedErrorDescription = "Unable to establish internet uplink connection";
                    diagnostics.FailedLayer = Layer;
                    return (false, diagnostics);
                }

                diagnostics.ComponentStatus.Add(("PublicIPAssignment", !string.IsNullOrEmpty(publicIp)));

                return (true, diagnostics);
            }
            catch (Exception ex)
            {
                diagnostics.DetailedErrorDescription = $"Uplink diagnostic error: {ex.Message}";
                diagnostics.FailedLayer = Layer;
                return (false, diagnostics);
            }
        }

        private async Task<string> GetPublicIPAddressAsync()
        {
            try
            {
                using (var client = new HttpClient())
                {
                    var response = await client.GetStringAsync("https://api.ipify.org");
                    return response.Trim();
                }
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}
