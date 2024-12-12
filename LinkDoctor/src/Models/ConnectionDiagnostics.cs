namespace LinkDoctor.src.Models
{
    public class ConnectionDiagnostics
    {
        public enum ConnectionLayer
        {
            PhysicalInterface,
            LocalNetwork,
            Gateway,
            InternetUplink,
            DNSResolution,
            GeneralInternet
        }

        public Boolean IsSuccessful { get; set; }
        public long? PingTime { get; set; }
        public ConnectionLayer FailedLayer { get; set; }
        public string? DetailedErrorDescription { get; set; }
        public List<(string? ComponentName, bool IsReachable)>? ComponentStatus { get; set; }
    }
}
