using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using ThreeNH.ColorAlgorithm;
using ThreeNH.ColorimeterInterface;
using ThreeNH.Optical;

namespace ThreeNH.YHComm
{

    // 产品参数字段
    public enum ProductInfoField : byte
    {
        INTERNAL_MODEL,				// 内部型号
        DEFAULT_LANGUAGE,				// 默认语言
        DEVICE_MODEL_NAME,             // 仪器内部型号名
        SOFTWARE_VERSION,              // 软件版本
        HARDWARE_VERSION,              // 硬件版本
        SN,             // 序列号
        SPECTROGRAPH,      // 光谱仪
        REFLECT_MODIFY, // 反射率修正系数，其他各种修正参数在这里增加  
        COMPENSATION_PORT_TRAVEL,                    // DC马达到补偿口的行程
        HAZE_CORRECTION_COEFFICIENTS,  // 雾度修正系数
        TRANS_CORRECTION_COEFFICIENTS, // 透过率修正系数
        LAMPS,                 // 使用的光源
        CLARITY_STANDARD,      // 清晰度标准板设置
        BLUETOOTH_NAME,        // 蓝牙默认名称
        DEFAULT_APERTURE,      // 默认口径
        MANUFACTURER,          // 生产厂商
        FREQUENCE,             // 采样频率
        TRAP_TRAVEL,           // DC马达到光阱反应时间行程
        CALIBRATION_LIMITS,    // 校正限制
        CALIBRATION_LIMITS_ENABLE, // 校正限制开关
        MOTOR_TYPE,            // DC马达的类型（带码盘或不带）
        //-------
    }

