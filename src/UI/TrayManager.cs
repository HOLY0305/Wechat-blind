using System.IO;

namespace WechatBlind.UI;

/// <summary>
/// 系统托盘管理器
/// 负责管理托盘图标和菜单
/// </summary>
internal sealed class TrayManager : IDisposable
{
    private readonly NotifyIcon _trayIcon;
    private readonly ContextMenuStrip _contextMenu;
    private bool _disposed;

    /// <summary>
    /// 启用/禁用遮罩事件
    /// </summary>
    public event EventHandler<bool>? ToggleEnabled;

    /// <summary>
    /// 打开设置面板事件
    /// </summary>
    public event EventHandler? OpenSettings;

    /// <summary>
    /// 退出应用事件
    /// </summary>
    public event EventHandler? ExitApplication;

    /// <summary>
    /// 是否启用
    /// </summary>
    private bool _isEnabled = true;

    public TrayManager()
    {
        _contextMenu = CreateContextMenu();
        _trayIcon = CreateTrayIcon();
    }

    private NotifyIcon CreateTrayIcon()
    {
        // 加载自定义图标，如果失败则使用系统图标
        Icon trayIcon;
        try
        {
            var iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets", "icon.ico");
            if (File.Exists(iconPath))
            {
                trayIcon = new Icon(iconPath, 16, 16);
            }
            else
            {
                trayIcon = SystemIcons.Application;
            }
        }
        catch
        {
            trayIcon = SystemIcons.Application;
        }

        var icon = new NotifyIcon
        {
            Text = "微信幕布 - 双击切换启用状态",
            Icon = trayIcon,
            Visible = true,
            ContextMenuStrip = _contextMenu,
        };

        // 双击图标切换启用状态
        icon.DoubleClick += OnTrayIconDoubleClick;

        return icon;
    }

    private ContextMenuStrip CreateContextMenu()
    {
        var menu = new ContextMenuStrip();

        var toggleItem = new ToolStripMenuItem("启用遮罩", null, OnToggleClick)
        {
            Checked = true,
            Tag = "toggle",
        };

        var settingsItem = new ToolStripMenuItem("设置", null, OnSettingsClick);
        var separator = new ToolStripSeparator();
        var exitItem = new ToolStripMenuItem("退出", null, OnExitClick);

        menu.Items.Add(toggleItem);
        menu.Items.Add(settingsItem);
        menu.Items.Add(separator);
        menu.Items.Add(exitItem);

        return menu;
    }

    /// <summary>
    /// 更新托盘图标状态
    /// </summary>
    /// <param name="isEnabled">是否启用</param>
    public void UpdateStatus(bool isEnabled)
    {
        _isEnabled = isEnabled;

        if (_contextMenu.Items["toggle"] is ToolStripMenuItem toggleItem)
        {
            toggleItem.Checked = isEnabled;
            toggleItem.Text = isEnabled ? "启用遮罩" : "禁用遮罩";
        }

        _trayIcon.Text = isEnabled
            ? "微信幕布 - 已启用"
            : "微信幕布 - 已禁用";
    }

    /// <summary>
    /// 显示气泡提示
    /// </summary>
    /// <param name="title">标题</param>
    /// <param name="message">消息内容</param>
    /// <param name="icon">图标类型</param>
    /// <param name="timeout">显示时间（毫秒）</param>
    public void ShowBalloonTip(string title, string message, ToolTipIcon icon = ToolTipIcon.Info, int timeout = 3000)
    {
        _trayIcon.ShowBalloonTip(timeout, title, message, icon);
    }

    private void OnTrayIconDoubleClick(object? sender, EventArgs e)
    {
        ToggleEnabled?.Invoke(this, !_isEnabled);
    }

    private void OnToggleClick(object? sender, EventArgs e)
    {
        ToggleEnabled?.Invoke(this, !_isEnabled);
    }

    private void OnSettingsClick(object? sender, EventArgs e)
    {
        OpenSettings?.Invoke(this, EventArgs.Empty);
    }

    private void OnExitClick(object? sender, EventArgs e)
    {
        ExitApplication?.Invoke(this, EventArgs.Empty);
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _trayIcon.Visible = false;
            _trayIcon.Dispose();
            _contextMenu.Dispose();
            _disposed = true;
        }
    }
}
