using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace WindowDebugger;

class Program
{
    [DllImport("user32.dll", SetLastError = true)]
    static extern IntPtr FindWindow(string? lpClassName, string? lpWindowName);

    [DllImport("user32.dll", SetLastError = true)]
    static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [DllImport("user32.dll", SetLastError = true)]
    static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll", SetLastError = true)]
    static extern bool IsWindow(IntPtr hWnd);

    [DllImport("user32.dll", SetLastError = true)]
    static extern bool IsWindowVisible(IntPtr hWnd);

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    static extern int GetClassName(IntPtr hWnd, System.Text.StringBuilder lpClassName, int nMaxCount);

    [DllImport("user32.dll", SetLastError = true)]
    static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

    [DllImport("user32.dll", SetLastError = true)]
    static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll", SetLastError = true)]
    static extern bool BringWindowToTop(IntPtr hWnd);

    [DllImport("user32.dll", SetLastError = true)]
    static extern IntPtr GetWindow(IntPtr hWnd, uint uCmd);

    static readonly IntPtr HWND_TOP = new(0);
    static readonly IntPtr HWND_TOPMOST = new(-1);
    static readonly IntPtr HWND_NOTOPMOST = new(-2);
    const uint SWP_NOMOVE = 0x0002;
    const uint SWP_NOSIZE = 0x0001;
    const uint SWP_NOACTIVATE = 0x0010;
    const uint SWP_SHOWWINDOW = 0x0040;
    const uint SWP_FRAMECHANGED = 0x0020;
    const int SW_SHOWNA = 8;
    const uint GW_HWNDNEXT = 2;
    const uint GW_HWNDPREV = 3;

