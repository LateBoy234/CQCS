using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using ThreeNH.DataModel;

namespace YL30.Model
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ToleranceForYL30
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 7)]
        public float[] Values;

        public float dE
        {
            get { return Values[0]; }
            set { Values[0] = value; }
        }

        public float dL_Lower
        {
            get { return Values[1]; }
            set { Values[1] = value; }
        }

        public float da_Lower
        {
            get { return Values[2]; }
            set { Values[2] = value; }
        }

        public float db_Lower
        {
            get { return Values[3]; }
            set { Values[3] = value; }
        }

        public float dL_Upper
        {
            get { return Values[4]; }
            set { Values[4] = value; }
        }

        public float da_Upper
        {
            get { return Values[5]; }
            set { Values[5] = value; }
        }

        public float db_Upper
        {
            get { return Values[6]; }
            set { Values[6] = value; }
        }

        
        public ThreeNH.DataModel.Tolerance Uniform()
        {
            var tolerance = new ThreeNH.DataModel.Tolerance(
                new ThreeNH.DataModel.ChromaticityField[]
                {
                    ThreeNH.DataModel.ChromaticityField.dL,
                    ThreeNH.DataModel.ChromaticityField.da,
                    ThreeNH.DataModel.ChromaticityField.db,
                    ThreeNH.DataModel.ChromaticityField.dEab,
                });

            Func<double, double, bool> isLimitValid = (lower, upper) => lower < upper;

            tolerance[ThreeNH.DataModel.ChromaticityField.dL] =
                new ThreeNH.DataModel.ToleranceItem
                {
                    Lower = dL_Lower,
                    Upper = dL_Upper,
                    UseForJudgement = isLimitValid(dL_Lower, dL_Upper)
                };
            tolerance[ThreeNH.DataModel.ChromaticityField.da] =
                new ThreeNH.DataModel.ToleranceItem
                {
                    Lower = da_Lower,
                    Upper = da_Upper,
                    UseForJudgement = isLimitValid(da_Lower, da_Upper)
                };
            tolerance[ThreeNH.DataModel.ChromaticityField.db] =
                new ThreeNH.DataModel.ToleranceItem
                {
                    Lower = db_Lower,
                    Upper = db_Upper,
                    UseForJudgement = isLimitValid(db_Lower, db_Upper)
                };
            tolerance[ThreeNH.DataModel.ChromaticityField.dEab] = new ThreeNH.DataModel.ToleranceItem
            {
                Upper = dE,
                UseForJudgement = dE > 0
            };
            ;
            return tolerance;
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
    public struct SystemtToleranceStruct
    {
        /// <summary>
        /// DeltaValue
        /// </summary>
        public float DeltaValue;
        /// <summary>
        /// 第一个值下限
        /// </summary>
        public float Value1Lower;
        /// <summary>
        /// 第二个值下限
        /// </summary>
        public float Value2Lower;
        /// <summary>
        /// 第三个值下限
        /// </summary>
        public float Value3Lower;
        /// <summary>
        /// 第一个值上限
        /// </summary>
        public float Value1Upper;
        /// <summary>
        /// 第二个值上限
        /// </summary>
        public float Value2Upper;
        /// <summary>
        /// 第三个值上限
        /// </summary>
        public float Value3Upper;
    }
}
