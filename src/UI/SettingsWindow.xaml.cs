using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using WechatBlind.Config;

namespace WechatBlind.UI;

/// <summary>
/// WPF 设置窗口
/// </summary>
internal partial class SettingsWindow : Window
{
    private readonly AppSettings _settings;
    private readonly PatternManager _patternManager;
    private List<PatternInfo> _patterns = new();
    private int _selectedPatternIndex = -1;
    private string? _selectedPatternPath;
    private bool _recordingHotkey;
    private uint _pendingModifiers;
    private uint _pendingVk;

    public event EventHandler<AppSettings>? SettingsSaved;
    public event EventHandler<AppSettings>? SettingsChanged;

    public SettingsWindow(AppSettings settings)
    {
        _settings = settings;
        _patternManager = new PatternManager();

        InitializeComponent();

        // 初始化控件值
        TglEnabled.IsChecked = _settings.Enabled;
        SldOpacity.Value = _settings.Opacity * 100;
        LblOpacity.Text = $"{(int)SldOpacity.Value}%";
        SldBlur.Value = _settings.BlurAmount;
        LblBlur.Text = $"{(int)SldBlur.Value}%";
        TglAutoStart.IsChecked = _settings.AutoStart;
        BtnHotkey.Content = FormatHotkey(_settings.HotKey);

        // 接线事件
        TglEnabled.Checked += OnToggleChanged;
        TglEnabled.Unchecked += OnToggleChanged;
        SldOpacity.ValueChanged += OnOpacityChanged;
        SldBlur.ValueChanged += OnBlurChanged;
        TglAutoStart.Checked += OnToggleChanged;
        TglAutoStart.Unchecked += OnToggleChanged;

        // 加载图案
        LoadPatterns();
    }

    #region 事件处理

    private void OnToggleChanged(object sender, RoutedEventArgs e) => OnSettingsChanged();

