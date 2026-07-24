using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThreeNH.ColorAlgorithm
{
    // RGB requires device-specific color specifications to convert the
    // CIE pereptual color into device parameters.
    public class RgbColorSystem
    {
        public double xRed { get; set; }
        public double yRed { get; set; }
        public double xGreen { get; set; }
        public double yGreen { get; set; }
        public double xBlue { get; set; }
        public double yBlue { get; set; }
        public double xWhite { get; set; }
        public double yWhite { get; set; }
    }

    // The following define the x and y coordinates of the phosphors and
    // reference white points of various broadcast systems.
    public static class RgbColorSystems
    {
        // [Martindale91] notes that NTSC primaries aren't remotely similar
        // to those used in modern displays.
        //
        // CCIR Report 476-1
        // "Colorimetric Standards in Colour Television"
        // See http://www.igd.fhg.de/icib/tv/ccir/rep_476-1/gen.html
        //
        // CCIR Report 624-4
        // "Characteristics of Television Systems"
        // See http://www.igd.fhg.de/icib/tv/ccir/rep_624/read.html
        public static readonly RgbColorSystem NTSCsystem = new RgbColorSystem
        {
            xRed = 0.67,
            yRed = 0.33,
            xGreen = 0.21,
            yGreen = 0.71,
            xBlue = 0.14,
            yBlue = 0.08,
            xWhite = 0.310,
            yWhite = 0.316
        };

        // CCIR Report 476-1
        // "Colorimetric Standards in Colour Television"
        // See http://www.igd.fhg.de/icib/tv/ccir/rep_476-1/gen.html
        //
        // CCIR Report 624-4
        // "Characteristics of Television Systems"
        // See http://www.igd.fhg.de/icib/tv/ccir/rep_624/read.html
        public static readonly RgbColorSystem PAL_SECAMsystem = new RgbColorSystem
        {
            xRed = 0.64,
            yRed = 0.33,
            xGreen = 0.29,
            yGreen = 0.60,
            xBlue = 0.15,
            yBlue = 0.06,
            xWhite = 0.313,
            yWhite = 0.329
        };

        // CCIR Recommendation 709
        // "Basic Parameter Values for the HDTV Standard for the Studio and for
        // International Programme Exchange"
        // See http=//www.igd.fhg.de/icib/tv/ccir/rec_709/pictures/page-2.tiff
        //
        // EBU Technical Document 3271 {EBU = European Broadcasting Union}
        // "Interlaced version of the 1250/50 HDTV production standard"
        // http=//www.igd.fhg.de/icib/tv/org/ebu/td_3271/pictures/page5.tiff
        public static readonly RgbColorSystem EBUsystem = new RgbColorSystem
        {
            xRed = 0.640,
            yRed = 0.330,
            xGreen = 0.300,
            yGreen = 0.600,
            xBlue = 0.150,
            yBlue = 0.060,
            xWhite = 0.3127,
            yWhite = 0.3290
        };

        // SMPTE 240M {SMPTE = The Society of Motion Picture and Television Engineers}
        // "Signal Parameters -- 1125-line High-Definition Production System"
        // http=//www.igd.fhg.de/icib/tv/org/smpte/st_240M-1992/read.html
        public static readonly RgbColorSystem SMPTEsystem = new RgbColorSystem
        {
            xRed = 0.630,
            yRed = 0.340,
            xGreen = 0.310,
            yGreen = 0.595,
            xBlue = 0.155,
            yBlue = 0.070,
            xWhite = 0.3127,
            yWhite = 0.3291    // Illuminant D65
        };

        public static readonly RgbColorSystem ShortPersistencePhosphors = new RgbColorSystem // [Foley96, p. 583]
        {
            xRed = 0.61,
            yRed = 0.35,
            xGreen = 0.29,
            yGreen = 0.59,
            xBlue = 0.15,
            yBlue = 0.063,
            xWhite = 0.3101,
            yWhite = 0.3162    // Illuminant C
        };

        public static readonly RgbColorSystem LongPersistencePhosphors = new RgbColorSystem // [Foley96, p. 583]
        {
            xRed = 0.62,
            yRed = 0.33,
            xGreen = 0.21,
            yGreen = 0.685,
            xBlue = 0.15,
            yBlue = 0.063,
            xWhite = 0.3101,
            yWhite = 0.3162     // Illuminant C
        };

        public static readonly RgbColorSystem DellPhosphors = new RgbColorSystem // 12 Jan 99 E-mail from Dell
        {
            xRed = 0.625,
            yRed = 0.340, // All Dell monitors except
            xGreen = 0.275,
            yGreen = 0.605, // Nokia 91862
            xBlue = 0.150,
            yBlue = 0.065,
            xWhite = 0.3127,
            yWhite = 0.3291 // Illuminant D65
        };
    }

    public static class XYZtoRGBConversion
    {
        public static double Clamp(double value, double low, double hight)
        {
            if (value < low)
                return low;
            if (value > hight)
                return hight;
            return value;
        }

        // Given an additive tricolor system, defined by the  CIE  x  and  y
        // chromaticities  of  its  three  primaries (z is derived trivially as
        // 1-x-y, and a desired chromaticity (XC,  YC,  ZC)  in  CIE  space,
        // determine  the  contribution of each primary in a linear combination
        // which  sums  to  the  desired  chromaticity.    If   the   requested
        // chromaticity falls outside the Maxwell triangle (color gamut) formed
        // by the three primaries, one of the  r,  g,  or  b  weights  will  be
        // negative.   Use  InsideGamut to  test  for  a  valid  color  and
        // ConstrainRGB to desaturate an outside-gamut color to the  closest
        // representation within the available gamut.
        public static void XYZtoRGB(RgbColorSystem rgbColorSystem,
            double xc, double yc, double zc,
            out double R, out double G, out double B)
        {
            var xr = rgbColorSystem.xRed;
            var yr = rgbColorSystem.yRed;
            var zr = 1.0 - xr - yr;

            var xg = rgbColorSystem.xGreen;
            var yg = rgbColorSystem.yGreen;
            var zg = 1.0 - xg - yg;

            var xb = rgbColorSystem.xBlue;
            var yb = rgbColorSystem.yBlue;
            var zb = 1.0 - xb - yb;
            // See Equation 13.29 in [Foley96, p. 587].  The following solves those
            // equations using Cramer's Rule.

            // Optimization could reduce number of operations here.
            var d = xr * yg * zb - xg * yr * zb - xr * yb * zg + xb * yr * zg + xg * yb * zr - xb * yg * zr;
            R = (-xg * yc * zb + xc * yg * zb + xg * yb * zc - xb * yg * zc - xc * yb * zg + xb * yc * zg) / d;
            G = (xr * yc * zb - xc * yr * zb - xr * yb * zc + xb * yr * zc + xc * yb * zr - xb * yc * zr) / d;
            B = (xr * yg * zc - xg * yr * zc - xr * yc * zg + xc * yr * zg + xg * yc * zr - xc * yg * zr) / d;

            //return new Tuple<double, double, double>(R, G, B);
        }

        // Test  whether  a requested color is within the gamut achievable with
        // the primaries of the current colour system.  This amounts  simply  to
        // testing whether all the primary weights are non-negative.
        public static bool InsideGamut(double R, double G, double B)
        {
            return R >= 0.0 && G >= 0.0 && B >= 0.0;
        }

        // If  the  requested RGB shade contains a negative weight for one of
        // the primaries, it lies outside the color gamut accessible from the
        // given triple of primaries.  Desaturate it by mixing with the white
        // point of the color system so as to reduce the  primary  with the
        // negative weight to zero.  This is equivalent to finding the
        // intersection on the CIE diagram of a line drawn between the white
        // point and the requested color with the edge of the  Maxwell
        // triangle formed by the three primaries.
        public static bool ConstrainRGB(RgbColorSystem rgbColorSystem,
            ref double x, ref double y, ref double z,
            ref double R, ref double G, ref double B)
        {
            //  Is the contribution of one of the primaries negative ?
            if (InsideGamut(R, G, B))
                return false; // Color is within gamut

            // Color modified to fit RGB gamut

            // Yes:  Find the RGB mixing weights of the white point (we
            //       assume the white point is in the gamut!).

            XYZtoRGB(rgbColorSystem,
                rgbColorSystem.xWhite,
                rgbColorSystem.yWhite,
                1.0 - rgbColorSystem.xWhite - rgbColorSystem.yWhite,
                out var wr, out var wg, out var wb);

            // Find the primary with negative weight and calculate the parameter
            // of the point on the vector from the white point to the original
            // requested color in RGB space.

            double par;
            if ((R < G) && (R < B))
                par = wr / (wr - R);
            else if ((G < R) && (G < B))
                par = wg / (wg - G);
            else
                par = wb / (wb - B);

            // Since XYZ space is a linear transformation of RGB space, we
            // can find the XYZ space coordinates of the point where the
            // edge of the gamut intersects the vector from the white point
            // to the original color by multiplying the parameter in RGB
            // space by the difference vector in XYZ space.

            x = Clamp(rgbColorSystem.xWhite + par * (x - rgbColorSystem.xWhite), 0, 1);
            y = Clamp(rgbColorSystem.yWhite + par * (y - rgbColorSystem.yWhite), 0, 1);
            z = Clamp(1 - x - y, 0, 1);

            //  Now finally calculate the gamut-constrained RGB weights.
            R = Clamp(wr + par * (R - wr), 0, 1);
            G = Clamp(wg + par * (G - wg), 0, 1);
            B = Clamp(wb + par * (B - wb), 0, 1);

            return true;
        }

        /// <summary>
        /// 归一化RGB的值为0到255
        /// </summary>
        /// <param name="r">XYZtoRGB转换出的R值</param>
        /// <param name="g">XYZtoRGB转换出的G值</param>
        /// <param name="b">XYZtoRGB转换出的B值</param>
        /// <param name="gamma">gamma校正值</param>
        /// <returns>归化后的RGB值</returns>
        public static Tuple<int, int, int> NormalizeRgb(double r, double g, double b, double gamma = 1.0)
        {
            r = XYZtoRGBConversion.Clamp(r, 0.0, 1.0);
            g = XYZtoRGBConversion.Clamp(g, 0.0, 1.0);
            b = XYZtoRGBConversion.Clamp(b, 0.0, 1.0);

            int rgbtRed, rgbtGreen, rgbtBlue;
            var maxRGB = new[] { r, g, b }.Max();
            if (Math.Abs(gamma - 1.00) < 0.0001)
            {
                rgbtRed = (int)Math.Round(255.0 * r / maxRGB);
                rgbtGreen = (int)Math.Round(255.0 * g / maxRGB);
                rgbtBlue = (int)Math.Round(255.0 * b / maxRGB);
            }
            else
            {
                rgbtRed = (int)Math.Round(255.0 * Math.Pow(r / maxRGB, gamma));
                rgbtGreen = (int)Math.Round(255.0 * Math.Pow(g / maxRGB, gamma));
                rgbtBlue = (int)Math.Round(255.0 * Math.Pow(b / maxRGB, gamma));
            }

            return new Tuple<int, int, int>(rgbtRed, rgbtGreen, rgbtBlue);
        }

        /// <summary>
        /// 将CIE XYZ转换为RGB
        /// </summary>
        /// <param name="rgbColorSystem"></param>
        /// <param name="X">CIEXYZ-X</param>
        /// <param name="Y">CIEXYZ-Y</param>
        /// <param name="Z">CIEXYZ-Z</param>
        /// <param name="gamma">gamma校正值</param>
        /// <returns>Item1、Item2、Item3分别返回归一化到0~255的R,G,B</returns>
        public static Tuple<int, int, int> XYZtoNormalizedRGB(
            RgbColorSystem rgbColorSystem,
            double X, double Y, double Z,
            double gamma = 1.0)
        {
            var sum = X + Y + Z;
            if (sum < 0.00001) return new Tuple<int, int, int>(0, 0, 0);

            XYZtoRGB(rgbColorSystem, X / sum, Y / sum, Z / sum, out var r, out var g, out var b);
            return NormalizeRgb(r, g, b, gamma);
        }
    }
}
