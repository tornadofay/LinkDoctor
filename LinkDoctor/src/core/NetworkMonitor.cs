using LinkDoctor.src.Endpoints;
using LinkDoctor.src.Interfaces;
using LinkDoctor.src.Models;
using Microsoft.Extensions.Logging;
using System.Net.NetworkInformation;
using System.Text;

namespace LinkDoctor.src.core
{

    public class NetworkMonitor : IDisposable
    {
        private readonly List<INetworkEndpoint> endpoints;
        private readonly ConnectionSettings settings;
        private readonly ILogger<NetworkMonitor> logger;
        private readonly object lockObject = new();
        private CancellationTokenSource? cts;
        private DateTime? disconnectedTime;
        private bool previousConnectionStatus = true;
        private bool isDisposed;

        private int consecutiveSuccessCount = 0;
        private readonly int maxSuccessLogs = 3;
        private bool silentMode = false;
        private bool isDiagnosticsRunning = false;


        public bool IsInSilentMode
        {
            get { return silentMode; }
        }

        #region Events

        public event EventHandler<ConnectionStateChangedEventArgs>? ConnectionStateChanged;
        public event EventHandler<EndpointDiagnosticsEventArgs>? EndpointDiagnosticsChanged;
        public event EventHandler<DiagnosticStepEventArgs>? DiagnosticStepChanged;
        public event EventHandler<ComprehensiveDiagnosticsCompletedEventArgs>? ComprehensiveDiagnosticsCompleted;
        public event EventHandler<PingTimeChangedEventArgs>? PingTimeChanged;

        public event EventHandler<EventArgs>? SilentModeEntered;

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

        public class EndpointDiagnosticsEventArgs : EventArgs
        {
            public bool IsConnected { get; }
            public string EndpointName { get; }
            public string Details { get; }

            public EndpointDiagnosticsEventArgs(bool isConnected, string endpointName, string details)
            {
                IsConnected = isConnected;
                EndpointName = endpointName;
                Details = details;
            }
        }

        public class DiagnosticStepEventArgs : EventArgs
        {
            public string StepName { get; }
            public bool IsSuccessful { get; }
            public string Details { get; }

            public DiagnosticStepEventArgs(string stepName, bool isSuccessful, string details)
            {
                StepName = stepName;
                IsSuccessful = isSuccessful;
                Details = details;
            }
        }

        public class ComprehensiveDiagnosticsCompletedEventArgs : EventArgs
        {
            public ConnectionDiagnostics Diagnostics { get; }

            public ComprehensiveDiagnosticsCompletedEventArgs(ConnectionDiagnostics diagnostics)
            {
                Diagnostics = diagnostics;
            }
        }

        public class PingTimeChangedEventArgs : EventArgs
        {
            public long? PingTime { get; }
            public DateTime Timestamp { get; }

            public PingTimeChangedEventArgs(long? pingTime, DateTime timestamp)
            {
                PingTime = pingTime;
                Timestamp = timestamp;
            }
        }

        #endregion Events

        public NetworkMonitor(ConnectionSettings settings, ILogger<NetworkMonitor> logger)
        {
            this.settings = settings;
            this.logger = logger;
            endpoints = InitializeEndpoints();

            NetworkChange.NetworkAvailabilityChanged += NetworkAvailabilityChangedHandler;
            NetworkChange.NetworkAddressChanged += NetworkAddressChangedHandler;
        }

        public void PauseMonitoring()
        {
            cts?.Cancel();
        }

        public void ResumeMonitoring()
        {
            cts = new CancellationTokenSource();
            _ = Task.Run(() => MonitoringLoop(cts.Token), cts.Token);
        }

        private List<INetworkEndpoint> InitializeEndpoints()
        {
            var endpoints = new List<INetworkEndpoint>();

            if (!settings.Endpoints.Any())
            {
                endpoints.Add(new PingEndpoint("Google DNS", "8.8.8.8", settings.TimeoutMs));
                endpoints.Add(new PingEndpoint("Cloudflare DNS", "1.1.1.1", settings.TimeoutMs));
                endpoints.Add(new HttpEndpoint("Google HTTP Check", "http://www.gstatic.com/generate_204", settings.TimeoutMs));
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
          //  endpoints.Add(new HttpEndpoint("Google HTTP Check", "http://www.gstatic.com/generate_204", settings.TimeoutMs));


            return endpoints;
        }

        public async Task StartMonitoringAsync()
        {
            cts = new CancellationTokenSource();
            await Task.Run(() => MonitoringLoop(cts.Token), cts.Token);
        }

        private async void MonitoringLoop(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    await CheckConnectivityAsync(token);
                    await Task.Delay(settings.CheckIntervalMs, token);
                }
                catch (OperationCanceledException)
                {
                    // Monitoring loop canceled
                    break;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error in monitoring loop");
                }
            }
        }

