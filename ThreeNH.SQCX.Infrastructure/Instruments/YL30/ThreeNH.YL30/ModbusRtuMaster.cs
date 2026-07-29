﻿﻿﻿﻿using NModbus;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Ports;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ThreeNH.CommDevice;
using  param($mt) $map[$mt.Value] ;
using  param($mt) $map[$mt.Value] ;
using ThreeNH.SQCX.Infrastructure.Instruments.YL30.Legacy;
using  param($mt) $map[$mt.Value] ;

namespace ThreeNH.SQCX.Infrastructure.Instruments.YL30
{

    public class ModbusRtuMaster
    {
        private CommDevice.ICommDevice _commDevice;
        private ReceiveAsyncCallback OnReceivedCallBack;
        private int slaveIDPacketLength;
        private AutoResetEvent _receivedPacketEvent = new AutoResetEvent(false);
        private ReadDataPacket _receivedReadingPacket = null;
        private ReadDataPacket _sendReadingPacket;
        private WriteDataPacket _sendWritingPacket;
        private WriteDataPacket _receivedWritingPacket;
        private CommandType _commandType;
        private bool isWorking;
        public ushort SlaveID { get; set; }
        public ModbusRtuMaster(CommDevice.SerialPortDevice serial, ushort slaveID)
        {
            // var serialPort = new SerialPort(portName, baudrate);
            //  EndianConverter.IsLittleEndian = BitConverter.IsLittleEndian;
            _commDevice = serial;
            OnReceivedCallBack += OnReceivedData;
            _commDevice.BeginReceiveAsync(OnReceivedCallBack);
            SlaveID = slaveID;
            slaveIDPacketLength = 18;
        }

        /// <summary>
        /// 设备是否已打开
        /// </summary>
        public bool IsOpen => _commDevice != null && _commDevice.IsOpen;

