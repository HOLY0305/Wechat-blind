using System.Drawing.Imaging;

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
        SolidBlack,
        Gradient,
        Checkerboard,
        DiagonalLines,
        Dots,
    }

    public PatternManager()
    {
        _patternsPath = Path.Combine(
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
        patterns.Add(new PatternInfo { Name = "纯黑", Type = PatternType.Preset, Preset = PresetPattern.SolidBlack });
        patterns.Add(new PatternInfo { Name = "渐变", Type = PatternType.Preset, Preset = PresetPattern.Gradient });
        patterns.Add(new PatternInfo { Name = "棋盘格", Type = PatternType.Preset, Preset = PresetPattern.Checkerboard });
        patterns.Add(new PatternInfo { Name = "斜线", Type = PatternType.Preset, Preset = PresetPattern.DiagonalLines });
        patterns.Add(new PatternInfo { Name = "圆点", Type = PatternType.Preset, Preset = PresetPattern.Dots });

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
        }

        return patterns;
    }

    /// <summary>
    /// 保存自定义图案
    /// </summary>
    public string SavePattern(string sourceFilePath, string patternName)
    {
        var fileName = $"{SanitizeFileName(patternName)}.png";
        var destPath = Path.Combine(_patternsPath, fileName);

        // 转换为 PNG 格式并保存
        using var image = Image.FromFile(sourceFilePath);
        image.Save(destPath, ImageFormat.Png);

        return destPath;
    }

    /// <summary>
    /// 删除自定义图案
    /// </summary>
    public bool DeletePattern(string patternName)
    {
        var filePath = GetPatternFilePath(patternName);

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

        return null;
    }

    /// <summary>
    /// 创建预设图案
    /// </summary>
    private Image CreatePresetPattern(PresetPattern preset)
    {
        var width = 100;
        var height = 100;
        var bitmap = new Bitmap(width, height);

        using var graphics = Graphics.FromImage(bitmap);

        switch (preset)
        {
            case PresetPattern.SolidBlack:
                graphics.Clear(Color.Black);
                break;

            case PresetPattern.Gradient:
                using (var brush = new System.Drawing.Drawing2D.LinearGradientBrush(
                    new Point(0, 0), new Point(width, height),
                    Color.Black, Color.Transparent))
                {
                    graphics.FillRectangle(brush, 0, 0, width, height);
                }
                break;

            case PresetPattern.Checkerboard:
                graphics.Clear(Color.Transparent);
                var size = 10;
                for (int x = 0; x < width; x += size)
                {
                    for (int y = 0; y < height; y += size)
                    {
                        if ((x / size + y / size) % 2 == 0)
                        {
                            graphics.FillRectangle(Brushes.Black, x, y, size, size);
                        }
                    }
                }
                break;

            case PresetPattern.DiagonalLines:
                graphics.Clear(Color.Transparent);
                using (var pen = new Pen(Color.Black, 2))
                {
                    for (int i = -height; i < width + height; i += 10)
                    {
                        graphics.DrawLine(pen, i, 0, i + height, height);
                    }
                }
                break;

            case PresetPattern.Dots:
                graphics.Clear(Color.Transparent);
                using (var brush = new SolidBrush(Color.Black))
                {
                    for (int x = 5; x < width; x += 10)
                    {
                        for (int y = 5; y < height; y += 10)
                        {
                            graphics.FillEllipse(brush, x - 2, y - 2, 4, 4);
                        }
                    }
                }
                break;
        }

        return bitmap;
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
}
