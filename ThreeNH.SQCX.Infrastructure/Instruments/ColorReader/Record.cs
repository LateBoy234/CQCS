using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using ThreeNH.ColorAlgorithm;
using ThreeNH.ColorSpecimen;
using ThreeNH.DataModel;
using ThreeNH.Optical;
using ThreeNH.StructHelper;

namespace ThreeNH.SQCX.Infrastructure.Instruments.ColorReader
{
    internal static class RecordHelper
    {
        /// <summary>
        /// 根据口径类型获取透镜位置
        /// </summary>
        /// <param name="caliber">口径类型</param>
        /// <returns>返回相应的透镜位置</returns>
        public static LensPos GetLensPosFromCaliber(Caliber caliber)
        {
            switch (caliber)
            {
                case Caliber.Phi4:
                    return LensPos.Pos4;

                case Caliber.Phi8:
                    return LensPos.Pos8;
                //                case Caliber.Phi15:
                //                    return LensPos.Pos15;
                //                case Caliber.Phi25:
                //                    return LensPos.Pos25;
                default:
                    return LensPos.Pos8;
            }
        }

        /// <summary>
        /// 由Specimen对象获取仪器兼容的反射率数据
        /// </summary>
        /// <param name="spectrum"></param>
        /// <returns></returns>
        public static double[] GetSpectrumData(Spectrum spectrum, UvMode uvMode = UvMode.NoCut)
        {
            double[] data = new double[Constants.VALID_WAVE_NUM];
            if (spectrum.Range.Step % Constants.VALID_WAVE_STEP != 0)
            {
                spectrum = spectrum.Interpolate(new WavelengthRange(spectrum.Range.Min, spectrum.Range.Max,
                    Constants.VALID_WAVE_STEP));
            }

            for (int wave = Constants.VALID_WAVE_BEGIN; wave <= Constants.VALID_WAVE_END; wave += Constants.VALID_WAVE_STEP)
            {
                int index = (wave - Constants.VALID_WAVE_BEGIN) / Constants.VALID_WAVE_STEP;
                if (wave < spectrum.Range.Min)
                    data[index] = uvMode == UvMode.NoCut ? spectrum.Data.First() : 0.0;
                else if (wave > spectrum.Range.Max)
                    data[index] = spectrum.Data.Last();
                else
                    data[index] = spectrum[wave];
            }

            return data;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = Constants.VALID_WAVE_NUM, Pack = 1)]
    public struct RecordColorData
    {
        public byte Observer; // 观察者角度
        public byte Illuminant; // 光源

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public float[] Values;

        public ColorData ToColorData(RecordFlags.RecordDataType dataType)
        {
            var ret = new ColorData();
            if (dataType == RecordFlags.RecordDataType.XYZ)
            {
                ret.Set((StandardObserver)Observer, (StandardIlluminant)Illuminant,
                    new CIEXYZ(Values[0], Values[1], Values[2]));
            }
            else if (dataType == RecordFlags.RecordDataType.Lab)
            {
                var lab = new CIELAB(Values[0], Values[1], Values[2]);
                var xyz = Formula.LabToXyz(lab, ((ColorReader.Illuminant)Illuminant).Uniform(),
                    ((ColorReader.Observer)Observer).Uniform());
                ret.Set((StandardObserver)Observer, (StandardIlluminant)Illuminant, xyz);
            }

            return ret;
        }

