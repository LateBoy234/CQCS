using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace ThreeNH.ColorQualityControl
{
    class ContentToImageWindow : Window
    {
        public ContentToImageWindow(object content, int? width = null, int? height = null)
        {
            this.WindowStyle = WindowStyle.None;
            this.Content = content;
            this.SizeToContent = SizeToContent.Height;

            if (width != null)
                this.Width = (int)width;
            if (height != null)
                this.Height = (int)height;
        }

        protected override void OnActivated(EventArgs e)
        {
            base.OnActivated(e);
            DispatcherTimer timer = new DispatcherTimer();
            timer.Tick += OnTimerTick;
            timer.Interval = new TimeSpan(1);
            timer.Start();
        }

        private void OnTimerTick(object sender, EventArgs e)
        {
            (sender as DispatcherTimer).Stop();

            double width = (Content as FrameworkElement).ActualWidth;
            double height = (Content as FrameworkElement).ActualHeight;
            SaveImage(this, (int)width, (int)height);
            Close();
        }

        protected override void OnContentRendered(EventArgs e)
        {
            base.OnContentRendered(e);
            SaveImage(this, (int)ActualWidth, (int)ActualHeight);
        }

        private void SaveImage(Visual visual, int width, int height)
        {
            RenderTargetBitmap bitmap = new RenderTargetBitmap(
                width, height, 96, 96, PixelFormats.Pbgra32);
            bitmap.Render(visual);
            Image = bitmap;
        }

        public BitmapSource Image
        {
            get; private set;
        }
    }
}
