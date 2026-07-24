using System;
using System.ComponentModel;

namespace ThreeNH.Optical
{
    public enum ColorDiffFormula
    {
        /// <summary>
        /// dE*(ab)
        /// </summary>
        [Description("\u0394E*ab")]
        CIE1976,
        /// <summary>
        /// dE94
        /// </summary>
        [Description("\u0394E*94")]
        CIE1994,    
        /// <summary>
        /// dE00
        /// </summary>
        [Description("\u0394E*00")]
        CIE2000,    
        /// <summary>
        /// DEcmc(1:1)
        /// </summary>
        [Description("CMC(1:1)")]
        CMC11,
        /// <summary>
        /// DEcmc(2:1)
        /// </summary>
        [Description("CMC(2:1)")]
        CMC21,
        /// <summary>
        /// DEcmc(1:c)
        /// </summary>
        [Description("CMC(l:c)")]
        CMC,        // dEcmc(l:c)
        /// <summary>
        /// DEuv
        /// </summary>
        [Description("\u0394E*uv")]
        DEuv,       // dE*(uv)
        /// <summary>
        /// DE
        /// </summary>
        [Description("\u0394E")]
        DHunterE,   // dE
        /// <summary>
        /// DIN99
        /// </summary>
        [Description("\u0394E99")]
        DIN99,   // DIN99
        [Description("")]
        Unknown,
    }

    public enum PathLengthEnum
    {
        Pathlength_10 = 10,
        Pathlength_30 = 30,
        Pathlength_50 = 50,
        Pathlength_100 = 100
    }
    
    public class ICUMSAParameter
    {
        /// <summary>1
        /// 比色皿厚度，单位cm
        /// </summary>
        public double IUThickness { get; set; }
        /// <summary>
        /// 溶液体浓度，单位g/mL
        /// </summary>
        public double IUConcentration { get; set; }

        public PathLengthEnum PathLength { get; set; }
    }
}
