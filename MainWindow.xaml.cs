using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace MouseKeyb;

/// <summary>
/// Dashboard window to configure mouse gestures and record keyboard shortcuts.
/// </summary>
public partial class MainWindow : Window
{
    private readonly SettingsStore _store;
    private readonly AppSettings _settings;
    private readonly List<KeyStroke> _recordedKeys = new();
    private GestureMapping? _mappingBeingRecorded;

    public ObservableCollection<GestureMapping> Mappings { get; }

    /// <summary>
    /// Initializes MainWindow, loading the mappings and initializing properties.
    /// Usage example: var window = new MainWindow();
    /// </summary>
    public MainWindow()
    {
        InitializeComponent();
        if (System.Windows.Application.Current is App app)
        {
            app.MainWindow = this;
        }
        _store = new SettingsStore();
        _settings = _store.Load();
        Mappings = new ObservableCollection<GestureMapping>(_settings.Mappings);
        MappingsListBox.ItemsSource = Mappings;
        ThresholdSlider.Value = _settings.SegmentThreshold;
        TrayMinimizeCheckBox.IsChecked = true;
        PreviewKeyDown += MainWindow_PreviewKeyDown;
        PreviewKeyUp += MainWindow_PreviewKeyUp;
    }

    private void AddMappingButton_Click(object sender, RoutedEventArgs e)
    {
        var newMapping = new GestureMapping
        {
            ActionName = "Novo Atalho",
            Pattern = "R",
            Keys = new List<KeyStroke> 
            { 
                new() { Vk = 0x11, Name = "Ctrl" }, 
                new() { Vk = 0x43, Name = "C" } 
            }
        };
        Mappings.Add(newMapping);
    }

    private void DeleteMapping_Click(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.Button btn && btn.DataContext is GestureMapping mapping)
        {
            Mappings.Remove(mapping);
        }
    }

    private void RecordKeys_Click(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.Button btn && btn.DataContext is GestureMapping mapping)
        {
            _mappingBeingRecorded = mapping;
            _recordedKeys.Clear();
            CapturedKeysText.Text = "Pressione as teclas...";
            RecorderOverlay.Visibility = Visibility.Visible;
        }
    }

    private void SaveRecordedKeys_Click(object sender, RoutedEventArgs e)
    {
        if (_mappingBeingRecorded != null && _recordedKeys.Count > 0)
        {
            _mappingBeingRecorded.Keys = GestureMapping.CollapseEvents(_recordedKeys);
            MappingsListBox.Items.Refresh();
        }
        CloseRecorderOverlay();
    }

    private void CancelRecording_Click(object sender, RoutedEventArgs e)
    {
        CloseRecorderOverlay();
    }

    private void CloseRecorderOverlay()
    {
        _mappingBeingRecorded = null;
        _recordedKeys.Clear();
        RecorderOverlay.Visibility = Visibility.Collapsed;
    }

    private void MainWindow_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (RecorderOverlay.Visibility != Visibility.Visible || e.IsRepeat)
        {
            return;
        }
        e.Handled = true;
        var key = e.Key == Key.System ? e.SystemKey : e.Key;
        ushort vk = (ushort)KeyInterop.VirtualKeyFromKey(key);
        string name = GetKeyName(key);

        _recordedKeys.Add(new KeyStroke { Vk = vk, Name = name, Type = KeyEventType.Down });
        UpdateCapturedKeysText();
    }

    private void MainWindow_PreviewKeyUp(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (RecorderOverlay.Visibility != Visibility.Visible)
        {
            return;
        }
        e.Handled = true;
        var key = e.Key == Key.System ? e.SystemKey : e.Key;
        ushort vk = (ushort)KeyInterop.VirtualKeyFromKey(key);
        string name = GetKeyName(key);

        _recordedKeys.Add(new KeyStroke { Vk = vk, Name = name, Type = KeyEventType.Up });
        UpdateCapturedKeysText();
    }

    private string GetKeyName(Key key)
    {
        if (key == Key.LeftCtrl || key == Key.RightCtrl) return "Ctrl";
        if (key == Key.LeftAlt || key == Key.RightAlt) return "Alt";
        if (key == Key.LeftShift || key == Key.RightShift) return "Shift";
        if (key == Key.LWin || key == Key.RWin) return "Win";
        return key.ToString();
    }

    private void UpdateCapturedKeysText()
    {
        var tempMapping = new GestureMapping { Keys = GestureMapping.CollapseEvents(_recordedKeys) };
        CapturedKeysText.Text = tempMapping.KeysString;
    }

    private void SaveSettings_Click(object sender, RoutedEventArgs e)
    {
        _settings.Mappings = new List<GestureMapping>(Mappings);
        _settings.SegmentThreshold = ThresholdSlider.Value;
        _store.Save(_settings);
        System.Windows.MessageBox.Show("Configurações salvas!", "MouseKeyb", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void ThresholdSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_settings != null)
        {
            _settings.SegmentThreshold = e.NewValue;
        }
    }

    private void TextBox_GotFocus(object sender, RoutedEventArgs e)
    {
        App.IsHookSuspended = true;
    }

    private void TextBox_LostFocus(object sender, RoutedEventArgs e)
    {
        App.IsHookSuspended = false;
    }

    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        if (App.IsExiting)
        {
            return;
        }
        if (TrayMinimizeCheckBox.IsChecked == true)
        {
            e.Cancel = true;
            Hide();
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);
        if (System.Windows.Application.Current is App app)
        {
            app.MainWindow = null;
        }
    }
}