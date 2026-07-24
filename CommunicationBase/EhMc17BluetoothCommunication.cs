using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;


namespace ThreeNH.CommunicationBase
{
    /// <summary>
    /// 命令类型
    /// </summary>
    public enum CommandType : byte
    {
        /// <summary>
        /// 通知，该类型命令无返回
        /// </summary>
        NOTIFICATION = 1,
        /// <summary>
        /// 调用类型，该命令类型有一个<c>RETURN</c>类型的同命令码的返回
        /// </summary>
        CALL,
        /// <summary>
        /// 调用类型的返回
        /// </summary>
        RETURN,
    }

    /// <summary>
    /// 通知命令
    /// </summary>
    enum NotificationCode : byte
    {
        CONNECTED = 1,       // 蓝牙连接事件
        DISCONNECTED,   // 连接断开事件
    }

    // 命令包
    public class CmdPacket
    {
        public const int PacketHeaderLength = 4; // 包头长度

        private List<byte> _packet;

        /// <summary>
        /// 命令码的类型
        /// </summary>
        public CommandType CommandType => (CommandType)_packet[0];

        /// <summary>
        /// 包的命令码
        /// </summary>
        public byte Command => _packet[1];

        /// <summary>
        /// 包的数据长度
        /// </summary>
        public int DataLength => (_packet[2] | (_packet[3] << 8));

        /// <summary>
        /// 包的数据
        /// </summary>
        public List<byte> Data => _packet.Count > DataLength
            ? _packet.GetRange(PacketHeaderLength, DataLength)
            : null;

        /// <summary>
        /// 是否是完整包
        /// </summary>
        public bool IsCompletedPacket =>
            (_packet.Count >= PacketHeaderLength) && (_packet.Count >= PacketHeaderLength + DataLength);

        /// <summary>
        /// 创建一个空包
        /// </summary>
        public CmdPacket()
        {
            _packet = new List<byte>();
        }

        /// <summary>
        /// 从命令类型、命令码和数据创建一个包
        /// </summary>
        /// <param name="cmd_type">命令类型</param>
        /// <param name="cmd">命令码</param>
        /// <param name="data">数据，可以为null</param>
        public CmdPacket(CommandType cmd_type, byte cmd, byte[] data = null)
        {
            UInt16 length = (UInt16)(data?.Length ?? 0);
            _packet = new List<byte>(PacketHeaderLength + length);
            _packet.AddRange(new[] { (byte)cmd_type, cmd, (byte)(length & 0xFF), (byte)((length >> 8) & 0xFF) });
            if (data != null)
            {
                _packet.AddRange(data);
            }
        }

        /// <summary>
        /// 从数据包创建一个包
        /// </summary>
        /// <param name="packet">包的数据</param>
        public CmdPacket(IEnumerable<byte> packet)
        {
            _packet = new List<byte>(packet);
        }

        /// <summary>
        /// 获取整个包的字节序列
        /// </summary>
        /// <returns>整个包的字节序列</returns>
        public byte[] ToBytes()
        {
            return _packet.ToArray();
        }

        /// <summary>
        /// 向包中追加一个字节的数据
        /// </summary>
        /// <param name="b">要追加的字节</param>
        /// <param name="update_data_length">是否更新包数据长度字段</param>
        public void AppendData(byte b, bool update_data_length = false)
        {
            _packet.Add(b);
            if (update_data_length)
            {
                UpdateDataLength();
            }
        }

        /// <summary>
        /// 修改包中指定位置数据
        /// </summary>
        /// <param name="data">要追加的数据</param>
        /// <param name="update_data_length">是否更新包数据长度字段</param>
        public void UpdateData(int index, byte value)
        {
            _packet[index] = value;
        }
        /// <summary>
        /// 向包中追加字节数据
        /// </summary>
        /// <param name="data">要追加的数据</param>
        /// <param name="update_data_length">是否更新包数据长度字段</param>
        public void AppendData(IEnumerable<byte> data, bool update_data_length = false)
        {
            _packet.AddRange(data);
            if (update_data_length)
            {
                UpdateDataLength();
            }
        }

        /// <summary>
        /// 根据包中存储的实际数据长度更新包的数据长度字段
        /// </summary>
        public void UpdateDataLength()
        {
            if (_packet.Count >= PacketHeaderLength)
            {
                int data_length = _packet.Count - PacketHeaderLength;
                _packet[2] = (byte)(data_length & 0xFF);
                _packet[3] = (byte)((data_length >> 8) & 0xFF);
            }
        }
    };

