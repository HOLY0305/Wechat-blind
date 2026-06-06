using WechatBlind.Config;
using WechatBlind.Win32;

namespace WechatBlind.UI;

/// <summary>
/// 设置面板
/// </summary>
internal sealed class SettingsForm : Form
{
    private readonly AppSettings _settings;
    private readonly PatternManager _patternManager;
    private readonly CheckBox _chkEnabled;
    private readonly TrackBar _trkOpacity;
    private readonly Label _lblOpacityValue;
    private readonly TrackBar _trkBlur;
    private readonly Label _lblBlurValue;
    private readonly ComboBox _cmbPattern;
    private readonly Button _btnUploadPattern;
    private readonly Button _btnDeletePattern;
    private readonly TrackBar _trkPatternOpacity;
    private readonly Label _lblPatternOpacityValue;
    private readonly Button _btnHotkey;
    private readonly CheckBox _chkAutoStart;
    private bool _recordingHotkey;
    private uint _pendingModifiers;
    private uint _pendingVk;
    private string? _selectedPatternPath;

    /// <summary>
    /// 设置保存后触发
    /// </summary>
    public event EventHandler<AppSettings>? SettingsSaved;

    /// <summary>
    /// 设置实时变化时触发（用于实时预览）
    /// </summary>
    public event EventHandler<AppSettings>? SettingsChanged;

    public SettingsForm(AppSettings settings)
    {
        _settings = settings;
        _patternManager = new PatternManager();

        Text = "微信幕布 - 设置";
        ClientSize = new Size(820, 780);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterScreen;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        Font = new Font("Microsoft YaHei UI", 12F);

        int y = 40;
        int labelX = 50;
        int controlX = 230;
        int rowHeight = 80;

        // 启用/禁用
        _chkEnabled = new CheckBox
        {
            Text = "启用遮罩",
            Checked = settings.Enabled,
            Location = new Point(labelX, y),
            AutoSize = true,
            Font = new Font("Microsoft YaHei UI", 13F),
        };
        _chkEnabled.CheckedChanged += (s, e) => OnSettingsChanged();
        y += rowHeight;

        // 透明度
        var lblOpacity = new Label
        {
            Text = "遮罩透明度：",
            Location = new Point(labelX, y + 10),
            AutoSize = true,
        };

        _trkOpacity = new TrackBar
        {
            Minimum = 0,
            Maximum = 100,
            Value = (int)(settings.Opacity * 100),
            TickFrequency = 10,
            LargeChange = 10,
            Location = new Point(controlX, y),
            Width = 420,
            Height = 45,
        };

        _lblOpacityValue = new Label
        {
            Text = $"{_trkOpacity.Value}%",
            Location = new Point(controlX + 440, y + 10),
            AutoSize = true,
        };

        _trkOpacity.ValueChanged += (s, e) =>
        {
            _lblOpacityValue.Text = $"{_trkOpacity.Value}%";
            OnSettingsChanged();
        };
        y += rowHeight;

        // 模糊程度
        var lblBlur = new Label
        {
            Text = "模糊程度：",
            Location = new Point(labelX, y + 10),
            AutoSize = true,
        };

        _trkBlur = new TrackBar
        {
            Minimum = 0,
            Maximum = 100,
            Value = settings.BlurAmount,
            TickFrequency = 10,
            LargeChange = 10,
            Location = new Point(controlX, y),
            Width = 420,
            Height = 45,
        };

        _lblBlurValue = new Label
        {
            Text = $"{_trkBlur.Value}%",
            Location = new Point(controlX + 440, y + 10),
            AutoSize = true,
        };

        _trkBlur.ValueChanged += (s, e) =>
        {
            _lblBlurValue.Text = $"{_trkBlur.Value}%";
            OnSettingsChanged();
        };
        y += rowHeight;

        // 遮罩图案
        var lblPattern = new Label
        {
            Text = "遮罩图案：",
            Location = new Point(labelX, y + 10),
            AutoSize = true,
        };

        _cmbPattern = new ComboBox
        {
            Location = new Point(controlX, y + 6),
            Width = 280,
            Height = 35,
            DropDownStyle = ComboBoxStyle.DropDownList,
        };

        _btnUploadPattern = new Button
        {
            Text = "上传图片",
            Location = new Point(controlX + 290, y + 4),
            Width = 100,
            Height = 38,
        };

        _btnDeletePattern = new Button
        {
            Text = "删除",
            Location = new Point(controlX + 400, y + 4),
            Width = 60,
            Height = 38,
        };

        LoadPatterns();
        _cmbPattern.SelectedIndexChanged += (s, e) => OnPatternChanged();
        _btnUploadPattern.Click += OnUploadPattern;
        _btnDeletePattern.Click += OnDeletePattern;
        y += rowHeight;

        // 图案透明度
        var lblPatternOpacity = new Label
        {
            Text = "图案透明度：",
            Location = new Point(labelX, y + 10),
            AutoSize = true,
        };

        _trkPatternOpacity = new TrackBar
        {
            Minimum = 0,
            Maximum = 100,
            Value = (int)(settings.PatternOpacity * 100),
            TickFrequency = 10,
            LargeChange = 10,
            Location = new Point(controlX, y),
            Width = 420,
            Height = 45,
        };

        _lblPatternOpacityValue = new Label
        {
            Text = $"{_trkPatternOpacity.Value}%",
            Location = new Point(controlX + 440, y + 10),
            AutoSize = true,
        };

        _trkPatternOpacity.ValueChanged += (s, e) =>
        {
            _lblPatternOpacityValue.Text = $"{_trkPatternOpacity.Value}%";
            OnSettingsChanged();
        };
        y += rowHeight;

        // 快捷键
        var lblHotkey = new Label
        {
            Text = "切换快捷键：",
            Location = new Point(labelX, y + 12),
            AutoSize = true,
        };

        _btnHotkey = new Button
        {
            Text = FormatHotkey(settings.HotKey),
            Location = new Point(controlX, y + 4),
            Width = 440,
            Height = 42,
            Font = new Font("Microsoft YaHei UI", 12F),
        };

        _btnHotkey.Click += OnHotkeyButtonClick;
        y += rowHeight;

        // 开机自启
        _chkAutoStart = new CheckBox
        {
            Text = "开机自动启动",
            Checked = settings.AutoStart,
            Location = new Point(labelX, y),
            AutoSize = true,
            Font = new Font("Microsoft YaHei UI", 13F),
        };
        y += 70;

        // 分隔线
        var separator = new Label
        {
            BorderStyle = BorderStyle.Fixed3D,
            Location = new Point(labelX, y),
            Width = ClientSize.Width - labelX * 2,
            Height = 2,
        };
        y += 30;

        // 按钮
        var btnOk = new Button
        {
            Text = "确定",
            DialogResult = DialogResult.OK,
            Location = new Point(ClientSize.Width - 320, y),
            Width = 130,
            Height = 50,
            Font = new Font("Microsoft YaHei UI", 13F),
        };

        var btnCancel = new Button
        {
            Text = "取消",
            DialogResult = DialogResult.Cancel,
            Location = new Point(ClientSize.Width - 170, y),
            Width = 130,
            Height = 50,
            Font = new Font("Microsoft YaHei UI", 13F),
        };

        btnOk.Click += OnSave;

        Controls.AddRange(new Control[]
        {
            _chkEnabled, lblOpacity, _trkOpacity, _lblOpacityValue,
            lblBlur, _trkBlur, _lblBlurValue,
            lblPattern, _cmbPattern, _btnUploadPattern, _btnDeletePattern,
            lblPatternOpacity, _trkPatternOpacity, _lblPatternOpacityValue,
            lblHotkey, _btnHotkey, _chkAutoStart,
            separator, btnOk, btnCancel,
        });

        AcceptButton = btnOk;
        CancelButton = btnCancel;
    }