        /// <summary>
        /// 是否已建立会话（打开通信设备并不代表建立了会话）
        /// </summary>
        public bool IsConnected
        {
            get { return IsOpen; }
        }
        private RequestResult<List<byte>> SendCommand(byte[] bytes, int timeout = 2000)
        {
            ComBuffer.AppendSendDatas(bytes);
            try
            {
                _commDevice.Write(bytes, 0, bytes.Length);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
            bool receivedResponse = _receivedPacketEvent.WaitOne(timeout);

            if (receivedResponse)
            {
                if (_commandType == CommandType.ReadingCommand)
                {
                    var result = _receivedReadingPacket?.Clone();
                    _receivedReadingPacket = null;

                    return result?.RequestResult;
                }
                if (_commandType == CommandType.WritingCommand)
                {
                    _sendWritingPacket = null;
                    _receivedWritingPacket = null;
                    return new RequestResult<List<byte>>(ResultType.OperationSuccess, "写入操作成功");
                }
            }
            return new RequestResult<List<byte>>(ResultType.OperationFail, "操作失败，请求超时");
        }

        private List<byte> _buffer = new List<byte>();
        private void OnReceivedData(object sender, byte[] bytesReceived, Exception ex)
        {
            try
            {
                _buffer.AddRange(bytesReceived);

                if (_sendReadingPacket != null && _buffer.Count == _sendReadingPacket.TotalLength)
                {
                    _receivedReadingPacket = _sendReadingPacket.Clone(_buffer);
                    ComBuffer.AppendRecieveDatas(_receivedReadingPacket.PacketFrame);
                    _sendReadingPacket = null;
                    _buffer.Clear();
                    _receivedPacketEvent.Set();
                }
                if (_sendWritingPacket != null && _buffer.Count > 2 && _buffer[1] == _sendWritingPacket.Funcode)
                {
                    _receivedWritingPacket = _sendWritingPacket.Clone(_buffer);
                    _buffer.Clear();
                    _receivedPacketEvent.Set();
                }
                if (_buffer.Count == slaveIDPacketLength)
                {
                    var str = Encoding.UTF8.GetString(_buffer.ToArray());
                    if (str.Contains("="))
                    {
                        var strs = str.Split('=');
                        if (strs.Length > 1)
                        {
                            SlaveID = Convert.ToUInt16(strs[1]);
                            _buffer.Clear();
                            _receivedPacketEvent.Set();
                        }
                    }
                }

                if (_buffer.Count > 1024)
                {
                    _buffer.Clear();
                    // _commDevice..DiscardInBuffer();
                    _receivedWritingPacket = null;
                    _sendWritingPacket = null;
                    _receivedReadingPacket = null;
                    _sendReadingPacket = null;
                }

            }
            catch
            {
                SlaveID = 88;
                _receivedWritingPacket = null;
                _sendWritingPacket = null;
                _receivedReadingPacket = null;
                _sendReadingPacket = null;
            }
        }
        /// <summary>
        /// 获取仪器基本信息
        /// </summary>
        /// <param name="slaveID"></param>
        /// <param name="startAddress"></param>
        /// <param name="numberOfPoints"></param>
        /// <returns></returns>
        public async Task<RequestResult<List<byte>>> ReadInputRegistersAsync(ushort slaveID, ushort startAddress, ushort numberOfPoints)
        {

            if (isWorking) return new RequestResult<List<byte>>(ResultType.DeviceBusy);
            isWorking = true;
            _sendReadingPacket = new ReadDataPacket(CommandType.ReadingCommand, slaveID, ModbusFunctionCodes.ReadInputRegisters,
             startAddress, numberOfPoints);
            _commandType = CommandType.ReadingCommand;
            var result = await Task.Run(() =>
            {
                return SendCommand(_sendReadingPacket.PacketFrame);
            });
            isWorking = false;
            return result;
        }
        /// <summary>
        /// 获取容差值
        /// </summary>
        /// <param name="slaveID"></param>
        /// <param name="startAddress"></param>
        /// <param name="numberOfPoints"></param>
        /// <returns></returns>
        public async Task<RequestResult<List<byte>>> ReadHoldingRegistersAsync(ushort slaveID, ushort startAddress, ushort numberOfPoints)
        {
            if (isWorking) return new RequestResult<List<byte>>(ResultType.DeviceBusy);
            isWorking = true;
            _sendReadingPacket = new ReadDataPacket(CommandType.ReadingCommand, slaveID, ModbusFunctionCodes.ReadHoldingRegisters,
             startAddress, numberOfPoints);
            _commandType = CommandType.ReadingCommand;
            var result = await Task.Run(() =>
            {
                return SendCommand(_sendReadingPacket.PacketFrame);
            });
            isWorking = false;
            return result;
        }

        /// <summary>
        /// 向仪器中写入多个值
        /// </summary>
        /// <param name="slaveID"></param>
        /// <param name="startAddress"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public async Task<RequestResult> WriteMultiRegistersAsync(ushort slaveID, ushort startAddress, byte[] data)
        {
            byte id = (byte)slaveID;
            if (isWorking) return new RequestResult(ResultType.DeviceBusy);
            isWorking = true;
            _sendWritingPacket = new WriteDataPacket(slaveID, ModbusFunctionCodes.WriteMultipleRegisters,
            startAddress, data, true);
            _commandType = CommandType.WritingCommand;
            var result = await Task.Run(() =>
            {
                return SendCommand(_sendWritingPacket.PacketFrame, 7000);
            });
            isWorking = false;
            return new RequestResult(result.ResultType, result.Msg);
        }

        /// <summary>
        /// 向仪器中写入单个值
        /// </summary>
        /// <param name="slaveID"></param>
        /// <param name="startAddress"></param>
        /// <param name="content"></param>
        /// <returns></returns>
        public async Task<RequestResult> WriteSingleRegistersAsync(ushort slaveID, ushort startAddress, byte[] content)
        {
            byte id = (byte)slaveID;
            if (isWorking) return new RequestResult(ResultType.DeviceBusy);
            isWorking = true;
            _sendWritingPacket = new WriteDataPacket(slaveID, ModbusFunctionCodes.WriteSingleRegister,
            startAddress, content);
            _commandType = CommandType.WritingCommand;
            var result = await Task.Run(() =>
            {
                return SendCommand(_sendWritingPacket.PacketFrame, 5000);
            });
            isWorking = false;
            return new RequestResult(result.ResultType, result.Msg);
        }


        /// <summary>
        /// 关闭设备连接
        /// </summary>
        public void Close()
        {
            _commDevice.Close();
        }

        /// <summary>
        /// 打开设备连接
        /// </summary>
        public void Open(object commDevice = null)
        {
            _commDevice.Open(commDevice);
        }

    }
}
