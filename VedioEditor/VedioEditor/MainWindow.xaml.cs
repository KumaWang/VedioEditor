using AduSkin.Controls.Metro;
using FFMediaToolkit;
using FFMediaToolkit.Audio;
using FFMediaToolkit.Decoding;
using FFMediaToolkit.Graphics;
using IronOcr;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using VideoStateAxis;
using static VedioEditor.VedioLibrary;
using Color = System.Windows.Media.Color;

namespace VedioEditor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {

        private ObservableCollection<MediaFileHeader> mHeader = new ObservableCollection<MediaFileHeader>();

        /// <summary>
        /// 获得或设置字幕占屏幕比例
        /// </summary>
        public double ContentArea => (double)PART_SubTitle.Value;

        /// <summary>
        /// 获得或设置图像二值化阈值
        /// </summary>
        public byte Threshold => (byte)PART_Threshold.Value;

        /// <summary>
        /// 获得或设置字幕识别精准度
        /// 越高越精准，但性能下降
        /// 值范围(1-100)
        /// </summary>
        public int Accuraciy => (int)PART_Accuray.Value;

        /// <summary>
        /// 获取变速值
        /// </summary>
        public double SpeedRate => (double)PART_FrameRate.Value;

        public MainWindow()
        {
            InitializeComponent();

            FFmpegLoader.LogCallback += FFmpegLoader_LogCallback;
            FFmpegLoader.FFmpegPath = "ffmpeg";
            FFmpegLoader.LoadFFmpeg();

            this.Closed += delegate { System.Windows.Application.Current.Shutdown(); };
            BorderBrush = new SolidColorBrush(Color.FromArgb(255, 45, 45, 48));
            Foreground = new SolidColorBrush(Color.FromArgb(255, 220, 220, 220));
            Title = "视频素材批处理";

            PART_List.ItemsSource = mHeader;
            PART_List.SelectedCellsChanged += PART_List_SelectedCellsChanged;
            PART_TimeLine.DragTimeLine += PART_TimeLine_DragTimeLine;
        }

        private void PART_TimeLine_DragTimeLine(object sender, RoutedEventArgs e)
        {
            var header = PART_List.SelectedItem as MediaFileHeader;
            if (header == null)
                return;

            var offset = PART_TimeLine.AxisTime - PART_TimeLine.StartTime;
            var media = header.MediaFile;
            var frame = header.MediaFrames.LastOrDefault(x => x.TimeOffset < offset);

            ImageData imgData = default;
            var hasVedio = offset <= media.Video.Info.Duration ? media.Video.TryGetFrame(frame.TimeOffset, out imgData) : false;
            if (hasVedio)
            {
                var image = imgData.ToBitmapSource();
                var bounds = GetSubtitleBounds(imgData, header.Threshold, header.ContentArea, header.Accuraciy, (int)PART_VaildPixel.Value).ToArray();
                if (bounds.Length > 0)
                {
                    DrawingVisual dv = new DrawingVisual();
                    using (DrawingContext dc = dv.RenderOpen())
                    {
                        dc.DrawImage(image, new Rect(0, 0, image.PixelWidth, image.PixelHeight));
                        foreach (var bound in bounds)
                            dc.DrawRectangle(null, new System.Windows.Media.Pen(System.Windows.Media.Brushes.Red, 2), ToRect(bound));
                    }

                    RenderTargetBitmap rtb = new RenderTargetBitmap(image.PixelWidth, image.PixelHeight, image.DpiX, image.DpiY, PixelFormats.Pbgra32);
                    rtb.Render(dv);
                    image = rtb;
                }

                PART_Preview.PreviewSource = image;
                media.Video.DiscardBufferedData();
            }
            else
            {
                DrawingVisual dv = new DrawingVisual();
                using (DrawingContext dc = dv.RenderOpen())
                {
                    dc.DrawRectangle(System.Windows.Media.Brushes.Black, null, new Rect(0, 0, header.MediaFile.Video.Info.FrameSize.Width, header.MediaFile.Video.Info.FrameSize.Height));
                }

                RenderTargetBitmap rtb = new RenderTargetBitmap(header.MediaFile.Video.Info.FrameSize.Width, header.MediaFile.Video.Info.FrameSize.Height, 96, 96, PixelFormats.Pbgra32);
                rtb.Render(dv);
                PART_Preview.PreviewSource = rtb;
            }
        }