        public async Task<ConnectionDiagnostics> PerformComprehensiveDiagnostics(int retryAttempts = 1, int retryDelayMs = 0)
        {
            if (isDiagnosticsRunning)
            {
                // Prevent multiple diagnostics from running
                return new ConnectionDiagnostics();
            }

            isDiagnosticsRunning = true;

            try
            {
                var diagnostics = new ConnectionDiagnostics
                {
                    ComponentStatus = new List<(string?, bool)>()
                };

                var diagnosticsEndpoints = new List<INetworkEndpoint>
                {
                    new LocalNetworkEndpoint(),
                    new GatewayEndpoint(),
                    new UplinkEndpoint()
                };
                diagnosticsEndpoints.AddRange(InitializeEndpoints());
                if (!diagnosticsEndpoints.Any(e => e is DnsEndpoint))
                {
                    diagnosticsEndpoints.Add(new DnsEndpoint("Google DNS Resolution", "dns.google", 1000));
                }

                foreach (var endpoint in diagnosticsEndpoints)
                {
                    var (success, diagnosticsResult) = await endpoint.DiagnoseConnectivityAsync();

                    diagnostics.ComponentStatus.Add((endpoint.Name, success));

                    DiagnosticStepChanged?.Invoke(this, new DiagnosticStepEventArgs(endpoint.Name, success, diagnosticsResult.DetailedErrorDescription ?? "Success"));

                    if (!success)
                    {
                        diagnostics.FailedLayer = endpoint.Layer;
                        diagnostics.DetailedErrorDescription = diagnosticsResult.DetailedErrorDescription;
                        break;
                    }

                    await Task.Delay(retryDelayMs);
                }
                ComprehensiveDiagnosticsCompleted?.Invoke(this, new ComprehensiveDiagnosticsCompletedEventArgs(diagnostics));

                return diagnostics;
            }
            finally
            {
                isDiagnosticsRunning = false;
            }
        }

