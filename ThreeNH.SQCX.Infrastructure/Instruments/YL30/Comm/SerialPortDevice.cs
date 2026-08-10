using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThreeNH.PortService;
namespace ThreeNH.SQCX.Infrastructure.Instruments.YL30
{
    public class SerialPortDevice : ICommDevice
    {
        private SerialPort _serialPort;
        //串口参数设置
        private const int _BaudRate = 115200;
        private const Parity _Parity = Parity.None;
        private const StopBits _StopBits = StopBits.One;
        private const int _DataBits = 8;
        private const int _ReadTimeout = 1000;
        private const int _WriteTimeout = 500;
        private ReceivedAsyncCallBack _receivedCallback;
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
            _serialPort.BaudRate = _BaudRate;
            _serialPort.Parity = _Parity;
            _serialPort.StopBits = _StopBits;
            _serialPort.DataBits = _DataBits;
            _serialPort.Handshake = Handshake.None;
            _serialPort.ReadBufferSize = 1024 * 1024; // 1M
            _serialPort.ReadTimeout = _ReadTimeout;
            _serialPort.WriteTimeout = _WriteTimeout;
        }
        public static readonly string[] PortDescriptions =
           {
                "USB-SERIAL CH340",
                "USB-SERIAL CH341"
            };
        private void OnDataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            lock (this)
            {
                try
                {
                    if (_serialPort.BytesToRead>0)
                    {
                        byte[] buffer = new byte[_serialPort.BytesToRead];
                        _serialPort.Read(buffer, 0, buffer.Length);
                        _receivedCallback?.Invoke(this, buffer, null);
                    }
                    
                }
                catch (Exception ex)
                {
                    _receivedCallback?.Invoke(this, null, ex);
                }        
            }
        }

        public string DriverDescription { get; set; }

        public string DeviceName => IsOpen ? _serialPort.PortName : null;

        public bool IsOpen =>_serialPort!=null && _serialPort.IsOpen;

        public bool IsBluetoothSerialPortDevice => string.Compare(DriverDescription, "USB-SERIAL CH340") == 0;

        public Stream stream => _serialPort.BaseStream;

        public SerialPort SerialPort => _serialPort;

       

        public bool BeginReceiveAsync(ReceivedAsyncCallBack callback)
        {
            if (callback==null)
            {
                throw new ArgumentNullException();
            }
            _receivedCallback = callback;
            return true;
        }

        public void Close()
        {
            lock (this)
            {
                if (_serialPort==null)
                {
                    return;
                }
                try
                {
                    EndReceiveAsync();
                    _serialPort.DataReceived -= OnDataReceived;
                    if (_serialPort.IsOpen)
                    {
                        _serialPort.Close();
                    }
                    //_serialPort.Close();
                }
                catch (Exception ex)
                {
                    //Do Nothing
                }
                finally
                {
                    _serialPort = null;
                    _receivedCallback = null;
                }
            
            }
        }

        public bool EndReceiveAsync()
        {
            lock (this)
            {
                _receivedCallback = null;
                return true;
            
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
        public void Open(object device=null)
        {
            lock (this)
            {
                string? deviceName = device as string;
                //获取portName
                if (device == null)
                {
                    foreach (var info in SerialPortService.SerialPorts)
                    {
                        if (info.IsUsb2SerialPort()|| info.IsBluetoothDonglePort()||info.IsSTM32VirtualPort())
                        {
                            deviceName = info.PortName;
                            DriverDescription = info.Description;
                            break;
                        }
                    }
                    if (deviceName == null)
                    {
                        throw new FileNotFoundException();
                    }
                }
                else
                {
                    foreach (var info in SerialPortService.SerialPorts)
                    {
                        if (string.Compare(deviceName,info.PortName)==0)
                        {
                            DriverDescription = info.Description;
                            break;
                        }
                    }
                }
                try
                {
                    InitSerialPort();
                    _serialPort.PortName= deviceName;
                    _serialPort.Open();
                    _serialPort.DataReceived += OnDataReceived;
                }
                catch (Exception ex)
                {
                  // throw ex;
                    //do nothing
                }
                //  丢弃所有数据，否则在关闭后马上打开可能收到上次打开时对方发送的数据
                _serialPort.DiscardInBuffer();
            
            }
        }

        public int Read(byte[] buffer, int offset, int count)
        {
            lock (this)
            {
                int n = _serialPort.Read(buffer, offset, count);
                return n;
            }
        }

        public void Write(byte[] buffer, int offset, int count)
        {
            lock (this)
            {
                try
                {
                    _serialPort.Write(buffer, offset, count);
                }
                catch (Exception ex)
                {

                   
                }
            }
        }
    }
}
