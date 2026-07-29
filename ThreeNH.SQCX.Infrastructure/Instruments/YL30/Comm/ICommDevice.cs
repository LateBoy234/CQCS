﻿using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThreeNH.SQCX.Infrastructure.Instruments.YL30.Legacy
{
    public delegate void ReceivedAsyncCallBack(object sender, byte[] bytesReceived,Exception ex);
    public interface ICommDevice
    {
        SerialPort SerialPort { get;}
        void Open(object device = null);
        void Close();
        string DriverDescription { get; set; }
        string DeviceName { get;}
        bool IsOpen { get; }
        bool IsBluetoothSerialPortDevice { get; }
        Stream stream { get; }
        int Read(byte[] buffer, int offset, int count);
        void Write(byte[] buffer, int offset, int count);
        bool BeginReceiveAsync(ReceivedAsyncCallBack callback);

        bool EndReceiveAsync();   
    }
}
