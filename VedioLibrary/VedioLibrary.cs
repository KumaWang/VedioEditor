using FFMediaToolkit.Decoding;
using FFMediaToolkit.Graphics;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace VedioEditor
{
    public static class VedioLibrary
    {
        public const string MediaExtands = "*.mp4;*.rmvb;*.mkv";

        struct RGB
        {
            public byte R;
            public byte G;
            public byte B;
        }

        public static bool CanCombine(MediaFrame left, MediaFrame right) 
        {
            return CompreAccuracy(left.Accuracy, right.Accuracy);
        }

        public static bool HasAudio(MediaFrame[] frames, int start, int end) 
        {
            for (var i = start; i < end && i < frames.Length; i++)
                if (frames[i].Flags.HasFlag(MediaFrameFlags.Audio))
                    return true;

            return false;
        }

        public static bool HasSubtitles(IAccuracy accuracy)
        {
            if (accuracy == null)
                return false;

            //const double v2 = 0.0001d;
            //var v = 0d;
            for (var i = 0; i < accuracy.Length; i++)
                if (accuracy.GetThreshold(i) > (100 - accuracy.Length) * 0.003)
                    return true;

            return false;
        }

        public static unsafe IEnumerable<Rectangle> GetSubtitleBounds(ImageData imgData, byte iss, double contentAreaValue, int accuracy, int valildPixels)
        {
            var points = new ConcurrentBag<Point>[accuracy];
            for (var i = 0; i < accuracy; i++)
                points[i] = new ConcurrentBag<Point>();

            var targetWidth = imgData.ImageSize.Width;
            var targetHeight = imgData.ImageSize.Height;
            var contentHeight = (int)(targetHeight * contentAreaValue);
            var bounds = new Rectangle(0, targetHeight - contentHeight, targetWidth, contentHeight);
            var signleWidth = (float)bounds.Width / accuracy;

            fixed (byte* p = imgData.Data)
            {
                var intptr = p;
                var stride = imgData.Stride / imgData.ImageSize.Width;
                Parallel.For(0, bounds.Width * bounds.Height, i =>
                {
                    int x = bounds.X + i % bounds.Width;
                    int y = bounds.Y + i / bounds.Width;

                    if (IsVaildPixel(intptr, x, y, bounds.Width, stride, iss))
                    {
                        // 计算在某个区间
                        var index = (int)(x / signleWidth);
                        points[index].Add(new Point(x, y));
                    }
                });
            }

            return points.Where(x => x.Count > valildPixels).Select(x => PointsToRect(x));
        }

        public static Rectangle PointsToRect(IEnumerable<Point> pts)
        {
            var left = int.MaxValue;
            var right = int.MinValue;
            var top = int.MaxValue;
            var bottom = int.MinValue;

            foreach (var p in pts)
            {
                if (p.X < left) left = p.X;
                if (p.X > right) right = p.X;
                if (p.Y < top) top = p.Y;
                if (p.Y > bottom) bottom = p.Y;
            }

            return new Rectangle(left, top, right - left, bottom - top);
        }

        public static bool CompreAccuracy(IAccuracy left, IAccuracy right)
        {
            if (left == right)
                return true;

            if (left == null || right == null)
                return false;

            if (left.Length != right.Length)
                return false;

            if (HasSubtitles(left) != HasSubtitles(right))
                return false;

            var vaild = 0;
            var diff = 0.2;
            for (var i = 0; i < left.Length; i++)
            {
                var ll = left.GetThreshold(i);
                var lr = right.GetThreshold(i);
         
                if (ll == lr)
                {
                    vaild++;
                    continue;
                }

                var maxl = Math.Max(ll, lr);
                var minl = Math.Min(ll, lr);
                var ss = 1 - minl / maxl;
                if (ss <= diff || ss >= (1 - diff))
                {
                    vaild++;
                }
            }

            return vaild >= (float)left.Length / 10 * 9;
        }

        public unsafe static IAccuracy GetAccuracy(ImageData bmp, byte iss, Rectangle bounds, int accuracy, int valildPixels)
        {
            var thresholds = new int[accuracy];
            var signleWidth = (float)bounds.Width / accuracy;
            long top = bounds.Top;
            long bottom = bounds.Bottom;

            fixed (byte* p = bmp.Data)
            {
                var intptr = p;
                var stride = bmp.Stride / bmp.ImageSize.Width;
                Parallel.For(0, bounds.Width * bounds.Height, i =>
                {
                    int x = bounds.X + i % bounds.Width;
                    var y = bounds.Y + i / bounds.Width;
     
                    if (IsVaildPixel(intptr, x, y, bounds.Width, stride, iss))
                    {
                        Interlocked.CompareExchange(ref top, y, Interlocked.Read(ref top) < y ? top : y);
                        Interlocked.CompareExchange(ref bottom, y, Interlocked.Read(ref bottom) > y ? bottom : y);

                        // 计算在某个区间
                        var index = (int)(x / signleWidth);
                        Interlocked.Increment(ref thresholds[index]);
                    }
                });

                var size = signleWidth * (top - bottom);
                return AccuracyFactory.Create(thresholds.Select(x => x < valildPixels ? 0 : x / size).ToArray());
            }
        }

        private static unsafe bool IsVaildPixel(byte* intptr, int x, int y, int width, int stride, int iss)
        {
            if (IsVaildSide(intptr, x, y, width, stride, iss))
            {
                var side = 0;
                if (IsVaildSide(intptr, x - 1, y, width, stride, iss)) side++;
                if (IsVaildSide(intptr, x + 1, y, width, stride, iss)) side++;
                if (IsVaildSide(intptr, x, y - 1, width, stride, iss)) side++;
                if (IsVaildSide(intptr, x, y + 1, width, stride, iss)) side++;

                return side >= 3;
            }

            return false;
        }

        private static unsafe RGB GetPixel(byte* intptr, int x, int y, int width, int stride)
        {
           return *(RGB*)(intptr + (x + y * width) * stride);
        }

        private static unsafe bool IsVaildSide(byte* intptr, int x, int y, int width, int stride, int iss)
        {
            var rgb = GetPixel(intptr, x, y, width, stride);
            int value = (rgb.R + rgb.G + rgb.B) / 3;
            return value >= iss;
        }


        private static string CreateAccuracies()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine("namespace VedioEditor");
            sb.AppendLine("{");

            for (var i = 1; i <= 100; i++)
            {
                sb.AppendLine($"struct Accuracy{i} : IAccuracy");
                sb.AppendLine("{");
                sb.AppendLine($"public int Length => {i};");
                for (var x = 0; x < i; x++)
                {
                    sb.AppendLine($"public float Threshold{x};");
                }

                sb.AppendLine($"public Accuracy{i}(float[] datas)");
                sb.AppendLine("{");
                for (var x = 0; x < i; x++)
                {
                    sb.AppendLine($"Threshold{x} = datas[{x}];");
                }
                sb.AppendLine("}");

                sb.AppendLine("public float GetThreshold(int index)");
                sb.AppendLine("{");
                sb.AppendLine("switch(index)");
                sb.AppendLine("{");
                for (var x = 0; x < i; x++)
                {
                    sb.AppendLine($"case {x}: return Threshold{x};");
                }
                sb.AppendLine("}");
                sb.AppendLine("throw new ArgumentException(index.ToString());");
                sb.AppendLine("}");
                sb.AppendLine("public void SetThreshold(int index, float threshold)");
                sb.AppendLine("{");
                sb.AppendLine("switch(index)");
                sb.AppendLine("{");
                for (var x = 0; x < i; x++)
                {
                    sb.AppendLine($"case {x}: Threshold{x} = threshold; break;");
                }
                sb.AppendLine("}");
                sb.AppendLine("throw new ArgumentException(index.ToString());");
                sb.AppendLine("}");

                sb.AppendLine("}");
            }

            sb.AppendLine("static class AccuracyFactory");
            sb.AppendLine("{");
            sb.AppendLine("public static IAccuracy Create(param float[] datas)");
            sb.AppendLine("{");
            sb.AppendLine("switch(datas.Length)");
            sb.AppendLine("{");
            for (var x = 1; x < 100; x++)
            {
                sb.AppendLine($"case {x}: return new Accuracy{x}(datas);");
            }
            sb.AppendLine("}");
            sb.AppendLine("throw new ArgumentException(datas.Length.ToString());");
            sb.AppendLine("}");
            sb.AppendLine("}");

            sb.AppendLine("}");
            return sb.ToString();
        }

    }
}
