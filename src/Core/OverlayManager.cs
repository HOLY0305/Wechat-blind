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
