﻿using System;
using System.Runtime.InteropServices;
using ThreeNH.ColorAlgorithm;
using ThreeNH.ColorSpecimen;
using ThreeNH.Optical;

namespace ThreeNH.SQCX.Infrastructure.Instruments.GratingSpectrometer
{
    /// <summary>
    /// 仪器中使用的枚举定义
    /// </summary>
    namespace Instrument
    {
        public enum InternalModelType
        {
            YS30XX,
            YS45XX,
        }

        /// <summary>
        /// 仪器中的标准光源代号
        /// </summary>
#if true
        internal enum StandardIlluminant : byte
        {
            D65,
            D50,
            A,
            C,
            D55,
            D75,
            F2,
            F7,
            F11,
            F1,
            F3,
            F4,
            F5,
            F6,
            F8,
            F9,
            F10,
            F12,

            #region 新增光源设置
         //   H,
            CWF,
            DLF,
            TL83,
            TL84,
            TPL5,
            U30,
            B,
            U35,
            NBF,
            ID50,
            ID65,
            LED_B1,
            LED_B2,
            LED_B3,
            LED_B4,
            LED_B5,
            LED_BH1,
            LED_RGB1,
            LED_V1,
            LED_V2,
            LED_C2,
            LED_C3,
            LED_C5,
            LED_T8G,
            U3500

            #endregion
        }
#else
        //20190805lilin change to comment,use following 界面选择的和实际的对不上
        internal enum StandardIlluminant : byte
        {
            D65,
            D50,
            A,
            C,
            D55,
            D75,
            F1,
            F2,
            F3,
            F4,
            F5,
            F6,
            F7,
            F8,
            F9,
            F10,
            F11,
            F12
        }
#endif
        internal enum StandardObserver : byte
        {
            CIE1931,
            CIE1964
        }

        internal enum MeasurementCaliber : byte
        {
            Phi8,
            Phi4,
            Phi1by3,
            Phi2,
            Phi7,
            Unknown,
        }

        internal enum SpecularComponent : byte
        {
            SCI,
            SCE,
            SCIAndSCE
        }

        internal enum SampleType : byte
        {
            Standard,
            Test,
            Target, // 存样
        }

        internal enum CalibrationType : byte
        {
            Black,
            White
        }

        internal enum ColorSpace : byte
        {
            CIE_LAB,
            CIE_LCH,
            CIE_XYZ,
            CIE_Yxy,
            CIE_LUV,
            HUNTER_LAB,
            S_RGB,
            Betaxy,
        }

        internal enum ColorDiffFormula : byte
        {
            CIE_DE,
            CIE_DE94,
            HUNTER_DE,
            CMC21, // cmc(2:1)
            CMC11, // cmc(1:1)
            CMCLC, // cmc(l:c)
            CIE_DE00, // dE2000
        }

        internal struct DateTime
        {
            public byte Day;
            public byte Month;
            public ushort Year;
            public byte Second;
            public byte Minute;
            public byte Hour;

            internal DateTime(System.DateTime time)
            {
                Year = (ushort)time.Year;
                Month = (byte)time.Month;
                Day = (byte)time.Day;
                Hour = (byte)time.Hour;
                Minute = (byte)time.Minute;
                Second = (byte)time.Second;
            }

            internal System.DateTime ToSystemDateTime()
            {
                try
                {
                    return new System.DateTime(Year, Month, Day,
                        Hour, Minute, Second);
                }
                catch (Exception)
                {
                    return System.DateTime.Now;
                }
            }
        }

        internal class SampleData
        {
            public ushort flags;
            public DateTime dateTime;

            public ushort index;

            //public char[] name = new char[9];
            public string name; // name 为 byte[9];
            public ushort standard;
            public ColorData sci;
            public ColorData sce;
            public Tolerance tolerance;

            public bool IsStandard
            {
                get { return standard == 0xFFFF; }
            }

            /// <summary>
            /// 当为交通路标样品时色阈的材料类型
            /// </summary>
            public MaterialType MaterialType
            {
                get
                {
                    return new SampleDataFlags(flags).TrafficFlag
                        ? (MaterialType)((standard >> 8) & 0xFF)
                        : MaterialType.Unknown;
                }
            }

