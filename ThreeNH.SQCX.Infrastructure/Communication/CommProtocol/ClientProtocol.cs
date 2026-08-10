using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ThreeNH.CommDevice;
using System.Diagnostics;
using System.Threading;
using System.Runtime.InteropServices;
using ThreeNH.PortService;
//using System.IO.Ports;
using ThreeNH.StructHelper;
using System.Threading.Tasks;

namespace ThreeNH.CommProtocol
{
    public enum ErrorCode
    {
        Successfull, // 操作成功
        AbortedByRemote, // 远端中止了传输或接收
        Timeout, // 接收或发送数据超时
    }

    public enum ProductType
    {
        BTS, // BTS系列
        PGM, // YG系列
        PTS, // PTS系列
        YH,  // YH系列
        CR,
        JSON,   // 使用JSON格式通讯
        HPBTS,  //高精度台式机
        YT,//YT系列
    }


    public enum Status
    {
        NoSession, // 没有连接
        ShakingHands, // 正在建立连接
        Ready, // 就绪，可收发数据
        Sending, // 正在发送数据
        Recving, // 正在接收数据
        Closing, // 正在关闭会话
    }

    public class ConnectedEventArgs : EventArgs
    {
        public ConnectedEventArgs(bool isOk)
        {
            IsOk = isOk;
        }

        // 是否成功建立连接
        public bool IsOk { get; }
    }

    public class ReceivedEventArgs : EventArgs
    {
        private readonly byte _operation;

        public ReceivedEventArgs(byte operation, int offset, byte[] data,
            bool isLast, ErrorCode errorCode = ErrorCode.Successfull,
            int remoteError = 0)
        {
            _operation = operation;
            Offset = offset;
            Data = data;
            IsLast = isLast;
            ErrorCode = errorCode;
            RemoteErrorCode = remoteError;
            IsAbortRecv = false;
        }

        // 接收到的操作码
        public byte Operation => _operation;

        // 当前接收到的数据的偏移位置
        public int Offset { get; }

        // 接收到的数据 
        public byte[] Data { get; }

        // 是否是最后一 
        public bool IsLast { get; }

        // 错误码
        public ErrorCode ErrorCode { get; }

        // 对方传回来的错误码，当ErrorCode为AbortedByRemote时会收到此附加错误码
        public int RemoteErrorCode { get; }

        // 如果用户要终止接收数据，可将此设为TRUE
        public bool IsAbortRecv { get; set; }
    }

    public class SendingProgressChangedEventArgs : EventArgs
    {
        public SendingProgressChangedEventArgs(byte operation, int? total, int count,
            ErrorCode error = ErrorCode.Successfull, int remoteError = 0)
        {
            Total = total;
            Operation = operation;
            SentCount = count;
            ErrorCode = error;
            RemoteErrorCode = remoteError;
        }

        // 是否已全部发送
        public bool IsCompleted
        {
            get { return SentCount == Total || ErrorCode != ErrorCode.Successfull; }
        }

        // 发送的操作码
        public byte Operation { get; }

        // 要发送的总字节数，如果为null表示总长度未知
        public int? Total { get; }

        // 已发送的字节数
        public int SentCount { get; }

        // 错误码
        public ErrorCode ErrorCode { get; }

        // 对方传回来的错误码，当ErrorCode为AbortedByRemote时会收到此附加错误码
        public int RemoteErrorCode { get; }

    }

    //public class ListFileNamesEventArgs : EventArgs
    //{
    //    public ListFileNamesEventArgs(string[] file_names, bool is_completed, ErrorCode error=ErrorCode.Successfull)
    //    {
    //        FileNames = file_names;
    //        IsCompleted = is_completed;
    //        ErrorCode = error;
    //    }

    //    public string[] FileNames { get; private set; }
    //    public bool IsCompleted { get; private set; }
    //    public ErrorCode ErrorCode { get; private set; }
    //}

    public class PassThroughReceivedEventArgs : EventArgs
    {
        public PassThroughReceivedEventArgs(byte[] data)
        {
            Data = data;
        }

        public byte[] Data { get; private set; }
    }


    public delegate bool ListFileNamesHandler(List<KeyValuePair<string, int>> fileNames, bool isCompleted,
        ErrorCode error);

    public class ClientProtocol : IDisposable
    {
        #region 常量

        public const string PortDescription = "STMicroelectronics Virtual COM Port";
        public const byte InvalidOperation = 0; // 无效操作码
        public const byte CustomOperationStart = 0x21; // 仪器相关操作码开始序号

        #endregion // 常量

        #region 事件

        /// <summary>
        /// 调试信息事件
        /// </summary>
        public event Action<string> Message;
        protected void OnMessage(string msg)
        {
            //EventWaitHandle会阻塞,必须另起线程或者放到DispatcherThreadFunc线程中处理
            Task.Factory.StartNew(() => { Message?.Invoke(msg); });
        }

        /// <summary>
        /// 连接返回事件，参数指示连接是否成功
        /// </summary>
        public event EventHandler<ConnectedEventArgs> Connected;

        /// <summary>
        /// 断开连接事件
        /// </summary>
        public event EventHandler Disconnected;

        /// <summary>
        /// 接数到数据或命令事件
        /// </summary>
        public event EventHandler<ReceivedEventArgs> Received;

        /// <summary>
        /// 发送数据命令事件
        /// </summary>
        public event EventHandler<SendingProgressChangedEventArgs> SendingProgressChanged;

