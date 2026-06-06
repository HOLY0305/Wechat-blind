using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Data;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using WechatBlind.Config;

namespace WechatBlind.UI;

/// <summary>
/// 将 PatternInfo 转换为 WPF ImageSource，用于图案预览显示
/// </summary>
[ValueConversion(typeof(PatternInfo), typeof(BitmapImage))]
internal sealed class PatternToImageSourceConverter : IValueConverter
{
    private static readonly PatternManager SharedManager = new();

    public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not PatternInfo pattern) return null;

        try
        {
            using var image = SharedManager.LoadPattern(pattern);
            if (image == null) return null;

            // 使用 Copy 确保像素格式兼容，再通过 HBitmap 桥接转 WPF
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

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();

    [System.Runtime.InteropServices.DllImport("gdi32.dll")]
    private static extern bool DeleteObject(IntPtr hObject);
}
