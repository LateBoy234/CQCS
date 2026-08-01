using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Ports;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using ThreeNH.CommDevice;
using ThreeNH.CommunicationBase;
using ThreeNH.Helper;
using ThreeNH.StructHelper;

namespace ThreeNH.ColorQualityControl
{
    public enum InstrumentType
    {
        Unknown,
        NR,
        NS,
        YS,
        YR,
        BTS,
        CR400,
        PTS,
        YH,
        CR,//微型色差宝
        JSON, // 使用JSON格式通讯协议的仪器
        YL30, // YL30系列仪器
    }

    class Parser : PacketParser
    {
        protected override bool HasAck(byte command)
        {
            return true;
        }

        protected override bool IsValidCommand(byte command)
        {
            return command == 0xA1;
        }
    }


    class InstrumentTypeDetector : IDisposable
    {
        struct YS_DeviceInfo
        {
            public string InstrumentMode;
            public string OpticalStruct;
            public string HardwareVersion;
            public string SoftwareVersion;
            public string WhiteBoardInnerNumber;
            public string WhiteBoardOuterNumber;
            public ushort Capacity;
            public ushort StandardCount;
            public ushort TestCount;
            public byte BlackCalibration;
            public byte WhiteCalibration;
            public string SN;
            public string Mark;
            public float[] Tolerance;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        struct ShakeHandsAckPacket
        {
            byte header1; // 消息头
            byte header2; // 消息头
            byte pkt_type; // 命令，恒为CMD_SHAKE_HANDS
            byte session; // 会话标志
            byte endian; // 大小端类型
            ushort buf_size; // 仪器端的接收缓冲区大小，即上位机允许的每个包的最大大小
            int version; // 通信接口版本信息
            ushort crc; // CRC校验码


            public int Version
            {
                get { return version & 0xff; }

                set { version = (version & ~0xff) | (0xff & value); }
            }

            public int ProductType
            {
                get { return (version >> 8) & 0xFFFFFF; }
                set { version = (version & 0xFF) | (value << 8); }
            }
        }


        /// <summary>
        /// 检测仪器类型
        /// </summary>
        /// <param name="device">设备通信接口</param>
        /// <returns>返回仪器类型，如果侦测出错返回InstrumentType.Unknown</returns>
        public static  InstrumentType Detect(ICommDevice port)
        {
            using (var detector = new InstrumentTypeDetector(port))
            {
                try
                {
#if true
                    //改成发送多次命令(防止某些仪器初始化发送命令不回复的情况,比如YS30系列蓝牙转串口设备)
                    for (int i = 0; i < 10; i++)
                    {
                        var type = detector.DetectInstrumentType();
                        if (type != InstrumentType.Unknown)
                            return type;
                    }
                    return InstrumentType.Unknown;
#else
                    return detector.DetectInstrumentType();
#endif
                }
                catch (CommunicationException)
                {
                    return InstrumentType.Unknown;
                }
            }
        }

        private List<byte> _recvList = new List<byte>();
        private ManualResetEvent _recvEvent = new ManualResetEvent(false);
        private Exception _ex = null;
        private Parser _parser = null;
        private ShakeHandsAckPacket _ackPacket;

        private ICommDevice CommDevice { get; set; }

        private InstrumentTypeDetector(ICommDevice port)
        {
            CommDevice = port;
            CommDevice.BeginReceiveAsync(ReceiveAsyncCallback);
        }

        private void ReceiveAsyncCallback(object sender, byte[] bytesreceived, Exception ex)
        {
            if (ex != null)
            {
                _ex = ex;
                _recvEvent.Set();
                return;
            }

            lock (_recvList)
            {
                _recvList.AddRange(bytesreceived);
                if (_recvList.IndexOf(new byte[] { 0x55, 0xaa }) >= 0 && _parser == null)
                {
                    _parser = new Parser();
                }

                if (_parser != null)
                {
                    int n = _parser.Parse(_recvList.ToArray(), 0, _recvList.Count);
                    _recvList.RemoveRange(0, n);
                    if (_parser.IsCompleted)
                    {
                        _recvList.Clear();
                        _recvList.AddRange(_parser.Result.PacketContent.Bytes);
                        _recvEvent.Set();
                    }
                    return;
                }

                int index;
                if ((index = _recvList.IndexOf(new byte[] { 0xaa, 0x55 })) >= 0)
                {
                    if (index != 0)
                    {
                        _recvList.RemoveRange(0, index);
                    }

                    if (_recvList.Count >= Marshal.SizeOf(typeof(ShakeHandsAckPacket)))
                    {
                        _ackPacket = StructConverter.BytesToStruct<ShakeHandsAckPacket>(_recvList);
                        _recvEvent.Set();
                    }
                }
            }
        }