        private async Task<ConnectionDiagnostics> CheckConnectivityAsync(CancellationToken token)
        {
            var diagnostics = new List<string>();
            var isConnected = false;
            string? successfulEndpoint = null;
            long? pingTime = null;

            foreach (var endpoint in endpoints)
            {
                bool endpointSucceeded = false;

                for (int attempt = 1; attempt <= settings.RetryAttempts; attempt++)
                {
                    token.ThrowIfCancellationRequested();

                    try
                    {
                        var (success, endpointDiagnostics) = await endpoint.DiagnoseConnectivityAsync();

                        if (success)
                        {
                            isConnected = true;
                            endpointSucceeded = true;
                            successfulEndpoint = endpoint.Name;
                            pingTime = endpointDiagnostics.PingTime;

                            EmitSuccessLog(endpoint.Name, endpointDiagnostics.PingTime);

                            // Invoke PingTimeChanged event if needed
                            if (endpointDiagnostics.PingTime.HasValue)
                            {
                                PingTimeChanged?.Invoke(this, new PingTimeChangedEventArgs(endpointDiagnostics.PingTime.Value, DateTime.Now));
                            }

                            break;
                        }

                        if (attempt < settings.RetryAttempts)
                        {
                            await Task.Delay(settings.RetryDelayMs, token);
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        diagnostics.Add($"Error checking {endpoint.Name}: {ex.Message}");
                        logger.LogError(ex, "Error during endpoint connectivity check");
                    }
                }

                if (endpointSucceeded)
                {
                    break;
                }
                else
                {
                    diagnostics.Add($"Minor connection issue detected at {endpoint.Name}");
                }
            }

            if (!isConnected)
            {
                diagnostics.Add("Connection lost. Performing comprehensive diagnostics...");
                var comprehensiveDiagnostics = await PerformComprehensiveDiagnostics(1, 0);

                // Handle comprehensive diagnostics result (omitted for brevity)

                UpdateConnectionState(false, string.Join("\n", diagnostics));
            }
            else
            {
                UpdateConnectionState(true, $"Connected via {successfulEndpoint}");
            }

            return new ConnectionDiagnostics
            {
                IsSuccessful = isConnected,
                PingTime = pingTime,
                DetailedErrorDescription = string.Join("\n", diagnostics)
            };
        }

        private void EmitSuccessLog(string endpointName, long? pingTime)
        {
            if (!silentMode)
            {
                consecutiveSuccessCount++;

                var timestamp = DateTime.Now;

                // Build the log message with ping time
                var logMessage = $"Connected via {endpointName}";
                if (pingTime.HasValue)
                {
                    logMessage += $" [{pingTime.Value}ms]";
                }

                logger.LogInformation($"[{timestamp:yyyy-MM-dd HH:mm:ss}] {logMessage}\r\nDetails: Success\r\n");

                // Raise the event to update the UI
                ConnectionStateChanged?.Invoke(this, new ConnectionStateChangedEventArgs(
                    true,
                    logMessage,
                    timestamp,
                    null));

                if (consecutiveSuccessCount >= maxSuccessLogs)
                {
                    // Enter silent mode after the maximum number of success logs
                    silentMode = true;
                    logger.LogInformation("Entering silent mode.");

                    // Raise the SilentModeEntered event
                    SilentModeEntered?.Invoke(this, EventArgs.Empty);
                }
            }
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
                        // Connection lost
                        disconnectedTime = now;
                        // Reset counters
                        consecutiveSuccessCount = 0;
                        silentMode = false;

                        // Log connection lost event and run diagnostics
                        LogConnectionEvent(new ConnectionDiagnostics
                        {
                            FailedLayer = ConnectionDiagnostics.ConnectionLayer.GeneralInternet,
                            DetailedErrorDescription = details,
                            ComponentStatus = new List<(string?, bool)> { ("Connection", false) }
                        });

                        // Perform comprehensive diagnostics
                        _ = Task.Run(async () =>
                        {
                            await PerformComprehensiveDiagnostics();
                        });
                    }
                    else if (disconnectedTime.HasValue)
                    {
                        // Connection restored
                        downtime = now - disconnectedTime.Value;
                        disconnectedTime = null;

                        // Reset counters
                        consecutiveSuccessCount = 0;
                        silentMode = false;

                        // Log connection restored event
                        LogConnectionEvent(new ConnectionDiagnostics
                        {
                            ComponentStatus = new List<(string?, bool)> { ("Connection", true) },
                            DetailedErrorDescription = $"Connection restored after {FormatDowntime(downtime.Value)}"
                        });

                        // Optionally perform diagnostics after restoration
                        _ = Task.Run(async () =>
                        {
                            await PerformComprehensiveDiagnostics();
                        });
                    }

                    // Update previousConnectionStatus
                    previousConnectionStatus = currentStatus;

                    // Raise ConnectionStateChanged event
                    ConnectionStateChanged?.Invoke(this, new ConnectionStateChangedEventArgs(
                        currentStatus,
                        details,
                        now,
                        downtime));
                }
            }
        }


        private void LogConnectionEvent(ConnectionDiagnostics diagnostics, TimeSpan? downtime = null)
        {
            var logMessage = new StringBuilder();

            if (diagnostics.ComponentStatus != null)
            {
                foreach (var component in diagnostics.ComponentStatus)
                {
                    logMessage.AppendLine($"{component.ComponentName}: {(component.IsReachable ? "Operational" : "Issue Detected")}");
                }
            }

            if (!string.IsNullOrEmpty(diagnostics.DetailedErrorDescription))
            {
                logMessage.AppendLine($"Details: {diagnostics.DetailedErrorDescription}");
            }

            if (downtime.HasValue)
            {
                logMessage.AppendLine($"Downtime: {FormatDowntime(downtime.Value)}");
            }

            if (diagnostics.FailedLayer != ConnectionDiagnostics.ConnectionLayer.GeneralInternet)
            {
                logger.LogWarning("Connection issue detected at layer: {FailedLayer}. Details: {Details}", diagnostics.FailedLayer, logMessage.ToString());
            }
            else
            {
                logger.LogInformation("Connection state changed. Details: {Details}", logMessage.ToString());
            }
        }

        private string FormatDowntime(TimeSpan duration)
        {
            if (duration.TotalSeconds < 60)
                return $"{duration.TotalSeconds:F1} seconds";
            if (duration.TotalMinutes < 60)
                return $"{duration.TotalMinutes:F1} minutes";

            return $"{duration.TotalHours:F1} hours";
        }

        private async void NetworkAvailabilityChangedHandler(object? sender, NetworkAvailabilityEventArgs e)
        {
            await CheckConnectivityAsync(CancellationToken.None);
        }

        private async void NetworkAddressChangedHandler(object? sender, EventArgs e)
        {
            await CheckConnectivityAsync(CancellationToken.None);
        }

        public void Dispose()
        {
            if (!isDisposed)
            {
                NetworkChange.NetworkAvailabilityChanged -= NetworkAvailabilityChangedHandler;
                NetworkChange.NetworkAddressChanged -= NetworkAddressChangedHandler;
                cts?.Cancel();
                cts?.Dispose();

                foreach (var endpoint in endpoints)
                {
                    if (endpoint is IDisposable disposable)
                        disposable.Dispose();
                }

                isDisposed = true;
            }
        }
    }
}
