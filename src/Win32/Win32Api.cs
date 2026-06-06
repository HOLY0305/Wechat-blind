using System.Runtime.InteropServices;

namespace WechatBlind.Win32;

/// <summary>
/// Windows API (Win32) 声明封装
/// </summary>
internal static class Win32Api
{
    private const string User32 = "user32.dll";
    private const string Dwmapi = "dwmapi.dll";

    #region 结构体

    /// <summary>
    /// 窗口矩形结构体
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct POINT
    {
        public int X;
        public int Y;
    }

    #endregion

    #region 窗口操作

    /// <summary>
    /// 根据类名或窗口名查找窗口
    /// </summary>
    /// <param name="lpClassName">窗口类名</param>
    /// <param name="lpWindowName">窗口标题</param>
    /// <returns>窗口句柄，未找到返回 IntPtr.Zero</returns>
    [DllImport(User32, SetLastError = true, CharSet = CharSet.Unicode)]
    public static extern IntPtr FindWindow(string? lpClassName, string? lpWindowName);

    /// <summary>
    /// 获取窗口矩形（位置和大小）
    /// </summary>
    /// <param name="hWnd">窗口句柄</param>
    /// <param name="lpRect">输出矩形</param>
    /// <returns>是否成功</returns>
    [DllImport(User32, SetLastError = true)]
    public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    /// <summary>
    /// 获取当前前台窗口句柄
    /// </summary>
    /// <returns>前台窗口句柄</returns>
    [DllImport(User32, SetLastError = true)]
    public static extern IntPtr GetForegroundWindow();

    /// <summary>
    /// 设置窗口位置和大小
    /// </summary>
    /// <param name="hWnd">窗口句柄</param>
    /// <param name="hWndInsertAfter">Z-order 位置</param>
    /// <param name="X">X 坐标</param>
    /// <param name="Y">Y 坐标</param>
    /// <param name="cx">宽度</param>
    /// <param name="cy">高度</param>
    /// <param name="uFlags">标志位</param>
    /// <returns>是否成功</returns>
    [DllImport(User32, SetLastError = true)]
    public static extern bool SetWindowPos(
        IntPtr hWnd,
        IntPtr hWndInsertAfter,
        int X,
        int Y,
        int cx,
        int cy,
        uint uFlags);

    /// <summary>
    /// 检查窗口句柄是否有效
    /// </summary>
    /// <param name="hWnd">窗口句柄</param>
    /// <returns>窗口是否有效</returns>
    [DllImport(User32, SetLastError = true)]
    public static extern bool IsWindow(IntPtr hWnd);

    /// <summary>
    /// 检查窗口是否最小化
    /// </summary>
    /// <param name="hWnd">窗口句柄</param>
    /// <returns>是否最小化</returns>
    [DllImport(User32, SetLastError = true)]
    public static extern bool IsIconic(IntPtr hWnd);

    /// <summary>
    /// 检查窗口是否可见
    /// </summary>
    /// <param name="hWnd">窗口句柄</param>
    /// <returns>是否可见</returns>
    [DllImport(User32, SetLastError = true)]
    public static extern bool IsWindowVisible(IntPtr hWnd);

    /// <summary>
    /// 显示或隐藏窗口
    /// </summary>
    /// <param name="hWnd">窗口句柄</param>
    /// <param name="nCmdShow">显示命令</param>
    /// <returns>之前窗口是否可见</returns>
    [DllImport(User32, SetLastError = true)]
    public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    /// <summary>
    /// 将窗口带到前台
    /// </summary>
    /// <param name="hWnd">窗口句柄</param>
    /// <returns>是否成功</returns>
    [DllImport(User32, SetLastError = true)]
    public static extern bool BringWindowToTop(IntPtr hWnd);

    /// <summary>
    /// 设置前台窗口
    /// </summary>
    /// <param name="hWnd">窗口句柄</param>
    /// <returns>是否成功</returns>
    [DllImport(User32, SetLastError = true)]
    public static extern bool SetForegroundWindow(IntPtr hWnd);

    /// <summary>
    /// 设置父窗口
    /// </summary>
    /// <param name="hWndChild">子窗口句柄</param>
    /// <param name="hWndNewParent">新父窗口句柄</param>
    /// <returns>旧父窗口句柄</returns>
    [DllImport(User32, SetLastError = true)]
    public static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

