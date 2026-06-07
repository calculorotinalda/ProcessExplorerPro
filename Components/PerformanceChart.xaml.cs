using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace ProcessExplorerPro.Components
{
    public partial class PerformanceChart : UserControl
    {
        public static readonly DependencyProperty StrokeColorProperty =
            DependencyProperty.Register("StrokeColor", typeof(Color), typeof(PerformanceChart), 
                new PropertyMetadata(Color.FromRgb(92, 98, 214), OnAppearanceChanged));

        public static readonly DependencyProperty FillColorProperty =
            DependencyProperty.Register("FillColor", typeof(Color), typeof(PerformanceChart), 
                new PropertyMetadata(Color.FromRgb(92, 98, 214), OnAppearanceChanged));

        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register("Title", typeof(string), typeof(PerformanceChart), 
                new PropertyMetadata("CPU", OnTitleChanged));

        public static readonly DependencyProperty MaxValueProperty =
            DependencyProperty.Register("MaxValue", typeof(double), typeof(PerformanceChart), 
                new PropertyMetadata(100.0, OnMaxOrUnitChanged));

        public static readonly DependencyProperty UnitProperty =
            DependencyProperty.Register("Unit", typeof(string), typeof(PerformanceChart), 
                new PropertyMetadata("%", OnMaxOrUnitChanged));

        private readonly List<double> _values = new();
        private const int MaxPoints = 60; // 60 data points (e.g. 60 seconds)

        public Color StrokeColor
        {
            get => (Color)GetValue(StrokeColorProperty);
            set => SetValue(StrokeColorProperty, value);
        }

        public Color FillColor
        {
            get => (Color)GetValue(FillColorProperty);
            set => SetValue(FillColorProperty, value);
        }

        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        public double MaxValue
        {
            get => (double)GetValue(MaxValueProperty);
            set => SetValue(MaxValueProperty, value);
        }

        public string Unit
        {
            get => (string)GetValue(UnitProperty);
            set => SetValue(UnitProperty, value);
        }

        public PerformanceChart()
        {
            InitializeComponent();
            SizeChanged += (s, e) => Redraw();

            // Populate with initial zero points
            for (int i = 0; i < MaxPoints; i++)
            {
                _values.Add(0);
            }
            
            Loaded += (s, e) => {
                LineStrokeBrush.Color = StrokeColor;
                GradientStopStart.Color = FillColor;
                TitleLabel.Text = Title;
                Redraw();
            };
        }

        private static void OnAppearanceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is PerformanceChart chart && chart.IsLoaded)
            {
                chart.LineStrokeBrush.Color = chart.StrokeColor;
                chart.GradientStopStart.Color = chart.FillColor;
            }
        }

        private static void OnTitleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is PerformanceChart chart)
            {
                chart.TitleLabel.Text = chart.Title;
            }
        }

        private static void OnMaxOrUnitChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is PerformanceChart chart)
            {
                chart.Redraw();
            }
        }

        public void AddValue(double val)
        {
            if (_values.Count >= MaxPoints)
            {
                _values.RemoveAt(0);
            }
            _values.Add(val);
            ValueLabel.Text = $"{val:0.0}{Unit}";
            Redraw();
        }

        private void Redraw()
        {
            double width = ActualWidth;
            double height = ActualHeight;

            if (width <= 0 || height <= 0) return;

            // 1. Draw Grid Lines
            GridCanvas.Children.Clear();
            int hLines = 4;
            for (int i = 1; i < hLines; i++)
            {
                double y = (height / hLines) * i;
                var line = new Line
                {
                    X1 = 0,
                    Y1 = y,
                    X2 = width,
                    Y2 = y,
                    Stroke = Brushes.White,
                    StrokeThickness = 0.5
                };
                GridCanvas.Children.Add(line);
            }

            int vLines = 10;
            for (int i = 1; i < vLines; i++)
            {
                double x = (width / vLines) * i;
                var line = new Line
                {
                    X1 = x,
                    Y1 = 0,
                    X2 = x,
                    Y2 = height,
                    Stroke = Brushes.White,
                    StrokeThickness = 0.5
                };
                GridCanvas.Children.Add(line);
            }

            // 2. Plot Points
            if (_values.Count == 0) return;

            var lineGeometry = new PathGeometry();
            var lineFigure = new PathFigure();

            var areaGeometry = new PathGeometry();
            var areaFigure = new PathFigure();

            double step = width / (MaxPoints - 1);
            double max = MaxValue <= 0 ? 100.0 : MaxValue;

            double firstY = height - (_values[0] / max) * height;
            firstY = Math.Clamp(firstY, 0.0, height);

            lineFigure.StartPoint = new Point(0, firstY);
            
            // Start area geometry at bottom-left corner
            areaFigure.StartPoint = new Point(0, height);
            areaFigure.Segments.Add(new LineSegment(new Point(0, firstY), false));

            for (int i = 1; i < _values.Count; i++)
            {
                double x = i * step;
                double y = height - (_values[i] / max) * height;
                y = Math.Clamp(y, 0.0, height);

                var pt = new Point(x, y);
                lineFigure.Segments.Add(new LineSegment(pt, true));
                areaFigure.Segments.Add(new LineSegment(pt, true));
            }

            // Close the area path to bottom-right and back to start
            areaFigure.Segments.Add(new LineSegment(new Point(width, height), false));
            areaFigure.IsClosed = true;

            lineGeometry.Figures.Add(lineFigure);
            areaGeometry.Figures.Add(areaFigure);

            LinePath.Data = lineGeometry;
            AreaPath.Data = areaGeometry;
        }
    }
}
