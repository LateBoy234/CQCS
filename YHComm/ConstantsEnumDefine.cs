using System.ComponentModel;

namespace ThreeNH.YHComm
{
    public static class Constants
    {
        public const int VALID_WAVE_BEGIN = 400;
        public const int VALID_WAVE_END = 700;
        public const int VALID_WAVE_BEGIN_Max = 360;
        public const int VALID_WAVE_END_Max = 780;
        public const int VALID_WAVE_STEP = 10;
        public const int VALID_WAVE_NUM = ((VALID_WAVE_END - VALID_WAVE_BEGIN) / VALID_WAVE_STEP + 1);
        public const int VALID_WAVE_NUM_Max = ((VALID_WAVE_END_Max - VALID_WAVE_BEGIN_Max) / VALID_WAVE_STEP + 1);
        public const int RECORD_NAME_MAX = 20;  // 名称最大长度

        public const int CUSTOM_COMMAND_START = 0x21;   // 仪器相关命令起始编号
        public const int DEBUG_COMMAND_START = 0xA1;    // 调试命令起始编号
        public const int SVD_ALGO_COMMAND_START = 0x17;

        public const int DEVICE_MODEL_NAME_LENGTH = 16;// 仪器型号最大长度

        public const int BLE_NAME_LENGTH = 16;  // 蓝牙名称支持的最大长度

        public const int HAZE_TYPE_COUNT = 3;   // 雾度类型个数

        public const int SPECTROMETER_COEFFICIENT_COUNT = 6; // 光谱仪系数个数

        public const int CCD_MAX_NUM = 256; // CCD采样值个数
    }

    // 测量口径
    public enum Aperture : byte
    {
        Phi4,      // 4mm
        Phi8,      // 8mm
        Phi15,     // 15mm
        Phi20,     // 20mm
        Num,        // 个数
    }

    enum Illuminant : byte
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
        F12,
    }

    enum Observer : byte
    {
        Degree2,       // 2度观察者角
        Degree10,		// 10度观察者角
    }

    // 颜色空间
    enum ColorSpace : byte
    {
        [Description("CIE Lab")]
        CIE_LAB = 0x00,    // CIE LAB
        [Description("CIE XYZ")]
        CIE_XYZ = 0x02,    // CIE XYZ
        [Description("CIE Yxy")]
        CIE_Yxy = 0x04,    // CIE Yxy
        [Description("CIE LCh")]
        CIE_LCH = 0x06,    // CIE LCH
        [Description("Hunter Lab")]
        HUNTER_LAB = 0x01, // Hunter Lab
        [Description("CIE Luv")]
        CIE_LUV = 0x08,    // CIE LUV
        [Description("sRGB")]
        sRGB = 0x03,
        [Description("Munsell")]
        MUNSELL = 0x05,    // 孟赛尔
        [Description("βxy")]
        BETA_xy = 0x07,    // βxy
        [Description("DIN99 Lab")]
        DIN99_LAB = 0x09,
    }

    // 色差公式
    enum ColorDeltaFormula : byte
    {
        CIEDE = 0x00,      // ΔE*ab
        CIEDE94 = 0x01,    // ΔE*94
        CIEDE2000 = 0x02,  // ΔE*2000 ΔE*00
        DECMC21 = 0x03,    // ΔE*cmc(2:1)
        DECMC11 = 0x04,    // ΔE*cmc(1:1)
        DECMC = 0x05,      // ΔE*cmc(l:c)
        DEuv = 0x06,       // dE*(uv)
        DIN_E99 = 0x07,    // DIN E99
        DEHUNTER = 0x08,   // dE(Hunter)
    }

    // 颜色指标
    enum ColorIndex : byte
    {
        YELLOWNESS = 0x00,         // 黄度指数
        WHITENESS = 0x01,          // 白度指数
        GLOSS_8 = 0x02,            //8 度光泽度
        STRENGTH = 0x03,           // 力份
        OPACITY = 0x04,            // 遮盖度
        COLOR_FASTNESS = 0x05,     // 变色牢度
        STAINING_FASTNESS = 0x06,  // 沾色牢度
        METAMERISM_INDIEX = 0x07,  // 同色异谱指数

        HUE_CLASSIFY_555 = 0x08,   //555色调分类

    }

    ///////////////////////////////////////////////////////////////////////////////
    // 感应器极性
    public enum SensorType : byte
    {
        Spectrometer,   // 光谱仪
        Photosensor,    // 光敏感应器
    }

    public enum Language : byte
    {
        English,
        SimplifiedChinese,
        TraditionalChinese,
    }

    public enum HazeReferStandard : byte
    {
        ASTM_D1003, // ASTM D1003
        ISO_13468_1,    // ISO 13468-1
    }

    /// <summary>
    /// 仪器内部型号
    /// </summary>
    public enum InternalModelType
    {
        YS1800,    // 分光，卧式，补偿口
        YS1600,    // 分光，立式，补偿口
        YH1210,    // 光电积分，卧式，补偿口
        YH1200,    // 光电积分，立式，补偿口
        YH1100,    // 光电积分，立式，无补偿口
    }

    public enum HazeType
    {
        ASTM_C, // ASTM C/2
        ASTM_A, // ASTM A/2
        ISO_D65,    // ISO D65/2
    }

    /// <summary>
    /// 测量类型
    /// </summary>
    public enum MeasurementType
    {
        HAZE,       // 雾度值
        TRANS,      // 透过率值
        CLARITY,    // 清晰度
        COMPENSATION,   //补偿口测量 
        TRANSMISSIONPORT,//透射口测量
    }

    /// <summary>
    /// 校正类型
    /// </summary>
    public enum CalibrationType
    {
        BLACK,      // 0% 校正
        WHITE,      // 100% 校正
        CLARITY,    // 清晰度校正
    }

}