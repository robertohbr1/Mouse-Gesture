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
    }

    private bool _isClosing;

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
        CloseWindow();
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
        CloseWindow();
    }

    private void Button_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("notepad++.exe") { UseShellExecute = true });
        }
        catch
        {
            LaunchFallbackNotepad();
        }
        CloseWindow();
    }

    private void LaunchFallbackNotepad()
    {
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("notepad.exe") { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Erro: {ex.Message}", "MouseKeyb", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