    private void OnOpacityChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        LblOpacity.Text = $"{(int)e.NewValue}%";
        OnSettingsChanged();
    }

    private void OnBlurChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        LblBlur.Text = $"{(int)e.NewValue}%";
        OnSettingsChanged();
    }

    private void OnSettingsChanged()
    {
        SettingsChanged?.Invoke(this, GetCurrentSettings());
    }

    private void OnHotkeyClick(object sender, RoutedEventArgs e)
    {
        if (_recordingHotkey) return;
        _recordingHotkey = true;
        BtnHotkey.Content = "请按下快捷键...";
        BtnHotkey.Focus();
        PreviewKeyDown += OnPreviewKeyDown;
    }

    private void OnPreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (!_recordingHotkey) return;

        var key = e.Key;
        if (key is System.Windows.Input.Key.LeftCtrl or System.Windows.Input.Key.RightCtrl or
            System.Windows.Input.Key.LeftShift or System.Windows.Input.Key.RightShift or
            System.Windows.Input.Key.LeftAlt or System.Windows.Input.Key.RightAlt or
            System.Windows.Input.Key.LWin or System.Windows.Input.Key.RWin)
            return;

        e.Handled = true;
        _recordingHotkey = false;
        PreviewKeyDown -= OnPreviewKeyDown;

        _pendingModifiers = 0;
        var mods = System.Windows.Input.Keyboard.Modifiers;
        if (mods.HasFlag(System.Windows.Input.ModifierKeys.Control)) _pendingModifiers |= 0x0002;
        if (mods.HasFlag(System.Windows.Input.ModifierKeys.Shift)) _pendingModifiers |= 0x0004;
        if (mods.HasFlag(System.Windows.Input.ModifierKeys.Alt)) _pendingModifiers |= 0x0001;

        _pendingVk = (uint)System.Windows.Input.KeyInterop.VirtualKeyFromKey(key);

        var settings = new HotKeySettings
        {
            Modifiers = ModifiersToString(_pendingModifiers),
            Key = key.ToString(),
        };
        BtnHotkey.Content = FormatHotkey(settings);
    }

    private void OnPatternClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (sender is not FrameworkElement fe || fe.Tag is not PatternInfo pattern) return;

        int index = _patterns.IndexOf(pattern);
        if (index >= 0)
        {
            SelectPattern(index);
            OnSettingsChanged();
        }
    }

    private void OnUploadPattern(object sender, RoutedEventArgs e)
    {
        var dlg = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "图片文件|*.png;*.jpg;*.jpeg;*.bmp;*.gif|所有文件|*.*",
            Title = "选择遮罩图案",
        };
        if (dlg.ShowDialog() != true) return;

        var name = Path.GetFileNameWithoutExtension(dlg.FileName);
        var savedPath = _patternManager.SavePattern(dlg.FileName, name);
        LoadPatterns();

        var idx = _patterns.FindIndex(p => p.FilePath == savedPath);
        if (idx >= 0) SelectPattern(idx);
    }

    private void OnDeletePattern(object sender, RoutedEventArgs e)
    {
        if (_selectedPatternIndex < 0 || _selectedPatternIndex >= _patterns.Count) return;
        var p = _patterns[_selectedPatternIndex];
        if (p.Type != PatternType.Custom && p.Type != PatternType.CustomGif) return;

        if (System.Windows.MessageBox.Show($"确定删除 \"{p.Name}\"？", "确认",
            MessageBoxButton.YesNo) == MessageBoxResult.Yes)
        {
            _patternManager.DeletePattern(p.Name);
            LoadPatterns();
        }
    }

    private void OnSave(object sender, RoutedEventArgs e)
    {
        _settings.Enabled = TglEnabled.IsChecked == true;
        _settings.Opacity = SldOpacity.Value / 100.0;
        _settings.BlurAmount = (int)SldBlur.Value;
        _settings.AutoStart = TglAutoStart.IsChecked == true;

        var sel = _selectedPatternIndex >= 0 && _selectedPatternIndex < _patterns.Count
            ? _patterns[_selectedPatternIndex] : null;
        if (sel != null)
        {
            _settings.PatternType = sel.Type.ToString();
            _settings.PresetPattern = sel.Preset.ToString();
            _settings.CustomPatternPath = sel.FilePath;
        }
        _settings.PatternOpacity = 1.0;
        _settings.IsGifPattern = sel?.Type == PatternType.CustomGif;

        if (_pendingModifiers != 0 || _pendingVk != 0)
        {
            _settings.HotKey.Modifiers = ModifiersToString(_pendingModifiers);
            _settings.HotKey.Key = System.Windows.Input.KeyInterop.KeyFromVirtualKey((int)_pendingVk).ToString();
        }

        SettingsSaved?.Invoke(this, _settings);
        DialogResult = true;
        Close();
    }

    private void OnCancel(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    #endregion

    #region 图案管理

    private void LoadPatterns()
    {
        _patterns = _patternManager.GetAllPatterns();
        PatternGrid.ItemsSource = _patterns;

        var cur = _patterns.FindIndex(p =>
            p.Type.ToString() == _settings.PatternType &&
            (p.Type != PatternType.Preset || p.Preset.ToString() == _settings.PresetPattern) &&
            (p.Type != PatternType.Custom || p.FilePath == _settings.CustomPatternPath) &&
            (p.Type != PatternType.CustomGif || p.FilePath == _settings.CustomPatternPath));

        if (cur >= 0)
        {
            _selectedPatternIndex = cur;
            // 延迟到容器生成后再高亮选中项
            PatternGrid.ItemContainerGenerator.StatusChanged += OnPatternContainerReady;
        }

        // 无匹配时默认选中第一项
        if (cur < 0 && _patterns.Count > 0)
        {
            _selectedPatternIndex = 0;
            PatternGrid.ItemContainerGenerator.StatusChanged += OnPatternContainerReady;
        }
    }

    private void OnPatternContainerReady(object? sender, EventArgs e)
    {
        if (PatternGrid.ItemContainerGenerator.Status != System.Windows.Controls.Primitives.GeneratorStatus.ContainersGenerated) return;
        PatternGrid.ItemContainerGenerator.StatusChanged -= OnPatternContainerReady;

        // 用 BeginInvoke 避免在布局期间同步调用导致死锁
        Dispatcher.BeginInvoke(() => SelectPattern(_selectedPatternIndex), System.Windows.Threading.DispatcherPriority.Loaded);
    }

    private void SelectPattern(int index)
    {
        _selectedPatternIndex = index;

        // 更新选中状态
        if (PatternGrid.ItemContainerGenerator.ContainerFromIndex(index) is FrameworkElement container)
        {
            var preview = FindChild<Border>(container, "PreviewBorder");
            if (preview != null)
            {
                preview.BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(52, 199, 89));
            }
        }

        // 重置其他项
        for (int i = 0; i < _patterns.Count; i++)
        {
            if (i == index) continue;
            if (PatternGrid.ItemContainerGenerator.ContainerFromIndex(i) is FrameworkElement c)
            {
                var preview = FindChild<Border>(c, "PreviewBorder");
                if (preview != null)
                {
                    preview.BorderBrush = System.Windows.Media.Brushes.Transparent;
                }
            }
        }

        if (index >= 0 && index < _patterns.Count)
        {
            _selectedPatternPath = _patterns[index].FilePath;
            BtnDeletePattern.IsEnabled = _patterns[index].Type == PatternType.Custom
                || _patterns[index].Type == PatternType.CustomGif;
        }
    }

    private static T? FindChild<T>(DependencyObject parent, string name) where T : FrameworkElement
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is T fe && fe.Name == name) return fe;
            var result = FindChild<T>(child, name);
            if (result != null) return result;
        }
        return null;
    }

    #endregion

    #region 工具方法

    private static string FormatHotkey(HotKeySettings h)
    {
        var parts = new List<string>();
        if (h.Modifiers.Contains("Control")) parts.Add("Ctrl");
        if (h.Modifiers.Contains("Shift")) parts.Add("Shift");
        if (h.Modifiers.Contains("Alt")) parts.Add("Alt");
        if (h.Modifiers.Contains("Win")) parts.Add("Win");
        parts.Add(h.Key.Replace("Key", ""));
        return string.Join(" + ", parts);
    }

    private static string ModifiersToString(uint m)
    {
        var parts = new List<string>();
        if ((m & 0x0002) != 0) parts.Add("Control");
        if ((m & 0x0004) != 0) parts.Add("Shift");
        if ((m & 0x0001) != 0) parts.Add("Alt");
        if ((m & 0x0008) != 0) parts.Add("Win");
        return string.Join(",", parts);
    }

    public AppSettings GetCurrentSettings()
    {
        var sel = _selectedPatternIndex >= 0 && _selectedPatternIndex < _patterns.Count
            ? _patterns[_selectedPatternIndex] : null;

        // 如果录制了新快捷键，使用新值；否则保留原值
        var hotkey = new HotKeySettings
        {
            Modifiers = _settings.HotKey.Modifiers,
            Key = _settings.HotKey.Key,
        };
        if (_pendingModifiers != 0 || _pendingVk != 0)
        {
            hotkey.Modifiers = ModifiersToString(_pendingModifiers);
            hotkey.Key = System.Windows.Input.KeyInterop.KeyFromVirtualKey((int)_pendingVk).ToString();
        }

        return new AppSettings
        {
            Enabled = TglEnabled.IsChecked == true,
            Opacity = SldOpacity.Value / 100.0,
            BlurAmount = (int)SldBlur.Value,
            PatternType = sel?.Type.ToString() ?? "None",
            PresetPattern = sel?.Preset.ToString() ?? "None",
            CustomPatternPath = sel?.FilePath,
            PatternOpacity = 1.0,
            IsGifPattern = sel?.Type == PatternType.CustomGif,
            AutoStart = TglAutoStart.IsChecked == true,
            HotKey = hotkey,
        };
    }

    protected override void OnClosed(EventArgs e)
    {
        _patternManager.Dispose();
        base.OnClosed(e);
    }

    #endregion
}