    /// <summary>
    /// 光谱仪参数
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 4)]
    public struct SpectrographParameter
    {
        /// <summary>
        /// 编号
        /// </summary>
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)]
        public string SN;

        /// <summary>
        /// 系数
        /// </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
        public float[] Coefficients;

        public void Init()
        {
            Coefficients = new float[6];
        }
    }

    /// <summary>
    /// 修正方法
    /// </summary>
    public enum CorrectionMethod
    {
        POLYNOMIAL,   // 多项式（序号从0到n，依次为a0,a1,...,an-1
        EXPONENT,     // 指数，依次为：a0 + a1^(a2*x)，系数个数必须为3
        LN,           // 自然对数：a0 + a1*ln(x)，系数个数必须为2
        LOG10,        // 以10为底的对数：a0 + a1*log10(x)，系数个数必须为2
        BINARY,       // 二元线性函数：a0 + a1*x + a2*y + a3*x*y + a4*x*x + a5*y*y
        SEGMENTS,     // 分段线性修正，如果为该修正方式，指定的修正结果类型会被忽略
    }

    /// <summary>
    /// 修正的结果类型
    /// </summary>
    public enum CorrectionResult
    {
        /// <summary>
        /// 修正比例，原始值乘以公式计算结果
        /// </summary>
        K,
        /// <summary>
        /// 差值，原始值加上公式计算结果
        /// </summary>
        DIFF,
        /// <summary>
        /// 最终结果，公式计算的结果为最终结果
        /// </summary>
        FINAL
    }

    /// <summary>
    /// 分段修正的数据对
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct CorrectionPoint
    {
        /// <summary>
        /// 对应原始测量的雾度或透过率x100
        /// </summary>
        internal Int16 _x; 
        /// <summary>
        /// 对应理想的雾度或透过率x100
        /// </summary>
        internal Int16 _y; 

        /// <summary>
        /// 原始雾度或透过率
        /// </summary>
        public double X { get => _x * 0.01; set => _x = (Int16)(value * 100); }
        /// <summary>
        /// 理想雾度或透过率
        /// </summary>
        public double Y { get => _y * 0.01; set => _y = (Int16)(value * 100); }
    }


    /// <summary>
    /// 修正系数
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct CorrectionCoefficients
    {
        byte n; // 修正系数个数，最大为10
        byte method; // 修正方法
        byte result_type; // 公式计算出的结果
        byte aperture;     // 口径，仅在通讯时使用
        short lower;      // 修正的下限乘以100
        short upper;      // 修正的上限乘以100
        /// <summary>
        /// 修正系数
        /// 如果是分段修正，为<see cref="CorrectionPoint"/>
        /// 否则为<c>float</c>。
        /// </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 40)]
        byte[] coefficients;

        /// <summary>
        /// 修正点个数
        /// </summary>
        public byte N {get => n; set => n = value;}

        /// <summary>
        /// 修正方法
        /// </summary>
        public CorrectionMethod CorrectionMethod
        {
            get => (CorrectionMethod)method;
            set => method = (byte)value;
        }

        /// <summary>
        /// 修正结果；如果修正方法是分段修正则忽略
        /// </summary>
        public CorrectionResult CorrectionResult
        {
            get => (CorrectionResult)result_type;
            set => result_type = (byte)CorrectionResult;
        }

        /// <summary>
        /// 当测量值小于该值时不修正
        /// </summary>
        public double Lower {get => lower * 0.01; set => lower = (Int16)(value * 100);}
        /// <summary>
        /// 当测量值大于该值时不修正
        /// </summary>
        public double Upper {get => upper * 0.01; set => upper = (Int16)(value * 100);}

        /// <summary>
        /// 修正系数，当修正方式为非分段修正时有效
        /// </summary>
        public double[] Coefficients
        {
            get
            {
                if (CorrectionMethod == CorrectionMethod.SEGMENTS)
                {
                    return null;
                }
                var arr = new double[N];
                for (int i = 0; i < N; i++)
                {
                    arr[i] = BitConverter.ToSingle(coefficients, i * 4);
                }
                return arr;
            }
            set
            {
                if (CorrectionMethod == CorrectionMethod.SEGMENTS)
                {
                    throw new NotSupportedException("Not supported assignment operation when the correction method is `SEGMENTS`");
                }
                if (value.Length > 10)
                {
                    throw new ArgumentOutOfRangeException();
                }
                coefficients = new byte[10 * 4];
                if (value == null || value.Length == 0)
                {
                    N = 0;
                    return;
                }
                N = (byte)value.Length;
                for (int i = 0; i < value.Length; i++)
                {
                    var bytes = BitConverter.GetBytes((float)value[i]);
                    for (int k = 0; k < 4; k++)
                    {
                        coefficients[i * 4 + k] = bytes[k];
                    }
                }
            }
        }

        /// <summary>
        /// 分段修正时的分段点
        /// </summary>
        public CorrectionPoint[] Segments
        {
            get
            {
                if (CorrectionMethod != CorrectionMethod.SEGMENTS)
                {
                    return null;
                }
                var arr = new CorrectionPoint[N];
                for (int i = 0; i < N; i++)
                {
                    arr[i]._x = BitConverter.ToInt16(coefficients, i * 4);
                    arr[i]._y = BitConverter.ToInt16(coefficients, i * 4 + 2);
                }
                return arr;
            }
            set
            {
                if (CorrectionMethod != CorrectionMethod.SEGMENTS)
                {
                    throw new NotSupportedException("Not supported assignment operation when the correction method is not `SEGMENTS`");
                }

                coefficients = new byte[10 * 4];
                if (value == null || value.Length == 0)
                {
                    N = 0;
                    return;
                }

                N = (byte)value.Length;
                List<byte> list = new List<byte>();
                for (int i = 0; i < value.Length; i++)
                {
                    list.AddRange(BitConverter.GetBytes(value[i]._x));
                    list.AddRange(BitConverter.GetBytes(value[i]._y));
                }
                for (int i = 0; i < list.Count; i++)
                {
                    coefficients[i] = list[i];
                }
            }
        }

        /// <summary>
        /// 修正的口径
        /// </summary>
        public MeasurementCaliber Aperture { 
            get => ((Aperture)aperture).Uniform(); 
            set => aperture = (byte)value.ToInstrumentForm();
        }
    }


    /// <summary>
    /// RTC时间
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Size = 8, Pack = 1)]
    internal struct RtcDateTime
    {
        public ushort year;
        public byte month;
        public byte date;
        public byte weekday;    // 1~6表示周1到周6，7表示周日
        public byte hour;
        public byte minute;
        public byte second;

        public DateTime ToDateTime()
        {
            try
            {
                return new DateTime(year, month, date, hour, minute, second);
            }
            catch
            {
                return DateTime.Now;
            }
        }

        public RtcDateTime(DateTime dateTime)
        {
            year = (ushort)dateTime.Year;
            month = (byte)dateTime.Month;
            date = (byte)dateTime.Day;
            hour = (byte)dateTime.Hour;
            minute = (byte)dateTime.Minute;
            second = (byte)dateTime.Second;
            weekday = dateTime.DayOfWeek == DayOfWeek.Sunday ? (byte)7 : (byte)dateTime.DayOfWeek;
        }
    }

    /// <summary>
    /// 设备信息
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
    public struct DeviceInfo
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)]
        string model; // 仪器型号

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)]
        string optical; // 光学结构

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)]
        string sn; // 序列号

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        string software_version; // 软件版本号

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)]
        string hardware_version; // 硬件版本号

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)]
        string white_board_number; // 白板编号

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)]
        string black_bucket_number; // 黑桶编号

        ushort standard_capacity; // 标样容量
        ushort trial_capacity; // 试样容量

        private byte is_color_haze_meter; // 是否是色彩雾度计
        private byte has_compensation_port; // 是否拥有补偿测量口

        private byte isClarityValid;

        private byte reserved;

        public string Model
        {
            get { return model; }

            set { model = value; }
        }

        public string Optical
        {
            get { return optical; }

            set { optical = value; }
        }

        public string Sn
        {
            get { return sn; }

            set { sn = value; }
        }


        public bool IsClarityValid
        {
            get => isClarityValid != 0x00;
            set => isClarityValid = (byte)(value ? 0x01 : 0x00);
        }
        public string SoftwareVersion
        {
            get { return software_version; }

            set { software_version = value; }
        }

        public string HardwareVersion
        {
            get { return hardware_version; }

            set { hardware_version = value; }
        }

        public string WhiteBoardNumber
        {
            get { return white_board_number; }

            set { white_board_number = value; }
        }

        public string BlackBucketNumber
        {
            get { return black_bucket_number; }

            set { black_bucket_number = value; }
        }

        public ushort StandardCapacity
        {
            get { return standard_capacity; }

            set { standard_capacity = value; }
        }

        public ushort TrialCapacity
        {
            get { return trial_capacity; }

            set { trial_capacity = value; }
        }

        public bool IsColorHazeMeter
        {
            get => is_color_haze_meter != 0;
            set => is_color_haze_meter = (byte)(value ? 1 : 0);
        }

        public bool HasCompensationPort
        {
            get => has_compensation_port != 0;
            set => has_compensation_port = (byte)(value ? 1 : 0);
        }
    };

    [StructLayout(LayoutKind.Sequential)]
    public struct InstrumentStatus
    {
        byte caliber;           // 口径
        byte lens_pos;          // 透镜位置，忽略
        byte transmission;      // 是否是透射
        byte sc_mode;           // 镜面反射光测量方式，忽略
        byte uv_mode;           // UV模式，忽略
        byte observer;          // 观察者角度
        byte light_type;        // 当前光源设置
        byte color_space;       // 颜色空间
        byte color_diff_formula; // 色差公式
        byte color_index;        // 颜色指数
        private ushort standard_count;  // 标样数
        private ushort trial_count; // 试样数
        private byte is_calibrated; // 是否已校正
        private byte lens_switch_mode;  // 透镜切换方式，忽略
        private byte ref_standard;      // 测量参考标准
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        byte[] reserved;       // 保留以

        public MeasurementCaliber Caliber
        {
            get
            {
                return ((Aperture)caliber).Uniform();
            }

            set
            {
                caliber = (byte)value.ToInstrumentForm();
            }
        }

        public bool IsTransmission => true;


        public StandardObserver Observer
        {
            get
            {
                return ((Observer)observer).Uniform();
            }

            set
            {
                observer = (byte)value.ToInstrumentForm();
            }
        }

        public StandardIlluminant Illuminant
        {
            get
            {
                return ((Illuminant)light_type).Uniform();
            }

            set
            {
                light_type = (byte)value.ToInstrumentForm();
            }
        }

        public ThreeNH.Optical.ColorSpace ColorSpace
        {
            get
            {
                return ((ColorSpace)color_space).Uniform();
            }

            set
            {
                color_space = (byte)value.ToInstrumentForm();
            }
        }

        public ThreeNH.Optical.ColorDiffFormula ColorDiffFormula
        {
            get
            {
                return ((ColorDeltaFormula)color_diff_formula).Uniform();
            }

            set
            {
                color_diff_formula = (byte)value.ToInstrumentForm();
            }
        }

        public ThreeNH.Optical.ColorIndex ColorIndex
        {
            get
            {
                return ((ColorIndex)color_index).Uniform();
            }

            set
            {
                color_index = (byte)value.ToInstrumentForm();
            }
        }

        public ushort StandardCount
        {
            get { return standard_count; }
            set { standard_count = value; }
        }

        public ushort TrialCount
        {
            get { return trial_count; }
            set { trial_count = value; }
        }

        public bool IsCalibrated
        {
            get { return is_calibrated != 0; }
            set { is_calibrated = (byte)(value ? 1 : 0); }
        }

        public HazeReferStandard HazeReferStandard
        {
            get { return (HazeReferStandard)ref_standard; }
            set { ref_standard = (byte)value; }
        }
    };

    /// <summary>
    /// 雾度测量结果
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct HazeMeasureResult
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = Constants.VALID_WAVE_NUM)]
        private short[] spectrum;   // 光谱数据*10000
        private short fill; // 用于对齐到4字节
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = Constants.HAZE_TYPE_COUNT)]
        private HazeData[] hazes;

        public Spectrum Spectrum
        {
            get
            {
                if (spectrum == null)
                {
                    return null;
                }

                double[] data = new double[Constants.VALID_WAVE_NUM];
                for (int i = 0; i < Constants.VALID_WAVE_NUM; i++)
                {
                    data[i] = spectrum[i] * 0.0001;
                }
                var ret = new Spectrum(new WavelengthRange(
                    Constants.VALID_WAVE_BEGIN, Constants.VALID_WAVE_END, Constants.VALID_WAVE_STEP), data);
                return ret;
            }
            set
            {
                if (spectrum == null)
                {
                    spectrum = new short[Constants.VALID_WAVE_NUM];
                }
                for (int i = 0; i < Constants.VALID_WAVE_NUM; i++)
                {
                    int wavelength = Constants.VALID_WAVE_BEGIN + Constants.VALID_WAVE_STEP * i;
                    spectrum[i] = (short)(value[wavelength] * 10000);
                }
            }
        }

        public HazeData[] Hazes
        {
            get => hazes;
            set
            {
                if (value.Length != Constants.HAZE_TYPE_COUNT)
                {
                    throw new ArgumentException();
                }

                hazes = value;
            }
        }
    }

    /// <summary>
    /// 雾度测量结果
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct HazeMeasureResultFor1810
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = Constants.VALID_WAVE_NUM_Max)]
        private short[] spectrum;   // 光谱数据*10000
        private short fill; // 用于对齐到4字节
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = Constants.HAZE_TYPE_COUNT)]
        private HazeData[] hazes;

        public Spectrum Spectrum
        {
            get
            {
                if (spectrum == null)
                {
                    return null;
                }

                double[] data = new double[Constants.VALID_WAVE_NUM_Max];
                for (int i = 0; i < Constants.VALID_WAVE_NUM_Max; i++)
                {
                    data[i] = spectrum[i] * 0.0001;
                }
                var ret = new Spectrum(new WavelengthRange(
                    Constants.VALID_WAVE_BEGIN_Max, Constants.VALID_WAVE_END_Max, Constants.VALID_WAVE_STEP), data);
                return ret;
            }
            set
            {
                if (spectrum == null)
                {
                    spectrum = new short[Constants.VALID_WAVE_NUM_Max];
                }
                for (int i = 0; i < Constants.VALID_WAVE_NUM_Max; i++)
                {
                    int wavelength = Constants.VALID_WAVE_BEGIN_Max + Constants.VALID_WAVE_STEP * i;
                    spectrum[i] = (short)(value[wavelength] * 10000);
                }
            }
        }

        public HazeData[] Hazes
        {
            get => hazes;
            set
            {
                if (value.Length != Constants.HAZE_TYPE_COUNT)
                {
                    throw new ArgumentException();
                }

                hazes = value;
            }
        }
    }
    /// <summary>
    /// 透过率测量结果
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct TransMeasureResult
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = Constants.VALID_WAVE_NUM)]
        private short[] spectrum;   // 光谱数据*10000
        short fill1;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = Constants.HAZE_TYPE_COUNT)]
        private short[] trans;
        short fill2;

        public Spectrum Spectrum
        {
            get
            {
                if (spectrum == null)
                {
                    return null;
                }

                var ret = new Spectrum(new WavelengthRange(
                    Constants.VALID_WAVE_BEGIN, Constants.VALID_WAVE_END, Constants.VALID_WAVE_STEP));
                for (int i = 0; i < Constants.VALID_WAVE_NUM; i++)
                {
                    int wave = Constants.VALID_WAVE_BEGIN + Constants.VALID_WAVE_STEP * i;
                    ret[wave] = spectrum[i] * 0.0001;
                }

                return ret;
            }
            set
            {
                if (spectrum == null)
                {
                    spectrum = new short[Constants.VALID_WAVE_NUM];
                }
                for (int i = 0; i < Constants.VALID_WAVE_NUM; i++)
                {
                    int wavelength = Constants.VALID_WAVE_BEGIN + Constants.VALID_WAVE_STEP * i;
                    spectrum[i] = (short)(value[wavelength] * 10000);
                }
            }
        }

        public short[] Trans
        {
            get => trans;
            set
            {
                if (value.Length != Constants.HAZE_TYPE_COUNT)
                {
                    throw new ArgumentException();
                }

                trans = value;
            }
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct RawSampleData
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = Constants.CCD_MAX_NUM)]
        public short[] main_off; // 主光谱仪灯灭采样
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = Constants.CCD_MAX_NUM)]
        public short[] ref_off;  // 参考光谱仪灯灭采样
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = Constants.CCD_MAX_NUM)]
        public short[] main_on; // 主光谱仪灯灭采样
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = Constants.CCD_MAX_NUM)]
        public short[] ref_on;  // 参考光谱仪灯灭采样
    }

}