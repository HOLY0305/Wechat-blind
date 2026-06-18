using System;
using System.Runtime.InteropServices;
using System.Text;

class Program
{
    delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    [DllImport("user32.dll")]
    static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

    [DllImport("user32.dll")]
    static extern bool IsWindowVisible(IntPtr hWnd);

    [DllImport("user32.dll")]
    static extern bool IsIconic(IntPtr hWnd);

    static void Main()
    {
        Console.OutputEncoding = Encoding.UTF8;
        Console.WriteLine("正在扫描所有窗口...\n");
        Console.WriteLine(new string('=', 100));
        Console.WriteLine($"{"句柄",-14} {"类名",-40} {"窗口标题"}");
        Console.WriteLine(new string('=', 100));

        int count = 0;

        EnumWindows((hWnd, _) =>
        {
            // 不过滤，列出所有窗口

            var className = new StringBuilder(256);
            GetClassName(hWnd, className, 256);

            var windowTitle = new StringBuilder(256);
            GetWindowText(hWnd, windowTitle, 256);

            var name = className.ToString();
            var title = windowTitle.ToString();

            // 列出所有窗口，显示状态
            var visible = IsWindowVisible(hWnd);
            var iconic = IsIconic(hWnd);
            var status = !visible ? "隐藏" : iconic ? "最小化" : "可见";
            Console.WriteLine($"{hWnd,-14} {name,-40} {title,-30} [{status}]");
            count++;

            return true;
        }, IntPtr.Zero);

        Console.WriteLine(new string('=', 100));
        Console.WriteLine($"\n找到 {count} 个相关窗口。");

        if (count == 0)
        {
            Console.WriteLine("\n提示：请确保微信窗口已打开（不是最小化到托盘）。");
            Console.WriteLine("微信需要有可见的主窗口才能被检测到。");
        }

        Console.WriteLine("\n按任意键退出...");
        Console.ReadKey();
    }
}