        private void PART_List_SelectedCellsChanged(object sender, System.Windows.Controls.SelectedCellsChangedEventArgs e)
        {
            var header = PART_List.SelectedItem as MediaFileHeader;

            UpdateTimeline(header);
        }

        private void UpdateTimeline(MediaFileHeader header) 
        {
            var VideoSource = new ObservableCollection<VideoStateItem>();

            for (var i = 0; i < header.MediaTracks.Length; i++) 
            {
                var track = header.MediaTracks[i];
                var color = Colors.Transparent;

                if (track.Flags == MediaFrameFlags.None)
                {
                    //color = Color.FromArgb(255, 250, 128, 114);
                }
                else 
                {
                    var r = Color.FromArgb(255, 250, 128, 114);
                    var g = Color.FromArgb(255, 144, 238, 144);
                    var b = Color.FromArgb(255, 30, 144, 255);

                    if (track.HasVedio) color = (color == Colors.Transparent) ? r : Blend(color, r);
                    if (track.HasAudio) color = (color == Colors.Transparent) ? g : Blend(color, g);
                    if (track.HasSubtitles) color = (color == Colors.Transparent) ? b : Blend(color, b);
                }

                var startFrame = header.MediaFrames[track.StartFrame];
                var endFrame = header.MediaFrames[track.EndFrame];
                var item = new VideoStateItem() { CameraName = $"{startFrame.TimeOffset}", DrawColor = color };
                item.StartTime = startFrame.TimeOffset;
                item.EndTime = endFrame.TimeOffset;
                item.CameraChecked = track.HasAudio;
                VideoSource.Add(item);
            }

            PART_TimeLine.HisVideoSources = VideoSource;

            PART_TimeLine_DragTimeLine(null, null);
        }

        /// <summary>Blends the specified colors together.</summary>
        /// <param name="color">Color to blend onto the background color.</param>
        /// <param name="backColor">Color to blend the other color onto.</param>
        /// <param name="amount">How much of <paramref name="color"/> to keep,
        /// “on top of” <paramref name="backColor"/>.</param>
        /// <returns>The blended colors.</returns>
        private static Color Blend(Color color, Color backColor, double amount = 0.5)
        {
            byte r = (byte)(color.R * amount + backColor.R * (1 - amount));
            byte g = (byte)(color.G * amount + backColor.G * (1 - amount));
            byte b = (byte)(color.B * amount + backColor.B * (1 - amount));
            return Color.FromRgb(r, g, b);
        }

        private void FFmpegLoader_LogCallback(string message)
        {
            Console.WriteLine(message); 
        }

        private MediaFile OpenMediaFile(string fileName) 
        {
            // Opens a multimedia file.
            // You can use the MediaOptions properties to set decoder options.
            var file = MediaFile.Open(fileName);

            // Print informations about the video stream.
            Console.WriteLine($"Bitrate: {file.Info.Bitrate / 1000.0} kb/s");
            var info = file.Video.Info;
            Console.WriteLine($"Duration: {info.Duration}");
            Console.WriteLine($"Frames count: {info.NumberOfFrames?.ToString() ?? "N/A"}");
            var frameRateInfo = info.IsVariableFrameRate ? "average" : "constant";
            Console.WriteLine($"Frame rate: {info.AvgFrameRate} fps ({frameRateInfo})");
            Console.WriteLine($"Frame size: {info.FrameSize}");
            Console.WriteLine($"Pixel format: {info.PixelFormat}");
            Console.WriteLine($"Codec: {info.CodecName}");
            Console.WriteLine($"Is interlaced: {info.IsInterlaced}");

            return file;
        }

        class MediaFileHeader 
        {
            public MediaFileHeader(string fileName, MediaFile mediaFile, MediaFrame[] frames, MediaTrack[] tracks, byte threshold, double contentArea, int accuraciy)
            {
                FileName = fileName;
                MediaFile = mediaFile;
                MediaFrames = frames;
                MediaTracks = tracks;
                Threshold = threshold;
                ContentArea = contentArea;
                Accuraciy = accuraciy;
            }

