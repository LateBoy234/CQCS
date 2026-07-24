using System;
using System.Diagnostics;
using System.IO.Ports;
using System.Threading;
using ThreeNH.ColorimeterInterface;
using ThreeNH.CommDevice;
using ThreeNH.CommunicationBase;
using ThreeNH.PortService;
using ThreeNH.Spectrophotometer;
using ThreeNH.Spectrophotometer.ProtocolImplementation;
using ThreeNH.YL30;
using static ThreeNH.CommunicationBase.EhMc17AtInterface;

using ThreeNH.ColorQualityControl.Services;

namespace ThreeNH.ColorQualityControl.ConnectStrategies
{
    internal sealed class Yl30InstrumentConnectStrategy : IInstrumentConnectStrategy
    {
        public ConnectionMethod Method => ConnectionMethod.ByYL30Device;

        public void Connect(InstrumentConnectContext context)
        {
            if (context.Address is string portName)
            {
                _ = new SerialPortInformation(portName);
            }
            else
            {
                context.ReportProgress("Looking up device...");
                _ = SerialPortService.SerialPorts;
            }

            context.ResetDeviceSelectionWait();
            context.ReportProgress(ConnectionProgressRequest.YL30SpecialDevice);
            context.WaitForDeviceSelection();
            if (context.SelectedSpecialDevice == null)
            {
                return;
            }

            YL30Communication slaveDevice = context.SelectedSpecialDevice;

            if (slaveDevice.CommType == EnumDefines.CommInterfaceType.Socket)
            {
                ConnectSocket(context, slaveDevice);
                return;
            }

            if (slaveDevice.CommType == EnumDefines.CommInterfaceType.USB)
            {
                ConnectUsb(context, slaveDevice);
                return;
            }

            ConnectModbusTcp(context, slaveDevice);
        }

        private static void ConnectSocket(InstrumentConnectContext context, YL30Communication slaveDevice)
        {
            context.Host.SetConnectWay("Socket");
            context.ReportProgress(string.Format("Trying connecting to: {0}...", slaveDevice.IP));
            try
            {
                var ts8450 = new TS8450(new SocketCommProtocol());
                ts8450.Open($"{slaveDevice.IP}:{slaveDevice.Port}");
                context.SetResult(new Tuple<IInstrument, InstrumentType>(ts8450, InstrumentType.JSON));
                context.ReportProgress(100, string.Format("Connecting successfully\"{0}\"", slaveDevice.IP));
                context.Host.SetDeviceSn($"SN:{ts8450.SN}");
            }
            catch (Exception ex)
            {
                Trace.TraceInformation("Trying connecting to \"{0}\" failed: {1}", context.SelectedPortName, ex.Message);
            }
            finally
            {
                context.SelectedSpecialDevice = null;
            }
        }

        private static void ConnectUsb(InstrumentConnectContext context, YL30Communication slaveDevice)
        {
            context.Host.SetConnectWay("USB");
            context.ReportProgress(string.Format("Trying connecting to: {0}...", slaveDevice.SerialComPort));

            Thread.Sleep(2000);
            if (slaveDevice.SerialComPort == null)
            {
                return;
            }

            var serialPort = new SerialPort(slaveDevice.SerialComPort, slaveDevice.Port);
            try
            {
                serialPort.Open();
            }
            catch (Exception ex)
            {
                Trace.TraceInformation("Trying connecting to \"{0}\" failed: {1}", context.SelectedPortName, ex.Message);
            }
            finally
            {
                if (serialPort != null && serialPort.IsOpen)
                {
                    serialPort.Close();
                }

                context.SelectedSpecialDevice = null;
            }

            context.ReportProgress("Checking device type again,please wait for a moment...");
            Thread.Sleep(2000);
            SerialPortDevice commDevice = new SerialPortDevice
            {
                BaudRate = (int)slaveDevice.BaudRate
            };
            try
            {
                commDevice.Open(context.SelectedPortName);
                IInstrument instrument = new SpectrometerForYL30(slaveDevice, commDevice);
                InstrumentType type = InstrumentType.YL30;
                if (instrument.SN == null)
                {
                    return;
                }

                var result = new Tuple<IInstrument, InstrumentType>(instrument, type);
                context.Host.SetDeviceSn($"SN:{result.Item1.SN}");
                context.SetResult(result);
                commDevice = null;
            }
            catch (Exception ex)
            {
                Trace.TraceInformation("Trying connecting to \"{0}\" failed: {1}", context.SelectedPortName, ex.Message);
            }
            finally
            {
                commDevice?.Close();
            }
        }

        private static void ConnectModbusTcp(InstrumentConnectContext context, YL30Communication slaveDevice)
        {
            context.Host.SetConnectWay("ModbusTCP");
            context.ReportProgress(string.Format("Trying connecting to: {0}...", slaveDevice.IP));

            try
            {
                IInstrument instrument = new SpectrometerForYL30(slaveDevice);
                instrument.Open();
                InstrumentType type = InstrumentType.YL30;
                if (instrument.SN == null)
                {
                    return;
                }

                var result = new Tuple<IInstrument, InstrumentType>(instrument, type);
                context.Host.SetDeviceSn($"SN:{result.Item1.SN}");
                context.SetResult(result);
            }
            catch (Exception ex)
            {
                Trace.TraceInformation("Trying connecting to \"{0}\" failed: {1}", context.SelectedPortName, ex.Message);
            }
            finally
            {
                context.SelectedSpecialDevice = null;
            }
        }
    }
}
