using WechatBlind.Win32;

namespace WechatBlind.Core;

/// <summary>
/// 全局快捷键管理器
/// 使用隐藏消息窗口接收 WM_HOTKEY 消息
/// </summary>
internal sealed class HotkeyManager : NativeWindow, IDisposable
{
    private const int WM_HOTKEY = 0x0312;
    private const int HOTKEY_ID = 1;

    private uint _modifiers;
    private uint _vk;
    private bool _registered;
    private bool _disposed;

    /// <summary>
    /// 快捷键被按下时触发
    /// </summary>
    public event EventHandler? HotkeyPressed;

    /// <summary>
    /// 注册全局快捷键
    /// </summary>
    /// <param name="modifiers">修饰键（Alt/Ctrl/Shift/Win）</param>
    /// <param name="vk">虚拟键码</param>
    /// <returns>是否注册成功</returns>
    public bool Register(uint modifiers, uint vk)
    {
        Unregister();

        // 创建隐藏消息窗口
        if (Handle == IntPtr.Zero)
        {
            CreateHandle(new CreateParams());
        }

        _modifiers = modifiers;
        _vk = vk;

        _registered = Win32Api.RegisterHotKey(Handle, HOTKEY_ID, modifiers, vk);
        return _registered;
    }

    /// <summary>
    /// 取消注册快捷键
    /// </summary>
    public void Unregister()
    {
        if (_registered && Handle != IntPtr.Zero)
        {
            Win32Api.UnregisterHotKey(Handle, HOTKEY_ID);
            _registered = false;
        }
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == WM_HOTKEY && m.WParam.ToInt32() == HOTKEY_ID)
        {
            HotkeyPressed?.Invoke(this, EventArgs.Empty);
        }

        base.WndProc(ref m);
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            Unregister();
            if (Handle != IntPtr.Zero)
            {
                DestroyHandle();
            }
            _disposed = true;
        }
    }
}
