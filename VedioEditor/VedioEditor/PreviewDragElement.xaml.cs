using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace VedioEditor
{
    /// <summary>
    /// PreviewDragElement.xaml 的交互逻辑
    /// </summary>
    public partial class PreviewDragElement : UserControl
    {
        private Point mDownPoint;
        private Point mOriginPoint;
        private string mFFMpegCommandTemplate;
        private UIElement mFFMpegElement;

        public Additional FFMpegCommand 
        {
            get 
            {
                var x = Canvas.GetLeft(this) + 3;
                var y = Canvas.GetTop(this) + 12;

                if (mFFMpegElement is TextBox tb)
                    return new Additional(mFFMpegCommandTemplate.Replace("{TEXT}", tb.Text), x, y, tb.FontSize);

                return new Additional(mFFMpegCommandTemplate, x, y, 0);
            }
        }

        public PreviewDragElement(string fileName)
        {
            InitializeComponent();

            mFFMpegElement = new Image() { Source = new BitmapImage(new Uri(fileName)) };
            Grid.SetRow(mFFMpegElement, 1);
            Root.Children.Add(mFFMpegElement);

            mFFMpegCommandTemplate = $"-i \"{{INPUT}}\" -vf \"movie=\"{fileName.Replace(@":\", @"\[kac]\:/").Replace("\\", "/").Replace(@"/[kac]/:/", @"\\:/")}\"[watermark];[in][watermark]overlay={{X}}:{{Y}}[out]\" \"{{OUTPUT}}\"";
        }

        public PreviewDragElement(string text, FontFamily family, double fontSize, FontStretch stretch, FontStyle style, FontWeight weight, SolidColorBrush foreground) 
        {
            InitializeComponent();

            mFFMpegElement = new TextBox()
            {
                Background = Brushes.Transparent,
                BorderBrush = Brushes.Transparent,
                Text = text,
                FontFamily = family,
                FontSize = fontSize,
                //FontWeight = weight,
                //FontStyle = style,
                //FontStretch = stretch,
                Foreground = foreground
            };

            Grid.SetRow(mFFMpegElement, 1);
            Root.Children.Add(mFFMpegElement);
            var color = foreground.Color;
            int colorValue = ((color.A << 24) | (color.R << 16) | (color.G << 8) | color.B) & 0xffffff;
            string colorHex = string.Format("0x{0:x6}", colorValue);
            mFFMpegCommandTemplate = $"-i \"{{INPUT}}\" -vf \"drawtext=fontfile='{family.Source}': text='{{TEXT}}':x={{X}}:y={{Y}}:fontsize={{FONTSIZE}}:fontcolor={colorHex}:shadowy=0\" \"{{OUTPUT}}\"";
        }

        private void AduSysButton_Click(object sender, RoutedEventArgs e)
        {
            (Parent as Canvas).Children.Remove(this);
        }

        private void Border_MouseDown(object sender, MouseButtonEventArgs e)
        {
            mDownPoint = e.GetPosition(Parent as Canvas);
            mOriginPoint = new Point(Canvas.GetLeft(this), Canvas.GetTop(this));
        }

        private void Border_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed) 
            {
                var move = e.GetPosition(Parent as Canvas);
                var offset = move - mDownPoint;
                var target = mOriginPoint + offset;

                Canvas.SetLeft(this, target.X);
                Canvas.SetTop(this, target.Y);
            }
        }

        private void Root_MouseEnter(object sender, MouseEventArgs e)
        {
            PART_Border.Visibility = Visibility.Visible;
        }

        private void Root_MouseLeave(object sender, MouseEventArgs e)
        {
            PART_Border.Visibility = Visibility.Collapsed;
        }
    }
}
