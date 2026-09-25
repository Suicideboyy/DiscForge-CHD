using System;
using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Graphics.Imaging;

sealed partial class MainWindow
{
    async Task SaveSnapshotAsync(string path)
    {
        var bitmap = new RenderTargetBitmap();
        await bitmap.RenderAsync((FrameworkElement)Content);
        var pixels = await bitmap.GetPixelsAsync();
        using var output = File.Create(path);
        using var stream = output.AsRandomAccessStream();
        var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, stream);
        encoder.SetPixelData(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied,
            (uint)bitmap.PixelWidth, (uint)bitmap.PixelHeight, 96, 96, pixels.ToArray());
        await encoder.FlushAsync();
    }
}
