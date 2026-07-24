using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace ThreeNH.CommunicationBase
{

    /// <summary>
    /// EH MC17的AT命令接口
    /// </summary>
    public class EhMc17AtInterface : IDisposable
    {
        /// <summary>
        /// 从设备
        /// </summary>
        public class SlaveDevice
        {
            public int Index { get; set; }  // 设备序号
            public string Name { get; set; } // 从设备名
            public string Address { get; set; } // 设备地址
            public int SignalIntensity { get; set; } // 信号强度
        }

        #region AT常量定义

        // 透传模式
        public const string NONE_PASSTHROUGH = "00"; // 非透传
        public const string PASSTHROUGH = "01"; // 透传模式(暂时没有用)
        public const string EXITABLE_PASSTHROUGH = "02"; // 可退出透传

        // 默认退出码
        public const string DEFAULT_EXIT_CODE16 = "71D56F4950DED3021A3A5A9764D1BD0F"; // 默认16位退出码
        public const string DEFAULT_EXIT_CODE32 = "71D56F4950DED3021A3A5A9764D1BD0FD6C4725270105A75084DD40C37A81750"; // 默认32位退出码

        // 退出码刷新模式
        public const string USE_DEFAULT_EXIT_CODE = "0"; // 使用默认退出码
        public const string USE_RANDOM_EXIT_CODE_FIRST_POWERED = "1"; // 第一次上电时随机生成一个



        public const string USE_RANDOM_EXIT_CODE_EVERY_POWERED = "2"; // 每次上电时随机生成一个
        public const string USE_RANDOM_EXIT_CODE_ENTER_PASSTHROUGH = "3"; // 每次进入透传模式时随机生成一个

        // 主从模式代码
        public const string ROLE_MASTER_DEVICE = "1"; // 主设备
        public const string ROLE_SLAVE_DEVICE = "0";    // 从设备
        #endregion

        private SerialPort _serialPort;
        private ManualResetEvent _event = new ManualResetEvent(false);

        private bool _isRunning = false;

        public event Action<SlaveDevice> DeviceDiscovered;
        protected void OnSlaveDeviceDiscovered(SlaveDevice device)
        {
            DeviceDiscovered?.Invoke(device);
        }

        /// <summary>
        /// 使用已打开的串口创建AT命令收发器
        /// </summary>
        /// <param name="serialPort">已打开的串口</param>
        public EhMc17AtInterface(SerialPort serialPort)
        {
            if (serialPort == null || !serialPort.IsOpen)
            {
                throw new ArgumentException("Invalid SerialPort instance");
            }
            _serialPort = serialPort;
            _serialPort.NewLine = "\r\n";
            _serialPort.DataReceived += OnDataReceived;
        }

        public void AttachSerialPort(SerialPort serialPort)
        {
            _serialPort = serialPort;
            _serialPort.NewLine = "\r\n";
            _serialPort.DataReceived += OnDataReceived;
        }

        public SerialPort DetachSerialPort()
        {
            _serialPort.DataReceived -= OnDataReceived;
            var tmp = _serialPort;
            _serialPort = null;
            return tmp;
        }

        private void OnDataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            _event.Set();
        }

        private string[] ReadLines(int return_line_count, int timeout)
        {
            List<string> lines = new List<string>(return_line_count);
            TimeSpan left_time = new TimeSpan(timeout * TimeSpan.TicksPerMillisecond);
            DateTime timestamp = DateTime.Now;

            bool isReceivedLr = true; // 是否已收到换行符
            try
            {
                while (true)
                {
                    if (_event == null || _event.SafeWaitHandle.IsClosed || !_event.WaitOne(left_time))
                    {
                        break;
                    }
                    do
                    {
                        var text = _serialPort.ReadExisting();
                        var strings = text.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
                        if (!isReceivedLr)
                        {
                            if (strings.Length > 0)
                            {
                                lines[lines.Count - 1] = lines[lines.Count - 1] + strings[0];
                                if (strings.Length > 1)
                                {
                                    lines.AddRange(strings.Skip(1));
                                }
                            }
                        }
                        else
                        {
                            lines.AddRange(strings);
                        }
                        isReceivedLr = text.EndsWith("\r\n");
                    } while (_serialPort.BytesToRead > 0);
                    _event.Reset();


                    left_time = left_time - (DateTime.Now - timestamp);

                    if ((isReceivedLr && lines.Count >= return_line_count) || left_time.TotalMilliseconds <= 0)
                    {
                        break;
                    }
                }
            }
            catch (Exception)
            {
                TimeSpan span = left_time;
                throw;
            }


            return lines.ToArray();
        }

        // 发送命令并等待收到指定行数据
        public string[] SendCommand(string cmd, string param = null, int return_line_count = 1, int timeout = 1000)
        {
            _serialPort.DiscardInBuffer();

            _event.Reset();
            if (param != null)
            {
                _serialPort.WriteLine($"{cmd}={param}");
            }
            else
            {
                _serialPort.WriteLine(cmd);
            }

            return ReadLines(return_line_count, timeout);
        }

        /// <summary>
        /// 复位蓝牙模块，即重新上电
        /// </summary>
        /// <returns>成功返回true</returns>
        public bool Reset()
        {
            var lines = SendCommand("AT+RT");
            return lines.Length >= 1 && lines[0] == "OK";
        }

        /// <summary>
        /// 恢复出厂设置
        /// </summary>
        /// <returns>成功返回真</returns>
        public bool RecoverFactory()
        {
            var lines = SendCommand("AT+DF");
            return lines.Length >= 1 && lines[0] == "OK";
        }

        /// <summary>
        /// 获取透传模式
        /// </summary>
        /// <returns>如果成功透传模式，否则返回null</returns>
        public string GetByMode()
        {
            var lines = SendCommand("AT+BY");
            if (lines?.Length == 1)
            {
                return GetReturnParam(lines[0]);
            }
            return null;
        }

        /// <summary>
        /// 设置透传模式
        /// </summary>
        /// <param name="mode">透传模式</param>
        /// <returns>成功返回true</returns>
        public bool SetByMode(string mode)
        {
            var lines = SendCommand("AT+BY", mode);
            if (lines.Length == 1)
            {
                var ret = GetReturnParam(lines[0]);
                return ret == mode;
            }

            return false;
        }

        /// <summary>
        /// 获取退出码长度
        /// </summary>
        /// <returns>如果成功返回退出码长度，失败返回0</returns>
        public int GetExitCodeLength()
        {
            var lines = SendCommand("AT+EXL", null, 1, 500);
            if (lines.Length == 1)
            {
                try
                {
                    return int.Parse(GetReturnParam(lines[0]));
                }
                catch
                {
                    //
                }
            }

            return 0;
        }

        /// <summary>
        /// 设置退出码长度
        /// </summary>
        /// <param name="length">退出码长度，只能为32或16位</param>
        /// <returns>成功返回真，失败返回假</returns>
        public bool SetExitCodeLength(int length)
        {
            if (length != 16 && length != 32)
            {
                return false;
            }

            var lines = SendCommand("AT+EXL", length.ToString());
            if (lines.Length == 1)
            {
                if (lines[0].StartsWith("ER"))
                {
                    return false;
                }

                return GetReturnParam(lines[0]) == length.ToString();
            }

            return false;
        }

        /// <summary>
        /// 设置或获取退出码刷新模式
        /// </summary>
        /// <param name="mode"></param>
        /// <returns>成功返回true</returns>
        public bool SetExitCodeRefreshMode(string mode)
        {
            var lines = SendCommand("AT+EXM", mode);
            return (lines.Length == 1 && lines[0] == "OK");
        }

        /// <summary>
        /// 获取退出码刷新模式
        /// </summary>
        /// <returns>成功返回退出码刷新模式，失败返回null</returns>
        public string GetExitCodeRefreshMode()
        {
            var lines = SendCommand("AT+EXM");
            if (lines.Length == 1 && lines[0].StartsWith("EXM"))
            {
                return GetReturnParam(lines[0]);
            }

            return null;
        }

        /// <summary>
        /// 获取当前的透传退出命令的退出码
        /// </summary>
        /// <returns>如果成功返回退出码，否则返回NULL</returns>
        public string GetExitCode()
        {
            var lines = SendCommand("AT+EXC");
            if (lines.Length == 1)
            {
                return GetReturnParam(lines[0]);
            }

            return null;
        }

        /// <summary>
        /// 退出透传模式
        /// </summary>
        /// <param name="exitCode">退出码，如果为空使用默认的16位退出码</param>
        /// <remarks>主模块透传模式必须设为<c>02</c></remarks>
        /// <returns>成功返回true</returns>
        public bool ExitPassThroughMode(string exitCode = null)
        {
            var lines = SendCommand("AT+EX", exitCode ?? DEFAULT_EXIT_CODE16);
            return lines.Length == 1 && lines[0] == "BY=00";
        }

        /// <summary>
        /// 获取固件版本
        /// </summary>
        /// <returns>成功返回固件版本，失败返回null</returns>
        public string GetVersion()
        {
            var lines = SendCommand("AT+VR", return_line_count: 2);
            if (lines.Length == 2 && lines[1] == "OK")
            {
                return lines[0];
            }

            return null;
        }

        /// <summary>
        /// 获取模块名称
        /// </summary>
        /// <returns>成功返回模块名称，失败返回null</returns>
        public string GetName()
        {
            var lines = SendCommand("AT+NM");
            return lines.Length == 1 ? GetReturnParam(lines[0]) : null;
        }

        /// <summary>
        /// 设置模块名称
        /// </summary>
        /// <param name="name">新名称</param>
        /// <returns>成功返回true，失败返回false</returns>
        public bool SetName(string name)
        {
            var lines = SendCommand("AT+NM", name, 2);
            return lines.Length == 2 && lines[1] == "OK";
        }

        /// <summary>
        /// 查询蓝牙模式的地址
        /// </summary>
        /// <returns>成功返回地址，失败返回null</returns>
        public string GetMacAddress()
        {
            var lines = SendCommand("AT+AR");
            if (lines.Length == 1)
            {
                return GetReturnParam(lines[0]);
            }
            return null;
        }

        /// <summary>
        /// 解析扫描结果
        /// </summary>
        /// <param name="scanResults">扫描返回的如果</param>
        /// <returns>返回解析到的从设备信息</returns>
        private SlaveDevice[] ParseSlaveDevices(IEnumerable<string> scanResults)
        {
            List<SlaveDevice> list = new List<SlaveDevice>();
            foreach (var line in scanResults)
            {
                var m = Regex.Match(line, @"^SR=(\d+):([\S\x20]+),([0-9a-fA-F]+),(-\d+)");
                if (m.Success)
                {
                    list.Add(new SlaveDevice
                    {
                        Index = int.Parse(m.Groups[1].Value),
                        Name = m.Groups[2].Value,
                        Address = m.Groups[3].Value,
                        SignalIntensity = int.Parse(m.Groups[4].Value)
                    });
                }
            }
            return list.ToArray();
        }
        /// <summary>
        /// 开始扫描从设备
        /// </summary>
        public void BeginScan()
        {
            var lines = SendCommand("AT+SC", return_line_count: 1, timeout: 1000);
            if (lines.Length >= 1 && lines[0] == "+INQE") // 表示前面扫描未关闭，需重新扫描
            {
                lines = SendCommand("AT+SC", return_line_count: 1, timeout: 1000);
            }
            _isRunning = true;
            Task.Factory.StartNew(() =>
            {
                while (_isRunning)
                {
                    var tmp = ReadLines(10, 1000);
                    var devices = ParseSlaveDevices(tmp.ToList());
                    foreach (var device in devices)
                    {
                        OnSlaveDeviceDiscovered(device);
                    }
                    Thread.Sleep(1);
                }
            });
        }

        public void StopScan()
        {
            _isRunning = false;
            Thread.Sleep(1000);
            var lines = SendCommand("AT+SC", return_line_count: 1, timeout: 1000);
            //if (lines.Length >= 1 && lines[0] != "+INQE") // 表示前面扫描未关闭，需重新扫描
            //{
            //    lines = SendCommand("AT+SC", return_line_count: 1, timeout: 1000);
            //}
        }

        /// <summary>
        /// 扫描从设备
        /// </summary>
        /// <param name="milliseconds">扫描时间</param>
        /// <returns>返回所有扫描到的从设备；如果失败返回null</returns>
        public SlaveDevice[] ScanSlaveDevices(int milliseconds = 5000)
        {
            DateTime timestamp = DateTime.Now;
            var lines = SendCommand("AT+SC", return_line_count: 1, timeout: milliseconds);
            if (lines.Length >= 1 && lines[0] == "+INQE") // 表示前面扫描未关闭，需重新扫描
            {
                lines = SendCommand("AT+SC", return_line_count: 1, timeout: milliseconds);
            }

            if (lines.Length >= 1 && lines[0] == "OK")
            {
                milliseconds = milliseconds - (int)(DateTime.Now - timestamp).TotalMilliseconds;
                // 读取剩余返回
                var tmp = ReadLines(10, milliseconds);
                List<string> list = new List<string>();
                list.AddRange(lines);
                list.AddRange(tmp);

                // 停止扫描
                tmp = SendCommand("AT+SC");
                if (tmp.Length == 0 || tmp[0] != "+INQE")
                {
                    return null;
                }
                return ParseSlaveDevices(list.Skip(2));
            }

            return null;
        }

        /// <summary>
        /// 获取已扫描到的从设备
        /// </summary>
        /// <returns>返回所有已扫描的从设备</returns>
        /// <remarks>与<c>ScanSlaveDevices</c>不同的是，该函数不会执行扫描</remarks>
        public SlaveDevice[] GetSlaveDevices()
        {
            var lines = SendCommand("AT+SR", return_line_count: 10, timeout: 500);
            return ParseSlaveDevices(lines);
        }

        /// <summary>
        /// 连接到从设备
        /// </summary>
        /// <param name="address">从设备的地址</param>
        /// <param name="exitCode">如果退出码刷新模式是每次进入透传模式时生成，则返回退出码，否则返回null</param>
        /// <returns></returns>
        public bool Connect(string address, out string exitCode)
        {
            var lines = SendCommand("AT+CT", address, 2);
            if (lines.Length >= 2 && lines[0] == "OK")
            {
                if (lines[1].StartsWith("EXC=")) // 有返回退出码
                {
                    exitCode = GetReturnParam(lines[1]);
                }
                else
                {
                    exitCode = null;
                }
                return true;
            }
            exitCode = null;
            return false;
        }

        /// <summary>
        /// 获取连接
        /// </summary>
        /// <returns>如果已连接，返回连接到的设备的地址；未连接返回空字符串；失败返回null</returns>
        /// <remarks>只能在非透传模式下</remarks>
        public string GetConnection()
        {
            var lines = SendCommand("AT+LC");
            if (lines.Length == 1)
            {
                if (lines[0].StartsWith("ER"))
                {
                    return String.Empty;
                }
                return GetReturnParam(lines[0]);
            }
            return null;
        }

        /// <summary>
        /// 配置主从模式
        /// </summary>
        /// <param name="role">主从模式代码</param>
        /// <returns>成功返回真</returns>
        public bool SetRole(string role)
        {
            var lines = SendCommand("AT+MT", role, 3, 3000);
            if (lines.Length == 3)
            {
                return GetReturnParam(lines[0]) == role;
            }

            return false;
        }

        /// <summary>
        /// 获取主从模式代码
        /// </summary>
        /// <returns>成功返回模式代码，失败返回null</returns>
        public string GetRole()
        {
            var lines = SendCommand("AT+MT");
            if (lines.Length == 1)
            {
                return GetReturnParam(lines[0]);
            }

            return null;
        }

        /// <summary>
        /// 断开连接
        /// </summary>
        /// <returns>成功返回真</returns>
        /// <remarks>只能在非透传模式</remarks>
        public bool Disconnect()
        {
            var lines = SendCommand("AT+DC");
            return lines.Length == 1 && lines[0] == "+DISCONNECT";
        }

        /// <summary>
        /// 仅解除串口事件绑定，不发送 AT 命令。透传已建立时用于把串口交给仪器协议层。
        /// </summary>
        public void Detach()
        {
            _isRunning = false;
            if (_serialPort != null)
                _serialPort.DataReceived -= OnDataReceived;
        }

        public void Dispose()
        {
            try
            {
                StopScan();
            }
            catch
            {
                _isRunning = false;
            }

            _event?.Dispose();
            if (_serialPort != null)
            {
                _serialPort.DataReceived -= OnDataReceived;
            }
        }

        /// <summary>
        /// 解析返回的参数
        /// </summary>
        /// <param name="text"></param>
        /// <returns>如果找到参数字符，否则返回NULL</returns>
        private string GetReturnParam(string text)
        {
            var m = Regex.Match(text, @"^\S+=([\S\x20]+)");
            if (m.Success)
            {
                return m.Groups[1].Value;
            }

            return null;
        }

        /// <summary>
        /// 初始化蓝牙模块
        /// </summary>
        /// <returns>成功返回true，失败返回false</returns>
        public bool InitDongle()
        {
            try
            {
                // 1. 获取模式，如果是从模式将其设为主模式
                ExitPassThroughMode();
                Disconnect();
                var role = GetRole();
                // if (role == null)
                // {
                //     // 1.1 可能由于原来未正常退出透传模式，尝试退出
                //     if (ExitPassThroughMode() && Disconnect())
                //     {
                //         role = GetRole();
                //     }
                if (role == null)
                {
                    Trace.TraceError("Get bluetooth module role failed\r\n");
                    return false;
                }
                // }

                if (role != EhMc17AtInterface.ROLE_MASTER_DEVICE)
                {
                    if (!SetRole(EhMc17AtInterface.ROLE_MASTER_DEVICE))
                    {
                        Trace.TraceError("Set bluetooth module role as master failed\r\n");
                        return false;
                    }
                }

                // 2. 获取透传模式，设置透传模式为可退出透传
                var byMode = GetByMode();
                if (byMode == null)
                {
                    Trace.TraceError("Get ByMode failed\r\n");
                    return false;
                }

                if (byMode != EhMc17AtInterface.EXITABLE_PASSTHROUGH)
                {
                    if (!SetByMode(EhMc17AtInterface.EXITABLE_PASSTHROUGH))
                    {
                        Trace.TraceError("Set ByMode as EXITABLE_PASSTHROUGH failed\r\n");
                        return false;
                    }
                }
                // 3. 获取退出码长度，如果不是16位将其设为16位
                // var exitCodeLength = GetExitCodeLength();
                // if (exitCodeLength == 0)
                // {
                //     Trace.TraceError("Get exit code length failed\r\n");
                //     // return false;
                // }
                //
                // if (exitCodeLength != 16)
                // {
                if (!SetExitCodeLength(16))
                {
                    Trace.TraceError("Set exit code length as 16 failed\r\n");
                    return false;
                }
                // }
                // 4. 获取退出码，如果不是默认退出码将其设为默认退出码
                var exitCodeRefreshMode = GetExitCodeRefreshMode();
                if (exitCodeRefreshMode != EhMc17AtInterface.USE_DEFAULT_EXIT_CODE)
                {
                    if (!SetExitCodeRefreshMode(EhMc17AtInterface.USE_DEFAULT_EXIT_CODE))
                    {
                        Trace.TraceError("Set exit code refresh mode as default exit code failed\r\n");
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError("初始化蓝牙模块失败：{0}", ex.Message);
                return false;
            }

            return true;
        }

    }
}
