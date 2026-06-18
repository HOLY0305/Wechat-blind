using System;
using System.Runtime.InteropServices;
using System.Text;

namespace WechatBlind.Tools;

/// <summary>
/// 用于检测微信窗口类名的工具
/// </summary>
internal static class FindWeChatWindow
{
    private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

    [DllImport("user32.dll")]
    private static extern bool IsWindowVisible(IntPtr hWnd);

    public static void Run()
    {
        Console.WriteLine("正在扫描所有窗口...");
        Console.WriteLine("=" .PadRight(80, '='));
        Console.WriteLine($"{"句柄",-12} {"类名",-35} {"窗口标题"}");
        Console.WriteLine("=".PadRight(80, '='));

        EnumWindows((hWnd, _) =>
        {
            if (!IsWindowVisible(hWnd))
                return true;

            var className = new StringBuilder(256);
            GetClassName(hWnd, className, 256);

            var windowTitle = new StringBuilder(256);
            GetWindowText(hWnd, windowTitle, 256);

            var name = className.ToString();
            var title = windowTitle.ToString();

            // 显示所有包含 "WeChat" 或 "微信" 的窗口
            if (name.Contains("WeChat", StringComparison.OrdinalIgnoreCase) ||
                name.Contains("微信", StringComparison.OrdinalIgnoreCase) ||
                title.Contains("WeChat", StringComparison.OrdinalIgnoreCase) ||
                title.Contains("微信", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine($"{hWnd,-12} {name,-35} {title}");
            }

            return true;
        }, IntPtr.Zero);

        Console.WriteLine("=".PadRight(80, '='));
        Console.WriteLine("扫描完成。请将包含 WeChat 的类名告诉我。");
    }
}