            /// <summary>
            /// 当为交通路标样品时色阈的序号
            /// </summary>
            public int ColorGamutIndex
            {
                get { return new SampleDataFlags(flags).TrafficFlag ? (standard & 0xFF) : 0; }
            }

            public bool IsTrafficSample
            {
                get { return new SampleDataFlags(flags).TrafficFlag; }
            }
        }
    }

    public class Tolerance
    {
        public double dE { get; set; }
        public double dL_Upper { get; set; }
        public double dL_Lower { get; set; }
        public double da_Upper { get; set; }
        public double da_Lower { get; set; }
        public double db_Upper { get; set; }
        public double db_Lower { get; set; }

        public override string ToString()
        {
            return string.Format("dE: {0}, L: [{1:F3}, {2:F3}], a: [{3:F3}, {4:F3}], b: [{5:F3}, {6:F3}]",
                dE, dL_Lower, dL_Upper, da_Lower, da_Upper, db_Lower, db_Upper);
        }
    }

    public class DeviceInfo
    {
        /// <summary>
        /// 仪器型号
        /// </summary>
        public string InstrumentMode { get; set; }

        /// <summary>
        /// 光学结构
        /// </summary>
        public string OpticalStruct { get; set; }

        /// <summary>
        /// 硬件版本号
        /// </summary>
        public string HardwareVersion { get; set; }

        /// <summary>
        /// 软件版本号
        /// </summary>
        public string SoftwareVersion { get; set; }

        /// <summary>
        /// 内部白板编号
        /// </summary>
        public string WhiteBoardInnerNumber { get; set; }

        /// <summary>
        /// 外部白板编号
        /// </summary>
        public string WhiteBoardOuterNumber { get; set; }

        /// <summary>
        /// 容量
        /// </summary>
        public UInt16 Capacity { get; set; }

        /// <summary>
        /// 标样数
        /// </summary>
        public UInt16 StandardCount { get; set; }

        /// <summary>
        /// 试样数
        /// </summary>
        public UInt16 TestCount { get; set; }

        /// <summary>
        /// 黑校正标志
        /// </summary>
        public bool BlackCalibration { get; set; }

        /// <summary>
        /// 白校正标志
        /// </summary>
        public bool WhiteCalibration { get; set; }

        /// <summary>
        /// SN
        /// </summary>
        public string SN { get; set; }

        /// <summary>
        /// 定制名称
        /// </summary>
        public string ModelName { get; set; }

        /// <summary>
        /// 系统黑认容差
        /// </summary>
        public Tolerance Tolerance { get; set; }

        public override string ToString()
        {
            return string.Format("InstrumentMode: {0}\n" +
                                 "OpticalStruct: {1}\n" +
                                 "HardwareVersion: {2}\n" +
                                 "SoftwareVersion: {3}\n" +
                                 "WhiteBoardInnerNumber: {4}\n" +
                                 "WhiteBoardOuterNumber: {5}\n" +
                                 "Capacity: {6}\n" +
                                 "StandardCount: {7}\n" +
                                 "TestCount: {8}\n" +
                                 "BlackCalibration: {9}\n" +
                                 "WhiteCalibration: {10}\n" +
                                 "SN: {11}\n" +
                                 "ModelName: {12}\n" +
                                 "Tolerance: {13}\n",
                InstrumentMode,
                OpticalStruct,
                HardwareVersion,
                SoftwareVersion,
                WhiteBoardInnerNumber,
                WhiteBoardOuterNumber,
                Capacity,
                StandardCount,
                TestCount,
                BlackCalibration,
                WhiteCalibration,
                SN,
                ModelName,
                Tolerance);
        }
    }

    public class VersionInfo
    {
        /// <summary>
        /// 硬件版本号
        /// </summary>
        public string HardwareVersion { get; set; }

        /// <summary>
        /// 软件版本号
        /// </summary>
        public string SoftwareVersion { get; set; }

