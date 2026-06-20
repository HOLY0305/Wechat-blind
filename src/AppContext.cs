using System.Drawing.Imaging;
using System.IO;
using WechatBlind.Config;
using WechatBlind.Core;
using WechatBlind.UI;
using WechatBlind.Win32;

namespace WechatBlind;

/// <summary>
/// 应用上下文
/// </summary>
internal sealed class AppContext : ApplicationContext
{
    private readonly WindowDetector _detector;
    private readonly FocusMonitor _focusMonitor;
    private readonly OverlayManager _overlayManager;
    private readonly TrayManager _trayManager;
    private readonly SettingsManager _settingsManager;
    private readonly PatternManager _patternManager;
    private readonly HotkeyManager _hotkeyManager;
    private readonly System.Windows.Forms.Timer _wechatWatcher;
    private IntPtr _wechatHwnd;
    private bool _isEnabled;

    public AppContext(IntPtr wechatHwnd)
    {
        _wechatHwnd = wechatHwnd;

        _settingsManager = new SettingsManager();
        _patternManager = new PatternManager();
        var settings = _settingsManager.GetSettings();
        _isEnabled = settings.Enabled;

        _detector = new WindowDetector();
        _focusMonitor = new FocusMonitor(wechatHwnd);
        _overlayManager = new OverlayManager(_detector, wechatHwnd);
        _trayManager = new TrayManager();
        _hotkeyManager = new HotkeyManager();

        // 微信关闭时自动隐藏遮罩并等待重启
        _wechatWatcher = new System.Windows.Forms.Timer { Interval = 2000 };
        _wechatWatcher.Tick += OnWeChatWatcherTick;

        _overlayManager.WeChatUnavailable += OnWeChatUnavailable;
        _focusMonitor.FocusChanged += OnFocusChanged;
        _trayManager.ToggleEnabled += OnToggleEnabled;
        _trayManager.OpenSettings += OnOpenSettings;
        _trayManager.ExitApplication += OnExitApplication;
        _hotkeyManager.HotkeyPressed += OnHotkeyPressed;

        // 注册默认快捷键
        RegisterHotkey(settings.HotKey);

        // 应用开机自启设置
        AutoStartManager.SetAutoStart(settings.AutoStart);

        Start();
    }

    private void Start()
    {
        _trayManager.ShowBalloonTip(
            "微信幕布",
            _isEnabled ? "已启用 (Ctrl+Shift+W 切换)" : "已禁用 - 双击托盘图标启用",
            ToolTipIcon.Info);

        _trayManager.UpdateStatus(_isEnabled);

        if (_isEnabled)
        {
            _focusMonitor.Start();

            if (!_focusMonitor.IsFocused())
            {
                _overlayManager.Show();
            }
        }

        // 在 overlay 创建后加载图案、透明度和模糊设置
        var settings = _settingsManager.GetSettings();
        UpdateOverlayPattern(settings);
        _overlayManager.SetOverlayOpacity(settings.Opacity);
        _overlayManager.SetOverlayBlur(settings.BlurAmount);
    }

    private void OnFocusChanged(object? sender, bool focused)
    {
        if (!_isEnabled) return;

        if (focused)
        {
            _overlayManager.Hide();
        }
        else
        {
            _overlayManager.Show();
        }
    }

    private void OnHotkeyPressed(object? sender, EventArgs e)
    {
        OnToggleEnabled(sender, !_isEnabled);
    }

    private void OnWeChatUnavailable(object? sender, EventArgs e)
    {
        _focusMonitor.Stop();
        _wechatWatcher.Start();

        _trayManager.ShowBalloonTip(
            "微信幕布",
            "微信已关闭，遮罩已隐藏。等待微信重新启动...",
            ToolTipIcon.Info);
    }

    private void OnWeChatWatcherTick(object? sender, EventArgs e)
    {
        var hwnd = _detector.FindWeChatWindow();
        if (hwnd == IntPtr.Zero) return;

        _wechatWatcher.Stop();
        _wechatHwnd = hwnd;
        _focusMonitor.UpdateWindowHandle(hwnd);
        _overlayManager.UpdateWindowHandle(hwnd);

        if (_isEnabled)
        {
            _focusMonitor.Start();
            if (!_focusMonitor.IsFocused())
            {
                _overlayManager.Show();
            }
            var settings = _settingsManager.GetSettings();
            UpdateOverlayPattern(settings);
            _overlayManager.SetOverlayOpacity(settings.Opacity);
            _overlayManager.SetOverlayBlur(settings.BlurAmount);
        }

        _trayManager.ShowBalloonTip(
            "微信幕布",
            "微信已重新启动，遮罩已恢复。",
            ToolTipIcon.Info);
    }

    private void OnToggleEnabled(object? sender, bool enabled)
    {
        _isEnabled = enabled;

        var settings = _settingsManager.GetSettings();
        settings.Enabled = enabled;
        _settingsManager.SaveSettings(settings);

        _trayManager.UpdateStatus(enabled);

        if (enabled)
        {
            _focusMonitor.Start();
            if (!_focusMonitor.IsFocused())
            {
                _overlayManager.Show();
            }
            UpdateOverlayPattern(settings);
            _overlayManager.SetOverlayOpacity(settings.Opacity);
            _overlayManager.SetOverlayBlur(settings.BlurAmount);
        }
        else
        {
            _focusMonitor.Stop();
            _overlayManager.Hide();
        }
    }

