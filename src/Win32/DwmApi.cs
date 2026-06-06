using System.Runtime.InteropServices;

namespace WechatBlind.Win32;

/// <summary>
/// DWM (Desktop Window Manager) API 声明
/// 用于实现窗口模糊效果
/// </summary>
internal static class DwmApi
{
    private const string Dwmapi = "dwmapi.dll";

    /// <summary>
    /// DWM 属性枚举
    /// </summary>
    public enum DwmWindowAttribute : uint
    {
        /// <summary>启用非客户区渲染</summary>
        DWMWA_NCRENDERING_ENABLED = 1,
        /// <summary>非客户区渲染策略</summary>
        DWMWA_NCRENDERING_POLICY = 2,
        /// <summary>禁止非客户区过渡</summary>
        DWMWA_TRANSITIONS_FORCEDISABLED = 3,
        /// <summary>允许绘制呼吸</summary>
        DWMWA_ALLOW_NCPAINT = 4,
        /// <summary>启用卡片点击穿透</summary>
        DWMWA_CAPTION_BUTTON_BOUNDS = 5,
        /// <summary>启用非客户端区右键菜单</summary>
        DWMWA_NONCLIENT_RTL_LAYOUT = 6,
        /// <summary>强制可见</summary>
        DWMWA_FORCE_VISIBLE_REDUCTION = 7,
        /// <summary>深色模式（Windows 10 1903+）</summary>
        DWMWA_USE_IMMERSIVE_DARK_MODE = 20,
        /// <summary>窗口圆角</summary>
        DWMWA_WINDOW_CORNER_PREFERENCE = 33,
        /// <summary>边框颜色</summary>
        DWMWA_BORDER_COLOR = 34,
        /// <summary>标题栏文本颜色</summary>
        DWMWA_CAPTION_TEXT_COLOR = 35,
        /// <summary>标题栏强调色</summary>
        DWMWA_TEXT_COLOR = 36,
        /// <summary>窗口背景材质（Windows 11）</summary>
        DWMWA_SYSTEMBACKDROP_TYPE = 38,
    }

    /// <summary>
    /// 窗口背景材质类型
    /// </summary>
    public enum SystemBackdropType : uint
    {
        /// <summary>默认</summary>
        DWMSBT_AUTO = 0,
        /// <summary>无材质</summary>
        DWMSBT_NONE = 1,
        /// <summary>主窗口材质（Mica）</summary>
        DWMSBT_MAINWINDOW = 2,
        /// <summary>亚克力材质</summary>
        DWMSBT_TRANSIENTWINDOW = 3,
        /// <summary>模糊折叠材质</summary>
        DWMSBT_TABBEDWINDOW = 4,
    }

    /// <summary>
    /// 窗口组合策略
    /// </summary>
    public enum AccentState
    {
        ACCENT_DISABLED = 0,
        ACCENT_ENABLE_GRADIENT = 1,
        ACCENT_ENABLE_TRANSPARENTGRADIENT = 2,
        ACCENT_ENABLE_BLURBEHIND = 3,
        ACCENT_ENABLE_ACRYLICBLURBEHIND = 4,
        ACCENT_INVALID_STATE = 5,
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct AccentPolicy
    {
        public AccentState AccentState;
        public int AccentFlags;
        public int GradientColor;
        public int AnimationId;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct WindowCompositionAttributeData
    {
        public WindowCompositionAttribute Attribute;
        public IntPtr Data;
        public int SizeOfData;
    }

    public enum WindowCompositionAttribute
    {
        WCA_ACCENT_POLICY = 19,
    }

    [DllImport(Dwmapi, SetLastError = true)]
    public static extern int DwmSetWindowAttribute(
        IntPtr hwnd,
        DwmWindowAttribute dwAttribute,
        ref int pvAttribute,
        uint cbAttribute);

    [DllImport("user32.dll")]
    public static extern int SetWindowCompositionAttribute(
        IntPtr hwnd,
        ref WindowCompositionAttributeData data);

    /// <summary>
    /// 为窗口启用亚克力模糊效果
    /// </summary>
    /// <param name="hwnd">窗口句柄</param>
    /// <param name="opacity">透明度（0-255）</param>
    /// <param name="blurAmount">模糊程度（0-100）</param>
    /// <returns>是否成功</returns>
    public static bool EnableBlur(IntPtr hwnd, int opacity = 180, int blurAmount = 50)
    {
        // 将模糊程度转换为 GradientColor（0-100 映射到 0-255）
        int gradientIntensity = (int)(blurAmount * 2.55);
        int gradientColor = (opacity << 24) | (gradientIntensity << 16) | (gradientIntensity << 8) | gradientIntensity;

        var accent = new AccentPolicy
        {
            AccentState = AccentState.ACCENT_ENABLE_ACRYLICBLURBEHIND,
            GradientColor = gradientColor,
        };

        var accentPtr = Marshal.AllocHGlobal(Marshal.SizeOf(accent));
        Marshal.StructureToPtr(accent, accentPtr, false);

        var data = new WindowCompositionAttributeData
        {
            Attribute = WindowCompositionAttribute.WCA_ACCENT_POLICY,
            SizeOfData = Marshal.SizeOf(accent),
            Data = accentPtr,
        };

        try
        {
            return SetWindowCompositionAttribute(hwnd, ref data) == 0;
        }
        finally
        {
            Marshal.FreeHGlobal(accentPtr);
        }
    }
}