    /// <summary>
    /// 收到仪器命令
    /// </summary>
    /// <param name="record">测量结果记录，如果是测量标样结果为<c>StandardRecord</c>，
    /// 如果是试样测量结果则为<c>TrialRecord</c>类型</param>
    public delegate void InstrumentCmdPacketReceived(CmdPacket packet);

    /// <summary>
    /// EH-MC17的蓝牙模块通讯协议类
    /// </summary>
    public class EhMc17BluetoothCommunication : IDisposable
    {
        private SerialPort _serialPort;
        private string _exitCode; // 退出码
        private ManualResetEvent _event = new ManualResetEvent(false);
        private CmdPacket _receivedPacket;
        private Mutex _receivedPacketMutex = new Mutex();
        private CmdPacket _receivingPacket; // 正在接收的包
        private DateTime _timestamp = DateTime.Now; // 接收包的时间截
        private HashSet<byte> _listeningNotifications = new HashSet<byte>(); // 监听的通知码

        #region Public Properties

        public bool IsOpen => _serialPort != null && _serialPort.IsOpen;

        protected InstrumentCmdPacketReceived InstrumentCmdPacketReceived;

        public SerialPort SerialPort
        {
            get => _serialPort;
            set => _serialPort = value;
        }

        /// <summary>
        /// 监听的仪器端通知码，在接收到在监听的通知码时将调用<c>InstrumentCmdPacketReceived</c>
        /// </summary>
        protected HashSet<byte> ListeningNotificationCode => _listeningNotifications;

        #endregion

        public EhMc17BluetoothCommunication()
        {
        }

        #region Public Methods

        public void DisableDataReceived()
        {
            if (_serialPort != null)
            {
                _serialPort.DataReceived -= OnDataReceived;
            }
        }

        public void EnableDataReceived()
        {
            if (_serialPort != null)
            {
                _serialPort.DataReceived += OnDataReceived;
            }
        }

        /// <summary>
        /// 打开设备
        /// </summary>
        /// <param name="portName">设备的串口名</param>
        /// <param name="macAddress">蓝牙的MAC地址</param>
        public void Open(string portName, string macAddress = null)
        {
            _serialPort = new SerialPort(portName, 115200, System.IO.Ports.Parity.None, 8, StopBits.One);
            _serialPort.Open();

            // 初始化蓝牙Dong
            if (!InitBluetoothDongle())
            {
                _serialPort.Close();
                throw new ConfigurationException("配置蓝牙模块失败");
            }

            // 查找可连接的蓝牙从设备
            if (!ConnectToSlave(macAddress))
            {
                _serialPort.Close();
                throw new FileNotFoundException("找不到蓝牙从设备");
            }


            _serialPort.DiscardInBuffer();
            _serialPort.DataReceived += OnDataReceived;

            _serialPort.PinChanged += (s, e) =>
            {
                if (e.EventType == SerialPinChange.CDChanged && !_serialPort.CDHolding)
                {
                    Trace.TraceWarning("Carrier detect lost → treat as disconnect");
                    Close();                       // 让上层立刻感知
                }
            };

            if (!CreateSession())
            {
                Thread.Sleep(2000);
            }
        }


        public virtual void AttatchSerialPort(SerialPort serialPort)
        {
            if (_serialPort != null)
            {
                throw new CommunicationException(CommunicationError.DeviceHasOpened);
            }
            _serialPort = serialPort;
            // _serialPort.DiscardInBuffer();
            _serialPort.DataReceived += OnDataReceived;

            if (!CreateSession())
            {
                Thread.Sleep(1500); // 休眠1秒
            }
        }

        public SerialPort DetachSerialPort()
        {
            CloseSession();
            _serialPort.DataReceived -= OnDataReceived;
            var serialPort = _serialPort;
            _serialPort = null;
            return serialPort;
        }

        public void Close()
        {
            if (_serialPort != null && _serialPort.IsOpen)
            {
                try
                {
                    CloseSession();
                    // 退出蓝牙透传模式并断开连接
                    _serialPort.DataReceived -= OnDataReceived;
                    DeinitBluetoothDongle();
                    _serialPort.Close();
                }
                finally
                {
                    _serialPort.DataReceived -= OnDataReceived;
                    _serialPort = null;
                }
            }
        }

        #endregion

        #region Private Methods

