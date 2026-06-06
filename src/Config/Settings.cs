using System.Text.Json;
using System.Text.Json.Serialization;

namespace WechatBlind.Config;

/// <summary>
/// 应用配置模型
/// </summary>
internal sealed class AppSettings
{
    /// <summary>
    /// 是否启用遮罩
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// 遮罩透明度（0.0 - 1.0）
    /// </summary>
    public double Opacity { get; set; } = 0.7;

    /// <summary>
    /// 是否开机自启
    /// </summary>
    public bool AutoStart { get; set; } = false;

    /// <summary>
    /// 快捷键配置
    /// </summary>
    public HotKeySettings HotKey { get; set; } = new();
}

/// <summary>
/// 快捷键配置
/// </summary>
internal sealed class HotKeySettings
{
    /// <summary>
    /// 修饰键（Alt, Ctrl, Shift, Win）
    /// </summary>
    public string Modifiers { get; set; } = "Control,Shift";

    /// <summary>
    /// 主键
    /// </summary>
    public string Key { get; set; } = "W";
}

/// <summary>
/// 配置管理器
/// 负责配置的读写和持久化
/// </summary>
internal sealed class SettingsManager
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly string _settingsPath;
    private AppSettings? _currentSettings;

    /// <summary>
    /// 初始化配置管理器
    /// </summary>
    /// <param name="settingsPath">配置文件路径（可选，默认为 %APPDATA%\WechatBlind\settings.json）</param>
    public SettingsManager(string? settingsPath = null)
    {
        _settingsPath = settingsPath ?? GetDefaultSettingsPath();
    }

    /// <summary>
    /// 获取当前配置
    /// </summary>
    /// <returns>配置对象</returns>
    public AppSettings GetSettings()
    {
        if (_currentSettings != null)
        {
            return _currentSettings;
        }

        _currentSettings = LoadSettings();
        return _currentSettings;
    }

    /// <summary>
    /// 保存配置
    /// </summary>
    /// <param name="settings">配置对象</param>
    public void SaveSettings(AppSettings settings)
    {
        _currentSettings = settings;

        var directory = Path.GetDirectoryName(_settingsPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var json = JsonSerializer.Serialize(settings, JsonOptions);
        File.WriteAllText(_settingsPath, json);
    }

    /// <summary>
    /// 加载配置文件
    /// </summary>
    /// <returns>配置对象</returns>
    private AppSettings LoadSettings()
    {
        if (!File.Exists(_settingsPath))
        {
            return new AppSettings();
        }

        try
        {
            var json = File.ReadAllText(_settingsPath);
            return JsonSerializer.Deserialize<AppSettings>(json, JsonOptions) ?? new AppSettings();
        }
        catch
        {
            return new AppSettings();
        }
    }

    /// <summary>
    /// 获取默认配置文件路径
    /// </summary>
    /// <returns>配置文件路径</returns>
    private static string GetDefaultSettingsPath()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(appData, "WechatBlind", "settings.json");
    }
}
