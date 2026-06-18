# AGENTS.md — 微信隐私遮罩 (WechatBlind)

## 项目概述
C# WinForms (.NET 6) 工具，微信窗口失去焦点时自动显示磨砂遮罩保护隐私。

## 技术栈
- **语言:** C# (.NET 6, Windows Forms)
- **构建:** `dotnet build` / `dotnet run`
- **运行时:** win-x64, self-contained

## 项目结构
```
src/
├── Program.cs              # 入口点
├── AppContext.cs            # 应用上下文（组件生命周期管理）
├── Core/
│   ├── WindowDetector.cs   # 微信窗口检测
│   ├── OverlayManager.cs   # 遮罩管理
│   ├── FocusMonitor.cs     # 焦点监控
│   └── HotkeyManager.cs    # 快捷键管理
├── UI/
│   ├── SettingsForm.cs     # 设置页面（Apple 风格，有排版 bug）
│   ├── OverlayForm.cs      # 遮罩窗口
│   ├── ToggleSwitch.cs     # iOS 风格开关控件
│   ├── RoundedPanel.cs     # 圆角面板控件
│   └── TrayManager.cs      # 托盘图标管理
├── Config/
│   ├── Settings.cs         # 设置数据模型 + JSON 序列化
│   ├── PatternManager.cs   # 图案管理（预设+自定义）
│   └── AutoStartManager.cs # 开机自启
└── Win32/
    ├── DwmApi.cs           # DWM API P/Invoke
    └── Win32Api.cs         # Win32 API P/Invoke
```

## 已知问题
- **[HIGH] 设置页面排版混乱** — `SettingsForm.cs` Apple 风格重构后字体/组件/间距不匹配，待用 Layout Panel 重构
