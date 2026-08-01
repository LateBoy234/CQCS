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

namespace ThreeNH.SQCX.Infrastructure.Instruments.BTS
{
    static class RecordHelper
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
                case Caliber.Phi15:
                    return LensPos.Pos15;
                case Caliber.Phi25:
                    return LensPos.Pos25;
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

        public static HazeData[] CalculateHazeDatas(ushort[] temp)
        {
            var recordHazeData = temp.Select(x => x / 100.0).ToArray();
            var hazeDatas = new HazeData[3] { new HazeData(), new HazeData(), new HazeData() };

            double t1, t2, t3, t4;

            t1 = recordHazeData[1];
            t3 = recordHazeData[0];
            t2 = recordHazeData[3];
            t4 = recordHazeData[2];

            hazeDatas[0].Illuminant = StandardIlluminant.C;
            hazeDatas[0].Observer = StandardObserver.CIE1931;
            hazeDatas[0].Haze = Formula.CalculateHaze(t1, t2, t3, t4);
            hazeDatas[0].Transmittance = t2 / t1;
            hazeDatas[0].ReferenceStandard = "ASTM D1003";


            t1 = recordHazeData[1];
            t3 = recordHazeData[0];
            t2 = recordHazeData[5];
            t4 = recordHazeData[4];

            hazeDatas[1].Illuminant = StandardIlluminant.A;
            hazeDatas[1].Observer = StandardObserver.CIE1931;
            hazeDatas[1].Haze = Formula.CalculateHaze(t1, t2, t3, t4);
            hazeDatas[1].Transmittance = t2 / t1;
            hazeDatas[1].ReferenceStandard = "ASTM D1003";



            t1 = recordHazeData[1];
            t3 = recordHazeData[0];
            t2 = recordHazeData[7];
            t4 = recordHazeData[6];

            hazeDatas[2].Illuminant = StandardIlluminant.D65;
            hazeDatas[2].Observer = StandardObserver.CIE1931;
            hazeDatas[2].Haze = Formula.CalculateHaze(t1, t2, t3, t4);
            hazeDatas[2].Transmittance = t2 / t1;
            hazeDatas[2].ReferenceStandard = "ISO 13468-1";

            return hazeDatas;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = Constants.VALID_WAVE_NUM, Pack = 1)]
    public struct RecordXyzData
    {
        public byte Observer; // 观察者角度
        public byte Illuminant; // 光源
        public float X; // X值
        public float Y; // Y值
        public float Z; // Z值

        public ColorData ToColorData()
        {
            var ret = new ColorData();
            ret.Set((StandardObserver)Observer, (StandardIlluminant)Illuminant,
                new CIEXYZ(X, Y, Z));
            return ret;
        }

        public static RecordXyzData FromColorData(ColorData clrData)
        {
            if (clrData.Formulas == null)
            {
                throw new ArgumentException("clrData must be XYZ data");
            }

            var formula = clrData.Formulas[0];
            CIEXYZ xyz = clrData.XYZ(formula);
            return new RecordXyzData
            {
                Illuminant = (byte)formula.Illuminant.ToInstrumentForm(),
                Observer = (byte)formula.Observer.ToInstrumentForm(),
                X = (float)xyz.X,
                Y = (float)xyz.Y,
                Z = (float)xyz.Z
            };
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

        private float _temperature;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        private ushort[] _hazeData; // 雾度数据，对应值如下
        //private ushort _hazeT3; // T3*100
        //private ushort _hazeT1; // T1*100
        //private ushort _hazeC2T4; // 雾度C/2条件下的T4*100
        //private ushort _hazeC2T2; // 雾度C/2条件下的T2*100
        //private ushort _hazeA2T4; // 雾度A/2条件下的T4*100
        //private ushort _hazeA2T2; // 雾度A/2条件下的T2*100
        //private ushort _hazeD65T4; // 雾度D65/2条件下的T4*100
        //private ushort _hazeD65T2; // 雾度D65/2条件下的T2*100
        private byte _hazeFlag;     // 雾度记录标志位，如果为1表示是雾度记录
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
        private byte[] _reserve; // 保留字节，对齐到256字节，flash一页的长度

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
            record.Name = specimen.Name.Length > Constants.RECORD_NAME_MAX
                ? specimen.Name.Substring(0, Constants.RECORD_NAME_MAX)
                : specimen.Name;
            record.DateTime = specimen.DateTime;
            record.Id = 0;
            record.Temperature = (float)specimen.Temperature;

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
                record._flags.DataType = RecordFlags.RecordDataType.XYZ;
                switch (specimen.SpecularComponent)
                {
                    case SpecularComponent.Unknown:
                        record._flags.ScMode = ScMode.SCI;
                        record.SciXyzData = RecordXyzData.FromColorData(specimen.Clr);


                        break;
                    case SpecularComponent.SCI:
                        record._flags.ScMode = ScMode.SCI;
                        record.SciXyzData = RecordXyzData.FromColorData(specimen.SCI);
                        break;
                    case SpecularComponent.SCE:
                        record._flags.ScMode = ScMode.SCE;
                        record.SceXyzData = RecordXyzData.FromColorData(specimen.SCE);
                        break;
                    default:
                        record._flags.ScMode = ScMode.SCIAndSCE;
                        record.SciXyzData = RecordXyzData.FromColorData(specimen.SCI);
                        record.SceXyzData = RecordXyzData.FromColorData(specimen.SCE);
                        break;
                }

                if (record._flags.ScMode != ScMode.SCE)
                {
                    record._flags.Illuminant = (byte)record.SciXyzData?.Illuminant;
                    record._flags.Observer = (byte)record.SciXyzData?.Observer;
                }
                else
                {
                    record._flags.Illuminant = (byte)record.SceXyzData?.Illuminant;
                    record._flags.Observer = (byte)record.SceXyzData?.Observer;
                }
            }

            var tolerance = (specimen as StandardSpecimen)?.ToleranceSetting?.PrimaryTolerance;
            record.Tolerance = Tolerance.FromUniformTolerance(tolerance);

            // 如果Tag是Tuple<ColorData, ColorData>表示是雾度记录
            var tag = specimen.Tag as Tuple<ColorData, ColorData>;
            if (tag != null)
            {
                var formulaA2 = new Formula(StandardIlluminant.A, StandardObserver.CIE1931);
                var t1A2 = tag.Item1.XYZ(formulaA2);
                var t3A2 = tag.Item2.XYZ(formulaA2);
                var t2A2 = specimen.SCI?.XYZ(formulaA2);
                var t4A2 = specimen.SCE?.XYZ(formulaA2);

                var formulaC2 = new Formula(StandardIlluminant.C, StandardObserver.CIE1931);
                var t1C2 = tag.Item1.XYZ(formulaC2);
                var t3C2 = tag.Item2.XYZ(formulaC2);
                var t2C2 = specimen.SCI?.XYZ(formulaC2);
                var t4C2 = specimen.SCE?.XYZ(formulaC2);

                var formulaD65 = new Formula(StandardIlluminant.D65, StandardObserver.CIE1931);
                var t1D65 = tag.Item1.XYZ(formulaD65);
                var t3D65 = tag.Item2.XYZ(formulaD65);
                var t2D65 = specimen.SCI?.XYZ(formulaD65);
                var t4D65 = specimen.SCE?.XYZ(formulaD65);

                if (t1A2 != null && t2A2 != null && t3A2 != null && t4A2 != null
                    && t1C2 != null && t2C2 != null && t3C2 != null && t4C2 != null
                    && t1D65 != null && t2D65 != null && t3D65 != null && t4D65 != null)
                {
                    record.HazeFlag = true;
                    record._hazeData = new ushort[8]
                    {
                        10, // 0.1
                        10010, // 100.01
                        (ushort) (t2A2.Y * 100),
                        (ushort) (t4A2.Y * 100),
                        (ushort) (t2C2.Y * 100),
                        (ushort) (t4C2.Y * 100),
                        (ushort) (t2D65.Y * 100),
                        (ushort) (t4D65.Y * 100)
                    };
                }
            }
            else if (specimen.Tag is double[] hazeData && hazeData.Length == 8) // 仪器上传的雾度记录
            {
                record.HazeFlag = true;
                record._hazeData = new ushort[8];
                for (int i = 0; i < 8; i++)
                {
                    record._hazeData[i] = (ushort)(hazeData[i] * 100.0);
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

        public float Temperature
        {
            get { return _temperature; }

            set { _temperature = value; }
        }

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

        private RecordXyzData? toRecordXyzData(RecordFlags flags, byte[] data)
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
                if (flags.IsLab)
                {
                    var xyz = Formula.LabToXyz(new CIELAB(values[0], values[1], values[2]), illuminant.Uniform(), observer.Uniform());
                    return new RecordXyzData
                    {
                        Observer = (byte)observer,
                        Illuminant = (byte)illuminant,
                        X = (float)xyz.X,
                        Y = (float)xyz.Y,
                        Z = (float)xyz.Z
                    };
                }
                else if (flags.IsXyz)
                {
                    return new RecordXyzData
                    {
                        Observer = (byte)observer,
                        Illuminant = (byte)illuminant,
                        X = values[0],
                        Y = values[1],
                        Z = values[2]
                    };
                }
            }
            return null;
        }

        public RecordXyzData? SciXyzData
        {
            get
            {
                return toRecordXyzData(Flags, _sci);
            }
            set
            {
                if (Flags.IsSpectrum)
                {
                    throw new ArgumentException("Flags.IsXyz is false");
                }

                if (value == null)
                {
                    throw new ArgumentNullException();
                }
                _sci = new byte[Constants.VALID_WAVE_NUM * 2];
                var tmp = StructConverter.StructToBytes(value);
                Array.Copy(tmp, _sci, tmp.Length);
            }
        }

        public RecordXyzData? SceXyzData
        {
            get
            {
                return toRecordXyzData(Flags, _sce);
            }
            set
            {
                if (Flags.IsSpectrum)
                {
                    throw new ArgumentException("Flags.IsXyz is false");
                }

                if (value == null)
                {
                    throw new ArgumentNullException();
                }

                _sce = new byte[Constants.VALID_WAVE_NUM * 2];
                var tmp = StructConverter.StructToBytes(value);
                Array.Copy(tmp, _sce, tmp.Length);
            }
        }

        public int Id
        {
            get { return _id; }

            set { _id = value; }
        }

        public bool HazeFlag
        {
            get { return _hazeFlag != 0; }
            set { _hazeFlag = (byte)(value ? 1 : 0); }
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
            specimen.Temperature = Temperature;
            specimen.Name = Name;

            if (Flags.IsSpectrum)
            {
                if (Flags.HasSCI)
                {
                    specimen.SCI = new ColorData(new Spectrum(new WavelengthRange(360, 780), SciSpectrum));
                }

                if (Flags.HasSCE)
                {
                    specimen.SCE = new ColorData(new Spectrum(new WavelengthRange(360, 780), SceSpectrum));
                }
            }
            else
            {
                if (Flags.HasSCI)
                {
                    specimen.SCI = SciXyzData?.ToColorData();
                }

                if (Flags.HasSCE)
                {
                    specimen.SCE = SceXyzData?.ToColorData();
                }
            }

            specimen.ToleranceSetting = new ToleranceSetting { PrimaryTolerance = Tolerance.Uniform() };

            if (HazeFlag)
            {
                specimen.HazeDatas = RecordHelper.CalculateHazeDatas(_hazeData);
            }
            return specimen;
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

        private int _stdId;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = Constants.VALID_WAVE_NUM * 2)]
        private byte[] _sci;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = Constants.VALID_WAVE_NUM * 2)]
        private byte[] _sce;

        private float _temperature;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        private ushort[] _hazeData; // 雾度数据，对应值如下
        //private ushort _hazeT3; // T3*100
        //private ushort _hazeT1; // T1*100
        //private ushort _hazeC2T4; // 雾度C/2条件下的T4*100
        //private ushort _hazeC2T2; // 雾度C/2条件下的T2*100
        //private ushort _hazeA2T4; // 雾度A/2条件下的T4*100
        //private ushort _hazeA2T2; // 雾度A/2条件下的T2*100
        //private ushort _hazeD65T4; // 雾度D65/2条件下的T4*100
        //private ushort _hazeD65T2; // 雾度D65/2条件下的T2*100
        private byte _hazeFlag;     // 雾度记录标志位，如果为1表示是雾度记录

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 25)]
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

        public float Temperature
        {
            get { return _temperature; }

            set { _temperature = value; }
        }


        public bool HazeFlag
        {
            get { return _hazeFlag != 0; }
            set { _hazeFlag = (byte)(value ? 1 : 0); }
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

        public RecordXyzData? SciXyzData
        {
            get
            {
                return Flags.IsSpectrum ? StructConverter.BytesToStruct<RecordXyzData>(_sci) : (RecordXyzData?)null;
            }
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

        public RecordXyzData? SceXyzData
        {
            get
            {
                return Flags.IsSpectrum ? StructConverter.BytesToStruct<RecordXyzData>(_sce) : (RecordXyzData?)null;
            }
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

        public int StandardId
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
            specimen.Temperature = Temperature;
            specimen.Name = Name;

            if (Flags.IsSpectrum)
            {
                if (Flags.HasSCI)
                {
                    specimen.SCI = new ColorData(new Spectrum(new WavelengthRange(360, 780), SciSpectrum));
                }

                if (Flags.HasSCE)
                {
                    specimen.SCE = new ColorData(new Spectrum(new WavelengthRange(360, 780), SceSpectrum));
                }
            }
            else
            {
                if (Flags.HasSCI)
                {
                    specimen.SCI = SciXyzData?.ToColorData();
                }

                if (Flags.HasSCE)
                {
                    specimen.SCE = SceXyzData?.ToColorData();
                }
            }

            if (HazeFlag)
            {
                specimen.HazeDatas = RecordHelper.CalculateHazeDatas(_hazeData);
            }

            return specimen;
        }
    }
}