    private void LoadPatterns()
    {
        var patterns = _patternManager.GetAllPatterns();
        _cmbPattern.Items.Clear();
        _cmbPattern.DisplayMember = "Name";

        foreach (var pattern in patterns)
        {
            _cmbPattern.Items.Add(pattern);
        }

        // 选择当前图案
        var currentIndex = patterns.FindIndex(p =>
            p.Type.ToString() == _settings.PatternType &&
            (p.Type != PatternType.Preset || p.Preset.ToString() == _settings.PresetPattern) &&
            (p.Type != PatternType.Custom || p.FilePath == _settings.CustomPatternPath));

        if (currentIndex >= 0 && currentIndex < _cmbPattern.Items.Count)
        {
            _cmbPattern.SelectedIndex = currentIndex;
        }
        else if (_cmbPattern.Items.Count > 0)
        {
            _cmbPattern.SelectedIndex = 0;
        }
    }

    private void OnPatternChanged()
    {
        if (_cmbPattern.SelectedItem is PatternInfo pattern)
        {
            _selectedPatternPath = pattern.FilePath;
            _btnDeletePattern.Enabled = pattern.Type == PatternType.Custom;
            OnSettingsChanged();
        }
    }

    private void OnUploadPattern(object? sender, EventArgs e)
    {
        using var openFileDialog = new OpenFileDialog
        {
            Filter = "图片文件|*.png;*.jpg;*.jpeg;*.bmp|所有文件|*.*",
            Title = "选择遮罩图案",
        };

        if (openFileDialog.ShowDialog() == DialogResult.OK)
        {
            var name = Path.GetFileNameWithoutExtension(openFileDialog.FileName);
            var savedPath = _patternManager.SavePattern(openFileDialog.FileName, name);

            LoadPatterns();

            // 选择新上传的图案
            var newIndex = _cmbPattern.Items.Cast<PatternInfo>()
                .ToList()
                .FindIndex(p => p.FilePath == savedPath);

            if (newIndex >= 0)
            {
                _cmbPattern.SelectedIndex = newIndex;
            }
        }
    }