    /// <summary>
    /// 获取父窗口
    /// </summary>
    /// <param name="hWnd">窗口句柄</param>
    /// <returns>父窗口句柄</returns>
    [DllImport(User32, SetLastError = true)]
    public static extern IntPtr GetParent(IntPtr hWnd);

    /// <summary>
    /// 获取光标位置
    /// </summary>
    /// <param name="lpPoint">输出光标坐标</param>
    /// <returns>是否成功</returns>
    [DllImport(User32, SetLastError = true)]
    public static extern bool GetCursorPos(out POINT lpPoint);

    /// <summary>
    /// 获取指定屏幕坐标处的窗口句柄
    /// </summary>
    /// <param name="point">屏幕坐标</param>
    /// <returns>该坐标处最顶层的窗口句柄</returns>
    [DllImport(User32, SetLastError = true)]
    public static extern IntPtr WindowFromPoint(POINT point);

    #endregion

    #region 快捷键

    /// <summary>
    /// 注册全局快捷键
    /// </summary>
    /// <param name="hWnd">接收消息的窗口句柄</param>
    /// <param name="id">快捷键 ID</param>
    /// <param name="fsModifiers">修饰键（Alt, Ctrl, Shift, Win）</param>
    /// <param name="vk">虚拟键码</param>
    /// <returns>是否成功</returns>
    [DllImport(User32, SetLastError = true)]
    public static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    /// <summary>
    /// 取消注册全局快捷键
    /// </summary>
    /// <param name="hWnd">窗口句柄</param>
    /// <param name="id">快捷键 ID</param>
    /// <returns>是否成功</returns>
    [DllImport(User32, SetLastError = true)]
    public static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    #endregion

    #region 分层窗口

    /// <summary>
    /// 设置分层窗口属性（透明度等）
    /// </summary>
    [DllImport(User32, SetLastError = true)]
    public static extern bool SetLayeredWindowAttributes(
        IntPtr hwnd,
        uint crKey,
        byte bAlpha,
        uint dwFlags);

    /// <summary>使用 Alpha 通道</summary>
    public const uint LWA_ALPHA = 0x00000002;

    #endregion

    #region DWM (Desktop Window Manager)

    /// <summary>
    /// 启用窗口模糊效果
    /// </summary>
    /// <param name="hwnd">窗口句柄</param>
    /// <param name="accent">DWM 属性</param>
    /// <returns>HRESULT</returns>
    [DllImport(Dwmapi, SetLastError = true)]
    public static extern int DwmSetWindowAttribute(
        IntPtr hwnd,
        uint dwAttribute,
        ref int pvAttribute,
        uint cbAttribute);

    #endregion

    #region 常量

    /// <summary>窗口置顶 - 最顶层</summary>
    public static readonly IntPtr HWND_TOPMOST = new(-1);

    /// <summary>取消窗口置顶</summary>
    public static readonly IntPtr HWND_NOTOPMOST = new(-2);

    /// <summary>窗口置顶 - Z-order 顶部（不是全局最顶层）</summary>
    public static readonly IntPtr HWND_TOP = new(0);

    /// <summary>显示窗口但不激活</summary>
    public const int SW_SHOWNA = 8;

    /// <summary>不移动窗口位置</summary>
    public const uint SWP_NOMOVE = 0x0002;

    /// <summary>不改变窗口大小</summary>
    public const uint SWP_NOSIZE = 0x0001;

    /// <summary>应用新的窗口样式</summary>
    public const uint SWP_FRAMECHANGED = 0x0020;

    /// <summary>不激活窗口</summary>
    public const uint SWP_NOACTIVATE = 0x0010;

    /// <summary>显示窗口</summary>
    public const uint SWP_SHOWWINDOW = 0x0040;

    /// <summary>隐藏窗口</summary>
    public const uint SWP_HIDEWINDOW = 0x0080;

    /// <summary>快捷键修饰键 - Alt</summary>
    public const uint MOD_ALT = 0x0001;

    /// <summary>快捷键修饰键 - Control</summary>
    public const uint MOD_CONTROL = 0x0002;

    /// <summary>快捷键修饰键 - Shift</summary>
    public const uint MOD_SHIFT = 0x0004;

    /// <summary>快捷键修饰键 - Windows 键</summary>
    public const uint MOD_WIN = 0x0008;

    #endregion
}
