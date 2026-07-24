using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;

namespace ThreeNH.ColorQualityControl.Chromaticity
{
    public class CIEChromaticity
    {
        /// <summary>
        /// 绘制CIE 1931 XYZ Diagram，其坐标范围为： x: [-0.1, 0.8], y: [0.0, 0.9]
        /// </summary>
        /// <param name="chartSize">图片的尺寸</param>
        /// <param name="observer">观察者</param>
        /// <param name="rgbColorSystem">RGB色空间</param>
        /// <param name="gamma">gamma校正系数</param>
        /// <param name="backgroundColor">背景色</param>
        /// <param name="showFullChart">如果为True显示整个图</param>
        /// <returns>色品图</returns>
        public static Bitmap CIE1931xyChart(
            CIEStandardObserver observer = CIEStandardObserver.CIEStandardObserver1964,
            RgbColorSystem rgbColorSystem = null,
            double gamma = 1.0,
            int chartSize = 900,
            bool showFullChart = true,
            Color? backgroundColor = null)
        {
            float xMax = 0.80f;
            float xMin = -0.10f;
            float yMax = 0.90f;
            float yMin = 0.00f;

            float sx = chartSize / (xMax - xMin); // X轴对于图片坐标的缩放比例
            float sy = chartSize / (yMin - yMax); // Y轴对于图片坐标的缩放比例
            float dx = 0 - xMin * sx;             // X轴对于图片坐标的平移量
            float dy = chartSize - yMin * sy;     // Y轴对于图片坐标的平移量

            if (rgbColorSystem == null)
                rgbColorSystem = RgbColorSystems.EBUsystem;


            var bitmap = new Bitmap(chartSize, chartSize, PixelFormat.Format32bppArgb);
            var graphics = Graphics.FromImage(bitmap);

            // 填充背景
            graphics.FillRectangle(new SolidBrush(backgroundColor ?? Color.Transparent), 0, 0, bitmap.Width, bitmap.Height);

            // 填充图上的像素点
            // x, y: 图像的像素坐标
            // r, g, b： x, y处的颜色
            void PlotPoint(int x, int y, double r, double g, double b)
            {
                r = XYZtoRGBConversion.Clamp(r, 0.0, 1.0);
                g = XYZtoRGBConversion.Clamp(g, 0.0, 1.0);
                b = XYZtoRGBConversion.Clamp(b, 0.0, 1.0);

                int rgbtRed, rgbtGreen, rgbtBlue;
                var maxRGB = new[] {r, g, b}.Max();
                if (Math.Abs(gamma - 1.00) < 0.0001)
                {
                    rgbtRed = (int) Math.Round(255.0 * r / maxRGB);
                    rgbtGreen = (int) Math.Round(255.0 * g / maxRGB);
                    rgbtBlue = (int) Math.Round(255.0 * b / maxRGB);
                }
                else
                {
                    rgbtRed = (int) Math.Round(255.0 * Math.Pow(r / maxRGB, gamma));
                    rgbtGreen = (int) Math.Round(255.0 * Math.Pow(g / maxRGB, gamma));
                    rgbtBlue = (int) Math.Round(255.0 * Math.Pow(b / maxRGB, gamma));
                }

                bitmap.SetPixel(x, y, Color.FromArgb(255, rgbtRed, rgbtGreen, rgbtBlue));
            }

            // 获取指定波长对应的轮廓线上的点
            PointF get_outline_point(int wavelength)
            {
                ColorMatchingFunctions.xyChromaticityCoordinates(observer, wavelength, out var x, out var y);
                return new PointF((float) x * sx + dx, (float) y * sy + dy);
            }

            // 图片像素点位置转换为CIE x,y值
            void img_pos_to_xy(int col, int row, out double x, out double y)
            {
                x = (col - dx) / sx;
                y = (row - dy) / sy;
            }

            // 所有轮廓点
            var outlinePoints = (from wavelength in Enumerable.Range(360, 730 - 360 + 1) select get_outline_point(wavelength))
                .ToArray();

            // 绘制轮廓线
            graphics.DrawPolygon(new Pen(Color.Red), outlinePoints);

            // 填充轮廓频色
            for (int rowIndex = 0; rowIndex < bitmap.Height; rowIndex++)
            {
                int sideLeft = 0;
                int sideRight = bitmap.Width - 1;

                // 查找左边界点的位置
                while (sideLeft < bitmap.Width && bitmap.GetPixel(sideLeft, rowIndex).ToArgb() == 0)
                {
                    ++sideLeft;
                }

                // 查找右边界点的位置
                while (sideRight >= 0 && bitmap.GetPixel(sideRight, rowIndex).R == 0)
                {
                    --sideRight;
                }

                if (sideRight < sideLeft)
                {
                    continue;
                }

                // 填充当前行的颜色
                for (int colIndex = sideLeft; colIndex <= sideRight; colIndex++)
                {
                    img_pos_to_xy(colIndex, rowIndex, out var x, out var y);
                    var z = 1.0 - x - y;
                    XYZtoRGBConversion.XYZtoRGB(rgbColorSystem, x, y, z,
                        out var R, out var G, out var B);
                    if (showFullChart || XYZtoRGBConversion.InsideGamut(R, G, B))
                    {
                        PlotPoint(colIndex, rowIndex, R, G, B);
                    }
                }

                // If we're not showing the full chart, make another pass through to
                // "fix" the outline of the horseshoe to be either a spectral color,
                // or a "line of purple" color.  This is somewhat of a kludge, but
                // optimization isn't really necessary.
                if (!showFullChart)
                {
                    for (int colIndex = sideLeft;
                        colIndex <= sideRight && bitmap.GetPixel(colIndex, rowIndex).R > 0;
                        colIndex++)
                    {
                        img_pos_to_xy(colIndex, rowIndex, out var x, out var y);
                        var z = 1.0 - x - y;
                        XYZtoRGBConversion.XYZtoRGB(rgbColorSystem, x, y, z,
                            out var R, out var G, out var B);
                        PlotPoint(colIndex, rowIndex, R, G, B);
                    }

                    for (int colIndex = sideRight;
                        colIndex >= 0 && bitmap.GetPixel(colIndex, rowIndex).R > 0;
                        colIndex--)
                    {
                        img_pos_to_xy(colIndex, rowIndex, out var x, out var y);
                        var z = 1.0 - x - y;
                        XYZtoRGBConversion.XYZtoRGB(rgbColorSystem, x, y, z,
                            out var R, out var G, out var B);
                        PlotPoint(colIndex, rowIndex, R, G, B);
                    }
                }
            }


            return bitmap;
        }

