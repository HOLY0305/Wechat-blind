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
}
