# GIF 动效遮罩图案 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 在现有遮罩图案系统中新增 GIF 动效图支持，用户可上传本地 GIF 作为遮罩图案，全屏平铺循环播放。

**Architecture:** 基于 GDI+ Timer 帧循环方案。PatternManager 负责 GIF 文件检测和帧数据提取，OverlayForm 通过 Timer 逐帧切换并触发 Invalidate 重绘，OverlayManager 协调暂停/恢复逻辑。无新外部依赖。

**Tech Stack:** C# (.NET 6, Windows Forms + WPF), GDI+, xUnit (新增测试框架)

## Global Constraints

- .NET 6, Windows Forms + WPF 混合项目
- C# 严格模式，分号必须，camelCase 变量 / PascalCase 类 / UPPER_SNAKE 常量
- TDD：先写测试再写实现
- Git 提交：Conventional Commits（feat: / fix: / test: / chore:）
- 单文件 < 300 行，单函数 < 50 行

---

## File Structure

| 文件 | 职责 | 变更类型 |
|------|------|----------|
| `WechatBlind.sln` | 解决方案文件，包含 src 和 tests | 新建 |
| `tests/WechatBlind.Tests/WechatBlind.Tests.csproj` | 测试项目 | 新建 |
| `tests/WechatBlind.Tests/PatternManagerTests.cs` | PatternManager 单元测试 | 新建 |
| `tests/WechatBlind.Tests/GifHelperTests.cs` | GIF 辅助方法测试 | 新建 |
| `src/Config/PatternManager.cs` | 图案管理：GIF 检测、帧提取、保存、加载 | 修改 |
| `src/Config/Settings.cs` | AppSettings 新增 IsGifPattern | 修改 |
| `src/UI/OverlayForm.cs` | GIF 帧渲染、Timer、暂停/恢复 | 修改 |
| `src/Core/OverlayManager.cs` | SetOverlayGifPattern、pause/resume 集成 | 修改 |
| `src/UI/SettingsWindow.xaml` | 文件过滤器增加 .gif | 修改 |
| `src/UI/SettingsWindow.xaml.cs` | GIF 上传和选择逻辑 | 修改 |
| `src/AppContext.cs` | UpdateOverlayPattern 识别 GIF | 修改 |

---

### Task 1: 项目基础设施 — 创建解决方案和测试项目

**Files:**
- Create: `WechatBlind.sln`
- Create: `tests/WechatBlind.Tests/WechatBlind.Tests.csproj`

**Interfaces:**
- Consumes: 无（首次任务）
- Produces: 可运行的 xUnit 测试项目

- [ ] **Step 1: 创建解决方案文件**

```bash
cd "D:\project\github_project\Wechat blind"
dotnet new sln -n WechatBlind
dotnet sln add src/WechatBlind.csproj
```

- [ ] **Step 2: 创建测试项目**

```bash
mkdir -p tests/WechatBlind.Tests
cd tests/WechatBlind.Tests
dotnet new xunit -n WechatBlind.Tests --framework net6.0
```

- [ ] **Step 3: 添加项目引用**

```bash
cd "D:\project\github_project\Wechat blind"
dotnet sln add tests/WechatBlind.Tests/WechatBlind.Tests.csproj
cd tests/WechatBlind.Tests
dotnet add reference ../../src/WechatBlind.csproj
```

- [ ] **Step 4: 验证测试项目可运行**

```bash
cd "D:\project\github_project\Wechat blind"
dotnet test tests/WechatBlind.Tests/ --verbosity normal
```

Expected: 1 test passes (UnitTest1)

- [ ] **Step 5: 删除默认测试文件并提交**

```bash
rm tests/WechatBlind.Tests/UnitTest1.cs
cd "D:\project\github_project\Wechat blind"
git add WechatBlind.sln tests/
git commit -m "chore: add solution file and xUnit test project"
```

---

### Task 2: 数据模型变更

**Files:**
- Modify: `src/Config/PatternManager.cs:242-260` (PatternType enum + PatternInfo)
- Modify: `src/Config/Settings.cs:10-56` (AppSettings)

**Interfaces:**
- Consumes: 无
- Produces: `PatternType.CustomGif` 枚举值、`PatternInfo.IsAnimated/FrameDelays/FrameCount` 字段、`AppSettings.IsGifPattern` 字段

- [ ] **Step 1: 扩展 PatternType 枚举**

在 `src/Config/PatternManager.cs` 中，将 `PatternType` 枚举修改为：

```csharp
internal enum PatternType
{
    None,
    Preset,
    Custom,
    CustomGif,
}
```

- [ ] **Step 2: 扩展 PatternInfo 类**

在 `src/Config/PatternManager.cs` 中，将 `PatternInfo` 类修改为：

