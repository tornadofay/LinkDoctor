using LinkDoctor.src.Interfaces;
using LinkDoctor.src.Models;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Diagnostics;

namespace LinkDoctor.src.Endpoints
{
    public class PingEndpoint : INetworkEndpoint, IDisposable
    {
        private readonly string address;
        private readonly int timeout;
        private readonly Ping ping = new Ping();
        private static readonly SemaphoreSlim semaphore = new SemaphoreSlim(1, 1);

        public string Name { get; }
        public ConnectionDiagnostics.ConnectionLayer Layer => ConnectionDiagnostics.ConnectionLayer.GeneralInternet;

        public PingEndpoint(string name, string address, int timeout)
        {
            Name = name;
            this.address = address;
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
                await semaphore.WaitAsync();
                var reply = await ping.SendPingAsync(address, timeout);
                bool isSuccessful = reply.Status == IPStatus.Success;
                diagnostics.PingTime = isSuccessful ? reply.RoundtripTime : (long?)null;
                diagnostics.ComponentStatus.Add((Name, isSuccessful));

                if (!isSuccessful)
                {
                    diagnostics.DetailedErrorDescription = $"Ping to {Name} ({address}) failed: {GetStatusDetails(reply.Status)}";
                    diagnostics.FailedLayer = Layer;
                }

                return (isSuccessful, diagnostics);
            }
            catch (PingException ex) when (ex.InnerException is SocketException socketEx)
            {
                diagnostics.DetailedErrorDescription = $"Ping to {Name} ({address}): {GetSocketErrorDetails(socketEx.ErrorCode)}";
                diagnostics.FailedLayer = Layer;
                return (false, diagnostics);
            }
            catch (Exception ex)
            {
                diagnostics.DetailedErrorDescription = $"Ping to {Name} ({address}) failed: {ex.Message}";
                diagnostics.FailedLayer = Layer;
                return (false, diagnostics);
            }
            finally
            {
                semaphore.Release();
            }
        }

        private string GetStatusDetails(IPStatus status) => status switch
        {
            IPStatus.Success => "Success",
            IPStatus.TimedOut => "Request timed out",
            IPStatus.TimeExceeded => "Time exceeded",
            IPStatus.DestinationHostUnreachable => "Host unreachable",
            IPStatus.DestinationNetworkUnreachable => "Network unreachable",
            IPStatus.DestinationPortUnreachable => "Port unreachable",
            IPStatus.DestinationProtocolUnreachable => "Protocol unreachable",
            IPStatus.BadDestination => "Bad destination",
            IPStatus.BadHeader => "Bad header",
            IPStatus.BadOption => "Bad option",
            IPStatus.BadRoute => "Bad route",
            IPStatus.HardwareError => "Hardware error",
            IPStatus.IcmpError => "ICMP error",
            IPStatus.NoResources => "No resources",
            IPStatus.PacketTooBig => "Packet too big",
            IPStatus.SourceQuench => "Source quench",
            IPStatus.TtlExpired => "TTL expired",
            IPStatus.UnrecognizedNextHeader => "Unrecognized next header",
            _ => $"Unknown error ({(int)status})"
        };

        private string GetSocketErrorDetails(int errorCode) => errorCode switch
        {

            10050 => "Network is down",
            10051 => "Network is unreachable",
            10052 => "Network dropped connection on reset",
            10053 => "Software caused connection abort",
            10054 => "Connection reset by peer",
            10055 => "No buffer space available",
            10056 => "Socket is already connected",
            10057 => "Socket is not connected",
            10058 => "Cannot send after socket shutdown",
            10060 => "Connection timed out",
            10061 => "Connection refused",
            10064 => "Host is down",
            10065 => "No route to host",
            10070 => "Stale file handle reference",//occurs when a file handle is no longer valid
            10101 => "Graceful shutdown in progress",//indicates a remote party is closing a connection properly
            11001 => "Host not found",
            11002 => "Non-authoritative host not found",
            11003 => "Non-recoverable error",
            11004 => "Valid name, no data record of requested type",
            _ => $"Socket error {errorCode}"
        };

        public void Dispose()
        {
            ping.Dispose();
        }
    }
}
