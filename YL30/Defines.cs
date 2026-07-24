using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YL30
{
    public static class Constants
    {
        public const int VALID_WAVE_BEGIN = 400;
        public const int VALID_WAVE_END = 700;
        public const int VALID_WAVE_STEP = 10;
        public const int VALID_WAVE_NUM = ((VALID_WAVE_END - VALID_WAVE_BEGIN) / VALID_WAVE_STEP + 1);
        public const int VALID_WAVE_NUM_1NM = (VALID_WAVE_END - VALID_WAVE_BEGIN + 1);
        public const int RECORD_NAME_MAX = 9;  // 名称最大长度

        public const int CUSTOM_COMMAND_START = 0x21;   // 仪器相关命令起始编号
        public const int DEBUG_COMMAND_START = 0xA1;    // 调试命令起始编号

        public const int SPECTROGRAPH_COEFFICIENT_COUNT = 40; // 光谱仪系数个数

        public const int DEVICE_MODEL_NAME_LENGTH = 16;// 仪器型号最大长度

        public const int LOGO_WIDTH_MAX = 320; // LOGO宽
        public const int LOGO_HEIGHT_MAX = 480; // LOGO高

        public const int MAX_CCD_NUM = 40;
        public const int VALID_CCD_BEGIN = 0; // 340.38
        public const int VALID_CCD_END = 39; // 800.65,  1012-418 = 594
        public const int VALID_CCD_NUM = (VALID_CCD_END - VALID_CCD_BEGIN + 1);
        public const int BLE_NAME_LENGTH = 20;  // 蓝牙名称支持的最大长度
        public const int MinIntegrationTime = 0; //积分时间最小值
        public const int MaxIntegrationTime = 1000;//积分时间最大值
        public const int MinDAC = 0; //积分时间最小值
        public const int MaxDAC= 100;//积分时间最大值
        public const int MinBackBrightness = 0;//背景光最小值
        public const int MaxBackBrightness = 100;//背景光最大值
        public const int MinAverageMeasurementCount = 1;//背景光最小值
        public const int MaxAverageMeasurementCount = 50;//背景光最小值
        public const int MaxStringLength = 16;
        public const int MinStringLength = 1;
        public const int INSTRUMENT_INFO_REGISTER_COUNT = 26;
        public const int COMMUNICATION_INFO_REGISTER_COUNT = 3;
        public const int SENSORS_COEFFICIENTS_REGISTER_COUNT = 48;
        public const int WHITEBOARD_PARAMETERS_REGISTER_COUNT = 109;
        public const int STANDARD_COUNT = 98;
        public const int SAMPLE_COUNT = 110;
        public const int COMMON_OPERATION_SINGNAL_REGISTER_COUNT = 1;
        public const int Motor_Position_REGISTER_COUNT = 8;
        public const int CPUID_REGISTER_COUNT = 6;
        public const int SYSTEM_INFO_REGISTER_COUNT = 35;
        public const int Measure_Condition_REGISTER_COUNT = 4;
        public const int SYSTEM_TOLERANCE = 14;
        public const int DETECT_FAULT_REGISTER_COUNT = 20;
        public const int SVD_METRIX_COL_PAMARS_COUNT = 31;
        public const int MCR_SVD_MEASURE_DATA_COUNT = 16;
        public const int SPECTROGRAPH_WHITEBOARDREFLECTANCE_COUNT = 31; // 白板反射率系数个数
    }
    public static class CommandData
    { 
        public const int FactoryReset= 0;
        public const int ProductionReset = 0;
        public const int BackLigthAlwaysOpen = 0;
    }
    public enum StandardIlluminant
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
    public enum StandardObserver
    {
        [Description("2°")]
        CIE1931,
        [Description("10°")]
        CIE1964
    }
    public enum ColorSpace
    {
        CIE_LAB,
        HUNTER_LAB,
        CIE_XYZ,
        S_RGB,
        CIE_Yxy,
        CIE_Munsell,
        CIE_LCH,
        Betaxy,
        CIE_LUV,
        DIN_LAB99
    }
    public enum ColorDiffFormula
    {
        CIE_DE,
        CIE_DE94,
        CIEDE2000,//CIE_DE00
        CMC21, // cmc(2:1)
        CMC11, // cmc(1:1)
        CMC,// cmc(l:c)
        CIEDUV,
        DIN_E99,
        HUNTER_DE,
    }
    public struct DateTime
    {
        public byte Day;
        public byte Month;
        public ushort Year;
        public byte Second;
        public byte Minute;
        public byte Hour;

        public DateTime(System.DateTime time)
        {
            Year = (ushort)time.Year;
            Month = (byte)time.Month;
            Day = (byte)time.Day;
            Hour = (byte)time.Hour;
            Minute = (byte)time.Minute;
            Second = (byte)time.Second;
        }

        public System.DateTime ToSystemDateTime()
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

    public enum BaudRate
    { 
        BaudRate9600,
        BaudRate19200,
        BaudRate38400,
        BaudRate115200,
    }
    public enum CommInterfaceType
    { 
       USB,
       RS232485,
       TcpIp
    }
    public enum DACRange
    {
        Min=0,
        Max=100
    }
    public enum  MeasureSpeed
    {
        Normal,
        Accurate
    }
    public enum Calibration
    { 
       Green,
       Black,
       White
    }
    public enum MeasureType
    { 
       Standard=3,
       Sample
    }
    public enum Language
    { 
       Chinese,
       English
    }
    public enum Buzzer
    { 
       Open,
       Close
    }
    public enum MeasureControlMode
    { 
       Instrumet,
       PC,
       InStruemtPC
    }
    public enum CalibrationMode
    { 
       Auto,
       Manual
    }
    public enum ShowOKNG
    { 
       Disable,
       Enable
    }
    public enum WriteStandardStatus
    { 
       Free,
       Busy,
       Done
    }
    public enum MeasureMode
    { 
      Reflective,
      Transmission
    }
    public enum SpecularComponent
    { 
       SCI,
       SCE,
       SCIAndSCE,
    }
    public enum UVMode
    { 
       NOCutOff,
       CutOff400,
       CutOff420,
    }
    // 测量口径
    public enum Caliber 
    {
        Phi4,      // 4mm
        Phi8,      // 8mm
        Auto,
        Phi1x3,
        Phi2,        // 个数
        [Description("Φ4||Φ8")]
        Phi4_Phi8,
        [Description("Φ4||Φ8||Φ1x3")]
        Phi4_Phi8_Phi1x3,

    }

    // 透镜位置
    public enum LensPos 
    {
        Pos4=3,  // 4MM
        Pos8=7,  // 8MM
        Pos15=14, // 15mm
        Pos25=24, // 25.4mm
        Num,    // 个数
    }
    public enum DataType
    { 
      Reflectance,
      LAB,
      XYZ
    }
    public enum LED
    { 
      Open,
      Close
    }
    public enum lockStatus
    { 
        Disable,
        Enable
    }
    public enum InstrumentInternalType
    { 
      YL30,
      Other
    }
    public enum CalibrationStatus
    {
        UnFinished,
        WhiteCalibrationUnfinished,
        BlackCalibrationUnFinished,
        AllFinished
    }
    /// <summary>
    /// 新版校准状态，供SQCX用
    /// </summary>
    public enum CalibrationNewStatus
    {
        UnFinished,
        WhiteCalibrationUnfinished,
        BlackCalibrationUnFinished,
        AllFinished
    }
    public enum CaliberMeasure
    {

        [Description("黑校")]
        Black = 1,
        [Description("白校")]
        White = 2,
        [Description("标样")]
        Standard = 3,
        [Description("试样")]
        Sample = 4,
        [Description("绿校")]
        Green = 5,
        [Description("一次校准")]
        OnceCaliberation = 6,

    }
}
