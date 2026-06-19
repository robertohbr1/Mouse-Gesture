using System;
using System.Windows;
using Microsoft.Win32;

namespace MouseKeyb;

/// <summary>
/// Dialog window to configure circular button caption and program path.
/// </summary>
public partial class ButtonConfigWindow : Window
{
    public string ButtonCaption => CaptionTextBox.Text;
    public string ProgramPath => ProgramTextBox.Text;

    public ButtonConfigWindow(string caption, string path)
    {
        InitializeComponent();
        CaptionTextBox.Text = caption;
        ProgramTextBox.Text = path;
    }

    private void BrowseButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "Executáveis (*.exe)|*.exe|Todos os arquivos (*.*)|*.*",
            Title = "Selecionar Programa"
        };
        if (dialog.ShowDialog() == true)
        {
            ProgramTextBox.Text = dialog.FileName;
        }
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
