using LinkDoctor.src.core;
using LinkDoctor.src.Models;
using Microsoft.Extensions.Logging;
using Serilog;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
 
namespace LinkDoctor
{
    public partial class FrmMain : Form
    {
        private readonly NetworkMonitor monitor;
        private readonly ILogger<NetworkMonitor> logger;
        private NotifyIcon? notifyIcon;
        private ConcurrentQueue<string> diagnosticMessages = new ConcurrentQueue<string>();
        private System.Windows.Forms.Timer diagnosticTimer = new System.Windows.Forms.Timer();
        private int expectedDiagnosticSteps = 0;
        private int receivedDiagnosticSteps = 0;
        private bool diagnosticSummaryPending = false;
        private string pendingSummary = "";
        private CancellationTokenSource cts = new CancellationTokenSource();
        private bool previousConnectionStatus = true; // Assuming connection is up initially

        public FrmMain()
        {
            InitializeComponent();
           

            diagnosticTimer.Interval = 100;
            diagnosticTimer.Tick += DiagnosticTimer_Tick;

            var settings = ConnectionSettings.LoadFromFile("connection-settings.json");
            logger = CreateLogger(settings);

            monitor = new NetworkMonitor(settings, logger);

            InitializeNotifyIcon();
            InitializeUI();

            

            btnDiagnose.Click += BtnDiagnose_Click;

           
            monitor.ConnectionStateChanged += ConnectionStateChangedHandler;
            monitor.EndpointDiagnosticsChanged += EndpointDiagnosticsChangedHandler;
            monitor.DiagnosticStepChanged += DiagnosticStepChangedHandler;
            monitor.ComprehensiveDiagnosticsCompleted += ComprehensiveDiagnosticsCompletedHandler;
            monitor.PingTimeChanged += OnPingTimeChanged;
            monitor.SilentModeEntered += Monitor_SilentModeEntered;

            btnClear.Click += btnClear_Click;
            btnCancel.Click += btnCancel_Click_1;
            btnTraceRoute.Click += btnTraceRoute_Click;
            btnPing.Click += btnPing_Click;

            DisplayGreeting();

            _ = monitor.StartMonitoringAsync();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                monitor.Dispose();
                btnClear.Click -= btnClear_Click;
                btnCancel.Click -= btnCancel_Click_1;
                btnTraceRoute.Click -= btnTraceRoute_Click;
                btnPing.Click -= btnPing_Click;
                components?.Dispose();
            }
            base.Dispose(disposing);
        }

