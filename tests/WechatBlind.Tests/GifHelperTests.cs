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
    public void GetGifFrameDelays_ThrowsForInvalidFile()
    {
        Assert.Throws<FileNotFoundException>(() =>
            PatternManager.GetGifFrameDelays("nonexistent.gif"));
    }

    [Fact]
    public void GetGifFrameDelays_ReadsDelaysFromMinimalGif()
    {
        // Arrange: construct a 3-frame GIF with known delays via byte-level operations
        var gifPath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.gif");
        try
        {
            var gifBytes = CreateMinimalGifWithDelays(new int[] { 10, 20, 15 }, 2, 2);
            File.WriteAllBytes(gifPath, gifBytes);

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

    /// <summary>
    /// Creates a minimal valid GIF with specified frame delays at the byte level.
    /// GIF89a format: Header + LSD + GCT + (GCE + ImageDesc + LZW data per frame) + Trailer
    /// </summary>
    private static byte[] CreateMinimalGifWithDelays(int[] delaysCentiseconds, int width, int height)
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        // GIF89a Header (no u8 suffix — .NET 6 / C# 10)
        writer.Write(new byte[] { 0x47, 0x49, 0x46, 0x38, 0x39, 0x61 });

        // Logical Screen Descriptor
        writer.Write((ushort)width);
        writer.Write((ushort)height);
        writer.Write((byte)0x80);  // GCT flag=1, color resolution=7
        writer.Write((byte)0x00);  // background color index
        writer.Write((byte)0x00);  // pixel aspect ratio

        // Global Color Table (2 colors)
        writer.Write(new byte[] { 0x00, 0x00, 0x00 }); // black
        writer.Write(new byte[] { 0xFF, 0xFF, 0xFF }); // white

        for (int i = 0; i < delaysCentiseconds.Length; i++)
        {
            // Graphic Control Extension
            writer.Write((byte)0x21);  // Extension Introducer
            writer.Write((byte)0xF9);  // GCE Label
            writer.Write((byte)0x04);  // Block Size
            writer.Write((byte)0x00);  // Packed byte
            writer.Write((ushort)delaysCentiseconds[i]); // Delay
            writer.Write((byte)0x00);  // Transparent color index
            writer.Write((byte)0x00);  // Block Terminator

            // Image Descriptor
            writer.Write((byte)0x2C);
            writer.Write((ushort)0);   // left
            writer.Write((ushort)0);   // top
            writer.Write((ushort)width);
            writer.Write((ushort)height);
            writer.Write((byte)0x00);  // no LCT

            // LZW compressed data (GIF sub-block format)
            // min code size=2, initial code size=3 bits, clear=4, EOI=5
            // Encoding solid-color 2x2 image: codes 4,0,6,0,5 packed LSB-first
            writer.Write((byte)0x02);  // LZW min code size
            writer.Write((byte)0x02);  // sub-block size = 2 bytes
            writer.Write((byte)0x84);  // LZW data byte 0 (codes 4,0,6 partial)
            writer.Write((byte)0x51);  // LZW data byte 1 (codes 6 finish,0,5)
            writer.Write((byte)0x00);  // block terminator
        }

        writer.Write((byte)0x3B); // GIF Trailer
        return ms.ToArray();
    }
}