        /// <summary>
        /// 接收到透传数据事件
        /// </summary>
        public event EventHandler<PassThroughReceivedEventArgs> PassThroughDataReceived;

        #endregion // 事件

        #region 公共属性

        /// <summary>
        /// 通信设备
        /// </summary>
        public ICommDevice CommDevice
        {
            get { return _commDevice; }
            set
            {
                if (_commDevice == value)
                {
                    return;
                }

                if (IsOpen)
                {
                    Close();
                }

                _commDevice = value;
                if (_commDevice.IsOpen)
                {
                    Initialize();
                }
            }
        }

        /// <summary>
        /// 设备是否已打开
        /// </summary>
        public bool IsOpen => CommDevice != null && CommDevice.IsOpen;

        /// <summary>
        /// 是否已建立会话（打开通信设备并不代表建立了会话）
        /// </summary>
        public bool IsConnected
        {
            get { return IsOpen && _status != Status.NoSession; }
        }

        public bool IsPassthroughEnabled
        {
            get { return _passthroughEnabled; }
            set
            {
                //if (value != _passthroughEnabled)
                //{
                _passthroughEnabled = value;
                EnablePassThrough(value);
                //}
            }
        }

        public Status Status
        {
            get { return _status; }
        }

        public uint Version { get; private set; }

        public int ProductType { get; private set; }

        #endregion //  公共属性

        #region 公共方法

        ~ClientProtocol()
        {
            //Close();
            Dispose();
        }

        /// <summary>
        /// 打开设备
        /// </summary>
        /// <param name="deviceName">设备名</param>
        public bool Open(string deviceName)
        {
            lock (this)
            {
                _eventQueue.Clear();

                if (IsOpen)
                {
                    Trace.TraceWarning("must close previous device!");
                    return false;
                }

                if (CommDevice == null)
                {
                    Trace.TraceWarning("CommDevice is null, use serial device");
                    CommDevice = new SerialPortDevice();
                }

                try
                {
                    if (string.IsNullOrWhiteSpace(deviceName))
                    {
                        foreach (var serialPort in SerialPortService.GetAvailableSerialPort().Values)
                        {
                            //指定STM32驱动会导致Win10,Win11自带驱动无法连接
#if False
                            if (string.Compare(serialPort.Description, PortDescription,
                                    StringComparison.OrdinalIgnoreCase) == 0)
                            {
                                try
                                {
                                    deviceName = serialPort.PortName;//190902lilin add
                                    CommDevice.Open(deviceName);
                                    break;
                                }
                                catch
                                {
                                    continue;
                                }
                            }
#else
                            try
                            {
                                deviceName = serialPort.PortName;//190902lilin add
                                CommDevice.Open(deviceName);
                                break;
                            }
                            catch
                            {
                                continue;
                            }
#endif
                        }

                        if (!CommDevice.IsOpen)
                        {
                            return false;
                        }
                    }
                    else
                    {
                        CommDevice.Open(deviceName);
                    }
                }
                catch (IOException)
                {
                    Trace.TraceError("Open device failed");
                    return false;
                }

                Initialize();

                return true;
            }


        }

        private void Initialize()
        {
            //            _receivedEvent.Reset();
            _stopEvent.Reset();
            _parseThread = new Thread(TimeoutCheckFunc);
            _parseThread.Start();

            CommDevice.BeginReceiveAsync(OnCommDataReceived);

            EnablePassThrough(_passthroughEnabled);

            if (_eventDispacherThread != null)
            {
                throw new Exception("期待_eventDispatcherThread为空");
            }
            //_eventQueue.Clear();
            //_queueSemaphore = new Semaphore(0, 100);
            _eventDispacherThread = new Thread(DispatcherThreadFunc);
            _eventDispacherThread.Start();
        }

        /// <summary>
        /// 关闭设备连接
        /// </summary>
        public void Close()
        {
            lock (this)
            {
                if (IsConnected)
                {
                    Disconnect();
                }

                _stopEvent.Set();
                _passthroughEnabled = false;
                CommDevice?.Close();

                if (_eventDispacherThread != null)
                {
#if TRACE
                    Trace.TraceInformation("发送信号通知退出事件分发线程");
#endif
                    lock (_eventQueue)
                    {
                        _eventQueue.Enqueue(new EventQueueItem(EventType.ExitThread, EventArgs.Empty));
                    }

                    _queueSemaphore.Release();
                    _eventDispacherThread.Join();
                    _eventDispacherThread = null;

                    //throw new Exception("_eventQueue should be null");
                    //                    Trace.TraceWarning("_eventQueue should be null");
                    while (_eventQueue.Count > 0)
                    {
                        _queueSemaphore.WaitOne();
                        lock (_eventQueue)
                        {
                            var item = _eventQueue.Dequeue();
                            Trace.TraceWarning("\tleft event: {0}", item.EventType);
                        }
                    }
                }
            }
        }

        public void Disconnect()
        {
            lock (this)
            {
                _disconnectEvent.Reset();
                SendAckMessagePakcet(PacketType.Close);
                _disconnectEvent.WaitOne((int)(ClosingTimeout * 1.5));
            }
        }

        /// <summary>
        /// 建立会话
        /// </summary>
        public void Connect()
        {
            lock (this)
            {
                if (CommDevice.IsOpen)
                {
                    ClearSession();
                    _status = Status.ShakingHands;
                    UpdateTimestamp();
                    CommDevice.Write(ShakeHandsCodes, 0, ShakeHandsCodes.Length);
                }
            }
        }


