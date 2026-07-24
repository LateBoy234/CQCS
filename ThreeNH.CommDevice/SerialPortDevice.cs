using System;
using System.IO;
using System.IO.Ports;
using ThreeNH.PortService;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Text;
using System.Threading.Tasks;

namespace ThreeNH.CommDevice
{
    /// <summary>
    /// 串口设备
    /// </summary>
    public class SerialPortDevice : ICommDevice
    {
        // 串口设置
        public static readonly string[] PortDescriptions =
           {
                "SCI USB2Serial",
                "SPRD U2S Diag",
                "STMicroelectronics Virtual COM Port",
                "USB-SERIAL CH340",//Added by Liguanghai,compatible YS1010 
                "USB 串行设备" // Add by zhoutengda, compatible BTS75
            };
        public int BaudRate { get; set; } = 115200;
        private const Parity _Parity = Parity.None;
        private const StopBits _StopBits = StopBits.One;
        private const int _DataBits = 8;
        //        private const Handshake _Handshake = Handshake.None; // 流控制
        private const int _ReadTimeout = 1000;
        private const int _WriteTimeout = 500;

        private SerialPort _serialPort;  // 通讯串口
        private ReceiveAsyncCallback _receiveCallback;
        //        private Mutex _lock = new Mutex();
        //        private object _recvFuncLock = new object();   
        private AutoResetEvent _ackEvent = new AutoResetEvent(false);
        private AutoResetEvent _recvEvent = new AutoResetEvent(false);
        private Mutex _mutex = new Mutex(false);

        public SerialPortDevice()
        {

        }

        public SerialPortDevice(SerialPort serialPort)
        {
            _serialPort = serialPort;
            _serialPort.ReadTimeout = _ReadTimeout;
            _serialPort.WriteTimeout = _WriteTimeout;
            _serialPort.DataReceived += OnDataReceived;
        }

        void InitSerialPort(int baudRate, Parity parity, int dataBits, StopBits stopBits)
        {

            Trace.TraceInformation("初始化串口");
            _serialPort = new SerialPort();
            _serialPort.BaudRate = baudRate;
            _serialPort.Parity = parity;
            _serialPort.StopBits = stopBits;
            _serialPort.DataBits = dataBits;
            _serialPort.Handshake = Handshake.None;
            _serialPort.ReadBufferSize = 1024 * 1024; // 1M
            _serialPort.ReadTimeout = _ReadTimeout;
            _serialPort.WriteTimeout = _WriteTimeout;
            _serialPort.DataReceived += OnDataReceived;
            _serialPort.RtsEnable = true;
        }

        void InitSerialPort()
        {
            Trace.TraceInformation("初始化串口");
            _serialPort = new SerialPort();
            _serialPort.BaudRate = BaudRate;
            _serialPort.Parity = _Parity;
            _serialPort.StopBits = _StopBits;
            _serialPort.DataBits = _DataBits;
            _serialPort.Handshake = Handshake.None;
            _serialPort.ReadBufferSize = 1024 * 1024; // 1M
            _serialPort.ReadTimeout = _ReadTimeout;
            _serialPort.WriteTimeout = _WriteTimeout;
            //_serialPort.DataReceived += OnDataReceived;
        }

        public string DeviceName
        {
            get
            {
                return IsOpen ? _serialPort.PortName : null;
            }
        }

        public bool IsOpen
        {
            get
            {
                return _serialPort != null && _serialPort.IsOpen;
            }
        }

        public Stream Stream
        {
            get
            {
                return _serialPort.BaseStream;
            }
        }

        public string DriverDescription { get; set; }

        public bool IsBlueSerialPortDevice => string.Compare(DriverDescription, "USB-SERIAL CH340") == 0;

        public void Close()
        {
            lock (this)
            {
                if (_serialPort == null)
                    return;

                try
                {
                    EndReceiveAsync();
                    _serialPort.DataReceived -= OnDataReceived;
                    Trace.TraceInformation("[{0}] 正在关闭串口", this);
                    if (_serialPort.IsOpen)
                        _serialPort.Close();
                }
                catch (Exception ex)
                {
                    Trace.TraceWarning("[{0}] 关闭串口失败： {1}\n{2}", this, ex.Message, ex.StackTrace);
                }
                finally
                {
                    _serialPort = null;
                    _receiveCallback = null;
                }
            }
        }

        public void Open(string portName, int baudRate, Parity parity, int dataBits, StopBits stopBits)
        {
            lock (this)
            {
                try
                {
                    InitSerialPort(baudRate, parity, dataBits, stopBits);
                    _serialPort.PortName = portName;

                    _serialPort.Open();
                    Trace.TraceInformation("[{0}] 打开串口成功", this);
                }
                catch (Exception ex)
                {
                    Trace.TraceInformation("[{0}] 打开串口失败：{1}\n{2}", this, ex.Message, ex.StackTrace);
                    //throw new CommunicationException(CommunicationError.DeviceOpenFailed, ex.Message);
                    throw new System.IO.FileLoadException();
                }
                _serialPort.DiscardInBuffer(); //  丢弃所有数据，否则在关闭后马上打开可能收到上次打开时对方发送的数据
            }
        }

