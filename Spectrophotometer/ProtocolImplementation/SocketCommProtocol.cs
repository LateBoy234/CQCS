using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ThreeNH.CommDevice;
using ThreeNH.Spectrophotometer.ProtocolContract;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.ProgressBar;

namespace ThreeNH.Spectrophotometer.ProtocolImplementation
{
    public class SocketCommProtocol : IJsonCommProtocol
    {
        #region Private

        #region Properties
        ICommDevice _commDevice;

        AutoResetEvent _ackEvent = new AutoResetEvent(false);

        string _ack; // 接受到的应答
        private bool _awaitingAck;                 // 是否正在等待 OK/ERR
        private readonly object _ackLock = new object();
        private readonly object _recvLock = new object();
        private readonly object _sendLock = new object();

        private readonly List<byte> _frameBuf = new List<byte>();   // 原始字节缓存
        private int _need = 8;                           // 当前阶段还需要多少字节
        private bool _gotHeader;                         // 是否已解析完 8 字节头
        private DateTime _lastCommandTime = DateTime.MinValue;  // 上次命令完成时间
        private int _minCommandIntervalMs = 2000;    // 命令之间最小间隔（毫秒）

        /// <summary>
        /// 设置命令之间的最小间隔时间（毫秒）
        /// </summary>
        /// <param name="intervalMs">最小间隔时间（毫秒）</param>
        public void SetCommandInterval(int intervalMs)
        {
            _minCommandIntervalMs = Math.Max(0, intervalMs); // 确保非负
        }

        /// <summary>
        /// 获取或设置命令之间的最小间隔时间（毫秒）
        /// </summary>
        public int CommandInterval
        {
            get { return _minCommandIntervalMs; }
            set { SetCommandInterval(value); }
        }



        /// <summary>
        /// 重置命令时间记录（通常在连接重置时调用）
        /// </summary>
        public void ResetCommandTimer()
        {
            _lastCommandTime = DateTime.MinValue;
        }


        #endregion

        #region Function
        private void ReceiveCallback(object sender, byte[] bytesReceived, Exception ex)
        {
            if (bytesReceived == null)
            {
                Trace.TraceWarning("ReceiveCallback: bytesReceived is null");
                return;
            }

            Trace.TraceInformation("ReceiveCallback: Received {0} bytes", bytesReceived.Length);

            lock (_recvLock)
            {
                _frameBuf.AddRange(bytesReceived);
                Trace.TraceInformation("FrameBuf size: {0}, Need: {1}, GotHeader: {2}", _frameBuf.Count, _need, _gotHeader);

                while (_frameBuf.Count >= _need)
                {
                    if (!_gotHeader)                // 阶段1：收 8 字节头
                    {
                        // 防止缓冲区过大，可能存在脏数据
                        if (_frameBuf.Count > 65536) // 64KB 安全阈值
                        {
                            Trace.TraceWarning("Frame buffer overflow, clearing. Size: {0}", _frameBuf.Count);
                            _frameBuf.Clear();
                            _gotHeader = false;
                            _need = 8;
                            break;
                        }

                        byte[] head = _frameBuf.GetRange(0, 8).ToArray();

                        uint magic = (uint)(head[0] << 24 | head[1] << 16 | head[2] << 8 | head[3]);
                        uint payloadLen = (uint)(head[4] << 24 | head[5] << 16 | head[6] << 8 | head[7]);

                        if (magic != 1)
                        {
                            // Magic不匹配，尝试在后续字节中寻找有效帧头
                            Trace.TraceWarning("Invalid magic: {0}, searching for valid frame", magic);
                            _frameBuf.RemoveAt(0); // 移除第一个字节，继续搜索
                            continue;
                        }

                        // 验证payload长度合理性
                        if (payloadLen > 65536) // 64KB 最大负载
                        {
                            Trace.TraceWarning("Invalid payload length: {0}, clearing buffer", payloadLen);
                            _frameBuf.Clear();
                            _gotHeader = false;
                            _need = 8;
                            break;
                        }

                        _frameBuf.RemoveRange(0, 8);
                        _need = (int)payloadLen;
                        _gotHeader = true;

                        if (_need == 0) // 空payload的情况
                        {
                            _gotHeader = false;
                            _need = 8;
                        }
                        continue;
                    }

                    // 阶段2：收够负载
                    var payloadBytes = _frameBuf.GetRange(0, _need).ToArray();
                    _frameBuf.RemoveRange(0, _need);

                    string line = Encoding.UTF8.GetString(payloadBytes).TrimEnd('\r', '\n');
                    Trace.TraceInformation("RECV:{0}", line);

                    // 现在 line 已经是纯文本，可以按老逻辑判
                    if (line.StartsWith("OK") || line.StartsWith("ERR"))
                    {
                        bool shouldSignal = false;
                        lock (_ackLock)
                        {
                            if (_awaitingAck)
                            {
                                _ack = line;
                                _awaitingAck = false; // 一个请求只消费一个 OK/ERR
                                shouldSignal = true;
                                Trace.TraceInformation("ACK matched: {0}", line);
                            }
                            else
                            {
                                Trace.TraceWarning("ACK received but not awaiting: {0}", line);
                            }
                        }

                        // 不在等待 ack 的情况下收到 OK/ERR，通常是"上一次超时/重入"的迟到包，忽略掉避免污染下一次
                        if (shouldSignal)
                        {
                            _ackEvent.Set();
                        }
                        else
                        {
                            Trace.TraceWarning("RECV(ignored stale ack):{0}", line);
                        }
                    }
                    else if (line.StartsWith("EVT "))
                    {
                        NotificationReceived?.Invoke(this,
                            new NotificationEventArgs(line.Substring(4).Trim()));
                    }
                    else if (line == "+DIS")
                    {
                        NotificationReceived?.Invoke(this,
                            new NotificationEventArgs(line));
                    }

                    // 复位，准备下一帧
                    _gotHeader = false;
                    _need = 8;
                }
            }
        }
        #endregion

