using LinkDoctor.src.Models;

namespace LinkDoctor.src.Interfaces
{
    public interface INetworkEndpoint
    {
        string Name { get; }
        ConnectionDiagnostics.ConnectionLayer Layer { get; }
        Task<(bool Success, ConnectionDiagnostics Diagnostics)> DiagnoseConnectivityAsync();
    }
}
