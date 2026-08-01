using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using ThreeNH.CommDevice;
using System.Diagnostics;
using System.IO.Ports;
using ThreeNH.SQCX.Infrastructure.Instruments.Spectrophotometer.ProtocolContract;

namespace ThreeNH.SQCX.Infrastructure.Instruments.Spectrophotometer
{
    public class JsonCommProtocol : IJsonCommProtocol
    {


        #region Private Properties

        ICommDevice _commDevice;
        EventWaitHandle _ackEvent = new EventWaitHandle(false, EventResetMode.ManualReset);
        List<byte> _recvBuffer = new List<byte>(); // 接受缓存
        string _ack; // 接受到的应答

        private bool disposedValue;

        #endregion

        #region Events

        /// <summary>
        /// 接收到仪器通知命令
        /// </summary>
      //  public EventHandler<NotificationEventArgs> NotificationReceived;
        public event EventHandler<NotificationEventArgs> NotificationReceived;
        #endregion

        #region Public Properties


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
                if (_commDevice?.IsOpen == true)
                {
                    _commDevice.BeginReceiveAsync(receiveCallback);
                }
            }
        }

        /// <summary>
        /// 设备是否已打开
        /// </summary>
        public bool IsOpen => CommDevice != null && CommDevice.IsOpen;


        #endregion

        public JsonCommProtocol()
        {

        }

        public JsonCommProtocol(ICommDevice device)
        {
            _commDevice = device;
            if (_commDevice.IsOpen)
            {
                _commDevice.BeginReceiveAsync(receiveCallback);
            }
        }

        public void Open(string deviceName = null)
        {
            if (IsOpen)
            {
                throw new InvalidOperationException("The device has been open.");
            }

            if (_commDevice == null)
            {
                _commDevice = new SerialPortDevice();
            }

            _commDevice.Open(deviceName);
            _commDevice.BeginReceiveAsync(receiveCallback);
        }

        public void Close()
        {
            if (_commDevice != null && _commDevice.IsOpen)
            {
                _commDevice.EndReceiveAsync();
                _commDevice.Close();
                _ack = null;
                _ackEvent.Set();
            }
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

        private void receiveCallback(object sender, byte[] bytesReceived, Exception ex)
        {
            if (bytesReceived != null)
            {
                Trace.TraceInformation("RECV:{0}", Encoding.UTF8.GetString(bytesReceived));
                _recvBuffer.AddRange(bytesReceived);
                if (bytesReceived.Last() == '\n')
                {
                    try
                    {
                        string line = Encoding.UTF8.GetString(_recvBuffer.ToArray()).Trim();
                        _recvBuffer.Clear();

                        if (string.IsNullOrEmpty(line))
                        {
                            return;
                        }

                        if (line.StartsWith("OK") || line.StartsWith("ERR"))
                        {
                            _ack = line;
                            _ackEvent.Set();
                        }
                        else if (line.StartsWith("EVT ") && (NotificationReceived != null)) // 用户通知
                        {
                            NotificationReceived?.Invoke(this, new NotificationEventArgs(line.Substring(4).Trim()));
                        }
                        //仪器主动断开
                        else if (line.StartsWith("+DIS"))
                        {
                            NotificationReceived?.Invoke(this, new NotificationEventArgs(line.Trim()));
                        }
                    }
                    catch (Exception e)
                    {
                        Trace.TraceError("接受到非字符串: {0}", e.Message);
                    }
                }
            }
        }

        /// <summary>
        /// 发送命令
        /// </summary>
        /// <param name="cmd">命令字符串，不能包含空白字符</param>
        /// <param name="param">命令参数，通常为json字符串，该参数不能包含换行符；如果没有参数可以为空</param>
        /// <param name="timeout">超时设置，单位为毫秒</param>
        /// <exception cref="InvalidOperationException" >未连接任何设备时</exception>
        /// <exception cref="OperationCanceledException">仪器断开连接</exception>
        /// <exception cref="ArgumentNullException" >当<paramref name="cmd"/>为<c>null</c>时</exception>
        /// <exception cref="ArgumentException" >当命令包含空格或参数包含换行时</exception>
        /// <exception cref="TimeoutException">接收超时</exception>
        public CommResult send(string cmd, string param = null, int timeout = 5000)
        {
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

            string s;
            if (param != null)
            {
                s = cmd + ' ' + param + '\n';
            }
            else
            {
                s = cmd + '\n';
            }

            Trace.TraceInformation("SEND:{0}", s);

            _ackEvent.Reset();

            var bytes = Encoding.UTF8.GetBytes(s);
            _commDevice.Write(bytes, 0, bytes.Length);

            if (_ackEvent.WaitOne(timeout))
            {
                if (_ack == null && !IsOpen) // 连接断开了
                {
                    throw new OperationCanceledException("The device has been disconnected.");
                }
                return new CommResult(_ack);
            }
            else
            {
                throw new TimeoutException();
            }
        }

        public Task<CommResult> sendAsync(string cmd, string param = null, int timeout = 5000)
        {
            return Task<CommResult>.Factory.StartNew(() => send(cmd, param, timeout));
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: 释放托管状态(托管对象)
                    _ackEvent.Dispose();
                }

                // TODO: 释放未托管的资源(未托管的对象)并重写终结器
                // TODO: 将大型字段设置为 null
                _commDevice.EndReceiveAsync();
                _commDevice = null;
                disposedValue = true;
            }
        }

        // TODO: 仅当“Dispose(bool disposing)”拥有用于释放未托管资源的代码时才替代终结器
        ~JsonCommProtocol()
        {
            // 不要更改此代码。请将清理代码放入“Dispose(bool disposing)”方法中
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            // 不要更改此代码。请将清理代码放入“Dispose(bool disposing)”方法中
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
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
                return false; ;
            }
            finally
            {
                serialPort.ReadTimeout = readTimeout;
            }
        }
    }
}