        public bool Send(byte operation, byte[] parameters, byte[] data)
        {
            lock (this)
            {
                if (parameters != null || data != null)
                {
                    MemoryStream stream = new MemoryStream();
                    if (parameters != null)
                    {
                        stream.Write(parameters, 0, parameters.Length);
                    }

                    if (data != null)
                    {
                        stream.Write(data, 0, data.Length);
                    }

                    stream.Seek(0, SeekOrigin.Begin);
                    return Send(operation, null, stream);
                }

                return Send(operation);
            }
        }

        public bool Send(byte operation, byte[] parameters = null, Stream dataStream = null)
        {
            lock (this)
            {

                if (_status != Status.Ready)
                {
                    var timespan = DateTime.Now - _timestamp;
                    if (_status == Status.Recving && _recvContext.IsCompleted)
                    {
                        SendRecvingAckPacket();
                        _recvContext.Clear();
                        _status = Status.Ready;
                    }
                    else if (_status == Status.Recving && timespan.TotalMilliseconds > RecvingTimeout) // 接收超时可以直接发送
                    {
                        _recvContext.Clear();
                        _status = Status.Ready;
                    }
                    else if (_status == Status.Sending && timespan.TotalMilliseconds > SendingTimeout) // 上一人个发送超时，也可以直接发送
                    {
                        _sendContext.Clear();
                        _status = Status.Ready;
                    }
                }

                if (_status != Status.Ready)
                {
                    Trace.TraceInformation("Expected status is Ready");
                    return false;
                }
#if TRACE
                Trace.TraceInformation("Send opt {0}", operation);
#endif
                OnMessage($"Send opt {operation}");
                _sendContext.Clear();
                _sendContext.Operation = operation;
                _sendContext.DataStream = dataStream;
                _sendContext.Buffer.Add(operation);
                if (parameters != null && parameters.Length > 0)
                {
                    _sendContext.Buffer.AddRange(parameters);
                }

                try
                {
                    _sendContext.Total = parameters?.Length ?? 0;
                    if (dataStream != null)
                    {
                        _sendContext.Total = _sendContext.Total + (int)dataStream.Length;
                    }
                }
                catch (NotSupportedException ex)
                {
                    _sendContext.Total = null;
                }

                _cancelSendingEvent.Reset();
                _status = Status.Sending;
                SendNextPacket();
                return true;
            }
        }

        /// <summary>
        /// 取消正在进行的发送
        /// </summary>
        public void CancelSending()
        {
            _cancelSendingEvent.Set();
        }

        /// <summary>
        /// 取消接收
        /// </summary>
        public void CancelReceiving()
        {
            _cancelReceivingEvent.Set();
        }

        public void PassThrough(string text)
        {
            var data = Encoding.UTF8.GetBytes(text);
            if (data.Length > 0)
            {
                if (CommDevice != null && CommDevice.IsOpen)
                    CommDevice.Write(data, 0, data.Length);
            }
        }

        private void SendNextPacket()
        {
            int maxLength = GetSendingMax();
            List<byte> data = new List<byte>();
            if (_sendContext.Buffer.Count > 0)
            {
                if (_sendContext.Buffer.Count <= maxLength)
                {
                    data.AddRange(_sendContext.Buffer);
                    _sendContext.Buffer.Clear();
                }
                else
                {
                    data.AddRange(_sendContext.Buffer.GetRange(0, maxLength));
                    _sendContext.Buffer.RemoveRange(0, maxLength);
                }
            }

            bool isLastPacket = _sendContext.DataStream == null;
            if (_sendContext.DataStream != null && data.Count < maxLength)
            {
                byte[] buffer = new byte[maxLength - data.Count];
                int n = _sendContext.DataStream.Read(buffer, 0, buffer.Length);
                if (n > 0)
                {
                    data.AddRange(buffer.Where((b, i) => i < n));
                }

                try
                {
                    isLastPacket = _sendContext.DataStream.Position == _sendContext.DataStream.Length;
                }
                catch (NotSupportedException)
                {
                    // do nothing
                    isLastPacket = n < buffer.Length;
                }

                if (isLastPacket)
                {
                    _sendContext.DataStream = null;
                }
            }

            var dataPacket = new DataPacket
            {
                header1 = PacketHeadFlag1,
                header2 = PacketHeadFlag2,
                pkt_type = (byte)PacketType.Data,
                session = _session,
                seq = (int)(isLastPacket ? _sendContext.Seq : _sendContext.Seq | MoreDataPacketFlag),
                len = (ushort)(data.Count + CRC.Size)
            };
            _sendContext.LatestPacket.AddRange(StructConverter.StructToBytes(dataPacket));
            var crc = data.CalculateCrc();
            _sendContext.LatestPacket.AddRange(data);
            _sendContext.LatestPacket.AddRange(BitConverter.GetBytes(crc));
#if TRACE
            Trace.TraceInformation("SEND: opt {0}, seq {1}", _sendContext.Operation, _sendContext.Seq);
            //            Trace.TraceInformation("PACKET CONTENT: {0}", BitConverter.ToString(_sendContext.LatestPacket.ToArray()));
#endif
            OnMessage($"Send:{ToHexString(_sendContext.LatestPacket)}");
            CommDevice.Write(_sendContext.LatestPacket.ToArray(),
                0, _sendContext.LatestPacket.Count);

            _sendContext.Sent += data.Count - ((_sendContext.Sent == 0) ? 1 : 0);

            UpdateTimestamp();
        }

