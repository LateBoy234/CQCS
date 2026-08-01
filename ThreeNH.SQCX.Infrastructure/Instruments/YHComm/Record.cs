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

namespace ThreeNH.SQCX.Infrastructure.Instruments.YHComm
{

    [StructLayout(LayoutKind.Sequential, Size = 64, Pack = 4)]
    public struct RecordColorData
    {
        private const int BYTES_SIZE = Constants.VALID_WAVE_NUM * sizeof(short);

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = BYTES_SIZE)]
        private byte[] bytes;

        public short[] Spectrum
        {
            get
            {
                var spectrum = new short[Constants.VALID_WAVE_NUM];
                for (int i = 0; i < Constants.VALID_WAVE_NUM; i++)
                {
                    spectrum[i] = BitConverter.ToInt16(bytes, i * sizeof(short));
                }

                return spectrum;
            }
            set
            {
                if (value.Length != Constants.VALID_WAVE_NUM)
                {
                    throw new ArgumentException();
                }

                if (bytes == null)
                {
                    bytes = new byte[BYTES_SIZE];
                }

                for (int i = 0; i < Constants.VALID_WAVE_NUM; i++)
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
                ret.Set((StandardObserver) Observer, (StandardIlluminant) Illuminant,
                    new CIEXYZ(Values[0], Values[1], Values[2]));
            }
            else if (dataType == RecordFlags.ColorDataType.Lab)
            {
                var lab = new CIELAB(Values[0], Values[1], Values[2]);
                var xyz = Formula.LabToXyz(lab, ((Illuminant) Illuminant).Uniform(),
                    ((Observer) Observer).Uniform());
                ret.Set((StandardObserver) Observer, (StandardIlluminant) Illuminant, xyz);
            }
            else if (dataType == RecordFlags.ColorDataType.Spectrum)
            {
                Spectrum spectrum = new Spectrum(new WavelengthRange(Constants.VALID_WAVE_BEGIN, Constants.VALID_WAVE_END, Constants.VALID_WAVE_STEP));
                for (int i = 0; i < Constants.VALID_WAVE_NUM; i++)
                {
                    int wavelength = Constants.VALID_WAVE_BEGIN + Constants.VALID_WAVE_STEP * i;
                    spectrum[wavelength] = this.Spectrum[i] * 0.0001;
                }
                ret.Spectrum = spectrum;
            }

            return ret;
        }

        public static RecordColorData FromColorData(ColorData clrData, RecordFlags.ColorDataType dataType = RecordFlags.ColorDataType.Lab)
        {
            if (dataType != RecordFlags.ColorDataType.Spectrum && clrData.Formulas == null)
            {
                throw new ArgumentException("clrData must be XYZ data");
            }

            if (dataType == RecordFlags.ColorDataType.Spectrum)
            {
                short[] spectrum = new short[Constants.VALID_WAVE_NUM];
                for (int i = 0; i < Constants.VALID_WAVE_NUM; i++)
                {
                    int wavelength = Constants.VALID_WAVE_BEGIN + Constants.VALID_WAVE_STEP * i;
                    spectrum[i] = (short) (clrData.Spectrum[wavelength] * 10000);
                }

                return new RecordColorData {Spectrum = spectrum};
            }

            var formula = clrData.Formulas[0];
            if (dataType == RecordFlags.ColorDataType.Lab)
            {
                var lab = clrData.Lab(formula);
                return new RecordColorData
                {
                    Illuminant = (byte) formula.Illuminant.ToInstrumentForm(),
                    Observer = (byte) formula.Observer.ToInstrumentForm(),
                    Values = new float[]{ (float)lab.L, (float) lab.a, (float) lab.b}
                };
            }
            if (dataType == RecordFlags.ColorDataType.XYZ)
            {
                CIEXYZ xyz = clrData.XYZ(formula);
                return new RecordColorData
                {
                    Illuminant = (byte) formula.Illuminant.ToInstrumentForm(),
                    Observer = (byte) formula.Observer.ToInstrumentForm(),
                    Values = new float[]{ (float)xyz.X, (float) xyz.Y, (float) xyz.Z}
                };
            }
            throw new ArgumentException();
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = Constants.VALID_WAVE_NUM, Pack = 4)]
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

                _rawData = value == null ? null : Array.ConvertAll(value, x => (ushort) (x * 10000));
            }
        }

        public ColorData ToColorData()
        {
            return new ColorData(new Spectrum(new WavelengthRange(360, 780), Spectrum));
        }
    }

    /// <summary>
    /// 雾度测量数据，包括透过率
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Size = 4, Pack = 4)]
    public struct HazeData
    {
        private short transmittance; // 透过率
        private short haze;          // 雾度值

        /// <summary>
        /// 透过率，已经是百分比了
        /// </summary>
        public double Transmittance
        {
            get => transmittance * 0.0001;
            set => transmittance = (short) (value * 10000);
        }

        /// <summary>
        /// 雾度值
        /// </summary>
        public double Haze
        {
            get => haze * 0.0001;
            set => haze = (short) (value * 10000);
        }

        public ColorSpecimen.HazeData ToUniform(HazeType hazeType)
        {
            var ret = new ColorSpecimen.HazeData
            {
                Observer = StandardObserver.CIE1931,
                Transmittance = Transmittance,
                Haze = Haze
            };
            
            if (hazeType == HazeType.ASTM_A)
            {
                ret.ReferenceStandard = "ASTM D1003";
                ret.Illuminant = StandardIlluminant.A;
            }
            else if (hazeType == HazeType.ASTM_C)
            {
                ret.ReferenceStandard = "ASTM D1003";
                ret.Illuminant = StandardIlluminant.C;
            }
            else if (hazeType == HazeType.ISO_D65)
            {
                ret.ReferenceStandard = "ISO 13468-1";
                ret.Illuminant = StandardIlluminant.D65;
            }
            else
            {
                throw new ArgumentOutOfRangeException();
            }

            return ret;
        }
    }


    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ToleranceItem
    {
        internal short lower;    // 下限*100
        internal short upper;    // 上限*100

        internal ToleranceItem(short lower, short upper)
        {
            this.lower = lower;
            this.upper = upper;
        }

        public double Lower
        {
            get => lower * 0.01;
            set => lower = (short)(value * 100);
        }

        public double Upper
        {
            get => upper * 0.01;
            set => upper = (short) (value * 100);
        }
    }

    // 颜色容差
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
   public struct ColorTolerance
   {
       internal byte _valid_items;    // 有效的容差项，其中第0~2置位表示items相应索引的项有效，第3位置位表示dE有效
       internal short _dE;                  // 色差公式的容差值，需要除以100
       [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
       internal ToleranceItem[] _items;     // 容差项，长度为3，目前为L、a、b

       internal ColorTolerance(byte valid_items, short dE, ToleranceItem[] items)
       {
           this._valid_items = valid_items;
           this._dE = dE;
           this._items = items;
       }

        public ToleranceItem[] Items
        {
            get => _items;
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException();
                }

                if (value.Length != 3)
                {
                    throw new ArgumentOutOfRangeException();
                }

                _items = value;
            }
        }

        public double DeltaE
        {
            get => _dE * 0.01;
            set => _dE = (short) (value * 100);
        }

        
        public static ColorTolerance FromUniformTolerance(Tolerance uniformTolerance)
        {
            ColorTolerance tolerance = new ColorTolerance {Items = new ToleranceItem[3]};
            if (uniformTolerance == null)
                return tolerance;

            Func<ChromaticityField, ToleranceItem?> getItem = field =>
                uniformTolerance.ItemKeys.Contains(field) && uniformTolerance[field].UseForJudgement
                    ? (ToleranceItem?) new ToleranceItem {Lower = uniformTolerance[field].Lower.GetValueOrDefault(0), Upper = uniformTolerance[field].Upper.GetValueOrDefault(0)}
                    : null;

            if (uniformTolerance.ItemKeys.Contains(ChromaticityField.dEab))
            {
                tolerance._valid_items = (byte) (tolerance._valid_items | 0x08);
                tolerance.DeltaE = uniformTolerance[ChromaticityField.dEab].Upper.GetValueOrDefault(1.0);
            }

            ChromaticityField[] fields = new[] {ChromaticityField.dL, ChromaticityField.da, ChromaticityField.db};
            for (int i = 0; i < 3; i++)
            {
                var item = getItem(fields[i]);
                if (item != null)
                {
                    tolerance._valid_items = (byte) (tolerance._valid_items | (1 << i));
                    tolerance.Items[i] = (ToleranceItem)item;
                }
            }

            return tolerance;
        }


        public Tolerance Uniform()
        {
            var tolerance = new Tolerance();

            if ((_valid_items & 0x08) != 0)
            {
                tolerance[ChromaticityField.dEab] = new DataModel.ToleranceItem
                {
                    Lower = null,
                    Upper = DeltaE,
                    UseForJudgement = true
                };
            }
            ChromaticityField[] fields = new[] {ChromaticityField.dL, ChromaticityField.da, ChromaticityField.db};
            for (int i = 0; i < 3; i++)
            {
                if ((_valid_items & (1 << i)) != 0)
                {
                    tolerance[fields[i]] = new DataModel.ToleranceItem
                    {
                        Lower = Items[i].Lower,
                        Upper = Items[i].Upper,
                        UseForJudgement = true
                    };
                }
            }

            return tolerance;
        }
   };

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct RecordTolerance
    {
        /// <summary>
        /// 标志位
        /// </summary>
        /// <remarks>
        /// <c>flags[0]</c>: dE是否有效
        /// <c>flags[1:3]</c>: 颜色容差项items[0:2] 是否有效
        /// <c>flags[4]</c>: 透过率容差是否有效
        /// <c>flags[5]</c>: 雾度容差是否有效
        /// <c>flags[6]</c>: 清晰度容差是否有效
        /// </remarks>
        private ushort flags;

        /// <summary>
        /// dE * 100
        /// </summary>
        private ushort dE; 

        /// <summary>
        /// 0～2分别对应L*、a*、b*的容差乘以100
        /// </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        private ToleranceItem[] clr_items;

        /// <summary>
        /// 透过率的容差
        /// </summary>
        private ToleranceItem trans;

        /// <summary>
        /// 雾度的容差
        /// </summary>
        private ToleranceItem haze;

        /// <summary>
        /// 清晰度的容差
        /// </summary>
        private ToleranceItem clarity;


        /// <summary>
        /// <c>dE</c>是否有效
        /// </summary>
        public bool IsDEValid
        {
            get => (flags & 0x01) != 0;
            set => flags |= 0x01;
        }

        /// <summary>
        /// 判断<c>items[index]</c>是否有效
        /// </summary>
        /// <param name="index">要判断的颜色容差项的序号</param>
        /// <returns></returns>
        public bool IsItemValid(int index)
        {
            return (flags & (0x02 << index)) != 0;
        }

        /// <summary>
        /// 设置<c>items[index]</c>是否有效
        /// </summary>
        /// <param name="index">要设置的颜色容差项的序号</param>
        /// <param name="valid">设置是否有效</param>
        public void SetItemValid(int index, bool valid)
        {
            flags |= (ushort)(0x02 << index);
        }

        /// <summary>
        /// 透过率容差是否有效
        /// </summary>
        public bool IsTransValid
        {
            get => (flags & 0x10) != 0;
            set => flags |= 0x10;
        }

        /// <summary>
        /// 雾度容差是否有效
        /// </summary>
        public bool IsHazeValid
        {
            get => (flags & 0x20) != 0;
            set => flags |= 0x20;
        }

        /// <summary>
        /// 清晰度容差是否有效
        /// </summary>
        public bool IsClarityValid
        {
            get => (flags & 0x40) != 0;
            set => flags &= 0x40;
        }

        public ToleranceItem? HazeTolerance
        {
            get => IsHazeValid ? (ToleranceItem?)haze : null;
            set
            {
                if (value is ToleranceItem item)
                {
                    haze = item;
                    IsHazeValid = true;
                }
                else
                {
                    IsHazeValid = false;
                }
            }
        }

        public ToleranceItem? TransToleranceItem
        {
            get => IsTransValid? (ToleranceItem?) trans : null;
            set
            {
                if (value is ToleranceItem item)
                {
                    trans = item;
                    IsTransValid = true;
                }
                else
                {
                    IsTransValid = false;
                }
            }
        }

        public ColorTolerance? ColorTolerance
        {
            get
            {
                int valid_item = 0;


                for (int i = 0; i < 3; i++)
                {
                    if (IsItemValid(i))
                    {
                        valid_item |= 1 << i;
                    }
                }

                if (IsDEValid)
                {
                    valid_item |= 0x80;
                }

                return new ColorTolerance((byte)valid_item, (short)dE, clr_items);
            }
            set
            {
                if (value is ColorTolerance t)
                {
                    dE = (ushort)t._dE;
                    clr_items = t.Items;
                    for (int i = 0; i < 3; i++)
                    {
                        SetItemValid(i, (t._valid_items & (1 << i)) != 0);
                    }

                    IsDEValid = (t._valid_items & 0x08) != 0;
                }
                else
                {
                    IsDEValid = false;
                    for (int i = 0; i < 3; i++)
                    {
                        SetItemValid(i, false);
                    }
                }

            }
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 128, Pack = 4, CharSet = CharSet.Ansi)]
    public struct StandardRecord
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
        private RecordColorData _color;

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

        public static StandardRecord FromSpecimen(Specimen specimen)
        {
            StandardRecord record = new StandardRecord();
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

            if (specimen.SCI?.Spectrum != null || specimen.SCE?.Spectrum != null|| specimen.Clr?.Spectrum != null)
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
                    record._color = RecordColorData.FromColorData(specimen.Clr, record._flags.ColorType);
                    break;
                case SpecularComponent.SCI:
                case SpecularComponent.SCIAndSCE:
                    record._color = RecordColorData.FromColorData(specimen.SCI, record._flags.ColorType);
                    break;
                case SpecularComponent.SCE:
                    record._color = RecordColorData.FromColorData(specimen.SCE, record._flags.ColorType);
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
                specimen.HazeDatas[i] = hazes[i].ToUniform((HazeType) i);
            }

            if (Flags.IsColorVaild)
            {
                var waverange = new WavelengthRange(Constants.VALID_WAVE_BEGIN, Constants.VALID_WAVE_END, Constants.VALID_WAVE_STEP);
                specimen.Clr = _color.ToColorData(Flags.ColorType);
            }

            var tolerance =  Tolerance.ColorTolerance;
            if (tolerance != null)
            {
                specimen.ToleranceSetting = new ToleranceSetting {PrimaryTolerance = tolerance.Value.Uniform()};
            }

            return specimen;
        }

        #endregion
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4, CharSet = CharSet.Ansi)]
    public struct TrialRecord
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
        private RecordColorData color;

        
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

        public static TrialRecord FromSpecimen(Specimen specimen)
        {
            TrialRecord record = new TrialRecord();
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

            if (specimen.SCI?.Spectrum != null || specimen.SCE?.Spectrum != null|| specimen.Clr?.Spectrum != null)
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
                    record.color = RecordColorData.FromColorData(specimen.Clr, record._flags.ColorType);
                    break;
                case SpecularComponent.SCI:
                case SpecularComponent.SCIAndSCE:
                    record.color = RecordColorData.FromColorData(specimen.SCI, record._flags.ColorType);
                    break;
                case SpecularComponent.SCE:
                    record.color = RecordColorData.FromColorData(specimen.SCE, record._flags.ColorType);
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
                specimen.HazeDatas[i] = hazes[i].ToUniform((HazeType) i);
            }

            if (Flags.IsColorVaild)
            {
                var waverange = new WavelengthRange(Constants.VALID_WAVE_BEGIN, Constants.VALID_WAVE_END, Constants.VALID_WAVE_STEP);
                specimen.Clr = color.ToColorData(Flags.ColorType);
            }

            return specimen;
        }
    }
}