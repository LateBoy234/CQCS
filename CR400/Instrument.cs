using System;
using System.Collections.Generic;
using System.IO.Ports;
using ThreeNH.ColorAlgorithm;
using ThreeNH.ColorimeterInterface;
using ThreeNH.ColorSpecimen;
using ThreeNH.CommDevice;
using ThreeNH.CommunicationBase;
using ThreeNH.Optical;

namespace ThreeNH.CR400
{
    public class Instrument : 
        IInstrument
    {
        private SerialPort _serialPort;

        public bool IsCalibrated { get; private set; } = true;


        public void DoWhiteCalibration()
        {
            try
            {
                _serialPort.WriteLine("CAL,00");
                _serialPort.ReadTimeout = 15000;
                var result = _serialPort.ReadLine();
                if (!result.StartsWith("OK"))
                {
                    throw new CommunicationException(CommunicationError.CalibrationFailed, result);
                }

                IsCalibrated = true;
            }
            catch (TimeoutException)
            {
                throw new CommunicationException(CommunicationError.ReceivingTimeout);
            }
            catch (InvalidOperationException)
            {
                throw new CommunicationException(CommunicationError.DeviceNotOpen);
            }
            catch (Exception ex)
            {
                throw new CommunicationException(CommunicationError.UnknownError, ex.Message, ex);
            }
        }

        public void DoBlackCalibration()
        {
            // 不需要黑校
            return;
        }

        public Specimen Measure()
        {
            try
            {
                _serialPort.WriteLine("MES");
                _serialPort.ReadTimeout = 2000;
                var result = _serialPort.ReadLine();
                if (result.StartsWith("OK"))
                {
                    var arr = result.Split(new[] {','});

                    double x = int.Parse(arr[1]) * 0.001;
                    double y = int.Parse(arr[2]) * 0.001;
                    double z = int.Parse(arr[3]) * 0.001;

                    return new Specimen
                    {
                        SCI = new ColorData(StandardIlluminant.C, StandardObserver.CIE1931, new CIEXYZ(x, y, z))
                    };
                }
                {
                    throw new CommunicationException(CommunicationError.ExecuteCommandFailed);
                }
            }
            catch (TimeoutException)
            {
                throw new CommunicationException(CommunicationError.ReceivingTimeout);
            }
            catch (InvalidOperationException)
            {
                throw new CommunicationException(CommunicationError.DeviceNotOpen);
            }
            catch (Exception ex)
            {
                throw new CommunicationException(CommunicationError.UnknownError, ex.Message, ex);
            }
        }

        
        public void ReferenceDeviceInfo()
        {
            try
            {
                _serialPort.WriteLine("IDR");
                _serialPort.ReadTimeout = 2000;
                var result = _serialPort.ReadLine();
                if (result.StartsWith("OK"))
                {
                    var arr = result.Split(new[] {','});
                    Model = arr[1];
                    SoftwareRevision = arr[2];
                    SN = arr[3];
                }
                else
                {
                    throw new CommunicationException(CommunicationError.ExecuteCommandFailed);
                }
            }
            catch (TimeoutException)
            {
                throw new CommunicationException(CommunicationError.ReceivingTimeout);
            }
            catch (InvalidOperationException)
            {
                throw new CommunicationException(CommunicationError.DeviceNotOpen);
            }
            catch (Exception ex)
            {
                throw new CommunicationException(CommunicationError.UnknownError, ex.Message, ex);
            }
        }

        public Specimen MeasureStandard()
        {
            return Measure();
        }

        public Specimen MeasureSample()
        {
            return Measure();
        }

        public string Model { get; private set; } = "CR400";
        public string SN { get; private set; }
        public string HardwareRevision { get; } = "";
        public string FirmwareRevision => SoftwareRevision;
        public string SoftwareRevision { get; private set; }
        public string Manufacturer { get; } = "KONICA MINOLTA";
        public string OpticalStructure { get; } = "d/8";
        public string WhiteBoardNumber { get; } = "";
        public DeviceType DeviceType { get; } = DeviceType.Colorimeter;
        
        public WavelengthRange WavelengthRange { get; } = new WavelengthRange(0, 0, 0);

        public string DeviceName => _serialPort?.PortName;

        public bool IsOpen => _serialPort?.IsOpen ?? false;

        public void Open(object commDevice = null)
        {
            if (commDevice is SerialPort serialPort)
            {
                _serialPort = serialPort;
            }
            else if (commDevice is string portName)
            {
                _serialPort = new SerialPort(portName, 9600, Parity.None);
            }
            else
            {
                throw   new ArgumentException();
            }

            try
            {
                _serialPort.NewLine = "\r\n";
                _serialPort.RtsEnable = true;

                if (!_serialPort.IsOpen)
                {
                    _serialPort.Open();
                }
            }
            catch (Exception e)
            {
                throw new CommunicationException(CommunicationError.DeviceOpenFailed, null, e);
            }

            ReferenceDeviceInfo();
        }

        public void RefreshInstrumentInfo() => ReferenceDeviceInfo();

        public void Close()
        {
            _serialPort.Close();
            _serialPort = null;
        }

    }
}
