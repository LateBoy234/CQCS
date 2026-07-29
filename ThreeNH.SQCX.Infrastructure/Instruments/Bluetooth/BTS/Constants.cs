﻿﻿namespace ThreeNH.SQCX.Infrastructure.Instruments.BTS
{
    public static class Constants
    {
        public const int VALID_WAVE_BEGIN = 360;
        public const int VALID_WAVE_END = 780;
        public const int VALID_WAVE_STEP = 10;
        public const int VALID_WAVE_NUM = ((VALID_WAVE_END - VALID_WAVE_BEGIN) / VALID_WAVE_STEP + 1);
        public const int VALID_WAVE_NUM_1NM = (VALID_WAVE_END - VALID_WAVE_BEGIN + 1);
        public const int RECORD_NAME_MAX = 16;  // 名称最大长度

        public const int CUSTOM_COMMAND_START = 0x21;   // 仪器相关命令起始编号
        public const int DEBUG_COMMAND_START = 0xA1;    // 调试命令起始编号
    }

    // 测量口径
    public enum Caliber
    {
        Phi4,      // 4mm
        Phi8,      // 8mm
        Phi15,     // 15mm
        Phi25,     // 25.4mm
        Num,        // 个数
    }

    /// <summary>
    /// 微孔口径
    /// </summary>
    public enum MicroCaliber
    {
        Phi_0_3, // 0.3MM
        Phi_0_5, // 0.5mm
        Phi_1,   // 1.0mm
        Num,
    }

    // 透镜位置
    public enum LensPos
    {
        Pos4,  // 4MM
        Pos8,  // 8MM
        Pos15, // 15mm
        Pos25, // 25.4mm
        Num,    // 个数
    }

    public enum MicroPore
    {
        None = 0x00,//不设置, 不支持
        No = 0x01,//非微孔,普通的
        Yes = 0x02,//微孔的
    }

    // 透镜切换模式
    public enum LensMode
    {
        Auto,       // 自动模式
        Manual      // 手动模式
    }

    // 测光方式
    public enum ScMode
    {
        SCI,        // 包含径向反射
        SCE,        // 排除径向反射
        SCIAndSCE, // 
        NUM,    // 含光方式个数
    }

    // LED序号
    public enum LedIndex
    {
        Green,  // 绿指示灯
        Red,    // 红指示灯
        LowWave,    // 蓝灯（360~390）
        HighWave,   // 红光（700~780）
        MainWaveB,   // 主灯2（410~700）
        U400,           // 蓝灯400nm
        MainWave,  // 主灯
        Count,
    }

    // UV
    public enum UvMode
    {
        NoCut = 0,  // 无截止
        Uv400,      // 400截止
        Uv420,      // 420截止
        Uv460,      // 460截止
    }

    enum Illuminant
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

    enum Observer
    {
        Degree2,       // 2度观察者角
        Degree10,		// 10度观察者角
    }

    // 颜色空间
    enum ColorSpace
    {
        CIELAB,        // CIE LAB
        CIEXYZ,        // CIE XYZ
        CIEYxy,        // CIE Yxy
        CIELCH,        // CIE LCh
        CIELUV,        // CIE Luv
        HUNTERLAB,     // Hunter LAB
        SRGB,          // sRGB
        MUNSELL,       // Munsell
        BETA_XY,       // βxy
        DIN_LAB99,     // DIN Lab99
    }

    // 色差公式
    enum ColorDeltaFormula
    {
        CIEDE,      // ΔE*ab
        CIEDE94,    // ΔE*94
        CIEDE2000,  // ΔE*2000 ΔE*00
        DECMC11,    // ΔE*cmc(1:1)
        DECMC21,    // ΔE*cmc(2:1)
        DECMC,      // ΔE*cmc(l:c)
        DIN_E99,    // DIN E99
        DEHUNTER,   // dE(Hunter)
    }

    // 颜色指标
    enum ColorIndex
    {
        INVALID,            //不选任何选项
        YELLOWNESS,         // 黄度指数
        WHITENESS,          // 白度指数
        STRENGTH,           // 力份
        METAMERISM_INDIEX,  // 同色异谱指数
        OPACITY,            // 遮盖度
        HUE_CLASSIFY_555,   //555色调分类
        STAINING_FASTNESS,  // 沾色牢度    
        GARDER_INDEX,       //透射Garder指数
        COLOR_FASTNESS,     // 变色牢度
        PT_CO_INDEX,        //pt co指数
        HAZE,               // 零度指数
        GLOSS_8,            //8 度光泽度
    }


    ///////////////////////////////////////////////////////////////////////////////
    public enum FaultMode
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

    public enum OpticalStructure : ushort
    {
        Optical_45_0,
        Optical_D8
    }
}