    private void OnDeletePattern(object? sender, EventArgs e)
    {
        if (_cmbPattern.SelectedItem is PatternInfo pattern && pattern.Type == PatternType.Custom)
        {
            var result = MessageBox.Show(
                $"确定要删除图案 \"{pattern.Name}\" 吗？",
                "确认删除",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                _patternManager.DeletePattern(pattern.Name);
                LoadPatterns();
            }
        }
    }

    /// <summary>
    /// 获取当前设置（用于实时预览）
    /// </summary>
    public AppSettings GetCurrentSettings()
    {
        var pattern = _cmbPattern.SelectedItem as PatternInfo;
        return new AppSettings
        {
            Enabled = _chkEnabled.Checked,
            Opacity = _trkOpacity.Value / 100.0,
            BlurAmount = _trkBlur.Value,
            PatternType = pattern?.Type.ToString() ?? "None",
            PresetPattern = pattern?.Preset.ToString() ?? "None",
            CustomPatternPath = pattern?.FilePath,
            PatternOpacity = _trkPatternOpacity.Value / 100.0,
            AutoStart = _chkAutoStart.Checked,
            HotKey = new HotKeySettings
            {
                Modifiers = _settings.HotKey.Modifiers,
                Key = _settings.HotKey.Key,
            },
        };
    }

    private void OnSettingsChanged()
    {
        SettingsChanged?.Invoke(this, GetCurrentSettings());
    }

    private void OnHotkeyButtonClick(object? sender, EventArgs e)
    {
        if (_recordingHotkey) return;

        _recordingHotkey = true;
        _btnHotkey.Text = "请按下快捷键...";
        _btnHotkey.Focus();

        PreviewKeyDown += OnPreviewKeyDown;
    }

    private void OnPreviewKeyDown(object? sender, PreviewKeyDownEventArgs e)
    {
        if (!_recordingHotkey) return;

        if (e.KeyCode is Keys.ControlKey or Keys.ShiftKey or Keys.Menu or Keys.LWin or Keys.RWin)
        {
            return;
        }

        _recordingHotkey = false;
        PreviewKeyDown -= OnPreviewKeyDown;

        _pendingModifiers = 0;
        if (e.Control) _pendingModifiers |= Win32Api.MOD_CONTROL;
        if (e.Shift) _pendingModifiers |= Win32Api.MOD_SHIFT;
        if (e.Alt) _pendingModifiers |= Win32Api.MOD_ALT;

        _pendingVk = (uint)e.KeyCode;

        _btnHotkey.Text = FormatHotkey(new HotKeySettings
        {
            Modifiers = ModifiersToString(_pendingModifiers),
            Key = e.KeyCode.ToString(),
        });
    }

    private void OnSave(object? sender, EventArgs e)
    {
        _settings.Enabled = _chkEnabled.Checked;
        _settings.Opacity = _trkOpacity.Value / 100.0;
        _settings.BlurAmount = _trkBlur.Value;
        _settings.AutoStart = _chkAutoStart.Checked;

        var pattern = _cmbPattern.SelectedItem as PatternInfo;
        _settings.PatternType = pattern?.Type.ToString() ?? "None";
        _settings.PresetPattern = pattern?.Preset.ToString() ?? "None";
        _settings.CustomPatternPath = pattern?.FilePath;
        _settings.PatternOpacity = _trkPatternOpacity.Value / 100.0;

        if (_pendingModifiers != 0 || _pendingVk != 0)
        {
            _settings.HotKey.Modifiers = ModifiersToString(_pendingModifiers);
            _settings.HotKey.Key = ((Keys)_pendingVk).ToString();
        }

        SettingsSaved?.Invoke(this, _settings);
    }

    private static string FormatHotkey(HotKeySettings hotkey)
    {
        var parts = new List<string>();

        if (hotkey.Modifiers.Contains("Control")) parts.Add("Ctrl");
        if (hotkey.Modifiers.Contains("Shift")) parts.Add("Shift");
        if (hotkey.Modifiers.Contains("Alt")) parts.Add("Alt");
        if (hotkey.Modifiers.Contains("Win")) parts.Add("Win");

        parts.Add(hotkey.Key.Replace("Key", ""));

        return string.Join(" + ", parts);
    }

    private static string ModifiersToString(uint modifiers)
    {
        var parts = new List<string>();
        if ((modifiers & Win32Api.MOD_CONTROL) != 0) parts.Add("Control");
        if ((modifiers & Win32Api.MOD_SHIFT) != 0) parts.Add("Shift");
        if ((modifiers & Win32Api.MOD_ALT) != 0) parts.Add("Alt");
        if ((modifiers & Win32Api.MOD_WIN) != 0) parts.Add("Win");
        return string.Join(",", parts);
    }
}
