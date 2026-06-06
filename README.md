# 微信幕布 (WeChat Blind)

微信窗口隐私保护工具 - 当微信失去焦点时自动显示磨砂遮罩

## 功能特性

- 自动检测微信窗口
- 焦点监控：微信失去焦点时显示遮罩
- 系统托盘：最小化到托盘运行
- 快捷键：Ctrl+Shift+W 切换遮罩
- 轻量级：内存占用 <10MB

## 环境要求

- Windows 10/11
- .NET 6.0 Runtime 或更高版本

## 安装使用

### 方式一：直接运行（推荐）

1. 下载 `WechatBlind.exe`
2. 双击运行
3. 程序会自动检测微信窗口并最小化到托盘

### 方式二：从源码构建

```bash
# 安装 .NET SDK
# 下载地址：https://dotnet.microsoft.com/download/dotnet/6.0

# 克隆项目
git clone <repository-url>
cd Wechat-blind

# 构建
cd src
dotnet build

# 运行
dotnet run

# 发布单文件 exe
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

## 使用说明

1. 启动微信
2. 运行微信幕布
3. 程序会自动检测微信窗口并开始监控
4. 当你切换到其他窗口时，微信窗口会显示磨砂遮罩
5. 点击微信窗口，遮罩自动消失

### 托盘图标

- **双击**：切换启用/禁用
- **右键**：打开菜单

### 快捷键

- `Ctrl+Shift+W`：切换遮罩显示/隐藏

## 配置文件

配置文件位于：`%APPDATA%\WechatBlind\settings.json`

```json
{
  "Enabled": true,
  "Opacity": 0.7,
  "AutoStart": false,
  "HotKey": {
    "Modifiers": "Control,Shift",
    "Key": "W"
  }
}
```

## 开发说明

### 项目结构

```
src/
├── Program.cs              # 入口点
├── AppContext.cs            # 应用上下文
├── Win32/
│   └── Win32Api.cs         # Windows API 声明
├── Core/
│   ├── WindowDetector.cs   # 微信窗口检测
│   ├── FocusMonitor.cs     # 焦点监控
│   └── OverlayManager.cs   # 遮罩管理
├── UI/
│   ├── OverlayForm.cs      # 遮罩窗口
│   └── TrayManager.cs      # 系统托盘
└── Config/
    └── Settings.cs         # 配置管理
```

### 开发流程

每个功能完成后必须：
1. 编写/运行测试
2. 代码审查
3. 提交代码（Conventional Commits）

详细流程见 `docs/development.md`

## 许可证

MIT License
