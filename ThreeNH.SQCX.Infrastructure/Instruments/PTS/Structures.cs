﻿﻿﻿using System;
using System.Runtime.InteropServices;
using ThreeNH.ColorAlgorithm;
using ThreeNH.ColorimeterInterface;
using ThreeNH.Optical;

namespace ThreeNH.SQCX.Infrastructure.Instruments.PTS
{

    // 产品参数字段
    public enum ProductInfoField : byte
    {
        INTERNAL_MODEL,				// 内部型号
        DEFAULT_LANGUAGE,				// 默认语言
        DEVICE_MODEL_NAME,             // 仪器内部型号名
        SOFTWARE_VERSION,              // 软件版本
        HARDWARE_VERSION,              // 硬件版本
        SN,
        BLACK_PLATE,
        WHITE_PLATE,
        SPECTROGRAPH,
        UPDATE,
        REFLECT_MODIFY, // 反射率修正系数，其他各种修正参数在这里增加  
        CAMERA_CENTER,  // 相机中心点
        MOTOR_TIME, // 马达时间
        THRESHOLD_8MM, // PHI8白板限制
        THRESHOLD_4MM, // PHI4白板限制
        CUSTOMIZE_CALIBER,  // 定制口径
        SENSOR_TYPE,        // 感应器极性设置
        IS_DEMONSTRATOR,     // 是否是演示机型
        M_KA_KD50,          // kakd50系数
        //-------
        ALL = 0xFF,	// 获取整个结构体
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
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = Constants.SPECTROGRAPH_COEFFICIENT_COUNT)]
        public float[] Coefficients;

        /// <summary>
        /// 偏移1
        /// </summary>
        public float Offset1;

        /// <summary>
        /// 偏移2
        /// </summary>
        public float Offset2;

        /// <summary>
        /// 保留位
        /// </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public float[] Reserved;

        public void Init()
        {
            Coefficients = new float[40];
        }
    }

    /// <summary>
    /// 光谱仪参数针对YS30系列
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 4)]
    public struct SpectrographParameterV1
    {
        /// <summary>
        /// 编号
        /// </summary>
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)]
        public string SN;

        /// <summary>
        /// C12666MA传感器信息
        /// </summary>
        public C12666MA C12666MASensorInfo;

        /// <summary>
        /// 偏移
        /// </summary>
        public float Offset;

        /// <summary>
        /// 保留位
        /// </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public float[] Reserved;

        public void Init()
        {
            C12666MASensorInfo = new C12666MA();
        }
    }

    /// <summary>
    ///  C12666MA传感器参数
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 4)]
    public struct C12666MA
    {
        /// <summary>
        /// 主传感器编号
        /// </summary>
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)]
        public string MainSN;

        /// <summary>
        /// 主传感器系数
        /// </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = Constants.C12666MA_COEFFICIENT_COUNT)]
        public float[] MainCcdWave;

        /// <summary>
        /// 辅传感器编号
        /// </summary>
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)]
        public string RefSN;

        /// <summary>
        /// 辅传感器系数
        /// </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = Constants.C12666MA_COEFFICIENT_COUNT)]
        public float[] RefCcdWave;

        /// <summary>
        /// 保留位
        /// </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 60)]
        public char[] Reserved;

        /// <summary>
        /// 积分时间
        /// </summary>
        public float IntergralTime;
    }


    /// <summary>
    /// 白板参数
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 4)]
    public struct CalibrationBoard
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)]
        private string _sn;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)]
        private string _innerNumber;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = Constants.VALID_WAVE_NUM)]
        private float[] _sci;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = Constants.VALID_WAVE_NUM)]
        private float[] _sce;

        public string Sn
        {
            get
            {
                return _sn;
            }

            set
            {
                if (value?.Length >= 16)
                {
                    throw new ArgumentException("Length of value must be less than 16");
                }
                _sn = value ?? string.Empty;
            }
        }

        public string InnerNumber
        {
            get => _innerNumber;
            set
            {
                if (value?.Length >= 16)
                {
                    throw new ArgumentException("Length of value must be less than 16");
                }

                _innerNumber = value ?? String.Empty;
            }
        }

        public float[] Sci
        {
            get
            {
                return _sci ?? (_sci = new float[Constants.VALID_WAVE_NUM]);
            }

            set
            {
                if (value?.Length != Constants.VALID_WAVE_NUM)
                {
                    throw new ArgumentException(string.Format("Length of value must be is {0}", Constants.VALID_WAVE_NUM));
                }
                _sci = value;
            }
        }

        public float[] Sce
        {
            get
            {
                return _sce ?? (_sce = new float[Constants.VALID_WAVE_NUM]);
            }

            set
            {
                if (value?.Length != Constants.VALID_WAVE_NUM)
                {
                    throw new ArgumentException(string.Format("Length of value must be is {0}", Constants.VALID_WAVE_NUM));
                }
                _sce = value;
            }
        }
    }

    /// <summary>
    /// 白板参数
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 4)]
    public struct WhiteCalibrationBoard
    {


        [MarshalAs(UnmanagedType.ByValArray, SizeConst = Constants.VALID_WAVE_NUM)]
        private float[] _sci;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = Constants.VALID_WAVE_NUM)]
        private float[] _sce;


        public float[] Sci
        {
            get
            {
                return _sci ?? (_sci = new float[Constants.VALID_WAVE_NUM]);
            }

            set
            {
                if (value?.Length != Constants.VALID_WAVE_NUM)
                {
                    throw new ArgumentException(string.Format("Length of value must be is {0}", Constants.VALID_WAVE_NUM));
                }
                _sci = value;
            }
        }

        public float[] Sce
        {
            get
            {
                return _sce ?? (_sce = new float[Constants.VALID_WAVE_NUM]);
            }

            set
            {
                if (value?.Length != Constants.VALID_WAVE_NUM)
                {
                    throw new ArgumentException(string.Format("Length of value must be is {0}", Constants.VALID_WAVE_NUM));
                }
                _sce = value;
            }
        }
    }
    // 产品参数- 版本0
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 4)]
    public struct ProductInfoV0
    {
        public UInt32 flags; // 保留

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)]
        private string device_model_name;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)]
        private string sn;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)]
        private string software_version;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)]
        private string hardward_version;
        private SpectrographParameter spectrograph;
        private CalibrationBoard white_plate;
        private CalibrationBoard black_plate;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = Constants.VALID_WAVE_NUM)]
        private float[] reflect_modify; // 反射率修正系数
        UInt16 motor_time;
        UInt16 camera_x;
        UInt16 camera_y;
        UInt16 encrypt;
        byte device_type;
        byte default_language; 	// 默认语言，恢复出厂设置时要恢到该语言

        float threshold_8mm; // 8mm口径白板限制
        float threshold_4mm; // 4mm口径白板限制

        private void checkSpectralData(float[] spectralData)
        {
            if (spectralData == null || spectralData.Length != Constants.VALID_WAVE_NUM)
            {
                throw new ArgumentException();
            }
        }

        public string DeviceModelName
        {
            get { return device_model_name; }
            set { device_model_name = value; }
        }

        public string SoftwareVersion
        {
            get { return software_version; }
            set { software_version = value; }
        }

        public string HardwardVersion
        {
            get { return hardward_version; }
            set { hardward_version = value; }
        }

        public string SN
        {
            get { return sn; }
            set { sn = value; }
        }

        public CalibrationBoard BlackPlate
        {
            get { return black_plate; }
            set { black_plate = value; }
        }

        public CalibrationBoard WhitePlate
        {
            get { return white_plate; }
            set { white_plate = value; }
        }


        public SpectrographParameter Spectrograph
        {
            get { return spectrograph; }
            set { spectrograph = value; }
        }

        public float[] ReflectModify
        {
            get { return reflect_modify; }
            set { reflect_modify = value; }
        }

        public ushort MotorTime
        {
            get => motor_time;
            set => motor_time = value;
        }

        public ushort CameraX
        {
            get => camera_x;
            set => camera_x = value;
        }

        public ushort CameraY
        {
            get => camera_y;
            set => camera_y = value;
        }

        public ushort Encrypt
        {
            get => encrypt;
            set => encrypt = value;
        }

        public ProductType InternalModel => (ProductType)(flags >> 9);

        public byte DeviceType
        {
            get => device_type;
            set => device_type = (byte)value;
        }



        public ThreeNH.SQCX.Infrastructure.Instruments.PTS.Language DefaultLanguage
        {
            get => (ThreeNH.SQCX.Infrastructure.Instruments.PTS.Language)default_language;
            set => default_language = (byte)value;
        }

        public float Threshold8Mm
        {
            get => threshold_8mm;
            set => threshold_8mm = value;
        }

        public float Threshold4Mm
        {
            get => threshold_4mm;
            set => threshold_4mm = value;
        }

        public void Init()
        {
            spectrograph.Init();
            ReflectModify = new float[Constants.VALID_WAVE_NUM];
        }
    }

    // 产品参数 - 版本1
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 4)]
    public struct ProductInfoV1
    {
        public UInt32 flags; // 为1

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)]
        private string device_model_name;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)]
        private string sn;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)]
        private string software_version;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)]
        private string hardward_version;
        private SpectrographParameter spectrograph;
        private CalibrationBoard white_plate;
        private CalibrationBoard black_plate;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = Constants.VALID_WAVE_NUM)]
        private float[] reflect_modify; // 反射率修正系数
        UInt16 motor_time;
        UInt16 camera_x;
        UInt16 camera_y;
        UInt16 encrypt;
        byte device_type;
        byte default_language; 	// 默认语言，恢复出厂设置时要恢到该语言

        float threshold_8mm; // 8mm口径白板限制
        float threshold_4mm; // 4mm口径白板限制

        Caliber customize_caliber;  //定制口径大小 0为4mm 1为8mm
        SensorType sensor_type;		  // 极性设置，有效值在_SENSOR_TYPE中定义

        public ProductInfoV1(ProductInfoV0 v0)
        {
            flags = v0.flags;
            device_model_name = v0.DeviceModelName;
            sn = v0.SN;
            software_version = v0.SoftwareVersion;
            hardward_version = v0.HardwardVersion;
            spectrograph = v0.Spectrograph;
            white_plate = v0.WhitePlate;
            black_plate = v0.BlackPlate;
            reflect_modify = v0.ReflectModify;
            motor_time = v0.MotorTime;
            camera_x = v0.CameraX;
            camera_y = v0.CameraY;
            encrypt = v0.Encrypt;
            device_type = (byte)v0.DeviceType;
            default_language = (byte)v0.DefaultLanguage;
            threshold_8mm = v0.Threshold8Mm;
            threshold_4mm = v0.Threshold4Mm;
            customize_caliber = Caliber.Phi8;
            sensor_type = SensorType.CommonCathode;
        }

        public ProductInfoV0 ToProductInfoV0()
        {
            var v0 = new ProductInfoV0();
            v0.flags = 0;
            v0.DeviceModelName = DeviceModelName;
            v0.SN = SN;
            v0.SoftwareVersion = SoftwareVersion;
            v0.HardwardVersion = HardwardVersion;
            v0.Spectrograph = Spectrograph;
            v0.WhitePlate = WhitePlate;
            v0.BlackPlate = BlackPlate;
            v0.ReflectModify = ReflectModify;
            v0.MotorTime = MotorTime;
            v0.CameraX = CameraX;
            v0.CameraY = CameraY;
            v0.Encrypt = Encrypt;
            v0.DeviceType = (byte)DeviceType;
            v0.Encrypt = Encrypt;
            v0.DefaultLanguage = DefaultLanguage;
            v0.Threshold8Mm = Threshold8Mm;
            return v0;
        }

        private void checkSpectralData(float[] spectralData)
        {
            if (spectralData == null || spectralData.Length != Constants.VALID_WAVE_NUM)
            {
                throw new ArgumentException();
            }
        }

        public string DeviceModelName
        {
            get { return device_model_name; }
            set { device_model_name = value; }
        }

        public string SoftwareVersion
        {
            get { return software_version; }
            set { software_version = value; }
        }

        public string HardwardVersion
        {
            get { return hardward_version; }
            set { hardward_version = value; }
        }

        public string SN
        {
            get { return sn; }
            set { sn = value; }
        }

        public CalibrationBoard BlackPlate
        {
            get { return black_plate; }
            set { black_plate = value; }
        }

        public CalibrationBoard WhitePlate
        {
            get { return white_plate; }
            set { white_plate = value; }
        }


        public SpectrographParameter Spectrograph
        {
            get { return spectrograph; }
            set { spectrograph = value; }
        }

        public float[] ReflectModify
        {
            get { return reflect_modify; }
            set { reflect_modify = value; }
        }


        public ushort MotorTime
        {
            get => motor_time;
            set => motor_time = value;
        }

        public ushort CameraX
        {
            get => camera_x;
            set => camera_x = value;
        }

        public ushort CameraY
        {
            get => camera_y;
            set => camera_y = value;
        }

        public ushort Encrypt
        {
            get => encrypt;
            set => encrypt = value;
        }

        //public ProductType InternalModel => (ProductType)(Flags >> 9);
        public ProductType InternalModel => (Flags >> 9) > 0xFFFF ? (ProductType)6 : (ProductType)(Flags >> 9);

        public byte DeviceType
        {
            get => device_type;
            set => device_type = (byte)value;
        }

        public ThreeNH.SQCX.Infrastructure.Instruments.PTS.Language DefaultLanguage
        {
            get => (ThreeNH.SQCX.Infrastructure.Instruments.PTS.Language)default_language;
            set => default_language = (byte)value;
        }

        public float Threshold8Mm
        {
            get => threshold_8mm;
            set => threshold_8mm = value;
        }

        public float Threshold4Mm
        {
            get => threshold_4mm;
            set => threshold_4mm = value;
        }

        public Caliber CustomizeCaliber
        {
            get => customize_caliber;
            set => customize_caliber = value;
        }

        public SensorType SensorType1
        {
            get => sensor_type;
            set => sensor_type = value;
        }

        public uint Flags => flags;

        public byte Version => (byte)(flags & 0xFF);

        public bool IsDemonstrator
        {
            get => (flags & 0x00000100) != 0;
            set
            {
                if (value)
                {
                    flags |= 0x00000100U;
                }
                else
                {
                    flags &= ~0x00000100U;
                }
            }
        }

        /// <summary>
        /// 特殊机型判定与蓝牙侧共用，新增机型只改 <see cref="SpecialInstrumentType"/>。
        /// </summary>
        public bool IsSpecialType
            => SpecialInstrumentType.IsSpecialType(InternalModel, DeviceType);

        //=> InternalModel == ProductType.TS7700
        //    && ((InternalModelTypeOf7700)DeviceType) == InternalModelTypeOf7700.ST50
        //    || ((InternalModelTypeOf7700)DeviceType) == InternalModelTypeOf7700.ST60
        //    || ((InternalModelTypeOf7700)DeviceType) == InternalModelTypeOf7700.ST70
        //    || ((InternalModelTypeOfPTS20)DeviceType) == InternalModelTypeOfPTS20.ST700DPlus
        //    || ((InternalModelTypeOfPTS20)DeviceType) == InternalModelTypeOfPTS20.CM700Plus;

        public void Init()
        {
            spectrograph.Init();
            ReflectModify = new float[Constants.VALID_WAVE_NUM];
        }

        /// <summary>
        /// 单点反射率
        /// </summary>
        /// <returns></returns>
        public bool IsSinglePointSpectrum()
        {
            if (InternalModel == ProductType.TS7700) return false;
            var internalModel = (InternalModelTypeOf7010)DeviceType;
            return internalModel == InternalModelTypeOf7010.TS7010 ||
                internalModel == InternalModelTypeOf7010.TS7020 ||
                internalModel == InternalModelTypeOf7010.TS7010A ||
                internalModel == InternalModelTypeOf7010.TS10 ||
                internalModel == InternalModelTypeOf7010.TS20;
        }

    }

    // 产品参数 - 版本2
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 4)]
    public struct ProductInfo
    {
        private UInt32 flags; // 为1

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)]
        private string device_model_name;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)]
        private string sn;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)]
        private string software_version;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)]
        private string hardward_version;
        private SpectrographParameter spectrograph;
        private CalibrationBoard white_plate;
        private CalibrationBoard black_plate;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = Constants.VALID_WAVE_NUM)]
        private float[] reflect_modify; // 反射率修正系数
        UInt16 motor_time;
        UInt16 camera_x;
        UInt16 camera_y;
        UInt16 encrypt;
        byte device_type;
        byte default_language; 	// 默认语言，恢复出厂设置时要恢到该语言

        float threshold_8mm; // 8mm口径白板限制
        float threshold_4mm; // 4mm口径白板限制

        Caliber customize_caliber;  //定制口径大小 0为4mm 1为8mm
        SensorType sensor_type;		  // 极性设置，有效值在_SENSOR_TYPE中定义

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = Constants.M_KA_KD_LENGTH)]
        private float[] m_ka_kd50; 

        public ProductInfo(ProductInfoV0 v0)
        {
            flags = v0.flags;
            device_model_name = v0.DeviceModelName;
            sn = v0.SN;
            software_version = v0.SoftwareVersion;
            hardward_version = v0.HardwardVersion;
            spectrograph = v0.Spectrograph;
            white_plate = v0.WhitePlate;
            black_plate = v0.BlackPlate;
            reflect_modify = v0.ReflectModify;
            motor_time = v0.MotorTime;
            camera_x = v0.CameraX;
            camera_y = v0.CameraY;
            encrypt = v0.Encrypt;
            device_type = (byte)v0.DeviceType;
            default_language = (byte)v0.DefaultLanguage;
            threshold_8mm = v0.Threshold8Mm;
            threshold_4mm = v0.Threshold4Mm;
            customize_caliber = Caliber.Phi8;
            sensor_type = SensorType.CommonCathode;
            m_ka_kd50 = new float[Constants.M_KA_KD_LENGTH];
        }

        public ProductInfo(ProductInfoV1 v1)
        {
            flags = v1.flags;
            device_model_name = v1.DeviceModelName;
            sn = v1.SN;
            software_version = v1.SoftwareVersion;
            hardward_version = v1.HardwardVersion;
            spectrograph = v1.Spectrograph;
            white_plate = v1.WhitePlate;
            black_plate = v1.BlackPlate;
            reflect_modify = v1.ReflectModify;
            motor_time = v1.MotorTime;
            camera_x = v1.CameraX;
            camera_y = v1.CameraY;
            encrypt = v1.Encrypt;
            device_type = (byte)v1.DeviceType;
            default_language = (byte)v1.DefaultLanguage;
            threshold_8mm = v1.Threshold8Mm;
            threshold_4mm = v1.Threshold4Mm;
            customize_caliber = Caliber.Phi8;
            sensor_type = SensorType.CommonCathode;
            customize_caliber = v1.CustomizeCaliber;
            sensor_type = v1.SensorType1;
            m_ka_kd50 = new float[Constants.M_KA_KD_LENGTH];
        }

        public ProductInfoV0 ToProductInfoV0()
        {
            var v0 = new ProductInfoV0();
            v0.flags = 0;
            v0.DeviceModelName = DeviceModelName;
            v0.SN = SN;
            v0.SoftwareVersion = SoftwareVersion;
            v0.HardwardVersion = HardwardVersion;
            v0.Spectrograph = Spectrograph;
            v0.WhitePlate = WhitePlate;
            v0.BlackPlate = BlackPlate;
            v0.ReflectModify = ReflectModify;
            v0.MotorTime = MotorTime;
            v0.CameraX = CameraX;
            v0.CameraY = CameraY;
            v0.Encrypt = Encrypt;
            v0.DeviceType = (byte)DeviceType;
            v0.Encrypt = Encrypt;
            v0.DefaultLanguage = DefaultLanguage;
            v0.Threshold8Mm = Threshold8Mm;
            return v0;
        }

        public ProductInfoV1 ToProductInfoV1()
        {
            var v1 = new ProductInfoV1();
            v1.flags = 0;
            v1.DeviceModelName = DeviceModelName;
            v1.SN = SN;
            v1.SoftwareVersion = SoftwareVersion;
            v1.HardwardVersion = HardwardVersion;
            v1.Spectrograph = Spectrograph;
            v1.WhitePlate = WhitePlate;
            v1.BlackPlate = BlackPlate;
            v1.ReflectModify = ReflectModify;
            v1.MotorTime = MotorTime;
            v1.CameraX = CameraX;
            v1.CameraY = CameraY;
            v1.Encrypt = Encrypt;
            v1.DeviceType = (byte)DeviceType;
            v1.Encrypt = Encrypt;
            v1.DefaultLanguage = DefaultLanguage;
            v1.Threshold8Mm = Threshold8Mm;
            v1.CustomizeCaliber = CustomizeCaliber;
            v1.SensorType1 = SensorType1;
            
            return v1;
        }

        private void checkSpectralData(float[] spectralData)
        {
            if (spectralData == null || spectralData.Length != Constants.VALID_WAVE_NUM)
            {
                throw new ArgumentException();
            }
        }

        public string DeviceModelName
        {
            get { return device_model_name; }
            set { device_model_name = value; }
        }

        public string SoftwareVersion
        {
            get { return software_version; }
            set { software_version = value; }
        }

        public string HardwardVersion
        {
            get { return hardward_version; }
            set { hardward_version = value; }
        }

        public string SN
        {
            get { return sn; }
            set { sn = value; }
        }

        public CalibrationBoard BlackPlate
        {
            get { return black_plate; }
            set { black_plate = value; }
        }

        public CalibrationBoard WhitePlate
        {
            get { return white_plate; }
            set { white_plate = value; }
        }


        public SpectrographParameter Spectrograph
        {
            get { return spectrograph; }
            set { spectrograph = value; }
        }

        public float[] ReflectModify
        {
            get { return reflect_modify; }
            set { reflect_modify = value; }
        }


        public ushort MotorTime
        {
            get => motor_time;
            set => motor_time = value;
        }

        public ushort CameraX
        {
            get => camera_x;
            set => camera_x = value;
        }

        public ushort CameraY
        {
            get => camera_y;
            set => camera_y = value;
        }

        public ushort Encrypt
        {
            get => encrypt;
            set => encrypt = value;
        }

        //public ProductType InternalModel => (ProductType)(Flags >> 9);
        public ProductType InternalModel => (Flags >> 9) > 0xFFFF ? (ProductType)6 : (ProductType)(Flags >> 9);

        public byte DeviceType
        {
            get => device_type;
            set => device_type = (byte)value;
        }

        public ThreeNH.SQCX.Infrastructure.Instruments.PTS.Language DefaultLanguage
        {
            get => (ThreeNH.SQCX.Infrastructure.Instruments.PTS.Language)default_language;
            set => default_language = (byte)value;
        }

        public float Threshold8Mm
        {
            get => threshold_8mm;
            set => threshold_8mm = value;
        }

        public float Threshold4Mm
        {
            get => threshold_4mm;
            set => threshold_4mm = value;
        }

        public Caliber CustomizeCaliber
        {
            get => customize_caliber;
            set => customize_caliber = value;
        }

        public SensorType SensorType1
        {
            get => sensor_type;
            set => sensor_type = value;
        }

        public float[] MKaKd50
        {
            get => m_ka_kd50;
            set => m_ka_kd50 = value;
        }

        public uint Flags => flags;

        public byte Version => (byte)(flags & 0xFF);

        public bool IsDemonstrator
        {
            get => (flags & 0x00000100) != 0;
            set
            {
                if (value)
                {
                    flags |= 0x00000100U;
                }
                else
                {
                    flags &= ~0x00000100U;
                }
            }
        }

        /// <summary>
        /// 特殊机型判定与蓝牙侧共用，新增机型只改 <see cref="SpecialInstrumentType"/>。
        /// </summary>
        public bool IsSpecialType
            => SpecialInstrumentType.IsSpecialType(InternalModel, DeviceType);

        public void Init()
        {
            spectrograph.Init();
            ReflectModify = new float[Constants.VALID_WAVE_NUM];
        }

        /// <summary>
        /// 单点反射率
        /// </summary>
        /// <returns></returns>
        public bool IsSinglePointSpectrum()
        {
            if (InternalModel == ProductType.TS7700) return false;
            var internalModel = (InternalModelTypeOf7010)DeviceType;
            return internalModel == InternalModelTypeOf7010.TS7010 ||
                internalModel == InternalModelTypeOf7010.TS7020 ||
                internalModel == InternalModelTypeOf7010.TS7010A ||
                internalModel == InternalModelTypeOf7010.TS10 ||
                internalModel == InternalModelTypeOf7010.TS20;
        }

    }

    /// <summary>
    /// RTC时间
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct RtcDateTime
    {
        public ushort year;
        public byte month;
        public byte date;
        public byte hour;
        public byte minute;
        public byte second;
        public byte reserved;

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
            reserved = 0;
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
        //ushort standard_count; // 已存标样数
        //ushort trial_count; // 已存试样数
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        private byte[] reserved;

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
    };

    [StructLayout(LayoutKind.Sequential)]
    public struct InstrumentStatus
    {
        byte caliber;           // 口径
        byte lens_pos;          // 透镜位置
        byte transmission;      // 是否是透射
        byte sc_mode;           // 镜面反射光测量方式
        byte uv_mode;           // UV模式
        byte observer;          // 观察者角度
        byte light_type;        // 当前光源设置
        byte color_space;       // 颜色空间
        byte color_diff_formula; // 色差公式
        byte color_index;        // 颜色指数
        private ushort standard_count;  // 标样数
        private ushort trial_count; // 试样数
        private byte is_calibrated; // 是否已校正
        private byte lens_switch_mode;  // 透镜切换方式
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        byte[] reserved;       // 保留以

        public MeasurementCaliber Caliber
        {
            get
            {
                return ((Caliber)caliber).Uniform();
            }

            set
            {
                caliber = (byte)value.ToInstrumentForm();
            }
        }

        public LensPosition LensPos
        {
            get
            {
                return ((LensPos)lens_pos).Uniform();
            }

            set
            {
                lens_pos = (byte)value.ToInstrumentForm();
            }
        }

        public bool IsTransmission
        {
            get
            {
                return transmission != 0;
            }

            set
            {
                transmission = (byte)(value ? 1 : 0);
            }
        }

        public SpecularComponent ScMode
        {
            get
            {
                return ((ScMode)sc_mode).Uniform();
            }

            set
            {
                sc_mode = (byte)value.ToInstrumentForm();
            }
        }

        public UVFlag UvMode
        {
            get
            {
                return ((UvMode)uv_mode).Uniform();
            }

            set
            {
                uv_mode = (byte)value.ToInstrumentForm();
            }
        }

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

        public LensSwitchMode LensSwitchMode
        {
            get { return ((LensMode)lens_switch_mode).Uniform(); }
            set { lens_switch_mode = (byte)value.ToInstrumentForm(); }
        }
    };

    [StructLayout(LayoutKind.Sequential, Pack = 4, CharSet = CharSet.Ansi)]
    public struct SpectraAdcSampleData
    {
        /*增加校正时间的存放*/
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = Constants.VALID_CCD_NUM)]
        float[] adc_valid;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = Constants.VALID_CCD_NUM)]
        float[] ccd_wave_length;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = Constants.VALID_CCD_NUM)]
        int[] adc_light_on_sum_minor; // 辅灯亮
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = Constants.VALID_CCD_NUM)]
        int[] adc_light_on_sum_major; // 主灯亮
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = Constants.VALID_CCD_NUM)]
        int[] adc_light_off_sum_minor;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = Constants.VALID_CCD_NUM)]
        int[] adc_light_off_sum_major;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = Constants.VALID_CCD_NUM + 1)]
        float[] adc_light_avg_minor;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = Constants.VALID_CCD_NUM + 1)]
        float[] adc_light_avg_major;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = Constants.VALID_CCD_NUM + 1)]
        float[] adc_light_dif;
        int adc_on_max_minor;
        int adc_on_max_major;
        int adc_on_min_minor;
        int adc_on_min_major;
        int adc_on_avg_minor;
        int adc_on_avg_major;
        int adc_off_max_minor;
        int adc_off_max_major;
        int adc_off_min_minor;
        int adc_off_min_major;
        int adc_off_avg_minor;
        int adc_off_avg;

        public float[] AdcValid
        {
            get => adc_valid;
        }

        public float[] CcdWaveLength
        {
            get => ccd_wave_length;
        }

        public int[] AdcLightOnSumMinor
        {
            get => adc_light_on_sum_minor;
        }

        public int[] AdcLightOnSumMajor
        {
            get => adc_light_on_sum_major;
        }

        public int[] AdcLightOffSumMinor
        {
            get => adc_light_off_sum_minor;
        }

        public int[] AdcLightOffSumMajor
        {
            get => adc_light_off_sum_major;
        }

        public float[] AdcLightAvgMinor
        {
            get => adc_light_avg_minor;
        }

        public float[] AdcLightAvgMajor
        {
            get => adc_light_avg_major;
        }

        public float[] AdcLightDif
        {
            get => adc_light_dif;
        }

        public int AdcOnMaxMinor
        {
            get => adc_on_max_minor;
            set => adc_on_max_minor = value;
        }

        public int AdcOnMaxMajor
        {
            get => adc_on_max_major;
        }

        public int AdcOnMinMinor
        {
            get => adc_on_min_minor;
        }

        public int AdcOnMinMajor
        {
            get => adc_on_min_major;
        }

        public int AdcOnAvgMinor
        {
            get => adc_on_avg_minor;
        }

        public int AdcOnAvgMajor
        {
            get => adc_on_avg_major;
        }

        public int AdcOffMaxMinor
        {
            get => adc_off_max_minor;
        }

        public int AdcOffMaxMajor
        {
            get => adc_off_max_major;
        }

        public int AdcOffMinMinor
        {
            get => adc_off_min_minor;
        }

        public int AdcOffMinMajor
        {
            get => adc_off_min_major;
        }

        public int AdcOffAvgMinor
        {
            get => adc_off_avg_minor;
        }

        public int AdcOffAvg
        {
            get => adc_off_avg;
        }
    };
}