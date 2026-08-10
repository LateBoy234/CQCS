using System;
using System.IO;

namespace ThreeNH.CommDevice
{

    public delegate void ReceiveAsyncCallback(object sender, byte[] bytesReceived, Exception ex);
    public interface ICommDevice
    {
       
        /// <summary>
        /// 打开设备
        /// </summary>
        /// <param name="device">要打开的设备的标识；如果为null打开默认设备</param>
        void Open(object device = null);
        /// <summary>
        /// 开闭设备
        /// </summary>
        void Close();

        /// <summary>
        /// 串口驱动描述
        /// </summary>
        string DriverDescription { get; set; }
        /// <summary>
        /// 通讯设备名称
        /// </summary>
        string DeviceName { get; }
        /// <summary>
        /// 通讯设备是否已打开
        /// </summary>
        bool IsOpen { get; }

        bool IsBlueSerialPortDevice { get; }

        /// <summary>
        /// 用于通讯的流
        /// </summary>
        Stream Stream { get; }
        /// <summary>
        /// 从设备输入缓冲区读取一些字节并将那些字节写入字节数组中指定的偏移量处。
        /// </summary>
        /// <param name="buffer">将输入写入到其中的字节数组</param>
        /// <param name="offset">缓冲区数组中开始写入的偏移量</param>
        /// <param name="count">要读取的字节数</param>
        /// <returns>读取的字节数</returns>
        int Read(byte[] buffer, int offset, int count);
        /// <summary>
        /// 向设备中写入指定字节数
        /// </summary>
        /// <param name="buffer">包含要写入端口的数据的字节数组</param>
        /// <param name="offset">参数中从零开始的字节偏移量，从此处开始将字节复制到设备</param>
        /// <param name="count">要写入的字节数</param>
        void Write(byte[] buffer, int offset, int count);
        /// <summary>
        /// 开始异步接收数据；如果要结束异步操作需要关闭连接
        /// </summary>
        /// <param name="receiveCallback">在接收到数据时的回调函数</param>
        /// <param name="parameter">传递给回调函数的附加参数</param>
        /// <returns>如果调用成功返回真，否则返回假</returns>
        bool BeginReceiveAsync(ReceiveAsyncCallback receiveCallback);
        /// <summary>
        /// 结束异步接收数据
        /// </summary>
        /// <param name="receiveCallback"></param>
        /// <returns></returns>
        bool EndReceiveAsync();
    }
}
