using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using ThreeNH.Helper;
using ThreeNH.Helper.Plugin.Utils;
using ThreeNH.Helper.Plugin.Utils.CommunicationBase;
using ThreeNH.PortService;
using ThreeNH.StructHelper;

namespace ThreeNH.CommDevice
{
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
    public class InstrumentTypeDetector : IDisposable
    {

        // 是否是STM32平台设备
        public static bool IsSTM32VirtualPort(SerialPortInformation portInfo)
        {
            return portInfo.IsSTM32VirtualPort();
        }

        // 是否是展讯平台
        public static bool IsSCIUsb2Serial(SerialPortInformation portInfo)
        {
            return portInfo.IsSCIUsb2Serial();
        }
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
        public static InstrumentType Detect(ICommDevice port)
        {
            using (var detector = new InstrumentTypeDetector(port))
            {
                try
                {
                    //改成发送多次命令(防止某些仪器初始化发送命令不回复的情况,比如YS30系列蓝牙转串口设备)
                    for (int i = 0; i < 10; i++)
                    {
                        var type = detector.DetectInstrumentType();
                        if (type != InstrumentType.Unknown)
                            return type;
                    }
                    return InstrumentType.Unknown;
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

            if (!_recvEvent.WaitOne(5000))
            {
                return InstrumentType.Unknown;
            }

            if (_parser != null && _parser.IsCompleted)
            {
                return DetectByParser();
            }
            if (_ackPacket.ProductType == (int)ProductType.BTS)
            {
                // 版本2开始支持JSON
                if (_ackPacket.Version == 2)
                {
                    return InstrumentType.JSON;
                }
                return InstrumentType.BTS;
            }

            if (_ackPacket.ProductType == (int)ProductType.PTS)
            {
                return InstrumentType.PTS;
            }

            if (_ackPacket.ProductType == (int)ProductType.YH)
            {
                return InstrumentType.YH;
            }

            if (_ackPacket.ProductType == (int)ProductType.CR)
            {
                return InstrumentType.CR;
            }

            if (_ackPacket.ProductType == (int)ProductType.JSON)
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
}
