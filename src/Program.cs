using WechatBlind.Config;
using WechatBlind.Core;
using WechatBlind.UI;

namespace WechatBlind;

/// <summary>
/// 微信幕布 - 微信窗口隐私保护工具
/// 当微信窗口失去焦点时自动显示磨砂遮罩
/// </summary>
internal static class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        // 启用高 DPI 支持
        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        // 测试模式：直接打开设置窗口（无需微信运行）
        if (args.Length > 0 && args[0] == "--test-settings")
        {
            try
            {
                var settings = new SettingsManager().GetSettings();
                var window = new SettingsWindow(settings);
                window.SettingsSaved += (s, e) => MessageBox.Show("设置已保存！");
                window.ShowDialog();
            }
            catch (Exception ex)
            {
                var msg = $"异常: {ex.Message}\n{ex.StackTrace}";
                if (ex.InnerException != null)
                    msg += $"\n\n内部异常: {ex.InnerException.Message}\n{ex.InnerException.StackTrace}";
                MessageBox.Show(msg, "异常", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            return;
        }

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
