using System.Diagnostics;
using System.Drawing;
using System.Text;
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
        "Tauri Window",         // 最新版微信（Tauri 框架）
        "Qt51514QWindowIcon",   // 微信（Qt 框架）
        "WeChatMainWndForPC",   // 旧版微信
        "WeChat",
    };

    /// <summary>
    /// 微信进程名关键词
    /// </summary>
    private static readonly string[] WeChatProcessNames = new[]
    {
        "WeChat",
        "WeChatAppEx",
        "wechat",
    };

    /// <summary>
    /// 微信窗口标题关键词（中文版）
    /// </summary>
    private const string WeChatTitleKeyword = "微信";

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
        // 优先通过类名查找（精确匹配），但跳过最小化窗口
        // Tauri 等框架会创建多个同名窗口，其中最小化的是辅助窗口
        IntPtr candidate = IntPtr.Zero;
        foreach (var className in WeChatClassNames)
        {
            var hwnd = Win32Api.FindWindow(className, null);

            if (hwnd != IntPtr.Zero && ValidateWindow(hwnd) && !Win32Api.IsIconic(hwnd))
            {
                candidate = hwnd;
                break;
            }
        }

        if (candidate != IntPtr.Zero)
        {
            return candidate;
        }

        // 降级：通过进程名枚举所有可见窗口（支持新版 Tauri / Electron 微信）
        return FindWeChatWindowByProcess();
    }

    /// <summary>
    /// 通过进程名或窗口标题枚举查找微信窗口
    /// </summary>
    private static IntPtr FindWeChatWindowByProcess()
    {
        IntPtr bestHwnd = IntPtr.Zero;
        int bestArea = 0;

        Win32Api.EnumWindows((hWnd, _) =>
        {
            if (!Win32Api.IsWindowVisible(hWnd)) return true;

            bool isWeChat = false;

            // 方式1：通过进程名匹配
            Win32Api.GetWindowThreadProcessId(hWnd, out uint pid);
            try
            {
                var proc = Process.GetProcessById((int)pid);
                foreach (var name in WeChatProcessNames)
                {
                    if (proc.ProcessName.Contains(name, StringComparison.OrdinalIgnoreCase))
                    {
                        isWeChat = true;
                        break;
                    }
                }
            }
            catch { }

            // 方式2：通过窗口标题匹配 "微信"
            if (!isWeChat)
            {
                var titleBuf = new StringBuilder(256);
                Win32Api.GetWindowText(hWnd, titleBuf, 256);
                if (titleBuf.ToString().Contains(WeChatTitleKeyword))
                {
                    isWeChat = true;
                }
            }

            if (!isWeChat) return true;

            // 获取窗口尺寸，选最大的那个（主窗口）
            if (!Win32Api.GetWindowRect(hWnd, out var rect)) return true;
            int area = (rect.Right - rect.Left) * (rect.Bottom - rect.Top);
            if (area > bestArea)
            {
                bestArea = area;
                bestHwnd = hWnd;
            }

            return true;
        }, IntPtr.Zero);

        return bestHwnd;
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
