using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThreeNH.SQCX.Infrastructure.Instruments.YL30Comm.Result
{
    public enum ResultType
    {
        OperationSuccess,//操作成功
        OperationFail,//操作失败
        DeviceNotFound,       // 找不到设备
        DeviceAlreadyOpen,    // 设备已打开
        DeviceOpenFailed,     // 打开设备失败
        Overtime,                // 超时
        DeviceBusy,          // 设备忙
        InvalidCommand,      // 无效命令
        ExecuteFailed,       // 执行命令失败
        GetResultFailed,      // 获取结果失败
        InvalidPara,         // 无效参数
        InvalidPacket,         // 无效参数
        Unknown,            // 未知错误
    }
}
