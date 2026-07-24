using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ThreeNH.ColorAlgorithm.IData;

namespace ThreeNH.ColorAlgorithm.Data
{
    public class CIELChData : IColorData
    {
        public object Clone()
        {
            return new CIELChData(LCh, Illuminant, Observer, TransmissionFlag, MeasurementSpec);
        }

        public bool IsSpectralData => false;
        public StandardIlluminant Illuminant { get; set; }
        public StandardObserver Observer { get; set; }
        public CIEXYZ Xyz => LCh.ToLab().ToCIEXYZ(Illuminant,Observer);
        public CIELAB Lab => LCh?.ToLab();
        public ISpectralData SpectralData => null;
        public sRGB PseudoColor => LCh.ToLab().TosRGB();
        public MeasurementSpec MeasurementSpec { get; set; }
        public bool TransmissionFlag { get; set; }
        public CIELCh LCh { get; set; }

        public CIELChData() { }

        public CIELChData(CIELCh lch, StandardIlluminant illuminant, StandardObserver observer,
            bool transmissionFlag = false, MeasurementSpec measurementSpec = null)
        {
            LCh = lch;
            Illuminant = illuminant;
            Observer = observer;
            TransmissionFlag = transmissionFlag;
            MeasurementSpec = measurementSpec;
        }
    }
}