        public static RecordColorData FromColorData(ColorData clrData, RecordFlags.RecordDataType dataType = RecordFlags.RecordDataType.Lab)
        {
            if (clrData.Formulas == null)
            {
                throw new ArgumentException("clrData must be XYZ data");
            }

            var formula = clrData.Formulas[0];
            if (dataType == RecordFlags.RecordDataType.Lab)
            {
                var lab = clrData.Lab(formula);
                return new RecordColorData
                {
                    Illuminant = (byte)formula.Illuminant.ToInstrumentForm(),
                    Observer = (byte)formula.Observer.ToInstrumentForm(),
                    Values = new float[] { (float)lab.L, (float)lab.a, (float)lab.b }
                };
            }
            else if (dataType == RecordFlags.RecordDataType.XYZ)
            {
                CIEXYZ xyz = clrData.XYZ(formula);
                return new RecordColorData
                {
                    Illuminant = (byte)formula.Illuminant.ToInstrumentForm(),
                    Observer = (byte)formula.Observer.ToInstrumentForm(),
                    Values = new float[] { (float)xyz.X, (float)xyz.Y, (float)xyz.Z }
                };
            }
            throw new ArgumentException();
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = Constants.VALID_WAVE_NUM, Pack = 1)]
    public struct RecordSpectrumData
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = Constants.VALID_WAVE_NUM)]
        private ushort[] _rawData;

        public double[] Spectrum
        {
            get { return _rawData != null ? Array.ConvertAll(_rawData, x => x * 0.0001) : null; }
            set
            {
                if (value != null && value.Length != Constants.VALID_WAVE_NUM)
                {
                    throw new ArgumentException($"Length must be is {Constants.VALID_WAVE_NUM}");
                }

                _rawData = value == null ? null : Array.ConvertAll(value, x => (ushort)(x * 10000));
            }
        }

        public ColorData ToColorData()
        {
            return new ColorData(new Spectrum(new WavelengthRange(360, 780), Spectrum));
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ToleranceCIE1976
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 10)]
        public float[] _values;

        public float[] Values
        {
            get => _values;
            set => _values = value;
        }
    }

    public struct StandardName
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 8)]
        private string _name;

        public string Name
        {
            get => _name;
            set => _name = value;
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct StandardNames
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 10)]
        private StandardName[] _names;

        public StandardName[] Names
        {
            get => _names;
            set => _names = value;
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct PharmaceuticalConfig
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
        private float[] _aValues;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
        private float[] _bValues;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
        private byte[] _name;

        private float _resultUpper;
        private float _resultLower;

        public float[] AValues
        {
            get => _aValues;
            set => _aValues = value;
        }

        public float[] BValues
        {
            get => _bValues;
            set => _bValues = value;
        }

        public string Name
        {
            get => Encoding.ASCII.GetString(_name);
            set => _name = Encoding.ASCII.GetBytes(value); 
            
        }

        public float Upper
        {
            get => _resultUpper;
            set => _resultUpper = value;
        }

        public float Lower
        {
            get => _resultLower;
            set => _resultLower = value;
        }

      
        public static implicit operator PharmaceuticalConfig(List<PharmaceuticalConfig> v)
        {
            throw new NotImplementedException();
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct PharmaceuticalInfo
    {
        private float _tempUpper;
        private float _tempLower;
        private ushort _waitTime;
        private RtcDateTime _dateTime;

        public float TempUpper
        {
            get => _tempUpper;
            set => _tempUpper = value;
        }

        public float TempLower
        {
            get => _tempLower;
            set => _tempLower = value;
        }

        public ushort WaitTime
        {
            get => _waitTime;
            set => _waitTime = value;
        }

        public DateTime DateTime
        {
            get => _dateTime.ToDateTime();
            set => _dateTime = new RtcDateTime(value);
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct Tolerance
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

        public static Tolerance FromUniformTolerance(DataModel.Tolerance uniformTolerance)
        {
            Tolerance tolerance = new Tolerance { Values = new float[7] };
            if (uniformTolerance == null)
                return tolerance;

            Func<ChromaticityField, float> getLower = field =>
                uniformTolerance.ItemKeys.Contains(field) && uniformTolerance[field].UseForJudgement
                    ? (float)uniformTolerance[field].Lower
                    : 0.0f;
            Func<ChromaticityField, float> getUpper = field =>
                uniformTolerance.ItemKeys.Contains(field) && uniformTolerance[field].UseForJudgement
                    ? (float)uniformTolerance[field].Upper
                    : 0.0f;

            tolerance.dE = getUpper(ChromaticityField.dEab);
            tolerance.dL_Lower = getLower(ChromaticityField.dL);
            tolerance.da_Lower = getLower(ChromaticityField.da);
            tolerance.db_Lower = getLower(ChromaticityField.db);
            tolerance.dL_Upper = getUpper(ChromaticityField.dL);
            tolerance.dL_Upper = getUpper(ChromaticityField.da);
            tolerance.dL_Upper = getUpper(ChromaticityField.db);

            return tolerance;
        }

        public DataModel.Tolerance Uniform()
        {
            var tolerance = new DataModel.Tolerance(
                new DataModel.ChromaticityField[]
                {
                    DataModel.ChromaticityField.dL,
                    DataModel.ChromaticityField.da,
                    DataModel.ChromaticityField.db,
                    DataModel.ChromaticityField.dEab,
                });

            Func<double, double, bool> isLimitValid = (lower, upper) => lower < upper;

            tolerance[DataModel.ChromaticityField.dL] =
                new DataModel.ToleranceItem
                {
                    Lower = dL_Lower,
                    Upper = dL_Upper,
                    UseForJudgement = isLimitValid(dL_Lower, dL_Upper)
                };
            tolerance[DataModel.ChromaticityField.da] =
                new DataModel.ToleranceItem
                {
                    Lower = da_Lower,
                    Upper = da_Upper,
                    UseForJudgement = isLimitValid(da_Lower, da_Upper)
                };
            tolerance[DataModel.ChromaticityField.db] =
                new DataModel.ToleranceItem
                {
                    Lower = db_Lower,
                    Upper = db_Upper,
                    UseForJudgement = isLimitValid(db_Lower, db_Upper)
                };
            tolerance[DataModel.ChromaticityField.dEab] = new DataModel.ToleranceItem
            {
                Upper = dE,
                UseForJudgement = dE > 0
            };
            ;
            return tolerance;
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
    public struct StandardRecord
    {
        private RecordFlags _flags;

        private int _id;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = Constants.RECORD_NAME_MAX)]
        private string _name;

        private RtcDateTime _dateTime;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = Constants.VALID_WAVE_NUM * 2)]
        private byte[] _sci;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = Constants.VALID_WAVE_NUM * 2)]
        private byte[] _sce;

        private Tolerance _tolerance;



        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 45)]
        private byte[] _reserve; // 保留字节，对齐到256字节，flash一页的长度

        private MNRecord _record;

        private ushort _checkSum; // 校验和

        public static StandardRecord FromSpecimen(Specimen specimen)
        {
            StandardRecord record = new StandardRecord();
            try
            {
                record._flags.Caliber = specimen.MeasurementCaliber.ToInstrumentForm();
            }
            catch
            {
                record._flags.Caliber = Caliber.Phi8;
            }
            try
            {
                record._flags.UvMode = specimen.UV.ToInstrumentForm();
            }
            catch
            {
                record._flags.UvMode = UvMode.NoCut;
            }
            record._flags.IsLock = false;

            record._flags.IsTransmission = specimen.IsTransmission;

            // 获取透镜位置
            record._flags.LensPos = specimen.LensPosition != LensPosition.Unknown
                ? specimen.LensPosition.ToInstrumentForm()
                : RecordHelper.GetLensPosFromCaliber(record.Flags.Caliber);

            // 如果名称过长截断
            record.Name = specimen.Name.Length >= Constants.RECORD_NAME_MAX
                ? specimen.Name.Substring(0, Constants.RECORD_NAME_MAX - 1)
                : specimen.Name;
            record.DateTime = specimen.DateTime;
            record.Id = 0;
            //            record.Temperature = (float)specimen.Temperature;

            if (specimen.SCI?.Spectrum != null || specimen.SCE?.Spectrum != null || specimen.Clr?.Spectrum != null)
            {
                record._flags.DataType = RecordFlags.RecordDataType.Spectrum;
                switch (specimen.SpecularComponent)
                {
                    case SpecularComponent.Unknown:
                        record._flags.ScMode = ScMode.SCI;
                        record.SciSpectrum = RecordHelper.GetSpectrumData(specimen.Clr.Spectrum);
                        break;

                    case SpecularComponent.SCI:
                        record._flags.ScMode = ScMode.SCI;
                        record.SciSpectrum = RecordHelper.GetSpectrumData(specimen.SCI.Spectrum);
                        break;

                    case SpecularComponent.SCE:
                        record._flags.ScMode = ScMode.SCE;
                        record.SceSpectrum = RecordHelper.GetSpectrumData(specimen.SCE.Spectrum);
                        break;

                    default:
                        record._flags.ScMode = ScMode.SCIAndSCE;
                        record.SciSpectrum = RecordHelper.GetSpectrumData(specimen.SCI.Spectrum);
                        record.SceSpectrum = RecordHelper.GetSpectrumData(specimen.SCE.Spectrum);
                        break;
                }
            }
            else
            {
                record._flags.DataType = RecordFlags.RecordDataType.Lab;
                switch (specimen.SpecularComponent)
                {
                    case SpecularComponent.Unknown:
                        record._flags.ScMode = ScMode.SCI;
                        record.SciColorData = RecordColorData.FromColorData(specimen.Clr, record._flags.DataType);
                        break;

                    case SpecularComponent.SCI:
                        record._flags.ScMode = ScMode.SCI;
                        record.SciColorData = RecordColorData.FromColorData(specimen.SCI, record._flags.DataType);
                        break;

                    case SpecularComponent.SCE:
                        record._flags.ScMode = ScMode.SCE;
                        record.SceColorData = RecordColorData.FromColorData(specimen.SCE, record._flags.DataType);
                        break;

                    default:
                        record._flags.ScMode = ScMode.SCIAndSCE;
                        record.SciColorData = RecordColorData.FromColorData(specimen.SCI, record._flags.DataType);
                        record.SceColorData = RecordColorData.FromColorData(specimen.SCE, record._flags.DataType);
                        break;
                }
            }

            var tolerance = (specimen as StandardSpecimen)?.ToleranceSetting?.PrimaryTolerance;
            record.Tolerance = Tolerance.FromUniformTolerance(tolerance);

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

        //public float Temperature
        //{
        //    get { return _temperature; }

        //    set { _temperature = value; }
        //}

        internal Tolerance Tolerance
        {
            get { return _tolerance; }

            set { _tolerance = value; }
        }

        public ushort CheckSum
        {
            get { return _checkSum; }

            set { _checkSum = value; }
        }

        public double[] SciSpectrum
        {
            get { return Flags.IsSpectrum ? StructConverter.BytesToStruct<RecordSpectrumData>(_sci).Spectrum : null; }
            set
            {
                if (!Flags.IsSpectrum)
                {
                    throw new ArgumentException("Flags.IsSpectrum is false");
                }

                if (value == null)
                {
                    throw new ArgumentNullException();
                }

                var tmp = new RecordSpectrumData { Spectrum = value };
                _sci = StructConverter.StructToBytes(tmp);
            }
        }

        public double[] SceSpectrum
        {
            get { return Flags.IsSpectrum ? StructConverter.BytesToStruct<RecordSpectrumData>(_sce).Spectrum : null; }
            set
            {
                if (!Flags.IsSpectrum)
                {
                    throw new ArgumentException("Flags.IsSpectrum is false");
                }

                if (value == null)
                {
                    throw new ArgumentNullException();
                }

                var tmp = new RecordSpectrumData { Spectrum = value };
                _sce = StructConverter.StructToBytes(tmp);
            }
        }

        public RecordColorData? SciColorData
        {
            get
            {
                return !Flags.IsSpectrum ? StructConverter.BytesToStruct<RecordColorData>(_sci) : (RecordColorData?)null;
            }
            set
            {
                if (Flags.IsSpectrum)
                {
                    throw new ArgumentException("不能是光谱数据");
                }

                if (value == null)
                {
                    throw new ArgumentNullException();
                }
                _sci = new byte[Constants.VALID_WAVE_NUM * 2];
                var temp = StructConverter.StructToBytes(value);
                Array.Copy(temp, _sci, temp.Length);
            }
        }

        public RecordColorData? SceColorData
        {
            get
            {
                return !Flags.IsSpectrum ? StructConverter.BytesToStruct<RecordColorData>(_sce) : (RecordColorData?)null;
            }
            set
            {
                if (Flags.IsSpectrum)
                {
                    throw new ArgumentException("不能是光谱数据");
                }

                if (value == null)
                {
                    throw new ArgumentNullException();
                }
                _sce = new byte[Constants.VALID_WAVE_NUM * 2];
                var temp = StructConverter.StructToBytes(value);
                Array.Copy(temp, _sce, temp.Length);
            }


        }

        public MNRecord Record
        {
            get => _record;
            set => _record = value;
        }

        public int Id
        {
            get { return _id; }

            set { _id = value; }
        }

        public StandardSpecimen ToSpecimen()
        {
            StandardSpecimen specimen = new StandardSpecimen();
            specimen.IsTransmission = Flags.IsTransmission;
            specimen.DateTime = DateTime;
            specimen.ID = Id;
            specimen.LensPosition = Flags.LensPos.Uniform();
            specimen.MeasurementCaliber = Flags.Caliber.Uniform();
            specimen.UV = Flags.UvMode.Uniform();
            //            specimen.Temperature = Temperature;
            specimen.Name = Name;

            if (Flags.IsSpectrum)
            {
                var waverange = new WavelengthRange(Constants.VALID_WAVE_BEGIN, Constants.VALID_WAVE_END, Constants.VALID_WAVE_STEP);
                if (Flags.HasSCI)
                {
                    specimen.SCI = new ColorData(new Spectrum(waverange, SciSpectrum));
                }

                if (Flags.HasSCE)
                {
                    specimen.SCE = new ColorData(new Spectrum(waverange, SceSpectrum));
                }
            }
            else
            {
                if (Flags.HasSCI)
                {
                    specimen.SCI = SciColorData?.ToColorData(Flags.DataType);
                }

                if (Flags.HasSCE)
                {
                    specimen.SCE = SceColorData?.ToColorData(Flags.DataType);
                }
            }

            specimen.ToleranceSetting = new ToleranceSetting { PrimaryTolerance = Tolerance.Uniform() };

            return specimen;
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
    public struct MNRecord
    {
        private byte _id;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 6)]
        private string _name;
        private short _year;
        private byte _month;
        private byte _day;
        private byte _hour;
        private byte _minute;
        private float _avalue;
        private float _bvalue;
        private float _result;
        private byte _unit;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
        private byte[] _reserve; // 保留字节，对齐到256字节，flash一页的长度


        public byte Id
        {
            get => _id;
            set
            {
                _id = value;
            }
        }

        public string Name
        {
            get => _name;
            set => _name = value;
        }

        public DateTime DateTime
        {
            get
            {
                try
                {
                   return new DateTime(_year, _month, _day, _hour, _minute, 0);
                }
                catch
                {
                    return new DateTime();
                }
            }
            set
            {
                _year = (short)value.Year;
                _month = (byte)value.Month;
                _day = (byte)value.Day;
                _hour = (byte)value.Hour;
                _minute = (byte)value.Minute;
            }
        }

        public float AValue
        {
            get => _avalue;
            set => _avalue = value;
        }

        public float BValue
        {
            get => _bvalue;
            set => _bvalue = value;
        }

        public float Result
        {
            get => _result; set => _result = value;
        }

        public char Unit
        {
            get => BitConverter.ToChar(new byte[] { _unit }, 0);
            set => _unit = Convert.ToByte(value);
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
    public struct TrialRecord
    {
        private RecordFlags _flags;

        private int _id;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = Constants.RECORD_NAME_MAX)]
        private string _name;

        private RtcDateTime _dateTime;

        private uint _stdId;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = Constants.VALID_WAVE_NUM * 2)]
        private byte[] _sci;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = Constants.VALID_WAVE_NUM * 2)]
        private byte[] _sce;

        //        private float _temperature;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 101)]
        private byte[] _reserve; // 保留字节，对齐到256字节，flash一页的长度

        private ushort _checkSum; // 校验和

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

        //public float Temperature
        //{
        //    get { return _temperature; }

        //    set { _temperature = value; }
        //}

        public ushort CheckSum
        {
            get { return _checkSum; }

            set { _checkSum = value; }
        }

        public double[] SciSpectrum
        {
            get { return Flags.IsSpectrum ? StructConverter.BytesToStruct<RecordSpectrumData>(_sci).Spectrum : null; }
            set
            {
                if (!Flags.IsSpectrum)
                {
                    throw new ArgumentException("Flags.IsSpectrum is false");
                }

                if (value == null)
                {
                    throw new ArgumentNullException();
                }

                var tmp = new RecordSpectrumData { Spectrum = value };
                _sci = StructConverter.StructToBytes(tmp);
            }
        }

        public double[] SceSpectrum
        {
            get { return Flags.IsSpectrum ? StructConverter.BytesToStruct<RecordSpectrumData>(_sce).Spectrum : null; }
            set
            {
                if (!Flags.IsSpectrum)
                {
                    throw new ArgumentException("Flags.IsSpectrum is false");
                }

                if (value == null)
                {
                    throw new ArgumentNullException();
                }

                var tmp = new RecordSpectrumData { Spectrum = value };
                _sce = StructConverter.StructToBytes(tmp);
            }
        }

        private static RecordColorData? ToRecordColorData(RecordFlags flags, byte[] data)
        {
            if (!flags.IsSpectrum)
            {
                Observer observer = (Observer)data[0];
                Illuminant illuminant = (Illuminant)data[1];
                float[] values = new[]
                {
                        BitConverter.ToSingle(data, 2),
                        BitConverter.ToSingle(data, 6),
                        BitConverter.ToSingle(data, 10)
                };

                return new RecordColorData
                {
                    Observer = (byte)observer,
                    Illuminant = (byte)illuminant,
                    Values = values
                };
            }
            return null;
        }

        public RecordColorData? SciColorData
        {
            get => ToRecordColorData(Flags, _sci);
            set
            {
                if (!Flags.IsSpectrum)
                {
                    throw new ArgumentException("Flags.IsXyz is false");
                }

                if (value == null)
                {
                    throw new ArgumentNullException();
                }

                _sci = StructConverter.StructToBytes(value);
            }
        }

        public RecordColorData? SceColorData
        {
            get => ToRecordColorData(Flags, _sce);
            set
            {
                if (!Flags.IsSpectrum)
                {
                    throw new ArgumentException("Flags.IsXyz is false");
                }

                if (value == null)
                {
                    throw new ArgumentNullException();
                }

                _sce = StructConverter.StructToBytes(value);
            }
        }

        public uint StandardId
        {
            get { return _stdId; }
            set { _stdId = value; }
        }

        public int Id
        {
            get { return _id; }

            set { _id = value; }
        }

        public Specimen ToSpecimen()
        {
            Specimen specimen = new Specimen();
            specimen.IsTransmission = Flags.IsTransmission;
            specimen.DateTime = DateTime;
            specimen.ID = Id;
            specimen.LensPosition = Flags.LensPos.Uniform();
            specimen.MeasurementCaliber = Flags.Caliber.Uniform();
            specimen.UV = Flags.UvMode.Uniform();
            specimen.Tag = StandardId;
            //            specimen.Temperature = Temperature;
            specimen.Name = Name;

            if (Flags.IsSpectrum)
            {
                if (Flags.HasSCI)
                {
                    specimen.SCI = new ColorData(new Spectrum(
                        new WavelengthRange(Constants.VALID_WAVE_BEGIN, Constants.VALID_WAVE_END, Constants.VALID_WAVE_STEP),
                        SciSpectrum));
                }

                if (Flags.HasSCE)
                {
                    specimen.SCE = new ColorData(new Spectrum(
                        new WavelengthRange(Constants.VALID_WAVE_BEGIN, Constants.VALID_WAVE_END, Constants.VALID_WAVE_STEP),
                        SceSpectrum));
                }
            }
            else
            {
                if (Flags.HasSCI)
                {
                    specimen.SCI = SciColorData?.ToColorData(Flags.DataType);
                }

                if (Flags.HasSCE)
                {
                    specimen.SCE = SceColorData?.ToColorData(Flags.DataType);
                }
            }

            return specimen;
        }
    }
}