# 微信窗口隐私保护工具 — 开发文档

## 1. 技术选型

### 1.1 方案对比

| 方案 | 体积 | 内存 | 开发效率 | 推荐 |
|------|------|------|----------|------|
| **C# + WinForms** | ~5MB | ~8MB | ⭐⭐⭐ | ⭐⭐⭐ 推荐 |
| C++ + Win32 | <1MB | ~3MB | ⭐ | ⭐⭐ |
| C# + WPF | ~15MB | ~15MB | ⭐⭐⭐ | ⭐⭐ |
| Electron | ~80MB | ~100MB | ⭐⭐⭐ | ❌ |

### 1.2 推荐方案：C# + WinForms + .NET 6+

**选择理由：**
- **体积小**：单文件发布约 5MB，无需安装运行时
- **性能好**：原生 Windows 应用，内存占用低
- **开发快**：.NET 生态完善，WinForms 简单易用
- **易分发**：可打包为单个 exe，双击即用

**技术栈：**
```
- 语言：C# 10+
- 框架：.NET 6+（LTS）
- UI：WinForms（托盘图标 + 透明窗口）
- Windows API：P/Invoke（调用 user32.dll、dwmapi.dll）
- 打包：dotnet publish 单文件发布
```

## 2. 架构设计

### 2.1 整体架构

```
┌─────────────────────────────────────────────────────────────┐
│                     WeChat Blind 进程                       │
│                                                             │
│  ┌─────────────────┐  ┌─────────────────┐  ┌────────────┐  │
│  │   WindowDetector│  │  FocusMonitor   │  │ TrayManager│  │
│  │   (微信窗口检测) │  │  (焦点状态监控) │  │ (托盘管理) │  │
│  └────────┬────────┘  └────────┬────────┘  └────────────┘  │
│           │                    │                           │
│           ▼                    ▼                           │
│  ┌─────────────────────────────────────────────────────┐   │
│  │              OverlayManager (遮罩管理)               │   │
│  │         创建/更新/显示/隐藏 透明遮罩窗口              │   │
│  └─────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
                    ┌─────────────────┐
                    │  Windows API    │
                    │  user32.dll     │
                    │  dwmapi.dll     │
                    └─────────────────┘
```

### 2.2 模块职责

| 模块 | 职责 | 文件 |
|------|------|------|
| **WindowDetector** | 查找微信窗口，获取位置/大小 | `WindowDetector.cs` |
| **FocusMonitor** | 监控焦点变化，触发显示/隐藏 | `FocusMonitor.cs` |
| **OverlayManager** | 管理遮罩窗口生命周期 | `OverlayManager.cs` |
| **TrayManager** | 系统托盘图标和菜单 | `TrayManager.cs` |
| **SettingsManager** | 配置读写 | `SettingsManager.cs` |
| **Win32Api** | Windows API 声明 | `Win32Api.cs` |

## 3. 核心实现

### 3.1 Windows API 封装

```csharp
// Win32Api.cs
using System.Runtime.InteropServices;

internal static class Win32Api
{
    private const string User32 = "user32.dll";
    private const string Dwmapi = "dwmapi.dll";

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [DllImport(User32, SetLastError = true)]
    public static extern IntPtr FindWindow(string? lpClassName, string? lpWindowName);

    [DllImport(User32, SetLastError = true)]
    public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [DllImport(User32, SetLastError = true)]
    public static extern IntPtr GetForegroundWindow();

    [DllImport(User32, SetLastError = true)]
    public static extern bool SetWindowPos(
        IntPtr hWnd, IntPtr hWndInsertAfter,
        int X, int Y, int cx, int cy, uint uFlags);

    [DllImport(User32, SetLastError = true)]
    public static extern bool IsWindow(IntPtr hWnd);

    [DllImport(User32, SetLastError = true)]
    public static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport(User32, SetLastError = true)]
    public static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    // 窗口置顶常量
    public static readonly IntPtr HWND_TOPMOST = new(-1);
    public const uint SWP_NOMOVE = 0x0002;
    public const uint SWP_NOSIZE = 0x0001;
    public const uint SWP_NOACTIVATE = 0x0010;
    public const uint SWP_SHOWWINDOW = 0x0040;
}
```

