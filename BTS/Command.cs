namespace BTS
{
    public enum Command
    {
        InvalidCommand = 0,
        GetDeviceInfo = Constants.CUSTOM_COMMAND_START, // 获取设备信息
        GetInstrumentStatus,        // 获取仪器状态
        MeasureStandard,               // 测量标样
        MeasureTrial,          // 测量试样
        Calibrate,  // 校正
        GetWavelengthCoef,    // 获取波长系数
        SetWavelengthCoef,    // 设置波长系数
        GetWhiteBoardParameter,  // 获取白板参数
        SetWhiteBoardParameter,	// 设置校正板参数
        RtcGetDatetime,           // 读取RTC时间
        RtcSetDatetime,           // 设置RTC时间
        GetStandardCount,     // 获取标样个数
        GetTrialCount,        // 获取试样记录个数
        UploadStandards,       // 上传标样
        UploadTrials,           // 上传试样
        DownloadStandards,        // 下载标样
        Backup,                 // 备份仪器数据
        Restore,                // 恢复备份
        GenerateAdLimit,        // 生成校准的AD限制
        SaveAdLimits,           // 存储生成的AD限制值
        GetInternalModel,       // 设置型号
        SetInternalModel,       // 获取型号
        DeleteStandard,        // 删除标样
        DeleteSample,          // 删除试样
        ColoredBrick1Calib = 0x3c, //校正色砖1
        ColoredBrick1Clear = 0x3d,//清除色砖1校正
        //调试命令
        GetProductEquipInfo = Constants.DEBUG_COMMAND_START,
        SetProductEquipInfo,
        MotorRunToStart,         // 马达返回起点
        MotorRunToEnd,           // 马达返回终点
        MotorStepTest,            // 马达步长测试
        DcMotorTest,              // DC 马达测试
        LedTest,                   // LED测试
        CameraTest,                // 相机测试
        FlashTest,                 // Flash测试
        SdramTest,                 // SDRAM测试
        BuzzerTest,                // 蜂鸣器测试
        LcdTest,                   // LCD测试
        TouchScreenTest,          // 触屏测试
        AutoGetIntegralTime,     // 自动获取积分时间
        GetAdcData,				// 获取ADC采样数据
        PrinterTest,            // 测试打印机
        GetCpuId,           // 获取CPU ID
        SetEncryption,      // 设置加密码
        GetMeasurementParameter,    // 获取测量参数
        SetMeasurementParameter,    // 设置测量参数
        FaultCheck,                 // 硬件检测
        SetSpecialTransmissionCaliber = 0x3F,//设置YS6060反射特殊口径
        StepMotorCalibration = 0xB6, //步进电机校准
        FAULT_CHECK = 181,                // 错误检测
        GET_GAMMA_CORRECTION,       // 获取GAMMA校正值
        SET_GAMMA_CORRECTION,       // 设置GAMMA校正值
        SET_DEFAULT_CALIBER,        // 针对单一口径的仪器，设置使用的口径
        GET_DEFAULT_CALIBER,        // 针对单一口径的仪器，获取使用的口径
        SET_CORRECTION,             // 设置修正
        GET_CORRECTION,             // 获取修正
        SET_CORRECTION_ENABLE,      // 设置修正全局开关
        GET_CORRECTION_ENABLE,      // 获取修正全局开关
        SWITCH_OPTICAL_STRUCTURE = 200,//切换光学结构，0-45°0,1-D8
       
    }
}