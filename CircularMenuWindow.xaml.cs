using System;
using System.Windows;
using System.Windows.Input;

namespace MouseKeyb;

/// <summary>
/// Window showing the circular menu image.
/// </summary>
public partial class CircularMenuWindow : Window
{
    /// <summary>
    /// Initializes the CircularMenuWindow.
    /// </summary>
    public CircularMenuWindow()
    {
        InitializeComponent();
        UpdateButtonsDisplay();
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
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(config.ProgramPath) { UseShellExecute = true });
        }
        catch
        {
            TryFallback(config.ProgramPath);
        }
        CloseWindow();
    }

    private void TryFallback(string programPath)
    {
        if (programPath.Equals("notepad++.exe", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("notepad.exe") { UseShellExecute = true });
                return;
            }
            catch {}
        }
        System.Windows.MessageBox.Show("Erro ao executar o programa.", "MouseKeyb", MessageBoxButton.OK, MessageBoxImage.Error);
    }
}
