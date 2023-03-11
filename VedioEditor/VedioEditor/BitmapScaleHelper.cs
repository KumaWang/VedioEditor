using System.Drawing;
using System.Drawing.Drawing2D;

namespace VedioEditor
{


    /// <summary>
    /// BitmapHelper
    /// </summary>
    public static class BitmapScaleHelper
    {
        /// <summary>
        /// 缩放图片
        /// </summary>
        /// <param name="bitmap">原图片</param>
        /// <param name="width">新图片宽度</param>
        /// <param name="height">新图片高度</param>
        /// <returns>新图片</returns>
        public static Bitmap ScaleToSize(this Bitmap bitmap, int width, int height)
        {
            if (bitmap.Width == width && bitmap.Height == height)
            {
                return bitmap;
            }

            var scaledBitmap = new Bitmap(width, height);
            using (var g = Graphics.FromImage(scaledBitmap))
            {
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.DrawImage(bitmap, 0, 0, width, height);
            }

            return scaledBitmap;
        }

        /// <summary>
        /// 缩放图片
        /// </summary>
        /// <param name="bitmap">原图片</param>
        /// <param name="size">新图片大小</param>
        /// <returns>新图片</returns>
        public static Bitmap ScaleToSize(this Bitmap bitmap, Size size)
        {
            return bitmap.ScaleToSize(size.Width, size.Height);
        }

        /// <summary>
        /// 按比例来缩放
        /// </summary>
        /// <param name="bitmap">原图</param>
        /// <param name="ratio">ratio大于1,则放大;否则缩小</param>
        /// <returns>新图片</returns>
        public static Bitmap ScaleToSize(this Bitmap bitmap, float ratio)
        {
            return bitmap.ScaleToSize((int)(bitmap.Width * ratio), (int)(bitmap.Height * ratio));
        }

        /// <summary>
        /// 按给定长度/宽度等比例缩放
        /// </summary>
        /// <param name="bitmap">原图</param>
        /// <param name="width">新图片宽度</param>
        /// <param name="height">新图片高度</param>
        /// <returns>新图片</returns>
        public static Bitmap ScaleProportional(this Bitmap bitmap, int width, int height)
        {
            float proportionalWidth, proportionalHeight;

            if (width.Equals(0))
            {
                proportionalWidth = ((float)height) / bitmap.Size.Height * bitmap.Width;
                proportionalHeight = height;
            }
            else if (height.Equals(0))
            {
                proportionalWidth = width;
                proportionalHeight = ((float)width) / bitmap.Size.Width * bitmap.Height;
            }
            else if (((float)width) / bitmap.Size.Width * bitmap.Size.Height <= height)
            {
                proportionalWidth = width;
                proportionalHeight = ((float)width) / bitmap.Size.Width * bitmap.Height;
            }
            else
            {
                proportionalWidth = ((float)height) / bitmap.Size.Height * bitmap.Width;
                proportionalHeight = height;
            }

            return bitmap.ScaleToSize((int)proportionalWidth, (int)proportionalHeight);
        }

        /// <summary>
        /// 按给定长度/宽度缩放,同时可以设置背景色
        /// </summary>
        /// <param name="bitmap">原图</param>
        /// <param name="backgroundColor">背景色</param>
        /// <param name="width">新图片宽度</param>
        /// <param name="height">新图片高度</param>
        /// <returns></returns>
        public static Bitmap ScaleToSize(this Bitmap bitmap, Color backgroundColor, int width, int height)
        {
            var scaledBitmap = new Bitmap(width, height);
            using (var g = Graphics.FromImage(scaledBitmap))
            {
                g.Clear(backgroundColor);

                var proportionalBitmap = bitmap.ScaleProportional(width, height);

                var imagePosition = new Point((int)((width - proportionalBitmap.Width) / 2m), (int)((height - proportionalBitmap.Height) / 2m));
                g.DrawImage(proportionalBitmap, imagePosition);
            }

            return scaledBitmap;
        }
    }
}
