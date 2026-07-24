using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ThreeNH.ColorAlgorithm.Basic
{
    class WhitePointPosition
    {
        public static CIEXYZ xyz_A_2_5nm => new CIEXYZ(109.850f, 100.000f, 35.585f);
        public static CIEXYZ xyz_A_10_5nm => new CIEXYZ(111.144f, 100.000f, 35.200f);
        public static CIEXYZ xyz_C_2_5nm => new CIEXYZ(98.074f, 100.000f, 118.232f);
        public static CIEXYZ xyz_C_10_5nm => new CIEXYZ(97.285f, 100.000f, 116.145f);
        public static CIEXYZ xyz_D50_2_5nm => new CIEXYZ(96.422f, 100.000f, 82.521f);
        public static CIEXYZ xyz_D50_10_5nm => new CIEXYZ(96.720f, 100.000f, 81.427f);
        public static CIEXYZ xyz_D55_2_5nm => new CIEXYZ(95.682f, 100.000f, 92.149f);
        public static CIEXYZ xyz_D55_10_5nm => new CIEXYZ(95.799f, 100.000f, 90.926f);
        public static CIEXYZ xyz_D65_2_5nm => new CIEXYZ(95.047f, 100.000f, 108.883f);
        public static CIEXYZ xyz_D65_10_5nm => new CIEXYZ(94.811f, 100.000f, 107.304f);
        public static CIEXYZ xyz_D75_2_5nm => new CIEXYZ(94.972f, 100.000f, 122.638f);
        public static CIEXYZ xyz_D75_10_5nm => new CIEXYZ(94.416f, 100.000f, 120.641f);
        public static CIEXYZ xyz_F1_2_5nm => new CIEXYZ(92.868f, 100.000f, 103.775f);
        public static CIEXYZ xyz_F1_10_5nm => new CIEXYZ(94.815f, 100.000f, 103.255f);
        public static CIEXYZ xyz_F2_2_5nm => new CIEXYZ(99.186f, 100.000f, 67.393f);
        public static CIEXYZ xyz_F2_10_5nm => new CIEXYZ(103.279f, 100.000f, 69.027f);
        public static CIEXYZ xyz_F3_2_5nm => new CIEXYZ(103.799f, 100.000f, 49.932f);
        public static CIEXYZ xyz_F3_10_5nm => new CIEXYZ(109.009f, 100.000f, 52.001f);
        public static CIEXYZ xyz_F4_2_5nm => new CIEXYZ(109.202f, 100.000f, 38.881f);
        public static CIEXYZ xyz_F4_10_5nm => new CIEXYZ(115.013f, 100.000f, 40.999f);
        public static CIEXYZ xyz_F5_2_5nm => new CIEXYZ(90.902f, 100.000f, 98.819f);
        public static CIEXYZ xyz_F5_10_5nm => new CIEXYZ(93.390f, 100.000f, 98.692f);
        public static CIEXYZ xyz_F6_2_5nm => new CIEXYZ(97.343f, 100.000f, 60.260f);
        public static CIEXYZ xyz_F6_10_5nm => new CIEXYZ(102.180f, 100.000f, 62.109f);
        public static CIEXYZ xyz_F7_2_5nm => new CIEXYZ(95.041f, 100.000f, 108.747f);
        public static CIEXYZ xyz_F7_10_5nm => new CIEXYZ(95.792f, 100.000f, 107.686f);
        public static CIEXYZ xyz_F8_2_5nm => new CIEXYZ(96.428f, 100.000f, 82.421f);
        public static CIEXYZ xyz_F8_10_5nm => new CIEXYZ(97.118f, 100.000f, 81.186f);
        public static CIEXYZ xyz_F9_2_5nm => new CIEXYZ(100.380f, 100.000f, 67.944f);
        public static CIEXYZ xyz_F9_10_5nm => new CIEXYZ(102.125f, 100.000f, 67.866f);
        public static CIEXYZ xyz_F10_2_5nm => new CIEXYZ(96.385f, 100.000f, 82.354f);
        public static CIEXYZ xyz_F10_10_5nm => new CIEXYZ(98.956f, 100.000f, 83.286f);
        public static CIEXYZ xyz_F11_2_5nm => new CIEXYZ(100.962f, 100.000f, 64.350f);
        public static CIEXYZ xyz_F11_10_5nm => new CIEXYZ(103.863f, 100.000f, 65.607f);
        public static CIEXYZ xyz_F12_2_5nm => new CIEXYZ(108.117f, 100.000f, 39.275f);
        public static CIEXYZ xyz_F12_10_5nm => new CIEXYZ(111.481f, 100.000f, 40.368f);
        public static CIEXYZ xyz_A_2_10nm => new CIEXYZ(109.850f, 100.000f, 35.585f);
        public static CIEXYZ xyz_A_10_10nm => new CIEXYZ(111.144f, 100.000f, 35.200f);
        public static CIEXYZ xyz_C_2_10nm => new CIEXYZ(98.074f, 100.000f, 118.232f);
        public static CIEXYZ xyz_C_10_10nm => new CIEXYZ(97.285f, 100.000f, 116.145f);
        public static CIEXYZ xyz_D50_2_10nm => new CIEXYZ(96.422f, 100.000f, 82.521f);
        public static CIEXYZ xyz_D50_10_10nm => new CIEXYZ(96.720f, 100.000f, 81.427f);
        public static CIEXYZ xyz_D55_2_10nm => new CIEXYZ(95.682f, 100.000f, 92.149f);
        public static CIEXYZ xyz_D55_10_10nm => new CIEXYZ(95.799f, 100.000f, 90.926f);
        public static CIEXYZ xyz_D65_2_10nm => new CIEXYZ(95.047f, 100.000f, 108.883f);
        public static CIEXYZ xyz_D65_10_10nm => new CIEXYZ(94.811f, 100.000f, 107.304f);
        public static CIEXYZ xyz_D75_2_10nm => new CIEXYZ(94.972f, 100.000f, 122.638f);
        public static CIEXYZ xyz_D75_10_10nm => new CIEXYZ(94.416f, 100.000f, 120.641f);
        public static CIEXYZ xyz_F2_2_10nm => new CIEXYZ(99.186f, 100.000f, 67.393f);
        public static CIEXYZ xyz_F2_10_10nm => new CIEXYZ(103.279f, 100.000f, 69.027f);
        public static CIEXYZ xyz_F7_2_10nm => new CIEXYZ(95.041f, 100.000f, 108.747f);
        public static CIEXYZ xyz_F7_10_10nm => new CIEXYZ(95.792f, 100.000f, 107.686f);
        public static CIEXYZ xyz_F11_2_10nm => new CIEXYZ(100.962f, 100.000f, 64.350f);
        public static CIEXYZ xyz_F11_10_10nm => new CIEXYZ(103.863f, 100.000f, 65.607f);
        public static CIEXYZ xyz_F1_2_10nm => new CIEXYZ(92.868f, 100.000f, 103.775f);
        public static CIEXYZ xyz_F1_10_10nm => new CIEXYZ(94.815f, 100.000f, 103.255f);
        public static CIEXYZ xyz_F3_2_10nm => new CIEXYZ(103.799f, 100.000f, 49.932f);
        public static CIEXYZ xyz_F3_10_10nm => new CIEXYZ(109.009f, 100.000f, 52.001f);
        public static CIEXYZ xyz_F4_2_10nm => new CIEXYZ(109.202f, 100.000f, 38.881f);
        public static CIEXYZ xyz_F4_10_10nm => new CIEXYZ(115.013f, 100.000f, 40.999f);
        public static CIEXYZ xyz_F5_2_10nm => new CIEXYZ(90.902f, 100.000f, 98.819f);
        public static CIEXYZ xyz_F5_10_10nm => new CIEXYZ(93.390f, 100.000f, 98.692f);
        public static CIEXYZ xyz_F6_2_10nm => new CIEXYZ(97.343f, 100.000f, 60.260f);
        public static CIEXYZ xyz_F6_10_10nm => new CIEXYZ(102.180f, 100.000f, 62.109f);
        public static CIEXYZ xyz_F8_2_10nm => new CIEXYZ(96.428f, 100.000f, 82.421f);
        public static CIEXYZ xyz_F8_10_10nm => new CIEXYZ(97.118f, 100.000f, 81.186f);
        public static CIEXYZ xyz_F9_2_10nm => new CIEXYZ(100.380f, 100.000f, 67.944f);
        public static CIEXYZ xyz_F9_10_10nm => new CIEXYZ(102.125f, 100.000f, 67.866f);
        public static CIEXYZ xyz_F10_2_10nm => new CIEXYZ(96.385f, 100.000f, 82.354f);
        public static CIEXYZ xyz_F10_10_10nm => new CIEXYZ(98.956f, 100.000f, 83.286f);
        public static CIEXYZ xyz_F12_2_10nm => new CIEXYZ(108.117f, 100.000f, 39.275f);
        public static CIEXYZ xyz_F12_10_10nm => new CIEXYZ(111.481f, 100.000f, 40.368f);

    }
}
