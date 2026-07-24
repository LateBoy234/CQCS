using System;
using ThreeNH.Optical;

namespace ThreeNH.ColorQualityControl
{
    static class ColorDiffFormulaExtension
    {
        public static string Name(this ColorDiffFormula formula)
        {
            switch(formula)
            {
                case ColorDiffFormula.CIE1976: return "\u0394E*";
                case ColorDiffFormula.CIE1994: return "\u0394E*94";
                case ColorDiffFormula.CIE2000: return "\u0394E*00";
                case ColorDiffFormula.CMC: return "CMC(l:c)";
                case ColorDiffFormula.DEuv: return "\u0394E*uv";
                case ColorDiffFormula.DHunterE: return "\u0394E";
                default: return "<None>";
            }
        }
        
        public static string[] Names()
        {
            return new string[]
            {
                ColorDiffFormula.CIE1976.Name(),
                ColorDiffFormula.CIE1994.Name(),
                ColorDiffFormula.CIE2000.Name(),
                ColorDiffFormula.CMC.Name(),
                ColorDiffFormula.DEuv.Name(),
                ColorDiffFormula.DHunterE.Name(),
                ColorDiffFormula.Unknown.Name()
            };
        }

        public static ColorDiffFormula ToColorDiffFormula(this string name)
        {
            foreach(ColorDiffFormula formula in Enum.GetValues(typeof(ColorDiffFormula)))
            {
                if (formula.Name() == name)
                    return formula;
            }
            return ColorDiffFormula.Unknown;
        }
    }
}
