using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using WpfColorFontDialog;
using System.Linq;
using UserControl = System.Windows.Controls.UserControl;
using Control = System.Windows.Controls.Control;

namespace VedioEditor
{
    /// <summary>
    /// Watermark.xaml 的交互逻辑
    /// </summary>
    public partial class Preview : UserControl
    {
        private Control _fontControl;

        public ImageSource PreviewSource
        {
            get 
            { 
                return PART_Image.Source; 
            }
            set 
            {
                // 
                var ro = value.Width / value.Height;
                if (ro == 1)
                {
                    PART_Image.Width = PART_Canvas_Parent.ActualWidth;
                    PART_Image.Height = PART_Canvas_Parent.ActualHeight;
                }
                else if (ro > 1)
                {
                    var height = PART_Canvas_Parent.ActualWidth / ro;
                    PART_Image.Width = PART_Canvas_Parent.ActualWidth;
                    PART_Image.Height = height;
                }
                else 
                {
                
                    var height = PART_Canvas_Parent.ActualHeight / ro;
                    PART_Image.Width = height;
                    PART_Image.Height = PART_Canvas_Parent.ActualHeight;
                }

       
                PART_Image.Source = value;
            }
        }

        public Preview()
        {
            InitializeComponent();
            _fontControl = new System.Windows.Controls.Control();
        }

        private void MetroButton_Click(object sender, RoutedEventArgs e)
        {
            //We can pass a bool to choose if we preview the font directly in the list of fonts.
            bool previewFontInFontList = true;
            //True to allow user to input arbitrary font sizes. False to only allow predtermined sizes
            bool allowArbitraryFontSizes = true;


            ColorFontDialog dialog = new ColorFontDialog(previewFontInFontList, allowArbitraryFontSizes);
            dialog.Owner = App.Current.MainWindow;
            dialog.Font = FontInfo.GetControlFont(_fontControl);
            dialog.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            dialog.ShowInTaskbar = false;

            //Optional custom allowed size range
            dialog.FontSizes = new int[] { 2, 4, 6, 8, 10, 12, 14, 16, 18, 20, 22, 24, 26, 28, 30, 32, 34, 36 };

            if (dialog.ShowDialog() == true)
            {
                FontInfo font = dialog.Font;
                if (font != null)
                {
                    FontInfo.ApplyFont(_fontControl, font);
                    _fontControl.Foreground = font.Color.Brush;
                }
            }
        }

        private Random random = new Random();

        private void MetroButton_Click_1(object sender, RoutedEventArgs e)
        {
            var element = new PreviewDragElement("新文本", _fontControl.FontFamily, _fontControl.FontSize, _fontControl.FontStretch, _fontControl.FontStyle, _fontControl.FontWeight, _fontControl.Foreground as SolidColorBrush);
            PART_Canvas.Children.Add(element);
            Canvas.SetLeft(element, random.Next(50, 120));
            Canvas.SetTop(element, random.Next(50, 120));
        }

        private void MetroButton_Click_2(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Multiselect = false;
            openFileDialog.Filter = $"图片文件|*.png";
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                var element = new PreviewDragElement(openFileDialog.FileName);
                PART_Canvas.Children.Add(element);
                Canvas.SetLeft(element, random.Next(50, 120));
                Canvas.SetTop(element, random.Next(50, 120));
            }
        }

        internal IEnumerable<Additional> GetAdditionals()
        {
            IEnumerable<Additional> additionals = Dispatcher.Invoke(new Func<IEnumerable<Additional>>(() =>
            {
                return PART_Canvas.Children.Cast<UIElement>().Where(x => x is PreviewDragElement).Cast<PreviewDragElement>().Select(x => x.FFMpegCommand).ToArray();
            }));

            return additionals;
        }
    }
}
