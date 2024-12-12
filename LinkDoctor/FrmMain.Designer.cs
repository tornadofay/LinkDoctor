namespace LinkDoctor
{
    partial class FrmMain
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;



        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            txtLog = new TextBox();
            nti = new NotifyIcon(components);
            btnDiagnose = new Button();
            txtComprehensiveDiagnosticSummary = new TextBox();
            txtRoute = new TextBox();
            btnTraceRoute = new Button();
            btnPing = new Button();
            btnCancel = new Button();
            prg = new ProgressBar();
            btnClear = new Button();
            cboInterval = new ComboBox();
            btnZoomIn = new Button();
            btnZoomOut = new Button();
            spc = new SpPerfChart.PerfChart();
            SuspendLayout();
            // 
            // txtLog
            // 
            txtLog.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            txtLog.Location = new Point(0, 41);
            txtLog.Multiline = true;
            txtLog.Name = "txtLog";
            txtLog.ScrollBars = ScrollBars.Vertical;
            txtLog.Size = new Size(311, 628);
            txtLog.TabIndex = 0;
            // 
            // nti
            // 
            nti.Text = "nti";
            nti.Visible = true;
            // 
            // btnDiagnose
            // 
            btnDiagnose.Location = new Point(317, 12);
            btnDiagnose.Name = "btnDiagnose";
            btnDiagnose.Size = new Size(394, 23);
            btnDiagnose.TabIndex = 1;
            btnDiagnose.Text = "Preform Comprehensive Diagnostic";
            btnDiagnose.UseVisualStyleBackColor = true;
            // 
            // txtComprehensiveDiagnosticSummary
            // 
            txtComprehensiveDiagnosticSummary.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            txtComprehensiveDiagnosticSummary.Location = new Point(311, 216);
            txtComprehensiveDiagnosticSummary.Multiline = true;
            txtComprehensiveDiagnosticSummary.Name = "txtComprehensiveDiagnosticSummary";
            txtComprehensiveDiagnosticSummary.ScrollBars = ScrollBars.Vertical;
            txtComprehensiveDiagnosticSummary.Size = new Size(655, 453);
            txtComprehensiveDiagnosticSummary.TabIndex = 0;
            // 
            // txtRoute
            // 
            txtRoute.BorderStyle = BorderStyle.FixedSingle;
            txtRoute.Location = new Point(317, 41);
            txtRoute.Name = "txtRoute";
            txtRoute.Size = new Size(154, 23);
            txtRoute.TabIndex = 2;
            txtRoute.Text = "google.com";
            txtRoute.TextAlign = HorizontalAlignment.Center;
            // 
            // btnTraceRoute
            // 
            btnTraceRoute.Location = new Point(597, 41);
            btnTraceRoute.Name = "btnTraceRoute";
            btnTraceRoute.Size = new Size(114, 23);
            btnTraceRoute.TabIndex = 3;
            btnTraceRoute.Text = "Trace Route";
            btnTraceRoute.UseVisualStyleBackColor = true;
            // 
            // btnPing
            // 
            btnPing.Location = new Point(477, 41);
            btnPing.Name = "btnPing";
            btnPing.Size = new Size(114, 23);
            btnPing.TabIndex = 3;
            btnPing.Text = "Ping";
            btnPing.UseVisualStyleBackColor = true;
            // 
            // btnCancel
            // 
            btnCancel.Location = new Point(717, 12);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new Size(114, 23);
            btnCancel.TabIndex = 3;
            btnCancel.Text = "Cancel";
            btnCancel.UseVisualStyleBackColor = true;
            // 
            // prg
            // 
            prg.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            prg.Location = new Point(837, 41);
            prg.Name = "prg";
            prg.Size = new Size(129, 23);
            prg.TabIndex = 4;
            // 
            // btnClear
            // 
            btnClear.Location = new Point(717, 41);
            btnClear.Name = "btnClear";
            btnClear.Size = new Size(114, 23);
            btnClear.TabIndex = 3;
            btnClear.Text = "Clear";
            btnClear.UseVisualStyleBackColor = true;
            // 
            // cboInterval
            // 
            cboInterval.FormattingEnabled = true;
            cboInterval.Items.AddRange(new object[] { "5 sec", "4 sec", "3 sec", "2 sec", "1 sec" });
            cboInterval.Location = new Point(0, 12);
            cboInterval.Name = "cboInterval";
            cboInterval.Size = new Size(55, 23);
            cboInterval.TabIndex = 5;
            // 
            // btnZoomIn
            // 
            btnZoomIn.Location = new Point(155, 11);
            btnZoomIn.Name = "btnZoomIn";
            btnZoomIn.Size = new Size(75, 21);
            btnZoomIn.TabIndex = 7;
            btnZoomIn.Text = "Zoom In";
            btnZoomIn.UseVisualStyleBackColor = true;
            // 
            // btnZoomOut
            // 
            btnZoomOut.Location = new Point(236, 12);
            btnZoomOut.Name = "btnZoomOut";
            btnZoomOut.Size = new Size(75, 21);
            btnZoomOut.TabIndex = 7;
            btnZoomOut.Text = "Zoom Out";
            btnZoomOut.UseVisualStyleBackColor = true;
            // 
            // spc
            // 
            spc.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            spc.Font = new Font("Segoe UI", 12F, FontStyle.Regular, GraphicsUnit.World);
            spc.Location = new Point(317, 70);
            spc.Margin = new Padding(4, 3, 4, 3);
            spc.Name = "spc";
            spc.PerfChartStyle.AntiAliasing = true;
            spc.PerfChartStyle.BackgroundColor = Color.LightGreen;
            spc.PerfChartStyle.ShowHorizontalGridLines = true;
            spc.PerfChartStyle.ShowVerticalGridLines = true;
            spc.PerfChartStyle.TextColor = Color.Black;
            spc.Size = new Size(644, 140);
            spc.TabIndex = 8;
            // 
            // FrmMain
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(966, 669);
            Controls.Add(spc);
            Controls.Add(btnZoomOut);
            Controls.Add(btnZoomIn);
            Controls.Add(cboInterval);
            Controls.Add(prg);
            Controls.Add(btnPing);
            Controls.Add(btnClear);
            Controls.Add(btnCancel);
            Controls.Add(btnTraceRoute);
            Controls.Add(txtRoute);
            Controls.Add(txtComprehensiveDiagnosticSummary);
            Controls.Add(btnDiagnose);
            Controls.Add(txtLog);
            Name = "FrmMain";
            Text = "Main";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private TextBox txtLog;
        private NotifyIcon nti;
        private Button btnDiagnose;
        private TextBox txtComprehensiveDiagnosticSummary;
        private TextBox txtRoute;
        private Button btnTraceRoute;
        private Button btnPing;
        private Button btnCancel;
        private ProgressBar prg;
        private Button btnClear;
        private ComboBox cboInterval;
        private Button btnZoomIn;
        private Button btnZoomOut;
        private SpPerfChart.PerfChart spc;
    }
}
