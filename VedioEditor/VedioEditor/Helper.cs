using FFMediaToolkit.Graphics;
using System;
using System.Drawing;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace VedioEditor
{
    static class Helper
    {
        public static unsafe Bitmap ToBitmap(this ImageData bitmap)
        {
            fixed (byte* p = bitmap.Data)
            {
                return new Bitmap(bitmap.ImageSize.Width, bitmap.ImageSize.Height, bitmap.Stride, System.Drawing.Imaging.PixelFormat.Format24bppRgb, new IntPtr(p));
            }
        }

        public static unsafe BitmapSource ToBitmapSource(this ImageData bitmapData)
        {
            fixed (byte* ptr = bitmapData.Data)
            {
                return BitmapSource.Create(bitmapData.ImageSize.Width, bitmapData.ImageSize.Height, 96, 96, PixelFormats.Bgr24, null, new IntPtr(ptr), bitmapData.Data.Length, bitmapData.Stride);
            }
        }

        public static ImageData ToImageData(this BitmapSource bitmap)
        {
            var wb = new WriteableBitmap(bitmap);
            return ImageData.FromPointer(wb.BackBuffer, ImagePixelFormat.Bgra32, wb.PixelWidth, wb.PixelHeight);
        }
    }
}