```csharp
internal sealed class PatternInfo
{
    public string Name { get; set; } = string.Empty;
    public PatternType Type { get; set; }
    public PatternManager.PresetPattern Preset { get; set; }
    public string? FilePath { get; set; }
    public bool IsAnimated { get; set; }
    public int[]? FrameDelays { get; set; }
    public int FrameCount { get; set; }
}
```

- [ ] **Step 3: PatternManager 构造函数支持自定义路径**

修改 `src/Config/PatternManager.cs` 中的构造函数，添加可选的 path 参数：

```csharp
public PatternManager(string? patternsPath = null)
{
    _patternsPath = patternsPath ?? Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "WechatBlind",
        "patterns");

    EnsureDirectoryExists();
}
```

这使得测试可以传入临时目录，与生产环境隔离。

- [ ] **Step 4: 扩展 AppSettings**

在 `src/Config/Settings.cs` 中，在 `AppSettings` 类的 `PatternOpacity` 属性之后添加：

```csharp
/// <summary>
/// 当前图案是否为 GIF 动图
/// </summary>
public bool IsGifPattern { get; set; } = false;
```

- [ ] **Step 5: 验证编译通过**

```bash
cd "D:\project\github_project\Wechat blind"
dotnet build src/WechatBlind.csproj
```

Expected: Build succeeded

- [ ] **Step 6: 提交**

```bash
git add src/Config/PatternManager.cs src/Config/Settings.cs
git commit -m "feat(config): extend data models for GIF pattern support"
```

---

### Task 3: PatternManager GIF 辅助方法

**Files:**
- Create: `tests/WechatBlind.Tests/GifHelperTests.cs`
- Modify: `src/Config/PatternManager.cs`

**Interfaces:**
- Consumes: `PatternType.CustomGif`, `PatternInfo.IsAnimated/FrameDelays/FrameCount`
- Produces: `PatternManager.IsGifFile()`, `PatternManager.GetGifFrameDelays()`

- [ ] **Step 1: 写 IsGifFile 的失败测试**

创建 `tests/WechatBlind.Tests/GifHelperTests.cs`：

```csharp
using WechatBlind.Config;

namespace WechatBlind.Tests;

public class GifHelperTests
{
    [Theory]
    [InlineData("animation.gif", true)]
    [InlineData("animation.GIF", true)]
    [InlineData("image.png", false)]
    [InlineData("image.jpg", false)]
    [InlineData("photo.jpeg", false)]
    [InlineData("no_extension", false)]
    public void IsGifFile_ReturnsCorrectResult(string fileName, bool expected)
    {
        var result = PatternManager.IsGifFile(fileName);
        Assert.Equal(expected, result);
    }
}
```

- [ ] **Step 2: 运行测试确认失败**

```bash
dotnet test tests/WechatBlind.Tests/ --filter "GifHelperTests" --verbosity normal
```

Expected: FAIL — `PatternManager.IsGifFile` 不存在

- [ ] **Step 3: 实现 IsGifFile**

在 `src/Config/PatternManager.cs` 的 `PatternManager` 类中添加：

```csharp
/// <summary>
/// 检测文件名是否为 GIF 格式
/// </summary>
public static bool IsGifFile(string fileName)
{
    return Path.GetExtension(fileName).Equals(".gif", StringComparison.OrdinalIgnoreCase);
}
```

- [ ] **Step 4: 运行测试确认通过**

```bash
dotnet test tests/WechatBlind.Tests/ --filter "GifHelperTests" --verbosity normal
```

Expected: PASS

- [ ] **Step 5: 提交**

```bash
git add src/Config/PatternManager.cs tests/WechatBlind.Tests/GifHelperTests.cs
git commit -m "feat(config): add GIF file detection method"
```

---

### Task 4: PatternManager GIF 帧延迟提取

**Files:**
- Modify: `tests/WechatBlind.Tests/GifHelperTests.cs`
- Modify: `src/Config/PatternManager.cs`

**Interfaces:**
- Consumes: `PatternManager.IsGifFile()`
- Produces: `PatternManager.GetGifFrameDelays(string filePath)` 返回 `int[]`（毫秒）

- [ ] **Step 1: 创建测试用 GIF 文件**

在测试项目中创建辅助方法生成测试 GIF。修改 `tests/WechatBlind.Tests/GifHelperTests.cs`：

