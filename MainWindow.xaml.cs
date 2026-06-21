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
    private readonly KeyboardRecordHook _recordHook = new();
    private readonly Dictionary<GestureMapping, string> _activePatterns = new();

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
        _recordHook.KeyCallback = OnRecordKeyCallback;
        InitializePatternValidation();
    }

    private void AddMappingButton_Click(object sender, RoutedEventArgs e)
    {
        string uniquePattern = GetUniqueDefaultPattern();
        var newMapping = new GestureMapping
        {
            ActionName = "Novo Atalho",
            Pattern = uniquePattern,
            Keys = new List<KeyStroke> 
            { 
                new() { Vk = 0x11, Name = "Ctrl" }, 
                new() { Vk = 0x43, Name = "C" } 
            }
        };
        Mappings.Add(newMapping);
        Dispatcher.BeginInvoke(new Action(() =>
        {
            MappingsListBox.SelectedItem = newMapping;
            MappingsListBox.ScrollIntoView(newMapping);
        }), System.Windows.Threading.DispatcherPriority.Background);
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
            _recordHook.Start();
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
        _recordHook.Stop();
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

    private void OnRecordKeyCallback(int msg, int vk)
    {
        var key = KeyInterop.KeyFromVirtualKey(vk);
        string name = GetKeyName(key);
        bool isDown = msg == 0x0100 || msg == 0x0104; // WM_KEYDOWN or WM_SYSKEYDOWN
        if (isDown)
        {
            AddKeyDownRecord((ushort)vk, name);
        }
        else
        {
            _recordedKeys.Add(new KeyStroke { Vk = (ushort)vk, Name = name, Type = KeyEventType.Up });
            UpdateCapturedKeysText();
        }
    }

    private void AddKeyDownRecord(ushort vk, string name)
    {
        if (!_recordedKeys.Any(k => k.Vk == vk && k.Type == KeyEventType.Down))
        {
            _recordedKeys.Add(new KeyStroke { Vk = vk, Name = name, Type = KeyEventType.Down });
            UpdateCapturedKeysText();
        }
    }

    private void InitializePatternValidation()
    {
        foreach (var mapping in Mappings)
        {
            _activePatterns[mapping] = mapping.Pattern;
            mapping.PropertyChanged += Mapping_PropertyChanged;
        }
        Mappings.CollectionChanged += Mappings_CollectionChanged;
    }

    private void Mappings_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems != null)
        {
            foreach (GestureMapping item in e.NewItems)
            {
                _activePatterns[item] = item.Pattern;
                item.PropertyChanged += Mapping_PropertyChanged;
            }
        }
        if (e.OldItems != null)
        {
            foreach (GestureMapping item in e.OldItems)
            {
                _activePatterns.Remove(item);
                item.PropertyChanged -= Mapping_PropertyChanged;
            }
        }
    }

    private void Mapping_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (sender is GestureMapping mapping && e.PropertyName == nameof(GestureMapping.Pattern))
        {
            ValidateAndRevertPattern(mapping);
        }
    }

    private void ValidateAndRevertPattern(GestureMapping mapping)
    {
        string newPattern = mapping.Pattern.ToUpperInvariant().Trim();
        string oldPattern = _activePatterns.TryGetValue(mapping, out string? val) ? val : string.Empty;
        if (newPattern == oldPattern) return;
        bool exists = Mappings.Any(m => m != mapping && m.Pattern.Equals(newPattern, StringComparison.OrdinalIgnoreCase));
        if (exists)
        {
            System.Windows.MessageBox.Show($"O gesto '{newPattern}' já está em uso por outro atalho.", "MouseKeyb", MessageBoxButton.OK, MessageBoxImage.Warning);
            Dispatcher.BeginInvoke(new Action(() => mapping.Pattern = oldPattern));
            return;
        }
        _activePatterns[mapping] = newPattern;
        mapping.Pattern = newPattern;
    }

    private string GetUniqueDefaultPattern()
    {
        string[] candidates = { "R", "L", "U", "D", "UR", "UL", "DR", "DL", "RU", "RD", "LU", "LD" };
        foreach (var cand in candidates)
        {
            if (!Mappings.Any(m => m.Pattern.Equals(cand, StringComparison.OrdinalIgnoreCase)))
            {
                return cand;
            }
        }
        return GenerateRandomUniquePattern();
    }

    private string GenerateRandomUniquePattern()
    {
        var rand = new Random();
        string[] dirs = { "R", "L", "U", "D" };
        for (int i = 0; i < 100; i++)
        {
            string pat = dirs[rand.Next(4)] + dirs[rand.Next(4)];
            if (!Mappings.Any(m => m.Pattern.Equals(pat, StringComparison.OrdinalIgnoreCase)))
            {
                return pat;
            }
        }
        return "R";
    }

    protected override void OnClosed(EventArgs e)
    {
        _recordHook.Stop();
        base.OnClosed(e);
        if (System.Windows.Application.Current is App app)
        {
            app.MainWindow = null;
        }
    }
}