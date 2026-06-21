using System;
using System.Windows;
using System.Windows.Input;

namespace MouseKeyb;

/// <summary>
/// Window showing the circular menu image.
/// </summary>
public partial class CircularMenuWindow : Window
{
    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern bool SetCursorPos(int X, int Y);

    /// <summary>
    /// Initializes the CircularMenuWindow.
    /// </summary>
    public CircularMenuWindow()
    {
        InitializeComponent();
        UpdateButtonsDisplay();
        Loaded += (s, e) => PositionMouseAtCenter();
    }

    private void PositionMouseAtCenter()
    {
        try
        {
            double cx = ActualWidth / 2;
            double cy = ActualHeight / 2;
            System.Windows.Point screenCenter = PointToScreen(new System.Windows.Point(cx, cy));
            SetCursorPos((int)screenCenter.X, (int)screenCenter.Y);
        }
        catch
        {
            PositionMouseAtPrimaryScreenCenter();
        }
    }

    private void PositionMouseAtPrimaryScreenCenter()
    {
        var source = PresentationSource.FromVisual(this);
        if (source?.CompositionTarget == null)
        {
            return;
        }
        var matrix = source.CompositionTarget.TransformToDevice;
        double cx = SystemParameters.PrimaryScreenWidth / 2;
        double cy = SystemParameters.PrimaryScreenHeight / 2;
        var pixelCenter = matrix.Transform(new System.Windows.Point(cx, cy));
        SetCursorPos((int)pixelCenter.X, (int)pixelCenter.Y);
    }

    private bool _isClosing;
    private bool _isConfiguring;

    private void CloseWindow()
    {
        if (_isClosing)
        {
            return;
        }
        _isClosing = true;
        Close();
    }

    private void Window_Deactivated(object sender, EventArgs e)
    {
        if (!_isConfiguring)
        {
            CloseWindow();
        }
    }

    private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == System.Windows.Input.Key.Escape)
        {
            CloseWindow();
        }
    }

    private void Window_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (e.ChangedButton == System.Windows.Input.MouseButton.Left)
        {
            CloseWindow();
        }
    }

    private void UpdateButtonsDisplay()
    {
        var settings = new SettingsStore().Load();
        Button1Text.Text = settings.CircularButtons[0].Caption;
        Button2Text.Text = settings.CircularButtons[1].Caption;
        Button3Text.Text = settings.CircularButtons[2].Caption;
        Button4Text.Text = settings.CircularButtons[3].Caption;
        Button5Text.Text = settings.CircularButtons[4].Caption;
        Button6Text.Text = settings.CircularButtons[5].Caption;
    }

    private void Button_LeftClick(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement element && int.TryParse(element.Tag?.ToString(), out int buttonIndex))
        {
            ExecuteButtonProgram(buttonIndex);
        }
    }

    private void Button_MouseRightButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement element && int.TryParse(element.Tag?.ToString(), out int buttonIndex))
        {
            OpenConfigDialog(buttonIndex);
        }
        e.Handled = true;
    }

    private void OpenConfigDialog(int buttonIndex)
    {
        _isConfiguring = true;
        var store = new SettingsStore();
        var settings = store.Load();
        var config = settings.CircularButtons[buttonIndex - 1];
        var configWindow = new ButtonConfigWindow(config.Caption, config.ProgramPath) { Owner = this };
        if (configWindow.ShowDialog() == true)
        {
            config.Caption = configWindow.ButtonCaption;
            config.ProgramPath = configWindow.ProgramPath;
            store.Save(settings);
            UpdateButtonsDisplay();
        }
        _isConfiguring = false;
    }

    private void ExecuteButtonProgram(int buttonIndex)
    {
        var settings = new SettingsStore().Load();
        var config = settings.CircularButtons[buttonIndex - 1];
        if (string.IsNullOrWhiteSpace(config.ProgramPath))
        {
            return;
        }
        LaunchProgram(config.ProgramPath);
        CloseWindow();
    }

    private void LaunchProgram(string programPath)
    {
        var (file, args) = ParseCommandLine(programPath);
        try
        {
            var info = new System.Diagnostics.ProcessStartInfo(file) { Arguments = args, UseShellExecute = true };
            System.Diagnostics.Process.Start(info);
        }
        catch
        {
            TryFallback(file, args);
        }
    }

    private void TryFallback(string filePath, string arguments)
    {
        if (filePath.EndsWith("notepad++.exe", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                var info = new System.Diagnostics.ProcessStartInfo("notepad.exe") { Arguments = arguments, UseShellExecute = true };
                System.Diagnostics.Process.Start(info);
                return;
            }
            catch {}
        }
        System.Windows.MessageBox.Show("Erro ao executar o programa.", "MouseKeyb", MessageBoxButton.OK, MessageBoxImage.Error);
    }

    private static (string File, string Args) ParseCommandLine(string commandLine)
    {
        string trimmed = commandLine.Trim();
        if (trimmed.StartsWith("\""))
        {
            int nextQuote = trimmed.IndexOf("\"", 1);
            if (nextQuote != -1)
            {
                string file = trimmed.Substring(1, nextQuote - 1).Trim();
                string args = trimmed.Substring(nextQuote + 1).Trim();
                return (file, args);
            }
        }
        int firstSpace = trimmed.IndexOf(" ");
        if (firstSpace != -1)
        {
            string file = trimmed.Substring(0, firstSpace).Trim();
            string args = trimmed.Substring(firstSpace + 1).Trim();
            return (file, args);
        }
        return (trimmed, string.Empty);
    }

    private void ListButton_Click(object sender, RoutedEventArgs e)
    {
        _isConfiguring = true;
        var listWindow = new CommandListWindow { Owner = this };
        if (listWindow.ShowDialog() == true && listWindow.SelectedMapping != null)
        {
            if (System.Windows.Application.Current is App app)
            {
                app.SimulateGestureMapping(listWindow.SelectedMapping);
            }
            CloseWindow();
        }
        else
        {
            _isConfiguring = false;
        }
    }

    private void ConfigButton_Click(object sender, RoutedEventArgs e)
    {
        if (System.Windows.Application.Current is App app)
        {
            app.ShowMainWindow();
        }
        CloseWindow();
    }

    private void ExitButton_Click(object sender, RoutedEventArgs e)
    {
        if (System.Windows.Application.Current is App app)
        {
            app.ExitApplication();
        }
        CloseWindow();
    }
}