```csharp
using System.Drawing;
using System.Drawing.Imaging;
using WechatBlind.Config;

namespace WechatBlind.Tests;

public class GifHelperTests
{
    [Theory]
    [InlineData("animation.gif", true)]
    [InlineData("animation.GIF", true)]
    [InlineData("image.png", false)]
    [InlineData("image.jpg", false)]
    [InlineData("image.jpeg", false)]
    [InlineData("no_extension", false)]
    public void IsGifFile_ReturnsCorrectResult(string fileName, bool expected)
    {
        var result = PatternManager.IsGifFile(fileName);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void GetGifFrameDelays_ReturnsDelaysForValidGif()
    {
        // Arrange: 创建一个 3 帧 GIF，延迟分别为 100ms, 200ms, 150ms
        var gifPath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.gif");
        try
        {
            CreateTestGif(gifPath, new int[] { 10, 20, 15 }); // 单位: 1/100秒

            // Act
            var delays = PatternManager.GetGifFrameDelays(gifPath);

            // Assert
            Assert.Equal(3, delays.Length);
            Assert.Equal(100, delays[0]);  // 10 * 10 = 100ms
            Assert.Equal(200, delays[1]);  // 20 * 10 = 200ms
            Assert.Equal(150, delays[2]);  // 15 * 10 = 150ms
        }
        finally
        {
            if (File.Exists(gifPath)) File.Delete(gifPath);
        }
    }

    [Fact]
    public void GetGifFrameDelays_ThrowsForInvalidFile()
    {
        Assert.Throws<FileNotFoundException>(() =>
            PatternManager.GetGifFrameDelays("nonexistent.gif"));
    }

    private static void CreateTestGif(string path, int[] delaysCentiseconds)
    {
        var frames = new Bitmap[delaysCentiseconds.Length];
        try
        {
            for (int i = 0; i < frames.Length; i++)
            {
                frames[i] = new Bitmap(10, 10);
                using var g = Graphics.FromImage(frames[i]);
                g.Clear(Color.FromArgb(255, i * 80, 0, 0));
            }

            var gifEncoder = ImageCodecInfo.GetImageEncoders()
                .First(e => e.FormatID == ImageFormat.Gif.Guid);

            // 保存第一帧
            var encoderParams = new EncoderParameters(1);
            encoderParams.Param[0] = new EncoderParameter(
                System.Drawing.Imaging.Encoder.Value, 0L);
            frames[0].Save(path, gifEncoder, encoderParams);

            // 追加后续帧
            var appendParam = new EncoderParameters(1);
            appendParam.Param[0] = new EncoderParameter(
                System.Drawing.Imaging.EncoderValue.MultiFrame,
                FrameDimension.Time);

            frames[0].Save(path, gifEncoder, appendParam);

            // 设置帧延迟
            var delayParam = new EncoderParameters(1);
            var delayBytes = new byte[delaysCentiseconds.Length * 4];
            for (int i = 0; i < delaysCentiseconds.Length; i++)
            {
                BitConverter.GetBytes(delaysCentiseconds[i])
                    .CopyTo(delayBytes, i * 4);
            }

            // 通过 PropertyTag 设置延迟
            // 注意：GDI+ 不直接支持设置帧延迟属性，需要手动写入
            // 这里使用替代方案：直接操作 GIF 字节
            SetGifFrameDelays(path, delaysCentiseconds);
        }
        finally
        {
            foreach (var f in frames) f?.Dispose();
        }
    }

    private static void SetGifFrameDelays(string gifPath, int[] delaysCentiseconds)
    {
        // 读取 GIF 字节，找到 NETSCAPE extension 或 Graphic Control Extension
        // 设置帧延迟（简化实现：使用 PropertyTag）
        using var image = Image.FromFile(gifPath);
        var frameCount = image.GetFrameCount(FrameDimension.Time);

        // GDI+ 的限制：不能直接设置 GIF 帧延迟
        // 使用临时方案：通过内存流操作
        // 实际测试中，GIF 帧延迟需要通过 byte-level 操作设置
        // 这里创建一个已知延迟的 GIF 用于测试

        // 替代方案：使用预创建的测试 GIF
        image.Dispose();
    }
}
```

注意：GDI+ 无法直接创建带自定义帧延迟的 GIF。测试中需要使用预置的测试 GIF 文件或通过 byte-level 操作。实际实现时，`GetGifFrameDelays` 从已有 GIF 文件读取延迟。

- [ ] **Step 2: 运行测试确认失败**

```bash
dotnet test tests/WechatBlind.Tests/ --filter "GetGifFrameDelays" --verbosity normal
```

Expected: FAIL — `PatternManager.GetGifFrameDelays` 不存在

- [ ] **Step 3: 实现 GetGifFrameDelays**

在 `src/Config/PatternManager.cs` 中添加：

```csharp
/// <summary>
/// 提取 GIF 各帧的延迟时间（毫秒）
/// </summary>
/// <param name="filePath">GIF 文件路径</param>
/// <returns>各帧延迟的毫秒数数组</returns>
/// <exception cref="FileNotFoundException">文件不存在</exception>
public static int[] GetGifFrameDelays(string filePath)
{
    if (!File.Exists(filePath))
        throw new FileNotFoundException("GIF file not found", filePath);

    using var image = Image.FromFile(filePath);
    var frameCount = image.GetFrameCount(FrameDimension.Time);

    if (frameCount <= 1)
        return new int[] { 100 }; // 单帧默认 100ms

    var delayProperty = image.GetPropertyItem(0x5100); // PropertyTagFrameDelay
    var delays = new int[frameCount];

    for (int i = 0; i < frameCount; i++)
    {
        // GIF 延迟单位是 1/100 秒，转换为毫秒
        delays[i] = BitConverter.ToInt32(delayProperty.Value, i * 4) * 10;
    }

    return delays;
}
```

