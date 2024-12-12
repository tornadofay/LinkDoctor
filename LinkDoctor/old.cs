using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LinkDoctor
{
    internal class OldCode
    {
        #region v1
        //public FrmMain()
        //{
        //    InitializeComponent();
        //    InitializeTimer();
        //    InitializeNotifyIcon();
        //    NetworkChange.NetworkAvailabilityChanged += NetworkAvailabilityChangedHandler;
        //}

        //private void NetworkAvailabilityChangedHandler(object? sender, System.EventArgs e)
        //{
        //    bool currentStatus = CheckInternetConnectivity();
        //    if (currentStatus != previousConnectionStatus)
        //    {
        //        if (!currentStatus)
        //        {
        //            // Connection dropped
        //            disconnectedTime = DateTime.Now;
        //            LogEvent("Internet connection dropped at " + disconnectedTime.ToString());
        //        }
        //        else
        //        {
        //            // Connection restored
        //            DateTime recoveredTime = DateTime.Now;
        //            TimeSpan downtime = recoveredTime - disconnectedTime;
        //            LogEvent("Internet connection restored at " + recoveredTime.ToString() +
        //                      " (Down for " + downtime.ToString() + ")");
        //        }
        //        previousConnectionStatus = currentStatus;
        //    }
        //}

        //private System.Timers.Timer? checkTimer;

        //private void InitializeTimer()
        //{
        //    checkTimer = new System.Timers.Timer();
        //    checkTimer.Interval = 10000; // 10 seconds
        //    checkTimer.Elapsed += TimerElapsedHandler;
        //    checkTimer.Start();
        //}

        //private void TimerElapsedHandler(object? sender, ElapsedEventArgs e)
        //{
        //    CheckAndLogConnectivity();
        //}

        //private bool previousConnectionStatus = true;
        //private DateTime disconnectedTime;

        //private void CheckAndLogConnectivity()
        //{
        //    bool currentStatus = CheckInternetConnectivity();
        //    if (currentStatus != previousConnectionStatus)
        //    {
        //        if (!currentStatus)
        //        {
        //            disconnectedTime = DateTime.Now;
        //            LogEvent("Internet connection dropped at " + disconnectedTime.ToString());
        //        }
        //        else
        //        {
        //            DateTime recoveredTime = DateTime.Now;
        //            TimeSpan downtime = recoveredTime - disconnectedTime;
        //            LogEvent("Internet connection restored at " + recoveredTime.ToString() + " (Down for " + downtime.ToString() + ")");
        //        }
        //        previousConnectionStatus = currentStatus;
        //    }
        //}

        //private bool CheckInternetConnectivity()
        //{
        //    try
        //    {
        //        using (Ping ping = new Ping())
        //        {
        //            PingReply reply = ping.Send("8.8.8.8", 500); // Google's DNS server
        //            return reply.Status == IPStatus.Success;
        //        }

        //    }
        //    catch
        //    {
        //        return false;
        //    }
        //}
        //private async void LogEvent(string message)
        //{
        //    try
        //    {
        //        string logPath = Path.Combine(Application.StartupPath, "InternetConnectionLog.txt");
        //        await File.AppendAllTextAsync(logPath, message + Environment.NewLine);

        //        AppendToLogTextBox(message);
        //    }
        //    catch (Exception ex)
        //    {
        //        MessageBox.Show("Error logging event: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        //    }
        //}

        //private void AppendToLogTextBox(string text)
        //{
        //    if (txtLog.InvokeRequired)
        //    {
        //        txtLog.Invoke(new Action(() =>
        //        {
        //            txtLog.AppendText(text + Environment.NewLine);
        //            txtLog.ScrollToCaret();
        //        }));
        //    }
        //    else
        //    {
        //        txtLog.AppendText(text + Environment.NewLine);
        //        txtLog.ScrollToCaret();
        //    }
        //}

        //private void InitializeNotifyIcon()
        //{
        //    NotifyIcon notifyIcon = new NotifyIcon
        //    {
        //        Icon = new Icon(Path.Combine(Application.StartupPath, "LinkDoctor.ico")),
        //        Visible = true,
        //        Text = "Internet Connection Logger"
        //    };

        //    ContextMenuStrip contextMenu = new ContextMenuStrip();
        //    ToolStripMenuItem restoreMenuItem = new ToolStripMenuItem("Restore");
        //    ToolStripMenuItem exitMenuItem = new ToolStripMenuItem("Exit");
        //    contextMenu.Items.Add(restoreMenuItem);
        //    contextMenu.Items.Add(exitMenuItem);

        //    restoreMenuItem.Click += (sender, e) => this.Show();
        //    exitMenuItem.Click += (sender, e) =>
        //    {
        //        System.Net.NetworkInformation.NetworkChange.NetworkAvailabilityChanged -= NetworkAvailabilityChangedHandler;
        //        Application.Exit();
        //    };

        //    notifyIcon.ContextMenuStrip = contextMenu;

        //    this.FormClosing += (sender, e) =>
        //    {
        //        if (this.WindowState == FormWindowState.Minimized)
        //        {
        //            e.Cancel = true;
        //            this.Hide();
        //        }
        //    };
        //}

        #endregion v1

        #region v2
        /*
           public interface INetworkEndpoint
    {
        string Name { get; }
        Task<(bool Success, string Details)> CheckConnectivityAsync();
    }

    public interface INetworkMonitor
    {
        event EventHandler<ConnectionStateChangedEventArgs> ConnectionStateChanged;
        Task StartMonitoringAsync();
        Task StopMonitoringAsync();
    }

    public class ConnectionStateChangedEventArgs : EventArgs
    {
        public bool IsConnected { get; }
        public string Details { get; }
        public DateTime Timestamp { get; }
        public TimeSpan? Downtime { get; }

        public ConnectionStateChangedEventArgs(bool isConnected, string details, DateTime timestamp, TimeSpan? downtime = null)
        {
            IsConnected = isConnected;
            Details = details;
            Timestamp = timestamp;
            Downtime = downtime;
        }
    }

    public class ConnectionSettings
    {
        public int CheckIntervalMs { get; set; } = 10000;
        public int TimeoutMs { get; set; } = 500;
        public int RetryAttempts { get; set; } = 3;
        public int RetryDelayMs { get; set; } = 1000;
        public string LogFilePath { get; set; } = "InternetConnectionLog.txt";
        public long MaxLogSizeBytes { get; set; } = 10485760; // 10MB
        public List<EndpointConfiguration> Endpoints { get; set; } = new();

        public static ConnectionSettings LoadFromFile(string path)
        {
            if (File.Exists(path))
            {
                string json = File.ReadAllText(path);
                return JsonSerializer.Deserialize<ConnectionSettings>(json) ?? new ConnectionSettings();
            }
            return new ConnectionSettings();
        }

        public void SaveToFile(string path)
        {
            string json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(path, json);
        }
    }

    public class EndpointConfiguration
    {
        public string Name { get; set; } = "";
        public string Address { get; set; } = "";
        public bool IsDomain { get; set; }
        public int Priority { get; set; }
    }

    public class PingEndpoint : INetworkEndpoint, IDisposable
    {
        private readonly string address;
        private readonly int timeout;
        private readonly Ping ping;

        public string Name { get; }

        public PingEndpoint(string name, string address, int timeout)
        {
            Name = name;
            this.address = address;
            this.timeout = timeout;
            ping = new Ping();
        }

        public async Task<(bool Success, string Details)> CheckConnectivityAsync()
        {
            try
            {
                var reply = await ping.SendPingAsync(address, timeout);
                string statusDetails = GetStatusDetails(reply.Status);

                return (reply.Status == IPStatus.Success,
                    $"Ping to {Name} ({address}): {statusDetails}{(reply.Status == IPStatus.Success ? $", RTT: {reply.RoundtripTime}ms" : "")}");
            }
            catch (PingException ex) when (ex.InnerException is SocketException socketEx)
            {
                return (false, $"Ping to {Name} ({address}): {GetSocketErrorDetails(socketEx.ErrorCode)}");
            }
            catch (Exception ex)
            {
                return (false, $"Ping to {Name} ({address}) failed: {ex.Message}");
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

    public class DnsEndpoint : INetworkEndpoint
    {
        public string Name { get; }
        private readonly string domain;
        private readonly int timeout;

        public DnsEndpoint(string name, string domain, int timeout)
        {
            Name = name;
            this.domain = domain;
            this.timeout = timeout;
        }

        public async Task<(bool Success, string Details)> CheckConnectivityAsync()
        {
            try
            {
                var result = await Dns.GetHostEntryAsync(domain);
                return (true, $"DNS resolution successful for {domain}");
            }
            catch (SocketException ex)
            {
                return (false, $"DNS resolution failed for {domain}: {ex.Message}");
            }
        }
    }

    public class NetworkMonitor : INetworkMonitor, IDisposable
    {
        private readonly List<INetworkEndpoint> endpoints;
        private readonly ConnectionSettings settings;
        private readonly ILogger logger;
        private readonly object lockObject = new();
        private CancellationTokenSource? cts;
        private DateTime? disconnectedTime;
        private bool previousConnectionStatus = true;
        private bool isDisposed;

        public event EventHandler<ConnectionStateChangedEventArgs>? ConnectionStateChanged;

        public NetworkMonitor(ConnectionSettings settings, ILogger logger)
        {
            this.settings = settings;
            this.logger = logger;
            endpoints = InitializeEndpoints();

            NetworkChange.NetworkAvailabilityChanged += NetworkAvailabilityChangedHandler;
            NetworkChange.NetworkAddressChanged += NetworkAddressChangedHandler;
        }

        private List<INetworkEndpoint> InitializeEndpoints()
        {
            var endpoints = new List<INetworkEndpoint>();

            // Add default endpoints if none configured
            if (!settings.Endpoints.Any())
            {
                endpoints.Add(new PingEndpoint("Google DNS", "8.8.8.8", settings.TimeoutMs));
                endpoints.Add(new PingEndpoint("Cloudflare DNS", "1.1.1.1", settings.TimeoutMs));
                endpoints.Add(new DnsEndpoint("Google DNS Resolution", "dns.google", settings.TimeoutMs));
            }
            else
            {
                foreach (var config in settings.Endpoints.OrderBy(e => e.Priority))
                {
                    if (config.IsDomain)
                        endpoints.Add(new DnsEndpoint(config.Name, config.Address, settings.TimeoutMs));
                    else
                        endpoints.Add(new PingEndpoint(config.Name, config.Address, settings.TimeoutMs));
                }
            }

            return endpoints;
        }

        public async Task StartMonitoringAsync()
        {
            cts = new CancellationTokenSource();

            while (!cts.Token.IsCancellationRequested)
            {
                await CheckConnectivityAsync();
                await Task.Delay(settings.CheckIntervalMs, cts.Token);
            }
        }

        public Task StopMonitoringAsync()
        {
            cts?.Cancel();
            return Task.CompletedTask;
        }

        private async Task CheckConnectivityAsync()
        {
            var diagnostics = new List<string>();
            var isConnected = false;
            var successfulEndpoint = string.Empty;

            foreach (var endpoint in endpoints)
            {
                var endpointSucceeded = false;

                for (int attempt = 1; attempt <= settings.RetryAttempts; attempt++)
                {
                    try
                    {
                        var (success, endpointResult) = await endpoint.CheckConnectivityAsync();

                        // Only add attempt number if we're retrying
                        if (settings.RetryAttempts > 1)
                            diagnostics.Add($"Attempt {attempt}/{settings.RetryAttempts}: {endpointResult}");
                        else
                            diagnostics.Add(endpointResult);

                        if (success)
                        {
                            isConnected = true;
                            endpointSucceeded = true;
                            successfulEndpoint = endpoint.Name;
                            break;
                        }

                        if (attempt < settings.RetryAttempts)
                            await Task.Delay(settings.RetryDelayMs);
                    }
                    catch (Exception ex)
                    {
                        diagnostics.Add($"Error checking {endpoint.Name} (Attempt {attempt}/{settings.RetryAttempts}): {ex.Message}");
                    }
                }

                if (endpointSucceeded)
                    break;
            }

            var diagnosticSummary = FormatDiagnostics(diagnostics, isConnected, successfulEndpoint);
            UpdateConnectionState(isConnected, diagnosticSummary);
        }

        private string FormatDiagnostics(List<string> diagnostics, bool isConnected, string successfulEndpoint)
        {
            var sb = new StringBuilder();

            if (isConnected && !string.IsNullOrEmpty(successfulEndpoint))
                sb.AppendLine($"Connection verified through: {successfulEndpoint}");

            foreach (var diagnostic in diagnostics)
                sb.AppendLine(diagnostic);

            return sb.ToString().TrimEnd();
        }

        private void UpdateConnectionState(bool currentStatus, string details)
        {
            lock (lockObject)
            {
                if (currentStatus != previousConnectionStatus)
                {
                    var now = DateTime.Now;
                    TimeSpan? downtime = null;

                    if (!currentStatus)
                    {
                        disconnectedTime = now;
                        LogConnectionEvent(false, details);
                    }
                    else if (disconnectedTime.HasValue)
                    {
                        downtime = now - disconnectedTime.Value;
                        LogConnectionEvent(true, details, downtime);
                        disconnectedTime = null;
                    }

                    var args = new ConnectionStateChangedEventArgs(currentStatus, details, now, downtime);
                    ConnectionStateChanged?.Invoke(this, args);
                    previousConnectionStatus = currentStatus;
                }
            }
        }

        private void LogConnectionEvent(bool isConnected, string details, TimeSpan? downtime = null)
        {
            var timestamp = DateTime.Now;
            var sb = new StringBuilder();

            sb.AppendLine($"[{timestamp:yyyy-MM-dd HH:mm:ss}] {(isConnected ? "Connection Restored" : "Connection Lost")}");

            if (downtime.HasValue)
                sb.AppendLine($"Downtime Duration: {FormatDowntime(downtime.Value)}");

            sb.AppendLine("Connection Details:");
            sb.AppendLine(details);

            // Check physical connection
            var networkInterfaces = NetworkInterface.GetAllNetworkInterfaces();
            var activeInterfaces = networkInterfaces.Where(nic =>
                nic.NetworkInterfaceType != NetworkInterfaceType.Loopback &&
                nic.OperationalStatus == OperationalStatus.Up).ToList();

            sb.AppendLine("\nNetwork Interfaces Status:");
            if (!activeInterfaces.Any())
            {
                sb.AppendLine("No active network interfaces found!");
            }
            else
            {
                foreach (var nic in networkInterfaces)
                {
                    if (nic.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                    {
                        sb.AppendLine($"Interface: {nic.Name}");
                        sb.AppendLine($"Type: {nic.NetworkInterfaceType}");
                        sb.AppendLine($"Status: {nic.OperationalStatus}");

                        var ipProps = nic.GetIPProperties();
                        if (ipProps.GatewayAddresses.Count == 0)
                            sb.AppendLine("Warning: No gateway configured");

                        sb.AppendLine();
                    }
                }
            }

            logger.LogInformation(sb.ToString());
        }

        private string FormatDowntime(TimeSpan duration)
        {
            if (duration.TotalSeconds < 60)
                return $"{duration.TotalSeconds:F1} seconds";
            if (duration.TotalMinutes < 60)
                return $"{duration.TotalMinutes:F1} minutes";
            return $"{duration.TotalHours:F1} hours";
        }

        private void NetworkAvailabilityChangedHandler(object? sender, NetworkAvailabilityEventArgs e)
        {
            Task.Run(CheckConnectivityAsync);
        }

        private void NetworkAddressChangedHandler(object? sender, EventArgs e)
        {
            Task.Run(CheckConnectivityAsync);
        }

        public void Dispose()
        {
            if (!isDisposed)
            {
                NetworkChange.NetworkAvailabilityChanged -= NetworkAvailabilityChangedHandler;
                NetworkChange.NetworkAddressChanged -= NetworkAddressChangedHandler;

                foreach (var endpoint in endpoints)
                {
                    if (endpoint is IDisposable disposable)
                        disposable.Dispose();
                }

                cts?.Cancel();
                cts?.Dispose();

                isDisposed = true;
            }
        }
    }

    public partial class FrmMain : Form
    {
        private readonly INetworkMonitor monitor;
        private readonly ILogger logger;
        private NotifyIcon? notifyIcon;

        public FrmMain()
        {
            InitializeComponent();

            var settings = ConnectionSettings.LoadFromFile("connection-settings.json");
            logger = CreateLogger(settings);
            monitor = new NetworkMonitor(settings, logger);
            monitor.ConnectionStateChanged += ConnectionStateChangedHandler;

            InitializeNotifyIcon();
            InitializeUI();

            FormClosing += FrmMain_FormClosing;
            _ = monitor.StartMonitoringAsync();
        }

        private ILogger CreateLogger(ConnectionSettings settings)
        {
            // Configure Serilog
            var serilogLogger = new LoggerConfiguration()
                .WriteTo.File(settings.LogFilePath,
                    rollingInterval: RollingInterval.Day,
                    fileSizeLimitBytes: settings.MaxLogSizeBytes,
                    rollOnFileSizeLimit: true,
                    retainedFileCountLimit: 31)
                .CreateLogger();

            // Create the logger factory using Serilog
            var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddSerilog(serilogLogger);
            });

            return loggerFactory.CreateLogger<FrmMain>();
        }

        private void ConnectionStateChangedHandler(object? sender, ConnectionStateChangedEventArgs e)
        {
            UpdateUI(e.IsConnected, e.Details, e.Timestamp, e.Downtime);
        }

        private void UpdateUI(bool isConnected, string details, DateTime timestamp, TimeSpan? downtime)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => UpdateUI(isConnected, details, timestamp, downtime)));
                return;
            }

            txtLog.AppendText($"[{timestamp:yyyy-MM-dd HH:mm:ss}] ");
            if (!isConnected)
            {
                txtLog.AppendText("Connection Lost\r\n");
                notifyIcon!.Icon = new Icon(Path.Combine(Application.StartupPath, "LinkDoctor.ico"));// Properties.Resources.DisconnectedIcon;
            }
            else
            {
                txtLog.AppendText($"Connection Restored (Down for {FormatDowntime(downtime!.Value)})\r\n");
                notifyIcon!.Icon = new Icon(Path.Combine(Application.StartupPath, "LinkDoctor.ico"));// Properties.Resources.ConnectedIcon;
            }

            txtLog.AppendText($"Details:\r\n{details}\r\n\r\n");
            txtLog.ScrollToCaret();
        }

        private string FormatDowntime(TimeSpan duration)
        {
            if (duration.TotalSeconds < 60)
                return $"{duration.TotalSeconds:F1} seconds";
            if (duration.TotalMinutes < 60)
                return $"{duration.TotalMinutes:F1} minutes";
            return $"{duration.TotalHours:F1} hours";
        }

        private void InitializeUI()
        {
            // Add UI initialization code here
            Text = "Internet Connection Monitor";
            txtLog.Multiline = true;
            txtLog.ScrollBars = ScrollBars.Vertical;
            txtLog.ReadOnly = true;
            txtLog.Dock = DockStyle.Fill;
        }

        private void InitializeNotifyIcon()
        {
            notifyIcon = new NotifyIcon
            {
                Icon = new Icon(Path.Combine(Application.StartupPath, "LinkDoctor.ico")), //Properties.Resources.ConnectedIcon,
                Visible = true,
                Text = "Internet Connection Monitor"
            };

            var contextMenu = new ContextMenuStrip();
            var restoreMenuItem = new ToolStripMenuItem("Restore");
            var exitMenuItem = new ToolStripMenuItem("Exit");

            restoreMenuItem.Click += (s, e) => Show();
            exitMenuItem.Click += (s, e) => Application.Exit();

            contextMenu.Items.AddRange(new ToolStripItem[] { restoreMenuItem, exitMenuItem });
            notifyIcon.ContextMenuStrip = contextMenu;

            notifyIcon.DoubleClick += (s, e) => Show();
        }

        private void FrmMain_FormClosing(object? sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                Hide();
            }
            else
            {
                monitor.StopMonitoringAsync().Wait();
                if (monitor is IDisposable disposable)
                    disposable.Dispose();
                notifyIcon?.Dispose();
            }
        }
    }

        */

        #endregion
    }
}


/*
enhance and improve the following code and I want it to be faster at detecting any kind of network drops and log it correctly

current task
with claude hauku
current version: v2
next version: v3
prompt: 

let's make the following improvement first:-
I need it to be more robust at detecting connection changes, and give diagnostics telling if connection problem in which connection, for example if my local network connection to the router or if from the router to street wires in street to phone line cabinet in street for ADSL connection to the ISP etc.. tracing the source of disconnection or unstable connection due to inference


 */