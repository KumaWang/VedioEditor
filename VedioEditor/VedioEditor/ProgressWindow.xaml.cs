using System;
using System.ComponentModel;
using System.Windows;

namespace VedioEditor
{
    /// <summary>
    /// ProgressWindow.xaml 的交互逻辑
    /// </summary>
    public partial class ProgressWindow : Window
    {
        private int mValue;
        public int Value
        {
            get => mValue;
            set 
            {
                if (mValue != value) 
                {
                    mValue = value;
                    this.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        PART_ProgressBar.Value = value;
                    }));
                }
            }
        }

        private int mTotalValue;
        public int TotalValue 
        {
            get => mTotalValue;
            set
            {
                if (mTotalValue != value)
                {
                    mTotalValue = value;
                    this.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        PART_ProgressBar.Maximum = value;
                    }));
                }
            }
        }

        private int mCount;
        public int Count
        {
            get => mCount;
            set
            {
                if (mCount != value)
                {
                    mCount = value;
                    this.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        PART_ProgressBar2.Value = value;
                    }));
                }
            }
        }

        private int mTotalCount;
        public int TotalCount
        {
            get => mTotalCount;
            set
            {
                if (mTotalCount != value)
                {
                    mTotalCount = value;
                    this.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        PART_ProgressBar2.Maximum = value;
                    }));
                }
            }
        }

        private string mMessage;

        public string Message 
        {
            get { return mMessage; }
            set 
            {
                if (mMessage != value) 
                {
                    mMessage = value;
                    this.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        PART_Message.Content = value;
                    }));
                }
            }
        }

        public ProgressWindow()
        {
            InitializeComponent();

            WindowStyle = WindowStyle.None;
            ResizeMode = ResizeMode.NoResize;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            ShowInTaskbar = false;
        }
    }
}
