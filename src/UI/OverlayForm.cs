using WechatBlind.Win32;

namespace WechatBlind.UI;

/// <summary>
/// 磨砂遮罩窗口
/// 使用 DWM 亚克力模糊效果，透明度通过分层窗口属性控制
/// </summary>
internal sealed class OverlayForm : Form
{
    private byte _alpha;
    private int _blurAmount = 50;
    private Image? _patternImage;
    private double _patternOpacity = 1.0;

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

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        ApplyRoundedCornerPreference();
    }

    private void ApplyRoundedCornerPreference()
    {
        // DWMWA_WINDOW_CORNER_PREFERENCE = 33
        // DWMWCP_ROUND = 2
        int preference = 2;
        DwmApi.DwmSetWindowAttribute(
            Handle,
            DwmApi.DwmWindowAttribute.DWMWA_WINDOW_CORNER_PREFERENCE,
            ref preference,
            sizeof(int));
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

    /// <summary>
    /// 设置模糊程度
    /// </summary>
    /// <param name="blurAmount">模糊程度 0-100</param>
    public void SetBlurAmount(int blurAmount)
    {
        _blurAmount = blurAmount;
        if (IsHandleCreated)
        {
            ApplyBlur();
        }
    }

    /// <summary>
    /// 设置遮罩图案
    /// </summary>
    /// <param name="patternImage">图案图片，null 表示无图案</param>
    /// <param name="patternOpacity">图案透明度 0.0-1.0</param>
    public void SetPattern(Image? patternImage, double patternOpacity = 1.0)
    {
        _patternImage = patternImage;
        _patternOpacity = patternOpacity;

        if (IsHandleCreated && Visible)
        {
            Invalidate();
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
        ApplyBlur();
    }

    private void ApplyAlpha()
    {
        Win32Api.SetLayeredWindowAttributes(Handle, 0, _alpha, Win32Api.LWA_ALPHA);
    }

    private void ApplyBlur()
    {
        DwmApi.EnableBlur(Handle, _alpha, _blurAmount);
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
        if (disposing)
        {
            _patternImage?.Dispose();
            _patternImage = null;
        }
        base.Dispose(disposing);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        if (_patternImage == null) return;

        // 使用图案透明度
        var matrix = new System.Drawing.Imaging.ColorMatrix
        {
            Matrix33 = (float)_patternOpacity,
        };

        using var attributes = new System.Drawing.Imaging.ImageAttributes();
        attributes.SetColorMatrix(matrix);

        // 绘制图案（平铺）
        var imageRect = new Rectangle(0, 0, _patternImage.Width, _patternImage.Height);
        e.Graphics.DrawImage(
            _patternImage,
            new Rectangle(0, 0, Width, Height),
            0, 0,
            _patternImage.Width,
            _patternImage.Height,
            GraphicsUnit.Pixel,
            attributes);
    }
}
