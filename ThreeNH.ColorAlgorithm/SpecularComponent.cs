using System;
using System.ComponentModel;

namespace ThreeNH.ColorAlgorithm
{
    // 镜面光测量方式
    public enum SpecularComponent
    {
        [Description("SpecularComponent_Unknown")]
        Unknown = 0,    // No setting
        [Description("SpecularComponent_SCI")]
        SCI = 0x01,  // Specular Component Include
        [Description("SpecularComponent_SCE")]
        SCE = 0x02,  // Specular Component exclude
        [Description("SpecularComponent_SCIAndSCE")]
        SCIAndSCE = SCI|SCE,  // Both way
    }

    public static class SpecularComponentExtension
    {
        public static bool HasSCI(this SpecularComponent sc)
        {
            return sc == SpecularComponent.SCI ||
                sc == SpecularComponent.SCIAndSCE;
        }

        public static bool HasSCE(this SpecularComponent sc)
        {
            return sc == SpecularComponent.SCE ||
                sc == SpecularComponent.SCIAndSCE;
        }
    }
}