        private static byte[] ToBytes(string str)
        {
            byte[] buffer = new byte[Encoding.UTF8.GetByteCount(str) + 1];
            int n = Encoding.UTF8.GetBytes(str, 0, str.Length, buffer, 0);
            buffer[n] = 0;
            return buffer;
        }

        public void SendFile(string fileName, Stream fileStream)
        {
            var list = new List<byte>();
            try
            {
                list.AddRange(BitConverter.GetBytes((uint)fileStream.Length));
            }
            catch (NotSupportedException)
            {
                list.AddRange(BitConverter.GetBytes(-1));
            }

            list.AddRange(ToBytes(fileName));

            Send((byte)OperationCode.WriteFile, list.ToArray(), fileStream);
        }

        public void SendResource(uint size, Stream dataStream)
        {
            List<byte> buffer = new List<byte>();
            buffer.AddRange(BitConverter.GetBytes(-1));
            buffer.AddRange(BitConverter.GetBytes(size));
            Send((byte)OperationCode.WriteResource, buffer.ToArray(), dataStream);
        }

        public void SendLogo(uint size, Stream dataStream)
        {
            List<byte> buffer = new List<byte>();
            buffer.AddRange(BitConverter.GetBytes(-1));
            buffer.AddRange(BitConverter.GetBytes(size));
            Send((byte)OperationCode.WriteLogo, buffer.ToArray(), dataStream);
        }

        public void ReadFile(string fileName)
        {
            Send((byte)OperationCode.ReadFile, ToBytes(fileName));
        }

        public void WriteFlash(uint address, byte[] data)
        {
            List<byte> buffer = new List<byte>();
            buffer.AddRange(BitConverter.GetBytes(address));
            buffer.AddRange(BitConverter.GetBytes(data.Length));
            Send((byte)OperationCode.WriteFlash, buffer.ToArray(), data);
        }

        public void WriteFlash(uint address, Stream dataStream)
        {
            List<byte> buffer = new List<byte>();
            buffer.AddRange(BitConverter.GetBytes(address));

            try
            {
                buffer.AddRange(BitConverter.GetBytes((uint)dataStream.Length));
            }
            catch (Exception)
            {
                buffer.AddRange(BitConverter.GetBytes((uint)0));
            }

            Send((byte)OperationCode.WriteFlash, buffer.ToArray(), dataStream);
        }

        public void ReadFlash(uint address, uint size)
        {
            List<byte> buffer = new List<byte>();
            buffer.AddRange(BitConverter.GetBytes(address));
            buffer.AddRange(BitConverter.GetBytes(size));
            Send((byte)OperationCode.ReadFlash, buffer.ToArray());
        }

        public void ListFileNames(string dirName, ListFileNamesHandler handler)
        {
            _fileNamesReceivedHandler = handler;
            Send((byte)OperationCode.ListFileNames);
        }

        public void DelFiles(string[] fileNames)
        {
            MemoryStream stream = new MemoryStream();
            foreach (var fileName in fileNames)
            {
                var bytes = ToBytes(fileName);
                bytes[bytes.Length - 1] = (byte)'\n';
                stream.Write(bytes, 0, bytes.Length);
            }

            stream.Seek(0, SeekOrigin.Begin);

            Send((byte)OperationCode.DeleteFile, null, stream);
        }

        #endregion 公共方法

        #region 私有变量

        private Status _status; // 状态
        private byte _session; // 会话ID
        private uint _remoteRecvBufSize; // 远端接收缓冲区大小
        private bool _passthroughEnabled;
        private DateTime _timestamp = DateTime.Now;
        private readonly RecvContext _recvContext = new RecvContext();
        private readonly SendContext _sendContext = new SendContext();
        private readonly List<byte> _passthroughData = new List<byte>();
        private Semaphore _queueSemaphore = new Semaphore(0, 100); //null; // 消息队列信号量
        private Queue<EventQueueItem> _eventQueue = new Queue<EventQueueItem>(100);

        private readonly EventWaitHandle _stopEvent = new EventWaitHandle(false, EventResetMode.ManualReset);
        private readonly EventWaitHandle _disconnectEvent = new EventWaitHandle(false, EventResetMode.ManualReset);
        private readonly EventWaitHandle _cancelSendingEvent = new EventWaitHandle(false, EventResetMode.ManualReset); // 取消收发事件
        private readonly EventWaitHandle _cancelReceivingEvent = new EventWaitHandle(false, EventResetMode.ManualReset);

        private Thread _parseThread; // 数据解析线程
        private Thread _eventDispacherThread; // 消息分发线程

        ListFileNamesHandler _fileNamesReceivedHandler;
        private ICommDevice _commDevice;

        #endregion // 私有变量

        #region 私有函数

        private void EnablePassThrough(bool enabled = true)
        {
            _passthroughEnabled = enabled;
            if (IsOpen)
            {
                MessagePacket packet = new MessagePacket
                {
                    header1 = PacketHeadFlag1,
                    header2 = PacketHeadFlag2,
                    pkt_type = (byte)PacketType.PassThrough,
                    session = (byte)(enabled ? 0xFF : 0)
                };
                byte[] bytes = StructConverter.StructToBytes(packet);
                CommDevice.Write(bytes, 0, bytes.Length);
            }
        }

        bool IsValidPacketType(byte type)
        {
            return type >= (byte)PacketType.ShakeHands && type <= (byte)PacketType.PassThrough;
        }


