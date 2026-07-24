using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Media.Imaging;
using ThreeNH.ColorQualityControl.Chromaticity;

namespace ThreeNH.ColorQualityControl.ViewModels.Charts
{
    static class ColorDiagrams
    {
        private static readonly Lazy<BitmapImage> LabDiagram = new Lazy<BitmapImage>(CreateLabDiagramWithCoords);

        private static readonly Lazy<BitmapImage> LazyCie1931Diagram10 = new Lazy<BitmapImage>(() =>
            ConvertToImageSource(Chromaticity.CIEChromaticity.CIE1931xyChart(CIEStandardObserver.CIEStandardObserver1964, RgbColorSystems.DellPhosphors, chartSize: 900)));

        private static readonly Lazy<BitmapImage> LazyCie1931Diagram2 = new Lazy<BitmapImage>(() =>
            ConvertToImageSource(Chromaticity.CIEChromaticity.CIE1931xyChart(CIEStandardObserver.CIEStandardObserver1931, RgbColorSystems.DellPhosphors, chartSize: 900)));

        private static double[][] _cie1931DiagramOutlinePoints = new[]
        {
            new[] {0.174110, 0.004960, 81, 0, 250}, // 380nm
	        new[] {0.174010, 0.004980, 81, 0, 250}, // 385nm
	        new[] {0.173800, 0.004920, 81, 0, 250}, // 390nm
	        new[] {0.173560, 0.004920, 80, 0, 250}, // 395nm
	        new[]{0.173340, 0.004800, 80, 0, 250}, // 400nm
	        new[]{0.173020, 0.004780, 80, 0, 250}, // 405nm
	        new[]{0.172580, 0.004800, 79, 0, 250}, // 410nm
            new[]{0.172090, 0.004830, 78, 0, 250}, // 415nm
            new[]{0.171410, 0.005100, 76, 0, 250}, // 420nm
            new[]{0.170300, 0.005790, 74, 0, 250}, // 425nm
            new[]{0.168880, 0.006900, 71, 0, 250}, // 430nm
            new[]{0.166900, 0.008560, 65, 0, 250}, // 435nm
            new[]{0.164410, 0.010860, 58, 0, 250}, // 440nm
            new[]{0.161100, 0.013790, 46, 0, 250}, // 445nm
            new[] {0.156640, 0.017700, 20, 0, 250}, // 450nm
            new[]{0.150990, 0.022740, 0, 0, 250}, // 455nm
            new[] {0.143960, 0.029700, 0, 0, 250}, // 460nm
            new[] {0.135500, 0.039880, 0, 0, 249}, // 465nm
            new[] {0.124120, 0.057800, 0, 49, 248}, // 470nm
            new[] {0.109590, 0.086840, 0, 88, 245}, // 475nm
            new[] {0.091290, 0.132700, 0, 124, 240}, // 480nm
            new[] {0.068710, 0.200720, 0, 159, 232}, // 485nm
            new[] {0.045390, 0.294980, 0, 194, 218}, // 490nm
            new[] {0.023460, 0.412700, 0, 228, 198}, // 495nm
            new[] {0.008170, 0.538420, 0, 255, 172}, // 500nm
            new[] {0.003860, 0.654820, 0, 255, 139}, // 505nm
            new[]{0.013870, 0.750190, 0, 255, 97}, // 510nm
            new[]{0.038850, 0.812020, 0, 255, 22}, // 515nm
            new[]{0.074300, 0.833800, 0, 255, 0}, // 520nm
            new[]{0.114160, 0.826210, 0, 255, 0}, // 525nm
            new[]{0.154720, 0.805860, 0, 255, 0}, // 530nm
            new[]{0.192880, 0.781630, 0, 255, 0}, // 535nm
            new[]{0.229620, 0.754330, 0, 255, 0}, // 540nm
            new[]{0.265780, 0.724320, 0, 255, 0}, // 545nm
            new[]{0.301600, 0.692310, 0, 255, 0}, // 550nm
            new[]{0.337360, 0.658850, 43, 246, 0}, // 555nm
            new[]{0.373100, 0.624450, 120, 235, 0}, // 560nm
            new[] {0.408740, 0.589610, 160, 222, 0}, // 565nm
            new[] {0.444060, 0.554710, 190, 208, 0}, // 570nm
            new[] {0.478770, 0.520200, 214, 193, 0}, // 575nm
            new[] {0.512490, 0.486590, 235, 177, 0}, // 580nm
            new[] {0.544790, 0.454430, 253, 160, 0}, // 585nm
            new[] {0.575150, 0.424230, 255, 141, 0}, // 590nm
            new[] {0.602930, 0.396500, 255, 120, 0}, // 595nm
            new[] {0.627040, 0.372490, 255, 98, 0}, // 600nm
            new[] {0.648230, 0.351390, 255, 70, 0}, // 605nm
            new[] {0.665760, 0.334010, 255, 30, 0}, // 610nm
            new[]{0.680080, 0.319750, 255, 0, 0}, // 615nm
            new[]{0.691500, 0.308340, 255, 0, 0}, // 620nm
            new[]{0.700610, 0.299300, 255, 0, 0}, // 625nm
            new[]{0.707920, 0.292030, 255, 0, 0}, // 630nm
            new[]{0.714030, 0.285930, 255, 0, 0}, // 635nm
            new[]{0.719030, 0.280930, 255, 0, 0}, // 640nm
            new[]{0.723030, 0.276950, 255, 0, 0}, // 645nm
            new[]{0.725990, 0.274010, 255, 0, 0}, // 650nm
            new[] {0.728270, 0.271730, 255, 0, 0}, // 655nm
            new[] {0.729970, 0.270030, 255, 0, 0}, // 660nm
            new[]  {0.731090, 0.268910, 255, 0, 0}, // 665nm
            new[] {0.731990, 0.268010, 255, 0, 0}, // 670nm
            new[]  {0.732720, 0.267280, 255, 0, 0}, // 675nm
            new[] {0.733420, 0.266580, 255, 0, 0}, // 680nm
            new[] {0.734050, 0.265950, 255, 0, 0}, // 685nm
            new[] {0.734390, 0.265610, 255, 0, 0}, // 690nm
            new[] {0.734590, 0.265410, 255, 0, 0}, // 695nm
            new[] {0.734690, 0.265310, 255, 0, 0}, // 700-780nm
	        // 无对应波长的边
            new[] {0.734690, 0.265310, 255, 0, 0},
            new[]  {0.678632, 0.239275, 255, 0, 81},
            new[]  {0.622574, 0.213240, 255, 0, 117},
            new[]  {0.566516, 0.187205, 255, 0, 143},
            new[]  {0.510458, 0.161170, 255, 0, 164},
            new[] {0.454400, 0.135135, 251, 0, 182},
            new[]  {0.398342, 0.109100, 230, 0, 198},
            new[]  {0.342284, 0.083065, 206, 0, 213},
            new[]  {0.286226, 0.057030, 177, 0, 226},
            new[]  {0.230168, 0.030995, 140, 0, 238},
        };