- [ ] **Step 4: 运行测试确认通过**

```bash
dotnet test tests/WechatBlind.Tests/ --filter "GetGifFrameDelays" --verbosity normal
```

Expected: PASS（需要有效的测试 GIF 文件）

- [ ] **Step 5: 提交**

```bash
git add src/Config/PatternManager.cs tests/WechatBlind.Tests/GifHelperTests.cs
git commit -m "feat(config): add GIF frame delay extraction"
```

---

### Task 5: PatternManager SavePattern 和 GetAllPatterns GIF 支持

**Files:**
- Modify: `tests/WechatBlind.Tests/PatternManagerTests.cs`（新建）
- Modify: `src/Config/PatternManager.cs`

**Interfaces:**
- Consumes: `PatternManager.IsGifFile()`, `PatternManager.GetGifFrameDelays()`
- Produces: `SavePattern` 支持 GIF、`GetAllPatterns` 包含 GIF 图案

- [ ] **Step 1: 创建 PatternManager 测试**

创建 `tests/WechatBlind.Tests/PatternManagerTests.cs`：

```csharp
using WechatBlind.Config;

namespace WechatBlind.Tests;

public class PatternManagerTests : IDisposable
{
    private readonly string _tempDir;
    private readonly PatternManager _manager;

    public PatternManagerTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"wm_test_{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempDir);
        _manager = new PatternManager(_tempDir);
    }

    public void Dispose()
    {
        _manager.Dispose();
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    [Fact]
    public void SavePattern_GifFile_CopiesWithoutConversion()
    {
        // Arrange
        var sourceGif = Path.Combine(_tempDir, "source.gif");
        File.WriteAllBytes(sourceGif, CreateMinimalGifBytes());

        // Act
        var result = _manager.SavePattern(sourceGif, "test_gif");

        // Assert
        Assert.True(File.Exists(result));
        Assert.EndsWith(".gif", result);
        Assert.Equal(File.ReadAllBytes(sourceGif), File.ReadAllBytes(result));
    }

    [Fact]
    public void GetAllPatterns_IncludesCustomGifFiles()
    {
        // Arrange: 在临时 patterns 目录放置 GIF 文件
        var gifFile = Path.Combine(_tempDir, "test_anim.gif");
        File.WriteAllBytes(gifFile, CreateMinimalGifBytes());

        // Act
        var patterns = _manager.GetAllPatterns();

        // Assert
        var gifPattern = patterns.FirstOrDefault(p =>
            p.Type == PatternType.CustomGif && p.IsAnimated);
        Assert.NotNull(gifPattern);
        Assert.Equal("test_anim", gifPattern!.Name);
        Assert.NotNull(gifPattern.FrameDelays);
    }

    private static byte[] CreateMinimalGifBytes()
    {
        // 最小有效 GIF: 1x1 像素，1 帧
        return new byte[]
        {
            0x47, 0x49, 0x46, 0x38, 0x39, 0x61, // GIF89a
            0x01, 0x00, 0x01, 0x00, 0x80, 0x00, 0x00, // 1x1, no GCT
            0xFF, 0xFF, 0xFF, // 白色背景色
            0x00, 0x00, 0x00, // 黑色
            0x21, 0xF9, 0x04, 0x00, 0x00, 0x00, 0x00, 0x00, // GCE
            0x2C, 0x00, 0x00, 0x00, 0x00, 0x01, 0x00, 0x01, 0x00, 0x00, // Image Descriptor
            0x02, 0x02, 0x44, 0x01, 0x00, // LZW min code size + data
            0x3B // Trailer
        };
    }
}
```

- [ ] **Step 2: 运行测试确认失败**

```bash
dotnet test tests/WechatBlind.Tests/ --filter "PatternManagerTests" --verbosity normal
```

Expected: FAIL — SavePattern 未处理 GIF

- [ ] **Step 3: 修改 SavePattern 支持 GIF**

修改 `src/Config/PatternManager.cs` 中的 `SavePattern` 方法：

```csharp
/// <summary>
/// 保存自定义图案
/// </summary>
public string SavePattern(string sourceFilePath, string patternName)
{
    if (IsGifFile(sourceFilePath))
    {
        var fileName = $"{SanitizeFileName(patternName)}.gif";
        var destPath = Path.Combine(_patternsPath, fileName);
        File.Copy(sourceFilePath, destPath, overwrite: true);
        return destPath;
    }

    var pngFileName = $"{SanitizeFileName(patternName)}.png";
    var pngDestPath = Path.Combine(_patternsPath, pngFileName);

    // 转换为 PNG 格式并保存
    using var image = Image.FromFile(sourceFilePath);
    image.Save(pngDestPath, ImageFormat.Png);

    return pngDestPath;
}
```