        private void HandleShakingHands()
        {
            if (_status != Status.ShakingHands)
            {
                Trace.TraceWarning("Received shake hands ack packet, but has never requested\n");
                _passthroughData.AddRange(_parser.ParsedData);
                return;
            }

            ShakeHandsAckPacket packet = StructConverter.BytesToStruct<ShakeHandsAckPacket>(_parser.ParsedData);
            ushort crc = _parser.ParsedData.Take(Marshal.SizeOf(typeof(ShakeHandsAckPacket)) - CRC.Size).CalculateCrc();

            Trace.Assert(packet.crc == crc, "packet.crc == crc in HandleShakingHands()");

            var isOk = packet.session != 0xFF;
            if (isOk)
            {
                _session = packet.session;
                _remoteRecvBufSize = packet.buf_size;
                Version = packet.Version;
                ProductType = packet.ProductType;
                SendAckMessagePakcet(PacketType.ShakeHandsAck2);

                _status = Status.Ready;
            }
            else
            {
                _status = Status.NoSession;
            }

            //Connected?.BeginInvoke(this, new ConnectedEventArgs(isOk), null, null);
            lock (_eventQueue)
            {
                _eventQueue.Enqueue(new EventQueueItem(EventType.Connected, new ConnectedEventArgs(isOk)));
                _queueSemaphore.Release();
            }
        }

        private void SendAckMessagePakcet(PacketType pktType)
        {
            MessagePacket ackPacket = new MessagePacket
            {
                header1 = PacketHeadFlag1,
                header2 = PacketHeadFlag2,
                pkt_type = (byte)pktType,
                session = _session
            };
            byte[] ackBytes = StructConverter.StructToBytes(ackPacket);
            if (CommDevice.IsOpen)
            {
                CommDevice.Write(ackBytes, 0, ackBytes.Length);
            }
        }

        private void HandleCloseRequest()
        {
            SendAckMessagePakcet(PacketType.CloseAck);
            ClearSession();
        }

        private void ClearSession()
        {
            if (_status != Status.NoSession && _status != Status.ShakingHands)
            {
                _disconnectEvent.Set();
                //Disconnected?.BeginInvoke(this, EventArgs.Empty, null, null);
                lock (_eventQueue)
                {
                    _eventQueue.Enqueue(new EventQueueItem(EventType.Disconnected, EventArgs.Empty));
                    _queueSemaphore.Release();
                }
            }

            _session = 0;
            _status = Status.NoSession;
            _remoteRecvBufSize = 0;

            _recvContext.Clear();
            _sendContext.Clear();
            _parser.Clear();
        }

        // 获取一次可发送的最大字节数
        private int GetSendingMax()
        {
            return (int)_remoteRecvBufSize - Marshal.SizeOf(typeof(DataPacket)) - CRC.Size;
        }

        enum RecvAckType
        {
            Normal,
            Retransmission,
            Abort,
        }

        private void HandleRecving()
        {
            // 如果状态不是接收状态，说明
            if (_status != Status.Recving)
            {
                _recvContext.Clear();
                _status = Status.Recving;
            }

            DataPacket dataPacket = StructConverter.BytesToStruct<DataPacket>(_parser.ParsedData.ToArray());

            // 对方中止了数据传输
            if (IsRemoteAbortTransimssion(dataPacket.seq))
            {
                int error = ExtractRemoteErrorCode(dataPacket.seq);
#if TRACE
                Trace.TraceInformation("Data transfer was aborted by remote: {0}", error);
#endif
                HandleRecvError(ErrorCode.AbortedByRemote, error);
                return;
            }

            bool isLast = (dataPacket.seq & MoreDataPacketFlag) == 0;
            int seq = (int)(dataPacket.seq & ~DataSeqFlagsMask);
            if (seq != _recvContext.NextSeq)
            {
                // 接收到非预期数据包，可能是某种原因重传过来的，直接忽略
                Trace.TraceWarning($"Received seq {seq}, expect {_recvContext.NextSeq}");
                return;
            }

            List<byte> list = new List<byte>(_parser.ParsedData);
            var data = list.GetRange(Marshal.SizeOf(typeof(DataPacket)), dataPacket.len - CRC.Size);

            ushort expectedCrc = BitConverter.ToUInt16(
                list.GetRange(Marshal.SizeOf(typeof(DataPacket)) + dataPacket.len - CRC.Size, CRC.Size).ToArray(), 0);
            ushort crc = data.CalculateCrc();

            // 判断用户是否已取消
            bool isCanceled = _cancelReceivingEvent.WaitOne(0);
            if (isCanceled && !isLast)
            {
                SendRecvingAckPacket(RecvAckType.Abort);
            }

            if (isCanceled)
            {
                _recvContext.Clear();
                _status = Status.Ready;
                return;
            }

            if (crc != expectedCrc)
            {
                SendRecvingAckPacket(RecvAckType.Retransmission);
                return;
            }

            int offset = 0;
            lock (_recvContext)
            {
                offset = _recvContext.Received;
                if (seq == 0)
                {
                    _cancelReceivingEvent.Reset();

                    _recvContext.Operation = data[0];
                    data = data.GetRange(1, data.Count - 1);
                }

                _recvContext.IsCompleted = isLast;
                _recvContext.NextSeq = seq + 1;
                _recvContext.Received += data.Count;
            }

            SendRecvingAckPacket();

            HandleReceivedData((OperationCode)_recvContext.Operation, offset, data.ToArray(), isLast);
            if (isLast)
            {
                _recvContext.Clear();
                _status = Status.Ready;
            }
        }

