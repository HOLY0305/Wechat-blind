using System.Drawing.Drawing2D;

namespace WechatBlind.UI;

/// <summary>
/// 圆角面板控件
/// </summary>
internal sealed class RoundedPanel : Panel
{
    public int BorderRadius { get; set; } = 10;
    public Color BorderColor { get; set; } = Color.FromArgb(230, 230, 230);
    public int BorderWidth { get; set; } = 1;

    public RoundedPanel()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        var rect = new Rectangle(0, 0, Width - 1, Height - 1);
        using var path = CreateRoundedRectPath(rect, BorderRadius);

        g.SetClip(path);
        using var bgBrush = new SolidBrush(BackColor == Color.Transparent ? SystemColors.Window : BackColor);
        g.FillRectangle(bgBrush, ClientRectangle);
        g.ResetClip();

        using var borderPen = new Pen(BorderColor, BorderWidth);
        g.DrawPath(borderPen, path);
    }

    private static GraphicsPath CreateRoundedRectPath(Rectangle rect, int radius)
    {
        var path = new GraphicsPath();
        int d = radius * 2;
        path.AddArc(rect.X, rect.Y, d, d, 180, 90);
        path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
        path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
        path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }
}
