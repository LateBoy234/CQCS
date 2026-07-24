using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace ThreeNH.CommDevice
{
    /// <summary>
    /// 网络连通性监控器
    /// </summary>
    public class NetworkConnectivityMonitor
    {
        /// <summary>
        /// 连接状态变更事件
        /// </summary>

        public static EventHandler<NetworkConnectivityChangedEventArgs> OnDisconnected;

        private CancellationTokenSource _cts;     // 取消令牌（控制异步任务）

        private async Task<bool> PingInstrumentAsync(string ip, int port)
        {
            try
            {
                using (var ping = new Ping())
                {
                    // communication.IP, Convert.ToInt32(communication.Port
                    var reply = await ping.SendPingAsync(ip, port);
                    var success = reply.Status == IPStatus.Success;
                    return success;
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError($"[Ping] Exception: {ex.Message}");
                return false;
            }
        }
        public async Task StartMonitoTCP(string ip, int port)
        {
            int pingCount = 0;
            _cts = new CancellationTokenSource();
            while (!_cts.Token.IsCancellationRequested)
            {
                try
                {
                    // 1. 先 Ping 检测网络连通性
                    if (!await PingInstrumentAsync(ip, port))
                    {
                        pingCount++;
                        if (pingCount > 1)
                        {
                            OnDisconnected?.Invoke(
                                this,
                                new NetworkConnectivityChangedEventArgs(
                                     ip, // host
                                    port,      // port
                                    false,      // isConnected
                                    System.DateTime.Now, // timestamp
                                    "网络不通，Ping 仪器 IP 失败" // errorMessage
                                )
                            );
                            _cts.Cancel();
                            _cts.Dispose();
                        }
                        continue;
                    }
                }
                catch (OperationCanceledException ex)
                {
                    Trace.TraceError($"[StartMasterMode] Operation cancelled: {ex.Message}");
                    // 主动取消，退出循环
                    break;
                }
                catch (Exception ex)
                {
                    Trace.TraceError($"[StartMasterMode] Exception: {ex.Message}");
                    Trace.TraceError($"[StartMasterMode] Stack trace: {ex.StackTrace}");
                }
                // 重连间隔
                if (!_cts.Token.IsCancellationRequested)
                {
                    Trace.TraceError($"[StartMasterMode] Waiting for reconnect interval...");
                    await Task.Delay(3000, _cts.Token);
                }
            }
        }

        public void Dispose()
        {
            _cts.Cancel();
            _cts.Dispose();
        }
    }

    /// <summary>
    /// 网络连通性变化事件参数
    /// </summary>
    public class NetworkConnectivityChangedEventArgs : EventArgs
    {
        /// <summary>
        /// 主机地址
        /// </summary>
        public string Host { get; }

        /// <summary>
        /// 端口号
        /// </summary>
        public int Port { get; }

        /// <summary>
        /// 连接状态
        /// </summary>
        public bool IsConnected { get; }

        /// <summary>
        /// 时间戳
        /// </summary>
        public DateTime Timestamp { get; }

        /// <summary>
        /// 错误信息（如果有）
        /// </summary>
        public string ErrorMessage { get; }

        public NetworkConnectivityChangedEventArgs(string host, int port, bool isConnected, DateTime timestamp, string errorMessage = null)
        {
            Host = host;
            Port = port;
            IsConnected = isConnected;
            Timestamp = timestamp;
            ErrorMessage = errorMessage;
        }
    }
}