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

        var up = settings.Mappings.FirstOrDefault(m => m.Pattern.Equals(currentPattern + "U", StringComparison.OrdinalIgnoreCase));
        var down = settings.Mappings.FirstOrDefault(m => m.Pattern.Equals(currentPattern + "D", StringComparison.OrdinalIgnoreCase));
        var left = settings.Mappings.FirstOrDefault(m => m.Pattern.Equals(currentPattern + "L", StringComparison.OrdinalIgnoreCase));
        var right = settings.Mappings.FirstOrDefault(m => m.Pattern.Equals(currentPattern + "R", StringComparison.OrdinalIgnoreCase));

        ConfigureLabel(UpBorder, UpLabel, up, mousePos.X, mousePos.Y, 0, -50, true, false, "↑ ");
        ConfigureLabel(DownBorder, DownLabel, down, mousePos.X, mousePos.Y, 0, 50, true, false, "↓ ");
        ConfigureLabel(LeftBorder, LeftLabel, left, mousePos.X, mousePos.Y, -50, 0, false, true, "← ");
        ConfigureLabel(RightBorder, RightLabel, right, mousePos.X, mousePos.Y, 50, 0, false, true, "→ ");
    }

    private void ConfigureLabel(Border border, TextBlock label, GestureMapping? action, double x, double y, double offsetX, double offsetY, bool centerX, bool centerY, string prefix)
    {
        if (action == null)
        {
            border.Visibility = Visibility.Collapsed;
            return;
        }
        label.Text = prefix + action.ActionName;
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