        /// <summary>
        /// 定制名称
        /// </summary>
        public string ModelName { get; set; }

        /// <summary>
        /// SN
        /// </summary>
        public string SN { get; set; }
    }

    public enum MeasurementCondition
    {
        M0,
        M1,
        M2,
        M3
    }

    public class MeasurementParameter
    {
        internal byte _measurementCondition;

        public StandardIlluminant PrimaryIlluminant { get; set; }
        public StandardIlluminant SecondaryIlluminant { get; set; }
        public StandardIlluminant TertiaryIlluminant { get; set; }
        public MeasurementCaliber MeasurementCaliber { get; set; }
        public SpecularComponent SpecularComponent { get; set; }
        public StandardObserver Observer { get; set; }
        public UVFlag UV
        {
            get => (UVFlag)_measurementCondition;
            set => _measurementCondition = (byte)value;
        }
        public MeasurementCondition MeasurementCondition
        {
            get => (MeasurementCondition)_measurementCondition;
            set => _measurementCondition = (byte)value;
        }

        public override string ToString()
        {
            return string.Format(
                "PrimaryIlluminant: {0}\n" +
                "SecondaryIlluminant: {1}\n" +
                "TertiaryIlluminant: {2}\n" +
                "Aperture: {3}\n" +
                "SpecularComponent: {4}\n" +
                "Observer: {5}\n" +
                "UV: {6}\n",
                PrimaryIlluminant,
                SecondaryIlluminant,
                TertiaryIlluminant,
                MeasurementCaliber,
                SpecularComponent,
                Observer,
                UV);
        }
    }

    /// <summary>
    /// 交通路标原材料
    /// </summary>
    enum MaterialType
    {
        Gbt18833DayWithoutMetal, // 道路交通反光膜白昼无金属镀膜
        Gbt18833DayWithMetal, // 道路交通反光膜白昼有金属镀膜
        Gbt18833Night, // 道路交通反光膜夜间色
        SafetyColoursNormal, // 安全色-普通材料
        SafetyColoursLuminescentMetrial, // 安全色-发光材料
        SafetyColoursRetroreflector1, // 安全色-逆反射材料-类型1
        SafetyColoursRetroreflector2, // 安全色-逆反射材料-类型2
        SafetyColoursCombinationMetrial, // 安全色-组合材料
        SafetyColoursNormalNarrow, // 安全色-普通材料（更窄范围）
        SafetyColoursRetroreflector1Narrow, // 安全色-逆反射材料-类型1（更窄范围）
        SafetyColoursRetroreflector2Narrow, // 安全色-逆反射材料-类型2（更窄范围）

        // MH5001_2013_GROUND_LIGHTING_COLORS,  // 民用航空地面灯
        // MH5001_2013_ORDINARY_COLORS,  // 标记牌、面版的普通色
        // MH5001_2013_RETRO_REFLECTING_COLORS, // 用于标记牌、面版的逆反射材料的颜色
        // MH5001_2013_TRANSILLUMINATED_SIGNS_DAY, // 透射照明的标记牌、面版的颜色
        // MH5001_2013_TRANSILLUMINATED_SIGNS_NIGHT, // 透射照明的标记牌、面版的夜间色
        Unknown = 0xFF // 未知
    }

    /// <summary>
    /// 光学结构
    /// </summary>
    enum OpticalStructure
    {
        _D_8, // d/8
        _8_D, // 8/d
        _45_0, // 45/0
    }

    public class ChromaticityOperations
    {
        public ChromaticityOperations()
        {
            Tolerance = new Tolerance();
        }

        public ColorSpace ColorSpace { get; set; }
        public ColorDiffFormula ColorDiffFormula { get; set; }
        public Tolerance Tolerance { get; set; }

    }

    public class WhiteBoardParameter
    {
        public string InnerNumber { get; set; }
        public string OuterNumber { get; set; }
        public Spectrum SCI { get; set; }
        public Spectrum SCE { get; set; }
    }

    /// <summary>
    /// 原始采样值
    /// </summary>
    public class SamplingData
    {
        /// <summary>
        /// 主光路灯亮数据
        /// </summary>
        public ushort[] MajorLightOn { get; set; } = new ushort[256];

