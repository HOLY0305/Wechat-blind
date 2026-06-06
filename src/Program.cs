using WechatBlind.Core;

namespace WechatBlind;

/// <summary>
/// 微信幕布 - 微信窗口隐私保护工具
/// 当微信窗口失去焦点时自动显示磨砂遮罩
/// </summary>
internal static class Program
{
    /// <summary>
    /// 应用程序入口点
    /// </summary>
    [STAThread]
    static void Main()
    {
        // 启用高 DPI 支持
        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        // 检测微信窗口
        var detector = new WindowDetector();
        var wechatHwnd = detector.FindWeChatWindow();

        if (wechatHwnd == IntPtr.Zero)
        {
            MessageBox.Show(
                "未检测到微信窗口。\n\n请先启动微信，然后重新运行本程序。",
                "微信幕布",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        // 启动应用
        using var app = new AppContext(wechatHwnd);
        Application.Run(app);
    }
}