### 3.2 微信窗口检测

```csharp
// WindowDetector.cs
public sealed class WindowDetector
{
    // 微信窗口类名（可能随版本变化）
    private static readonly string[] WeChatClassNames = new[]
    {
        "WeChatMainWndForPC",
        "WeChat",
    };

    public IntPtr FindWeChatWindow()
    {
        foreach (var className in WeChatClassNames)
        {
            var hwnd = Win32Api.FindWindow(className, null);
            if (hwnd != IntPtr.Zero && Win32Api.IsWindow(hwnd))
            {
                return hwnd;
            }
        }
        return IntPtr.Zero;
    }

    public Rectangle GetWindowPosition(IntPtr hwnd)
    {
        if (!Win32Api.GetWindowRect(hwnd, out var rect))
        {
            return Rectangle.Empty;
        }

        return new Rectangle(
            rect.Left,
            rect.Top,
            rect.Right - rect.Left,
            rect.Bottom - rect.Top
        );
    }
}
```

### 3.3 焦点监控

```csharp
// FocusMonitor.cs
public sealed class FocusMonitor : IDisposable
{
    private readonly System.Windows.Forms.Timer _timer;
    private IntPtr _wechatHwnd;
    private bool _isWeChatFocused;

    public event EventHandler<bool>? FocusChanged;

    public FocusMonitor(IntPtr wechatHwnd, int checkIntervalMs = 50)
    {
        _wechatHwnd = wechatHwnd;
        _timer = new System.Windows.Forms.Timer { Interval = checkIntervalMs };
        _timer.Tick += OnTimerTick;
    }

    public void Start() => _timer.Start();
    public void Stop() => _timer.Stop();

    private void OnTimerTick(object? sender, EventArgs e)
    {
        var foreground = Win32Api.GetForegroundWindow();
        var focused = foreground == _wechatHwnd;

        if (focused != _isWeChatFocused)
        {
            _isWeChatFocused = focused;
            FocusChanged?.Invoke(this, focused);
        }
    }

    public void UpdateWindowHandle(IntPtr hwnd)
    {
        _wechatHwnd = hwnd;
    }

    public void Dispose()
    {
        _timer.Stop();
        _timer.Dispose();
    }
}
```

### 3.4 遮罩窗口

```csharp
// OverlayForm.cs
internal sealed class OverlayForm : Form
{
    public OverlayForm()
    {
        // 窗口属性设置
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        TopMost = true;
        BackColor = Color.Black;
        Opacity = 0.7; // 默认透明度
        StartPosition = FormStartPosition.Manual;

        // 启用 DWM 模糊效果（Windows 10+）
        EnableBlur();
    }

    private void EnableBlur()
    {
        // 使用 DwmExtendFrameIntoClientArea 实现磨砂效果
        // 具体实现依赖 dwmapi.dll
    }

    public void SyncToWindow(Rectangle targetRect)
    {
        Location = new Point(targetRect.X, targetRect.Y);
        Size = new Size(targetRect.Width, targetRect.Height);
    }

    protected override CreateParams CreateParams
    {
        get
        {
            var cp = base.CreateParams;
            // 穿透鼠标点击，不影响底层窗口操作
            cp.ExStyle |= 0x80000; // WS_EX_LAYERED
            cp.ExStyle |= 0x20;    // WS_EX_TRANSPARENT
            return cp;
        }
    }
}
```

### 3.5 主程序入口

```csharp
// Program.cs
internal static class Program
{
    [STAThread]
    static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);

        // 检测微信窗口
        var detector = new WindowDetector();
        var wechatHwnd = detector.FindWeChatWindow();

        if (wechatHwnd == IntPtr.Zero)
        {
            MessageBox.Show("未检测到微信窗口，请先启动微信。", "微信幕布",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        // 启动应用
        using var app = new AppContext(wechatHwnd);
        Application.Run(app);
    }
}
```