        /// <summary>
        /// 1960 uv Chart or 1976 u'v' Chart
        /// 其中uv的范围：u: [0.0, 6.0], v: [0.0, 6.0]
        /// </summary>
        /// <param name="cieChart">CIEChart1960或CIEChart1971</param>
        /// <param name="observer">观察者</param>
        /// <param name="rgbColorSystem">RGB颜色系统</param>
        /// <param name="gamma">Gamma校准值</param>
        /// <param name="backgroundColor">背景色</param>
        /// <param name="chartSize">尺寸</param>
        /// <param name="showFullChart">是否显示全图</param>
        /// <returns></returns>
        public static Bitmap CIEuvChart(
            CIEChart cieChart,
            CIEStandardObserver observer = CIEStandardObserver.CIEStandardObserver1964,
            RgbColorSystem rgbColorSystem = null,
            double gamma = 1.0,
            int chartSize = 900,
            bool showFullChart = true,
            Color? backgroundColor = null)
        {
            if (cieChart == CIEChart.CIEChart1931)
            {
                throw new ArgumentException("CIE 1931 not supported uv chart", nameof(rgbColorSystem));
            }

            if (rgbColorSystem == null)
                rgbColorSystem = RgbColorSystems.EBUsystem;

            float xMax = 0.60f;
            float xMin = 0.00f;
            float yMax = 0.60f;
            float yMin = 0.00f;

            float sx = chartSize / (xMax - xMin);
            float sy = chartSize / (yMin - yMax);
            float dx = 0 - xMin * sx;
            float dy = chartSize - yMin * sy;

            var bitmap = new Bitmap(chartSize, chartSize, PixelFormat.Format32bppArgb);
            var graphics = Graphics.FromImage(bitmap);
            graphics.FillRectangle(new SolidBrush(backgroundColor ?? Color.Transparent), 0, 0, bitmap.Width, bitmap.Height);

            void PlotPoint(int x, int y, double R, double G, double B)
            {
                R = XYZtoRGBConversion.Clamp(R, 0.0, 1.0);
                G = XYZtoRGBConversion.Clamp(G, 0.0, 1.0);
                B = XYZtoRGBConversion.Clamp(B, 0.0, 1.0);
                var maxRGB = new[] {R, G, B}.Max();
                bitmap.SetPixel(x, y, Color.FromArgb(
                    255,
                    (int) Math.Round(255 * Math.Pow(R / maxRGB, gamma)),
                    (int) Math.Round(255 * Math.Pow(G / maxRGB, gamma)),
                    (int) Math.Round(255 * Math.Pow(B / maxRGB, gamma))
                ));
            }

            PointF get_outline_point(int wavelength)
            {
                ColorMatchingFunctions.uvChromaticityCoordinates(
                    cieChart, observer, wavelength, out var u, out var v);
                return new PointF((float) (u * sx + dx), (float) (v * sy + dy));
            }

            void img_pos_to_uv(int col, int row, out float u, out float v)
            {
                u = (col - dx) / sx;
                v = (row - dy) / sy;
            }

            var outlinePoints = (from wavelength in Enumerable.Range(360, 730-360+1)
                select get_outline_point(wavelength)).ToArray();

            graphics.DrawPolygon(new Pen(Color.Red), outlinePoints);


            for (int rowIndex = 0; rowIndex < bitmap.Height; rowIndex++)
            {
                int sideLeft;
                for (sideLeft = 0;
                    sideLeft < bitmap.Width && bitmap.GetPixel(sideLeft, rowIndex).R == 0;
                    ++sideLeft)
                {
                }

                int sideRight;
                for (sideRight = bitmap.Width - 1;
                    sideRight >= 0 && bitmap.GetPixel(sideRight, rowIndex).R == 0;
                    --sideRight)
                {
                }

                for (int colIndex = sideLeft; colIndex <= sideRight; ++colIndex)
                {
                    img_pos_to_uv(colIndex, rowIndex, out var u, out var v);
                    uv_to_xy(cieChart, u, v, out var x, out var y);
                    var z = 1.0 - x - y;

                    // See comments above in CIE1931Chart as to why x=X, y=Y and z=Z here
                    XYZtoRGBConversion.XYZtoRGB(rgbColorSystem, x, y, z, out var R, out var G, out var B);

                    if (showFullChart || XYZtoRGBConversion.InsideGamut(R, G, B))
                    {
                        PlotPoint(colIndex, rowIndex, R, G, B);
                    }
                }

                if (!showFullChart)
                {
                    for (int colIndex = sideLeft;
                        colIndex < sideRight && bitmap.GetPixel(colIndex, rowIndex).R > 0;
                        ++colIndex)
                    {
                        img_pos_to_uv(colIndex, rowIndex, out var u, out var v);
                        uv_to_xy(cieChart, u, v, out var x, out var y);
                        var z = 1.0 - x - y;
                        XYZtoRGBConversion.XYZtoRGB(rgbColorSystem, x, y, z, out var R, out var G, out var B);
                        PlotPoint(colIndex, rowIndex, R, G, B);
                    }

                    for (int colIndex = sideRight;
                        colIndex >= 0 && bitmap.GetPixel(colIndex, rowIndex).R > 0;
                        --colIndex)
                    {
                        img_pos_to_uv(colIndex, rowIndex, out var u, out var v);
                        uv_to_xy(cieChart, u, v, out var x, out var y);
                        var z = 1.0 - x - y;
                        XYZtoRGBConversion.XYZtoRGB(rgbColorSystem, x, y, z, out var R, out var G, out var B);
                        PlotPoint(colIndex, rowIndex, R, G, B);
                    }
                }
            }

            return bitmap;
        }

        private static void uv_to_xy(CIEChart cieChart, float u, float v, out float x, out float y)
        {
            if (cieChart == CIEChart.CIEChart1960)
            {
                // "Color Theory and Its Application in Art and Design"
                // George A. Agoston, Springer-Verlag, p. 240, 1987
                //
                // "Color in Business, Science and Industry" (3rd edition)
                // Deane B. Judd and Gunter Wyszecki, John Wiley, p. 296, 1975
                // for inverse of these functions
                //
                var denominator = 2 * u - 8 * v + 4;
                x = 3 * u / denominator;
                y = 2 * v / denominator;
            }
            else // 1976
            {
                // Here (u,v) is really (u',v')
                // "Principles of Color Technology" (2nd edition)
                // Fred W. Billmeyer, Jr. and Max Saltzman
                // John Wiley, p. 58, 1981
                var denominator = 18 * u - 48 * v + 36;
                x = 27 * u / denominator;
                y = 12 * v / denominator;
            }
        }
    }
}