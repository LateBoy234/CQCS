using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using ThreeNH.ColorAlgorithm;
using ThreeNH.ColorSpecimen;
using ThreeNH.DataModel;
using ThreeNH.Helper;
using ThreeNH.Optical;
using ThreeNH.StructHelper;

namespace ThreeNH.YHComm
{

    [StructLayout(LayoutKind.Sequential, Size = 64, Pack = 4)]
    public struct RecordColorDataFor1810
    {
        private const int BYTES_SIZE = Constants.VALID_WAVE_NUM_Max * sizeof(short);

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = BYTES_SIZE)]
        private byte[] bytes;

        public short[] Spectrum
        {
            get
            {
                var spectrum = new short[Constants.VALID_WAVE_NUM_Max];
                for (int i = 0; i < Constants.VALID_WAVE_NUM_Max; i++)
                {
                    spectrum[i] = BitConverter.ToInt16(bytes, i * sizeof(short));
                }

                return spectrum;
            }
            set
            {
                if (value.Length != Constants.VALID_WAVE_NUM_Max)
                {
                    throw new ArgumentException();
                }

                if (bytes == null)
                {
                    bytes = new byte[BYTES_SIZE];
                }

                for (int i = 0; i < Constants.VALID_WAVE_NUM_Max; i++)
                {
                    var tmp = BitConverter.GetBytes(value[i]);
                    bytes[i * 2] = tmp[0];
                    bytes[i * 2 + 1] = tmp[1];
                }
            }
        }

        public byte Illuminant
        {
            get => bytes[0];
            set
            {
                if (bytes == null)
                {
                    bytes = new byte[BYTES_SIZE];
                }
                bytes[0] = value;
            }
        }

        public byte Observer
        {
            get => bytes[1];
            set
            {
                if (bytes == null)
                {
                    bytes = new byte[BYTES_SIZE];
                }
                bytes[1] = value;
            }
        }

        public float[] Values
        {
            get
            {
                var values = new float[3];
                values[0] = BitConverter.ToInt16(bytes, 4);
                values[1] = BitConverter.ToInt16(bytes, 4 + sizeof(float));
                values[2] = BitConverter.ToInt16(bytes, 4 + sizeof(float) * 2);
                return values;
            }
            set
            {
                if (value.Length != 3)
                {
                    throw new ArgumentException();
                }

                if (bytes == null)
                {
                    bytes = new byte[BYTES_SIZE];
                }

                for (int i = 0; i < 3; i++)
                {
                    var tmp = BitConverter.GetBytes(value[i]);
                    Array.Copy(tmp, 0, bytes, 4 + sizeof(float) * i, sizeof(float));
                }
            }
        }



        public ColorData ToColorData(RecordFlags.ColorDataType dataType)
        {
            var ret = new ColorData();
            if (dataType == RecordFlags.ColorDataType.XYZ)
            {
                ret.Set((StandardObserver)Observer, (StandardIlluminant)Illuminant,
                    new CIEXYZ(Values[0], Values[1], Values[2]));
            }
            else if (dataType == RecordFlags.ColorDataType.Lab)
            {
                var lab = new CIELAB(Values[0], Values[1], Values[2]);
                var xyz = Formula.LabToXyz(lab, ((ThreeNH.YHComm.Illuminant)Illuminant).Uniform(),
                    ((ThreeNH.YHComm.Observer)Observer).Uniform());
                ret.Set((StandardObserver)Observer, (StandardIlluminant)Illuminant, xyz);
            }
            else if (dataType == RecordFlags.ColorDataType.Spectrum)
            {
                Spectrum spectrum = new Spectrum(new WavelengthRange(Constants.VALID_WAVE_BEGIN_Max, Constants.VALID_WAVE_END_Max, Constants.VALID_WAVE_STEP));
                for (int i = 0; i < Constants.VALID_WAVE_NUM_Max; i++)
                {
                    int wavelength = Constants.VALID_WAVE_BEGIN_Max + Constants.VALID_WAVE_STEP * i;
                    spectrum[wavelength] = this.Spectrum[i] * 0.0001;
                }
                ret.Spectrum = spectrum;
            }

            return ret;
        }

        public static RecordColorDataFor1810 FromColorData(ColorData clrData, RecordFlags.ColorDataType dataType = RecordFlags.ColorDataType.Lab)
        {
            if (dataType != RecordFlags.ColorDataType.Spectrum && clrData.Formulas == null)
            {
                throw new ArgumentException("clrData must be XYZ data");
            }

            if (dataType == RecordFlags.ColorDataType.Spectrum)
            {
                short[] spectrum = new short[Constants.VALID_WAVE_NUM_Max];
                for (int i = 0; i < Constants.VALID_WAVE_NUM_Max; i++)
                {
                    int wavelength = Constants.VALID_WAVE_BEGIN_Max + Constants.VALID_WAVE_STEP * i;
                    spectrum[i] = (short)(clrData.Spectrum[wavelength] * 10000);
                }

                return new RecordColorDataFor1810 { Spectrum = spectrum };
            }

            var formula = clrData.Formulas[0];
            if (dataType == RecordFlags.ColorDataType.Lab)
            {
                var lab = clrData.Lab(formula);
                return new RecordColorDataFor1810
                {
                    Illuminant = (byte)formula.Illuminant.ToInstrumentForm(),
                    Observer = (byte)formula.Observer.ToInstrumentForm(),
                    Values = new float[] { (float)lab.L, (float)lab.a, (float)lab.b }
                };
            }
            if (dataType == RecordFlags.ColorDataType.XYZ)
            {
                CIEXYZ xyz = clrData.XYZ(formula);
                return new RecordColorDataFor1810
                {
                    Illuminant = (byte)formula.Illuminant.ToInstrumentForm(),
                    Observer = (byte)formula.Observer.ToInstrumentForm(),
                    Values = new float[] { (float)xyz.X, (float)xyz.Y, (float)xyz.Z }
                };
            }
            throw new ArgumentException();
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = Constants.VALID_WAVE_NUM_Max, Pack = 4)]
    public struct RecordSpectrumDataFor1810
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = Constants.VALID_WAVE_NUM_Max)]
        private ushort[] _rawData;

        public double[] Spectrum
        {
            get { return _rawData != null ? Array.ConvertAll(_rawData, x => x * 0.0001) : null; }
            set
            {
                if (value != null && value.Length != Constants.VALID_WAVE_NUM_Max)
                {
                    throw new ArgumentException($"Length must be is {Constants.VALID_WAVE_NUM_Max}");
                }

                _rawData = value == null ? null : Array.ConvertAll(value, x => (ushort)(x * 10000));
            }
        }

        public ColorData ToColorData()
        {
            return new ColorData(new Spectrum(new WavelengthRange(360, 780), Spectrum));
        }
    }

   

    [StructLayout(LayoutKind.Sequential, Size = 128, Pack = 4, CharSet = CharSet.Ansi)]
    public struct StandardRecordFor1810
    {
        private RecordFlags _flags;
        private int _id;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = Constants.RECORD_NAME_MAX)]
        private string _name;
        private RtcDateTime _dateTime;
        /// <summary>
        ///  雾度测量结果，索引相对对于`HazeType`的枚举值。
        ///  如果不是色彩雾度计，所有类型的值都是一样的。
        ///  注意：只有当记录标记中的<c>IsHazeValid</c>有效时，雾度值才有效，
        ///  <c>IsTransmittanceValid</c>透过率才有效
        /// </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = Constants.HAZE_TYPE_COUNT)]
        private HazeData[] hazes;

        /// <summary>
        /// 颜色值
        /// 注意：只有当记录标志位中的<c>IsColorValid</c>有效时才有效。
        /// 另外颜色类型要通过记录标志位中的<c>ColorType</c>来确定
        /// </summary>
        private RecordColorDataFor1810 _color;

        /// <summary>
        /// 清晰度
        /// 注意：只有当标志位的<c>IsClarityValid</c>为真是有效。
        /// </summary>
        private short _clarity;

        /// <summary>
        /// 填充两个2节
        /// </summary>
        private short _fills;

        /// <summary>
        /// 记录容差
        /// </summary>
        private RecordTolerance _tolerance;

        #region Properties

        public static StandardRecordFor1810 FromSpecimen(Specimen specimen)
        {
            StandardRecordFor1810 record = new StandardRecordFor1810();
            try
            {
                record._flags.Aperture = specimen.MeasurementCaliber.ToInstrumentForm();
            }
            catch
            {
                record._flags.Aperture = Aperture.Phi8;
            }

            record._flags.IsLock = false;
            record._flags.IsColorVaild = true;


            // 如果名称过长截断
            record.Name = specimen.Name.Length >= Constants.RECORD_NAME_MAX
                ? specimen.Name.Substring(0, Constants.RECORD_NAME_MAX - 1)
                : specimen.Name;
            record.DateTime = specimen.DateTime;
            record.Id = 0;

            if (specimen.SCI?.Spectrum != null || specimen.SCE?.Spectrum != null || specimen.Clr?.Spectrum != null)
            {
                record._flags.ColorType = RecordFlags.ColorDataType.Spectrum;
            }
            else
            {
                record._flags.ColorType = RecordFlags.ColorDataType.Lab;
            }
            switch (specimen.SpecularComponent)
            {
                case SpecularComponent.Unknown:
                    record._color = RecordColorDataFor1810.FromColorData(specimen.Clr, record._flags.ColorType);
                    break;
                case SpecularComponent.SCI:
                case SpecularComponent.SCIAndSCE:
                    record._color = RecordColorDataFor1810.FromColorData(specimen.SCI, record._flags.ColorType);
                    break;
                case SpecularComponent.SCE:
                    record._color = RecordColorDataFor1810.FromColorData(specimen.SCE, record._flags.ColorType);
                    break;
            }

            if (specimen.HazeDatas != null)
            {
                record.hazes = new HazeData[3];
                var illuminants = new StandardIlluminant[]
                {
                    StandardIlluminant.C,
                    StandardIlluminant.A,
                    StandardIlluminant.D65
                };

                for (int i = 0; i < 3; i++)
                {
                    var tmp = specimen.GetHazeData(illuminants[i], StandardObserver.CIE1931);
                    record.hazes[i].Haze = tmp.Haze;
                    record.hazes[i].Transmittance = tmp.Transmittance;
                }
            }

            var tolerance = (specimen as StandardSpecimen)?.ToleranceSetting?.PrimaryTolerance;
            record._tolerance.ColorTolerance = ColorTolerance.FromUniformTolerance(tolerance);

            return record;
        }

        public RecordFlags Flags
        {
            get { return _flags; }
            set { _flags = value; }
        }

        public string Name
        {
            get { return _name; }

            set { _name = value; }
        }

        internal DateTime DateTime
        {
            get { return _dateTime.ToDateTime(); }

            set { _dateTime = new RtcDateTime(value); }
        }


        internal RecordTolerance Tolerance
        {
            get { return _tolerance; }

            set { _tolerance = value; }
        }



        public int Id
        {
            get { return _id; }

            set { _id = value; }
        }

        /// <summary>
        /// 清晰度（只有Flags.IsClarityValid为真时有效）
        /// </summary>
        public double Clarity
        {
            get => _clarity * 0.0001;
            set => _clarity = (short)Math.Round(value * 10000, 0);
        }

        public StandardSpecimen ToSpecimen()
        {
            StandardSpecimen specimen = new StandardSpecimen();
            specimen.IsTransmission = true;
            specimen.DateTime = DateTime;
            specimen.ID = Id;
            specimen.MeasurementCaliber = Flags.Aperture.Uniform();
            specimen.Name = Name;
            specimen.HazeDatas = new ColorSpecimen.HazeData[3];
            for (int i = 0; i < Constants.HAZE_TYPE_COUNT; i++)
            {
                specimen.HazeDatas[i] = hazes[i].ToUniform((HazeType)i);
            }

            if (Flags.IsColorVaild)
            {
                var waverange = new WavelengthRange(Constants.VALID_WAVE_BEGIN_Max, Constants.VALID_WAVE_END_Max, Constants.VALID_WAVE_STEP);
                specimen.Clr = _color.ToColorData(Flags.ColorType);
            }

            var tolerance = Tolerance.ColorTolerance;
            if (tolerance != null)
            {
                specimen.ToleranceSetting = new ToleranceSetting { PrimaryTolerance = tolerance.Value.Uniform() };
            }

            return specimen;
        }

        #endregion
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4, CharSet = CharSet.Ansi)]
    public struct TrialRecordFor1810
    {
        private RecordFlags _flags;

        private int _id;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = Constants.RECORD_NAME_MAX)]
        private string _name;

        private RtcDateTime _dateTime;

        /// <summary>
        ///  雾度测量结果，索引相对对于`HazeType`的枚举值。
        ///  如果不是色彩雾度计，所有类型的值都是一样的。
        ///  注意：只有当记录标记中的<c>IsHazeValid</c>有效时，雾度值才有效，
        ///  <c>IsTransmittanceValid</c>透过率才有效
        /// </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = Constants.HAZE_TYPE_COUNT)]
        private HazeData[] hazes;

        /// <summary>
        /// 颜色值
        /// 注意：只有当记录标志位中的<c>IsColorValid</c>有效时才有效。
        /// 另外颜色类型要通过记录标志位中的<c>ColorType</c>来确定
        /// </summary>
        private RecordColorDataFor1810 color;


        /// <summary>
        /// 清晰度
        /// 注意：只有当标志位的<c>IsClarityValid</c>为真是有效。
        /// </summary>
        private short _clarity;

        /// <summary>
        /// 填充两个2节
        /// </summary>
        private short _fills;

        /// <summary>
        /// 标样ID
        /// </summary>
        private int standard_id;

        public static TrialRecordFor1810 FromSpecimen(Specimen specimen)
        {
            TrialRecordFor1810 record = new TrialRecordFor1810();
            try
            {
                record._flags.Aperture = specimen.MeasurementCaliber.ToInstrumentForm();
            }
            catch
            {
                record._flags.Aperture = Aperture.Phi8;
            }

            record._flags.IsLock = false;
            record._flags.IsColorVaild = true;


            // 如果名称过长截断
            record.Name = specimen.Name.Length >= Constants.RECORD_NAME_MAX
                ? specimen.Name.Substring(0, Constants.RECORD_NAME_MAX - 1)
                : specimen.Name;
            record.DateTime = specimen.DateTime;
            record.Id = 0;

            if (specimen.SCI?.Spectrum != null || specimen.SCE?.Spectrum != null || specimen.Clr?.Spectrum != null)
            {
                record._flags.ColorType = RecordFlags.ColorDataType.Spectrum;
            }
            else
            {
                record._flags.ColorType = RecordFlags.ColorDataType.Lab;
            }
            switch (specimen.SpecularComponent)
            {
                case SpecularComponent.Unknown:
                    record.color = RecordColorDataFor1810.FromColorData(specimen.Clr, record._flags.ColorType);
                    break;
                case SpecularComponent.SCI:
                case SpecularComponent.SCIAndSCE:
                    record.color = RecordColorDataFor1810.FromColorData(specimen.SCI, record._flags.ColorType);
                    break;
                case SpecularComponent.SCE:
                    record.color = RecordColorDataFor1810.FromColorData(specimen.SCE, record._flags.ColorType);
                    break;
            }

            record.standard_id = 0;

            if (specimen.HazeDatas != null)
            {
                record.hazes = new HazeData[3];
                var illuminants = new StandardIlluminant[]
                {
                    StandardIlluminant.C,
                    StandardIlluminant.A,
                    StandardIlluminant.D65
                };

                for (int i = 0; i < 3; i++)
                {
                    var tmp = specimen.GetHazeData(illuminants[i], StandardObserver.CIE1931);
                    record.hazes[i].Haze = tmp.Haze;
                    record.hazes[i].Transmittance = tmp.Transmittance;
                }
            }

            return record;
        }

        public RecordFlags Flags
        {
            get { return _flags; }
            set { _flags = value; }
        }

        public string Name
        {
            get { return _name; }

            set { _name = value; }
        }

        internal DateTime DateTime
        {
            get { return _dateTime.ToDateTime(); }

            set { _dateTime = new RtcDateTime(value); }
        }


        public int Id
        {
            get { return _id; }

            set { _id = value; }
        }

        /// <summary>
        /// 标样ID
        /// </summary>
        public int StandardId
        {
            get => standard_id;
            set => standard_id = value;
        }

        /// <summary>
        /// 清晰度
        /// </summary>
        public double Clarity
        {
            get => _clarity * 0.0001;
            set => _clarity = (short)Math.Round(value * 10000, 0);
        }


        public TestSpecimen ToSpecimen()
        {
            TestSpecimen specimen = new TestSpecimen();
            specimen.IsTransmission = true;
            specimen.DateTime = DateTime;
            specimen.ID = Id;
            specimen.MeasurementCaliber = Flags.Aperture.Uniform();
            specimen.Name = Name;
            specimen.Tag = StandardId;

            specimen.HazeDatas = new ColorSpecimen.HazeData[3];
            for (int i = 0; i < Constants.HAZE_TYPE_COUNT; i++)
            {
                specimen.HazeDatas[i] = hazes[i].ToUniform((HazeType)i);
            }

            if (Flags.IsColorVaild)
            {
                var waverange = new WavelengthRange(Constants.VALID_WAVE_BEGIN_Max, Constants.VALID_WAVE_END_Max, Constants.VALID_WAVE_STEP);
                specimen.Clr = color.ToColorData(Flags.ColorType);
            }

            return specimen;
        }
    }
}