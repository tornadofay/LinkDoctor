using LinkDoctor.src.Interfaces;
using LinkDoctor.src.Models;
using System.Net.Http;
using System.Threading.Tasks;

namespace LinkDoctor.src.Endpoints
{
    public class HttpEndpoint : INetworkEndpoint
    {
        public string Name { get; }
        private readonly string url;
        private readonly int timeout;

        public ConnectionDiagnostics.ConnectionLayer Layer => ConnectionDiagnostics.ConnectionLayer.GeneralInternet;

        public HttpEndpoint(string name, string url, int timeout)
        {
            Name = name;
            this.url = url;
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
                using (var httpClient = new HttpClient())
                {
                    httpClient.Timeout = TimeSpan.FromMilliseconds(timeout);
                    var response = await httpClient.GetAsync(url);

                    if (response.IsSuccessStatusCode)
                    {
                        diagnostics.ComponentStatus.Add((Name, true));
                        return (true, diagnostics);
                    }
                    else
                    {
                        diagnostics.DetailedErrorDescription = $"HTTP request to {Name} failed with status code: {response.StatusCode}";
                        diagnostics.FailedLayer = Layer;
                        diagnostics.ComponentStatus.Add((Name, false));
                        return (false, diagnostics);
                    }
                }
            }
            catch (HttpRequestException ex)
            {
                diagnostics.DetailedErrorDescription = $"HTTP request to {Name} failed: {ex.Message}";
                diagnostics.FailedLayer = Layer;
                diagnostics.ComponentStatus.Add((Name, false));
                return (false, diagnostics);
            }
            catch (TaskCanceledException)
            {
                diagnostics.DetailedErrorDescription = $"HTTP request to {Name} timed out.";
                diagnostics.FailedLayer = Layer;
                diagnostics.ComponentStatus.Add((Name, false));
                return (false, diagnostics);
            }
            catch (OperationCanceledException)
            {
                diagnostics.DetailedErrorDescription = $"HTTP request to {Name} was cancelled";
                diagnostics.FailedLayer = Layer;
                diagnostics.ComponentStatus.Add((Name, false));
                return (false, diagnostics);
            }
            catch (Exception ex)
            {
                diagnostics.DetailedErrorDescription = $"HTTP request to {Name} failed: {ex.Message}";
                diagnostics.FailedLayer = Layer;
                diagnostics.ComponentStatus.Add((Name, false));
                return (false, diagnostics);
            }
        }
    }
}