    [StructLayout(LayoutKind.Sequential)]
    struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    static void Main(string[] args)
    {
        Console.WriteLine("=== 微信窗口 Z-order 测试 ===\n");

        // 查找微信窗口
        string[] classNames = { "Qt51514QWindowIcon", "WeChatMainWndForPC", "WeChat" };
        IntPtr wechatHwnd = IntPtr.Zero;

        foreach (var className in classNames)
        {
            wechatHwnd = FindWindow(className, null);
            if (wechatHwnd != IntPtr.Zero)
            {
                Console.WriteLine($"[找到] 类名: {className}");
                break;
            }
        }

        if (wechatHwnd == IntPtr.Zero)
        {
            Console.WriteLine("[错误] 未找到微信窗口");
            return;
        }

        if (GetWindowRect(wechatHwnd, out var rect))
        {
            Console.WriteLine($"微信窗口位置: Left={rect.Left}, Top={rect.Top}, Right={rect.Right}, Bottom={rect.Bottom}");
            Console.WriteLine($"微信窗口大小: {rect.Right - rect.Left} x {rect.Bottom - rect.Top}");
        }

        // 创建测试窗口（模拟遮罩）
        Console.WriteLine("\n=== 创建测试遮罩窗口 ===");
        var testForm = new TestOverlayForm();
        testForm.Show();

        Console.WriteLine($"测试窗口句柄: 0x{testForm.Handle:X}");

        // 等待窗口显示
        System.Threading.Thread.Sleep(500);

        // 设置窗口位置和大小与微信窗口相同
        testForm.Location = new System.Drawing.Point(rect.Left, rect.Top);
        testForm.Size = new System.Drawing.Size(rect.Right - rect.Left, rect.Bottom - rect.Top);
        Console.WriteLine($"测试窗口位置: {testForm.Location}");
        Console.WriteLine($"测试窗口大小: {testForm.Size}");

        // 先隐藏再显示，确保窗口状态正确
        testForm.Hide();
        System.Threading.Thread.Sleep(100);
        testForm.Show();
        System.Threading.Thread.Sleep(100);

        // 测试多种方法
        Console.WriteLine("\n=== 测试 SetWindowPos 方法 ===");

        // 方法1: 使用 HWND_TOPMOST
        Console.WriteLine("\n方法1: HWND_TOPMOST");
        bool result1 = SetWindowPos(testForm.Handle, HWND_TOPMOST, 0, 0, 0, 0,
            SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE | SWP_SHOWWINDOW);
        Console.WriteLine($"  结果: {result1}, 错误码: {Marshal.GetLastWin32Error()}");

        // 显示测试窗口的 Z-order
        System.Threading.Thread.Sleep(100);
        IntPtr nextWindow = GetWindow(testForm.Handle, GW_HWNDNEXT);
        if (nextWindow != IntPtr.Zero)
        {
            var nextClassName = new System.Text.StringBuilder(256);
            GetClassName(nextWindow, nextClassName, 256);
            Console.WriteLine($"  下一个窗口: 0x{nextWindow:X} (类名: {nextClassName})");
            Console.WriteLine($"  下一个窗口是微信: {nextWindow == wechatHwnd}");
        }

        // 方法2: 先取消 TOPMOST 再设置
        Console.WriteLine("\n方法2: 先取消 TOPMOST 再设置");
        SetWindowPos(testForm.Handle, HWND_NOTOPMOST, 0, 0, 0, 0,
            SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);
        System.Threading.Thread.Sleep(100);

        bool result2 = SetWindowPos(testForm.Handle, HWND_TOPMOST, 0, 0, 0, 0,
            SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE | SWP_SHOWWINDOW | SWP_FRAMECHANGED);
        Console.WriteLine($"  结果: {result2}, 错误码: {Marshal.GetLastWin32Error()}");

        System.Threading.Thread.Sleep(100);
        nextWindow = GetWindow(testForm.Handle, GW_HWNDNEXT);
        if (nextWindow != IntPtr.Zero)
        {
            var nextClassName = new System.Text.StringBuilder(256);
            GetClassName(nextWindow, nextClassName, 256);
            Console.WriteLine($"  下一个窗口: 0x{nextWindow:X} (类名: {nextClassName})");
            Console.WriteLine($"  下一个窗口是微信: {nextWindow == wechatHwnd}");
        }

        // 方法3: 使用 BringWindowToTop
        Console.WriteLine("\n方法3: BringWindowToTop");
        bool result3 = BringWindowToTop(testForm.Handle);
        Console.WriteLine($"  结果: {result3}, 错误码: {Marshal.GetLastWin32Error()}");

        System.Threading.Thread.Sleep(100);
        nextWindow = GetWindow(testForm.Handle, GW_HWNDNEXT);
        if (nextWindow != IntPtr.Zero)
        {
            var nextClassName = new System.Text.StringBuilder(256);
            GetClassName(nextWindow, nextClassName, 256);
            Console.WriteLine($"  下一个窗口: 0x{nextWindow:X} (类名: {nextClassName})");
            Console.WriteLine($"  下一个窗口是微信: {nextWindow == wechatHwnd}");
        }

        Console.WriteLine("\n测试窗口已显示，5秒后自动退出...");
        System.Threading.Thread.Sleep(5000);

        testForm.Close();
    }
}

// 模拟遮罩窗口
class TestOverlayForm : System.Windows.Forms.Form
{
    public TestOverlayForm()
    {
        Text = "测试遮罩";
        FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
        ShowInTaskbar = false;
        TopMost = true;
        BackColor = System.Drawing.Color.White;
        Opacity = 0.85;
        StartPosition = System.Windows.Forms.FormStartPosition.Manual;

        // 设置窗口样式
        SetStyle(
            System.Windows.Forms.ControlStyles.OptimizedDoubleBuffer |
            System.Windows.Forms.ControlStyles.AllPaintingInWmPaint |
            System.Windows.Forms.ControlStyles.UserPaint,
            true);
    }

    protected override System.Windows.Forms.CreateParams CreateParams
    {
        get
        {
            var cp = base.CreateParams;
            cp.ExStyle |= 0x80000;  // WS_EX_LAYERED
            cp.ExStyle |= 0x20;     // WS_EX_TRANSPARENT - 鼠标穿透
            cp.ExStyle |= 0x80;     // WS_EX_TOOLWINDOW
            return cp;
        }
    }

    protected override void OnPaint(System.Windows.Forms.PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        using var brush = new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(200, 255, 255, 255));
        g.FillRectangle(brush, ClientRectangle);

        // 画一个 W 标志
        using var font = new System.Drawing.Font("Arial", 48, System.Drawing.FontStyle.Bold);
        using var textBrush = new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(180, 7, 193, 96));
        var textSize = g.MeasureString("W", font);
        g.DrawString("W", font, textBrush,
            (Width - textSize.Width) / 2,
            (Height - textSize.Height) / 2);
    }
}
