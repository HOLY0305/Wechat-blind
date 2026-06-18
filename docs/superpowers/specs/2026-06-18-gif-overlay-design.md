# GIF 动效遮罩图案设计文档

## 概述

在现有遮罩图案系统基础上，新增 GIF 动效图支持。用户可上传本地 GIF 文件作为遮罩图案，GIF 动画全屏平铺显示，按原始帧率循环播放。

## 目标

- GIF 作为独立图案类型，与预设/自定义 PNG 并列
- 仅支持用户自定义上传（无内置预设 GIF）
- GIF 动效全屏平铺填满遮罩区域
- 遮罩隐藏时暂停播放，显示时恢复，节省 CPU
- 无新外部依赖，基于 GDI+ 实现

## 数据模型变更

### PatternType 枚举扩展

```csharp
internal enum PatternType
{
    None,
    Preset,
    Custom,       // 静态 PNG
    CustomGif,    // GIF 动图（新增）
}
```

### PatternInfo 新增字段

| 字段 | 类型 | 说明 |
|------|------|------|
| `IsAnimated` | `bool` | 是否为 GIF 动图 |
| `FrameDelays` | `int[]?` | 各帧延迟（毫秒） |
| `FrameCount` | `int` | 帧数 |

### AppSettings 新增字段

| 字段 | 类型 | 说明 |
|------|------|------|
| `IsGifPattern` | `bool` | 当前图案是否为 GIF |

## 模块变更

### 1. PatternManager

**新增方法**:
- `IsGifFile(string filePath)` — 检测文件扩展名是否为 `.gif`
- `GetGifFrameDelays(string filePath)` — 提取 GIF 帧延迟（PropertyTagFrameDelay）

**修改方法**:
- `SavePattern` — 增加 GIF 文件处理（直接复制，不做格式转换）
- `GetAllPatterns` — 扫描 `*.gif` 文件，填充 `PatternInfo` 的 GIF 字段

**GIF 帧延迟提取**:
- GIF 延迟单位为 1/100 秒，需转换为毫秒
- 使用 `Image.GetPropertyItem(0x5100)` 获取帧延迟属性

### 2. OverlayForm

**新增私有字段**:
```csharp
private Image[]? _gifFrames;        // GIF 各帧图片
private int[]? _gifFrameDelays;     // 各帧延迟（毫秒）
private int _currentFrameIndex;     // 当前帧索引
private System.Windows.Forms.Timer? _gifTimer;  // 帧切换 Timer
private bool _isGifAnimating;       // 是否正在播放 GIF
```

**新增公共方法**:
- `SetGifPattern(Image[] frames, int[] delays)` — 设置 GIF 图案（帧数组 + 延迟数组）
- `PauseGif()` — 暂停 GIF 播放（隐藏时调用）
- `ResumeGif()` — 恢复 GIF 播放（显示时调用）

**修改方法**:
- `OnPaint` — 增加 GIF 帧绘制逻辑：当 `_gifFrames` 不为 null 时，绘制当前帧
- `Dispose` — 清理 GIF 帧资源和 Timer
- `SetPattern` — 切换图案时重置 GIF 状态

**GIF 渲染逻辑**:
```
OnPaint:
  if _gifFrames != null:
    DrawImage(_gifFrames[_currentFrameIndex], ...)  // 绘制当前帧
  else:
    DrawImage(_patternImage, ...)  // 原有静态图案逻辑
```

**Timer 逻辑**:
```
OnGifTimerTick:
  _currentFrameIndex = (_currentFrameIndex + 1) % _gifFrameDelays.Length
  _gifTimer.Interval = _gifFrameDelays[_currentFrameIndex]
  Invalidate()  // 触发重绘
```

**暂停/恢复**:
- 隐藏时（`Hide()` 调用链）：停止 `_gifTimer`
- 显示时（`ShowAboveWindow` 调用链）：启动 `_gifTimer`

### 3. OverlayManager

**新增方法**:
- `SetOverlayGifPattern(Image[] frames, int[] delays, double patternOpacity)` — 设置 GIF 图案
- `Hide()` 修改：调用 `_overlayForm.PauseGif()`
- `Show()` 修改：调用 `_overlayForm.ResumeGif()`

### 4. SettingsWindow

**修改**:
- `OnUploadPattern`：文件过滤器增加 `*.gif`
- GIF 图案预览：使用第一帧作为缩略图

**文件过滤器更新**:
```csharp
Filter = "图片文件|*.png;*.jpg;*.jpeg;*.bmp;*.gif|所有文件|*.*"
```

### 5. AppContext

**修改 `UpdateOverlayPattern`**:
```
if settings.IsGifPattern && settings.PatternType == "CustomGif":
  提取 GIF 帧数据
  调用 _overlayManager.SetOverlayGifPattern(frames, delays, opacity)
else:
  原有静态图案逻辑
```

## 数据流

```
用户上传 GIF
  → SettingsWindow.OnUploadPattern
  → PatternManager.SavePattern (复制到 AppData)
  → LoadPatterns (刷新列表，提取帧数据)

用户选择 GIF 图案
  → SettingsWindow.SelectPattern
  → SettingsSaved 事件
  → AppContext.UpdateOverlayPattern
  → OverlayManager.SetOverlayGifPattern
  → OverlayForm.SetGifPattern (帧数组 + 延迟)
  → Timer 启动，开始动画

遮罩隐藏/显示
  → OverlayManager.Hide → OverlayForm.PauseGif (停止 Timer)
  → OverlayManager.Show → OverlayForm.ResumeGif (启动 Timer)
```

## 资源管理

- GIF 帧数组在 `OverlayForm.Dispose` 时释放
- PatternManager 的 `Image` 缓存不变（GIF 首帧用于预览）
- 切换图案时，旧 GIF 帧数组被替换并释放

## 性能考虑

- GIF 帧预加载到内存，避免播放时解码开销
- 遮罩隐藏时 Timer 停止，不消耗 CPU
- 大尺寸 GIF（如 4K）可能占用较多内存，但遮罩场景可接受

## 边界情况

- 损坏的 GIF 文件：`GetGifFrameDelays` 捕获异常，回退为静态图片（仅显示第一帧）
- 单帧 GIF：正常显示为静态图片，不启动 Timer
- 超大 GIF（>100 帧）：正常支持，无限制
- GIF 透明通道：通过 GDI+ 原生支持

## 实现约束

- GIF 帧解码使用 `Image.SelectActiveFrame` + `Image.Clone` 逐帧提取，预加载到内存
- 单帧 GIF（`FrameCount == 1`）不启动动画 Timer，按静态图片处理
- 切换图案时必须先释放旧 GIF 帧资源，再加载新帧
- `PatternToImageSourceConverter` 仅提取首帧用于 WPF 预览缩略图
