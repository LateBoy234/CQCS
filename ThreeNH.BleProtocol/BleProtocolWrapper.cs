using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using ThreeNH.CommunicationBase;

namespace ThreeNH.BleProtocol
{
    public static class BleProtocolWrapper
    {

        public enum ErrorCode
        {
            Success, // 操作成功
            Fail, // 执行失败
            InvalidHandle, // 无效句柄
            DeviceNotFound, // 找不到设备
            DeviceOpenFaild, // 打开设备失败
            DeviceDisconnected, // 未连接设备
            AlreadyConnected, // 设备已经打开了
            NotSupport, // 不支持的操作
            AccessDenied, // 访问被拒绝
            Unreachable, // 不可到达，可能连接已断开
            ProtocolError, // GATT协议错误
            Busy, // 设备忙
            Timeout, // 超时
            Exception,
        }

        public const string PTS_PNP_ID = "TNH PTS";

        const string BleProtocolDllName = "BtsBle.dll";

        
        static BleProtocolWrapper()
        {
            if (IsSupportBle())
            {
                //var path = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);
                //path = System.IO.Path.Combine(path, Environment.Is64BitOperatingSystem ? "x64" : "x86");
                var ptr = LoadLibrary((Environment.Is64BitOperatingSystem ? "x64" : "x86") + $"\\{BleProtocolDllName}");
                IsSupported = ptr != IntPtr.Zero;
            }
        }

        
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr LoadLibrary(string libraryPath);

        [DllImport("kernel32.dll")]
        public static extern bool FreeLibrary(IntPtr hModule);

        public static bool IsSupportBle()
        {
            var os = Environment.OSVersion;
            return os.Platform == PlatformID.Win32NT &&
                   ((os.Version.Major >= 10)  // Win 10 以上
                    || (os.Version.Major == 6 && os.Version.Minor >= 3)); // 或Windows 8.1
        }

        public static bool IsSupported { get; private set; }

        public static CommunicationException ToCommunicationException(this ErrorCode errorCode)
        {
            switch (errorCode)
            {
                case ErrorCode.Success:
                    return new CommunicationException(ErrorCode.Success.ToString());
                case ErrorCode.AccessDenied:
                    return new CommunicationException(CommunicationError.AccessDenied);
                case ErrorCode.AlreadyConnected:
                    return new CommunicationException(CommunicationError.DeviceHasOpened);
                case ErrorCode.Busy:
                    return new CommunicationException(CommunicationError.DeviceBusy);
                case ErrorCode.DeviceNotFound:
                    return new CommunicationException(CommunicationError.DeviceNotFound);
                case ErrorCode.DeviceDisconnected:
                    return new CommunicationException(CommunicationError.DeviceDisconnected);
                case ErrorCode.DeviceOpenFaild:
                    return new CommunicationException(CommunicationError.DeviceOpenFailed);
                case ErrorCode.Exception:
                    return new CommunicationException(CommunicationError.SystemException);
                case ErrorCode.Fail:
                    return new CommunicationException(CommunicationError.ExecuteCommandFailed);
                case ErrorCode.NotSupport:
                    return new CommunicationException(CommunicationError.NotSupported);
                case ErrorCode.InvalidHandle:
                    return new CommunicationException(CommunicationError.InvalidParameter);
                case ErrorCode.Timeout:
                    return new CommunicationException(CommunicationError.ReceivingTimeout);
                case ErrorCode.Unreachable:
                    return new CommunicationException(CommunicationError.WritingTimeout);
                default:
                    return new CommunicationException(CommunicationError.UnknownError);
            }
        }

        /**
        * 连接仪器
        * @Param address: 蓝牙的地址；如果为0则自动查找第一个可连接的设备
        * @Return：如果连接成功返回句柄，否则返回NULL
        */
        [DllImport(BleProtocolDllName, CharSet = CharSet.Ansi)]
        public static extern IntPtr tnhble_Connect(UInt64 address);

        /**
         *@Param handle: 已连接的设备句柄
        * 断开连接，断开连接口句柄将变为无效
        */
        [DllImport(BleProtocolDllName, CharSet = CharSet.Ansi)]
        public static extern void tnhble_Disconnect(IntPtr handle);

