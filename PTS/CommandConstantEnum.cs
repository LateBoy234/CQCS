namespace PTS
{
    public enum Command
    {
        InvalidCommand = 0,
        GetDeviceInfo = Constants.CUSTOM_COMMAND_START, // 获取设备信息
        GetInstrumentStatus,        // 获取仪器状态
        MeasureStandard,               // 测量标样
        MeasureTrial,          // 测量试样
        Calibrate,  // 校正
        GetWhiteBoardParameter,  // 获取白板参数
        SetWhiteBoardParameter,	// 设置校正板参数
        RtcGetDatetime,           // 读取RTC时间
        RtcSetDatetime,           // 设置RTC时间
        GetStandardCount,     // 获取标样个数
        GetTrialCount,        // 获取试样记录个数
        UploadStandards,       // 上传标样
        UploadTrials,           // 上传试样
        DownloadStandards,        // 下载标样
        Backup,
        Restore,                  //回复出厂设置
        GenerateAdLimit,        // 生成校准的AD限制
        SaveAdLimits,           // 存储生成的AD限制值
        GetInternalModel,       // 设置型号
        SetInternalModel,       // 获取型号
        DeleteStandard,        // 删除标样
        DeleteSample,          // 删除试样
        GetCameraImage,         // 获取相机影像
        GetSystemCfg,			// 获取系统配置
        SetSystemCfg,			// 设置系统配置

        //调试命令
        GetProductEquipInfo = Constants.DEBUG_COMMAND_START,
        SetProductEquipInfo,        //A2
        DcMotorTimeGet,             // 获取DC马达旋转时间
        DcMotorTimeSet,             // 设置DC马达旋转时间
        DcMotorReset,               // 复位DC马达
        DcMotorTest,                // DC 马达测试A6
        LedTest,                    // LED测试A7
        CameraTest,                 // 相机测试A8
        FlashTest,                  // Flash测试A9
        SdramTest,                  // SDRAM测试AA
        BuzzerTest,                 // 蜂鸣器测试AB
        LcdTest,                    // LCD测试AC
        TouchScreenTest,            // 触屏测试AD
        AutoGetIntegralTime,        // 自动获取积分时间AE
        GetAdcData,				    // 获取ADC采样数据AF
        PrinterTest,                // 测试打印机B0
        GetCpuId,                   // 获取CPU ID B1
        SetEncryption,              // 设置加密码 B2
        GetMeasurementParameter,    // 获取测量参数B3
        SetMeasurementParameter,    // 设置测量参数B4
        FaultCheck,                 // 硬件检测B5
        BleSetName,				    // 设置BLE的名称
        BleSwitch,					// 开关蓝牙
        BleSendData,                // BLE发送串行数据
        CaliberTest,                //口径测试
        GetMachineCorrectionFactors,//获取台间差修正系数
        SetMachineCorrectionFactors,//设置台间差修正系数
        WriteSVD = 0x3A,            //SVD算法写入仪器
        ReadSVD = 0x3B,             //SVD算法读取仪器
        ReadSpectrumEight = 0x3C,   //测量数据读取
        GetAdcType = 0xBC,          //获取ADC类型
        SetAdcType = 0xBD,          //设置ADC类型
        MeasureKaKd50 = 0xBE,       //测量KA_KD50系数

        GetOPTOStatu = 0xBF,       //获取光耦状态

    }
}