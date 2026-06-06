# 代码审查规范

## 1. 审查原则

### 1.1 核心目标
- **正确性**：代码逻辑无误，边界情况处理完整
- **可读性**：命名清晰，结构合理，易于理解
- **简洁性**：无冗余代码，符合 KISS 原则
- **安全性**：无资源泄漏，无安全漏洞

### 1.2 审查时机
- 每个功能模块完成后
- 每次提交前
- 发现潜在问题时

## 2. 审查清单

### 2.1 基础规范

| 检查项 | 标准 | 严重程度 |
|--------|------|----------|
| 命名规范 | 符合 C# 命名约定 | 必须 |
| 文件长度 | < 300 行 | 建议 |
| 方法长度 | < 30 行 | 必须 |
| 类长度 | < 200 行 | 建议 |
| 嵌套深度 | < 3 层 | 必须 |
| 魔法值 | 提取为常量 | 必须 |

### 2.2 代码质量

| 检查项 | 标准 | 严重程度 |
|--------|------|----------|
| 未使用的 using | 删除 | 必须 |
| 未使用的变量 | 删除 | 必须 |
| 空 catch 块 | 必须有注释或处理 | 必须 |
| 重复代码 | 提取为方法 | 建议 |
| 复杂条件 | 提取为有意义的方法 | 建议 |
| 硬编码字符串 | 提取为常量或资源 | 必须 |

### 2.3 资源管理

| 检查项 | 标准 | 严重程度 |
|--------|------|----------|
| IDisposable 对象 | 使用 using 或显式释放 | 必须 |
| 事件订阅 | 在适当时机取消订阅 | 必须 |
| 非托管资源 | 使用 SafeHandle 或 Finalizer | 必须 |
| 定时器 | 在 Dispose 中停止 | 必须 |

### 2.4 并发安全

| 检查项 | 标准 | 严重程度 |
|--------|------|----------|
| 共享状态 | 使用锁或线程安全集合 | 必须 |
| UI 线程操作 | 使用 Invoke/BeginInvoke | 必须 |
| 异步方法 | 正确使用 CancellationToken | 建议 |

### 2.5 Windows API

| 检查项 | 标准 | 严重程度 |
|--------|------|----------|
| P/Invoke 签名 | 正确匹配 Win32 签名 | 必须 |
| 句柄有效性 | 使用前检查 IsWindow | 必须 |
| 错误处理 | 检查 GetLastError | 建议 |
| DPI 感知 | 使用 PerMonitorV2 | 必须 |

## 3. 代码模式

### 3.1 正确示例

```csharp
/// <summary>
/// 检测微信窗口是否存在
/// </summary>
/// <returns>窗口句柄，未找到返回 IntPtr.Zero</returns>
public IntPtr FindWeChatWindow()
{
    const string className = "WeChatMainWndForPC";
    
    var hwnd = Win32Api.FindWindow(className, null);
    
    if (hwnd == IntPtr.Zero)
    {
        return IntPtr.Zero;
    }
    
    // 验证窗口仍然有效
    if (!Win32Api.IsWindow(hwnd))
    {
        return IntPtr.Zero;
    }
    
    return hwnd;
}
```

### 3.2 错误示例

```csharp
// ❌ 错误：无文档注释，魔法值，未验证句柄
public IntPtr FindWindow()
{
    var h = Win32Api.FindWindow("WeChatMainWndForPC", null);
    return h;
}

// ❌ 错误：空 catch 块，资源泄漏
public void StartMonitor()
{
    try
    {
        _timer = new Timer();
        _timer.Start();
    }
    catch { } // 吞掉异常
}

// ✅ 正确：显式释放，异常处理
public void Dispose()
{
    _timer?.Stop();
    _timer?.Dispose();
}
```

## 4. 审查流程

### 4.1 自我审查（每次提交前）

```bash
# 1. 代码格式检查
dotnet format --verify-no-changes

# 2. 静态分析
dotnet analyze

# 3. 手动检查清单
# - [ ] 所有公共成员有 XML 注释
# - [ ] 无未使用的 using
# - [ ] 无空 catch 块
# - [ ] IDisposable 正确释放
# - [ ] 命名符合规范
```

### 4.2 审查记录模板

```markdown
## 审查记录 - [日期]

### 变更概述
- [简要说明变更内容]

### 检查项
- [x] 基础规范
- [x] 代码质量
- [x] 资源管理
- [ ] 并发安全（不适用）
- [x] Windows API

### 发现问题
1. [问题描述] - [严重程度] - [状态：已修复/待修复]

### 审查结论
- [ ] 通过
- [ ] 需要修改
```

## 5. 常见问题

### 5.1 资源泄漏
```csharp
// ❌ 错误：using 块外使用 Bitmap
var bitmap = new Bitmap(100, 100);
// ... 使用 bitmap
bitmap.Dispose(); // 容易遗忘

// ✅ 正确：使用 using 语句
using var bitmap = new Bitmap(100, 100);
// 自动释放
```

### 5.2 UI 线程阻塞
```csharp
// ❌ 错误：在 UI 线程执行耗时操作
private void OnTimerTick(object sender, EventArgs e)
{
    var rect = GetWindowRect(wechatHwnd); // 可能耗时
    UpdateOverlay(rect);
}

// ✅ 正确：快速检测，异步更新
private void OnTimerTick(object sender, EventArgs e)
{
    if (!Win32Api.GetWindowRect(wechatHwnd, out var rect))
    {
        return; // 快速失败
    }
    
    BeginInvoke(() => UpdateOverlay(rect));
}
```

### 5.3 事件泄漏
```csharp
// ❌ 错误：未取消订阅
public void Subscribe()
{
    someObject.Event += OnEvent;
}

// ✅ 正确：成对出现
public void Subscribe()
{
    someObject.Event += OnEvent;
}

public void Unsubscribe()
{
    someObject.Event -= OnEvent;
}

public void Dispose()
{
    Unsubscribe();
}
```

## 6. 工具支持

### 6.1 推荐工具
- **dotnet format**：代码格式化
- **dotnet analyze**：静态代码分析
- **StyleCop Analyzers**：编码规范检查
- **ReSharper**：代码质量分析（可选）

### 6.2 集成到项目

```xml
<!-- .editorconfig -->
[*.{cs}]
indent_size = 4
end_of_line = lf
charset = utf-8
trim_trailing_whitespace = true
insert_final_newline = true

# C# 规则
dotnet_style_qualification_for_field = false:warning
dotnet_style_qualification_for_property = false:warning
dotnet_style_qualification_for_method = false:warning
dotnet_style_qualification_for_event = false:warning
```
