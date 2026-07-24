using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Ports;
using ThreeNH.ColorimeterInterface;
using ThreeNH.ColorQualityControl.Device;
using ThreeNH.CommDevice;
using ThreeNH.PortService;

namespace ThreeNH.ColorQualityControl.ConnectStrategies
{
    internal sealed class UsbInstrumentConnectStrategy : IInstrumentConnectStrategy
    {
        public ConnectionMethod Method => ConnectionMethod.ByUSB;

        public void Connect(InstrumentConnectContext context)
        {
            var driverDetectorResult = SpreadtrumDriverDetector.CheckInstalledDriver();
            if (!driverDetectorResult.IsInstalled)
            {
                context.Host.ShowDriverDownloadDialog();
            }

            context.Host.SetConnectWay("USB");
            List<string> portNames = new List<string>();
            if (context.Address is string portName)
            {
                Console.WriteLine($"find out com:{portName}");
                portNames.Add(portName);
            }
            else
            {
                context.ReportProgress("Looking up device...");
                foreach (var portInfo in SerialPortService.SerialPorts)
                {
                    if (!InstrumentConnectSupport.IsSTM32VirtualPort(portInfo) && !InstrumentConnectSupport.IsSCIUsb2Serial(portInfo))
                    {
                        Console.WriteLine($"{portInfo.PortName}can't connection");
                        Trace.TraceInformation($"{portInfo.PortName}无法连接");
                        continue;
                    }

                    portNames.Add(portInfo.PortName);
                }
            }

            if (portNames.Count == 0)
            {
                Console.WriteLine("Cannot find out valid serial ports");
                Trace.TraceInformation("Cannot find out valid serial ports");
                return;
            }

            foreach (var port in portNames)
            {
                Console.WriteLine("Checking device type...");
                context.ReportProgress("Checking device type...");
                SerialPortDevice commDevice = new SerialPortDevice();
                try
                {
                    Console.WriteLine($"Open Com: {port}");
                    commDevice.Open(port);
                    var result = InstrumentConnectSupport.CreateInstrumentInstance(commDevice);
                    if (result != null)
                    {
                        context.SetResult(result);
                        commDevice = null;
                        return;
                    }
                }
                catch (Exception ex)
                {
                    Trace.TraceInformation("Trying connecting to \"{0}\" failed: {1}", port, ex.Message);
                    Console.WriteLine($"Exception Information: {ex.Message}");
                }
                finally
                {
                    commDevice?.Close();
                }
            }
        }
    }
}
