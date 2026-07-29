﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ThreeNH.SQCX.Infrastructure.Instruments.YRColorimeter
{
    /// <summary>
    /// 仪器中使用的枚举定义
    /// </summary>
    namespace Instrument
    {
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
            F12
        }
#else
        //20190805lilin change to comment,use following ys30仪器 光源界面选择的和实际的对不上
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
            Phi8Plus,
            Phi20,
            Unknown
        }

        internal enum SpecularComponent : byte
        {
            SCI,
            SCE,
            SCI_AND_SCE,
            OTHER,
        }

        internal enum SampleType : byte
        {
            Standard,
            Test
        }

        internal enum CalibrationType : byte
        {
            Black,
            White
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
                return new System.DateTime(Year, Month, Day,
                    Hour, Minute, Second);
            }
        }
    }

    public enum ModelCode
    {
        YR20, YR10,
    }

    public class DeviceInfo
    {
        public string Model { get; set; }           // 设备型号
        public string OpticalStruct { get; set; }   // 光学结构
        public string HardwareVersion { get; set; }         // 设备号
        public string FirmwareVersion { get; set; }      // 固件版本
        public string InternalSnOfWhiteBoard { get; set; }
        public string ExternalSnOfWhiteBoard { get; set; }
        public ushort Capacity { get; set; }                 // 内存容量
        public ushort StandardCount { get; set; }            // 已存标样数
        public ushort TestCount { get; set; }                // 已存试样数
        public bool IsWhiteCalibrated { get; set; }           // 白板校正标志
        public bool IsBlackCalibrated { get; set; }           // 黑板校正标志
        public string Sn { get; set; }  // 仪器序列号
        public string CustomModel { get; set; } // 定制型号名
        public Tolerance Tolerance { get; set; }    // 系统默认容差
        

        public override string ToString()
        {
            return string.Format(
                "Model: {0}\n" +
                "OpticalStruct: {1}\n" +
                "HardwareVersion: {2}\n" +
                "FirmwareVersion: {3}\n" +
                "InternalSnOfWhiteBoard: {4}\n" +
                "ExternalSnOfWhiteBoard: {5}\n" +
                "Capacity: {6}\n" +
                "StandardCount: {7}\n" +
                "TestCount: {8}\n" +
                "WhiteCalibration: {9}\n" +
                "BlackCalibration: {10}\n" + 
                "Tolerance: {11}\n",
                Model,
                OpticalStruct,
                HardwareVersion,
                FirmwareVersion,
                InternalSnOfWhiteBoard,
                ExternalSnOfWhiteBoard,
                Capacity,
                StandardCount,
                TestCount,
                IsWhiteCalibrated,
                IsBlackCalibrated,
                Tolerance);
        }
    }

    public class MeasurementParameter
    {
        public Optical.StandardIlluminant PrimaryIlluminant { get; set; }
        public Optical.StandardIlluminant SecondaryIlluminant { get; set; }
        public Optical.StandardIlluminant TertiaryIlluminant { get; set; }
        public Optical.MeasurementCaliber MeasurementCaliber { get; set; }
        public Optical.SpecularComponent SpecularComponent { get; set; }
        public Optical.StandardObserver Observer { get; set; }

        public override string ToString()
        {
            return string.Format(
                "PrimaryIlluminant: {0}\n" +
                "SecondaryIlluminant: {1}\n" +
                "TertiaryIlluminant: {2}\n" +
                "Aperture: {3}\n" +
                "SpecularComponent: {4}\n" +
                "Observer: {5}\n",
                PrimaryIlluminant,
                SecondaryIlluminant,
                TertiaryIlluminant,
                MeasurementCaliber,
                SpecularComponent,
                Observer);
        }
    }

    public struct SampleFlags
    {
        const int MEAS_MODE_SHIFT = 0;  // 测量方式偏移
        const int UV_SHIFT = 2;         // UV的偏移
        const int APERTURE_SHIFT = 4;
        const int DATA_TYPE_SHIFT = 8;
        const int TOLERANCE_SHIFT = 10;
        const int TRANS_SHIFT = 11;
        const int RAW_DATA_SHIFT = 12;

        const uint MEAS_MODE_MASK = 0x0003;
        const uint UV_MASK = 0x000c;
        const uint APERTURE_MASK = 0x00F0;
        const uint DATA_TYPE_MASK = 0x0300;
        const uint TOLERANCE_MASK = 0x0400;
        const uint TRANS_MASK = 0x0800;
        const uint RAW_DATA_MASK = 0x1000;

        public uint Value { get; set; }

        public Optical.SpecularComponent SpecularComponent
        {
            get
            {
                return ((Instrument.SpecularComponent)getFlag(
                    MEAS_MODE_SHIFT, MEAS_MODE_MASK)).Uniform();
            }
            set
            {
                setFlag(MEAS_MODE_SHIFT, MEAS_MODE_MASK,
                    (uint)(value.ToInstrumentForm()));
            }
        }

        public Optical.UVFlag UV
        {
            get
            {
                return (Optical.UVFlag)getFlag(UV_SHIFT, UV_MASK);
            }
            set
            {
                setFlag(UV_SHIFT, UV_MASK, (uint)value);
            }
        }

        public Optical.MeasurementCaliber Aperture
        {
            get
            {
                return ((Instrument.MeasurementCaliber)getFlag(APERTURE_SHIFT, APERTURE_MASK)).Uniform();
            }
            set
            {
                setFlag(APERTURE_SHIFT, APERTURE_MASK, (uint)value.ToInstrumentForm());
            }
        }

        public uint DataType
        {
            get
            {
                return getFlag(DATA_TYPE_SHIFT, DATA_TYPE_MASK);
            }
            set
            {
                setFlag(DATA_TYPE_SHIFT, DATA_TYPE_MASK, value);
            }
        }

        public bool HasTolerance
        {
            get
            {
                return getFlag(TOLERANCE_SHIFT, TOLERANCE_MASK) != 0;
            }
            set
            {
                setFlag(TOLERANCE_SHIFT, TOLERANCE_MASK, value ? 1u : 0);
            }
        }

        public bool IsTransmission
        {
            get
            {
                return getFlag(TRANS_SHIFT, TRANS_MASK) != 0;
            }
            set
            {
                setFlag(TRANS_SHIFT, TRANS_MASK, value ? 1u : 0);
            }
        }

        public bool HasRawData
        {
            get
            {
                return getFlag(RAW_DATA_SHIFT, RAW_DATA_MASK) != 0;
            }
            set
            {
                setFlag(RAW_DATA_SHIFT, RAW_DATA_MASK, value ? 1u : 0);
            }
        }

        private void setFlag(int shift, uint mask, uint value)
        {
            Value = (Value & ~mask) | ((value << shift) & mask);
        }

        private uint getFlag(int shift, uint mask)
        {
            return (Value & mask) >> shift;
        }

    }

    public class ColorData
    {
        private double[] _value = new double[3];
        public Optical.StandardObserver Observer { get; set; }
        public Optical.StandardIlluminant Illuminant { get; set; }
        public double[] Value { get { return _value; }}
        public ColorAlgorithm.CIEXYZ XYZ
        {
            get { return new ColorAlgorithm.CIEXYZ(Value[0], Value[1], Value[2]); }
            set
            {
                Value[0] = value.X;
                Value[1] = value.Y;
                Value[2] = value.Z;
            }
        }
        public ColorAlgorithm.CIELAB Lab
        {
            get { return new ColorAlgorithm.CIELAB(Value[0], Value[1], Value[2]); }
            set
            {
                Value[0] = value.L;
                Value[1] = value.a;
                Value[2] = value.b;
            }
        }
    }

    public class YRSamplingData
    {
        private ushort[] majorLightOn = new ushort[3];  // 主灯亮
        private ushort[] minorLightOn = new ushort[3];	// 辅灯亮
        private ushort[] majorLightOff = new ushort[3];    // 主灯灭
        private ushort[] minorLightOff = new ushort[3];	    // 辅灯灭

        public ushort[] MajorLightOn { get { return majorLightOn; } }
        public ushort[] MinorLightOn { get { return minorLightOn; } }
        public ushort[] MajorLightOff { get { return majorLightOff; } }
        public ushort[] MinorLightOff { get { return minorLightOff; } }
    }

    public class YRSampleData
    {
        public DateTime Time;
        public float L, a, b, C, h;
        public Optical.StandardIlluminant Illuminant;
        public Optical.MeasurementCaliber Caliber;
        public Optical.SpecularComponent SpecularComponent;
        public ushort Index;
        public YRSamplingData SamplingData;
    }

    public class Tolerance
    {
        private double[] _data = new double[7];

        public double[] Data { get { return _data; } }

        public override string ToString()
        {
            return $"dE: {dE:F2}, dL: {dL_Lower:F2}~{dL_Upper:F2}, da: {da_Lower:F2}~{da_Upper:F2}, db: {db_Lower:F2}~{db_Upper:F2}";
        }

        public double dE
        {
            get { return Data[0]; }
            set { Data[0] = value; }
        }
        public double dL_Lower
        {
            get { return Data[1]; }
            set { Data[1] = value; }
        }
        public double dL_Upper
        {
            get { return Data[2]; }
            set { Data[2] = value; }
        }
        public double da_Lower
        {
            get { return Data[3]; }
            set { Data[3] = value; }
        }
        public double da_Upper
        {
            get { return Data[4]; }
            set { Data[4] = value; }
        }
        public double db_Lower
        {
            get { return Data[5]; }
            set { Data[5] = value; }
        }
        public double db_Upper
        {
            get { return Data[6]; }
            set { Data[6] = value; }
        }
    }

    public class SampleData
    {
        public const int NAME_LENGTH = 9;
        private ColorData[] _color = new ColorData[2];

        public SampleFlags Flags { get; set; }
        public int Index { get; set; }
        public int StdIndex { get; set; }
        public string Name { get; set; }
        public DateTime DateTime { get; set; }
        public ColorData[] Color { get { return _color; } }
        public Tolerance Tolerance { get; set; }
        public YRSamplingData RawData { get; set; }

        public bool IsStandard
        {
            get { return (StdIndex & 0xFFFF) == 0xFFFF; }
            set { StdIndex = 0xFFFF; }
        }
    }

    public class FactoryInfo
    {
        public string HardwareVersion { get; set; }
        public string SoftwareVersion { get; set; }
        public string Sn { get; set; }
        public string CustomModel { get; set; }
    }
}
