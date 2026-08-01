using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Text;
using System.Text.RegularExpressions;

namespace ThreeNH.SQCX.Infrastructure.Instruments.YL30
{
    public class SerialPortInformation
    {
        // STM32 设备
        public const int STM_CDC_PORT_VID = 0x0483;
        public const int STM_CDC_PORT_PID = 0x5740;
        // 展讯设备
        public const int SCI_USB_PORT_VID = 0x1782;
        public const int SCI_USB_PORT_PID = 0x4D00;

        public SerialPortInformation()
        {
            // do nothing
        }

        /// <summary>
        /// 用给定串口的信息构造一个实例
        /// </summary>
        /// <param name="portName">串口ID号（COM + 数字）</param>
        public SerialPortInformation(string portName)
        {
            PortName = portName;

            ManagementObjectSearcher searcher = new ManagementObjectSearcher("root\\cimv2",
                "SELECT * FROM Win32_PnPEntity WHERE ClassGuid=\"{4d36e978-e325-11ce-bfc1-08002be10318}\"");

            portName = portName.ToUpper();
            foreach (ManagementObject queryObj in searcher.Get())
            {
                string caption = queryObj["Caption"].ToString()?.ToUpper();
                if (caption == null) continue;

                if (string.Compare(portName, queryObj["DeviceID"].ToString()) == 0 || caption.Contains(portName))
                {
                    PortName = portName;
                    Description = queryObj["Description"].ToString();
                    Caption = queryObj["Caption"].ToString();
                    DeviceID = queryObj["DeviceID"].ToString();
                    break;
                }
            }
        }

        public string PortName { get; set; }
        public string Description { get; set; }
        public string Caption { get; set; }
        //public string PNPDeviceID;
        //public uint MaxBaudRate;
        public string DeviceID { get; set; }

        public int VID
        {
            get
            {
                if (DeviceID != null)
                {
                    var match = Regex.Match(DeviceID, @"VID_([0-9a-f]{4})", RegexOptions.IgnoreCase);
                    if (match.Success)
                    {
                        return int.Parse(match.Groups[1].Value, System.Globalization.NumberStyles.HexNumber);
                    }
                }
                return -1;
            }
        }

        public int PID
        {
            get
            {
                if (DeviceID != null)
                {
                    var match = Regex.Match(DeviceID, @"PID_([0-9a-f]{4})", RegexOptions.IgnoreCase);
                    if (match.Success)
                    {
                        return int.Parse(match.Groups[1].Value, System.Globalization.NumberStyles.HexNumber);
                    }
                }
                return -1;
            }
        }
        // 是否是蓝牙狗设备描述符
        public bool IsBluetoothDonglePort()
        {
            return string.Compare(Description, "USB-SERIAL CH340", StringComparison.CurrentCultureIgnoreCase) == 0
                || string.Compare(Description, "USB-SERIAL CH341", StringComparison.CurrentCultureIgnoreCase) == 0;
        }
        // 是否是STM32平台设备
        public bool IsSTM32VirtualPort()
        {
            return (string.Compare(Description, "STMicroelectronics Virtual COM Port", StringComparison.CurrentCultureIgnoreCase) == 0)
                || (PID == STM_CDC_PORT_PID && VID == STM_CDC_PORT_VID);
        }
        // 是否是展讯平台
        public bool IsSCIUsb2Serial()
        {
            return string.Compare(Description, "SCI USB2Serial", StringComparison.CurrentCultureIgnoreCase) == 0
                || (PID == SCI_USB_PORT_PID && VID == SCI_USB_PORT_VID);
        }

        // 是否是USB转串口
        public bool IsUsb2SerialPort()
        {
            return string.Compare(Description, "Prolific USB-to-Serial Comm Port", StringComparison.CurrentCultureIgnoreCase) == 0;
        }
    }
}
