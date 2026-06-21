using System;
using System.Windows;
using System.Windows.Input;

namespace MouseKeyb;

/// <summary>
/// Window showing the list of available commands and gestures.
/// </summary>
public partial class CommandListWindow : Window
{
    /// <summary>
    /// Gets the gesture mapping selected by the user.
    /// </summary>
    public GestureMapping? SelectedMapping { get; private set; }

    /// <summary>
    /// Initializes a new instance of the CommandListWindow.
    /// </summary>
    public CommandListWindow()
    {
        InitializeComponent();
        LoadCommands();
    }

    private void LoadCommands()
    {
        var store = new SettingsStore();
        var settings = store.Load();
        CommandItemsControl.ItemsSource = settings.Mappings;
    }

    private void CommandItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement element && element.DataContext is GestureMapping mapping)
        {
            SelectedMapping = mapping;
            DialogResult = true;
            Close();
        }
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            DialogResult = false;
            Close();
        }
    }
}
