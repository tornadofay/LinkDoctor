using LinkDoctor.src.Interfaces;
using LinkDoctor.src.Models;
using System.Net;
using System.Net.Sockets;

namespace LinkDoctor.src.Endpoints
{
    public class DnsEndpoint : INetworkEndpoint
    {
        public string Name { get; }
        private readonly string domain;
        private readonly int timeout;

        public ConnectionDiagnostics.ConnectionLayer Layer => ConnectionDiagnostics.ConnectionLayer.DNSResolution;

        public DnsEndpoint(string name, string domain, int timeout)
        {
            Name = name;
            this.domain = domain;
            this.timeout = timeout;
        }

        public async Task<(bool, ConnectionDiagnostics)> DiagnoseConnectivityAsync()
        {
            var diagnostics = new ConnectionDiagnostics
            {
                ComponentStatus = new List<(string?, bool)>()
            };

            try
            {
                // Set a timeout for DNS resolution
                using var cts = new CancellationTokenSource(timeout);
                var result = await Dns.GetHostEntryAsync(domain, cts.Token);

                diagnostics.ComponentStatus.Add(("DNSResolution", true));
                return (true, diagnostics);
            }
            catch (OperationCanceledException)
            {
                diagnostics.DetailedErrorDescription = $"DNS resolution for {domain} timed out";
                diagnostics.FailedLayer = Layer;
                diagnostics.ComponentStatus.Add(("DNSResolution", false));
                return (false, diagnostics);
            }
            catch (SocketException ex)
            {
                diagnostics.DetailedErrorDescription = $"DNS resolution failed for {domain}: {ex.Message}";
                diagnostics.FailedLayer = Layer;
                diagnostics.ComponentStatus.Add(("DNSResolution", false));
                return (false, diagnostics);
            }
            catch (Exception ex)
            {
                diagnostics.DetailedErrorDescription = $"Unexpected error during DNS resolution for {domain}: {ex.Message}";
                diagnostics.FailedLayer = Layer;
                diagnostics.ComponentStatus.Add(("DNSResolution", false));
                return (false, diagnostics);
            }
        }
    }
}