## 4. 项目结构

```
WechatBlind/
├── src/
│   ├── WechatBlind.csproj      # 项目文件
│   ├── Program.cs              # 入口点
│   ├── AppContext.cs            # 应用上下文（生命周期管理）
│   ├── Win32/
│   │   └── Win32Api.cs         # Windows API 声明
│   ├── Core/
│   │   ├── WindowDetector.cs   # 微信窗口检测
│   │   ├── FocusMonitor.cs     # 焦点监控
│   │   └── OverlayManager.cs   # 遮罩管理
│   ├── UI/
│   │   ├── OverlayForm.cs      # 遮罩窗口
│   │   └── TrayManager.cs      # 系统托盘
│   └── Config/
│       └── Settings.cs         # 配置管理
├── assets/
│   └── icon.ico                # 应用图标
├── publish.ps1                 # 发布脚本
└── README.md
```

## 5. 代码质量规范

### 5.1 编码规范
- 使用 C# 10+ 语法（file-scoped namespaces, global using）
- 所有类、方法、属性必须有 XML 文档注释
- 私有字段使用 `_camelCase` 命名
- 公共成员使用 `PascalCase` 命名
- 常量使用 `PascalCase`（C# 规范）

### 5.2 代码审查清单

**每次提交前检查：**
- [ ] 无硬编码魔法值（提取为常量）
- [ ] 无未使用的 using 语句
- [ ] 无空的 catch 块
- [ ] IDisposable 对象正确释放
- [ ] 事件订阅正确取消
- [ ] 无内存泄漏风险
- [ ] 命名清晰，符合规范
- [ ] 单个方法 < 30 行
- [ ] 单个类 < 200 行

### 5.3 自动化审查

```bash
# 运行代码分析
dotnet analyze

# 运行格式检查
dotnet format --verify-no-changes
```

## 6. 配置管理

### 6.1 配置文件（JSON）

```json
{
  "Enabled": true,
  "Opacity": 0.7,
  "AutoStart": true,
  "HotKey": {
    "Modifiers": "Control,Shift",
    "Key": "W"
  }
}
```

### 6.2 配置存储位置
- 开发模式：`%APPDATA%\WechatBlind\settings.json`
- 生产模式：同上

## 7. 打包与分发

### 7.1 单文件发布

```xml
<!-- WechatBlind.csproj -->
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net6.0-windows</TargetFramework>
    <UseWindowsForms>true</UseWindowsForms>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    
    <!-- 单文件发布配置 -->
    <PublishSingleFile>true</PublishSingleFile>
    <SelfContained>true</SelfContained>
    <RuntimeIdentifier>win-x64</RuntimeIdentifier>
    <PublishTrimmed>true</PublishTrimmed>
    <PublishReadyToRun>true</PublishReadyToRun>
  </PropertyGroup>
</Project>
```

### 7.2 发布命令

```bash
# 开发调试
dotnet run

# 发布单文件 exe
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true

# 输出位置：bin/Release/net6.0-windows/win-x64/publish/WechatBlind.exe
```

### 7.3 预期产出
- **单文件 exe**：约 5-8 MB（含 .NET 运行时）
- **无依赖**：用户双击即可运行
- **无需安装**：绿色软件

## 8. 测试策略

### 8.1 单元测试
- WindowDetector：窗口查找逻辑
- FocusMonitor：焦点状态判断
- Settings：配置读写

### 8.2 手动测试清单
- [ ] 启动后检测到微信窗口
- [ ] 切换窗口遮罩正常显示/隐藏
- [ ] 微信窗口移动后遮罩同步
- [ ] 托盘图标功能正常
- [ ] 快捷键切换正常
- [ ] 开机自启正常
- [ ] 长时间运行稳定（>1小时）

## 9. 开发计划

### 阶段一：基础框架（1天）
- [ ] 项目初始化
- [ ] Windows API 封装
- [ ] 微信窗口检测
- [ ] 基础遮罩窗口

