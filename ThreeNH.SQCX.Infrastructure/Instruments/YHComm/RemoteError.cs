using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ThreeNH.CommunicationBase;

namespace ThreeNH.SQCX.Infrastructure.Instruments.YHComm
{
    public enum RemoteError
    {
        NoError,            // 没有错误
        CommonError,        // 一般性错误
        DataLen,            // 数据长度不对
        InvalidParam,       // 无效参数
        InvalidCalibration, // 无效校正
        DeviceBusy,         // 设备忙，即正在测量中
        IoError,            // 读写出错
        NoMemory,           // 内存不足，无法获取足够的缓冲区
        StorageIsFull,      // 存储区已满
        InvalidOperation,   // 无效操作
    }

    public static class RemoteErrorExtens
    {
        public static CommunicationError ToCommError(this RemoteError error)
        {
            switch (error)
            {
                case RemoteError.NoError: return CommunicationError.UnknownError;
                
                case RemoteError.DataLen:
                case RemoteError.InvalidParam:
                    return CommunicationError.InvalidParameter;
                case RemoteError.InvalidCalibration:
                    return CommunicationError.CalibrationExpired;
                case RemoteError.DeviceBusy:
                    return CommunicationError.DeviceBusy;
                //case RemoteError.IoError:
                //case RemoteError.NoMemory:
                //case RemoteError.StorageIsFull:
                //case RemoteError.CommonError:
                case RemoteError.InvalidOperation:
                    return CommunicationError.InvalidOperation;
                default:
                    return CommunicationError.ExecuteCommandFailed;
            }
        }
    }
}
