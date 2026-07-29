﻿﻿﻿﻿using Modbus.Device;
using Modbus.Message;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using  param($mt) $map[$mt.Value] ;

namespace ThreeNH.SQCX.Infrastructure.Instruments.YL30.Legacy
{
    public class ModbusRtuMaster
    {
        private ICommDevice _commDevice;
        private IModbusSerialMaster modbusMaster;
        private ReceivedAsyncCallBack OnReceivedCallBack;
        private int slaveIDPacketLength;
        private EventWaitHandle _receivedPacketEvent = new EventWaitHandle(false, EventResetMode.AutoReset);
        public ushort SlaveID { get; set; }

        public ModbusRtuMaster()
        {
            _commDevice=new SerialPortDevice();
          //  OnReceivedCallBack += OnReceivedData;
          //  _commDevice.BeginReceiveAsync(OnReceivedCallBack);
            slaveIDPacketLength=18;
        }
        private List<byte> _buffer = new List<byte>();
        private void OnReceivedData(object sender, byte[] bytesReceived, Exception ex)
        {
            try
            {
                _buffer.AddRange(bytesReceived);
                if (_buffer.Count == slaveIDPacketLength)
                {
                    var str = Encoding.UTF8.GetString(bytesReceived);
                    if (str.Contains("="))
                    {
                        var strs = str.Split('=');
                        if (strs.Length > 1)
                        {
                            SlaveID = Convert.ToUInt16(strs[1]);
                            _receivedPacketEvent.Set();
                            _buffer.Clear();
                        }
                    }
                }
                if (_buffer.Count == 11)
                {
                    var str = Encoding.UTF8.GetString(bytesReceived);
                    if (str.Contains("="))
                    {
                        var strs = str.Split('=');
                        if (strs.Length > 1)
                        {
                            SlaveID = Convert.ToUInt16(strs[1]);
                            _receivedPacketEvent.Set();
                            _buffer.Clear();
                        }
                    }
                }
            }
            catch 
            {
                SlaveID = 88;
            }
        }

        public bool IsOpen=>_commDevice.IsOpen;
        public void Open() 
        {
            _commDevice.Open();
            modbusMaster = ModbusSerialMaster.CreateRtu(_commDevice.SerialPort);
            
            modbusMaster.Transport.WriteTimeout = 5000;
            modbusMaster.Transport.ReadTimeout = 5000;
            modbusMaster.Transport.WaitToRetryMilliseconds = 5000;
            modbusMaster.Transport.Retries = 3;
        }
        public void Close() 
        {
            _commDevice.Close();
            modbusMaster = null;
        }
        public async Task GetSlaveID()
        {
            if (_commDevice.IsOpen)
            {
                byte[] bytes = {0x55,0xAA,0xAA,0x88};
                _commDevice.Write(bytes,0,bytes.Length);
                _receivedPacketEvent.Reset();
                if (_receivedPacketEvent.WaitOne(5000))
                {
                    await Task.CompletedTask;
                } ;
                 
            }
        }
        public async Task GetCommSetting()
        {
            if (_commDevice.IsOpen)
            {
                byte[] bytes = { 0x58, 0x03, 0x75, 0x31,0x00,0x03,0x42,0xC1 };
                _commDevice.Write(bytes, 0, bytes.Length);
                _receivedPacketEvent.Reset();
                if (_receivedPacketEvent.WaitOne(5000))
                {
                    await Task.CompletedTask;
                };

            }
        }
        public async Task<ushort[]> ReadHoldingRegistersAsync(ushort slaveID,ushort startAddress,ushort numberOfPoints)
        {
            byte id =(byte)slaveID;
            return   await modbusMaster.ReadHoldingRegistersAsync(id, startAddress, numberOfPoints);   
        }
        public async Task WriteSingleRegistersAsync(ushort slaveID, ushort startAddress, ushort data)
        {
            byte id = (byte)slaveID;
            await modbusMaster.WriteSingleRegisterAsync(id, startAddress, data);
        }
        public async Task WriteMultiRegistersAsync(ushort slaveID, ushort startAddress, ushort[] data)
        {
            byte id = (byte)slaveID;
            await modbusMaster.WriteMultipleRegistersAsync(id, startAddress, data);
        }
        public async Task<ushort[]> ReadInputRegistersAsync(ushort slaveID, ushort startAddress, ushort numberOfPoints)
        {
            byte id = (byte)slaveID;
            return await modbusMaster.ReadInputRegistersAsync(id, startAddress, numberOfPoints);
        }

    }

}