        /// <summary>
        /// 辅光路灯亮数据
        /// </summary>
        public ushort[] MinorLightOn { get; set; } = new ushort[256];

        /// <summary>
        /// 主光路灯灭数据
        /// </summary>
        public ushort[] MajorLightOff { get; set; } = new ushort[256];

        /// <summary>
        /// 辅光路灯灭数据
        /// </summary>
        public ushort[] MinorLightOff { get; set; } = new ushort[256];
    }

    internal class SampleDataFlags
    {
        const int SpecularComponentShift = 0;
        const int UVShift = 2;
        const int MeasurementConditionShift = 2;
        const int CaliberShift = 4;
        const int DataTypeShift = 8;
        const int ToleranceShift = 10;
        private const int TrafficShift = 11; // 交通路标样品标志
        private const int OpticalStructShift = 14; // 光学结构
        private const int TargetShift = 15; // 存样标志位

        const int SpecularComponentMask = 0x0003; // SCI/SCE标志掩码
        const int UVMask = 0x000C; // UV标志掩码
        private const int MeasurementConditionMask = 0x000C; // 测量条件的掩码
        const int CaliberMask = 0x00F0; // 测量口径掩码
        const int ToleranceMask = 0x0400; // 容差标志掩码
        const int DataTypeMask = 0x0300; // 数据类型掩码
        private const int TrafficMask = 0x0800; // 交通路标样品标志掩码
        private const int OpticalStructMask = 0xC000; // 光学结构的掩码
        private const int TargetMask = 0x8000; // 存样库标志位（仅写入标样时有效）

        enum DataType
        {
            RefelectanceFromInstrument = 0, // 测量得到的反射率数据
            RefelectanceFromUser, // 用户输入的反射率
            Lab, // 用户输入的Lab数据
            XYZ // 用户输入的XYZ数据
        }

        public SampleDataFlags()
        {
            Flags = 0;
        }

        public SampleDataFlags(UInt16 flags)
        {
            Flags = flags;
        }


        int flags;

        public UInt16 Flags
        {
            get { return (UInt16)flags; }
            set { flags = value; }
        }

        /// <summary>
        /// 低八位
        /// </summary>
        public byte LowByte
        {
            get { return BitConverter.GetBytes(flags)[0]; }
        }

        /// <summary>
        /// 高八位
        /// </summary>
        public byte HighByte
        {
            get { return BitConverter.GetBytes(flags)[1]; }
        }

        public SpecularComponent SpecularComponent
        {
            get { return ((Instrument.SpecularComponent)(Flags & SpecularComponentMask)).Uniform(); }
            set
            {
                int tmp = (int)value.ToInstrumentForm();
                flags &= ~SpecularComponentMask;
                flags |= tmp;
            }
        }

        public bool HasSCI
        {
            get
            {
                return SpecularComponent == SpecularComponent.SCI ||
                       SpecularComponent == SpecularComponent.SCIAndSCE || TrafficFlag;
            }
        }

        public bool HasSCE
        {
            get
            {
                return !TrafficFlag && (SpecularComponent == SpecularComponent.SCE ||
                                        SpecularComponent == SpecularComponent.SCIAndSCE);
            }
        }

        static UVFlag ToUVFlag(int uv)
        {
            if (uv == 0)
                return UVFlag.None;
            else
                return UVFlag.On;
        }

        static int ToValue(UVFlag uv)
        {
            if (uv == UVFlag.None)
                return 0;
            else
                return 1;
        }

        public MeasurementCondition MeasurementCondition
        {
            get
            {
                int tmp = (flags & MeasurementConditionMask) >> MeasurementConditionShift;
                return (MeasurementCondition)tmp;
            }
            set
            {
                flags &= ~MeasurementConditionMask;
                flags = flags | (((int)value >> MeasurementConditionShift) & MeasurementConditionMask);
            }
        }