        private void OnDataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            if (e.EventType == SerialData.Eof)
            {
                return;
            }
            if (_receivingPacket == null || _receivingPacket.IsCompletedPacket || (DateTime.Now - _timestamp).TotalSeconds > 1)
            {
                _receivingPacket = new CmdPacket();
            }
            _timestamp = DateTime.Now;


            var buffer = new byte[_serialPort.BytesToRead];
            try
            {
                _serialPort.Read(buffer, 0, buffer.Length);
#if DEBUG
                Trace.TraceInformation("接收到数据：{0}", buffer.Length);
#endif
                try
                {
                    // 检测蓝牙从设备是否断开
                    if (buffer.Length == 13 && Encoding.ASCII.GetString(buffer) == "+DISCONNECT\r\n")
                    {
#if DEBUG
                        Trace.TraceInformation("Slave device disconnected");
#endif
                        Close();
                        InstrumentCmdPacketReceived?.Invoke(null);
                        return;
                    }
                }
                catch
                {
                    _receivedPacket = null;
                    _receivingPacket = null;
                    // do nothing
                }
                _receivingPacket.AppendData(buffer);
                if (_receivingPacket.IsCompletedPacket)
                {
#if DEBUG
                    Trace.TraceInformation("接收到包类型{0}，命令码{1}",
                        Enum.GetName(typeof(CommandType), _receivingPacket.CommandType), _receivingPacket.Command);
#endif
                    if (_receivingPacket.CommandType == CommandType.NOTIFICATION && ListeningNotificationCode.Contains(_receivingPacket.Command))
                    {
                        InstrumentCmdPacketReceived?.Invoke(_receivingPacket);
                    }
                    else
                    {
                        _receivedPacketMutex.WaitOne();
                        _receivedPacket = _receivingPacket;
                        _receivingPacket = null;
                        _receivedPacketMutex.ReleaseMutex();
                        _event.Set();
                    }
                }
            }
            catch (Exception ex)
            {
                _receivedPacket = null;
                _receivingPacket = null;
                Trace.TraceInformation("OnDataReceived Exception: {0}", ex);
                // _event.Set();
            }
        }
        private object _responseLock=new object();
        /// <summary>
        /// 发送数据包
        /// </summary>
        /// <param name="packet">要发送的命令包</param>
        /// <param name="result">接收到的数据</param>
        /// <param name="timeout">超时设置</param>
        /// <returns>如果发送完成并接收到返回返回<c>true</c>；如果串口未打开台超时返回<c>false</c></returns>
        /// <exception cref="CommunicationException"></exception>
        protected byte[] SendPacket(CmdPacket packet, int timeout)
        {
            if (!IsOpen)
            {
                throw new CommunicationException(CommunicationError.DeviceNotOpen);
            }

            _event.Reset();

            // 1. 丢掉硬件缓冲区里的老数据
            _serialPort.DiscardInBuffer();
            _serialPort.DiscardOutBuffer();

            // 2. 丢掉正在拼的半包
            _receivingPacket = null;

            // 3. 丢掉上一次响应槽/事件状态
            lock (_responseLock)
            {
                _receivedPacket = null;
            }
#if DEBUG
            Trace.TraceInformation($"发送{packet.CommandType.ToString()}:{packet.Command.ToString()}\r\n");
#endif

            var bytes = packet.ToBytes();

            _serialPort.Write(bytes, 0, bytes.Length);

            if (!_event.WaitOne(timeout))
            {
                Trace.TraceInformation("SendPacket timeout");
                throw new CommunicationException(CommunicationError.ReceivingTimeout);
            }

            _receivedPacketMutex.WaitOne();

            var result = _receivedPacket?.Data?.ToArray() ?? new byte[0];
            _receivedPacket = null;

            _receivedPacketMutex.ReleaseMutex();
            _event.Reset();
            return result;
        }

        private bool CreateSession()
        {
            // try
            // {
            //     var bytes = SendPacket(new CmdPacket(CommandType.NOTIFICATION, (byte)NotificationCode.CONNECTED),
            //         1000);
            //     return bytes.Length == 2 && bytes[0] == 'O' && bytes[1] == 'K';
            // }
            // catch (CommunicationException ex)
            // {
            //     Trace.TraceInformation("CreateSession: {0}", ex.Message);
            //     return false;
            // }
            return true;
        }

        private void CloseSession()
        {
            // var bytes = (new CmdPacket(CommandType.NOTIFICATION, (byte)NotificationCode.DISCONNECTED)).ToBytes();
            // try
            // {
            //     _serialPort.Write(bytes, 0, bytes.Length);
            // }
            // catch
            // {
            //     // do nothing
            // }
        }

