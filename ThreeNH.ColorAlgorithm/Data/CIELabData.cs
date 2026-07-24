using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ThreeNH.ColorAlgorithm.IData;

namespace ThreeNH.ColorAlgorithm.Data
{
    public class CIElabData : IColorData
    {
        public object Clone()
        {
            return new CIElabData(Lab, Illuminant, Observer, TransmissionFlag, MeasurementSpec);
        }

        public bool IsSpectralData { get; } = false;
        public StandardIlluminant Illuminant { get; set; }
        public StandardObserver Observer { get; set; }
        public CIEXYZ Xyz => Lab.ToCIEXYZ(Illuminant,Observer);
        public CIELAB Lab { get; set; }
        public ISpectralData SpectralData { get; } = null;
        public sRGB PseudoColor => Lab.TosRGB();

        public MeasurementSpec MeasurementSpec { get; set; }
        public bool TransmissionFlag { get; set; }

        public CIElabData() { }

        public CIElabData(CIELAB lab, StandardIlluminant illuminant, StandardObserver observer,
            bool transmissionFlag = false, MeasurementSpec measurementSpec = null)
        {
            Lab = lab;
            Illuminant = illuminant;
            Observer = observer;
            TransmissionFlag = transmissionFlag;
            MeasurementSpec = measurementSpec;
        }
    }
}