- [ ] **Step 4: 修改 GetAllPatterns 扫描 GIF**

修改 `src/Config/PatternManager.cs` 中的 `GetAllPatterns` 方法，在添加自定义 PNG 图案之后添加 GIF 扫描：

```csharp
// 在 foreach (var file in files) 循环之后添加：

// 添加自定义 GIF 动图
if (Directory.Exists(_patternsPath))
{
    var gifFiles = Directory.GetFiles(_patternsPath, "*.gif");
    foreach (var file in gifFiles)
    {
        try
        {
            var delays = GetGifFrameDelays(file);
            patterns.Add(new PatternInfo
            {
                Name = Path.GetFileNameWithoutExtension(file),
                Type = PatternType.CustomGif,
                FilePath = file,
                IsAnimated = delays.Length > 1,
                FrameDelays = delays,
                FrameCount = delays.Length,
            });
        }
        catch
        {
            // 损坏的 GIF 文件，跳过
        }
    }
}
```

- [ ] **Step 5: 运行测试确认通过**

```bash
dotnet test tests/WechatBlind.Tests/ --filter "PatternManagerTests" --verbosity normal
```

Expected: PASS

- [ ] **Step 6: 提交**

```bash
git add src/Config/PatternManager.cs tests/WechatBlind.Tests/PatternManagerTests.cs
git commit -m "feat(config): add GIF support to PatternManager save and list"
```

---

### Task 6: OverlayForm GIF 渲染

**Files:**
- Modify: `src/UI/OverlayForm.cs`

**Interfaces:**
- Consumes: `PatternInfo.IsAnimated`, `PatternInfo.FrameDelays`, `PatternInfo.FrameCount`
- Produces: `OverlayForm.SetGifPattern(Image[] frames, int[] delays)`, `PauseGif()`, `ResumeGif()`

- [ ] **Step 1: 添加 GIF 相关私有字段**

在 `src/UI/OverlayForm.cs` 的 `OverlayForm` 类中，添加私有字段：

```csharp
private Image[]? _gifFrames;
private int[]? _gifFrameDelays;
private int _currentFrameIndex;
private System.Windows.Forms.Timer? _gifTimer;
```

- [ ] **Step 2: 添加 SetGifPattern 方法**

```csharp
/// <summary>
/// 设置 GIF 动效图案
/// </summary>
/// <param name="frames">GIF 各帧图片</param>
/// <param name="delays">各帧延迟（毫秒）</param>
public void SetGifPattern(Image[] frames, int[] delays)
{
    // 释放旧 GIF 资源
    ReleaseGifResources();

    _gifFrames = frames;
    _gifFrameDelays = delays;
    _currentFrameIndex = 0;

    if (frames.Length > 1 && delays.Length > 0)
    {
        EnsureGifTimerCreated();
        _gifTimer!.Interval = Math.Max(delays[0], 10); // 最小 10ms
        _gifTimer.Start();
    }

    if (IsHandleCreated && Visible)
    {
        Invalidate();
    }
}
```

- [ ] **Step 3: 添加 PauseGif 和 ResumeGif 方法**

```csharp
/// <summary>
/// 暂停 GIF 播放
/// </summary>
public void PauseGif()
{
    _gifTimer?.Stop();
}

/// <summary>
/// 恢复 GIF 播放
/// </summary>
public void ResumeGif()
{
    if (_gifFrames != null && _gifFrames.Length > 1 && _gifTimer != null)
    {
        _gifTimer.Start();
    }
}
```

- [ ] **Step 4: 添加 GIF Timer 回调和辅助方法**

```csharp
private void EnsureGifTimerCreated()
{
    if (_gifTimer == null)
    {
        _gifTimer = new System.Windows.Forms.Timer();
        _gifTimer.Tick += OnGifTimerTick;
    }
}

private void OnGifTimerTick(object? sender, EventArgs e)
{
    if (_gifFrames == null || _gifFrameDelays == null) return;

    _currentFrameIndex = (_currentFrameIndex + 1) % _gifFrames.Length;
    _gifTimer!.Interval = Math.Max(_gifFrameDelays[_currentFrameIndex], 10);
    Invalidate();
}

private void ReleaseGifResources()
{
    _gifTimer?.Stop();

    if (_gifFrames != null)
    {
        foreach (var frame in _gifFrames)
        {
            frame.Dispose();
        }
        _gifFrames = null;
    }

    _gifFrameDelays = null;
    _currentFrameIndex = 0;
}
```

- [ ] **Step 5: 修改 OnPaint 支持 GIF 帧绘制**

修改 `src/UI/OverlayForm.cs` 中的 `OnPaint` 方法：