### 阶段二：核心功能（1天）
- [ ] 焦点监控
- [ ] 遮罩显示/隐藏逻辑
- [ ] 窗口位置同步
- [ ] 磨砂效果

### 阶段三：完善体验（1天）
- [ ] 系统托盘
- [ ] 设置功能
- [ ] 快捷键
- [ ] 开机自启

### 阶段四：优化测试（0.5天）
- [ ] 性能优化
- [ ] 代码审查
- [ ] 打包发布
- [ ] 文档完善

### 阶段五：产品美化（v2，1天）
- [ ] 设置页面 UI 重新设计
- [ ] 添加应用图标（托盘、窗口）
- [ ] 设置实时预览功能
- [ ] 遮罩自定义图案支持
- [ ] 预设图案库

## 10. 开发流程

### 10.1 核心原则
**每实现一个功能，必须完成以下步骤后才能继续下一个功能：**

```
编码 → 测试 → 审查 → 提交 → 下一个功能
```

### 10.2 功能开发流程

```
┌─────────────────────────────────────────────────────────────┐
│                    功能开发循环                              │
│                                                             │
│  ┌─────────┐   ┌─────────┐   ┌─────────┐   ┌─────────┐   │
│  │ 1.编码  │──▶│ 2.测试  │──▶│ 3.审查  │──▶│ 4.提交  │   │
│  └─────────┘   └─────────┘   └─────────┘   └─────────┘   │
│       │             │             │             │          │
│       ▼             ▼             ▼             ▼          │
│  实现功能      单元测试      代码审查      Git Commit       │
│  编写代码      手动验证      清单检查      Conventional     │
│                              规范检查      规范提交         │
└─────────────────────────────────────────────────────────────┘
```

### 10.3 开发顺序（推荐）

```
阶段一：基础框架
├── 1.1 项目初始化（.csproj, 目录结构）
├── 1.2 Windows API 封装（Win32Api.cs）
├── 1.3 微信窗口检测（WindowDetector.cs）
└── 1.4 基础测试验证

阶段二：核心功能
├── 2.1 焦点监控（FocusMonitor.cs）
├── 2.2 遮罩窗口（OverlayForm.cs）
├── 2.3 显示/隐藏逻辑
└── 2.4 集成测试验证

阶段三：用户体验
├── 3.1 系统托盘（TrayManager.cs）
├── 3.2 设置功能（Settings.cs）
├── 3.3 快捷键支持
└── 3.4 功能测试验证

阶段四：优化发布
├── 4.1 性能优化
├── 4.2 代码审查
├── 4.3 打包发布
└── 4.4 文档完善

阶段五：产品美化
├── 5.1 设置页面 UI 重新设计（SettingsForm 美化）
├── 5.2 添加应用图标（托盘图标、窗口图标）
├── 5.3 设置实时预览功能（透明度/模糊度即时显示）
├── 5.4 遮罩自定义图案支持（OverlayForm 图案绘制）
└── 5.5 预设图案库（提供默认图案选项）
```

### 10.4 提交规范

**每次提交必须：**
1. **原子提交**：一个功能/修复一个提交，不要混合
2. **规范信息**：使用 Conventional Commits 格式
3. **关联测试**：说明测试情况

**提交信息格式：**
```
<type>(<scope>): <description>

[optional body]

测试：
- [x] 单元测试通过
- [x] 手动测试通过
- [ ] 边界情况测试（如需要）
```

**示例：**
```bash
feat(window): implement WeChat window detection

- Add WindowDetector class with FindWeChatWindow method
- Support multiple WeChat class names
- Add window validity check

测试：
- [x] 单元测试：正常检测到微信窗口
- [x] 单元测试：未启动微信时返回 IntPtr.Zero
- [x] 手动测试：启动微信后自动检测

Co-Authored-By: Claude Opus 4.7 <noreply@anthropic.com>
```

### 10.5 测试要求

**每个功能模块完成后：**
1. 编写单元测试（如果适用）
2. 手动测试核心场景
3. 检查代码审查清单
4. 确认无资源泄漏
5. 提交代码