        /// <summary>
        /// 发送接收应答包
        /// </summary>
        /// <param name="ackType">应答类型</param>
        /// <param name="abortReason">如果应答类型是Abort，可以添加中止的原因代码</param>
        private void SendRecvingAckPacket(RecvAckType ackType = RecvAckType.Normal, int abortReason = 0)
        {
            DataAckPacket ackPacket = new DataAckPacket
            {
                header1 = PacketHeadFlag1,
                header2 = PacketHeadFlag2,
                pkt_type = (byte)PacketType.DataAck,
                session = _session,
                seq = _recvContext.NextSeq
            };

            if (ackType == RecvAckType.Retransmission)
            {
                ackPacket.seq = (int)(ackPacket.seq | RetransmissionFlag);
            }
            else if (ackType == RecvAckType.Abort)
            {
                ackPacket.seq = (int)(0xFF000000 | abortReason);
            }

            byte[] bytes = StructConverter.StructToBytes(ackPacket);
            CommDevice.Write(bytes, 0, bytes.Length);
        }

        private bool HandleReceivedData(OperationCode operation, int dataOffset, byte[] data, bool isLast)
        {
            bool ret = true;
            switch (operation)
            {
                case OperationCode.ListFileNames:
                    {
                        List<KeyValuePair<string, int>> list = new List<KeyValuePair<string, int>>();
                        for (int i = 0; i < data.Length;)
                        {
                            int size = BitConverter.ToInt32(data, i);
                            i += 4;
                            int index = Array.IndexOf<byte>(data, 0, i);
                            string name = Encoding.UTF8.GetString(data, i, index - i);
                            i = index + 1;

                            list.Add(new KeyValuePair<string, int>(name, size));
                        }

                        if (_fileNamesReceivedHandler != null)
                        {
                            ret = _fileNamesReceivedHandler(list, isLast, ErrorCode.Successfull);
                            if (isLast)
                            {
                                _fileNamesReceivedHandler = null;
                            }
                        }

                        break;
                    }
                default:
                    {
                        ReceivedEventArgs args = new ReceivedEventArgs(
                            (byte)operation, dataOffset, data, isLast);
                        //Received?.BeginInvoke(this, args, null, null);
                        lock (_eventQueue)
                        {
                            _eventQueue.Enqueue(new EventQueueItem(EventType.Received, args));
                            _queueSemaphore.Release();
                        }
                        ret = !args.IsAbortRecv;
                    }
                    break;
            }

            return ret;
        }

        private void HandleRecvError(ErrorCode errorCode, int remoteError = 0)
        {
            switch ((OperationCode)_recvContext.Operation)
            {
                case OperationCode.ListFileNames:
                    _fileNamesReceivedHandler?.Invoke(null, true, errorCode);
                    break;
                default:
                    //Received?.BeginInvoke(this, new ReceivedEventArgs(
                    //        _recvContext.Operation, _recvContext.Received, null, true, errorCode, remoteError),
                    //    null, null);
                    lock (_eventQueue)
                    {
                        _eventQueue.Enqueue(new EventQueueItem(EventType.Received, new ReceivedEventArgs(
                            _recvContext.Operation, _recvContext.Received, null, true, errorCode, remoteError)));
                        _queueSemaphore.Release();
                    }
                    break;
            }

            _recvContext.Clear();
            _status = Status.Ready;
        }

        private void HandleSendingError(ErrorCode errorCode, int remoteError = 0)
        {
            switch ((OperationCode)_sendContext.Operation)
            {
                case OperationCode.ListFileNames:
                    _fileNamesReceivedHandler?.Invoke(null, true, errorCode);
                    break;
                default:
                    //SendingProgressChanged?.BeginInvoke(this, new SendingProgressChangedEventArgs(
                    //        _sendContext.Operation, _sendContext.Total, _sendContext.Sent, errorCode, remoteError),
                    //    null, null);
                    lock (_eventQueue)
                    {
                        _eventQueue.Enqueue(new EventQueueItem(EventType.SendingProgressChanged, new SendingProgressChangedEventArgs(
                            _sendContext.Operation, _sendContext.Total, _sendContext.Sent, errorCode, remoteError)));
                        _queueSemaphore.Release();
                    }
                    break;
            }

            _sendContext.Clear();
            _status = Status.Ready;
        }

        private void HandleSending()
        {
            bool hasMore = _sendContext.Buffer.Count > 0 || _sendContext.DataStream != null;

            bool isCanceled = _cancelSendingEvent.WaitOne(0);
            if (isCanceled && hasMore)
            {
                // 中止命令
                var bytes = new byte[]
                {
                    0xaa, 0x55, 0xa6, _session, 0x00, 0x00, 0x00, 0xFF, 0x00, 0x00
                };
                CommDevice.Write(bytes, 0, bytes.Length);
                _sendContext.Clear();
                _status = Status.Ready;
                return;
            }

            DataAckPacket paket = StructConverter.BytesToStruct<DataAckPacket>(_parser.ParsedData);
            if (IsRemoteAbortTransimssion(paket.seq))
            {
                int err = ExtractRemoteErrorCode(paket.seq);
#if TRACE
                Trace.TraceInformation("Remote aborted receiving: %d", err);
#endif
                HandleSendingError(ErrorCode.AbortedByRemote, err);
                return;
            }


            if ((paket.seq & RetransmissionFlag) == RetransmissionFlag)
            {
                CommDevice.Write(_sendContext.LatestPacket.ToArray(),
                    0, _sendContext.LatestPacket.Count);
            }
            else
            {
                SendingProgressChangedEventArgs eventArgs = new SendingProgressChangedEventArgs(_sendContext.Operation,
                    _sendContext.Total, _sendContext.Sent);
                //Trace.TraceInformation($"已发送：{_sendContext.Sent}/{_sendContext.Total}");
                //SendingProgressChanged?.BeginInvoke(this, eventArgs, null, null);
                lock (_eventQueue)
                {
                    _eventQueue.Enqueue(new EventQueueItem(EventType.SendingProgressChanged, eventArgs));
                    _queueSemaphore.Release();
                }
                if (hasMore)
                {
                    _sendContext.Seq += 1;
                    _sendContext.LatestPacket.Clear();
                    SendNextPacket();
                }
                else
                {
                    _status = Status.Ready;
                    _sendContext.Clear();
                }

                UpdateTimestamp();
            }
        }

