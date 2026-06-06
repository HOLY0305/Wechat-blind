using WechatBlind.Win32;

namespace WechatBlind.UI;

/// <summary>
/// 磨砂遮罩窗口
/// 使用 DWM 亚克力模糊效果，透明度通过分层窗口属性控制
/// </summary>
internal sealed class OverlayForm : Form
{
    private byte _alpha;

    public OverlayForm()
    {
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        TopMost = true;
        BackColor = Color.Black;
        Opacity = 1.0;
        StartPosition = FormStartPosition.Manual;
        Visible = false;

        SetStyle(
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.UserPaint,
            true);
    }

    protected override CreateParams CreateParams
    {
        get
        {
            var cp = base.CreateParams;
            cp.ExStyle |= 0x00080000; // WS_EX_LAYERED - 分层窗口，支持透明
            cp.ExStyle |= 0x00000020; // WS_EX_TRANSPARENT - 鼠标穿透
            cp.ExStyle |= 0x00000080; // WS_EX_TOOLWINDOW - 不在 Alt+Tab 显示
            return cp;
        }
    }

    /// <summary>
    /// 设置遮罩透明度
    /// </summary>
    /// <param name="opacity">透明度 0.0-1.0</param>
    public void SetOpacity(double opacity)
    {
        _alpha = (byte)(opacity * 255);
        if (IsHandleCreated)
        {
            ApplyAlpha();
        }
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);

        if (_alpha == 0)
        {
            _alpha = 217; // 默认 85% 透明度
        }

        ApplyAlpha();
        DwmApi.EnableBlur(Handle, _alpha);
    }

    private void ApplyAlpha()
    {
        Win32Api.SetLayeredWindowAttributes(Handle, 0, _alpha, Win32Api.LWA_ALPHA);
    }

    /// <summary>
    /// 将遮罩放在指定窗口之上
    /// </summary>
    public void ShowAboveWindow(IntPtr targetHwnd)
    {
        if (!Visible)
        {
            Show();
        }

        Win32Api.ShowWindow(Handle, Win32Api.SW_SHOWNA);

        Win32Api.SetWindowPos(
            Handle,
            Win32Api.HWND_TOPMOST,
            0, 0, 0, 0,
            Win32Api.SWP_NOMOVE | Win32Api.SWP_NOSIZE |
            Win32Api.SWP_NOACTIVATE | Win32Api.SWP_SHOWWINDOW |
            Win32Api.SWP_FRAMECHANGED);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
    }
}
