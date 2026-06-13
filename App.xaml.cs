using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace MouseKeyb;

/// <summary>
/// Main application entry point and lifecycle coordinator for MouseKeyb.
/// </summary>
public partial class App : System.Windows.Application
{
    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    /// <summary>
    /// Gets or sets a value indicating whether the global mouse gesture hook is temporarily suspended.
    /// Usage example: App.IsHookSuspended = true;
    /// </summary>
    public static bool IsHookSuspended { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the application is fully exiting (closing completely).
    /// Usage example: App.IsExiting = true;
    /// </summary>
    public static bool IsExiting { get; set; }

    private static System.Threading.Mutex? _mutex;

    private SettingsStore _store = null!;
    private AppSettings _settings = null!;
    private GestureRecognizer _recognizer = null!;
    private KeyboardSimulator _simulator = null!;
    private MouseHook _hook = null!;
    private OverlayWindow _overlay = null!;
    private System.Windows.Forms.NotifyIcon? _notifyIcon;

    protected override void OnStartup(StartupEventArgs e)
    {
        const string appName = "MouseKeybSingleInstanceMutex";
        _mutex = new System.Threading.Mutex(true, appName, out bool createdNew);
        if (!createdNew)
        {
            System.Windows.MessageBox.Show("O MouseKeyb já está rodando em segundo plano. Verifique o ícone na bandeja.", "MouseKeyb", MessageBoxButton.OK, MessageBoxImage.Information);
            Shutdown();
            return;
        }
        base.OnStartup(e);
        ShutdownMode = ShutdownMode.OnExplicitShutdown;
        InitializeServices();
        SetupTrayIcon();
        StartHook();
    }

    private void InitializeServices()
    {
        _store = new SettingsStore();
        _settings = _store.Load();
        _recognizer = new GestureRecognizer { SegmentThreshold = _settings.SegmentThreshold };
        _simulator = new KeyboardSimulator();
        _hook = new MouseHook();
        _overlay = new OverlayWindow();
    }

    private void SetupTrayIcon()
    {
        _notifyIcon = new System.Windows.Forms.NotifyIcon
        {
            Icon = System.Drawing.SystemIcons.Application,
            Text = "MouseKeyb",
            Visible = true
        };
        _notifyIcon.DoubleClick += (s, e) => ShowMainWindow();
        _notifyIcon.ContextMenuStrip = CreateContextMenu();
    }

    private System.Windows.Forms.ContextMenuStrip CreateContextMenu()
    {
        var menu = new System.Windows.Forms.ContextMenuStrip();
        menu.Items.Add("Configurações", null, (s, e) => ShowMainWindow());
        menu.Items.Add("Sair", null, (s, e) => ExitApplication());
        return menu;
    }

    private void StartHook()
    {
        _hook.RightButtonDown += OnRightButtonDown;
        _hook.GestureMove += OnGestureMove;
        _hook.GestureComplete += OnGestureComplete;
        _hook.Start();
    }

    private void OnRightButtonDown(object? sender, POINT pt)
    {
        if (IsHookSuspended || IsActiveWindowSelf())
        {
            return;
        }
        _overlay.StartGesture(new System.Windows.Point(pt.x, pt.y));
    }

    private void OnGestureMove(object? sender, POINT pt)
    {
        if (IsHookSuspended || IsActiveWindowSelf())
        {
            return;
        }
        _overlay.AddPoint(new System.Windows.Point(pt.x, pt.y));
    }

    private void OnGestureComplete(object? sender, List<POINT> points)
    {
        _overlay.FadeOutAndHide();
        if (IsHookSuspended || IsActiveWindowSelf())
        {
            return;
        }

        try
        {
            string pattern = _recognizer.Recognize(points);
            if (!string.IsNullOrEmpty(pattern))
            {
                ExecuteGestureAction(pattern);
            }
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Erro no gesto: {ex.Message}", "MouseKeyb", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ExecuteGestureAction(string pattern)
    {
        var activeSettings = _store.Load();
        var match = activeSettings.Mappings.FirstOrDefault(m => m.Pattern.Equals(pattern, StringComparison.OrdinalIgnoreCase));
        if (match != null)
        {
            var vkList = match.Keys.Select(k => k.Vk).ToArray();
            _simulator.SimulateKeys(vkList);
        }
    }

    private bool IsActiveWindowSelf()
    {
        IntPtr activeHwnd = GetForegroundWindow();
        if (activeHwnd == IntPtr.Zero)
        {
            return false;
        }
        return IsWindowOwnedBySelf(activeHwnd);
    }

    private bool IsWindowOwnedBySelf(IntPtr hwnd)
    {
        var mainHelper = MainWindow != null ? new WindowInteropHelper(MainWindow).Handle : IntPtr.Zero;
        var overlayHelper = _overlay != null ? new WindowInteropHelper(_overlay).Handle : IntPtr.Zero;
        return hwnd == mainHelper || hwnd == overlayHelper;
    }

    private void ShowMainWindow()
    {
        if (MainWindow == null)
        {
            MainWindow = new MainWindow();
        }
        MainWindow.Show();
        MainWindow.Activate();
    }

    private void ExitApplication()
    {
        IsExiting = true;
        _hook.Stop();
        _notifyIcon?.Dispose();
        _overlay.Close();
        MainWindow?.Close();
        _mutex?.ReleaseMutex();
        _mutex?.Dispose();
        Shutdown();
    }
}
