using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThreeNH.Helper.Properties;

namespace ThreeNH.Helper.Plugin.Utils
{
    public enum CommunicationError
    {
        UnknownError,           // 未知错误
        DeviceNotFound,         // 找不到设备
        DeviceNotOpen,          // 设备未打开
        DeviceHasOpened,        // 设备已打开
        DeviceOpenFailed,       // 打开设备失败
        DeviceBusy,             // 设备忙
        DeviceDisconnected,     // 设备连接已断开
        InvalidCommand,         // 无效命令
        InvalidParameter,       // 参数错误
        ReceivingTimeout,       // 接收数据超时
        WritingTimeout,         // 发送数据超时
        NotSupported,           // 不支持的操作
        ParsedBadCommand,       // 解析到无效命令
        ParsedBadData,          // 解析到错误数据
        ParsedBadCheckSum,      // 解析到校验和错误
        ExecuteCommandFailed,   // 执行命令失败
        CalibrationExpired,     // 黑白校正过期
        CalibrationFailed,      // 校正失败
        MeasurementAbnormal,    // 测量结果异常
        AccessDenied,           // 拒绝访问
        SystemException,        // 系统异常
        InvalidOperation,       // 无效操作
    }

    public class CommunicationException : Exception
    {
        private CommunicationError _errorCode = CommunicationError.UnknownError;

        public CommunicationException()
            : base(Resources.UnknownError)
        {

        }

        public CommunicationException(string message)
            : base(message)
        {

        }

        public CommunicationException(CommunicationError error, string message = null, Exception innerException = null)
            : base(message ?? Resources.ResourceManager.GetString(error.ToString()), innerException)
        {
            ErrorCode = error;
        }


        public CommunicationError ErrorCode
        {
            get { return _errorCode; }
            private set { _errorCode = value; }
        }
    }
}