        private bool ConnectToSlave(string slaveAddress = null)
        {
            try
            {
                var at = new EhMc17AtInterface(_serialPort);
                if (slaveAddress == null)
                {
                    var slaveDevices = at.ScanSlaveDevices();
                    if (slaveDevices == null || slaveDevices.Length == 0)
                    {
                        return false;
                    }

                    slaveAddress = slaveDevices[0].Address;
                }

                string exitCode;
                if (at.Connect(slaveAddress, out exitCode))
                {
                    _exitCode = exitCode;
                    return true;
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError("Connect to slave device failed: {0}", ex.Message);
            }
            return false;
        }

        /// <summary>
        /// 初始化蓝牙狗
        /// </summary>
        /// <returns>成功返回真，失败返回返回假</returns>
        private bool InitBluetoothDongle()
        {
            try
            {
                var at = new EhMc17AtInterface(_serialPort);
                // 1. 获取模式，如果是从模式将其设为主模式
                at.ExitPassThroughMode();
                at.Disconnect();
                var role = at.GetRole();
                if (role == null)
                {
                    // // 1.1 可能由于原来未正常退出透传模式，尝试退出
                    // if (at.ExitPassThroughMode() && at.Disconnect())
                    // {
                    //     role = at.GetRole();
                    // }
                    // if (role == null)
                    // {
                    Trace.TraceError("Get bluetooth module role failed\r\n");
                    return false;
                    // }
                }

                if (role != EhMc17AtInterface.ROLE_MASTER_DEVICE)
                {
                    if (!at.SetRole(EhMc17AtInterface.ROLE_MASTER_DEVICE))
                    {
                        Trace.TraceError("Set bluetooth module role as master failed\r\n");
                        return false;
                    }
                }

                // 2. 获取透传模式，设置透传模式为可退出透传
                var byMode = at.GetByMode();
                if (byMode == null)
                {
                    Trace.TraceError("Get ByMode failed\r\n");
                    return false;
                }

                if (byMode != EhMc17AtInterface.EXITABLE_PASSTHROUGH)
                {
                    if (!at.SetByMode(EhMc17AtInterface.EXITABLE_PASSTHROUGH))
                    {
                        Trace.TraceError("Set ByMode as EXITABLE_PASSTHROUGH failed\r\n");
                        return false;
                    }
                }
                // 3. 获取退出码长度，如果不是16位将其设为16位
                // var exitCodeLength = at.GetExitCodeLength();
                // if (exitCodeLength == 0)
                // {
                //     Trace.TraceError("Get exit code length failed\r\n");
                //     // return false;
                // }
                //
                // if (exitCodeLength != 16)
                // {
                if (!at.SetExitCodeLength(16))
                {
                    Trace.TraceError("Set exit code length as 16 failed\r\n");
                    return false;
                }
                // }
                // 4. 获取退出码，如果不是默认退出码将其设为默认退出码
                var exitCodeRefreshMode = at.GetExitCodeRefreshMode();
                if (exitCodeRefreshMode != EhMc17AtInterface.USE_DEFAULT_EXIT_CODE)
                {
                    if (!at.SetExitCodeRefreshMode(EhMc17AtInterface.USE_DEFAULT_EXIT_CODE))
                    {
                        Trace.TraceError("Set exit code refresh mode as default exit code failed\r\n");
                        return false;
                    }
                }
                _exitCode = at.GetExitCode(); // 同时获取退出码，因为设置之后原来的退出码需要重置才会改变
            }
            catch (Exception ex)
            {
                Trace.TraceError("Init bluetooth dongle failed：{0}", ex.Message);
            }

            return true;
        }

        /// <summary>
        /// 退出透传模式并断开设备连接
        /// </summary>
        private bool DeinitBluetoothDongle()
        {
            try
            {
                var at = new EhMc17AtInterface(_serialPort);
                if (at.ExitPassThroughMode(_exitCode) && at.Disconnect())
                {
                    return true;
                }
            }
            catch (Exception e)
            {
                Trace.TraceError("Exit passthrough mode failed: {0}", e.Message);
            }

            return false;
        }

        #endregion

        public void Dispose()
        {
            if (IsOpen)
            {
                Close();
            }
            _event?.Dispose();
            _event = null;
            _receivedPacketMutex?.Dispose();
            _receivedPacketMutex = null;
        }

    }
}
