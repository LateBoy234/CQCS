using System;
using System.Diagnostics;
using System.IO.Ports;
using System.Threading;
using ThreeNH.Bluetooth;
using ThreeNH.ColorimeterInterface;
using ThreeNH.ColorQualityControl.Device;
using ThreeNH.CommDevice;
using ThreeNH.PortService;
using ThreeNH.Spectrophotometer;
using ThreeNH.Spectrophotometer.ProtocolImplementation;
using YHComm = ThreeNH.YHComm;
using static ThreeNH.CommunicationBase.EhMc17AtInterface;

using ThreeNH.ColorQualityControl.Services;

namespace ThreeNH.ColorQualityControl.ConnectStrategies
{
    internal sealed class Bluetooth5InstrumentConnectStrategy : IInstrumentConnectStrategy
    {
        public ConnectionMethod Method => ConnectionMethod.ByBluettooth_5;

        public void Connect(InstrumentConnectContext context)
        {
            context.Host.SetConnectWay("Bluetooth");
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
            context.ReportProgress(ConnectionProgressRequest.DongleSelectSlaveDevice);
            context.WaitForDeviceSelection();
            if (context.SelectedDevice == null)
            {
                return;
            }

            SlaveDevice slaveDevice = context.SelectedDevice as SlaveDevice;
            context.ReportProgress(
                string.Format("Trying connecting to: {0}, {1}...", slaveDevice.Name, slaveDevice.Address));

            Thread.Sleep(2000);
            var serialPort = new SerialPort(context.SelectedPortName, 115200);
            try
            {
                serialPort.Open();

                var detectedType = BleInstrumentDetector.Detect(serialPort);
                if (TryConnectDongleInstrument(context, slaveDevice, serialPort, detectedType))
                {
                    // 成功后把局部变量设为 null，是为了阻止 finally 关闭串口：
                    // SerialPort 所有权转交给创建出的仪器对象
                    serialPort = null;
                    return;
                }

                context.ReportProgress($"InstrumentType:{detectedType}");
                Thread.Sleep(2000);
            }
            catch (Exception ex)
            {
                Trace.TraceInformation("Trying connecting to \"{0}\" failed: {1}", context.SelectedPortName, ex.Message);
            }
            finally
            {
                if (serialPort != null && serialPort.IsOpen)
                {
                    //连接失败：
                   // Connect 方法负责关闭 SerialPort
                    serialPort.Close();
                }

                context.SelectedDevice = null;
            }

            context.ReportProgress("Checking device type again,please wait for a moment...");
            Thread.Sleep(2000);
            SerialPortDevice commDevice = new SerialPortDevice();
            try
            {
                commDevice.Open(context.SelectedPortName);
                var result = InstrumentConnectSupport.CreateInstrumentInstance(commDevice);
                if (result != null)
                {
                    context.Host.SetDeviceSn($"SN:{slaveDevice.Name} Mac:{slaveDevice.Address}");
                    context.SetResult(result);
                    commDevice = null;
                }
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

        private static bool TryConnectDongleInstrument(
            InstrumentConnectContext context,
            SlaveDevice slaveDevice,
            SerialPort serialPort,
            InstrumentType detectedType)
        {
            string successMessage = string.Format("Connecting successfully\"{0}, {1}\"", slaveDevice.Name, slaveDevice.Address);
            string deviceSn = $"SN:{slaveDevice.Name} Mac:{slaveDevice.Address}";

            if (detectedType == InstrumentType.PTS)
            {
                Thread.Sleep(1200);
                var spec = new PTS.BluetoothDongleSpectrometer();
                spec.AttatchSerialPort(serialPort);
                context.SetResult(new Tuple<IInstrument, InstrumentType>(spec, InstrumentType.PTS));
                context.ReportProgress(100, successMessage);
                context.Host.SetDeviceSn(deviceSn);
                return true;
            }

            if (detectedType == InstrumentType.BTS)
            {
                Thread.Sleep(1200);
                var spec = new BTS.BluetoothDongleSpectrometer();
                spec.AttatchSerialPort(serialPort);
                context.SetResult(new Tuple<IInstrument, InstrumentType>(spec, InstrumentType.BTS));
                context.ReportProgress(100, successMessage);
                context.Host.SetDeviceSn(deviceSn);
                return true;
            }

            if (detectedType == InstrumentType.YH)
            {
                Thread.Sleep(1200);
                var spec = new YHComm.BleComm();
                spec.AttatchSerialPort(serialPort);
                context.SetResult(new Tuple<IInstrument, InstrumentType>(spec, InstrumentType.YH));
                context.ReportProgress(100, successMessage);
                context.Host.SetDeviceSn(deviceSn);
                return true;
            }

            if (detectedType == InstrumentType.JSON)
            {
                SerialPortDevice device = new SerialPortDevice(serialPort);
                var ts8450 = new TS8450(new JsonCommProtocol());
                ts8450.Open(device);
                context.SetResult(new Tuple<IInstrument, InstrumentType>(ts8450, InstrumentType.JSON));
                context.ReportProgress(100, successMessage);
                context.Host.SetDeviceSn(deviceSn);
                return true;
            }

            if (detectedType == InstrumentType.CR)
            {
                Thread.Sleep(1200);
                var spec = new ColorReader.BluetoothDongleSpectrometer();
                spec.AttatchSerialPort(serialPort);
                context.SetResult(new Tuple<IInstrument, InstrumentType>(spec, InstrumentType.CR));
                context.ReportProgress(100, successMessage);
                context.Host.SetDeviceSn(deviceSn);
                return true;
            }

            return false;
        }
    }
}
