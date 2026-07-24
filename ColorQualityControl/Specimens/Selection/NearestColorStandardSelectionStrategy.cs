using System.Collections.Generic;
using ThreeNH.ColorAlgorithm;
using ThreeNH.ColorSpecimen;
using ThreeNH.DataModel;
using ThreeNH.Optical;

namespace ThreeNH.ColorQualityControl.Specimens.Selection
{
    /// <summary>
    /// 最近色选标样：按 ΔE 找最近标样，并受 AutoSelectStandardLimits 约束。
    /// 编辑菜单「自动匹配标样」与自动选标样共用此逻辑。
    /// </summary>
    internal sealed class NearestColorStandardSelectionStrategy : IStandardSelectionStrategy
    {
        public static NearestColorStandardSelectionStrategy Instance { get; } = new NearestColorStandardSelectionStrategy();

        public string Name => "NearestColor";

        public StandardSpecimen Select(StandardSelectionContext context)
        {
            if (context?.Host == null || context.Sample == null)
            {
                return null;
            }

            CIELAB lab = context.Sample.GetColorData(context.SpecularComponent)?.Lab(context.ColorFormula);
            if (lab == null)
            {
                return null;
            }

            StandardSpecimen std = context.Database.FindNearestStandard(
                lab, context.SpecularComponent, context.Observer, context.Illuminant);
            if (std == null)
            {
                return null;
            }

            CIELAB stdLab = std.GetColorData(context.SpecularComponent).Lab(context.ColorFormula);
            CIEDE1976 delta = new CIEDE1976(lab, stdLab);
            var dict = new Dictionary<ChromaticityField, double>
            {
                [ChromaticityField.dL] = System.Math.Abs(delta.dL),
                [ChromaticityField.da] = System.Math.Abs(delta.da),
                [ChromaticityField.db] = System.Math.Abs(delta.db),
                [ChromaticityField.dEab] = delta.dE
            };

            Tolerance limits = context.AutoSelectLimits;
            foreach (var each in dict.Keys)
            {
                if (limits[each].UseForJudgement && dict[each] > limits[each].Upper)
                {
                    return null;
                }
            }

            return std;
        }
    }
}
