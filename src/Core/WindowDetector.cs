using System.Drawing;
using WechatBlind.Win32;

namespace WechatBlind.Core;

/// <summary>
/// 微信窗口检测器
/// 负责查找微信窗口并获取其位置信息
/// </summary>
internal sealed class WindowDetector
{
    /// <summary>
    /// 微信窗口类名列表（可能随版本变化）
    /// </summary>
    private static readonly string[] WeChatClassNames = new[]
    {
        "Qt51514QWindowIcon",   // 新版微信（Qt 框架）
        "WeChatMainWndForPC",   // 旧版微信
        "WeChat",
    };

    /// <summary>
    /// 上次检测到的窗口位置（用于变化检测）
    /// </summary>
    private Rectangle _lastRect = Rectangle.Empty;

    /// <summary>
    /// 查找微信窗口
    /// </summary>
    /// <returns>窗口句柄，未找到返回 IntPtr.Zero</returns>
    public IntPtr FindWeChatWindow()
    {
        foreach (var className in WeChatClassNames)
        {
            var hwnd = Win32Api.FindWindow(className, null);

            if (hwnd != IntPtr.Zero && ValidateWindow(hwnd))
            {
                return hwnd;
            }
        }

        return IntPtr.Zero;
    }

    /// <summary>
    /// 验证窗口是否有效
    /// </summary>
    /// <param name="hwnd">窗口句柄</param>
    /// <returns>窗口是否有效</returns>
    public bool ValidateWindow(IntPtr hwnd)
    {
        if (hwnd == IntPtr.Zero)
        {
            return false;
        }

        // 检查窗口是否有效
        if (!Win32Api.IsWindow(hwnd))
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// 检查窗口是否可见（非最小化）
    /// </summary>
    /// <param name="hwnd">窗口句柄</param>
    /// <returns>窗口是否可见</returns>
    public bool IsWindowVisible(IntPtr hwnd)
    {
        if (hwnd == IntPtr.Zero)
        {
            return false;
        }

        return Win32Api.IsWindowVisible(hwnd) && !Win32Api.IsIconic(hwnd);
    }

    /// <summary>
    /// 获取窗口位置和大小
    /// </summary>
    /// <param name="hwnd">窗口句柄</param>
    /// <returns>窗口矩形，失败返回 Rectangle.Empty</returns>
    public Rectangle GetWindowPosition(IntPtr hwnd)
    {
        if (!Win32Api.GetWindowRect(hwnd, out var rect))
        {
            return Rectangle.Empty;
        }

        return new Rectangle(
            rect.Left,
            rect.Top,
            rect.Right - rect.Left,
            rect.Bottom - rect.Top
        );
    }

    /// <summary>
    /// 检查窗口位置是否发生变化
    /// </summary>
    /// <param name="hwnd">窗口句柄</param>
    /// <returns>是否发生变化</returns>
    public bool HasPositionChanged(IntPtr hwnd)
    {
        var currentRect = GetWindowPosition(hwnd);

        if (currentRect == Rectangle.Empty)
        {
            return false;
        }

        bool changed = currentRect != _lastRect;
        _lastRect = currentRect;

        return changed;
    }

    /// <summary>
    /// 获取上次检测到的窗口位置
    /// </summary>
    /// <returns>上次的窗口矩形</returns>
    public Rectangle GetLastKnownPosition()
    {
        return _lastRect;
    }
}
