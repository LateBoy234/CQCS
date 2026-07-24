using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ThreeNH.NRColorimeter
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
            Phi8Plus,
            Phi20,
            Phi40,
            Unknown
        }

        internal enum SpecularComponent : byte
        {
            SCI,
            SCE
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

        internal class SampleData
        {
            public DateTime Time;
            public float L, a, b, C, h;
            public Optical.StandardIlluminant Illuminant;
            public Optical.MeasurementCaliber Caliber;
            public Optical.SpecularComponent SpecularComponent;
            public ushort Index;
        }
    }

    public class DeviceInfo
    {
        public string DeviceName { get; set; }           // 设备型号
        public string DeviceNumber { get; set; }         // 设备号
        public string FirmwareVersion { get; set; }      // 固件版本
        public ushort Capacity { get; set; }                 // 内存容量
        public ushort StandardCount { get; set; }            // 已存标样数
        public ushort TestCount { get; set; }                // 已存试样数
        public bool BlackCalibration { get; set; }           // 黑板校正标志
        public bool WhiteCalibration { get; set; }           // 白板校正标志
        public string SN { get; set; }                       // SN码

        public override string ToString()
        {
            return string.Format(
                "DeviceName: {0}\n" +
                "DeviceNumber: {1}\n" +
                "FirmwareVersion: {2}\n" +
                "Capacity: {5}\n" +
                "StandardCount: {6}\n" +
                "TestCount: {7}\n" +
                "WhiteCalibration: {8}\n" +
                "BlackCalibration: {9}\n" + 
                "SN: {10}\n",
                DeviceName,
                DeviceNumber,
                FirmwareVersion,
                Capacity,
                StandardCount,
                TestCount,
                WhiteCalibration,
                BlackCalibration,
                SN);
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

    public class SensorData
    {
        // 长度为8
        public ushort[] Sensor1Bright { get; internal set; }
        // 长度为8
        public ushort[] Sensor2Bright { get; internal set; }
        // 长度为8
        public ushort[] Sensor1Dark { get; internal set; }
        // 长度度为8
        public ushort[] Sensor2Dark { get; internal set; }
    }

    //public class YRMeasureResult
    //{
    //    private ushort[] majorLightOn = new ushort[3];
    //    private ushort[] majorLightOff = new ushort[3];

    //    public DateTime Time;
    //    public float L, a, b, C, h;
    //    public Optical.StandardIlluminant Illuminant;
    //    public Optical.MeasurementCaliber Caliber;
    //    public Optical.SpecularComponent SpecularComponent;
    //    public ushort Index;
    //    public ushort StdIndex;
    //    public ushort[] MajorLightOn { get { return majorLightOn; } }
    //    public ushort[] MajorLightOff { get { return majorLightOff; } }
    //}
}
