using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace Tulpar;

public partial class MainWindow : Window
{
    private static readonly CultureInfo InputCulture = CultureInfo.InvariantCulture;
    private readonly DispatcherTimer _timer;
    private readonly List<(double Time, double Altitude)> _altitudeSamples = [];
    private FlightSimulation? _simulation;
    private bool _isPaused;
    private double _lastChartSampleTime = -1;

    public MainWindow()
    {
        InitializeComponent();
        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(20) };
        _timer.Tick += Timer_Tick;
        UpdateReadouts();
        DrawScene();
        DrawChart();
    }

    private void LaunchButton_Click(object sender, RoutedEventArgs e)
    {
        if (!TryReadConfiguration(out var configuration))
        {
            return;
        }

        _simulation = new FlightSimulation(configuration);
        _altitudeSamples.Clear();
        _altitudeSamples.Add((0, 0));
        _lastChartSampleTime = 0;
        _isPaused = false;
        SetInputsEnabled(false);
        PauseButton.IsEnabled = true;
        PauseButton.Content = "Pause";
        SetStatus("IN FLIGHT", "#49D6A0");
        _timer.Start();
        UpdateReadouts();
        DrawScene();
        DrawChart();
    }

    private void PauseButton_Click(object sender, RoutedEventArgs e)
    {
        if (_simulation is null || _simulation.IsLanded)
        {
            return;
        }

        _isPaused = !_isPaused;
        if (_isPaused)
        {
            _timer.Stop();
            PauseButton.Content = "Resume";
            SetStatus("PAUSED", "#F4B544");
        }
        else
        {
            _timer.Start();
            PauseButton.Content = "Pause";
            SetStatus("IN FLIGHT", "#49D6A0");
        }
    }

    private void ResetButton_Click(object sender, RoutedEventArgs e)
    {
        _timer.Stop();
        _simulation = null;
        _isPaused = false;
        _altitudeSamples.Clear();
        _altitudeSamples.Add((0, 0));
        _lastChartSampleTime = 0;
        SetInputsEnabled(true);
        PauseButton.IsEnabled = false;
        PauseButton.Content = "Pause";
        SetStatus("READY", "#929DB4");
        UpdateReadouts();
        DrawScene();
        DrawChart();
    }

    private void Timer_Tick(object? sender, EventArgs e)
    {
        if (_simulation is null || _isPaused)
        {
            return;
        }

        _simulation.Advance(0.02);
        if (_simulation.TimeSeconds - _lastChartSampleTime >= 0.1
            || _simulation.IsLanded)
        {
            _altitudeSamples.Add((_simulation.TimeSeconds, _simulation.AltitudeMeters));
            _lastChartSampleTime = _simulation.TimeSeconds;
        }

        UpdateReadouts();
        DrawScene();
        DrawChart();

        if (_simulation.IsLanded)
        {
            _timer.Stop();
            PauseButton.IsEnabled = false;
            PauseButton.Content = "Pause";
            SetStatus("LANDED", "#929DB4");
        }
    }

    private bool TryReadConfiguration(out RocketConfiguration configuration)
    {
        configuration = new RocketConfiguration(0, 0, 0, 0, 0);
        if (!TryReadPositive(DryMassInput, "Dry mass", out var dryMass)
            || !TryReadPositive(PropellantInput, "Propellant mass", out var propellantMass)
            || !TryReadPositive(ThrustInput, "Engine thrust", out var thrust)
            || !TryReadPositive(BurnDurationInput, "Burn duration", out var burnDuration))
        {
            return false;
        }

        if (!double.TryParse(
                DragCoefficientInput.Text,
                NumberStyles.Float,
                InputCulture,
                out var dragCoefficient)
            || !double.IsFinite(dragCoefficient)
            || dragCoefficient < 0)
        {
            MessageBox.Show(
                this,
                "Enter a valid drag coefficient of zero or greater.",
                "Check rocket setup",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            DragCoefficientInput.Focus();
            DragCoefficientInput.SelectAll();
            return false;
        }

        configuration = new RocketConfiguration(
            dryMass,
            propellantMass,
            thrust,
            burnDuration,
            dragCoefficient);
        return true;
    }

    private bool TryReadPositive(TextBox input, string label, out double value)
    {
        if (double.TryParse(input.Text, NumberStyles.Float, InputCulture, out value)
            && double.IsFinite(value)
            && value > 0)
        {
            return true;
        }

        MessageBox.Show(
            this,
            $"{label} must be a number greater than zero.",
            "Check rocket setup",
            MessageBoxButton.OK,
            MessageBoxImage.Warning);
        input.Focus();
        input.SelectAll();
        return false;
    }

    private void SetInputsEnabled(bool enabled)
    {
        DryMassInput.IsEnabled = enabled;
        PropellantInput.IsEnabled = enabled;
        ThrustInput.IsEnabled = enabled;
        BurnDurationInput.IsEnabled = enabled;
        DragCoefficientInput.IsEnabled = enabled;
        LaunchButton.IsEnabled = enabled;
    }

    private void SetStatus(string text, string color)
    {
        StatusText.Text = text;
        StatusDot.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color));
    }

    private void UpdateReadouts()
    {
        var state = _simulation;
        AltitudeText.Text = $"{(state?.AltitudeMeters ?? 0):N0} m";
        VelocityText.Text = $"{(state?.VelocityMetersPerSecond ?? 0):N1} m/s";
        AccelerationText.Text = $"{(state?.AccelerationMetersPerSecondSquared ?? 0):N1} m/s²";
        TimeText.Text = $"{(state?.TimeSeconds ?? 0):N1} s";
    }

    private void SceneCanvas_SizeChanged(object sender, SizeChangedEventArgs e) => DrawScene();
    private void ChartCanvas_SizeChanged(object sender, SizeChangedEventArgs e) => DrawChart();

    private void DrawScene()
    {
        if (SceneCanvas is null)
        {
            return;
        }

        var width = SceneCanvas.ActualWidth;
        var height = SceneCanvas.ActualHeight;
        if (width < 80 || height < 80)
        {
            return;
        }

        SceneCanvas.Children.Clear();
        var groundY = height - 45;
        var rocketAltitude = _simulation?.AltitudeMeters ?? 0;
        var viewHeightMeters = Math.Max(250, rocketAltitude * 1.3);
        var skyHeight = groundY - 30;
        var rocketY = groundY - rocketAltitude / viewHeightMeters * skyHeight;

        for (var i = 0; i <= 4; i++)
        {
            var y = 30 + skyHeight * i / 4;
            AddLine(SceneCanvas, 0, y, width, y, "#25334A", 1);
            var markAltitude = viewHeightMeters * (1 - i / 4.0);
            AddText(SceneCanvas, $"{markAltitude:N0} m", 10, y - 17, "#8290A8", 10);
        }

        AddLine(SceneCanvas, width / 2, 24, width / 2, groundY, "#384862", 1, true);

        var ground = new Rectangle
        {
            Width = width,
            Height = 45,
            Fill = Brush("#202B3D")
        };
        Canvas.SetLeft(ground, 0);
        Canvas.SetTop(ground, groundY);
        SceneCanvas.Children.Add(ground);
        AddLine(SceneCanvas, 0, groundY, width, groundY, "#6D7E94", 1.5);

        AddLine(SceneCanvas, width / 2, groundY, width / 2, rocketY + 20, "#F4B544", 2);
        DrawRocket(width / 2, rocketY, _simulation?.IsEngineRunning == true);

        AddText(SceneCanvas, "AUTO-SCALED VIEW", Math.Max(16, width - 138), 12, "#8290A8", 9);
    }

    private void DrawRocket(double centerX, double centerY, bool engineRunning)
    {
        var rocket = new Polygon
        {
            Points = new PointCollection
            {
                new(centerX, centerY - 23),
                new(centerX + 10, centerY - 8),
                new(centerX + 9, centerY + 14),
                new(centerX - 9, centerY + 14),
                new(centerX - 10, centerY - 8)
            },
            Fill = Brush("#E8EDF5"),
            Stroke = Brush("#AAB7CA"),
            StrokeThickness = 1
        };
        SceneCanvas.Children.Add(rocket);

        var nose = new Polygon
        {
            Points = new PointCollection
            {
                new(centerX, centerY - 23),
                new(centerX + 10, centerY - 8),
                new(centerX - 10, centerY - 8)
            },
            Fill = Brush("#F4B544")
        };
        SceneCanvas.Children.Add(nose);

        AddEllipse(SceneCanvas, centerX - 3, centerY - 3, 6, 6, "#334761");
        AddPolygon(
            [new Point(centerX - 9, centerY + 7), new Point(centerX - 17, centerY + 16),
                new Point(centerX - 9, centerY + 13)],
            "#CF8D2E");
        AddPolygon(
            [new Point(centerX + 9, centerY + 7), new Point(centerX + 17, centerY + 16),
                new Point(centerX + 9, centerY + 13)],
            "#CF8D2E");

        if (engineRunning)
        {
            AddPolygon(
                [new Point(centerX - 5, centerY + 14), new Point(centerX, centerY + 28),
                    new Point(centerX + 5, centerY + 14)],
                "#F4B544");
            AddPolygon(
                [new Point(centerX - 2, centerY + 14), new Point(centerX, centerY + 23),
                    new Point(centerX + 2, centerY + 14)],
                "#FFF0C2");
        }
    }

    private void DrawChart()
    {
        if (ChartCanvas is null)
        {
            return;
        }

        var width = ChartCanvas.ActualWidth;
        var height = ChartCanvas.ActualHeight;
        if (width < 60 || height < 30)
        {
            return;
        }

        ChartCanvas.Children.Clear();
        const double left = 34;
        const double right = 8;
        const double top = 5;
        const double bottom = 21;
        var plotWidth = width - left - right;
        var plotHeight = height - top - bottom;
        var points = _altitudeSamples.Count == 0
            ? new List<(double Time, double Altitude)> { (0, 0) }
            : _altitudeSamples;
        var maxTime = Math.Max(1, points[^1].Time);
        var maxAltitude = Math.Max(100, points.Max(point => point.Altitude) * 1.15);

        for (var i = 0; i <= 2; i++)
        {
            var y = top + plotHeight * i / 2;
            AddLine(ChartCanvas, left, y, width - right, y, "#29354A", 1);
            AddText(ChartCanvas, $"{maxAltitude * (1 - i / 2.0):N0}", 0, y - 7, "#8290A8", 9);
        }

        AddText(ChartCanvas, "0", left - 3, height - bottom + 4, "#8290A8", 9);
        AddText(ChartCanvas, $"{maxTime:N0}s", width - right - 30, height - bottom + 4, "#8290A8", 9);

        if (points.Count < 2)
        {
            return;
        }

        var profile = new Polyline
        {
            Stroke = Brush("#F4B544"),
            StrokeThickness = 2,
            Points = new PointCollection(points.Select(point => new Point(
                left + point.Time / maxTime * plotWidth,
                top + (1 - point.Altitude / maxAltitude) * plotHeight)))
        };
        ChartCanvas.Children.Add(profile);
    }

    private void AddLine(Canvas canvas, double x1, double y1, double x2, double y2,
        string color, double thickness, bool dashed = false)
    {
        var line = new Line
        {
            X1 = x1,
            Y1 = y1,
            X2 = x2,
            Y2 = y2,
            Stroke = Brush(color),
            StrokeThickness = thickness
        };
        if (dashed)
        {
            line.StrokeDashArray = new DoubleCollection { 4, 5 };
        }
        canvas.Children.Add(line);
    }

    private void AddText(Canvas canvas, string text, double x, double y, string color, double size)
    {
        var label = new TextBlock
        {
            Text = text,
            Foreground = Brush(color),
            FontSize = size
        };
        Canvas.SetLeft(label, x);
        Canvas.SetTop(label, y);
        canvas.Children.Add(label);
    }

    private void AddEllipse(Canvas canvas, double x, double y, double width, double height, string color)
    {
        var ellipse = new Ellipse { Width = width, Height = height, Fill = Brush(color) };
        Canvas.SetLeft(ellipse, x);
        Canvas.SetTop(ellipse, y);
        canvas.Children.Add(ellipse);
    }

    private void AddPolygon(Point[] points, string color)
    {
        SceneCanvas.Children.Add(new Polygon
        {
            Points = new PointCollection(points),
            Fill = Brush(color)
        });
    }

    private static SolidColorBrush Brush(string color) =>
        new((Color)ColorConverter.ConvertFromString(color));
}
