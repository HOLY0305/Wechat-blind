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
        // Arrange: create a 2-frame animated GIF
        var gifFile = Path.Combine(_tempDir, "test_anim.gif");
        File.WriteAllBytes(gifFile, CreateMultiFrameGifBytes());

        // Act
        var patterns = _manager.GetAllPatterns();

        // Assert
        var gifPattern = patterns.FirstOrDefault(p =>
            p.Type == PatternType.CustomGif && p.IsAnimated);
        Assert.NotNull(gifPattern);
        Assert.Equal("test_anim", gifPattern!.Name);
        Assert.NotNull(gifPattern.FrameDelays);
        Assert.Equal(2, gifPattern.FrameCount);
    }

    private static byte[] CreateMinimalGifBytes()
    {
        // Minimal valid GIF: 1x1 pixel, 1 frame
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        // GIF89a Header
        writer.Write(new byte[] { 0x47, 0x49, 0x46, 0x38, 0x39, 0x61 });

        // Logical Screen Descriptor
        writer.Write((ushort)1);  // width
        writer.Write((ushort)1);  // height
        writer.Write((byte)0x80); // GCT flag=1
        writer.Write((byte)0x00);
        writer.Write((byte)0x00);

        // Global Color Table (2 colors)
        writer.Write(new byte[] { 0x00, 0x00, 0x00 }); // black
        writer.Write(new byte[] { 0xFF, 0xFF, 0xFF }); // white

        // GCE
        writer.Write((byte)0x21);
        writer.Write((byte)0xF9);
        writer.Write((byte)0x04);
        writer.Write((byte)0x00);
        writer.Write((ushort)10); // 10cs delay
        writer.Write((byte)0x00);
        writer.Write((byte)0x00);

        // Image Descriptor
        writer.Write((byte)0x2C);
        writer.Write((ushort)0);
        writer.Write((ushort)0);
        writer.Write((ushort)1);
        writer.Write((ushort)1);
        writer.Write((byte)0x00);

        // LZW data
        writer.Write((byte)0x02);
        writer.Write((byte)0x02);
        writer.Write((byte)0x84);
        writer.Write((byte)0x51);
        writer.Write((byte)0x00);

        // Trailer
        writer.Write((byte)0x3B);

        return ms.ToArray();
    }

    private static byte[] CreateMultiFrameGifBytes()
    {
        // 2-frame GIF with delays 10cs and 20cs (same format as GifHelperTests)
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        // GIF89a Header
        writer.Write(new byte[] { 0x47, 0x49, 0x46, 0x38, 0x39, 0x61 });

        // Logical Screen Descriptor
        writer.Write((ushort)2);  // width
        writer.Write((ushort)2);  // height
        writer.Write((byte)0x80); // GCT flag=1
        writer.Write((byte)0x00);
        writer.Write((byte)0x00);

        // Global Color Table (2 colors)
        writer.Write(new byte[] { 0x00, 0x00, 0x00 }); // black
        writer.Write(new byte[] { 0xFF, 0xFF, 0xFF }); // white

        // Frame 1: delay=10cs
        writer.Write((byte)0x21);  // Extension Introducer
        writer.Write((byte)0xF9);  // GCE Label
        writer.Write((byte)0x04);  // Block Size
        writer.Write((byte)0x00);  // Packed byte
        writer.Write((ushort)10);  // Delay
        writer.Write((byte)0x00);  // Transparent color index
        writer.Write((byte)0x00);  // Block Terminator

        writer.Write((byte)0x2C);  // Image Descriptor
        writer.Write((ushort)0);
        writer.Write((ushort)0);
        writer.Write((ushort)2);
        writer.Write((ushort)2);
        writer.Write((byte)0x00);

        writer.Write((byte)0x02);  // LZW min code size
        writer.Write((byte)0x02);  // sub-block size
        writer.Write((byte)0x84);
        writer.Write((byte)0x51);
        writer.Write((byte)0x00);  // block terminator

        // Frame 2: delay=20cs
        writer.Write((byte)0x21);
        writer.Write((byte)0xF9);
        writer.Write((byte)0x04);
        writer.Write((byte)0x00);
        writer.Write((ushort)20);  // Delay
        writer.Write((byte)0x00);
        writer.Write((byte)0x00);

        writer.Write((byte)0x2C);
        writer.Write((ushort)0);
        writer.Write((ushort)0);
        writer.Write((ushort)2);
        writer.Write((ushort)2);
        writer.Write((byte)0x00);

        writer.Write((byte)0x02);
        writer.Write((byte)0x02);
        writer.Write((byte)0x84);
        writer.Write((byte)0x51);
        writer.Write((byte)0x00);

        writer.Write((byte)0x3B); // Trailer
        return ms.ToArray();
    }
}
