using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ThreeNH.ColorAlgorithm
{
    public class TristimulusWeightingFactorTable
    {
        private List<TristimulusWeightingFactor> _factors = new List<TristimulusWeightingFactor>();
        public static TristimulusWeightingFactorTable TRISTIMULUS_WEIGHTING_WEIGHTING_FACTORS_5NM_TABLEs
        {
            get
            {
                var table = new TristimulusWeightingFactorTable();
                table._factors.Add(TristimulusWeightingFactor.A_10_5nm_Factors);
                table._factors.Add(TristimulusWeightingFactor.A_2_5nm_Factors);
                table._factors.Add(TristimulusWeightingFactor.C_10_5nm_Factors);
                table._factors.Add(TristimulusWeightingFactor.C_2_5nm_Factors);
                table._factors.Add(TristimulusWeightingFactor.D50_10_5nm_Factors);
                table._factors.Add(TristimulusWeightingFactor.D50_2_5nm_Factors);
                table._factors.Add(TristimulusWeightingFactor.D55_10_5nm_Factors);
                table._factors.Add(TristimulusWeightingFactor.D55_2_5nm_Factors);
                table._factors.Add(TristimulusWeightingFactor.D65_10_5nm_Factors);
                table._factors.Add(TristimulusWeightingFactor.D65_2_5nm_Factors);
                table._factors.Add(TristimulusWeightingFactor.D75_10_5nm_Factors);
                table._factors.Add(TristimulusWeightingFactor.D75_2_5nm_Factors);
                table._factors.Add(TristimulusWeightingFactor.F10_10_5nm_Factors);
                table._factors.Add(TristimulusWeightingFactor.F10_2_5nm_Factors);
                table._factors.Add(TristimulusWeightingFactor.F11_10_5nm_Factors);
                table._factors.Add(TristimulusWeightingFactor.F11_2_5nm_Factors);
                table._factors.Add(TristimulusWeightingFactor.F12_10_5nm_Factors);
                table._factors.Add(TristimulusWeightingFactor.F12_2_5nm_Factors);
                table._factors.Add(TristimulusWeightingFactor.F1_10_5nm_Factors);
                table._factors.Add(TristimulusWeightingFactor.F1_2_5nm_Factors);
                table._factors.Add(TristimulusWeightingFactor.F2_10_5nm_Factors);
                table._factors.Add(TristimulusWeightingFactor.F2_2_5nm_Factors);
                table._factors.Add(TristimulusWeightingFactor.F3_10_5nm_Factors);
                table._factors.Add(TristimulusWeightingFactor.F3_2_5nm_Factors);
                table._factors.Add(TristimulusWeightingFactor.F4_10_5nm_Factors);
                table._factors.Add(TristimulusWeightingFactor.F4_2_5nm_Factors);
                table._factors.Add(TristimulusWeightingFactor.F5_10_5nm_Factors);
                table._factors.Add(TristimulusWeightingFactor.F5_2_5nm_Factors);
                table._factors.Add(TristimulusWeightingFactor.F6_10_5nm_Factors);
                table._factors.Add(TristimulusWeightingFactor.F6_2_5nm_Factors);
                table._factors.Add(TristimulusWeightingFactor.F7_10_5nm_Factors);
                table._factors.Add(TristimulusWeightingFactor.F7_2_5nm_Factors);
                table._factors.Add(TristimulusWeightingFactor.F8_10_5nm_Factors);
                table._factors.Add(TristimulusWeightingFactor.F8_2_5nm_Factors);
                table._factors.Add(TristimulusWeightingFactor.F9_10_5nm_Factors);
                table._factors.Add(TristimulusWeightingFactor.F9_2_5nm_Factors);
                return table;
            }
        }

        public static TristimulusWeightingFactorTable TRISTIMULUS_WEIGHTING_WEIGHTING_FACTORS_10NM_TABLEs
        {
            get
            {
                var table = new TristimulusWeightingFactorTable();
                table._factors.Add(TristimulusWeightingFactor.A_10_10nm_Factors);
                table._factors.Add(TristimulusWeightingFactor.A_2_10nm_Factors);
                table._factors.Add(TristimulusWeightingFactor.C_10_10nm_Factors);
                table._factors.Add(TristimulusWeightingFactor.C_2_10nm_Factors);
                table._factors.Add(TristimulusWeightingFactor.D50_10_10nm_Factors);
                table._factors.Add(TristimulusWeightingFactor.D50_2_10nm_Factors);
                table._factors.Add(TristimulusWeightingFactor.D55_10_10nm_Factors);
                table._factors.Add(TristimulusWeightingFactor.D55_2_10nm_Factors);
                table._factors.Add(TristimulusWeightingFactor.D65_10_10nm_Factors);
                table._factors.Add(TristimulusWeightingFactor.D65_2_10nm_Factors);
                table._factors.Add(TristimulusWeightingFactor.D75_10_10nm_Factors);
                table._factors.Add(TristimulusWeightingFactor.D75_2_10nm_Factors);
                table._factors.Add(TristimulusWeightingFactor.F10_10_10nm_Factors);
                table._factors.Add(TristimulusWeightingFactor.F10_2_10nm_Factors);
                table._factors.Add(TristimulusWeightingFactor.F11_10_10nm_Factors);
                table._factors.Add(TristimulusWeightingFactor.F11_2_10nm_Factors);
                table._factors.Add(TristimulusWeightingFactor.F12_10_10nm_Factors);
                table._factors.Add(TristimulusWeightingFactor.F12_2_10nm_Factors);
                table._factors.Add(TristimulusWeightingFactor.F1_10_10nm_Factors);
                table._factors.Add(TristimulusWeightingFactor.F1_2_10nm_Factors);
                table._factors.Add(TristimulusWeightingFactor.F2_10_10nm_Factors);
                table._factors.Add(TristimulusWeightingFactor.F2_2_10nm_Factors);
                table._factors.Add(TristimulusWeightingFactor.F3_10_10nm_Factors);
                table._factors.Add(TristimulusWeightingFactor.F3_2_10nm_Factors);
                table._factors.Add(TristimulusWeightingFactor.F4_10_10nm_Factors);
                table._factors.Add(TristimulusWeightingFactor.F4_2_10nm_Factors);
                table._factors.Add(TristimulusWeightingFactor.F5_10_10nm_Factors);
                table._factors.Add(TristimulusWeightingFactor.F5_2_10nm_Factors);
                table._factors.Add(TristimulusWeightingFactor.F6_10_10nm_Factors);
                table._factors.Add(TristimulusWeightingFactor.F6_2_10nm_Factors);
                table._factors.Add(TristimulusWeightingFactor.F7_10_10nm_Factors);
                table._factors.Add(TristimulusWeightingFactor.F7_2_10nm_Factors);
                table._factors.Add(TristimulusWeightingFactor.F8_10_10nm_Factors);
                table._factors.Add(TristimulusWeightingFactor.F8_2_10nm_Factors);
                table._factors.Add(TristimulusWeightingFactor.F9_10_10nm_Factors);
                table._factors.Add(TristimulusWeightingFactor.F9_2_10nm_Factors);
                return table;
            }
        }

        public TristimulusWeightingFactor this[StandardObserver observer, StandardIlluminant illuminant]
        {
            get
            {
                return _factors.FirstOrDefault(x=>x.Observer == observer&&x.Illuminant == illuminant);
            }
        }

    }
}
