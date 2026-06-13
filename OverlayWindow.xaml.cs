using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Animation;

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
    }

    /// <summary>
    /// Fades out the visual trail and hides the overlay window.
    /// Usage example: overlay.FadeOutAndHide();
    /// </summary>
    public void FadeOutAndHide()
    {
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
