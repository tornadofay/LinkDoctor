using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace SpPerfChart
{
    public partial class PerfChart : UserControl
    {
        #region Constants
        private const int MAX_VALUE_COUNT = 512;
        private const int GRID_SPACING = 20;
        private const int LEFT_MARGIN = 50; // Margin to the left for Y-axis labels
        private const int VERTICAL_MARGIN = 10; // Pixels of margin at top and bottom
        private const int TIME_INTERVAL_SECONDS = 5; // Time interval between vertical grid lines
        #endregion

        #region Member Variables
        private int valueSpacing = 5;
        private int gridScrollOffset = 0;
        private Queue<double> drawValues = new Queue<double>(MAX_VALUE_COUNT);
        private PerfChartStyle perfChartStyle;
        private bool needsRecalculation = true;
        private double minValue = 0;
        private double maxValue = 100;
        private Pen verticalGridPenCache;
        private Pen horizontalGridPenCache;
        private Pen chartLinePenCache;
        private SolidBrush textBrush;
        private SolidBrush backGroundSolidBrushCache;
        private Pen verticalGridBoldPenCache;

        private double sum = 0;
        private int count = 0;
        private double currentMin = double.MaxValue;
        private double currentMax = double.MinValue;
        #endregion

        #region Constructors
        public PerfChart()
        {
            InitializeComponent();
            perfChartStyle = new PerfChartStyle();
            this.SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            this.DoubleBuffered = true;

            UpdateCachedResources();
            perfChartStyle.PropertyChanged += PerfChartStyle_PropertyChanged;
            this.Font = SystemInformation.MenuFont;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                components?.Dispose();
                verticalGridPenCache?.Dispose();
                horizontalGridPenCache?.Dispose();
                chartLinePenCache?.Dispose();
                textBrush?.Dispose();
                perfChartStyle.PropertyChanged -= PerfChartStyle_PropertyChanged;

                verticalGridPenCache?.Dispose();
                horizontalGridPenCache?.Dispose();
                chartLinePenCache?.Dispose();
                textBrush?.Dispose();
                backGroundSolidBrushCache?.Dispose();
            }
            base.Dispose(disposing);
        }

        private void PerfChartStyle_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            UpdateCachedResources();
        }

        private void UpdateCachedResources()
        {
            verticalGridPenCache = perfChartStyle.VerticalGridPen.Pen;
            horizontalGridPenCache = perfChartStyle.HorizontalGridPen.Pen;
            chartLinePenCache = perfChartStyle.ChartLinePen.Pen;
            textBrush = new SolidBrush(perfChartStyle.TextColor);
            backGroundSolidBrushCache = new SolidBrush(perfChartStyle.BackgroundColor);
            verticalGridBoldPenCache = new Pen(verticalGridPenCache.Color, 3f);
        }
        #endregion

        #region Properties
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Content),
        Category("Appearance"), Description("Appearance and Style")]
        public PerfChartStyle PerfChartStyle
        {
            get { return perfChartStyle; }
            set { perfChartStyle = value; }
        }
        #endregion

        #region Public Methods
        public void Clear()
        {
            drawValues.Clear();
            sum = 0;
            count = 0;
            currentMin = double.MaxValue;
            currentMax = double.MinValue;
            needsRecalculation = true;
            Invalidate();
        }

        public void AddValue(double value)
        {
            needsRecalculation = true;
            // Use -1 to indicate timeout/failure
            ChartAppend(value < 0 ? double.NaN : value);
            Invalidate();
        }
        #endregion

        #region Private Methods
        private void ChartAppend(double value)
        {
            drawValues.Enqueue(Math.Max(value, 0));
            if (drawValues.Count > MAX_VALUE_COUNT)
                DequeueValue();

            sum += value;
            count++;
            currentMin = Math.Min(currentMin, value);
            currentMax = Math.Max(currentMax, value);

            gridScrollOffset += valueSpacing;
            if (gridScrollOffset >= GRID_SPACING)
                gridScrollOffset %= GRID_SPACING;
        }

        private void DequeueValue()
        {
            double oldValue = drawValues.Dequeue();
            sum -= oldValue;
            count--;

            if (drawValues.Count > 0)
            {
                currentMin = drawValues.Min();
                currentMax = drawValues.Max();
            }
            else
            {
                currentMin = double.MaxValue;
                currentMax = double.MinValue;
            }
        }

        private int CalcVerticalPosition(double value, double minValue, double maxValue)
        {
            if (double.IsNaN(value))
                return 0;

            if (maxValue <= minValue)
                return Height / 2;

            // Calculate position using the full range including padding
            int effectiveHeight = Height - (2 * VERTICAL_MARGIN);
            double normalizedValue = (value - minValue) / (maxValue - minValue);
            int y = Height - VERTICAL_MARGIN - (int)(normalizedValue * effectiveHeight);

            // Clamp the value to prevent drawing outside bounds
            return Math.Clamp(y, VERTICAL_MARGIN, Height - VERTICAL_MARGIN);
        }

        #endregion

        #region Drawing Methods

        private void DrawChart(Graphics g, double[] visibleValuesArray, double minValue, double maxValue)
        {
            if (visibleValuesArray.Length == 0)
                return;

            List<Point> points = new List<Point>();
            int x = Width - 1;

            for (int i = visibleValuesArray.Length - 1; i >= 0; i--)
            {
                double val = visibleValuesArray[i];

                if (double.IsNaN(val))
                {
                    // If we have points, draw the path up to here
                    if (points.Count > 1)
                    {
                        using (GraphicsPath path = new GraphicsPath())
                        {
                            path.AddLines(points.ToArray());
                            g.DrawPath(chartLinePenCache, path);
                        }
                        points.Clear();
                    }
                }
                else
                {
                    int y = CalcVerticalPosition(val, minValue, maxValue);
                    points.Add(new Point(x, y));
                }

                x -= valueSpacing;
                if (x < LEFT_MARGIN)
                    break;
            }

            // Draw any remaining points
            if (points.Count > 1)
            {
                using (GraphicsPath path = new GraphicsPath())
                {
                    path.AddLines(points.ToArray());
                    g.DrawPath(chartLinePenCache, path);
                }
            }
        }

        private void DrawBackgroundAndGrid(Graphics g, double minValue, double maxValue)
        {
            Rectangle baseRectangle = new Rectangle(LEFT_MARGIN, 0, Width - LEFT_MARGIN, Height);
            g.FillRectangle(backGroundSolidBrushCache, baseRectangle);

            if (perfChartStyle.ShowVerticalGridLines)
            {
                int timeIndex = 0;
                for (int i = Width - gridScrollOffset; i >= LEFT_MARGIN; i -= GRID_SPACING)
                {
                    int seconds = (timeIndex * TIME_INTERVAL_SECONDS) % 60;

                    // Use a bolder pen for 0-second lines
                    if (seconds == 0)
                    {
                        g.DrawLine(verticalGridBoldPenCache, i, 0, i, Height);
                    }
                    else
                    {
                        g.DrawLine(verticalGridPenCache, i, 0, i, Height);
                    }

                    // Draw time labels
                    string timeLabel = $"{seconds}";
                    SizeF size = g.MeasureString(timeLabel, Font);
                    g.DrawString(timeLabel, Font, textBrush, i - (size.Width / 2), Height - size.Height);
                    timeIndex++;
                }
            }

            if (perfChartStyle.ShowHorizontalGridLines)
            {
                // Calculate the number of grid lines based on available height
                int effectiveHeight = Height - (2 * VERTICAL_MARGIN);
                int numberOfGridLines = effectiveHeight / GRID_SPACING;

                // Calculate value increment per grid line
                double valueRange = maxValue - minValue;
                double valueIncrement = valueRange / numberOfGridLines;

                // Round the increment to a nice number
                valueIncrement = RoundToNiceNumber(valueIncrement);

                // Adjust min and max values to align with grid lines
                double adjustedMin = Math.Floor(minValue / valueIncrement) * valueIncrement;
                double adjustedMax = Math.Ceiling(maxValue / valueIncrement) * valueIncrement;

                // Draw grid lines and labels
                for (double value = adjustedMin; value <= adjustedMax; value += valueIncrement)
                {
                    int y = CalcVerticalPosition(value, minValue, maxValue);

                    if (y >= VERTICAL_MARGIN && y <= Height - VERTICAL_MARGIN)
                    {
                        g.DrawLine(horizontalGridPenCache, LEFT_MARGIN, y, Width, y);

                        // Format the label based on the value
                        string label = FormatAxisLabel(value);
                        SizeF size = g.MeasureString(label, Font);
                        g.DrawString(label, Font, textBrush, LEFT_MARGIN - size.Width - 5, y - size.Height / 2);
                    }
                }
            }
        }

        private string FormatAxisLabel(double value)
        {
            if (value >= 1000)
            {
                return $"{value:F0}";  // No decimals for large numbers
            }
            else if (value >= 100)
            {
                return $"{value:F1}";  // One decimal for medium numbers
            }
            else
            {
                return $"{value:F2}";  // Two decimals for small numbers
            }
        }

        private double RoundToNiceNumber(double value)
        {
            // Find the magnitude of the value
            double magnitude = Math.Pow(10, Math.Floor(Math.Log10(value)));
            double normalized = value / magnitude;

            // Round to a nice number
            double niceNormalized;
            if (normalized < 1.5) niceNormalized = 1;
            else if (normalized < 3) niceNormalized = 2;
            else if (normalized < 7) niceNormalized = 5;
            else niceNormalized = 10;

            return niceNormalized * magnitude;
        }

        #endregion

        #region Overrides
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            if (needsRecalculation)
            {
                CalculateMinAndMaxValues();
                needsRecalculation = false;
            }

            int visibleValues = (Width - LEFT_MARGIN) / valueSpacing;
            visibleValues = Math.Min(visibleValues, drawValues.Count);

            double[] valuesArray = drawValues.ToArray();
            double[] visibleValuesArray = new double[visibleValues];

            Array.Copy(valuesArray, drawValues.Count - visibleValues, visibleValuesArray, 0, visibleValues);

            if (perfChartStyle.AntiAliasing)
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            DrawBackgroundAndGrid(e.Graphics, minValue, maxValue);
            DrawChart(e.Graphics, visibleValuesArray, minValue, maxValue);
        }

        private void CalculateMinAndMaxValues()
        {
            if (count == 0)
            {
                minValue = 0;
                maxValue = 100;
                return;
            }

            // Get the current visible values
            int visibleValues = (Width - LEFT_MARGIN) / valueSpacing;
            visibleValues = Math.Min(visibleValues, drawValues.Count);

            var values = drawValues.TakeLast(visibleValues).Where(v => !double.IsNaN(v)).ToList();

            if (!values.Any())
            {
                minValue = 0;
                maxValue = 100;
                return;
            }

            // Calculate the mean and range of visible values
            double min = values.Min();
            double max = values.Max();
            double range = max - min;

            // Add padding (10% on each side)
            double padding = range * 0.1;

            // Ensure minimum range and padding
            if (range < 10)
            {
                padding = 5;  // Minimum 5 units padding when range is small
            }

            minValue = Math.Max(0, min - padding);
            maxValue = max + padding;

            // Ensure minimum range
            if (maxValue - minValue < 20)
            {
                maxValue = minValue + 20;
            }
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            needsRecalculation = true;
            Invalidate();
        }
        #endregion
    }

    #region PerfChartStyle Class
    public class PerfChartStyle : INotifyPropertyChanged
    {
        private ChartPen verticalGridPen;
        private ChartPen horizontalGridPen;
        private ChartPen chartLinePen;
        private Color backgroundColor = Color.LightGreen;
        private Color textColor = Color.Black;
        private bool showVerticalGridLines = true;
        private bool showHorizontalGridLines = true;
        private bool antiAliasing = true;

        public event PropertyChangedEventHandler? PropertyChanged;

        public PerfChartStyle()
        {
            verticalGridPen = new ChartPen(Color.Gray);
            horizontalGridPen = new ChartPen(Color.Gray);
            chartLinePen = new ChartPen(Color.Red, 2f);
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public bool ShowVerticalGridLines
        {
            get { return showVerticalGridLines; }
            set
            {
                if (showVerticalGridLines != value)
                {
                    showVerticalGridLines = value;
                    OnPropertyChanged(nameof(ShowVerticalGridLines));
                }
            }
        }

        public bool ShowHorizontalGridLines
        {
            get { return showHorizontalGridLines; }
            set
            {
                if (showHorizontalGridLines != value)
                {
                    showHorizontalGridLines = value;
                    OnPropertyChanged(nameof(ShowHorizontalGridLines));
                }
            }
        }

        public ChartPen VerticalGridPen
        {
            get { return verticalGridPen; }
            set
            {
                if (verticalGridPen != value)
                {
                    verticalGridPen = value;
                    OnPropertyChanged(nameof(VerticalGridPen));
                }
            }
        }

        public ChartPen HorizontalGridPen
        {
            get { return horizontalGridPen; }
            set
            {
                if (horizontalGridPen != value)
                {
                    horizontalGridPen = value;
                    OnPropertyChanged(nameof(HorizontalGridPen));
                }
            }
        }

        public ChartPen ChartLinePen
        {
            get { return chartLinePen; }
            set
            {
                if (chartLinePen != value)
                {
                    chartLinePen = value;
                    OnPropertyChanged(nameof(ChartLinePen));
                }
            }
        }

        public bool AntiAliasing
        {
            get { return antiAliasing; }
            set
            {
                if (antiAliasing != value)
                {
                    antiAliasing = value;
                    OnPropertyChanged(nameof(AntiAliasing));
                }
            }
        }

        public Color BackgroundColor
        {
            get { return backgroundColor; }
            set
            {
                if (backgroundColor != value)
                {
                    backgroundColor = value;
                    OnPropertyChanged(nameof(BackgroundColor));
                }
            }
        }

        public Color TextColor
        {
            get { return textColor; }
            set
            {
                if (textColor != value)
                {
                    textColor = value;
                    OnPropertyChanged(nameof(TextColor));
                }
            }
        }
    }

    public class ChartPen
    {
        private Pen pen;

        public ChartPen(Color color, float width = 1f, DashStyle dashStyle = DashStyle.Solid)
        {
            pen = new Pen(color, width) { DashStyle = dashStyle };
        }

        public Color Color
        {
            get { return pen.Color; }
            set { pen.Color = value; }
        }

        public DashStyle DashStyle
        {
            get { return pen.DashStyle; }
            set { pen.DashStyle = value; }
        }

        public float Width
        {
            get { return pen.Width; }
            set { pen.Width = value; }
        }

        [Browsable(false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public Pen Pen
        {
            get { return pen; }
        }
    }
    #endregion
}


