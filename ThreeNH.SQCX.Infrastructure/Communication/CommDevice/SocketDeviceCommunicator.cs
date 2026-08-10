using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ThreeNH.CommDevice;


// 通信状态枚举
public enum ConnectionState
{
    Disconnected,  // 未连接
    Connecting,    // 连接中
    Connected      // 已连接
}

/// <summary>
/// 上位机与仪器通信核心类（支持主站/从站模式）
/// </summary>
public class SocketDeviceCommunicator : ICommDevice, IDisposable
{
    #region 配置参数
    private readonly string _ipAddress;       // 仪器IP（主站模式用）
    private readonly int _port;               // 通信端口
    #endregion

    #region 核心对象
    private TcpClient _tcpClient;             // TCP客户端（主站/从站共用）
    private NetworkStream _networkStream;     // 网络流
    private CancellationTokenSource _cts;     // 取消令牌（控制异步任务）
    private ConnectionState _state = ConnectionState.Disconnected;
    #region ICommDevice接口属性
    public string DriverDescription { get; set; }
    public string DeviceName { get; private set; }

    public bool IsOpen => _state == ConnectionState.Connected;

    public bool IsBlueSerialPortDevice => false;

    public Stream Stream => null;
    #endregion

    #endregion

    #region 事件（供外部监听状态）

    /// <summary>
    /// 接收数据事件
    /// </summary>
    public event ReceiveAsyncCallback OnDataReceived;


    #endregion

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="ipAddress">仪器IP（主站模式必填）</param>
    /// <param name="port">通信端口</param>
    /// <param name="mode">通信模式</param>
    public SocketDeviceCommunicator(string ipAddress, int port)
    {
        _ipAddress = ipAddress;
        _port = port;
        _cts = new CancellationTokenSource();
    }



    #region 核心辅助方法


    /// <summary>
    /// 接收仪器数据
    /// </summary>
    private async Task ReceiveDataAsync()
    {
        var buffer = new byte[8192]; // 接收缓冲区

        while (!_cts.Token.IsCancellationRequested && _tcpClient?.Connected == true)
        {
            try
            {
                // 实时监听数据（无数据则阻塞，直到有数据/断开/超时）
                var bytesRead = await _networkStream.ReadAsync(buffer, 0, buffer.Length, _cts.Token);
                if (bytesRead == 0)
                {
                    // 读取到 0 字节 = 连接已断开
                    Debug.WriteLine($"[Receive] Detected disconnect (0 bytes read)");
                    break;
                }
                // 复制有效数据并触发接收事件
                var receivedData = new byte[bytesRead];
                Array.Copy(buffer, receivedData, bytesRead);
                OnDataReceived?.Invoke(this, receivedData, null);
            }
            catch (IOException ex)
            {
                // 网络流异常 = 连接中断
                Trace.TraceError($"[Receive] IOException: {ex.Message}");
                break;
            }
            catch (Exception ex)
            {
                Trace.TraceError($"[Receive] Exception: {ex.Message}");
                break;
            }
        }
        Trace.TraceError($"[Receive] Stopped at {DateTime.Now:HH:mm:ss.fff}");
    }

    /// <summary>
    /// 发送数据到仪器
    /// </summary>
    /// <param name="data">待发送的字节数组</param>
    public void SendData(byte[] data, int offset, int count)
    {
        if (_state != ConnectionState.Connected || _networkStream == null)
        {
            return;
        }

        try
        {
            _networkStream.Write(data, offset, count);
            _networkStream.Flush();
        }
        catch
        {
            CleanupConnection();
            SetState(ConnectionState.Disconnected);
        }
    }

    /// <summary>
    /// 清理连接资源
    /// </summary>
    private void CleanupConnection()
    {
        _networkStream?.Dispose();
        _tcpClient?.Close();
        _networkStream = null;
        _tcpClient = null;
    }

    /// <summary>
    /// 设置连接状态并触发事件
    /// </summary>
    private void SetState(ConnectionState state)
    {
        if (_state != state)
        {
            _state = state;
        }
    }

    /// <summary>
    /// 停止通信（释放资源）
    /// </summary>
    public void Stop()
    {
        _cts.Cancel();
        CleanupConnection();
        SetState(ConnectionState.Disconnected);
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        Stop();
        _cts.Dispose();
    }

    public void Open(object device = null)
    {
        // 2. 建立TCP连接
        _tcpClient = new TcpClient { ReceiveTimeout = 5000, SendTimeout = 5000 };
        _tcpClient.Connect(_ipAddress, _port);
        _networkStream = _tcpClient.GetStream();

        SetState(ConnectionState.Connected);

        DeviceName = _ipAddress;

        // 启动心跳和数据接收任务
        var receiveTask = Task.Run(async () => await ReceiveDataAsync(), _cts.Token);
    }

    public void Close()
    {
        Stop();
    }

    public int Read(byte[] buffer, int offset, int count)
    {
        throw new NotImplementedException();
    }

    public void Write(byte[] buffer, int offset, int count)
    {
        SendData(buffer, offset, count);
    }

    public bool BeginReceiveAsync(ReceiveAsyncCallback receiveCallback)
    {
        lock (this)
        {
            if (receiveCallback == null)
                throw new ArgumentNullException(nameof(receiveCallback));
            if (OnDataReceived != null)
            {
                Trace.TraceWarning("[{0}] in BeginReceiveAsync, _receiveCallback != null", this);
            }
            OnDataReceived = receiveCallback;
            return true;
        }
    }

    public bool EndReceiveAsync()
    {
        lock (this)
        {
            OnDataReceived = null;
            return true;
        }
    }
    #endregion
}