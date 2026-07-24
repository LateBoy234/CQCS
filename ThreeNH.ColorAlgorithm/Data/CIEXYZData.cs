using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ThreeNH.ColorAlgorithm.IData;

namespace ThreeNH.ColorAlgorithm.Data
{
    public class CIEXYZData : IColorData
    {
        public CIEXYZData() { }

        public CIEXYZData(CIEXYZ xyz, StandardIlluminant illuminant, StandardObserver observer,
            bool transmissionFlag = false, MeasurementSpec measurementSpec = null)
        {
            Xyz = xyz;
            Illuminant = illuminant;
            Observer = observer;
            TransmissionFlag = transmissionFlag;
            MeasurementSpec = measurementSpec;
        }

        public object Clone()
        {
            return new CIEXYZData(Xyz, Illuminant, Observer, TransmissionFlag, MeasurementSpec);
        }

        public bool IsSpectralData => false;
        public StandardIlluminant Illuminant { get; set; }
        public StandardObserver Observer { get; set; }
        public CIEXYZ Xyz { get; set; }
        public CIELAB Lab => Xyz.ToCIELab();
        public ISpectralData SpectralData => null;
        public sRGB PseudoColor => Xyz.ToCIELab()?.TosRGB();
        public MeasurementSpec MeasurementSpec { get; set; }
        public bool TransmissionFlag { get; set; }
    }
}