        public void Open(object device)
        {
            lock (this)
            {
                string deviceName = device as string;
                if (device == null)
                {
                    foreach (var info in SerialPortService.SerialPorts)
                    {
                        if (PortDescriptions.Contains(info.Description))
                        {
                            deviceName = info.PortName;
                            DriverDescription = info.Description;
                            break;
                        }
                    }
                    if (deviceName == null)
                        throw new System.IO.FileNotFoundException();
                }
                else
                {
                    foreach (var info in SerialPortService.SerialPorts)
                    {
                        if (string.Compare(deviceName, info.PortName) == 0)
                        {
                            DriverDescription = info.Description;
                            break;
                        }
                    }
                }

                try
                {
                    Trace.TraceInformation("[{0}] 准备打开串口 {1}", this, device);
                    InitSerialPort();
                    _serialPort.PortName = deviceName;
                    //Thread.Sleep(3000);
                    _serialPort.Open();
                    _serialPort.DataReceived += OnDataReceived;
                    Trace.TraceInformation("[{0}] 打开串口成功", this);
                }
                catch (Exception ex)
                {
                    Trace.TraceInformation("[{0}] 打开串口失败：{1}\n{2}", this, ex.Message, ex.StackTrace);
                    //throw new CommunicationException(CommunicationError.DeviceOpenFailed, ex.Message);
                    throw new System.IO.FileLoadException();
                }
                _serialPort.DiscardInBuffer(); //  丢弃所有数据，否则在关闭后马上打开可能收到上次打开时对方发送的数据
            }
        }



        private byte[] Read(int timeout)
        {
            var dt = DateTime.Now;
            while (_serialPort.BytesToRead == 0)
            {
                Thread.Sleep(1);
                if (DateTime.Now.Subtract(dt).TotalMilliseconds > timeout)
                    return null;
            }
            Thread.Sleep(50);
            byte[] recData = new byte[_serialPort.BytesToRead];
            _serialPort.Read(recData, 0, recData.Length);
            return recData;
        }

        public int Read(byte[] buffer, int offset, int count)
        {
            lock (this)
            {
#if TRACE
                Trace.TraceInformation("[{0}]准备读取{1}个字节", this, count);
#endif
                try
                {
                    int n = _serialPort.Read(buffer, offset, count);
#if TRACE
                    Trace.TraceInformation("[{0}]成功读取{1}个字节", this, n);
#endif
                    return n;
                }
                catch (Exception ex)
                {
                    Trace.TraceError("[{0}]读取数据出错：{1}\n{2}", this, ex.Message, ex.StackTrace);
                    throw ex;
                }
            }
        }


        public void Write(byte[] buffer, int offset, int count)
        {
            lock (this)
            {
#if TRACE
                Trace.TraceInformation("[{0}]准备写入{1}个字节", this, count);
#endif
                try
                {
                    _serialPort.Write(buffer, offset, count);
#if TRACE
                    Trace.TraceInformation("[{0}]写入数据完成", this);
#endif
                }
                catch (Exception ex)
                {
                    Trace.TraceError("[{0}]写入数据出错：{1}\n{2}", this, ex.Message, ex.StackTrace);
                }
            }
        }

        public bool BeginReceiveAsync(ReceiveAsyncCallback receiveCallback)
        {
            lock (this)
            {
                if (receiveCallback == null)
                    throw new ArgumentNullException();

                //if (!_lock.WaitOne(50))
                //{
                //    return false;
                //}

                if (_receiveCallback != null)
                {
                    Trace.TraceWarning("[{0}] in BeginReceiveAsync, _receiveCallback != null", this);
                }

                _receiveCallback = receiveCallback;

                //_lock.ReleaseMutex();
                return true;
            }
        }

        public bool EndReceiveAsync()
        {
            lock (this)
            {
                //if (!_lock.WaitOne(0))
                //{
                //    Trace.TraceWarning("[{0}]EndReceiveAsync falied", this);
                //    return false;
                //}

                _receiveCallback = null;

                //_lock.ReleaseMutex();
                return true;
            }
        }

        private void OnDataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            lock (this)
            {
#if TRACE
                Trace.TraceInformation("[{0}]收到据数：", this);
#endif

                //_lock.WaitOne();

                try
                {
                    if (_serialPort.BytesToRead > 0)
                    {
                        byte[] buffer = new byte[_serialPort.BytesToRead];
                        _serialPort.Read(buffer, 0, buffer.Length);

#if TRACE
                            Trace.TraceInformation("[{0}]读取{1}个字节", this, buffer.Length);
                            Trace.TraceInformation("[{0}]调用回调函数", this);
#endif
                        if (_receiveCallback != null)
                        {
                            _receiveCallback.Invoke(this, buffer, null);
                        }

#if TRACE
                            Trace.TraceInformation("[{0}]回调函数返回", this);
#endif
                    }
                }
                catch (Exception ex)
                {
                    Trace.TraceInformation("[{0}]向回调函数发送数据异常：<{1}>{2}\n{3}", this, ex.GetType().Name, ex.Message, ex.StackTrace);
                    _receiveCallback.Invoke(this, null, ex);
                }
                // _lock.ReleaseMutex();
            }
        }

    }
}
