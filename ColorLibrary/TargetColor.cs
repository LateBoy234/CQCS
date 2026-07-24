using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Diagnostics;
using System.Linq;
using ThreeNH.ColorAlgorithm;
using ThreeNH.ColorSpecimen;
using ThreeNH.Optical;

namespace ThreeNH.ColorLibrary
{
    public class SpectralData
    {
        public SpecularComponent ScMode { get; set; }
        public WavelengthRange Range { get; set; }
        public double[][] Data { get; set; }
    }

    /// <summary>
    /// 颜色
    /// </summary>
    public class TargetColor
    {
        private const int SPECTRAL_DATA = 0;
        private const int XYZ_DATA = 1;
        public const int OTHER = 0;
        public const int SCI = 1;
        public const int SCE = 2;

        private CIELAB[] _labs = new CIELAB[3];
        private ColorData[] _data = new ColorData[3];


        public int Id { get; set; }
        public string Number { get; set; }
        public string Name { get; set; }
        public DateTime DateTime { get; set; }
        public string Material { get; set; }
        public string Instrument { get; set; }
        public string Conditions { get; set; }
        public bool IsTransmission { get; set; }
        public byte[] ColorDataBytes { get; private set; }
        public bool IsSpectralData { get; private set; }
        public StandardIlluminant Illuminant { get; private set; } = StandardIlluminant.D65;
        public StandardObserver Observer { get; private set; } = StandardObserver.CIE1964;
        public string ColorFormula { get; set; }

        public CIELAB[] Labs
        {
            get { return _labs; }
        }

        public ColorData Sci
        {
            get { return _data[SCI]; }
        }

        public ColorData Sce
        {
            get { return _data[SCE]; }
        }

        public ColorData Clr
        {
            get { return _data[OTHER]; }
        }

        public void SetColorDataBytes(byte[] bytes)
        {
            ColorDataBytes = bytes;

            _data[0] = _data[1] = _data[2] = null;
            _labs[0] = _labs[1] = _labs[2] = null;

            IsSpectralData = ColorDataBytes[0] == SPECTRAL_DATA;
            int scMode = ColorDataBytes[1];
            ColorData clr1 = null;
            ColorData clr2 = null;

            if (IsSpectralData)
            {
                int start = BitConverter.ToInt16(ColorDataBytes, 2);
                int interval = BitConverter.ToInt16(ColorDataBytes, 4);
                int count = BitConverter.ToInt16(ColorDataBytes, 6);
                var range = new WavelengthRange(start, start + (count - 1) * interval, interval);

                double[] data = new double[count];
                for (int i = 0; i < count; i++)
                {
                    data[i] = BitConverter.ToSingle(ColorDataBytes, 8 + i * sizeof(float));
                }

                clr1 = new ColorData(new Spectrum(range, data));

                if ((scMode & SCI) != 0 && (scMode & SCE) != 0)
                {
                    data = new double[count];
                    for (int i = 0; i < count; i++)
                    {
                        data[i] = BitConverter.ToSingle(ColorDataBytes, 8 + (count + i) * sizeof(float));
                    }

                    clr2 = new ColorData(new Spectrum(range, data));
                }
            }
            else
            {
                var illuminant = (StandardIlluminant) ColorDataBytes[2];
                var observer = (StandardObserver) ColorDataBytes[3];
                clr1 = new ColorData();
                clr1.Set(observer, illuminant, new CIEXYZ(
                    BitConverter.ToSingle(ColorDataBytes, 4),
                    BitConverter.ToSingle(ColorDataBytes, 8),
                    BitConverter.ToSingle(ColorDataBytes, 12)));
                if ((scMode & SCI) != 0 && (scMode & SCE) != 0)
                {
                    clr2 = new ColorData();
                    clr2.Set(observer, illuminant,
                        new CIEXYZ(
                            BitConverter.ToSingle(ColorDataBytes, 16),
                            BitConverter.ToSingle(ColorDataBytes, 20),
                            BitConverter.ToSingle(ColorDataBytes, 24)));
                }

                Illuminant = illuminant;
                Observer = observer;
            }

            if (scMode == OTHER)
            {
                _data[OTHER] = clr1;
            }
            else if (scMode == SCI)
            {
                _data[SCI] = clr1;
            }
            else if (scMode == SCE)
            {
                _data[SCE] = clr1;
            }
            else if ((scMode & SCI) != 0 && (scMode & SCE) != 0)
            {
                _data[SCI] = clr1;
                _data[SCE] = clr2;
            }

            var formula = new Formula(Illuminant, Observer);
            for (int i = 0; i < 3; i++)
            {
                _labs[i] = _data[i]?.Lab(formula);
            }
        }

        public void SetColorData(ColorData clr)
        {
            _data[OTHER] = clr;
            _data[SCI] = null;
            _data[SCE] = null;
            UpdateColorDataBytes();
        }

        public void SetColorData(ColorData sci, ColorData sce)
        {
            _data[OTHER] = null;
            _data[SCI] = sci;
            _data[SCE] = sce;
            UpdateColorDataBytes();
        }

