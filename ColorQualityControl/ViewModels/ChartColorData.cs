using System;
using ThreeNH.ColorAlgorithm;
using ThreeNH.ColorLibrary;
using ThreeNH.ColorQualityControl.Annotations;
using ThreeNH.ColorSpecimen;
using ThreeNH.Optical;

namespace ThreeNH.ColorQualityControl.ViewModels.Charts
{
    /// <summary>
    /// 颜色数据
    /// 注意：如果使用光谱数据，不要在使用期间更新引用反射率数据，
    /// 否则会导致XYZ和Lab的值与光谱表示的颜色不一致
    /// </summary>
    public class ChartColorData : Specimen, IColorData
    {

        public ChartColorData(StandardIlluminant illuminant, StandardObserver observer,SpecularComponent specularComponent, Specimen specimen) : base(specimen)
        {
            _illuminant = illuminant;
            _observer = observer;
            SpecularComponent = specularComponent;
        }

        private StandardIlluminant _illuminant;
        public StandardIlluminant Illuminant
        {
            get => _illuminant;
            set => _illuminant = value;
        }

        private StandardObserver _observer;
        public StandardObserver Observer
        {
            get => _observer;
            set => _observer = value;
        }
        private SpecularComponent _specularComponent;
        public new SpecularComponent SpecularComponent
        {
            get => _specularComponent;
            set => _specularComponent = value;
        }

        public CIEXYZ Xyz
        {
            get
            {
                var formula = new Formula(_illuminant, _observer);
                return base.GetColorDataEx(SpecularComponent)?.XYZ(formula);
            }
        }
        public CIELAB Lab
        {
            get
            {
                var formula = new Formula(_illuminant, _observer); ;
                return base.GetColorDataEx(SpecularComponent)?.Lab(formula);
            }
        }
        public Spectrum SpectralData => base.GetSpectrum(SpecularComponent);
        public sRGB PseudoColor => Formula.LabToSrgb(Lab);
        public MeasurementSpec MeasurementSpec
        {
            get => new MeasurementSpec()
            {
                Aperture = base.MeasurementCaliber.ToString(),
                DeviceModel = base.Instrument,
                OpticalStructure = base.OpticalStruct,
                MeasurementCondition = base.Remark
            };
        }
        public bool TransmissionFlag => base.IsTransmission;




        public object Clone()
        {
            return new ChartColorData(_illuminant, _observer,_specularComponent, this);
        }
    }
}
