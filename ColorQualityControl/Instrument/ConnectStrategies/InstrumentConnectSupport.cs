﻿﻿using ThreeNH.SQCX.Infrastructure.Instruments.PTS;
using System;
using System.Diagnostics;
using System.Text;
using ThreeNH.BleProtocol;
using ThreeNH.Bluetooth;
using ThreeNH.ColorimeterInterface;
using ThreeNH.CommDevice;
using ThreeNH.CommunicationBase;
using ThreeNH.PortService;
using ThreeNH.SQCX.Infrastructure.Instruments.Spectrophotometer;
using  param($mt) $map[$mt.Value] ;
using YHComm = ThreeNH.YHComm;

namespace ThreeNH.ColorQualityControl.ConnectStrategies
{
    /// <summary>
    /// 各连接策略共用的仪器探测与实例化逻辑（自 ConnectionWork 迁出）。
    /// </summary>
    internal static class InstrumentConnectSupport
    {
        internal static bool IsSTM32VirtualPort(SerialPortInformation portInfo) => portInfo.IsSTM32VirtualPort();

        internal static bool IsSCIUsb2Serial(SerialPortInformation portInfo) => portInfo.IsSCIUsb2Serial();

        internal static Tuple<IInstrument, InstrumentType> CreateInstrumentInstance(ICommDevice commDevice)
        {
            var type = InstrumentTypeDetector.Detect(commDevice);
            Console.WriteLine($"The instrument type is {(InstrumentType)type}");
            Trace.TraceInformation($"仪器类型是{(InstrumentType)type}");
            IInstrument instrument = null;
            switch (type)
            {
                case InstrumentType.NR:
                    instrument = new NRColorimeter.Colorimeter();
                    break;
                case InstrumentType.NS:
                    instrument = new LedSpectrometer.Spectrometer();
                    break;
                case InstrumentType.YS:
                    instrument = new ThreeNH.SQCX.Infrastructure.Instruments.GratingSpectrometer.Spectrometer();
                    break;
                case InstrumentType.BTS:
                    instrument = new ThreeNH.SQCX.Infrastructure.Instruments.BTS.Spectrometer();
                    break;
                case InstrumentType.PTS:
                    instrument = new ThreeNH.SQCX.Infrastructure.Instruments.PTS.Spectrometer();
                    break;
                case InstrumentType.YH:
                    instrument = new YHComm.USBComm();
                    break;
                case InstrumentType.CR:
                    instrument = new ColorReader.Spectrometer();
                    break;
                case InstrumentType.JSON:
                    instrument = new TS8450(new JsonCommProtocol());
                    break;
                default:
                    return null;
            }

            try
            {
                instrument.Open(commDevice);
            }
            catch (CommunicationException)
            {
                return null;
            }

            return new Tuple<IInstrument, InstrumentType>(instrument, type);
        }

        internal static Tuple<IInstrument, InstrumentType> TryConnectBtsBle()
        {
            if (!BleProtocolWrapper.IsSupported)
            {
                return null;
            }

            try
            {
                var handle = BleProtocolWrapper.tnhble_Connect(0);
                if (handle == IntPtr.Zero)
                {
                    return null;
                }

                StringBuilder buffer = new StringBuilder(20);
                if (BleProtocolWrapper.tnhble_GetPnpId(handle, buffer, buffer.Capacity) &&
                    string.Compare(buffer.ToString(), BleProtocolWrapper.PTS_PNP_ID, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    return new Tuple<IInstrument, InstrumentType>(new ThreeNH.SQCX.Infrastructure.Instruments.PTS.BleSpectrophotometer(handle), InstrumentType.PTS);
                }

                return new Tuple<IInstrument, InstrumentType>(new ThreeNH.SQCX.Infrastructure.Instruments.BTS.BleSpectrophotometer(handle), InstrumentType.BTS);
            }
            catch (Exception)
            {
                return null;
            }
        }

        internal static Tuple<IInstrument, InstrumentType> TryConnectYS30Bluetooth(object deviceAddress)
        {
            var device = new BluetoothDevice();
            device.Open(deviceAddress);
            return CreateInstrumentInstance(device);
        }
    }
}
