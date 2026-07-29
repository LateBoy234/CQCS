﻿﻿namespace ThreeNH.SQCX.Infrastructure.Instruments.GratingSpectrometer
{
    enum Command : byte
    {
        SayHello = 0xA0,         // 与仪器握手，告诉仪器连接状态和测量参数
        GetDeviceInfo = 0XA1,     // 获取设备信息
        SetMeasurementParameter,  //A2  设置测量参数
        SetChromaticitySetting,   //A3  设置颜色空间、色差公式、容差
        GetChromaticitySetting,   //A4  获取颜色空间、容差设置等信息
        Calibrate,                //A5  校正
        Measure,                  //A6  测量
        UploadSpecimen,               //A7  通知准备上传数据的条数和种类
        TransportSpecimen,            //A8  通知仪器继续上传数据
        StopTransportSpecimen,        //A9  通知仪器中断数据上传
        SpecimenPacket,               //AA  仪器在上传数据时发送过来的数据包
        DeleteSpecimen,               //AB  删除数据
        SNPasswordCheck,          //AC  废弃的命令
        WriteStandard,            //AD  写入标样
        GetMeasurementParamter,	  //AE  获取测量参数
        WriteWhiteBoardData,      // AF 写白板参数
        GetDacGain,               // B0 获取增益
        SetDacGain,               // B1 设置增益
        GetDacLightRange,         // B2 获取灯的增益范围
        ReadWhiteBoardData,       // B3 获取白板参数
        SetRasterSensorCoefficients, // B4 写宾松光栅波长系数
        GetRasterSensorCoefficients,  // B5 读取宾松光栅波长系数
        SetIntegrationTime,       // B6  设置光谱仪积分时间
        GetIntegrationTime,       // B7  读取光谱仪积分时间
        SetSamplingTimes,        // B8  设置光谱仪采样次数
        GetSamplingTimes,        // B9  获取光谱仪采样次数
        SetPixelSamplingTimes, // BA  设置光谱仪象素点采样次数
        GetPixelSamplingTimes, // BB  获取光谱仪象素点采样次数
        RotateMotor,         // BC 设置SCE/SCI切换及马达转的时间
        SetDateTime,              // BD 设置仪器的日期日间
        SetVersionInfo,		      // BE 设置版本信息
        GetVersionInfo,		      // BF 获取版本信息
        TakeSample,  // C0 测量，但直接返回原始采样数据
        SetDarkness, // C1 设置暗度值
        GetDarkness, // C2 获取暗度值
        SetImagePosition, // C3 设置相机遮挡图相的位置
        GetImagePosition, // C4 获取相机遮挡图相的位置
        SetBluetoothName, // C5 修改蓝牙名称
        GetBluetoothName,   // C6 读取蓝牙名称
        SetBluetoothPairingCode, // C7 设置bluetooth配对码
        GetBluetoothPairingCode, // C8 读取配对码
        SetLanguage = 0xCB,         // CB 设置仪器显示语言
        RestoreFactorySettings,     // CC 恢复仪器出产设置
        SetMotorDuration = 0xD5,           // D5 设置马达转动持续时间
        GetMotorDuration = 0xD6,           // D6 获取马达转动持续时间
        SetStandardGloss = 0xD7,           // D7 设置光泽度标准值
        GetStandardGloss = 0xD8,            // D8 获取标准光泽度值
        SetModel = 0xDF,                    //设置型号
        GetModel = 0xE0,                    //获取型号
        SetIntegration = 0xE1,                //NR+/NH+设置积分时间
        GetIntegration = 0xE2,                 //NR+/NH+获取积分时间
        SetGain = 0xE3,                      //设置增益
        GetGain = 0xE4,                     //获取增益
        SetYSWaveLengthOffset = 0xE1,
        GetYSWaveLengthOffset = 0xE2,
        SetYSNotOffset = 0xE3,
        SamplingDataResult = 0xE5,               //获取原始数据采样结果
        AutoRegulate = 0xE6,                    //自动调节
        WriteSVD=0xE9, //SVD算法写入仪器
        ReadSVD = 0xEA, //SVD算法读取仪器
        SpecimenAutoUpload = 0xEA,                  //样品自动上传(特殊版本)
        ReadSpectrumEight = 0xEC, //SVD算法读取仪器
        ResetSVD=0xED,//恢复默认算法数据
        MeasureEx = 0xF5,                         //测量波长，并返回对应8个点波长对应的反射率
        SetWaveLengths = 0xF6,                    //设置8个点的反射率波长
        GetWaveLengths = 0xF7,                    //获取8个点的反射率波长
        DefaultWaveLengths = 0xF8,
        BluetoothExit=0xFA,//蓝牙连接主动退出
        NullCommand,                 // 无效命令 命令解析器中是根据它的值判断命令是否在有效范围
    }
}
