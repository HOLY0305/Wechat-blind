using System.Drawing.Imaging;
using System.IO;

namespace WechatBlind.Config;

/// <summary>
/// 图案管理器
/// 负责管理遮罩图案的加载、保存和删除
/// </summary>
internal sealed class PatternManager : IDisposable
{
    private readonly string _patternsPath;
    private readonly Dictionary<string, (Image Image, MemoryStream Stream)> _imageCache = new();

    /// <summary>
    /// 预设图案类型
    /// </summary>
    public enum PresetPattern
    {
        None,
        PureBlack,
        PureWhite,
        StaticDog,
        AnimatedDog,
        AnimatedCheer,
    }

    public PatternManager(string? patternsPath = null)
    {
        _patternsPath = patternsPath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "WechatBlind",
            "patterns");

        EnsureDirectoryExists();
    }

    /// <summary>
    /// 获取所有可用图案（预设 + 自定义）
    /// </summary>
    public List<PatternInfo> GetAllPatterns()
    {
        var patterns = new List<PatternInfo>();

        // 添加预设图案
        patterns.Add(new PatternInfo { Name = "无图案", Type = PatternType.None });
        patterns.Add(new PatternInfo { Name = "纯黑", Type = PatternType.Preset, Preset = PresetPattern.PureBlack });
        patterns.Add(new PatternInfo { Name = "纯白", Type = PatternType.Preset, Preset = PresetPattern.PureWhite });
        patterns.Add(new PatternInfo { Name = "臭(静)", Type = PatternType.Preset, Preset = PresetPattern.StaticDog });
        patterns.Add(new PatternInfo { Name = "臭", Type = PatternType.Preset, Preset = PresetPattern.AnimatedDog, IsAnimated = true });
        patterns.Add(new PatternInfo { Name = "欢呼", Type = PatternType.Preset, Preset = PresetPattern.AnimatedCheer, IsAnimated = true });

        // 添加自定义图案
        if (Directory.Exists(_patternsPath))
        {
            var files = Directory.GetFiles(_patternsPath, "*.png");
            foreach (var file in files)
            {
                patterns.Add(new PatternInfo
                {
                    Name = Path.GetFileNameWithoutExtension(file),
                    Type = PatternType.Custom,
                    FilePath = file,
                });
            }

            // 添加自定义 GIF 图案
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
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Skipping corrupted GIF '{file}': {ex.Message}");
                }
            }
        }

        return patterns;
    }

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

    /// <summary>
    /// 删除自定义图案
    /// </summary>
    public bool DeletePattern(string patternName)
    {
        // 尝试 .png 和 .gif 两种扩展名
        var baseName = SanitizeFileName(patternName);
        var pngPath = Path.Combine(_patternsPath, $"{baseName}.png");
        var gifPath = Path.Combine(_patternsPath, $"{baseName}.gif");

        var filePath = File.Exists(pngPath) ? pngPath : gifPath;

        if (_imageCache.TryGetValue(filePath, out var cached))
        {
            cached.Image.Dispose();
            cached.Stream.Dispose();
            _imageCache.Remove(filePath);
        }

        if (File.Exists(filePath))
        {
            File.Delete(filePath);
            return true;
        }
        return false;
    }

    /// <summary>
    /// 加载图案图片
    /// </summary>
    public Image? LoadPattern(PatternInfo pattern)
    {
        if (pattern.Type == PatternType.None)
        {
            return null;
        }

        if (pattern.Type == PatternType.Preset)
        {
            return CreatePresetPattern(pattern.Preset);
        }

        if (pattern.Type == PatternType.Custom && !string.IsNullOrEmpty(pattern.FilePath) && File.Exists(pattern.FilePath))
        {
            if (_imageCache.TryGetValue(pattern.FilePath, out var cached))
            {
                return cached.Image;
            }

            var bytes = File.ReadAllBytes(pattern.FilePath);
            var stream = new MemoryStream(bytes);
            var image = Image.FromStream(stream);
            _imageCache[pattern.FilePath] = (image, stream);
            return image;
        }

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

            // For GIF: select first frame for preview
            if (image.RawFormat.Guid == ImageFormat.Gif.Guid && image.GetFrameCount(FrameDimension.Time) > 1)
            {
                image.SelectActiveFrame(FrameDimension.Time, 0);
            }

            _imageCache[pattern.FilePath] = (image, stream);
            return image;
        }

        return null;
    }

    /// <summary>
    /// 判断预设是否为 GIF 动效
    /// </summary>
    public bool IsGifPreset(PresetPattern preset)
    {
        return preset is PresetPattern.AnimatedDog or PresetPattern.AnimatedCheer;
    }

    /// <summary>
    /// 加载 GIF 预设的各帧图片和延迟
    /// </summary>
    public (Image[] Frames, int[] Delays) LoadGifPresetFrames(PresetPattern preset)
    {
        var resourceName = preset switch
        {
            PresetPattern.AnimatedDog => "patterns_dog.gif",
            PresetPattern.AnimatedCheer => "patterns_cheer.gif",
            _ => throw new ArgumentException($"Not a GIF preset: {preset}"),
        };

        using var stream = GetEmbeddedResourceStream(resourceName);
        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        ms.Position = 0;

        var gifImage = Image.FromStream(ms);
        var frameCount = gifImage.GetFrameCount(FrameDimension.Time);
        var frames = new Image[frameCount];

        for (int i = 0; i < frameCount; i++)
        {
            gifImage.SelectActiveFrame(FrameDimension.Time, i);
            frames[i] = (Image)gifImage.Clone();
        }

        // 从资源流重新读取延迟信息
        ms.Position = 0;
        using var delayImage = Image.FromStream(ms);
        var delays = ExtractGifDelays(delayImage, frameCount);

        gifImage.Dispose();
        return (frames, delays);
    }

    private static int[] ExtractGifDelays(Image image, int frameCount)
    {
        if (frameCount <= 1) return new[] { 100 };

        if (!image.PropertyIdList.Contains(0x5100))
            return new[] { 100 };

        var delayProperty = image.GetPropertyItem(0x5100)!;
        if (delayProperty.Value == null || delayProperty.Value.Length < frameCount * 4)
            return new[] { 100 };

        var delays = new int[frameCount];
        for (int i = 0; i < frameCount; i++)
        {
            var raw = BitConverter.ToUInt32(delayProperty.Value, i * 4);
            delays[i] = (int)Math.Clamp(raw * 10, 10, 1000);
        }
        return delays;
    }

    /// <summary>
    /// 从嵌入资源加载预设图案（静态图片返回 Image，GIF 返回第一帧预览）
    /// </summary>
    private Image CreatePresetPattern(PresetPattern preset)
    {
        var resourceName = preset switch
        {
            PresetPattern.PureBlack => "patterns_pure_black.png",
            PresetPattern.PureWhite => "patterns_pure_white.png",
            PresetPattern.StaticDog => "patterns_dog_static.png",
            PresetPattern.AnimatedDog => "patterns_dog.gif",
            PresetPattern.AnimatedCheer => "patterns_cheer.gif",
            _ => throw new ArgumentException($"Unknown preset: {preset}"),
        };

        using var stream = GetEmbeddedResourceStream(resourceName);
        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        ms.Position = 0;

        var image = Image.FromStream(ms);

        // GIF 预设：返回第一帧作为静态预览
        if (image.RawFormat.Guid == ImageFormat.Gif.Guid && image.GetFrameCount(FrameDimension.Time) > 1)
        {
            image.SelectActiveFrame(FrameDimension.Time, 0);
        }

        return (Image)image.Clone();
    }

    private static Stream GetEmbeddedResourceStream(string logicalName)
    {
        var assembly = typeof(PatternManager).Assembly;
        var stream = assembly.GetManifestResourceStream(logicalName);
        if (stream == null)
            throw new FileNotFoundException($"Embedded resource '{logicalName}' not found.");
        return stream;
    }

    private string GetPatternFilePath(string patternName)
    {
        var fileName = $"{SanitizeFileName(patternName)}.png";
        return Path.Combine(_patternsPath, fileName);
    }

    private static string SanitizeFileName(string name)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        return new string(name.Where(c => !invalidChars.Contains(c)).ToArray());
    }

    private void EnsureDirectoryExists()
    {
        if (!Directory.Exists(_patternsPath))
        {
            Directory.CreateDirectory(_patternsPath);
        }
    }

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

        var bytes = File.ReadAllBytes(filePath);
        using var ms = new MemoryStream(bytes);
        using var image = Image.FromStream(ms);
        var frameCount = image.GetFrameCount(FrameDimension.Time);

        if (frameCount <= 1)
            return new int[] { 100 }; // single frame default 100ms

        if (!image.PropertyIdList.Contains(0x5100))
            return new int[] { 100 }; // fallback for GIFs without delay property

        PropertyItem delayProperty = image.GetPropertyItem(0x5100)!; // PropertyTagFrameDelay
        if (delayProperty.Value == null || delayProperty.Value.Length < frameCount * 4)
            return new int[] { 100 }; // fallback for malformed GIF

        var delays = new int[frameCount];

        for (int i = 0; i < frameCount; i++)
        {
            // GIF delay unit is 1/100 second, convert to milliseconds
            // Use unsigned read; clamp to [10ms, 1000ms] for safety
            var raw = BitConverter.ToUInt32(delayProperty.Value, i * 4);
            delays[i] = (int)Math.Clamp(raw * 10, 10, 1000);
        }

        return delays;
    }

    /// <summary>
    /// 检测文件名是否为 GIF 格式
    /// </summary>
    public static bool IsGifFile(string fileName)
    {
        return Path.GetExtension(fileName).Equals(".gif", StringComparison.OrdinalIgnoreCase);
    }

    public void Dispose()
    {
        foreach (var entry in _imageCache.Values)
        {
            entry.Image.Dispose();
            entry.Stream.Dispose();
        }
        _imageCache.Clear();
    }
}

/// <summary>
/// 图案类型
/// </summary>
internal enum PatternType
{
    None,
    Preset,
    Custom,
    CustomGif,
}

/// <summary>
/// 图案信息
/// </summary>
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
