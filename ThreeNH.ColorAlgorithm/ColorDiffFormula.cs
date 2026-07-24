using System;
using System.ComponentModel;

namespace ThreeNH.ColorAlgorithm
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
}