        private async void btnPing_Click(object sender, EventArgs e)
        {
            string target = txtRoute.Text.Trim();
            if (string.IsNullOrEmpty(target))
            {
                string message = "Please enter a target address.\r\n" +
                    "Examples:\r\n" +
                    "- Website: www.google.com\r\n" +
                    "- DNS server: 8.8.8.8\r\n" +
                    "- Local network device: 192.168.1.1\r\n\r\n" +
                    "You can copy and paste any of these examples into the textbox and click Ping or Trace Route to perform diagnostics.\r\n\r\n";

                txtComprehensiveDiagnosticSummary.AppendText(message);
                return;
            }

            if (!IsValidTarget(target))
            {
                txtComprehensiveDiagnosticSummary.AppendText("Invalid target address.\r\n");
                return;
            }

            prg.Style = ProgressBarStyle.Marquee;
            prg.Visible = true;
            btnCancel.Enabled = true;
            cts = new CancellationTokenSource();

            Ping ping = new Ping();
            IPAddress address;

            // Attempt to parse the target as an IPAddress
            if (IPAddress.TryParse(target, out address))
            {
                try
                {
                    PingReply reply = await ping.SendPingAsync(address, TimeSpan.FromMilliseconds(1000), new byte[32], null, cts.Token);
                    txtComprehensiveDiagnosticSummary.AppendText($"[{DateTime.Now}] PING to {target}: ");
                    if (reply.Status == IPStatus.Success)
                    {
                        txtComprehensiveDiagnosticSummary.AppendText($"Success, Time={reply.RoundtripTime}ms\r\n");
                    }
                    else
                    {
                        txtComprehensiveDiagnosticSummary.AppendText($"Failed, Status={reply.Status}\r\n");
                    }
                }
                catch (OperationCanceledException)
                {
                    txtComprehensiveDiagnosticSummary.AppendText($"[{DateTime.Now}] PING to {target} was canceled.\r\n");
                }
                catch (Exception ex)
                {
                    txtComprehensiveDiagnosticSummary.AppendText($"[{DateTime.Now}] PING to {target} failed: {ex.Message}\r\n");
                }
                finally
                {
                    prg.Visible = false;
                    btnCancel.Enabled = false;
                }
            }
            else
            {
                // Resolve hostname to IP address
                try
                {
                    IPAddress[] addresses = await Dns.GetHostAddressesAsync(target);
                    if (addresses.Length > 0)
                    {
                        address = addresses[0];
                        PingReply reply = await ping.SendPingAsync(address, TimeSpan.FromMilliseconds(1000), new byte[32], null, cts.Token);
                        txtComprehensiveDiagnosticSummary.AppendText($"[{DateTime.Now}] PING to {target}: ");
                        if (reply.Status == IPStatus.Success)
                        {
                            txtComprehensiveDiagnosticSummary.AppendText($"Success, Time={reply.RoundtripTime}ms\r\n");
                        }
                        else
                        {
                            txtComprehensiveDiagnosticSummary.AppendText($"Failed, Status={reply.Status}\r\n");
                        }
                    }
                    else
                    {
                        txtComprehensiveDiagnosticSummary.AppendText($"[{DateTime.Now}] PING to {target} failed: No IP address found.\r\n");
                    }
                }
                catch (OperationCanceledException)
                {
                    txtComprehensiveDiagnosticSummary.AppendText($"[{DateTime.Now}] PING to {target} was canceled.\r\n");
                }
                catch (Exception ex)
                {
                    txtComprehensiveDiagnosticSummary.AppendText($"[{DateTime.Now}] PING to {target} failed: {ex.Message}\r\n");
                }
                finally
                {
                    prg.Visible = false;
                    btnCancel.Enabled = false;
                }
            }
        }

        private async void btnTraceRoute_Click(object sender, EventArgs e)
        {
            string target = txtRoute.Text.Trim();
            if (string.IsNullOrEmpty(target))
            {
                string message = "Please enter a target address.\r\n" +
                     "Examples:\r\n" +
                     "- Website: www.google.com\r\n" +
                     "- DNS server: 8.8.8.8\r\n" +
                     "- Local network device: 192.168.1.1\r\n\r\n" +
                     "You can copy and paste any of these examples into the textbox and click Ping or Trace Route to perform diagnostics.\r\n\r\n";

                txtComprehensiveDiagnosticSummary.AppendText(message);
                return;
            }

            if (!IsValidTarget(target))
            {
                txtComprehensiveDiagnosticSummary.AppendText("Invalid target address.\r\n");
                return;
            }

            prg.Style = ProgressBarStyle.Marquee;
            prg.Visible = true;
            btnCancel.Enabled = true;
            cts = new CancellationTokenSource();

            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = "tracert",
                Arguments = target,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (Process process = new Process())
            {
                process.StartInfo = psi;
                process.OutputDataReceived += (senderProcess, eOutput) =>
                {
                    if (!string.IsNullOrEmpty(eOutput.Data))
                    {
                        txtComprehensiveDiagnosticSummary.Invoke(new Action(() =>
                        {
                            txtComprehensiveDiagnosticSummary.AppendText($"[{DateTime.Now}] {eOutput.Data}\r\n");
                        }));
                        if (int.TryParse(eOutput.Data.Split(':')[0].Trim(), out int hop))
                        {
                            prg.Value = hop;
                        }
                    }
                };
                process.ErrorDataReceived += (senderProcess, eError) =>
                {
                    if (!string.IsNullOrEmpty(eError.Data))
                    {
                        txtComprehensiveDiagnosticSummary.Invoke(new Action(() =>
                        {
                            txtComprehensiveDiagnosticSummary.AppendText($"[{DateTime.Now}] Error: {eError.Data}\r\n");
                        }));
                    }
                };

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                while (!process.HasExited)
                {
                    await Task.Delay(100);
                    if (cts.IsCancellationRequested)
                    {
                        try
                        {
                            process.Kill();
                        }
                        catch { }
                        break;
                    }
                }
            }

            prg.Visible = false;
            btnCancel.Enabled = false;
        }

