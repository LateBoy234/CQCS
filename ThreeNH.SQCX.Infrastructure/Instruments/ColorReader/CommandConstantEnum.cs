namespace ThreeNH.SQCX.Infrastructure.Instruments.ColorReader
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
        Restore,
        GenerateAdLimit,        // 生成校准的AD限制
        SaveAdLimits,           // 存储生成的AD限制值
        GetInternalModel,       // 获取型号
        SetInternalModel,       // 设置型号
        DeleteStandard,        // 删除标样
        DeleteSample,          // 删除试样
        GetCameraImage,         // 获取相机影像
        GetSystemCfg,			// 获取系统配置
        SetSystemCfg,			// 设置系统配置
        GetFactors = 0x40,//获取K系数
        SetFactors = 0x41,
        GetWhiteBoardFactor = 0x42,//获取白板修正系数
        SetWhiteBoardFactor = 0x43,//设置白板修正系数
        ProductBlackCalibration = 0x47,      //生产黑校
        ProductWhiteCalibration = 0x48,    //生产白校
        GetKmtcMode = 0x49,      //读取Kmtc模式，或发送SVD修正参数到仪器
        SetKmtcMode = 0x4A,       //设置Kmtc模式
        GetStandardNames = 0x4B,       //获取10个标样名称
        SetStandardNames = 0x4C,       //设置10个标样名称
        GetCIE1976Tolerance = 0x4D,       //获取CIE1976容差
        SetCIE1976Tolerance = 0x4E,       //设置CIE1976容差
        ReaSVDParameters = 0x50,          //读取SVD修正参数到上位机
        ReadPharmaceuticalConfig = 0x52,   //读取药剂配置、或读8个SVD测量数据
        WritePharmaceuticalConfig = 0x53, //写入药剂配置
        ReadPharmaceuticalInfo = 0x54,        //读取药剂信息
        WritePharmaceuticalInfo = 0x55,        //写入药剂信息

        //调试命令
        GetProductEquipInfo = Constants.DEBUG_COMMAND_START,

        SetProductEquipInfo,        //A2
        DcMotorTimeGet,       // 获取DC马达旋转时间
        DcMotorTimeSet,       // 设置DC马达旋转时间
        DcMotorReset,          // 复位DC马达
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
        BleSendData,                 // BLE发送串行数据
        EnterBoot,              // 进入Bootloader
    }
}