        private InstrumentType DetectByParser()
        {

            PacketContent pktContent = new PacketContent(_recvList);
            var deviceInfo = new YS_DeviceInfo { Tolerance = new float[7] };
            //获取data数据
            Trace.TraceInformation("开始获取data数据");
            Trace.TraceInformation(string.Join(" ", pktContent.Bytes.Select(b => b.ToString("X2"))));
            if (pktContent.Dequeue(out deviceInfo.InstrumentMode) &&
                pktContent.Dequeue(out deviceInfo.OpticalStruct) &&
                pktContent.Dequeue(out deviceInfo.HardwareVersion) &&
                pktContent.Dequeue(out deviceInfo.SoftwareVersion) &&
                pktContent.Dequeue(out deviceInfo.WhiteBoardInnerNumber) &&
                pktContent.Dequeue(out deviceInfo.WhiteBoardOuterNumber) &&
                pktContent.Dequeue(out deviceInfo.Capacity) &&
                pktContent.Dequeue(out deviceInfo.StandardCount) &&
                pktContent.Dequeue(out deviceInfo.TestCount) &&
                pktContent.Dequeue(out deviceInfo.BlackCalibration) &&
                pktContent.Dequeue(out deviceInfo.WhiteCalibration) &&
                pktContent.Dequeue(out deviceInfo.SN) &&
                pktContent.Dequeue(out deviceInfo.Mark))
            {
                for (int i = 0; i < deviceInfo.Tolerance.Length; i++)
                {
                    if (!pktContent.Dequeue(out deviceInfo.Tolerance[i]))
                        return InstrumentType.Unknown;
                }

                if (string.Compare(deviceInfo.InstrumentMode, "Colorimeter") == 0)
                {
                    return InstrumentType.YR;
                }
                Regex regex = new Regex("^H.YD50*");
                var res = regex.Match(deviceInfo.HardwareVersion);
                if (res.Success)
                {
                    return InstrumentType.Unknown;
                }
                return InstrumentType.YS;
            }

            pktContent = new PacketContent(_recvList);
            if (pktContent.Dequeue(out deviceInfo.InstrumentMode) &&
                pktContent.Dequeue(out deviceInfo.OpticalStruct) &&
                pktContent.Dequeue(out deviceInfo.SoftwareVersion) &&
                pktContent.Dequeue(out deviceInfo.WhiteBoardInnerNumber) &&
                pktContent.Dequeue(out deviceInfo.WhiteBoardOuterNumber) &&
                pktContent.Dequeue(out deviceInfo.Capacity) &&
                pktContent.Dequeue(out deviceInfo.StandardCount) &&
                pktContent.Dequeue(out deviceInfo.TestCount) &&
                pktContent.Dequeue(out deviceInfo.BlackCalibration) &&
                pktContent.Dequeue(out deviceInfo.WhiteCalibration))
            {
                return InstrumentType.NS;
            }

            pktContent = new PacketContent(_recvList);
            if (pktContent.Dequeue(out deviceInfo.InstrumentMode) &&
                pktContent.Dequeue(out deviceInfo.OpticalStruct) &&
                pktContent.Dequeue(out deviceInfo.SoftwareVersion) &&
                pktContent.Dequeue(out deviceInfo.Capacity) &&
                pktContent.Dequeue(out deviceInfo.StandardCount) &&
                pktContent.Dequeue(out deviceInfo.TestCount) &&
                pktContent.Dequeue(out deviceInfo.BlackCalibration) &&
                pktContent.Dequeue(out deviceInfo.WhiteCalibration))
            {
                return InstrumentType.NR;
            }
            return InstrumentType.Unknown;
        }

        private InstrumentType DetectInstrumentType()
        {
            _recvEvent.Reset();
            CommDevice.Write(new byte[] { 0x55, 0xaa, 0xa1, 0x00, 0x00, 0x00, 0x02, 0x00, 0x02 }, 0, 9);
#if true
            if (!_recvEvent.WaitOne(5000))
#else
            if (!_recvEvent.WaitOne(30000))
#endif
            {
                return InstrumentType.Unknown;
            }

            if (_parser != null && _parser.IsCompleted)
            {
                return DetectByParser();
            }
            if (_ackPacket.ProductType == (int)ThreeNH.CommProtocol.ProductType.BTS)
            {
                // 版本2开始支持JSON
                if (_ackPacket.Version == 2)
                {
                    return InstrumentType.JSON;
                }
                return InstrumentType.BTS;
            }

            if (_ackPacket.ProductType == (int)ThreeNH.CommProtocol.ProductType.PTS)
            {
                return InstrumentType.PTS;
            }

            if (_ackPacket.ProductType == (int)ThreeNH.CommProtocol.ProductType.YH)
            {
                return InstrumentType.YH;
            }

            if (_ackPacket.ProductType == (int)ThreeNH.CommProtocol.ProductType.CR)
            {
                return InstrumentType.CR;
            }

            if (_ackPacket.ProductType == (int)ThreeNH.CommProtocol.ProductType.JSON)
            {
                return InstrumentType.JSON;
            }

            return InstrumentType.Unknown;
        }