        private bool IsValidTarget(string target)
        {
            if (string.IsNullOrEmpty(target))
            {
                return false;
            }

            string uriTarget = target;
            if (!uriTarget.Contains("://"))
            {
                uriTarget = "http://" + uriTarget;
            }

            // Check if it's a valid IP address
            if (IPAddress.TryParse(target, out _))
            {
                return true;
            }

            // Check if it's a valid URL
            if (Uri.IsWellFormedUriString(uriTarget, UriKind.Absolute))
            {
                Uri uri = new Uri(uriTarget);
                return !string.IsNullOrEmpty(uri.Host);
            }

            return false;
        }

        private void btnClear_Click(object sender, EventArgs e)
        {
            txtComprehensiveDiagnosticSummary.Clear();
        }

        private void btnCancel_Click_1(object sender, EventArgs e)
        {
            cts.Cancel();
            prg.Visible = false;
            btnCancel.Enabled = false;
        }

        private void OnPingTimeChanged(object sender, NetworkMonitor.PingTimeChangedEventArgs e)
        {
            this.Invoke(() =>
            {
                // Update UI element with ping time
                //lblPingTime.Text = $"Ping: {e.PingTime} ms";
                spc.AddValue(e.PingTime.Value);
            });
        }


        private void DiagnosticTimer_Tick(object sender, EventArgs e)
        {
            string messagesToAppend = "";
            string? message;
            while (diagnosticMessages.TryDequeue(out message))
            {
                messagesToAppend += message;
            }
            if (!string.IsNullOrEmpty(messagesToAppend))
            {
                txtComprehensiveDiagnosticSummary.AppendText(messagesToAppend);
                txtComprehensiveDiagnosticSummary.ScrollToCaret();
            }
            if (receivedDiagnosticSteps >= expectedDiagnosticSteps)
            {
                if (diagnosticSummaryPending)
                {
                    txtComprehensiveDiagnosticSummary.AppendText(pendingSummary);
                    txtComprehensiveDiagnosticSummary.ScrollToCaret();
                    diagnosticSummaryPending = false;
                }
                diagnosticTimer.Stop();

                // Reset counts for next diagnostics
                receivedDiagnosticSteps = 0;
                expectedDiagnosticSteps = 0;
            }
        }

