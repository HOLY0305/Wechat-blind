using Microsoft.Win32;

namespace WechatBlind.Config;

/// <summary>
/// 开机自启管理器
/// 通过注册表 HKCU 实现开机自启
/// </summary>
internal static class AutoStartManager
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string AppName = "WechatBlind";

    /// <summary>
    /// 设置开机自启状态
    /// </summary>
    /// <param name="enabled">是否启用</param>
    public static void SetAutoStart(bool enabled)
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, true);
        if (key == null) return;

        if (enabled)
        {
            var exePath = Environment.ProcessPath ?? "";
            key.SetValue(AppName, $"\"{exePath}\"");
        }
        else
        {
            key.DeleteValue(AppName, false);
        }
    }

    /// <summary>
    /// 检查是否已设置开机自启
    /// </summary>
    /// <returns>是否已启用</returns>
    public static bool IsAutoStartEnabled()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, false);
        if (key == null) return false;

        var value = key.GetValue(AppName);
        return value != null;
    }
}