        /**
        * 获取连接的蓝牙设备名
        * @Param handle: 已连接的设备句柄
        * @Param buf: 存储已连接的设备名
        * @Param bufLen: buf的长度
        * @Return: 如果可以获取设备名返回真，否则返回假
        */
        [DllImport(BleProtocolDllName, CharSet = CharSet.Ansi)]
        public static extern bool tnhble_GetDeviceName(IntPtr handle, StringBuilder buf, int bufLen);

        /**
        * 获取已连接的蓝牙地址
        * @Param handle: 已连接的设备句柄
        * @Return: 如果已连接返回蓝牙地址，否则返回0
        */
        [DllImport(BleProtocolDllName, CharSet = CharSet.Ansi)]
        public static extern UInt64 tnhble_GetDeviceAddress(IntPtr handle);

        /**
        * 获取错误码。
        * @Param handle: 已连接的设备句柄
        * 在调用其他函数返回后可以调用该函数获取相应的错误码获取详细信息
        */
        [DllImport(BleProtocolDllName, CharSet = CharSet.Ansi)]
        public static extern ErrorCode tnhble_GetErrorCode(IntPtr handle);

        /**
        * 获取仪器型号
        * @Param handle: 已连接的设备句柄
        * @Param buf: 存储仪器型号的缓冲区
        * @Param bufLen: buf的长度
        * @Return: 如果成功返回真，否则返回假并设置错误码
        */
        [DllImport(BleProtocolDllName, CharSet = CharSet.Ansi)]
        public static extern bool tnhble_GetDeviceModel(IntPtr handle, StringBuilder buf, int bufLen);

        /**
        * 获取仪器序列号
        * @Param handle: 已连接的设备句柄
        * @Param buf: 存储仪器序列号的缓冲区
        * @Param bufLen: buf的长度
        * @Return: 如果成功返回真，否则返回假并设置错误码
        */
        [DllImport(BleProtocolDllName, CharSet = CharSet.Ansi)]
        public static extern bool tnhble_GetSerialNumber(IntPtr handle, StringBuilder buf, int bufLen);

        /**
        * 获取仪器硬件版本号
        * @Param handle: 已连接的设备句柄
        * @Param buf: 存储仪器版本号的缓冲区
        * @Param bufLen: buf的长度
        * @Return: 如果成功返回真，否则返回假并设置错误码
        */
        [DllImport(BleProtocolDllName, CharSet = CharSet.Ansi)]
        public static extern bool tnhble_GetHardwareRevision(IntPtr handle, StringBuilder buf, int bufLen);

        /**
        * 获取软件版本号
        * @Param handle: 已连接的设备句柄
        * @Param buf: 存储软件版本号的缓冲区
        * @Param bufLen: buf的长度
        * @Return: 如果成功返回真，否则返回假并设置错误码
        */
        [DllImport(BleProtocolDllName, CharSet = CharSet.Ansi)]
        public static extern bool tnhble_GetSoftwareRevision(IntPtr handle, StringBuilder buf, int bufLen);

        /**
        * 获取固件版本号
        * @Param handle: 已连接的设备句柄
        * @Param buf: 存储固件版本号的缓冲区
        * @Param bufLen: buf的长度
        * @Return: 如果成功返回真，否则返回假并设置错误码
        */
        [DllImport(BleProtocolDllName, CharSet = CharSet.Ansi)]
        public static extern bool tnhble_GetFirmwareRevision(IntPtr handle, StringBuilder buf, int bufLen);

        /**
        * 获取生产厂商
        * @Param handle: 已连接的设备句柄
        * @Param buf: 存储生产厂商的缓冲区
        * @Param bufLen: buf的长度
        * @Return: 如果成功返回真，否则返回假并设置错误码
        */
        [DllImport(BleProtocolDllName, CharSet = CharSet.Ansi)]
        public static extern bool tnhble_GetManufacturer(IntPtr handle, StringBuilder buf, int bufLen);