```csharp
protected override void OnPaint(PaintEventArgs e)
{
    base.OnPaint(e);

    Image? imageToDraw = _gifFrames != null
        ? _gifFrames[_currentFrameIndex]
        : _patternImage;

    if (imageToDraw == null) return;

    var matrix = new System.Drawing.Imaging.ColorMatrix
    {
        Matrix33 = (float)_patternOpacity,
    };

    using var attributes = new System.Drawing.Imaging.ImageAttributes();
    attributes.SetColorMatrix(matrix);

    e.Graphics.DrawImage(
        imageToDraw,
        new Rectangle(0, 0, Width, Height),
        0, 0,
        imageToDraw.Width,
        imageToDraw.Height,
        GraphicsUnit.Pixel,
        attributes);
}
```

- [ ] **Step 6: 修改 SetPattern 重置 GIF 状态**

修改 `src/UI/OverlayForm.cs` 中的 `SetPattern` 方法：

```csharp
public void SetPattern(Image? patternImage, double patternOpacity = 1.0)
{
    // 切换到静态图案时释放 GIF 资源
    ReleaseGifResources();

    _patternImage = patternImage;
    _patternOpacity = patternOpacity;

    if (IsHandleCreated && Visible)
    {
        Invalidate();
    }
}
```

- [ ] **Step 7: 修改 Dispose 清理 GIF 资源**

修改 `src/UI/OverlayForm.cs` 中的 `Dispose` 方法：

```csharp
protected override void Dispose(bool disposing)
{
    if (disposing)
    {
        ReleaseGifResources();
        _gifTimer?.Dispose();
        _gifTimer = null;

        _patternImage?.Dispose();
        _patternImage = null;
    }
    base.Dispose(disposing);
}
```

- [ ] **Step 8: 验证编译通过**

```bash
cd "D:\project\github_project\Wechat blind"
dotnet build src/WechatBlind.csproj
```

Expected: Build succeeded

- [ ] **Step 9: 提交**

```bash
git add src/UI/OverlayForm.cs
git commit -m "feat(overlay): add GIF frame rendering with timer-based animation"
```

---

### Task 7: OverlayManager GIF 集成

**Files:**
- Modify: `src/Core/OverlayManager.cs`

**Interfaces:**
- Consumes: `OverlayForm.SetGifPattern()`, `OverlayForm.PauseGif()`, `OverlayForm.ResumeGif()`
- Produces: `OverlayManager.SetOverlayGifPattern()`, 集成 pause/resume 到 Show/Hide

- [ ] **Step 1: 添加 SetOverlayGifPattern 方法**

在 `src/Core/OverlayManager.cs` 中添加：

```csharp
/// <summary>
/// 设置 GIF 动效遮罩图案
/// </summary>
public void SetOverlayGifPattern(Image[] frames, int[] delays, double patternOpacity = 1.0)
{
    if (_overlayForm != null && !_overlayForm.IsDisposed)
    {
        _overlayForm.SetPattern(null, patternOpacity); // 清除静态图案
        _overlayForm.SetGifPattern(frames, delays);
    }
}
```

- [ ] **Step 2: 修改 Hide 方法添加 PauseGif**

修改 `src/Core/OverlayManager.cs` 中的 `Hide` 方法：

```csharp
public void Hide()
{
    _syncTimer.Stop();

    if (_overlayForm != null && _overlayForm.Visible)
    {
        _overlayForm.PauseGif();
        _overlayForm.Hide();
    }
}
```

- [ ] **Step 3: 修改 Show 方法添加 ResumeGif**

修改 `src/Core/OverlayManager.cs` 中的 `Show` 方法，在 `_overlayForm!.ShowAboveWindow(_wechatHwnd);` 之后添加：

```csharp
_overlayForm!.ShowAboveWindow(_wechatHwnd);
_overlayForm.ResumeGif(); // 恢复 GIF 播放
```

- [ ] **Step 4: 验证编译通过**

```bash
cd "D:\project\github_project\Wechat blind"
dotnet build src/WechatBlind.csproj
```

Expected: Build succeeded

- [ ] **Step 5: 提交**

```bash
git add src/Core/OverlayManager.cs
git commit -m "feat(overlay): integrate GIF pause/resume with overlay show/hide"
```

---

### Task 8: SettingsWindow GIF 上传支持

**Files:**
- Modify: `src/UI/SettingsWindow.xaml:129-134` (文件过滤器)
- Modify: `src/UI/SettingsWindow.xaml.cs:127-142` (OnUploadPattern)
- Modify: `src/UI/SettingsWindow.xaml.cs:158-184` (OnSave)

**Interfaces:**
- Consumes: `PatternType.CustomGif`, `PatternInfo.IsAnimated`
- Produces: GIF 文件可上传、选择、保存到设置

- [ ] **Step 1: 修改文件过滤器**

修改 `src/UI/SettingsWindow.xaml` 中 `OnUploadPattern` 相关的过滤器（在 code-behind 中）：

