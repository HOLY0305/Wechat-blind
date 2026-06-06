# 微信幕布 (WeChat Blind)

微信窗口隐私保护工具 - 当微信失去焦点时自动显示磨砂遮罩，保护你的聊天隐私。

## 功能特性

- **隐私遮罩** — 微信失去焦点时自动显示磨砂遮罩
- **鼠标识别** — 鼠标悬停在微信窗口上方时自动隐藏遮罩
- **自定义图案** — 支持预设图案和自定义图片作为遮罩
- **实时预览** — 设置页面实时预览透明度、模糊度和图案效果
- **全局快捷键** — `Ctrl+Shift+W` 切换遮罩（可自定义）
- **系统托盘** — 最小化到托盘运行，双击切换启用/禁用
- **开机自启** — 可选开机自动启动

## 环境要求

- Windows 10/11
- 微信（支持传统版、Qt 版、Tauri 版）

## 安装使用

### 方式一：下载 Release（推荐）

1. 下载 `WechatBlind.exe`（单文件，无需安装）
2. 先启动微信，再运行本程序
3. 程序自动检测微信窗口并最小化到托盘

### 方式二：从源码构建

```bash
# 需要 .NET 6.0 SDK
# 下载：https://dotnet.microsoft.com/download/dotnet/6.0

git clone https://github.com/HOLY0305/Wechat-blind.git
cd Wechat-blind/src

# 构建运行
dotnet run

# 发布单文件 exe
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o ../publish
```

## 使用说明

1. 启动微信
2. 运行微信幕布
3. 当你切换到其他窗口时，微信窗口自动显示磨砂遮罩
4. 将鼠标移到微信窗口上，遮罩自动消失
5. 右键托盘图标可打开设置页面

### 快捷键

| 快捷键 | 功能 |
|--------|------|
| `Ctrl+Shift+W` | 切换遮罩启用/禁用 |

### 设置页面

右键托盘图标 → 设置，可调整：
- 遮罩透明度
- 模糊程度
- 遮罩图案（预设/自定义图片）
- 图案透明度
- 切换快捷键
- 开机自启

## 项目结构

```
src/
├── Program.cs                  # 入口点
├── AppContext.cs                # 应用上下文（组件生命周期管理）
├── Core/
│   ├── WindowDetector.cs       # 微信窗口检测（支持 Tauri/Qt/旧版）
│   ├── FocusMonitor.cs         # 焦点状态监控
│   ├── OverlayManager.cs       # 遮罩窗口管理
│   └── HotkeyManager.cs        # 全局快捷键
├── UI/
│   ├── SettingsWindow.xaml     # WPF 设置页面（Apple 风格）
│   ├── SettingsWindow.xaml.cs  # 设置页面逻辑
│   ├── OverlayForm.cs          # 磨砂遮罩窗口
│   ├── TrayManager.cs          # 系统托盘
│   ├── ToggleSwitch.cs         # iOS 风格开关控件
│   ├── RoundedPanel.cs         # 圆角面板控件
│   └── PatternToImageSourceConverter.cs  # 图案预览转换器
├── Config/
│   ├── Settings.cs             # 设置数据模型 + JSON 序列化
│   ├── PatternManager.cs       # 图案管理（预设+自定义）
│   └── AutoStartManager.cs     # 开机自启管理
└── Win32/
    ├── Win32Api.cs             # Win32 API P/Invoke
    └── DwmApi.cs               # DWM API P/Invoke
```

## 技术栈

- C# / .NET 6.0 / Windows Forms
- WPF（设置页面）
- Win32 API（窗口检测、焦点监控、全局快捷键）
- DWM API（窗口模糊效果、圆角）

## 许可证

MIT License