        /**
       * 获取PNP ID
       * @Param handle: 已连接的设备句柄
       * @Param buf: 存储PNP ID的缓冲区
       * @Param bufLen: buf的长度
       * @Return: 如果成功返回真，否则返回假并设置错误码
       */
        [DllImport(BleProtocolDllName, CharSet = CharSet.Ansi)]
        public static extern bool tnhble_GetPnpId(IntPtr handle, StringBuilder buf, int bufLen);

        /**
        * 检查仪器是否校正
        * @Param handle: 已连接的设备句柄
        * @Return: 如果已校正返回真，否则返回假
        */
        [DllImport(BleProtocolDllName, CharSet = CharSet.Ansi)]
        public static extern bool tnhble_IsCalibrated(IntPtr handle);

        /**
        * 校准仪器
        * @Param handle: 已连接的设备句柄
        * @Param isWhiteCalibration: false黑校，true白校
        * @Return: 校准成功返回真，失败返回假
        */
        [DllImport(BleProtocolDllName, CharSet = CharSet.Ansi)]
        public static extern bool tnhble_Calibrate(IntPtr handle, bool isWhiteCalibration);

        /**
        * 黑校
        * @Param handle: 已连接的设备句柄
        * @Return: 校准成功返回真，失败返回假
        */
        [DllImport(BleProtocolDllName, CharSet = CharSet.Ansi)]
        public static extern bool tnhble_DoBlackCalibration(IntPtr handle);

        /**
        * 白校
        * @Param handle: 已连接的设备句柄
        * @Return: 校准成功返回真，失败返回假
        */
        [DllImport(BleProtocolDllName, CharSet = CharSet.Ansi)]
        public static extern bool tnhble_DoWhiteCalibration(IntPtr handle);

        /**
        * 测量标样
        * @Param handle: 已连接的设备句柄
        * @Param buf: 存储测量结果
         * @Param size: 标样大小
        * @Return: 如果成功返回true，否则返回false
        */
        [DllImport(BleProtocolDllName, CharSet = CharSet.Ansi)]
        public static extern bool tnhble_MeasureStandard(IntPtr handle, IntPtr buf, int size);

        /**
        * 测量试样
        * @Param handle: 已连接的设备句柄
        * @Param buf: 存储测量结果
         * @Param size: 试样大小
        * @Return: 如果成功返回true，否则返回false
        */
        [DllImport(BleProtocolDllName, CharSet = CharSet.Ansi)]
        public static extern bool tnhble_MeasureSample(IntPtr handle, IntPtr buf, int size);

        /**
        * 获取标样总数
        * @Param handle: 已连接的设备句柄
        * @Return: 标样总数
        */
        [DllImport(BleProtocolDllName, CharSet = CharSet.Ansi)]
        public static extern int tnhble_GetTotalStandards(IntPtr handle);

        /**
        * 获取试样总数
        * @Param handle: 已连接的设备句柄
        * @Return: 试样总数
        */
        [DllImport(BleProtocolDllName, CharSet = CharSet.Ansi)]
        public static extern int tnhble_GetTotalSamples(IntPtr handle);

        /**
        * 获取指定标样关联的试样数
        * @Param handle: 已连接的设备句柄
        * @Param standardIndex: 要获取其试样个数的标样的序号
        * @Return: 与指定标样关联的试样数
        */
        [DllImport(BleProtocolDllName, CharSet = CharSet.Ansi)]
        public static extern int tnhble_GetSampleCount(IntPtr handle, int standardIndex);

        /**
        * 获取与指定标样ID相关联的试样的个数
        * @Param handle: 已连接的设备句柄
        * @Param standardId: 要获取其试样个数的标样的ID
        * @Return: 与指定标样ID关联的试样数
        */
        [DllImport(BleProtocolDllName, CharSet = CharSet.Ansi)]
        public static extern int tnhble_GetSampleCountById(IntPtr handle, int standardId);

        /**
        * 删除指定的标样
        * @Param handle: 已连接的设备句柄
        * @Param standardIndex: 要删除的标样的序号；如果为-1则删除所有样品
        * @Return: 成功返回真，失败返回假
        */
        [DllImport(BleProtocolDllName, CharSet = CharSet.Ansi)]
        public static extern bool tnhble_DeleteStandard(IntPtr handle, int standardIndex);