        #endregion



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

            }
        }

        public bool IsOpen => CommDevice != null && CommDevice.IsOpen;

        public event EventHandler<NotificationEventArgs> NotificationReceived;

        public void Close()
        {
            if (_commDevice != null)
            {
                _commDevice?.EndReceiveAsync();
                _commDevice?.Close();

                // 清理所有状态
                lock (_ackLock)
                {
                    _ack = null;
                    _awaitingAck = false;
                }

                lock (_recvLock)
                {
                    _frameBuf.Clear();
                    _gotHeader = false;
                    _need = 8;
                }

                // 重置命令计时器
                ResetCommandTimer();

                _ackEvent.Set(); // 唤醒可能在等待的线程
            }
        }

        public void Dispose()
        {
            Close();
        }
        private NetworkConnectivityMonitor monitor;
        public void Open(string deviceName = null)
        {
            if (IsOpen)
            {
                throw new InvalidOperationException("The device has been open.");
            }

            // 确保打开前状态干净
            lock (_recvLock)
            {
                _frameBuf.Clear();
                _gotHeader = false;
                _need = 8;
            }

            lock (_ackLock)
            {
                _ack = null;
                _awaitingAck = false;
            }

            // 重置命令计时器
            ResetCommandTimer();

            string device = deviceName as string;
            if (string.IsNullOrWhiteSpace(device))
                throw new ArgumentException("设备名称不能为空", nameof(device));

            string[] parts = device.Split(':');
            if (parts.Length != 2 || !int.TryParse(parts[1], out int port))
                throw new ArgumentException("设备名称格式错误，应为 IP:Port", nameof(device));
            if (CommDevice == null)
            {
                CommDevice = new SocketDeviceCommunicator(parts[0], port);
            }

            // 打开连接
            _commDevice.Open(device);
            if (_commDevice?.IsOpen == true)
            {
                _commDevice.BeginReceiveAsync(ReceiveCallback);
                monitor = new NetworkConnectivityMonitor();
                // 异步启动主站模式（不阻塞 UI）
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await monitor.StartMonitoTCP(parts[0], port);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[SocketCommProtocol] Failed to start MasterMode: {ex.Message}");
                    }
                });
            }
        }
        // protocol.send("MEA", "std", 10000);
        public CommResult send(string cmd, string param = null, int timeout = 5000)
        {
            if (cmd != "MEA")
            {
                CommandInterval = 0;
            }
            else
            {
                CommandInterval = 2000;
            }
            if (_commDevice == null || !_commDevice.IsOpen)
            {
                throw new InvalidOperationException("The device is not open");
            }
            if (cmd == null)
            {
                throw new ArgumentNullException(nameof(cmd));
            }
            if (param != null && param.Contains('\n'))
            {
                throw new ArgumentException(nameof(param));
            }

            string s = param != null ? $"{cmd} {param}\n" : $"{cmd}\n";

            // 同一时刻只允许一个请求等待 OK/ERR，避免连续测量时出现 ack 串台/被 Reset 掉导致超时
            lock (_sendLock)
            {
                Trace.TraceInformation(">>> SEND START: {0}", cmd);

                // 确保与上次命令有足够的时间间隔，避免设备端还没准备好
                var elapsed = (DateTime.Now - _lastCommandTime).TotalMilliseconds;
                if (elapsed < _minCommandIntervalMs && _lastCommandTime != DateTime.MinValue)
                {
                    var waitTime = (int)(_minCommandIntervalMs - elapsed);
                    Trace.TraceInformation("Throttling: waiting {0}ms before sending (elapsed: {1}ms)", waitTime, elapsed);
                    Thread.Sleep(waitTime);
                }

                // 清理上一次的状态
                lock (_ackLock)
                {
                    _ack = null;
                    _awaitingAck = true;
                }

                var bytes = Encoding.UTF8.GetBytes(s);
                var packet = new byte[8 + bytes.Length];

                BinaryPrimitives.WriteUInt32BigEndian(packet.AsSpan(0, 4), 1);
                BinaryPrimitives.WriteUInt32BigEndian(packet.AsSpan(4, 4), (uint)bytes.Length);
                bytes.CopyTo(packet, 8);

                Trace.TraceInformation("SEND:{0}, PacketSize:{1}", s.TrimEnd('\n'), packet.Length);
                string hex = BitConverter.ToString(packet).Replace('-', ' ');
                try
                {
                    _commDevice.Write(packet, 0, packet.Length);   // 一次发完
                    Trace.TraceInformation("Write completed, waiting for response...");
                }
                catch (Exception ex)
                {
                    lock (_ackLock)
                    {
                        _awaitingAck = false;
                        _ack = null;
                    }
                    Trace.TraceError("Send failed: {0}", ex.Message);
                    throw;
                }

                // 等待响应
                Stopwatch sw = Stopwatch.StartNew();
                bool received = _ackEvent.WaitOne(timeout);
                sw.Stop();

                Trace.TraceInformation("WaitOne returned: {0}, Elapsed: {1}ms", received, sw.ElapsedMilliseconds);

                string ackSnapshot;
                lock (_ackLock)
                {
                    ackSnapshot = _ack;
                    _awaitingAck = false; // 无论成功失败都要清理等待状态
                    Trace.TraceInformation("After wait - ackSnapshot: {0}, _awaitingAck set to false", ackSnapshot ?? "NULL");
                }

                if (!received)
                {
                    // 超时情况：清理状态，记录日志
                    Trace.TraceWarning("Timeout waiting for response to: {0}, timeout: {1}ms", s.TrimEnd('\n'), timeout);

                    // 清理可能的脏数据，为下次请求做准备
                    lock (_recvLock)
                    {
                        // 如果缓冲区中有未处理的数据，记录警告但不清空（可能是正在接收的有效数据）
                        if (_frameBuf.Count > 0)
                        {
                            Trace.TraceWarning("Buffer not empty on timeout. Size: {0}, GotHeader: {1}, Need: {2}",
                                _frameBuf.Count, _gotHeader, _need);
                        }
                    }

                    // 即使超时也要更新时间戳，避免后续命令立即发送
                    _lastCommandTime = DateTime.Now;

                    throw new TimeoutException($"Timeout waiting for response to command: {cmd}");
                }

                if (ackSnapshot == null && !IsOpen) // 连接断开了
                {
                    throw new OperationCanceledException("The device has been disconnected.");
                }

                if (ackSnapshot == null)
                {
                    Trace.TraceWarning("Received signal but ack is null for command: {0}", cmd);
                    throw new InvalidOperationException("Received invalid response");
                }

                // 更新最后命令时间
                _lastCommandTime = DateTime.Now;

                return new CommResult(ackSnapshot);
            }
        }
    }
}