        private void DiagnosticStepChangedHandler(object? sender, NetworkMonitor.DiagnosticStepEventArgs e)
        {
            this.Invoke(() =>
            {
                // Append each diagnostic step to the summary textbox
                txtComprehensiveDiagnosticSummary.AppendText($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {(e.IsSuccessful ? "Success" : "Failed")} - {e.StepName}\r\nDetails: {e.Details}\r\n\r\n");
                txtComprehensiveDiagnosticSummary.ScrollToCaret();
            });
        }


        private void ComprehensiveDiagnosticsCompletedHandler(object? sender, NetworkMonitor.ComprehensiveDiagnosticsCompletedEventArgs e)
        {
            this.Invoke(() =>
            {
                txtComprehensiveDiagnosticSummary.AppendText("Comprehensive Diagnostic Summary:\r\n");
                txtComprehensiveDiagnosticSummary.AppendText(FormatDiagnostics(e.Diagnostics));
                txtComprehensiveDiagnosticSummary.AppendText("\r\n\r\n");
                txtComprehensiveDiagnosticSummary.ScrollToCaret();
            });
        }

        private void Monitor_SilentModeEntered(object? sender, EventArgs e)
        {
            this.Invoke(() =>
            {
                var timestamp = DateTime.Now;
                txtLog.AppendText($"[{timestamp:yyyy-MM-dd HH:mm:ss}] Entering silent mode.\r\n");
                txtLog.ScrollToCaret();
            });
        }


        private void EndpointDiagnosticsChangedHandler(object? sender, NetworkMonitor.EndpointDiagnosticsEventArgs e)
        {
            UpdateEndpointLog(e.IsConnected, e.EndpointName, e.Details);
        }

        private void UpdateEndpointLog(bool isConnected, string endpointName, string details)
        {
            this.Invoke(() =>
            {
                txtLog.AppendText($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] ");
                txtLog.AppendText($"{(isConnected ? "Connected" : "Disconnected")} via {endpointName}\r\n");
                txtLog.AppendText($"Details: {details}\r\n\r\n");
                txtLog.ScrollToCaret();
            });
        }

        private async void BtnDiagnose_Click(object? sender, EventArgs e)
        {
            txtComprehensiveDiagnosticSummary.Clear();
            txtComprehensiveDiagnosticSummary.AppendText("Diagnostics in progress...\r\n");
            receivedDiagnosticSteps = 0;
            expectedDiagnosticSteps = 0;
            diagnosticSummaryPending = false;
            pendingSummary = "";
            monitor.PauseMonitoring();

            try
            {
                await monitor.PerformComprehensiveDiagnostics(1, 0);
                txtComprehensiveDiagnosticSummary.AppendText("Diagnostics completed.\r\n");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred during diagnostics: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                logger.LogError(ex, "Error during manual diagnostics");
            }
            finally
            {
                monitor.ResumeMonitoring();
            }
        }

        private string FormatDiagnostics(ConnectionDiagnostics diagnostics)
        {
            var sb = new StringBuilder();

            if (diagnostics.ComponentStatus != null)
            {
                foreach (var component in diagnostics.ComponentStatus)
                {
                    sb.AppendLine($"{component.ComponentName}: {(component.IsReachable ? "Operational" : "Issue Detected")}");
                }
            }

            if (!string.IsNullOrEmpty(diagnostics.DetailedErrorDescription))
            {
                sb.AppendLine($"Detailed Error: {diagnostics.DetailedErrorDescription}");
            }

            return sb.ToString().TrimEnd();
        }

        private ILogger<NetworkMonitor> CreateLogger(ConnectionSettings settings)
        {
            var serilogLogger = new LoggerConfiguration()
                .WriteTo.File(settings.LogFilePath, rollingInterval: RollingInterval.Day, fileSizeLimitBytes: settings.MaxLogSizeBytes, rollOnFileSizeLimit: true, retainedFileCountLimit: 31)
                .CreateLogger();

            var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddSerilog(serilogLogger);
            });

