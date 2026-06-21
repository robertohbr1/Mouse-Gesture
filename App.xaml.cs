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
    internal System.Windows.Forms.NotifyIcon? NotifyIcon => _notifyIcon;

    private MainWindow? _settingsWindow;
    private CircularMenuWindow? _circularMenu;
    public new MainWindow? MainWindow
    {
        get => _settingsWindow;
        set
        {
            _settingsWindow = value;
            base.MainWindow = value;
        }
    }

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
        ShutdownMode = ShutdownMode.OnExplicitShutdown;
        base.OnStartup(e);
        
        string logPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "debug_log.txt");
        try
        {
            System.IO.File.WriteAllText(logPath, $"[{DateTime.Now:HH:mm:ss}] Application Startup.\n");
        }
        catch {}

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
        _hook.OpenCircularMenuAction = ShowCircularMenu;
        _overlay = new OverlayWindow();
    }

    private void ShowCircularMenu()
    {
        Dispatcher.BeginInvoke(new Action(() =>
        {
            if (_circularMenu != null)
            {
                _circularMenu.Close();
            }
            _circularMenu = new CircularMenuWindow();
            _circularMenu.Closed += (s, e) => _circularMenu = null;
            _circularMenu.Show();
            _circularMenu.Activate();
        }));
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

    internal System.Windows.Forms.ContextMenuStrip CreateContextMenu()
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
        _hook.RightButtonUp += OnRightButtonUp;
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

    private void OnRightButtonUp(object? sender, POINT pt)
    {
        _overlay.FadeOutAndHide();
    }

    private void Log(string message)
    {
        string logPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "debug_log.txt");
        try
        {
            System.IO.File.AppendAllText(logPath, $"[{DateTime.Now:HH:mm:ss}] {message}\n");
        }
        catch {}
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
            Log($"Recognized gesture: '{pattern}'");
            if (!string.IsNullOrEmpty(pattern))
            {
                ExecuteGestureAction(pattern);
            }
        }
        catch (Exception ex)
        {
            Log($"Error in OnGestureComplete: {ex.Message}");
            System.Windows.MessageBox.Show($"Erro no gesto: {ex.Message}", "MouseKeyb", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Simulates the keyboard shortcut associated with a gesture mapping.
    /// </summary>
    public void SimulateGestureMapping(GestureMapping mapping)
    {
        if (mapping == null) return;
        System.Threading.Tasks.Task.Run(async () =>
        {
            await System.Threading.Tasks.Task.Delay(50);
            _ = Dispatcher.BeginInvoke(new Action(() => SimulateKeysInternal(mapping)));
        });
    }

    private void SimulateKeysInternal(GestureMapping mapping)
    {
        try
        {
            _simulator.SimulateKeys(mapping.Keys);
            Log("Keys simulated successfully.");
        }
        catch (Exception ex)
        {
            Log($"Simulation error: {ex.Message}");
        }
    }

    private void ExecuteGestureAction(string pattern)
    {
        var activeSettings = _store.Load();
        var match = activeSettings.Mappings.FirstOrDefault(m => m.Pattern.Equals(pattern, StringComparison.OrdinalIgnoreCase));
        if (match != null)
        {
            SimulateGestureMapping(match);
        }
        else
        {
            Log($"No action mapping found for pattern: '{pattern}'");
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
        var circularHelper = _circularMenu != null ? new WindowInteropHelper(_circularMenu).Handle : IntPtr.Zero;
        return hwnd == mainHelper || hwnd == overlayHelper || hwnd == circularHelper;
    }

    internal void ShowMainWindow()
    {
        string logPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "debug_log.txt");
        try
        {
            System.IO.File.AppendAllText(logPath, $"[{DateTime.Now:HH:mm:ss}] ShowMainWindow called. MainWindow is null: {MainWindow == null}\n");
        }
        catch {}

        Dispatcher.BeginInvoke(new Action(() =>
        {
            try
            {
                if (MainWindow == null)
                {
                    try { System.IO.File.AppendAllText(logPath, $"[{DateTime.Now:HH:mm:ss}] Creating new MainWindow\n"); } catch {}
                    MainWindow = new MainWindow();
                }
                else
                {
                    try { System.IO.File.AppendAllText(logPath, $"[{DateTime.Now:HH:mm:ss}] Reusing existing MainWindow. Visibility: {MainWindow.Visibility}, WindowState: {MainWindow.WindowState}\n"); } catch {}
                }

                if (MainWindow.WindowState == WindowState.Minimized)
                {
                    MainWindow.WindowState = WindowState.Normal;
                }
                MainWindow.Visibility = Visibility.Visible;
                MainWindow.Show();
                MainWindow.Activate();
                MainWindow.Focus();
                
                try { System.IO.File.AppendAllText(logPath, $"[{DateTime.Now:HH:mm:ss}] Show, Activate and Focus completed successfully. Visibility: {MainWindow.Visibility}, WindowState: {MainWindow.WindowState}\n"); } catch {}
            }
            catch (Exception ex)
            {
                try { System.IO.File.AppendAllText(logPath, $"[{DateTime.Now:HH:mm:ss}] ERROR inside Dispatcher: {ex.Message}\n{ex.StackTrace}\n"); } catch {}
            }
        }));
    }

    internal void ExitApplication()
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