        public static BitmapImage CieLabDiagram => LabDiagram.Value;

        /// <summary>
        /// CIE 1931 10观察者的色品图
        /// </summary>
        public static BitmapImage Cie1931DiagramObserver10 => LazyCie1931Diagram10.Value;
        /// <summary>
        /// CIE 1931 2度观察者的色品图
        /// </summary>
        public static BitmapImage Cie1931DiagramObserver2 => LazyCie1931Diagram2.Value;

        public static BitmapImage ConvertToImageSource(Bitmap src)
        {
            MemoryStream ms = new MemoryStream();

            ((System.Drawing.Bitmap)src).Save(ms, System.Drawing.Imaging.ImageFormat.Png);
            BitmapImage image = new BitmapImage();
            image.BeginInit();
            ms.Seek(0, SeekOrigin.Begin);
            image.StreamSource = ms;
            image.EndInit();
            //src.Save($"H:/{Guid.NewGuid()}.bmp");
            return image;

        }

        static BitmapImage CreateLabDiagramWithCoords()
        {
            const int W = 1000;
            const int H = 1000;
            const int Cx = W / 2;
            const int Cy = H / 2;
            const int InnerR = 450;          // 内圆半径（渐变区域）
            const int OuterR = 480;          // 文字到圆心距离（写在圆外）

            Bitmap bmp = new Bitmap(W, H, PixelFormat.Format32bppArgb);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                // 1. 整图先刷白（外圈背景）
                g.Clear(Color.White);

                // 2. 内圆 800×800 的径向渐变（Lab 色轮）
                RectangleF innerRect = new RectangleF(Cx - InnerR, Cy - InnerR, InnerR * 2, InnerR * 2);
                GraphicsPath path = new GraphicsPath();
                path.AddEllipse(innerRect);

                float tmp = (float)(Math.Sqrt(2.0) / 2.0 * InnerR);
                PathGradientBrush brush = new PathGradientBrush(new[]
                {
            new PointF(Cx + InnerR, Cy),
            new PointF(Cx + tmp,    Cy - tmp),
            new PointF(Cx,          Cy - InnerR),
            new PointF(Cx - tmp,    Cy - tmp),
            new PointF(Cx - InnerR, Cy),
            new PointF(Cx - tmp,    Cy + tmp),
            new PointF(Cx,          Cy + InnerR),
            new PointF(Cx + tmp,    Cy + tmp),
        }, WrapMode.Tile);
                brush.CenterPoint = new PointF(Cx, Cy);
                brush.CenterColor = Color.White;
                brush.SurroundColors = new Color[]
                {
            Color.Red, Color.Orange, Color.Yellow,
            Color.GreenYellow, Color.Green, Color.Cyan,
            Color.Blue, Color.Purple
                };
                g.FillEllipse(brush, innerRect);   // 只画内圆！

                // 3. 在圆外写 a、-a、b、-b
                using (Font font = new Font("Segoe UI", 20, FontStyle.Bold))
                using (StringFormat sf = new StringFormat() { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                {
                    g.DrawString("a*", font, Brushes.Black, Cx + OuterR, Cy-30, sf);   // 右侧
                    g.DrawString("-a*", font, Brushes.Black, Cx - OuterR+20, Cy-30, sf); // 左侧
                    g.DrawString("-b*", font, Brushes.Black, Cx-30, Cy + OuterR, sf);  // 下方
                    g.DrawString("b*", font, Brushes.Black, Cx-30, Cy - OuterR, sf); // 上方
                }
            }
            return ConvertToImageSource(bmp);
        }
    }
}