        private void HandleKeepActivity()
        {
            SendAckMessagePakcet(PacketType.KeepActivityAck);
        }

        private void DispatcherThreadFunc()
        {
            while (_queueSemaphore.WaitOne())
            {
                EventQueueItem item = null;
                lock (_eventQueue)
                {
                    item = _eventQueue.Dequeue();
                }
#if TRACE
                Trace.TraceInformation($"准处分发{item.EventType}事件");
#endif
                switch (item.EventType)
                {
                    case EventType.Connected:
                        Connected?.Invoke(this, item.Args as ConnectedEventArgs);
                        break;
                    case EventType.Disconnected:
                        Disconnected?.Invoke(this, item.Args);
                        break;
                    case EventType.Received:
                        Received?.Invoke(this, item.Args as ReceivedEventArgs);
                        break;
                    case EventType.SendingProgressChanged:
                        SendingProgressChanged?.Invoke(this, item.Args as SendingProgressChangedEventArgs);
                        break;
                    case EventType.PassThroughDataReceived:
                        PassThroughDataReceived?.Invoke(this, item.Args as PassThroughReceivedEventArgs);
                        break;
                    case EventType.ReceiveTimeout:
                        HandleRecvTimeout();
                        break;
                    case EventType.SendingTimeout:
                        HandleSendTimeout();
                        break;
                    case EventType.ExitThread:
                        return;
                }
            }
        }

        private void DispatchParseResult()
        {
            switch (_parser.PacketType)
            {
                case PacketType.ShakeHandsAck:
                    HandleShakingHands();
                    break;
                case PacketType.Close:
                    HandleCloseRequest();
                    break;
                case PacketType.CloseAck:
                    ClearSession();
                    break;
                case PacketType.Data:
                    HandleRecving();
                    break;
                case PacketType.DataAck:
                    HandleSending();
                    break;
                case PacketType.KeepActivity:
                    HandleKeepActivity();
                    break;
                default:
                    Trace.TraceWarning("Unknown packet type 0x{0:X2}", (byte)_parser.PacketType);
                    break;
            }
        }

        private void HandlePassthroughData()
        {
            if (IsPassthroughEnabled)
            {
                PassThroughDataReceived?.Invoke(this, new PassThroughReceivedEventArgs(_passthroughData.ToArray()));
            }

            _passthroughData.Clear();
        }

        private void UpdateTimestamp()
        {
            _timestamp = DateTime.Now;
        }

        private PacketParser _parser = new PacketParser();

        private void OnCommDataReceived(object sender, byte[] bytesReceived, Exception ex)
        {
            if (bytesReceived != null)
            {
                Trace.TraceInformation($"Received {bytesReceived.Length} bytes");
                List<byte> list = new List<byte>(bytesReceived);
                OnMessage($"Receive:{ToHexString(bytesReceived)}");
                while (list.Count > 0)
                {
                    int n = _parser.Parse(list);
                    list.RemoveRange(0, n);

                    lock (_passthroughData)
                    {
                        _passthroughData.AddRange(_parser.InvalidData);
                    }

                    if (_parser.IsComplete)
                    {
                        Trace.TraceInformation($"Received a '{_parser.PacketType}' packet");
                        DispatchParseResult();
                    }
                }

                lock (_passthroughData)
                {
                    if (_passthroughData.Count > 0)
                    {
                        //PassThroughDataReceived?.BeginInvoke(
                        //    this, new PassThroughReceivedEventArgs(_passthroughData.ToArray()), null, null);
                        lock (_eventQueue)
                        {
                            _eventQueue.Enqueue(new EventQueueItem(EventType.PassThroughDataReceived,
                                new PassThroughReceivedEventArgs(_passthroughData.ToArray())));
                            _queueSemaphore.Release();
                        }
                        _passthroughData.Clear();
                    }
                }
            }
        }

