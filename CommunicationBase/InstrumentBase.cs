using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Ports;
using System.Threading;
using ThreeNH.PortService;
using ThreeNH.CommDevice;
using System.Text;
using System.Linq;

namespace ThreeNH.CommunicationBase
{
    public class InstrumentBase
    {
        private byte _waitingCommand = 0;
        private int _isBusy = 0;
        private DataPacket _receivedPacket = null;
        private EventWaitHandle _receivedPacketEvent = new EventWaitHandle(false, EventResetMode.AutoReset);
        private Mutex _receivedPacketMutex = new Mutex();
        private List<byte> _buffer = new List<byte>();
        private ICommDevice _device;


        public InstrumentBase()
        {
            _device = new SerialPortDevice();
        }

        ~InstrumentBase()
        {
            try
            {
                Disconnect();
            }
            catch
            {
                //
            }
        }

        public ICommDevice CommunicationDevice
        {
            get { return _device; }
            set
            {
                if (IsOpen)
                {
                    throw new CommunicationException(CommunicationError.DeviceHasOpened);
                }
                _device = value;
                if (_device.IsOpen)
                {
                    _device.BeginReceiveAsync(OnDataReceived);
                }
                DeviceName = _device.DeviceName;
            }
        }

        public ICommDevice DetachCommunicationDevice()
        {
            if (_device != null)
            {
                _device.EndReceiveAsync();
            }
            var tmp = _device;
            _device = null;
            return tmp;
        }

        public string DeviceName { get; protected set; }

        public bool IsOpen
        {
            get
            {
                return _device.IsOpen;
            }
        }

        public virtual void Connect(object device = null)
        {
            if (device is ICommDevice)
            {
                CommunicationDevice = (ICommDevice)device;
                return;
            }

            if (IsOpen)
            {
                throw new CommunicationException(CommunicationError.DeviceHasOpened);
            }

            _isBusy = 0;
            try
            {
                _device.Open(device);
            }
            catch (IOException)
            {
                throw new CommunicationException(CommunicationError.DeviceNotFound);
            }

            if (!_device.BeginReceiveAsync(OnDataReceived))
            {
                Trace.TraceError("[{0}]_device.BeginReceiveAsync(OnDataReceived)调用失败");
            }

            DeviceName = _device.DeviceName;
        }

        public bool IsBusy
        {
            get { return _isBusy != 0; }
        }

        /// <summary>
        /// 通信超时或长时间空闲后，清除残留收包状态，避免后续命令 DeviceBusy 或解析错乱。
        /// </summary>
        public void ResetCommunicationState()
        {
            if (_waitingCommand != 0)
            {
                _receivedPacketEvent.Set();
            }

            _receivedPacketMutex.WaitOne();
            try
            {
                _waitingCommand = 0;
                _receivedPacket = null;
            }
            finally
            {
                _receivedPacketMutex.ReleaseMutex();
            }

            _buffer.Clear();
            Interlocked.Exchange(ref _isBusy, 0);

            try
            {
                PacketParser?.Reset();
            }
            catch (Exception ex)
            {
                Trace.TraceWarning("[{0}] ResetCommunicationState parser reset failed: {1}", this, ex.Message);
            }
        }

        public virtual void Disconnect()
        {
            if (_device == null)
                return;

            if (_waitingCommand != 0)
            {
                _receivedPacketEvent.Set();
                _receivedPacketMutex.WaitOne();
                _waitingCommand = 0;
                _receivedPacket = null;
                _buffer.Clear();
                _receivedPacketMutex.ReleaseMutex();
            }
            try
            {
                _isBusy = 0;
                _device.Close();
            }
            catch
            {
                // do nothing
            }
        }

        /// <summary>
        /// 发送一个命令
        /// </summary>
        /// <param name="command">要发送的命令</param>
        /// <param name="content">命令附加的数据</param>
        /// <param name="ack">发送给仪器的应答，只有某些命令需要</param>
        /// <param name="waitingAck">是否等待应答命令返回</param>
        /// <param name="timeout">等待超时时间，单位为毫秒</param>
        /// <returns>如果等waitingAck为true且发送的命令的应答命令不为空，等待应答命令返回</returns>
        public static int RetryCount { get; set; } = 3;
        public static int RetryIntervalMs { get; set; } = 1000;

