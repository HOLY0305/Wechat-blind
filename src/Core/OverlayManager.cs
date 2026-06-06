using System.Drawing;
using WechatBlind.UI;
using WechatBlind.Win32;

namespace WechatBlind.Core;

/// <summary>
/// 遮罩管理器
/// 负责遮罩窗口的创建、位置同步、显示/隐藏
/// </summary>
internal sealed class OverlayManager : IDisposable
{
    private readonly WindowDetector _detector;
    private readonly System.Windows.Forms.Timer _syncTimer;
    private OverlayForm? _overlayForm;
    private IntPtr _wechatHwnd;
    private Rectangle _lastWeChatRect;
    private bool _disposed;

    public bool IsShowing => _overlayForm?.Visible == true;

    /// <summary>
    /// 微信窗口关闭/不可用时触发
    /// </summary>
    public event EventHandler? WeChatUnavailable;

    public OverlayManager(WindowDetector detector, IntPtr wechatHwnd)
    {
        _detector = detector;
        _wechatHwnd = wechatHwnd;
        _lastWeChatRect = Rectangle.Empty;

        _syncTimer = new System.Windows.Forms.Timer { Interval = 100 };
        _syncTimer.Tick += OnSyncTick;
    }

    /// <summary>
    /// 显示遮罩
    /// </summary>
    public void Show()
    {
        if (_disposed) return;

        if (!EnsureWeChatValid()) return;
        if (!EnsureWeChatVisible()) return;

        // 微信被其他窗口遮挡时不显示
        if (IsWeChatCovered()) return;

        var rect = _detector.GetWindowPosition(_wechatHwnd);
        if (rect == Rectangle.Empty) return;

        EnsureOverlayCreated();

        SyncOverlayPosition(rect);
        _overlayForm!.ShowAboveWindow(_wechatHwnd);

        if (!_syncTimer.Enabled)
        {
            _syncTimer.Start();
        }
    }

    /// <summary>
    /// 设置遮罩透明度
    /// </summary>
    public void SetOverlayOpacity(double opacity)
    {
        if (_overlayForm != null && !_overlayForm.IsDisposed)
        {
            _overlayForm.SetOpacity(opacity);
        }
    }

    /// <summary>
    /// 隐藏遮罩
    /// </summary>
    public void Hide()
    {
        _syncTimer.Stop();

        if (_overlayForm != null && _overlayForm.Visible)
        {
            _overlayForm.Hide();
        }
    }

    /// <summary>
    /// 更新微信窗口句柄（微信重启后调用）
    /// </summary>
    public void UpdateWindowHandle(IntPtr hwnd)
    {
        _wechatHwnd = hwnd;
        _lastWeChatRect = Rectangle.Empty;
    }

    private void OnSyncTick(object? sender, EventArgs e)
    {
        if (_disposed) return;

        // 微信窗口已关闭
        if (!Win32Api.IsWindow(_wechatHwnd))
        {
            Hide();
            WeChatUnavailable?.Invoke(this, EventArgs.Empty);
            return;
        }

        // 微信最小化时隐藏遮罩
        if (Win32Api.IsIconic(_wechatHwnd))
        {
            if (_overlayForm?.Visible == true)
            {
                _overlayForm.Hide();
            }
            return;
        }

        // 微信不可见时隐藏遮罩
        if (!Win32Api.IsWindowVisible(_wechatHwnd))
        {
            if (_overlayForm?.Visible == true)
            {
                _overlayForm.Hide();
            }
            return;
        }

        // 鼠标悬停在微信窗口上方时隐藏遮罩
        if (IsMouseOverWeChat())
        {
            if (_overlayForm?.Visible == true)
            {
                _overlayForm.Hide();
            }
            return;
        }

        // 微信窗口被其他窗口遮挡时隐藏遮罩
        if (IsWeChatCovered())
        {
            if (_overlayForm?.Visible == true)
            {
                _overlayForm.Hide();
            }
            return;
        }

        // 同步位置
        var rect = _detector.GetWindowPosition(_wechatHwnd);
        if (rect == Rectangle.Empty) return;

        if (rect != _lastWeChatRect)
        {
            SyncOverlayPosition(rect);

            // 确保遮罩在最顶层
            if (_overlayForm?.Visible == true)
            {
                Win32Api.SetWindowPos(
                    _overlayForm.Handle,
                    Win32Api.HWND_TOPMOST,
                    0, 0, 0, 0,
                    Win32Api.SWP_NOMOVE | Win32Api.SWP_NOSIZE |
                    Win32Api.SWP_NOACTIVATE | Win32Api.SWP_SHOWWINDOW);
            }
        }

        // 微信恢复显示时，确保遮罩也显示
        if (_overlayForm?.Visible != true && !Win32Api.IsIconic(_wechatHwnd))
        {
            _overlayForm?.ShowAboveWindow(_wechatHwnd);
        }
    }

    /// <summary>
    /// 检测鼠标是否在微信窗口区域内
    /// </summary>
    private bool IsMouseOverWeChat()
    {
        if (!Win32Api.GetCursorPos(out var cursorPos)) return false;

        var rect = _detector.GetWindowPosition(_wechatHwnd);
        if (rect == Rectangle.Empty) return false;

        return cursorPos.X >= rect.X &&
               cursorPos.X <= rect.X + rect.Width &&
               cursorPos.Y >= rect.Y &&
               cursorPos.Y <= rect.Y + rect.Height;
    }

    /// <summary>
    /// 检测微信窗口中心点是否被其他窗口遮挡
    /// </summary>
    private bool IsWeChatCovered()
    {
        var rect = _detector.GetWindowPosition(_wechatHwnd);
        if (rect == Rectangle.Empty) return true;

        var centerPoint = new Win32Api.POINT
        {
            X = rect.X + rect.Width / 2,
            Y = rect.Y + rect.Height / 2,
        };

        var windowAtCenter = Win32Api.WindowFromPoint(centerPoint);

        // 中心点处没有窗口，或就是微信本身 → 未被遮挡
        if (windowAtCenter == IntPtr.Zero || windowAtCenter == _wechatHwnd)
        {
            return false;
        }

        // 中心点处的窗口是遮罩本身 → 未被遮挡
        if (_overlayForm != null && windowAtCenter == _overlayForm.Handle)
        {
            return false;
        }

        return true;
    }

    private bool EnsureWeChatValid()
    {
        if (Win32Api.IsWindow(_wechatHwnd))
        {
            return true;
        }

        _wechatHwnd = _detector.FindWeChatWindow();
        return _wechatHwnd != IntPtr.Zero;
    }

    private bool EnsureWeChatVisible()
    {
        return _detector.IsWindowVisible(_wechatHwnd);
    }

    private void EnsureOverlayCreated()
    {
        if (_overlayForm == null || _overlayForm.IsDisposed)
        {
            _overlayForm = new OverlayForm();
        }
    }

    private void SyncOverlayPosition(Rectangle rect)
    {
        if (_overlayForm == null) return;

        _overlayForm.Location = new Point(rect.X, rect.Y);
        _overlayForm.Size = new Size(rect.Width, rect.Height);
        _lastWeChatRect = rect;
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _syncTimer.Stop();
            _syncTimer.Dispose();

            if (_overlayForm != null)
            {
                _overlayForm.Hide();
                _overlayForm.Close();
                _overlayForm.Dispose();
                _overlayForm = null;
            }
            _disposed = true;
        }
    }
}
