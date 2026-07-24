using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ThreeNH.ColorAlgorithm.IData;

namespace ThreeNH.ColorAlgorithm.Data
{
    public class CIEYxyData : IColorData
    {
        public CIEYxyData()
        {

        }

        public CIEYxyData(CIEYxy yxy, StandardIlluminant illuminant, StandardObserver observer,
            bool transmissionFlag = false, MeasurementSpec measurementSpec = null)
        {
            Yxy = yxy;
            Illuminant = illuminant;
            Observer = observer;
            TransmissionFlag = transmissionFlag;
        }

        public object Clone()
        {
            return new CIEYxyData(Yxy, Illuminant, Observer, TransmissionFlag, MeasurementSpec);
        }

        public CIEYxy Yxy { get; set; }

        public bool IsSpectralData => false;
        public StandardIlluminant Illuminant { get; set; }
        public StandardObserver Observer { get; set; }
        public CIEXYZ Xyz => Yxy?.XYZ;
        public CIELAB Lab => Yxy.XYZ.ToCIELab();
        public ISpectralData SpectralData => null;
        public sRGB PseudoColor => Yxy.XYZ.ToCIELab().TosRGB();
        public MeasurementSpec MeasurementSpec { get; set; }
        public bool TransmissionFlag { get; set; }
    }
}