    private void OnOpenSettings(object? sender, EventArgs e)
    {
        var settings = _settingsManager.GetSettings();

        var window = new SettingsWindow(settings);
        window.SettingsSaved += OnSettingsSaved;
        window.SettingsChanged += OnSettingsChanged;
        window.ShowDialog();
    }

    /// <summary>
    /// 设置实时变化时更新遮罩（用于实时预览）
    /// </summary>
    private void OnSettingsChanged(object? sender, AppSettings settings)
    {
        // 实时更新遮罩透明度
        _overlayManager.SetOverlayOpacity(settings.Opacity);

        // 实时更新遮罩模糊程度
        _overlayManager.SetOverlayBlur(settings.BlurAmount);

        // 实时更新遮罩图案
        UpdateOverlayPattern(settings);
    }

    private void OnSettingsSaved(object? sender, AppSettings settings)
    {
        _settingsManager.SaveSettings(settings);

        // 更新快捷键
        RegisterHotkey(settings.HotKey);

        // 更新开机自启
        AutoStartManager.SetAutoStart(settings.AutoStart);

        // 应用新的启用状态
        if (settings.Enabled != _isEnabled)
        {
            OnToggleEnabled(this, settings.Enabled);
        }

        // 应用新的透明度
        _overlayManager.SetOverlayOpacity(settings.Opacity);

        // 应用新的模糊程度
        _overlayManager.SetOverlayBlur(settings.BlurAmount);

        // 应用新的图案
        UpdateOverlayPattern(settings);
    }

    /// <summary>
    /// 更新遮罩图案
    /// </summary>
    private void UpdateOverlayPattern(AppSettings settings)
    {
        if (settings.IsGifPattern && settings.PatternType == "CustomGif"
            && !string.IsNullOrEmpty(settings.CustomPatternPath)
            && File.Exists(settings.CustomPatternPath))
        {
            try
            {
                var frames = ExtractGifFrames(settings.CustomPatternPath);
                var delays = PatternManager.GetGifFrameDelays(settings.CustomPatternPath);
                _overlayManager.SetOverlayGifPattern(frames, delays, settings.PatternOpacity);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load GIF pattern: {ex.Message}");
                _overlayManager.SetOverlayPattern(null, settings.PatternOpacity);
            }
            return;
        }

        Image? patternImage = null;

        if (settings.PatternType == "Preset"
            && Enum.TryParse<PatternManager.PresetPattern>(settings.PresetPattern, out var preset))
        {
            if (_patternManager.IsGifPreset(preset))
            {
                try
                {
                    var (frames, delays) = _patternManager.LoadGifPresetFrames(preset);
                    _overlayManager.SetOverlayGifPattern(frames, delays, settings.PatternOpacity);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to load GIF preset: {ex.Message}");
                    _overlayManager.SetOverlayPattern(null, settings.PatternOpacity);
                }
                return;
            }

            patternImage = _patternManager.LoadPattern(
                new PatternInfo { Type = PatternType.Preset, Preset = preset });
        }
        else if (settings.PatternType == "Custom" && !string.IsNullOrEmpty(settings.CustomPatternPath))
        {
            patternImage = _patternManager.LoadPattern(
                new PatternInfo { Type = PatternType.Custom, FilePath = settings.CustomPatternPath });
        }

        _overlayManager.SetOverlayPattern(patternImage, settings.PatternOpacity);
    }

    /// <summary>
    /// 从 GIF 文件提取帧图片数组
    /// </summary>
    private static Image[] ExtractGifFrames(string filePath)
    {
        var bytes = File.ReadAllBytes(filePath);
        using var ms = new MemoryStream(bytes);
        using var gifImage = Image.FromStream(ms);
        var frameCount = gifImage.GetFrameCount(FrameDimension.Time);
        var frames = new Image[frameCount];

        for (int i = 0; i < frameCount; i++)
        {
            gifImage.SelectActiveFrame(FrameDimension.Time, i);
            frames[i] = (Image)gifImage.Clone();
        }

        return frames;
    }

    private void RegisterHotkey(HotKeySettings hotkey)
    {
        uint modifiers = 0;
        if (hotkey.Modifiers.Contains("Control")) modifiers |= Win32Api.MOD_CONTROL;
        if (hotkey.Modifiers.Contains("Shift")) modifiers |= Win32Api.MOD_SHIFT;
        if (hotkey.Modifiers.Contains("Alt")) modifiers |= Win32Api.MOD_ALT;
        if (hotkey.Modifiers.Contains("Win")) modifiers |= Win32Api.MOD_WIN;

        if (Enum.TryParse<Keys>(hotkey.Key, out var key))
        {
            _hotkeyManager.Register(modifiers, (uint)key);
        }
    }

    private void OnExitApplication(object? sender, EventArgs e)
    {
        _hotkeyManager.Unregister();
        _focusMonitor.Stop();
        _overlayManager.Hide();
        ExitThread();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _wechatWatcher?.Stop();
            _wechatWatcher?.Dispose();
            _hotkeyManager?.Dispose();
            _focusMonitor?.Dispose();
            _overlayManager?.Dispose();
            _trayManager?.Dispose();
        }
        base.Dispose(disposing);
    }
}
