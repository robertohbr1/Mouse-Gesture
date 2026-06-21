using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace MouseKeyb;

/// <summary>
/// A transparent overlay window spanning all monitors to render mouse gesture trails.
/// </summary>
public partial class OverlayWindow : Window
{
    private const int GWL_EXSTYLE = -20;
    private const int WS_EX_TRANSPARENT = 0x00000020;
    private const int WS_EX_TOOLWINDOW = 0x00000080;
    private const int WS_EX_NOACTIVATE = 0x08000000;

    [DllImport("user32.dll")]
    private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll")]
    private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

    private DispatcherTimer? _holdTimer;
    private DispatcherTimer? _pauseTimer;
    private System.Windows.Point _lastMousePos;

    private static readonly System.Windows.Media.Brush NormalTextBrush = CreateFrozenBrush("#FF00D2FF");
    private static readonly System.Windows.Media.Brush NormalBorderBrush = CreateFrozenBrush("#3000D2FF");
    private static readonly System.Windows.Media.Brush GreenTextBrush = CreateFrozenBrush("#FF00E676");
    private static readonly System.Windows.Media.Brush GreenBorderBrush = CreateFrozenBrush("#8000E676");

    private static System.Windows.Media.Brush CreateFrozenBrush(string hex)
    {
        var color = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(hex);
        var brush = new System.Windows.Media.SolidColorBrush(color);
        brush.Freeze();
        return brush;
    }

    /// <summary>
    /// Initializes the OverlayWindow and sets its size to virtual screen bounds.
    /// Usage example: var overlay = new OverlayWindow();
    /// </summary>
    public OverlayWindow()
    {
        InitializeComponent();
        Left = SystemParameters.VirtualScreenLeft;
        Top = SystemParameters.VirtualScreenTop;
        Width = SystemParameters.VirtualScreenWidth;
        Height = SystemParameters.VirtualScreenHeight;
        InitializeTimers();
        SetupSizeChangedHandlers();
    }

    private void InitializeTimers()
    {
        _holdTimer = new DispatcherTimer();
        _holdTimer.Interval = TimeSpan.FromMilliseconds(1000);
        _holdTimer.Tick += HoldTimer_Tick;

        _pauseTimer = new DispatcherTimer();
        _pauseTimer.Interval = TimeSpan.FromMilliseconds(1000);
        _pauseTimer.Tick += PauseTimer_Tick;
    }

    private void SetupSizeChangedHandlers()
    {
        UpBorder.SizeChanged += (s, e) => ApplyTransform(UpBorder, true, false);
        DownBorder.SizeChanged += (s, e) => ApplyTransform(DownBorder, true, false);
        LeftBorder.SizeChanged += (s, e) => ApplyTransform(LeftBorder, false, true);
        RightBorder.SizeChanged += (s, e) => ApplyTransform(RightBorder, false, true);
    }

    private void HoldTimer_Tick(object? sender, EventArgs e)
    {
        _holdTimer?.Stop();
        ShowGestureMenu(_lastMousePos, "");
    }

    private void PauseTimer_Tick(object? sender, EventArgs e)
    {
        _pauseTimer?.Stop();
        string currentPattern = RecognizeCurrentPattern();
        ShowGestureMenu(_lastMousePos, currentPattern);
    }

    private string RecognizeCurrentPattern()
    {
        var recognizer = new GestureRecognizer();
        try
        {
            var settings = new SettingsStore().Load();
            recognizer.SegmentThreshold = settings.SegmentThreshold;
        }
        catch {}

        var pointsList = new List<POINT>();
        foreach (var pt in TrailPolyline.Points)
        {
            pointsList.Add(new POINT { x = (int)pt.X, y = (int)pt.Y });
        }
        return recognizer.Recognize(pointsList);
    }

    private void ShowGestureMenu(System.Windows.Point mousePos, string currentPattern)
    {
        AppSettings settings;
        try { settings = new SettingsStore().Load(); }
        catch { return; }

        char lastChar = string.IsNullOrEmpty(currentPattern) ? '\0' : char.ToUpper(currentPattern[currentPattern.Length - 1]);

        var up = GetMappingForDirection(settings, currentPattern, 'U', lastChar);
        var down = GetMappingForDirection(settings, currentPattern, 'D', lastChar);
        var left = GetMappingForDirection(settings, currentPattern, 'L', lastChar);
        var right = GetMappingForDirection(settings, currentPattern, 'R', lastChar);

        ConfigureLabel(UpBorder, UpLabel, up, mousePos.X, mousePos.Y, 0, -50, true, false, "↑ ", lastChar == 'U');
        ConfigureLabel(DownBorder, DownLabel, down, mousePos.X, mousePos.Y, 0, 50, true, false, "↓ ", lastChar == 'D');
        ConfigureLabel(LeftBorder, LeftLabel, left, mousePos.X, mousePos.Y, -50, 0, false, true, "← ", lastChar == 'L');
        ConfigureLabel(RightBorder, RightLabel, right, mousePos.X, mousePos.Y, 50, 0, false, true, "→ ", lastChar == 'R');
    }

