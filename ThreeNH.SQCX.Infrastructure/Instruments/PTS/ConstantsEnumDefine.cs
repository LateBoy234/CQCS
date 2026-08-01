using System.ComponentModel;

namespace PTS
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
        public const int C12666MA_COEFFICIENT_COUNT = 8; // C12666MA传感器系数个数


        public const int DEVICE_MODEL_NAME_LENGTH = 16;// 仪器型号最大长度

        public const int LOGO_WIDTH_MAX = 320; // LOGO宽
        public const int LOGO_HEIGHT_MAX = 480; // LOGO高

        public const int MAX_CCD_NUM = 40;
        public const int VALID_CCD_BEGIN = 0; // 340.38
        public const int VALID_CCD_END = 39; // 800.65,  1012-418 = 594
        public const int VALID_CCD_NUM = (VALID_CCD_END - VALID_CCD_BEGIN + 1);

        public const int BLE_NAME_LENGTH = 20;  // 蓝牙名称支持的最大长度

        public const int M_KA_KD_LENGTH = 15; // 齐工拓展的仪器信息结构
    }

    // 测量口径
    public enum Caliber : byte
    {
        Phi4,      // 4mm
        Phi8,      // 8mm
        Auto,
        Phi1x3,
        Phi2,        // 个数
        Phi3,
        Phi5,
        Phi6,
        Phi10,
        Phi11,
        Phi12,
        [Description("Φ4||Φ8")]
        Phi4_Phi8,
        [Description("Φ4||Φ8||Φ1x3")]
        Phi4_Phi8_Phi1x3,
    }

    // 透镜位置
    public enum LensPos : byte
    {
        Pos4,  // 4MM
        Pos8,  // 8MM
        Pos15, // 15mm
        Pos25, // 25.4mm
        Num,    // 个数
    }

    // 透镜切换模式
    public enum LensMode : byte
    {
        Auto,       // 自动模式
        Manual      // 手动模式
    }

    // 测光方式
    public enum ScMode : byte
    {
        SCI,        // 包含径向反射
        SCE,        // 排除径向反射
        SCIAndSCE, // 
        NUM,    // 含光方式个数
    }

    // LED序号
    public enum LedIndex : byte
    {
        White400nm700nm, // 白灯+400+700
        Uv365nm,           // 356
        Lighting,        // 照明
        IndicationLight, // 指示灯
        PTD_400,          // PDT专用
        Count,
    }

    // UV
    public enum UvMode : byte
    {
        NoCut,      // 无截止
        Uv400,      // 400截止
        Uv420,      // 420截止
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
       // H,
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

    public enum CameraTestCode : byte
    {
        Close, // 
        Open,
        EnableConfig,
        DisableConfig,
        SaveConfig,
    }


    public enum SystemCfgField : byte
    {
        OPTICAL_MODE,
        TRIAL_SC_MODE,
        UV_MODE,
        CALIBER_SIZE,
        AVERAGE_COUNT,
        CONTINUE_INTERVAL,
        CONTINUE_COUNT,
        TRIAL_AVERAGE_COUNT,
        LIGHT_TYPE,
        OBSERVER_ANGLE,
        COLOR_SPACE,
        COLOR_DELTA_FORMULA,
        COLOR_INDEX,
        LIGHT_TYPE1,
        LIGHT_TYPE2,
        OBSERVER_ANGLE1,
        OBSERVER_ANGLE2,
        MAXABS_WAVELENGTH,
        AUTO_SAVE,
        BLE,
        LANGUAGE,
        BUZZER,
        PRINTER,
        MEASURE_CONTROL_MODEL,
        LCD_BACK_LIGHT,
        BACK_LIGHT_TIME,
        TOLERANCE,
        BACK_FACTOR_94,
        BACK_FACTOR_CMC,
        BACK_FACTOR_2000,
        //---------------------------------------
        ALL = 0xFF,
    }

    ///////////////////////////////////////////////////////////////////////////////
    public enum FaultMode : byte
    {
        CLOCK,				//时钟
        RTC,				//时间
        POWER,				//电源
        PHOTO,				//光敏二极管故障
        EXT_AD,				//外部AD
        LED_CAM,			//CAMERA补光灯
        LED_UV,     		//UV LED灯 (360~390nm)	
        LED_RED,		    //RED LED  (700~780nm)
        LED_WHITE,		    //WHITE LED(410~700nm)
        LED_U400,			//U400 LED (400nm)
        LED_WHITE_B,	    //WHITE B LED(420~700nm)
        SPECTRA_MAIN,		//主光谱
        SPECTRA_REF,		//参考光谱
        DC_MOTOR,			//直流电机
        STEP_MOTOR,			//步进电机	
        CAMERA,				//摄像头
        TOUCH_SCREEN,		//触摸屏
        EXT_FLASH,			//外部FLASH
        EXT_RAM,			//外部RAM
        BLE,				//蓝牙
        OPTO_START,         //起点光耦
        OPTO_END,           //终点光耦
        OPTO_APERTURE,      //孔径光耦
        XE_DISCHARGE_1,		//蓝牙
        XE_DISCHARGE_2,     //起点光耦
        XE_DISCHARGE_3,     //终点光耦
        XE_DISCHARGE_0,     //孔径光耦    
        COUNT
    };

    // 感应器极性
    public enum SensorType : byte
    {
        CommonCathode, // 共阴极
        CommonAnode,   // 共阳性
    }

    // AD转换器类型
    public enum AdcType : byte
    {
        NonDomesticAd,    // 非国产
        DomesticAd,       // 国产
    }

    public enum Language : byte
    {
        English,
        Russian,
        SimplifiedChinese,
        TraditionalChinese,
        Korea,
    }

    public enum BackupType
    {
        Resource,
        Logo
    }
}