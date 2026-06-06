using WechatBlind.Config;
using WechatBlind.Core;
using WechatBlind.UI;

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
    private readonly System.Windows.Forms.Timer _wechatWatcher;
    private IntPtr _wechatHwnd;
    private bool _isEnabled;

    public AppContext(IntPtr wechatHwnd)
    {
        _wechatHwnd = wechatHwnd;

        _settingsManager = new SettingsManager();
        var settings = _settingsManager.GetSettings();
        _isEnabled = settings.Enabled;

        _detector = new WindowDetector();
        _focusMonitor = new FocusMonitor(wechatHwnd);
        _overlayManager = new OverlayManager(_detector, wechatHwnd);
        _trayManager = new TrayManager();

        // 微信关闭时自动隐藏遮罩并等待重启
        _wechatWatcher = new System.Windows.Forms.Timer { Interval = 2000 };
        _wechatWatcher.Tick += OnWeChatWatcherTick;

        _overlayManager.WeChatUnavailable += OnWeChatUnavailable;
        _focusMonitor.FocusChanged += OnFocusChanged;
        _trayManager.ToggleEnabled += OnToggleEnabled;
        _trayManager.OpenSettings += OnOpenSettings;
        _trayManager.ExitApplication += OnExitApplication;

        Start();
    }

    private void Start()
    {
        _trayManager.ShowBalloonTip(
            "微信幕布",
            _isEnabled ? "已启用" : "已禁用 - 双击托盘图标启用",
            ToolTipIcon.Info);

        _trayManager.UpdateStatus(_isEnabled);

        if (_isEnabled)
        {
            _focusMonitor.Start();

            // 启动时如果微信不在前台，显示遮罩
            if (!_focusMonitor.IsFocused())
            {
                _overlayManager.Show();
            }
        }
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

        // 微信重新启动了
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
        }
        else
        {
            _focusMonitor.Stop();
            _overlayManager.Hide();
        }
    }

    private void OnOpenSettings(object? sender, EventArgs e)
    {
        _trayManager.ShowBalloonTip("设置", "设置功能开发中...", ToolTipIcon.Info);
    }

    private void OnExitApplication(object? sender, EventArgs e)
    {
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
            _focusMonitor?.Dispose();
            _overlayManager?.Dispose();
            _trayManager?.Dispose();
        }
        base.Dispose(disposing);
    }
}