    private GestureMapping? GetMappingForDirection(AppSettings settings, string currentPattern, char dir, char lastChar)
    {
        string patternToFind = (dir == lastChar) ? currentPattern : (currentPattern + dir);
        return settings.Mappings.FirstOrDefault(m => m.Pattern.Equals(patternToFind, StringComparison.OrdinalIgnoreCase));
    }

    private void ConfigureLabel(Border border, TextBlock label, GestureMapping? action, double x, double y, double offsetX, double offsetY, bool centerX, bool centerY, string prefix, bool isReleaseAction)
    {
        if (action == null)
        {
            border.Visibility = Visibility.Collapsed;
            return;
        }
        label.Text = prefix + action.ActionName;
        label.Foreground = isReleaseAction ? GreenTextBrush : NormalTextBrush;
        border.BorderBrush = isReleaseAction ? GreenBorderBrush : NormalBorderBrush;
        border.Visibility = Visibility.Visible;
        Canvas.SetLeft(border, x + offsetX);
        Canvas.SetTop(border, y + offsetY);
    }

    private void ApplyTransform(FrameworkElement element, bool centerX, bool centerY)
    {
        double xOffset = 0;
        double yOffset = 0;
        if (centerX) xOffset = -element.ActualWidth / 2;
        else if (element.Name == "LeftBorder") xOffset = -element.ActualWidth;

        if (centerY) yOffset = -element.ActualHeight / 2;
        else if (element.Name == "UpBorder") yOffset = -element.ActualHeight;

        element.RenderTransform = new System.Windows.Media.TranslateTransform(xOffset, yOffset);
    }

    private void HideLabels()
    {
        UpBorder.Visibility = Visibility.Collapsed;
        DownBorder.Visibility = Visibility.Collapsed;
        LeftBorder.Visibility = Visibility.Collapsed;
        RightBorder.Visibility = Visibility.Collapsed;
    }

    /// <summary>
    /// Starts tracking a gesture by clearing the old trail and showing the window.
    /// Usage example: overlay.StartGesture(new Point(100, 200));
    /// </summary>
    public void StartGesture(System.Windows.Point startPoint)
    {
        BeginAnimation(OpacityProperty, null);
        TrailPolyline.Points.Clear();
        Opacity = 1.0;
        var localPt = ScreenToWindow(startPoint);
        TrailPolyline.Points.Add(localPt);
        _lastMousePos = localPt;

        HideLabels();
        _holdTimer?.Stop();
        _holdTimer?.Start();
        _pauseTimer?.Stop();
        Show();
    }

    /// <summary>
    /// Adds a screen coordinate point to the drawn gesture path.
    /// Usage example: overlay.AddPoint(new Point(105, 205));
    /// </summary>
    public void AddPoint(System.Windows.Point screenPoint)
    {
        var localPt = ScreenToWindow(screenPoint);
        TrailPolyline.Points.Add(localPt);
        _lastMousePos = localPt;

        HideLabels();
        _holdTimer?.Stop();
        _pauseTimer?.Stop();
        _pauseTimer?.Start();
    }

    /// <summary>
    /// Fades out the visual trail and hides the overlay window.
    /// Usage example: overlay.FadeOutAndHide();
    /// </summary>
    public void FadeOutAndHide()
    {
        _holdTimer?.Stop();
        _pauseTimer?.Stop();
        HideLabels();

        var anim = new DoubleAnimation(1.0, 0.0, TimeSpan.FromMilliseconds(300));
        anim.Completed += (s, e) =>
        {
            Hide();
            TrailPolyline.Points.Clear();
            Opacity = 1.0;
        };
        BeginAnimation(OpacityProperty, anim);
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        MakeClickThrough();
    }

    private void MakeClickThrough()
    {
        var helper = new WindowInteropHelper(this);
        int exStyle = GetWindowLong(helper.Handle, GWL_EXSTYLE);
        SetWindowLong(helper.Handle, GWL_EXSTYLE, exStyle | WS_EX_TRANSPARENT | WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE);
    }

    private System.Windows.Point ScreenToWindow(System.Windows.Point screenPoint)
    {
        double x = screenPoint.X - Left;
        double y = screenPoint.Y - Top;
        return new System.Windows.Point(x, y);
    }
}