        private void TimeoutCheckFunc()
        {
            while (true)
            {
                //                bool hasSginal = _receivedEvent.WaitOne(200);
                if (!IsOpen || _stopEvent.WaitOne(200))
                {
                    _eventQueue.Enqueue(new EventQueueItem(EventType.ExitThread, EventArgs.Empty));
                    _queueSemaphore.Release();
                    break;
                }

                var timeSpan = DateTime.Now - _timestamp;
                if (_status == Status.ShakingHands && timeSpan.TotalMilliseconds > ShakeHandsTimeout)
                {
                    ClearSession();
                    //Connected?.BeginInvoke(this, new ConnectedEventArgs(false), null, null);
                    lock (_eventQueue)
                    {
                        _eventQueue.Enqueue(new EventQueueItem(EventType.Connected, new ConnectedEventArgs(false)));
                        _queueSemaphore.Release();
                    }
                }

                //if (_status == Status.Sending && timeSpan.TotalMilliseconds > SendingTimeout)
                //{
                //    lock (_eventQueue)
                //    {
                //        _eventQueue.Enqueue(new EventQueueItem(EventType.SendingTimeout, EventArgs.Empty));
                //        _queueSemaphore.Release();
                //    }
                //}

                //if (_status == Status.Recving && timeSpan.TotalMilliseconds > RecvingTimeout)
                //{
                //    lock (_eventQueue)
                //    {
                //        _eventQueue.Enqueue(new EventQueueItem(EventType.ReceiveTimeout, EventArgs.Empty));
                //        _queueSemaphore.Release();
                //    }
                //}

                if (_status == Status.Closing && timeSpan.TotalMilliseconds > ClosingTimeout)
                {
                    ClearSession();
                }
            }
        }

        private void HandleRecvTimeout()
        {
#if TRACE
            Trace.TraceInformation("Recving timeout");
#endif
            HandleRecvError(ErrorCode.Timeout);
        }

        private void HandleSendTimeout()
        {
            Trace.TraceWarning("Sending timeout");
            if (_sendContext.Retransmission == 0)
            {
#if TRACE
                Trace.TraceInformation("Retransmission");
#endif
                CommDevice.Write(_sendContext.LatestPacket.ToArray(), 0, _sendContext.LatestPacket.Count);
                _sendContext.Retransmission = 1;
                UpdateTimestamp();
            }
            else
            {
                HandleSendingError(ErrorCode.Timeout);
            }
        }

        public void Dispose()
        {
            Close();

        }

        #endregion 私有函数

        // 根据应答码判断对方是否终止了接收
        static bool IsRemoteAbortTransimssion(int seq)
        {
            return (seq & 0xFF000000) == 0xFF000000;
        }

        // 获取远端返回的错误码，仅在远端终止了传输时有用
        static int ExtractRemoteErrorCode(int seq)
        {
            return seq & 0x00FFFFFF;
        }

        private string ToHexString(IEnumerable<byte> array)
        {
            var buffer = new StringBuilder();
            foreach (var b in array)
            {
                buffer.Append($"{b.ToString("X2")} ");
            }
            return buffer.ToString();
        }

        #region 内部类型及常量


        class RecvContext
        {
            internal byte Operation = (byte)OperationCode.InvalidOperation; // 操收到的操作码
            internal bool IsCompleted; // 指示是否已接收到所有数据
            internal int Received; // 已接收到的字节数

            internal int NextSeq; // 期待接收的下一个包的序号
                                  //            internal readonly List<byte> Data = new List<byte>(); // 已接收的数据

            //            internal int Timeout = RecvingTimeoutDefault;

            internal void Clear()
            {
                Operation = (byte)OperationCode.InvalidOperation;
                Received = 0;
                NextSeq = 0;
                IsCompleted = false;
                //                Timeout = RecvingTimeoutDefault;
                //                Data.Clear();
            }
        }

        class SendContext
        {
            internal byte Operation = (byte)OperationCode.InvalidOperation; // 发送的操作码
            internal readonly List<byte> Buffer = new List<byte>(); // 操作码的参数
            internal Stream DataStream; // 附加的数据流
            internal readonly List<byte> LatestPacket = new List<byte>(); // 最近一次发送的数据包（用于重发）
            internal int Sent; // 已发送的字节数
            internal int Seq; // 上一次发送的包的序号
            internal int Retransmission; // 已重发的次数
            internal int? Total; // 总共要发送的字节数
                                 //            internal int Timeout = SendingTimeoutDefault; // 超时设置

            internal void Clear()
            {
                Operation = (byte)OperationCode.InvalidOperation;
                Buffer.Clear();
                DataStream = null;
                LatestPacket.Clear();
                Sent = 0;
                Seq = 0;
                Retransmission = 0;
                //                Timeout = RecvingTimeoutDefault;
            }
        }


        enum EventType
        {
            ExitThread,      // 关闭线程
            Connected,
            Disconnected,
            Received,
            SendingProgressChanged,
            PassThroughDataReceived,
            ReceiveTimeout, // 接收超时
            SendingTimeout, // 发送超时
        }

        class EventQueueItem
        {
            public EventQueueItem(EventType eventType, EventArgs args)
            {
                EventType = eventType;
                Args = args;
            }
            // 事件类型
            public EventType EventType { get; set; }
            // 事件参数
            public EventArgs Args { get; set; }
        }


        private static readonly byte[] ShakeHandsCodes =
        {
            0x55, 0xaa, 0xa1, 0x00, 0x00, 0x00, 0x02, 0x00, 0x02
        };

        private const int ShakeHandsTimeout = 30000; // 会话握手超时设置 30s

        private const int RecvingTimeout = 1000; // 接收数据超时限制 1s
        private const int SendingTimeout = 1000; // 发送超时限制 1s
        private const int ClosingTimeout = 1000; // 关闭会话超时限制 1s

        private const uint MoreDataPacketFlag = 0x40000000; // 更多数据包标志位
        private const uint RetransmissionFlag = 0x80000000; // 请求重传标志掩码
        private const uint DataSeqFlagsMask = 0xC0000000; // 数据序号标志位掩码

        private const byte PacketHeadFlag1 = 0x55;
        private const byte PacketHeadFlag2 = 0xAA;

        #endregion // 内部类型
    }
}