        public UVFlag UVFlag
        {
            get
            {
                int tmp = flags & UVMask;
                return ToUVFlag(tmp >> UVShift);
            }
            set
            {
                flags &= ~UVMask;
                flags = flags | ((ToValue(value) << UVShift) & UVMask);
            }
        }

        public bool HasTolerance
        {
            get { return (flags & ToleranceMask) != 0; }
            set
            {
                if (value)
                {
                    flags = flags | ToleranceMask;
                }
                else
                {
                    flags = flags & (~ToleranceMask);
                }
            }
        }

        DataType DataTypeFlag
        {
            get { return (DataType)((flags & DataTypeMask) >> DataTypeShift); }
            set
            {
                flags &= ~DataTypeMask;
                flags |= (int)value << DataTypeShift;
            }
        }

        public bool IsLab
        {
            get { return DataTypeFlag == DataType.Lab; }
            set { DataTypeFlag = DataType.Lab; }
        }

        public bool IsXyz
        {
            get { return DataTypeFlag == DataType.XYZ; }
            set { DataTypeFlag = DataType.XYZ; }
        }

        public bool IsReflectanceFromInstrument
        {
            get { return DataTypeFlag == DataType.RefelectanceFromInstrument; }
            set { DataTypeFlag = DataType.RefelectanceFromInstrument; }
        }

        public bool IsReflectanceFromUser
        {
            get { return DataTypeFlag == DataType.RefelectanceFromUser; }
            set { DataTypeFlag = DataType.RefelectanceFromUser; }
        }

        public bool IsReflectance
        {
            get { return IsReflectanceFromInstrument || IsReflectanceFromUser; }
        }

        public bool IsUserData
        {
            get { return DataTypeFlag != DataType.RefelectanceFromInstrument; }
        }

        public MeasurementCaliber MeasurementCaliber
        {
            get { return GetMeasurementCaliber(false); }
            set { SetMeasurementCaliber(value, false); }
        }

        /// <summary>
        /// KS/PS 系列与常规机型口径枚举编码不一致，按机型选择 Uniform / UniformForKS
        /// </summary>
        public MeasurementCaliber GetMeasurementCaliber(bool isKSSeries)
        {
            var instrumentCaliber = (Instrument.MeasurementCaliber)((flags & CaliberMask) >> CaliberShift);
            return isKSSeries ? instrumentCaliber.UniformForKS() : instrumentCaliber.Uniform();
        }

        public void SetMeasurementCaliber(MeasurementCaliber value, bool isKSSeries)
        {
            int tmp;
            try
            {
                // KS 回写用逆映射，否则 Phi11 等会落入 catch 被写成 0（仪器侧误成 Φ8）
                tmp = (int)(isKSSeries ? value.ToInstrumentFormForKS() : value.ToInstrumentForm());
            }
            catch
            {
                tmp = 0;
            }
            flags &= ~CaliberMask;
            flags |= (tmp << CaliberShift) & CaliberMask;
        }

        public bool TrafficFlag
        {
            get { return ((flags & TrafficMask) >> TrafficShift) != 0; }
            set
            {
                flags &= ~TrafficMask;
                if (value)
                {
                    flags |= 1 << TrafficShift;
                }
            }
        }

        public OpticalStructure OpticalStructure
        {
            get { return (OpticalStructure)((flags & OpticalStructMask) >> OpticalStructShift); }
            set
            {
                flags &= ~OpticalStructMask;
                flags |= (((int)value << OpticalStructShift) & OpticalStructMask);
            }
        }

        /// <summary>
        /// 存样库标志位，仅在写入标样到仪器时有效，其读取时应忽略
        /// </summary>
        public bool IsTargetColor
        {
            get { return ((flags & TargetMask) >> TargetShift) == 1; }
            set
            {
                if (value)
                {
                    flags |= (1 << TargetShift);
                }
                else
                {
                    flags &= ~TargetMask;
                }
            }
        }
    }

    public class SamplingDataResult
    {
        public byte Result { get; set; }

        public ushort Small { get; set; }

        public ushort Normal { get; set; }

        public ushort Large { get; set; }

        public ushort Huge { get; set; }
    }
}