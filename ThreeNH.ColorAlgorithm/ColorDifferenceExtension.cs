using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThreeNH.ColorAlgorithm
{
    public static class ColorDifferenceExtension
    {
        /// <summary>
        /// 计算DeltaH
        /// </summary>
        /// <param name="sample">试样CIELAB</param>
        /// <param name="target">标样CIELAB</param>
        /// <returns></returns>
        public static double CalculateDeltaH(this CIELAB sample, CIELAB target)
        {
            double dh = sample.h - target.h;
            if (dh > 180.0)
            {
                dh -= 360.0;
            }
            else if (dh < -180.0)
            {
                dh += 360.0;
            }
            return 2.0 * Math.Sqrt(sample.C * target.C) * Math.Sin(RADIANS(dh / 2.0));
        }

        public static int Get555Shade(this CIELAB test,CIELAB standard, float tolL, float tolA, float tolB)
        {
            if (standard.L < 0 || standard.L > 150 || standard.a < -130
                || standard.a > 130 || standard.b < -130 || standard.b > 130
                || test.L < 0 || test.L > 150 || test.a < -130
                || test.a > 130 || test.b < -130 || test.b > 130
                || tolL <= 0 || tolA <= 0 || tolB <= 0)
            {
                // 无效参数
                return 0;
            }

            return get_shade_number(test.L - standard.L, tolL) * 100 +
                get_shade_number(test.a - standard.a, tolA) * 10 +
                get_shade_number(test.b - standard.b, tolB);
        }

        #region Private

        /// <summary>
        /// // 将角度转换为弧度
        /// </summary>
        /// <param name="degrees"></param>
        /// <returns></returns>
        private static float RADIANS(double degrees)
        {
            return ((float)((degrees) / 180.0 * Math.PI));
        }


        /// <summary>
        /// 获取555色调分类的等级
        /// </summary>
        /// <param name="d">试样的L, a, b减标样的差值之一</param>
        /// <param name="tol">容差，必须大于0</param>
        /// <returns></returns>
        private static int get_shade_number(float d, float tol)
        {
            int gradeL;
            if (tol <= 0)
            {
                return 0;
            }

            if (d >= -tol && d < tol)
                gradeL = 5;
            else if (d >= -2 * tol && d < -tol)
                gradeL = 4;
            else if (d >= -3 * tol && d < -2 * tol)
                gradeL = 3;
            else if (d >= -4 * tol && d < -3 * tol)
                gradeL = 2;
            else if (d < -4 * tol)
                gradeL = 1;
            else if (d > tol && d <= 2 * tol)
                gradeL = 6;
            else if (d > 2 * tol && d <= 3 * tol)
                gradeL = 7;
            else if (d > 3 * tol && d <= 4 * tol)
                gradeL = 8;
            else
                gradeL = 9;

            return gradeL;
        }

        #endregion
    }
}