            return loggerFactory.CreateLogger<NetworkMonitor>();
        }


        private void ConnectionStateChangedHandler(object? sender, NetworkMonitor.ConnectionStateChangedEventArgs e)
        {
            this.Invoke(() =>
            {
                // Determine if the connection status has changed
                bool connectionStatusChanged = e.IsConnected != previousConnectionStatus;
                previousConnectionStatus = e.IsConnected;

                if (connectionStatusChanged)
                {
                    // Connection status has changed (either lost or restored)
                    txtLog.AppendText($"[{e.Timestamp:yyyy-MM-dd HH:mm:ss}] ");

                    if (!e.IsConnected)
                    {
                        // Connection Lost
                        txtLog.AppendText("Connection Lost\r\n");
                        txtLog.AppendText($"Details:\r\n{e.Details}\r\n\r\n");
                    }
                    else
                    {
                        // Connection Restored
                        txtLog.AppendText("Connection Restored");
                        if (e.Downtime.HasValue)
                        {
                            txtLog.AppendText($" (Down for {FormatDowntime(e.Downtime.Value)})");
                        }
                        txtLog.AppendText("\r\n");
                        txtLog.AppendText($"Details:\r\n{e.Details}\r\n\r\n");
                    }
                }
                else if (e.IsConnected && !monitor.IsInSilentMode)
                {
                    // Connection is up and we're logging success messages
                    txtLog.AppendText($"[{e.Timestamp:yyyy-MM-dd HH:mm:ss}] {e.Details}\r\n");
                    txtLog.AppendText("Details: Success\r\n\r\n");
                }

                txtLog.ScrollToCaret();
            });
        }

        private void DisplayGreeting()
        {
            txtLog.AppendText("Welcome to Internet Connection Monitor!\r\n");
            txtLog.AppendText("Logging the first three successful connection checks...\r\n\r\n");
        }

        private void UpdateUI(bool isConnected, string details, DateTime timestamp, TimeSpan? downtime)
        {
            this.Invoke(() =>
            {
                txtLog.AppendText($"[{timestamp:yyyy-MM-dd HH:mm:ss}] ");
                if (!isConnected)
                {
                    txtLog.AppendText("Connection Lost\r\n");
                    notifyIcon?.ShowBalloonTip(3000, "Connection Lost", "The internet connection has been lost.", ToolTipIcon.Error);
                    logger.LogWarning("Connection lost at {Timestamp}. Details: {Details}", timestamp, details);
                }
                else
                {
                    txtLog.AppendText($"Connection Restored (Down for {FormatDowntime(downtime!.Value)})\r\n");
                    notifyIcon?.ShowBalloonTip(3000, "Connection Restored", $"Connection restored after {FormatDowntime(downtime!.Value)}.", ToolTipIcon.Info);
                    logger.LogInformation("Connection restored at {Timestamp} after downtime of {Downtime}. Details: {Details}", timestamp, downtime, details);
                }

                double value = isConnected ? 100 : 0;
                spc.AddValue(value);

                txtLog.AppendText($"Details:\r\n{details}\r\n\r\n");
                txtLog.ScrollToCaret();
            });
        }

        private string FormatDowntime(TimeSpan duration)
        {
            if (duration.TotalSeconds < 1)
                return $"{duration.TotalMilliseconds:F0} milliseconds";
            else if (duration.TotalSeconds < 60)
                return $"{duration.TotalSeconds:F1} seconds";
            else if (duration.TotalMinutes < 60)
                return $"{duration.TotalMinutes:F1} minutes";
            else
                return $"{duration.TotalHours:F1} hours";
        }

        private void InitializeUI()
        {
            Text = "LinkDoctor: Diagnose and Heal Your Network";
            txtLog.Multiline = true;
            txtComprehensiveDiagnosticSummary.Multiline = true;
            txtLog.ScrollBars = ScrollBars.Vertical;
            txtComprehensiveDiagnosticSummary.ScrollBars = ScrollBars.Vertical;
            txtLog.ReadOnly = true;
            txtComprehensiveDiagnosticSummary.ReadOnly = true;
        }

        private void InitializeNotifyIcon()
        {
            notifyIcon = new NotifyIcon
            {
                Icon = new Icon(Path.Combine(Application.StartupPath, "LinkDoctor.ico")),
                Visible = true,
                Text = "LinkDoctor: Diagnose and Heal Your Network"
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

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                Hide();
            }
            else
            {
                monitor.Dispose();
                notifyIcon?.Dispose();
            }

            base.OnFormClosing(e);
        }
    }
}