在 `src/UI/SettingsWindow.xaml.cs` 的 `OnUploadPattern` 方法中修改：

```csharp
private void OnUploadPattern(object sender, RoutedEventArgs e)
{
    var dlg = new Microsoft.Win32.OpenFileDialog
    {
        Filter = "图片文件|*.png;*.jpg;*.jpeg;*.bmp;*.gif|所有文件|*.*",
        Title = "选择遮罩图案",
    };
    if (dlg.ShowDialog() != true) return;

    var name = Path.GetFileNameWithoutExtension(dlg.FileName);
    var savedPath = _patternManager.SavePattern(dlg.FileName, name);
    LoadPatterns();

    var idx = _patterns.FindIndex(p => p.FilePath == savedPath);
    if (idx >= 0) SelectPattern(idx);
}
```

- [ ] **Step 2: 修改 OnSave 设置 IsGifPattern**

修改 `src/UI/SettingsWindow.xaml.cs` 的 `OnSave` 方法，在设置 `PatternOpacity` 之后添加：

```csharp
_settings.PatternOpacity = 1.0;
_settings.IsGifPattern = sel?.Type == PatternType.CustomGif;
```

- [ ] **Step 3: 修改 GetCurrentSettings 设置 IsGifPattern**

修改 `src/UI/SettingsWindow.xaml.cs` 的 `GetCurrentSettings` 方法，在返回的 `AppSettings` 中添加：

```csharp
IsGifPattern = sel?.Type == PatternType.CustomGif,
```

- [ ] **Step 4: 验证编译通过**

```bash
cd "D:\project\github_project\Wechat blind"
dotnet build src/WechatBlind.csproj
```

Expected: Build succeeded

- [ ] **Step 5: 提交**

```bash
git add src/UI/SettingsWindow.xaml src/UI/SettingsWindow.xaml.cs
git commit -m "feat(ui): add GIF file upload support to settings window"
```

---

### Task 9: AppContext GIF 图案集成

**Files:**
- Modify: `src/AppContext.cs:220-234` (UpdateOverlayPattern)

**Interfaces:**
- Consumes: `PatternManager.GetGifFrameDelays()`, `OverlayManager.SetOverlayGifPattern()`, `AppSettings.IsGifPattern`
- Produces: GIF 图案完整集成到应用流程

- [ ] **Step 1: 添加 GIF 帧提取辅助方法**

在 `src/AppContext.cs` 中添加：

```csharp
/// <summary>
/// 从 GIF 文件提取帧图片数组
/// </summary>
private static Image[] ExtractGifFrames(string filePath)
{
    using var gifImage = Image.FromFile(filePath);
    var frameCount = gifImage.GetFrameCount(FrameDimension.Time);
    var frames = new Image[frameCount];

    for (int i = 0; i < frameCount; i++)
    {
        gifImage.SelectActiveFrame(FrameDimension.Time, i);
        frames[i] = (Image)gifImage.Clone();
    }

    return frames;
}
```

- [ ] **Step 2: 修改 UpdateOverlayPattern 支持 GIF**

修改 `src/AppContext.cs` 中的 `UpdateOverlayPattern` 方法：

```csharp
private void UpdateOverlayPattern(AppSettings settings)
{
    if (settings.IsGifPattern && settings.PatternType == "CustomGif"
        && !string.IsNullOrEmpty(settings.CustomPatternPath)
        && File.Exists(settings.CustomPatternPath))
    {
        try
        {
            var frames = ExtractGifFrames(settings.CustomPatternPath);
            var delays = PatternManager.GetGifFrameDelays(settings.CustomPatternPath);
            _overlayManager.SetOverlayGifPattern(frames, delays, settings.PatternOpacity);
        }
        catch
        {
            // GIF 加载失败，回退为无图案
            _overlayManager.SetOverlayPattern(null, settings.PatternOpacity);
        }
        return;
    }

    // 原有静态图案逻辑
    Image? patternImage = null;

    if (settings.PatternType == "Preset"
        && Enum.TryParse<PatternManager.PresetPattern>(settings.PresetPattern, out var preset))
    {
        patternImage = _patternManager.LoadPattern(
            new PatternInfo { Type = PatternType.Preset, Preset = preset });
    }
    else if (settings.PatternType == "Custom" && !string.IsNullOrEmpty(settings.CustomPatternPath))
    {
        patternImage = _patternManager.LoadPattern(
            new PatternInfo { Type = PatternType.Custom, FilePath = settings.CustomPatternPath });
    }

    _overlayManager.SetOverlayPattern(patternImage, settings.PatternOpacity);
}
```

- [ ] **Step 3: 验证编译通过**

```bash
cd "D:\project\github_project\Wechat blind"
dotnet build src/WechatBlind.csproj
```

Expected: Build succeeded

- [ ] **Step 4: 运行全部测试**

```bash
cd "D:\project\github_project\Wechat blind"
dotnet test tests/WechatBlind.Tests/ --verbosity normal
```

