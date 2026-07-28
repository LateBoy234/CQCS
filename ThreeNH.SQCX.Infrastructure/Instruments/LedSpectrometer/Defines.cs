using ThreeNH.ColorAlgorithm;
using ThreeNH.ColorimeterInterface;
using ThreeNH.Optical;

namespace ThreeNH.LedSpectrometer
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

        internal enum DataType : byte
        {
            Lab,
            XYZ,
            Reflectance
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
            public Optical.StandardObserver Observer;
            public Optical.SpecularComponent SpecularComponent;
            public ushort Index;
            //public char[] Name = new char[9];
            public string Name; // Name为byte[9]类型的
            public ushort StandardIndex;
            public Spectrum Spectrum;

            public bool IsStandard
            {
                get { return StandardIndex == 0xFFFF; }
            }
        }

        internal enum ColorSpace
        {
            CIE_LABCH = 0,
            CIE_LAB,
            CIE_XYZ,
            HUNTER_LAB,
        }

        internal enum ColorDiffFormula
        {
            DE = 0, 
            DE94, 
            HUNTER_DE,
            CMC21,
            CMC11,
            EISO, 
            JPC79,
            EBFD,
            EFMCII,
            NH_ELUV76,
            DE2000
        }
    }

    public class DeviceInfo
    {
        public string DeviceName { get; set; }           // 设备型号
        public string DeviceNumber { get; set; }         // 设备号
        public string FirmwareVersion { get; set; }      // 固件版本
        public string WhiteBoardInnerNumber { get; set; }    // 内部白板号
        public string WhiteBoardOuterNumber { get; set; }    // 外部白板号
        public ushort Capacity { get; set; }                 // 内存容量
        public ushort StandardCount { get; set; }            // 已存标样数
        public ushort TestCount { get; set; }                // 已存试样数
        public bool BlackCalibration { get; set; }           // 黑板校正标志
        public bool WhiteCalibration { get; set; }           // 白板校正标志

        public override string ToString()
        {
            return string.Format(
                "DeviceName: {0}\n" +
                "DeviceNumber: {1}\n" +
                "FirmwareVersion: {2}\n" +
                "WhiteBoardInnerNumber: {3}\n" +
                "WhiteBoardOuterNumber: {4}\n" +
                "Capacity: {5}\n" +
                "StandardCount: {6}\n" +
                "TestCount: {7}\n" +
                "WhiteCalibration: {8}\n" +
                "BlackCalibration: {9}\n",
                DeviceName,
                DeviceNumber,
                FirmwareVersion,
                WhiteBoardInnerNumber,
                WhiteBoardOuterNumber,
                Capacity,
                StandardCount,
                TestCount,
                WhiteCalibration,
                BlackCalibration);
        }
    }

    public class MeasurementParameter
    {
        public StandardIlluminant PrimaryIlluminant { get; set; }
        public StandardIlluminant SecondaryIlluminant { get; set; }
        public StandardIlluminant TertiaryIlluminant { get; set; }
        public MeasurementCaliber MeasurementCaliber { get; set; }
        public SpecularComponent SpecularComponent { get; set; }
        public StandardObserver Observer { get; set; }

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

    public class ChromaticitySetting
    {
        public ChromaticitySetting()
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
        public Spectrum Spectrum { get; set; }
    }
}