        protected DataPacket SendCommand(byte command,
                                         PacketContent content = null,
                                         byte? ack = null,
                                         bool waitingAck = true,
                                         int timeout = 10000)
        {
            if (!IsOpen)
                throw new CommunicationException(CommunicationError.DeviceNotOpen);

            if (Interlocked.CompareExchange(ref _isBusy, 1, 0) != 0)
                throw new CommunicationException(CommunicationError.DeviceBusy);

            // 把真正逻辑抽出来，方便循环
            DataPacket DoOnce()
            {
                _waitingCommand = waitingAck ? AckCommand(command) : (byte)0;
                _receivedPacketEvent.Reset();

                DataPacket packet = new DataPacket
                {
                    Command = command,
                    PacketContent = content,
                    Ack = ack
                };

                try
                {
                    var data = packet.ToBytes();
                    if (data != null && data.Length > 0)
                    {
                        Trace.TraceInformation("开始写入命令码：{0}:", command);
                        Trace.TraceInformation(string.Join(" ", data.Select(b => b.ToString("X2"))));
                    }
                    _device.Write(data, 0, data.Length);
                }
                catch
                {
                    _waitingCommand = 0;
                    throw;
                }

                if (_waitingCommand == 0)          // 不等待应答
                    return null;

                bool ok = _receivedPacketEvent.WaitOne(timeout);
                _receivedPacketMutex.WaitOne();
                _waitingCommand = 0;
                try
                {
                    if (!ok)                       // 超时
                        return null;               // 不抛，交给外层判断
                    if (_receivedPacket == null)
                        throw new CommunicationException(CommunicationError.DeviceDisconnected);
                    var ret = _receivedPacket;
                    _receivedPacket = null;
                    return ret;
                }
                finally
                {
                    _receivedPacketMutex.ReleaseMutex();
                }
            }

            try
            {
                DataPacket result = null;
                for (int i = 0; i <= RetryCount; i++)
                {
                    result = DoOnce();
                    if (result != null)            // 收到数据立即返回
                        return result;

                    // 最后一次超时后不再睡，直接抛
                    if (i < RetryCount)
                    {
                        Trace.TraceWarning("命令 0x{0:X2} 超时，第 {1} 次重试…", command, i + 1);
                        Thread.Sleep(RetryIntervalMs);
                    }
                }
                // 走到这里说明最后一次依旧超时
                throw new CommunicationException(CommunicationError.ReceivingTimeout);
            }
            finally
            {
                Interlocked.Decrement(ref _isBusy);
            }
        }

        protected virtual PacketParser PacketParser
        {
            get { throw new NotImplementedException(); }
        }

        private void OnDataReceived(object sender, byte[] data, Exception ex)
        {
            Interlocked.Increment(ref _isBusy);

            // 收到null表示连接已经异常断开
            if (data == null)
            {
                Interlocked.Decrement(ref _isBusy);
                Disconnect();
                return;
            }
            // 检测蓝牙从设备是否断开
            if (data.Length == 13 && Encoding.ASCII.GetString(data) == "+DISCONNECT\r\n")
            {
#if DEBUG
                Trace.TraceInformation("Slave device disconnected");
#endif
                Interlocked.Decrement(ref _isBusy);
                Disconnect();
                DataPacket bleResult = new DataPacket()
                {
                    Command = 0xFA
                };
                OnPacketArrived(bleResult);
                return;
            }
            _buffer.AddRange(data);
            DataPacket result = null;
            try
            {
#if TRACE
                Trace.TraceInformation("[{0}]准备角析数据...", this);
#endif
                int n = PacketParser.Parse(_buffer.ToArray(), 0, _buffer.Count);
#if TRACE
                Trace.TraceInformation("[{0}]解析返回", this);
#endif 
                _buffer.RemoveRange(0, n);  // 移除已解析的数据

                if (PacketParser.IsCompleted)
                {
#if TRACE
                    Trace.TraceInformation("[{0}]解析完成", this);
#endif
                    result = PacketParser.Result;
                    PacketParser.Reset();       // 重置解析器

                    if (_waitingCommand == result.Command)
                    {
                        _receivedPacketMutex.WaitOne();
                        if (_receivedPacket != null)
                        {
                            Trace.TraceWarning("[{0}] in OnDataReceived, _receivedPacket != null", this);
                        }
                        _receivedPacket = result;
                        _receivedPacketMutex.ReleaseMutex();
                        Interlocked.Decrement(ref _isBusy);
#if TRACE
                        Trace.TraceInformation("[{0}]in OnDataReceived _isBusy = {1}", this, _isBusy);
#endif
                        _receivedPacketEvent.Set();
                    }
                    else
                    {
                        Interlocked.Decrement(ref _isBusy);
                        OnPacketArrived(result);
                    }
                }
                else
                {
                    Interlocked.Decrement(ref _isBusy);
                }
            }
            catch (Exception pex)
            {
                Trace.TraceError("[{0}]解析出错：{1}\n{2}", this, pex.Message, pex.StackTrace);
                Interlocked.Decrement(ref _isBusy);
            }
        }

        // 当收到仪器发送过来的命令时调用此函数
        // 该函数可能在辅助线程中执行
        protected virtual void OnPacketArrived(DataPacket packet, Exception ex = null)
        {
            // do nothing
        }

        /// <summary>
        /// 获取命令的应答命令
        /// </summary>
        /// <param name="command">发送给仪器的命令</param>
        /// <returns>仪器应答的命令；如果没有应答命令返回0</returns>
        protected virtual byte AckCommand(byte command)
        {
            throw new NotImplementedException();
        }
    }
}