        public void Dispose()
        {
            CommDevice?.EndReceiveAsync();
            CommDevice = null;
        }

        ~InstrumentTypeDetector()
        {
            CommDevice?.EndReceiveAsync();
            CommDevice = null;
        }
    }

    internal class BleInstrumentDetector : EhMc17BluetoothCommunication
    {
        static readonly byte GET_PNP_ID = 10;

        public string GetPnpId()
        {
            Trace.WriteLine("获取pnpid的当前时间");
            var result = SendPacket(new CmdPacket(CommandType.CALL, GET_PNP_ID), 2000);
            if (result != null)
            {
                Trace.WriteLine("获取有结果pnpid的当前时间");
                return Encoding.ASCII.GetString(result);
            }
            Trace.WriteLine("获取没有结果pnpid的当前时间");
            return null;
        }

        public static bool IsJsonProtocol(SerialPort serialPort)
        {
            var readTimeout = serialPort.ReadTimeout;
            // 发送查询仪器信息的命令来检查是否支持协议。
            try
            {
                serialPort.ReadTimeout = 1000;
                // 先发送一个空行，用以清除前面可能发送的无效命令
                serialPort.WriteLine("INF");
                serialPort.WriteLine("INF");
                try
                {
                    while (true)
                    {
                        var line = serialPort.ReadLine();
                        if (line.StartsWith("OK"))
                        {
                            return true;
                        }
                    }
                }
                catch (TimeoutException)
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                Trace.TraceInformation("IsJsonProtocol: {0}", ex.Message);
                return false; 
            }
            finally
            {
                serialPort.ReadTimeout = readTimeout;
            }
        }

        /// <summary>
        /// 判断是否是PTS系列仪器
        /// </summary>
        /// <param name="serialPort">已连接的串口</param>
        /// <returns>如果是PTS返回真，否则返回假</returns>
        public static InstrumentType Detect(SerialPort serialPort)
        {
            // 先判断是否是 json 通用接口
            //TS系列先调用此接口会导致无法获取PnpId
#if false
            if (IsJsonProtocol(serialPort))
            {
                return InstrumentType.JSON;
            }
#endif
            // 尝试获取仪器类型，最多尝试两次
            var instrumentType = GetPnpId(serialPort);
            if (instrumentType == InstrumentType.Unknown)
            {
                instrumentType = GetPnpId(serialPort);
            }

            // 如果两次尝试后仍然未知，则检查是否为 JSON 协议
            if (instrumentType == InstrumentType.Unknown && IsJsonProtocol(serialPort))
            {
                instrumentType = InstrumentType.JSON;
            }

            return instrumentType;
        }

        private static InstrumentType GetPnpId(SerialPort serialPort)
        {
            var instrumentType = InstrumentType.Unknown;
            using (BleInstrumentDetector s = new BleInstrumentDetector())
            {
                try
                {
                    s.AttatchSerialPort(serialPort);
                    var pnpId = s.GetPnpId().Trim(new char[] { ' ', '\n', '\r', '\0' }).ToUpper();
                    s.DetachSerialPort();
                    switch (pnpId)
                    {
                        case "TNH PTS":
                            instrumentType = InstrumentType.PTS;
                            break;
                        case "TNH PTSMCR":
                            instrumentType = InstrumentType.CR;
                            break;
                        case "TNH BTS":
                            if (IsJsonProtocol(serialPort))
                                return InstrumentType.JSON;
                            instrumentType = InstrumentType.BTS;
                            break;
                        case "CLR-HZ":
                            instrumentType = InstrumentType.YH;
                            break;
                        case "TNH-HZ":
                            instrumentType = InstrumentType.YH;
                            break;
                        default:
                            Trace.TraceError("Unknown instrument type: {0}", pnpId);
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Trace.TraceError("GetPnpId: {0}", ex.Message);
                    // 不是
                    s.DetachSerialPort();
                }
            }
            return instrumentType;
        }
    }
}