            public string 文件名 => Path.GetFileName(FileName);

            public string 时长 => MediaFile.Video.Info.Duration.ToString(@"hh\:mm\:ss");


            [Browsable(false)]
            internal double ContentArea { get; }

            [Browsable(false)]
            internal int Accuraciy { get; }

            [Browsable(false)]
            internal byte Threshold { get; }

            [Browsable(false)]
            internal MediaFile MediaFile { get; }

            [Browsable(false)]
            internal string FileName { get; }

            [Browsable(false)]
            internal MediaFrame[] MediaFrames { get; }

            [Browsable(false)]
            internal MediaTrack[] MediaTracks { get; }
        }

        class AudioTrackComparable : IComparable<float>
        {
            private float mValue;

            public AudioTrackComparable(float value) 
            {
                mValue = value;
            }

            public int CompareTo(float other)
            {
                if (Math.Abs(other) <= mValue) return -1;

                return 1;
            }
        }

        private Rect ToRect(Rectangle rectangle) 
        {
            return new Rect(rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height);
        }

        private void PART_Import_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Multiselect = true;
            openFileDialog.Filter = $"视频文件|{MediaExtands}";
            if (openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK) 
            {
                ProgressWindow prompt = new ProgressWindow();
                prompt.TotalCount = openFileDialog.FileNames.Length;
                Task task = new Task(new Action<object>(sender =>
                {
                    var contentAreaValue = 0.2d;
                    var thresholdValue = (byte)220;
                    var accuraciyValue = 6;
                    var orcEnabled = false;
                    var noiseEnabled = true;
                    var vaildPixels = 5;
                    var audioRange = 0.1;

                    Dispatcher.Invoke(() => 
                    {
                        contentAreaValue = ContentArea;
                        thresholdValue = Threshold;
                        accuraciyValue = Accuraciy;
                        orcEnabled = PART_ORC.IsChecked ?? false;
                        noiseEnabled = PART_NOISE.IsChecked ?? true;
                        vaildPixels = (int)PART_VaildPixel.Value;
                        audioRange = PART_SubAudio.Value;
                    });

                    var audioComparable = new AudioTrackComparable((float)audioRange);
                    var fileNames = sender as string[];
                    for (int z = 0; z < fileNames.Length; z++)
                    {
                        string fileName = fileNames[z];

                        prompt.Message = $"正在处理文件{fileName}";

                        try
                        {
                            prompt.Count = z;     

                            var media = OpenMediaFile(fileName);
                            /*
                            if (media.Video.Info.AvgFrameRate > 25) 
                            {
                                media.Dispose();
                                var moveFile = Path.GetTempFileName();
                                File.Move(fileName, moveFile, true);
                                File.Delete(fileName);
                                FFmpeg.Run($"-y -r {media.Video.Info.AvgFrameRate} -i \"{moveFile}\" -r {25} \"{fileName}\"");
                                media = OpenMediaFile(fileName);
                            }
                            */

                            // 对媒体进行字母式分段处理
                            var audioFrameCount = media.Audio?.Info?.NumberOfFrames ?? 0;
                            var vedioFrameCount = media.Video.Info?.NumberOfFrames ?? 0;
                            var audioDuration = (media.Audio?.Info?.Duration.TotalMilliseconds ?? 0) / audioFrameCount;
                            var vedioDuration = (media.Video.Info?.Duration.TotalMilliseconds ?? 0) / vedioFrameCount;
                            var frames = new MediaFrame[vedioFrameCount];
                            prompt.TotalValue = vedioFrameCount + 10;

                            for (var i = 0; i < vedioFrameCount; i++)
                            {
                                prompt.Value = i;
                                ImageData imgData = default;
                                AudioData audData = default;
                                int audioFrame = 0;

                                var hasVedio = media.Video.TryGetNextFrame(out imgData);
                                var hasAudio = media.Audio == null ? false : media.Audio.TryGetFrame(TimeSpan.FromMilliseconds(i * vedioDuration), out audData);
                                if (hasAudio)
                                {
                                    var tempAudio = false;
                                    for (uint x = 0; x < audData.NumChannels; x++)
                                    {
                                        if (audData.GetChannelData(x).BinarySearch(audioComparable) != -1)
                                        {
                                            tempAudio = true;
                                            break;
                                        }
                                    }

                                    hasAudio = tempAudio;
                                    audioFrame = (int)((i * vedioDuration) / media.Audio.Info?.Duration.TotalMilliseconds * audioFrameCount);
                                }

                                var targetWidth = imgData.ImageSize.Width;
                                var targetHeight = imgData.ImageSize.Height;
                                var contentHeight = (int)(targetHeight * contentAreaValue);
                                var contentArea = new Rectangle(0, targetHeight - contentHeight, targetWidth, contentHeight);

                                // 获取当前图像平方值
                                var accuracy = hasVedio ? GetAccuracy(imgData, thresholdValue, contentArea, accuraciyValue, vaildPixels) : null;
                                var flags = (hasAudio ? MediaFrameFlags.Audio : MediaFrameFlags.None) | (hasVedio ? MediaFrameFlags.Vedio : MediaFrameFlags.None);
                                if (HasSubtitles(accuracy))
                                    flags = flags | MediaFrameFlags.Subtitles;

                                var timeOffset = media.Video.Position;
                                if (i != 0 && (timeOffset - frames[i - 1].TimeOffset) <= TimeSpan.Zero)
                                    timeOffset = frames[i - 1].TimeOffset + TimeSpan.FromMilliseconds(vedioDuration);

                                frames[i] = new MediaFrame(
                                    timeOffset,
                                    i,
                                    audioFrame,
                                    accuracy,
                                    flags);

                                media.Video.DiscardBufferedData();

                                if (media.Audio != null)
                                media.Audio.DiscardBufferedData();
                            }

                            // 计算轨道分段
                            var tracks = new List<MediaTrack>();
                            var curr = frames[0];
                            for (var i = 1; i < frames.Length; i++)
                            {
                                var next = frames[i];
                                if (!CanCombine(curr, next))
                                {
                                    var flags = curr.Flags | frames[i - 1].Flags;
                                    if (HasAudio(frames, curr.VedioFrame, next.VedioFrame))
                                        flags = flags | MediaFrameFlags.Audio;

                                    tracks.Add(new MediaTrack(curr.VedioFrame, next.VedioFrame, flags));
                                    curr = next;
                                }
                            }

                            if (curr.VedioFrame != frames[frames.Length - 1].VedioFrame)
                            {
                                var next = frames[frames.Length - 1];
                                var flags = curr.Flags | next.Flags;
                                if (HasAudio(frames, curr.VedioFrame, next.VedioFrame))
                                    flags = flags | MediaFrameFlags.Audio;

                                tracks.Add(new MediaTrack(curr.VedioFrame, next.VedioFrame, flags));
                            }

                            // 剔除噪音
                            if (noiseEnabled)
                            {
                                var loop = true;
                                while (loop)
                                {
                                    loop = false;
                                    for (var i = 0; i < tracks.Count; i++)
                                    {
                                        var track = tracks[i];
                                        if (track.EndFrame - track.StartFrame <= 1)
                                        {
                                            if (i != 0 && i != tracks.Count - 1)
                                            {
                                                var currTrack = tracks[i - 1];
                                                var nextTrack = tracks[i + 1];

                                                if (CanCombine(frames[currTrack.EndFrame], frames[nextTrack.StartFrame]))
                                                {
                                                    tracks.Insert(i, new MediaTrack(currTrack.StartFrame, nextTrack.EndFrame, currTrack.Flags | track.Flags | nextTrack.Flags));
                                                    tracks.Remove(currTrack);
                                                    tracks.Remove(nextTrack);
                                                    i--;

                                                    loop = true;
                                                }
                                            }

                                            tracks.Remove(track);
                                            i--;
                                        }
                                    }
                                }
                            }

                            // 对分段进行orc二次校验
                            if (orcEnabled)
                            {
                                IronTesseract orc = new IronTesseract();
                                orc.Language = OcrLanguage.ChineseSimplifiedBest;

                                string lastText = null;
                                for (var i = 0; i < tracks.Count; i++)
                                {
                                    var track = tracks[i];
                                    using (OcrInput input = new OcrInput())
                                    {
                                        ImageData bmp = default;
                                        var offset = TimeSpan.FromMilliseconds(track.StartFrame * vedioDuration);
                                        var hasVedio = media.Video.TryGetFrame(offset, out bmp);
                                        if (hasVedio)
                                        {
                                            var rects = GetSubtitleBounds(bmp, thresholdValue, contentAreaValue, accuraciyValue, vaildPixels).ToArray();
                                            if (rects.Length > 0)
                                            {
                                                var rect = rects[0];
                                                for (var x = 1; x < rects.Length; x++)
                                                    rect = Rectangle.Union(rect, rects[x]);

                                                input.AddImage(bmp.ToBitmap(), rect);
                                                media.Video.DiscardBufferedData();
                                                var text = orc.Read(input).Text;

                                                if (i != 0)
                                                {
                                                    if (lastText == text)
                                                    {
                                                        // 合并
                                                        var flags = track.Flags;
                                                        if (!HasSubtitles(frames[track.StartFrame].Accuracy))
                                                            flags = flags ^ MediaFrameFlags.Subtitles;

                                                        var lastTrack = tracks[i - 1];
                                                        if (lastTrack.Flags == flags)
                                                        {
                                                            tracks.Insert(i, new MediaTrack(lastTrack.StartFrame, track.EndFrame, lastTrack.Flags));
                                                            tracks.Remove(track);
                                                            tracks.Remove(lastTrack);
                                                            i--;
                                                        }
                                                    }
                                                }

                                                lastText = text;
                                            }
                                            else 
                                            {
                                                lastText = string.Empty;
                                            }
                                        }
                                    }
                                }


                            }

                            var header = new MediaFileHeader(fileName, media, frames, tracks.ToArray(), thresholdValue, contentAreaValue, accuraciyValue);
                            this.Dispatcher.Invoke(new Action<object>((sender) =>
                            {
                                mHeader.Add(sender as MediaFileHeader);
                            }), header);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Load media file '{fileName}' failed.");
                            Console.WriteLine(ex.Message);
                        }
                    }

                    this.Dispatcher.Invoke(new Action(() =>
                    {
                        prompt.Close();
                    }));

                }), openFileDialog.FileNames);
                
                task.Start();
                prompt.Owner = this;
                prompt.ShowDialog();
            }
        }

        private unsafe void PART_Export_Click(object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog folderBrowser = new FolderBrowserDialog();
            if (folderBrowser.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                ProgressWindow prompt = new ProgressWindow();
                prompt.TotalCount = mHeader.Count;
                Task task = new Task(new Action<object>(sender =>
                {
                    var igroneAudio = false;
                    var igroneSubtitles = false;

                    this.Dispatcher.Invoke(() => 
                    {
                        igroneAudio = PART_Export_Opations_HasAudio.IsChecked ?? false;
                        igroneSubtitles = PART_Export_Opations_HasSubtitle.IsChecked ?? false;
                    });

                    var headers = sender as ObservableCollection<MediaFileHeader>;   
                    for (var i = 0; i < headers.Count; i++)
                    {
                        prompt.Count = i;

                        var header = headers[i];
                        prompt.Message = $"正在处理文件{header.FileName}";
                        prompt.TotalValue = header.MediaTracks.Length;
                        for (var j = 0; j < header.MediaTracks.Length; j++)
                        {
                            prompt.Value = j;
                            var track = header.MediaTracks[j];

                            var export = (igroneAudio || track.HasAudio) && (igroneSubtitles || track.HasSubtitles);
                            if (export && track.HasVedio)
                            {
                                var path = Path.Combine(folderBrowser.SelectedPath, Path.GetFileNameWithoutExtension(header.FileName) + "." + j.ToString() + ".mp4");
                                if (File.Exists(path))
                                    File.Delete(path);

                                var startFrame = header.MediaFrames[track.StartFrame];
                                var endFrame = header.MediaFrames[track.EndFrame];
                                var length = endFrame.TimeOffset - startFrame.TimeOffset;
                                if (length > TimeSpan.Zero)
                                {
                                    FFmpeg.Run($"-ss {startFrame.TimeOffset} -i \"{header.FileName}\" -vcodec copy -acodec copy -t {length} \"{path}\"");
                   
                                    var tempFile = Path.GetTempFileName();
                                    foreach (var cmd in GetAdditionals()) 
                                    {
                                        // 移动原视频到指定位置
                                        File.Move(path, tempFile, true);

                                        var cmdStr = cmd.Template.
                                            Replace("{INPUT}", tempFile).
                                            Replace("{OUTPUT}", path).
                                            Replace("{X}", $"{cmd.X / PART_Preview.PART_Canvas.ActualWidth * header.MediaFile.Video.Info.FrameSize.Width}").
                                            Replace("{Y}", $"{cmd.Y / PART_Preview.PART_Canvas.ActualHeight * header.MediaFile.Video.Info.FrameSize.Height}").
                                            Replace("{FONTSIZE}", $"{header.MediaFile.Video.Info.FrameSize.Width / PART_Preview.PART_Image.ActualWidth * cmd.FontSize}");

                                        FFmpeg.Run(cmdStr);
                                    }
                                }
                            }
                        }
                    }

                    this.Dispatcher.Invoke(new Action(() =>
                    {
                        prompt.Close();
                    }));
                }), mHeader);

                task.Start();
                prompt.Owner = this;
                prompt.ShowDialog();

                // 打开输出文件夹
                System.Diagnostics.Process.Start("Explorer.exe", folderBrowser.SelectedPath);
            }
        }

        private IEnumerable<Additional> GetAdditionals() 
        {
            return PART_Preview.GetAdditionals();
        }

        private void PART_Export_Audio_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Multiselect = true;
            openFileDialog.Filter = $"视频文件|{MediaExtands}";
            if (openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                ProgressWindow prompt = new ProgressWindow();
                prompt.TotalCount = openFileDialog.FileNames.Length;
                Task task = new Task(new Action<object>(sender =>
                {
                    var fileNames = sender as string[];
                    for (var i = 0; i < fileNames.Length; i++) 
                    { 
                        var fileName = fileNames[i];
                        prompt.Count = i;
                        prompt.Message = $"正在处理文件{fileName}";
                        var output = Path.GetDirectoryName(fileName);
                        var outputFileName = Path.Combine(output, Path.GetFileNameWithoutExtension(fileName) + ".m4a");
                        if (File.Exists(outputFileName))
                            File.Delete(outputFileName);

                        FFmpeg.Run($"-i \"{fileName}\" -acodec copy -vn -y \"{outputFileName}\"");
                    }

                    this.Dispatcher.Invoke(new Action(() =>
                    {
                        prompt.Close();
                    }));

                }), openFileDialog.FileNames);

                task.Start();
                prompt.Owner = this;
                prompt.ShowDialog();
            }
        }

        private void PART_Union_Media_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Multiselect = true;
            openFileDialog.Filter = $"视频文件|{MediaExtands}";
            if (openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                if (openFileDialog.FileNames.Length < 2)
                    return;

                ProgressWindow prompt = new ProgressWindow();
                prompt.TotalCount = openFileDialog.FileNames.Length;
                Task task = new Task(new Action<object>(sender =>
                {
                    // 首先创建临时目录
                    var folder = Path.GetTempPath();
                    var sources = new List<string>();
                    var fileNames = sender as string[];
                    for (var i = 0; i < fileNames.Length; i++)
                    {
                        var fileName = fileNames[i];
                        prompt.Count = i;
                        prompt.Message = $"正在处理文件{fileName}";
          
                        var outputFileName = Path.Combine(folder, Path.GetFileNameWithoutExtension(fileName) + ".ts");
                        if (File.Exists(outputFileName))
                            File.Delete(outputFileName);

                        sources.Add(Path.GetFileNameWithoutExtension(fileName) + ".ts");
                        FFmpeg.Run($"-i \"{fileName}\" -vcodec copy -acodec copy -vbsf h264_mp4toannexb {outputFileName}");
                    }

                    var fileList = Path.Combine(folder, "fileList.txt");
                    var sb = new StringBuilder();
                    foreach (var src in sources)
                        sb.AppendLine($"file '{src}'");

                    File.WriteAllText(fileList, sb.ToString());

                    var coutput = Path.Combine(Path.GetDirectoryName(fileNames[0]), Path.GetFileNameWithoutExtension(fileNames[0]) + ".combine.mp4");
                    FFmpeg.Run($"-f concat -i {fileList} -acodec copy -vcodec copy -absf aac_adtstoasc {coutput}");

                    this.Dispatcher.Invoke(new Action(() =>
                    {
                        prompt.Close();
                    }));

                }), openFileDialog.FileNames);

                task.Start();
                prompt.Owner = this;
                prompt.ShowDialog();

                // 打开输出文件夹
                if (openFileDialog.FileNames.Length > 1)
                    System.Diagnostics.Process.Start("Explorer.exe", Path.GetDirectoryName(openFileDialog.FileNames[0]));
            }
        }

        private void PART_Vedio_Trun_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Multiselect = true;
            openFileDialog.Filter = $"视频文件|{MediaExtands}";
            if (openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                ProgressWindow prompt = new ProgressWindow();
                prompt.TotalCount = openFileDialog.FileNames.Length;
                Task task = new Task(new Action<object>(sender =>
                {
                    var fileNames = sender as string[];
                    for (var i = 0; i < fileNames.Length; i++)
                    {
                        var fileName = fileNames[i];
                        prompt.Count = i;
                        prompt.Message = $"正在处理文件{fileName}";
                        var output = Path.GetDirectoryName(fileName);
                        var outputFileName = Path.Combine(output, Path.GetFileNameWithoutExtension(fileName) + ".transpose.mp4");
                        if (File.Exists(outputFileName))
                            File.Delete(outputFileName);

                        var media = OpenMediaFile(fileName);
                        if (media != null)
                        {
                            var width = media.Video.Info.FrameSize.Width;
                            var height = media.Video.Info.FrameSize.Height;

                            FFmpeg.Run($"-i \"{fileName}\" -vf scale={height}:-1,pad={height}:{width}:0:{height - width}:black -y \"{outputFileName}\"");
                        }
                    }

                    this.Dispatcher.Invoke(new Action(() =>
                    {
                        prompt.Close();
                    }));

                }), openFileDialog.FileNames);

                task.Start();
                prompt.Owner = this;
                prompt.ShowDialog();
            }
        }

        private void PART_Vedio_Setpts_Click(object sender, RoutedEventArgs e)
        {
            if (PART_FrameRate.Value == 1)
                return;

            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Multiselect = true;
            openFileDialog.Filter = $"视频文件|{MediaExtands}";
            if (openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                ProgressWindow prompt = new ProgressWindow();
                prompt.TotalCount = openFileDialog.FileNames.Length;
                Task task = new Task(new Action<object>(sender =>
                {
                    double rate = 0.5;
                    Dispatcher.Invoke(() => 
                    {
                        rate = PART_FrameRate.Value;
                    });

                    var pts = $"setpts={1 / rate}*PTS";
                    var atempo = rate > 2 ? $"atempo=2.0,atempo={4 - rate}" : $"atempo={2 - rate}";

                    var fileNames = sender as string[];
                    for (var i = 0; i < fileNames.Length; i++)
                    {
                        var fileName = fileNames[i];
                        prompt.Count = i;
                        prompt.Message = $"正在处理文件{fileName}";
                        var output = Path.GetDirectoryName(fileName);
                        var outputFileName = Path.Combine(output, Path.GetFileNameWithoutExtension(fileName) + $".{rate.ToString().Replace('.' , '_')}x.mp4");
                        if (File.Exists(outputFileName))
                            File.Delete(outputFileName);

                        FFmpeg.Run($"-i \"{fileName}\" -filter_complex \"[0:v]{pts}[v]; [0:a]{atempo}[a]\" -map \"[v]\" -map \"[a]\" \"{outputFileName}\"");
                    }

                    this.Dispatcher.Invoke(new Action(() =>
                    {
                        prompt.Close();
                    }));

                }), openFileDialog.FileNames);

                task.Start();
                prompt.Owner = this;
                prompt.ShowDialog();
            }
        }

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            base.OnRenderSizeChanged(sizeInfo);

            PART_TimeLine_DragTimeLine(null, null);
        }
    }
}