        /**
        * 删除指定ID的标样
        * @Param handle: 已连接的设备句柄
        * @Param standardId: 要删除的标样的ID；如果为-1则删除所有样品
        * @Return: 成功返回真，失败返回假
        */
        [DllImport(BleProtocolDllName, CharSet = CharSet.Ansi)]
        public static extern bool tnhble_DeleteStandardById(IntPtr handle, int standardId);

        /**
        * 删除指定的样标下的指定试样
        * @Param handle: 已连接的设备句柄
        * @Param standardIndex: 要删除的试样的序号；如果为-1删除所有试样
        * @Param sampleIndex: 要删除的试样的序号；如果为-1则删除与指定标样关联的所有试样
        * @Return: 成功返回真，失败返回假
        */
        [DllImport(BleProtocolDllName, CharSet = CharSet.Ansi)]
        public static extern bool tnhble_DeleteSample(IntPtr handle, int standardIndex, int sampleIndex);

        /**
        * 删除指定样标ID下的指定试样
        * @Param handle: 已连接的设备句柄
        * @Param standardId: 要删除的试样关联的标样的ID；如果为-1删除所有试样
        * @Param sampleIndex: 要删除的试样的序号；如果为-1则删除与指定标样关联的所有试样
        * @Return: 成功返回真，失败返回假
        */
        [DllImport(BleProtocolDllName, CharSet = CharSet.Ansi)]
        public static extern bool tnhble_DeleteSampleById(IntPtr handle, int standardId, int sampleIndex);

        /**
        * 上传指定序号的标样
        * @Param handle: 已连接的设备句柄
        * @Param standardIndex: 要上传的标样的序号
        * @Param buf: 存储上传的标样
         * @Param size: 标样大小
        * @Return: 成功返回真，失败返回假
        */
        [DllImport(BleProtocolDllName, CharSet = CharSet.Ansi)]
        public static extern bool tnhble_UploadStandard(IntPtr handle, int standardIndex, IntPtr buf, int size);

        /**
        * 上传指定ID的标样
        * @Param handle: 已连接的设备句柄
        * @Param standardID: 要上传的标样的ID
        * @Param buf: 存储上传的标样
         * @Param size: 标样大小
        * @Return: 成功返回真，失败返回假
        */
        [DllImport(BleProtocolDllName, CharSet = CharSet.Ansi)]
        public static extern bool tnhble_UploadStandardById(IntPtr handle, int standardId, IntPtr buf, int size);

        /**
        * 上传试样
        * @Param handle: 已连接的设备句柄
        * @Param standardIndex: 试样关联的标样序号
        * @Param sampleIndex: 要上传的试样的序号
        * @Param buf: 上传成功存储试样
         * @Param size: 标样大小
        * @Return: 成功返回真，失败返回假
        */
        [DllImport(BleProtocolDllName, CharSet = CharSet.Ansi)]
        public static extern bool tnhble_UploadSample(IntPtr handle, int standardIndex, int sampleIndex,
            IntPtr buf, int size);

        /**
        * 上传指定ID的标样的试样
        * @Param handle: 已连接的设备句柄
        * @Param standardId: 试样关联的标样ID
        * @Param sampleIndex: 要上传的试样的序号
        * @Param buf: 上传成功存储试样
         * @Param size: 试样大小
        * @Return: 成功返回真，失败返回假
        */
        [DllImport(BleProtocolDllName, CharSet = CharSet.Ansi)]
        public static extern bool tnhble_UploadSampleById(IntPtr handle, int standardId, int sampleIndex,
            IntPtr buf, int size);

        /**
        * 输入标样到仪器中
        * @Param handle: 已连接的设备句柄
        * @Param standard: 要写入的标样数据（忽略不支持的Flag和标样ID）
         * @Param size: 标样大小
        * @Return: 成功返回真，失败返回假
        */
        [DllImport(BleProtocolDllName, CharSet = CharSet.Ansi)]
        public static extern bool tnhble_InputStandard(IntPtr handle, IntPtr standard, int size);
    }
}