**测试检查点：**
```markdown
## 功能测试检查 - [功能名称]

### 单元测试
- [ ] 核心逻辑测试
- [ ] 边界条件测试
- [ ] 错误处理测试

### 手动测试
- [ ] 正常流程测试
- [ ] 异常流程测试
- [ ] 性能基本验证

### 代码审查
- [ ] 命名规范
- [ ] 资源管理
- [ ] 无魔法值
- [ ] 文档完整

### 提交确认
- [ ] 测试全部通过
- [ ] 代码符合规范
- [ ] 提交信息准确
```

### 10.6 分支策略

```
main (稳定版本)
│
├── feature/window-detection    # 功能分支
├── feature/focus-monitor
├── feature/overlay
├── feature/tray
└── fix/xxx                     # 修复分支
```

**流程：**
1. 从 `main` 创建功能分支
2. 在功能分支上开发、测试、提交
3. 完成后合并到 `main`
4. 删除已合并的功能分支

---

## 11. 已知问题与解决方案

### 11.1 已修复

| 问题 | 原因 | 解决方案 | 版本 |
|------|------|----------|------|
| 遮罩显示在微信窗口下方 | `SetWindowPos` 用目标句柄做 `hWndInsertAfter` 无法保证 Z-order；`TopMost=false` 与 `WS_EX_TOPMOST` 矛盾 | 重构 OverlayForm，统一使用 `HWND_TOPMOST` 置顶 | v1.0.1 |
| 遮罩位置不同步 | `Show()` 每次重设 Location 但无位置变化检测 | 添加 100ms Timer 定期检测微信窗口位置并同步 | v1.0.1 |
| DWM 模糊被 OnPaint 覆盖 | `OnPaint` 用不透明白色背景覆盖了 DWM 模糊 | 移除 `OnPaint` 自绘，改用纯 DWM 亚克力模糊 | v1.0.1 |
| TopMost 属性矛盾 | 构造函数 `TopMost=false` 与 `CreateParams` 中 `WS_EX_TOPMOST` 冲突 | 移除 `CreateParams` 中 `WS_EX_TOPMOST`，统一用 `TopMost=true` | v1.0.1 |
| WeChat 关闭后遮罩卡住 | 无后台监控 | 添加 `WeChatUnavailable` 事件 + 2s 轮询等待重启 | v1.0.1 |
| 微信最小化时遮罩未隐藏 | 未检查 `IsIconic` | sync 定时器中检查最小化状态 | v1.0.1 |
| 遮罩遮挡其他窗口 | `HWND_TOPMOST` 使遮罩在所有窗口之上 | 添加 `WindowFromPoint` 检测微信是否被遮挡 | v1.0.2 |
| 鼠标悬停不隐藏遮罩 | 无鼠标位置检测 | 添加 `IsMouseOverWeChat` 检测 | v1.0.2 |

### 11.2 已实现

| 功能 | 说明 | 版本 |
|------|------|------|
| 快捷键切换 | `HotkeyManager` + `RegisterHotKey`，默认 Ctrl+Shift+W | v1.0.3 |
| 设置面板 | `SettingsForm` 支持启用/透明度/快捷键/开机自启 | v1.0.3 |
| 开机自启 | `AutoStartManager` 写入 `HKCU\...\Run` 注册表 | v1.0.3 |
| 启动时遮罩大小修复 | 使用 `ForceRefreshPosition()` 强制刷新位置 | v1.0.3 |

### 11.3 待实现（产品美化 v2）

| 功能 | 说明 | 优先级 |
|------|------|--------|
| 设置页面 UI 重新设计 | 优化视觉布局、颜色、字体 | 高 |
| 应用图标 | 托盘图标、窗口图标 | 高 |
| 设置实时预览 | 调整透明度/模糊度时实时显示效果 | 高 |
| 遮罩自定义图案 | 支持图片、文字作为遮罩内容 | 中 |
| 预设图案库 | 提供默认图案选项 | 中 |
| 鼠标悬停策略配置 | 让用户选择"悬停时是否隐藏" | 低 |
