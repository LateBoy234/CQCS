namespace ThreeNH.SQCX.Infrastructure.Instruments.YHComm
{
    public enum Command
    {
        // 无效操作码
        InvalidCommand = 0,

        // 仪器参数命令（SVD操作）
        ReadAndWriteSVDAlgoParams = Constants.SVD_ALGO_COMMAND_START, //读写SVD参数矩阵
        MeasureSVDAlgoDatas,            //采样SVD算法数据

        // 用户命令
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
        GetInternalModel,       // 设置型号
        SetInternalModel,       // 获取型号
        DeleteStandard,        // 删除标样
        DeleteSample,          // 删除试样
        GetCameraImage,         // 获取相机影像
        GetSystemCfg,			// 获取系统配置
        SetSystemCfg,			// 设置系统配置

        // 系统调试命令
        GetProductEquipInfo = Constants.DEBUG_COMMAND_START,
        SetProductEquipInfo,        //A2
        DcMotorTimeGet,       // 获取DC马达旋转时间
        DcMotorTimeSet,       // 设置DC马达旋转时间
        DcMotorReset,          // 复位DC马达
        DcMotorTest,                // DC 马达测试A6
        LedTest,                    // LED测试A7
        CameraTest,                 // 相机测试A8
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
        BleSetName,				    // 设置BLE的名称
        BleSwitch,                  // 开关蓝牙
    }
}
