using WechatBlind.Win32;

namespace WechatBlind.Core;

/// <summary>
/// 焦点状态监控器
/// 负责检测微信窗口的焦点状态变化
/// </summary>
internal sealed class FocusMonitor : IDisposable
{
    /// <summary>
    /// 焦点状态变化事件
    /// true = 微信获得焦点, false = 微信失去焦点
    /// </summary>
    public event EventHandler<bool>? FocusChanged;

    /// <summary>
    /// 窗口句柄变化事件
    /// </summary>
    public event EventHandler<IntPtr>? WindowHandleChanged;

    private readonly System.Windows.Forms.Timer _timer;
    private IntPtr _wechatHwnd;

    /// <summary>
    /// 初始值设为 true，确保启动时第一次检测会触发状态变化
    /// </summary>
    private bool _isWeChatFocused = true;
    private bool _disposed;

    public FocusMonitor(IntPtr wechatHwnd, int checkIntervalMs = 50)
    {
        _wechatHwnd = wechatHwnd;
        _timer = new System.Windows.Forms.Timer { Interval = checkIntervalMs };
        _timer.Tick += OnTimerTick;
    }

    public void Start()
    {
        if (!_disposed)
        {
            _timer.Start();
        }
    }

    public void Stop()
    {
        _timer.Stop();
    }

    public void UpdateWindowHandle(IntPtr hwnd)
    {
        if (_wechatHwnd != hwnd)
        {
            _wechatHwnd = hwnd;
            WindowHandleChanged?.Invoke(this, hwnd);
        }
    }

    public bool IsFocused()
    {
        // 直接检查当前前台窗口，不依赖缓存状态
        var foregroundWindow = Win32Api.GetForegroundWindow();
        return foregroundWindow == _wechatHwnd;
    }

    private void OnTimerTick(object? sender, EventArgs e)
    {
        // 窗口无效时重置状态
        if (!Win32Api.IsWindow(_wechatHwnd))
        {
            if (_isWeChatFocused)
            {
                _isWeChatFocused = false;
                FocusChanged?.Invoke(this, false);
            }
            return;
        }

        // 检测前台窗口
        var foregroundWindow = Win32Api.GetForegroundWindow();
        var focused = foregroundWindow == _wechatHwnd;

        // 只在状态变化时触发
        if (focused != _isWeChatFocused)
        {
            _isWeChatFocused = focused;
            FocusChanged?.Invoke(this, focused);
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _timer.Stop();
            _timer.Dispose();
            _disposed = true;
        }
    }
}
