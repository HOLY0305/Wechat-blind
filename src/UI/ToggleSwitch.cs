namespace WechatBlind.UI;

/// <summary>
/// iOS 风格开关控件
/// </summary>
internal sealed class ToggleSwitch : Control
{
    private bool _checked;
    private float _thumbX;

    private const int ThumbRadius = 13;
    private const int TrackWidth = 65;
    private const int TrackHeight = 38;
    private const int ThumbTravel = TrackWidth - ThumbRadius * 2 - 6;

    public event EventHandler? CheckedChanged;

    public bool Checked
    {
        get => _checked;
        set
        {
            if (_checked == value) return;
            _checked = value;
            _thumbX = value ? ThumbTravel : 0;
            Invalidate();
            CheckedChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public ToggleSwitch()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
        Size = new Size(TrackWidth, TrackHeight);
        Cursor = Cursors.Hand;
        _thumbX = 0;
    }

    protected override void OnMouseClick(MouseEventArgs e)
    {
        base.OnMouseClick(e);
        Checked = !Checked;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

        int trackY = (Height - TrackHeight) / 2;
        var trackRect = new Rectangle(0, trackY, TrackWidth, TrackHeight);
        int trackRadius = TrackHeight / 2;

        using var trackPath = CreateRoundedRectPath(trackRect, trackRadius);
        using var trackBrush = new SolidBrush(_checked ? Color.FromArgb(52, 199, 89) : Color.FromArgb(224, 224, 224));
        g.FillPath(trackBrush, trackPath);

        int thumbCenterX = (int)(_thumbX + ThumbRadius + 2);
        int thumbCenterY = trackY + TrackHeight / 2;
        using var thumbBrush = new SolidBrush(Color.White);
        g.FillEllipse(thumbBrush, thumbCenterX - ThumbRadius, thumbCenterY - ThumbRadius, ThumbRadius * 2, ThumbRadius * 2);
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        _thumbX = _checked ? ThumbTravel : 0;
    }

    protected override void OnSizeChanged(EventArgs e)
    {
        base.OnSizeChanged(e);
        Width = TrackWidth;
        Height = TrackHeight;
    }

    private static System.Drawing.Drawing2D.GraphicsPath CreateRoundedRectPath(Rectangle rect, int radius)
    {
        var path = new System.Drawing.Drawing2D.GraphicsPath();
        int d = radius * 2;
        path.AddArc(rect.X, rect.Y, d, d, 180, 90);
        path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
        path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
        path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }
}