        private void UpdateColorDataBytes()
        {
            if (this.ScMode == SpecularComponent.SCIAndSCE)
            {
                Formula formula;
                if (this._data[SCI].Spectrum != null &&
                    this._data[SCE].Spectrum != null)
                {
                    Trace.Assert(this._data[SCI].Spectrum.Range == this._data[SCE].Spectrum.Range);
                    formula = new Formula(this.Illuminant, this.Observer);
                    this.IsSpectralData = true;
                }
                else
                {
                    Trace.Assert(this._data[SCI].Formulas[0].Illuminant == this._data[SCE].Formulas[0].Illuminant &&
                                 this._data[SCI].Formulas[0].Observer == this._data[SCE].Formulas[0].Observer);

                    formula = this._data[SCI].Formulas[0];
                    this.Illuminant = formula.Illuminant;
                    this.Observer = formula.Observer;
                    this.IsSpectralData = false;
                }

                this._labs[SCI] = this._data[SCI].Lab(formula);
                this._labs[SCE] = this._data[SCE].Lab(formula);
            }
            else
            {
                for (int i = 0; i < 3; i++)
                {
                    if (this._data[i] != null)
                    {
                        Formula formula;
                        if (this._data[i].Spectrum == null)
                        {
                            formula = this._data[i].Formulas[0];
                            this.Illuminant = formula.Illuminant;
                            this.Observer = formula.Observer;
                            this.IsSpectralData = false;
                        }
                        else
                        {
                            formula = new Formula(this.Illuminant, this.Observer);
                            this.IsSpectralData = true;
                        }

                        this._labs[i] = this._data[i].Lab(formula);
                    }
                }
            }

            List<byte> bytes = new List<byte>();
            if (this.IsSpectralData)
            {
                bytes.Add((byte) SPECTRAL_DATA);
                bytes.Add((byte) this.ScMode);
                var range = this._data.First(_ => _?.Spectrum != null).Spectrum.Range;
                bytes.AddRange(BitConverter.GetBytes((short) range.Min));
                bytes.AddRange(BitConverter.GetBytes((short) range.Step));
                bytes.AddRange(BitConverter.GetBytes((short) range.Count));
                foreach (var colorData in this._data)
                {
                    if (colorData != null)
                    {
                        foreach (var v in colorData.Spectrum.Data)
                        {
                            bytes.AddRange(BitConverter.GetBytes((float) v));
                        }
                    }
                }
            }
            else
            {
                bytes.Add((byte) XYZ_DATA);
                bytes.Add((byte) this.ScMode);
                bytes.Add((byte) this.Illuminant);
                bytes.Add((byte) this.Observer);
                foreach (var colorData in this._data)
                {
                    if (colorData != null)
                    {
                        var xyz = colorData.XYZ(colorData.Formulas[0]);
                        bytes.AddRange(BitConverter.GetBytes((float) xyz.X));
                        bytes.AddRange(BitConverter.GetBytes((float) xyz.Y));
                        bytes.AddRange(BitConverter.GetBytes((float) xyz.Z));
                    }
                }
            }

            ColorDataBytes = bytes.ToArray();
        }

        public bool SetIlluminantObserver(StandardIlluminant illuminant, StandardObserver observer)
        {
            if (IsSpectralData)
            {
                Illuminant = illuminant;
                Observer = observer;
                var formula = new Formula(Illuminant, Observer);
                for (int i = 0; i < 3; i++)
                {
                    _labs[i] = _data[i]?.Lab(formula);
                }

                return true;
            }

            return false;
        }

        public static TargetColor FromSpecimen(Specimen specimen)
        {
            var target = new TargetColor
            {
                Id = -1,
                Number = specimen.Name,
                Name = specimen.Name,
                DateTime = specimen.DateTime,
                Instrument = specimen.Instrument,
                IsTransmission = specimen.IsTransmission,
                ColorFormula = specimen.ColorFormula,
            };

            target._data[OTHER] = specimen.Clr;
            target._data[SCI] = specimen.SCI;
            target._data[SCE] = specimen.SCE;
            target.UpdateColorDataBytes();


            return target;
        }

        public Specimen ToSpecimen()
        {
            return new Specimen
            {
                Name = Number,
                Clr = _data[OTHER],
                SCI = _data[SCI],
                SCE = _data[SCE],
                ColorFormula = ColorFormula,
                DateTime = DateTime,
                ID = Id,
                Instrument = Instrument,
                IsTransmission = IsTransmission,
            };
        }

        public SpecularComponent ScMode
        {
            get
            {
                if (_data[OTHER] != null)
                    return SpecularComponent.Unknown;
                if (_data[SCI] != null && _data[SCE] != null)
                    return SpecularComponent.SCIAndSCE;
                if (_data[SCI] != null)
                    return SpecularComponent.SCI;
                if (_data[SCE] != null)
                    return SpecularComponent.SCE;
                Trace.Assert(false);
                return SpecularComponent.Unknown;
            }
        }
    }
}