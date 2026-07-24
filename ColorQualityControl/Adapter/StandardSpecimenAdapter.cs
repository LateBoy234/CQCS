using System.Collections.Generic;
using ThreeNH.DataModel;
using ThreeNH.Optical;
using ThreeNH.ColorAlgorithm;

namespace ThreeNH.ColorQualityControl.Adapter
{
    public class StandardSpecimenAdapter : SpecimenAdapter
    {

        public StandardSpecimenAdapter(StandardSpecimen std,
            Formula formula,
            ChromaticitySetting setting,
            SpecularComponent sc,
            Tolerance tolerance = null)
            : base(std)
        {
            Tolerance = tolerance ?? std.ToleranceSetting?.PrimaryTolerance;

            CalculateValues(setting, sc, formula);
        }

        public StandardSpecimen StandardSpecimen
        {
            get { return Specimen as StandardSpecimen; }
        }

        public Tolerance Tolerance;
    }
}