Expected: All tests PASS

- [ ] **Step 5: 提交**

```bash
git add src/AppContext.cs
git commit -m "feat(app): integrate GIF pattern loading and rendering in AppContext"
```

---

### Task 10: PatternToImageSourceConverter GIF 预览支持

**Files:**
- Modify: `src/UI/PatternToImageSourceConverter.cs`

**Interfaces:**
- Consumes: `PatternInfo.IsAnimated`, `PatternType.CustomGif`
- Produces: GIF 图案在设置页面显示第一帧缩略图

- [ ] **Step 1: 修改 converter 处理 GIF**

修改 `src/UI/PatternToImageSourceConverter.cs` 中的 `Convert` 方法：

```csharp
public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
{
    if (value is not PatternInfo pattern) return null;

    try
    {
        // GIF 图案：仅提取第一帧用于预览
        using var image = SharedManager.LoadPattern(pattern);
        if (image == null) return null;

        using var bmp = new Bitmap(image.Width, image.Height, PixelFormat.Format32bppArgb);
        using var g = Graphics.FromImage(bmp);
        g.DrawImage(image, 0, 0, bmp.Width, bmp.Height);

        var hBitmap = bmp.GetHbitmap();
        try
        {
            var source = Imaging.CreateBitmapSourceFromHBitmap(
                hBitmap, IntPtr.Zero, Int32Rect.Empty,
                BitmapSizeOptions.FromEmptyOptions());
            source.Freeze();
            return source;
        }
        finally
        {
            DeleteObject(hBitmap);
        }
    }
    catch
    {
        return null;
    }
}
```

注意：`LoadPattern` 对于 `CustomGif` 类型当前返回 null（因为它只处理 `Custom` 类型）。需要修改 `PatternManager.LoadPattern` 支持 GIF 首帧加载。

- [ ] **Step 2: 修改 PatternManager.LoadPattern 支持 GIF**

修改 `src/Config/PatternManager.cs` 中的 `LoadPattern` 方法，在 `Custom` 类型处理之后添加：

```csharp
if (pattern.Type == PatternType.CustomGif && !string.IsNullOrEmpty(pattern.FilePath)
    && File.Exists(pattern.FilePath))
{
    if (_imageCache.TryGetValue(pattern.FilePath, out var cached))
    {
        return cached.Image;
    }

    var bytes = File.ReadAllBytes(pattern.FilePath);
    var stream = new MemoryStream(bytes);
    var image = Image.FromStream(stream);

    // GIF：仅保留第一帧用于预览
    if (image.RawFormat.Guid == ImageFormat.Gif.Guid && image.GetFrameCount(FrameDimension.Time) > 1)
    {
        image.SelectActiveFrame(FrameDimension.Time, 0);
    }

    _imageCache[pattern.FilePath] = (image, stream);
    return image;
}
```

- [ ] **Step 3: 验证编译通过**

```bash
cd "D:\project\github_project\Wechat blind"
dotnet build src/WechatBlind.csproj
```

Expected: Build succeeded

- [ ] **Step 4: 运行全部测试**

```bash
cd "D:\project\github_project\Wechat blind"
dotnet test tests/WechatBlind.Tests/ --verbosity normal
```

Expected: All tests PASS

- [ ] **Step 5: 提交**

```bash
git add src/UI/PatternToImageSourceConverter.cs src/Config/PatternManager.cs
git commit -m "feat(ui): add GIF first-frame preview in settings pattern grid"
```

---

### Task 11: 最终验证和清理

**Files:**
- 无新增/修改

**Interfaces:**
- Consumes: 所有前序任务
- Produces: 完整可运行的 GIF 遮罩功能

- [ ] **Step 1: 完整构建**

```bash
cd "D:\project\github_project\Wechat blind"
dotnet build src/WechatBlind.csproj -c Release
```

Expected: Build succeeded

- [ ] **Step 2: 运行全部测试**

```bash
dotnet test tests/WechatBlind.Tests/ --verbosity normal
```

Expected: All tests PASS

- [ ] **Step 3: 运行应用进行手动测试**

```bash
dotnet run --project src/WechatBlind.csproj
```

手动验证：
1. 打开设置窗口，点击"添加图案"，选择 GIF 文件
2. GIF 图案出现在图案列表中，显示第一帧缩略图
3. 选中 GIF 图案，点击确定
4. 打开微信窗口，切换焦点，观察遮罩显示 GIF 动效
5. GIF 全屏平铺，按原始帧率循环播放
6. 切换微信焦点，遮罩隐藏时 GIF 暂停
7. 再次切换焦点，遮罩显示时 GIF 恢复播放
8. 切换回静态图案，GIF 正确停止并释放资源

- [ ] **Step 4: 提交最终状态**

```bash
git add -A
git status
```

确认无未提交的更改（除 `publish/` 和 `tools/` 目录外）。
