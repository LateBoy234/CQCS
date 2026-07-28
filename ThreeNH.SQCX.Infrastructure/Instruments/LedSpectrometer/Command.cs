using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ThreeNH.LedSpectrometer
{
    enum Command : byte
    {
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
        WriteWhiteBoardData,      // AF 写白板reflect rate参数
        GetDacGain,               // B0 获取增益
        SetDacGain,               // B1 设置增益
        GetDacLightRange,         // B2 获取灯的增益范围
        ReadWhiteBoardData,       // B3 获取白板reflect rate参数
        SetDateTime = 0xBD,       // 设置仪器时间
        SetCorrectionAlgorithmThreshold,    // 设置颜色修正算法的阈值
        GetCorrectionAlgorithmThreshold,    // 获取颜色修正算法的阈值
        SetLanguage = 0xCB,         // CB 设置仪器显示语言
        RestoreFactorySettings,     // CC 恢复仪器出产设置
        NullCommand,           // 无效命令
        WriteSVD = 0xE9,            //SVD算法写入仪器
        ReadSVD = 0xEA,             //SVD算法读取仪器
        ReadSpectrumEight = 0xEC,   //测量数据读取
    }
}
