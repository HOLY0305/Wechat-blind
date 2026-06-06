using WechatBlind.Config;
using WechatBlind.Win32;

namespace WechatBlind.UI;

/// <summary>
/// 设置面板
/// </summary>
internal sealed class SettingsForm : Form
{
    private readonly AppSettings _settings;
    private readonly CheckBox _chkEnabled;
    private readonly TrackBar _trkOpacity;
    private readonly Label _lblOpacityValue;
    private readonly Button _btnHotkey;
    private readonly CheckBox _chkAutoStart;
    private bool _recordingHotkey;
    private uint _pendingModifiers;
    private uint _pendingVk;

    /// <summary>
    /// 设置保存后触发
    /// </summary>
    public event EventHandler<AppSettings>? SettingsSaved;

    public SettingsForm(AppSettings settings)
    {
        _settings = settings;

        Text = "微信幕布 - 设置";
        Size = new Size(380, 320);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterScreen;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;

        // 启用/禁用
        _chkEnabled = new CheckBox
        {
            Text = "启用遮罩",
            Checked = settings.Enabled,
            Location = new Point(20, 20),
            AutoSize = true,
        };

        // 透明度
        var lblOpacity = new Label
        {
            Text = "遮罩透明度：",
            Location = new Point(20, 60),
            AutoSize = true,
        };

        _trkOpacity = new TrackBar
        {
            Minimum = 0,
            Maximum = 100,
            Value = (int)(settings.Opacity * 100),
            TickFrequency = 10,
            LargeChange = 10,
            Location = new Point(120, 55),
            Width = 180,
        };

        _lblOpacityValue = new Label
        {
            Text = $"{_trkOpacity.Value}%",
            Location = new Point(310, 60),
            AutoSize = true,
        };

        _trkOpacity.ValueChanged += (s, e) =>
        {
            _lblOpacityValue.Text = $"{_trkOpacity.Value}%";
        };

        // 快捷键
        var lblHotkey = new Label
        {
            Text = "切换快捷键：",
            Location = new Point(20, 105),
            AutoSize = true,
        };

        _btnHotkey = new Button
        {
            Text = FormatHotkey(settings.HotKey),
            Location = new Point(120, 100),
            Width = 210,
            Height = 28,
        };

        _btnHotkey.Click += OnHotkeyButtonClick;

        // 开机自启
        _chkAutoStart = new CheckBox
        {
            Text = "开机自动启动",
            Checked = settings.AutoStart,
            Location = new Point(20, 145),
            AutoSize = true,
        };

        // 按钮
        var btnOk = new Button
        {
            Text = "确定",
            DialogResult = DialogResult.OK,
            Location = new Point(160, 240),
            Width = 90,
        };

        var btnCancel = new Button
        {
            Text = "取消",
            DialogResult = DialogResult.Cancel,
            Location = new Point(260, 240),
            Width = 90,
        };

        btnOk.Click += OnSave;

        Controls.AddRange(new Control[]
        {
            _chkEnabled, lblOpacity, _trkOpacity, _lblOpacityValue,
            lblHotkey, _btnHotkey, _chkAutoStart,
            btnOk, btnCancel,
        });

        AcceptButton = btnOk;
        CancelButton = btnCancel;
    }

    private void OnHotkeyButtonClick(object? sender, EventArgs e)
    {
        if (_recordingHotkey)
        {
            return;
        }

        _recordingHotkey = true;
        _btnHotkey.Text = "请按下快捷键...";
        _btnHotkey.Focus();

        // 使用 KeyDown 捕获按键组合
        PreviewKeyDown += OnPreviewKeyDown;
    }

    private void OnPreviewKeyDown(object? sender, PreviewKeyDownEventArgs e)
    {
        if (!_recordingHotkey) return;

        // 忽略纯修饰键
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

        var display = FormatHotkey(new HotKeySettings
        {
            Modifiers = ModifiersToString(_pendingModifiers),
            Key = e.KeyCode.ToString(),
        });
        _btnHotkey.Text = display;
    }

    private void OnSave(object? sender, EventArgs e)
    {
        _settings.Enabled = _chkEnabled.Checked;
        _settings.Opacity = _trkOpacity.Value / 100.0;
        _settings.AutoStart = _chkAutoStart.Checked;

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
