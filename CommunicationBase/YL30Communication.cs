using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using static ThreeNH.CommunicationBase.EnumDefines;

namespace ThreeNH.CommunicationBase
{
    [XmlRoot("YL30Config")]
    public  class YL30Communication
    {
        public YL30Communication()
        {
            CommType = EnumDefines.CommInterfaceType.USB;
            BaudRate = EnumDefines.BaudRate._115200;
            DataBits = EnumDefines.DataBits._8;
            StopBit = EnumDefines.StopBit.StopBit1;
            Parity = EnumDefines.Parity.NoneParity;
            IP = "192.168.3.122";
            Port = 8080;
        }
        [XmlAttribute] public CommInterfaceType CommType { get; set; }
        [XmlAttribute] public string IP { get; set; }
        [XmlAttribute] public int Port { get; set; }
        [XmlAttribute] public string SerialComPort { get; set; }
        [XmlAttribute] public BaudRate BaudRate { get; set; }
        [XmlAttribute] public DataBits DataBits { get; set; }
        [XmlAttribute] public Parity Parity { get; set; }
        [XmlAttribute] public StopBit StopBit { get; set; }
        [XmlAttribute] public ushort LastSlaveID { get; set; }



        /* ---------- 存 / 取 ---------- */
        public void Save(string filePath)
        {
            var xs = new XmlSerializer(typeof(YL30Communication));
            using (var fs = new StreamWriter(filePath, false, Encoding.UTF8))
                xs.Serialize(fs, this);
        }

        public static YL30Communication Load(string filePath)
        {
            if (!File.Exists(filePath)) return new YL30Communication();
            var xs = new XmlSerializer(typeof(YL30Communication));
            using (var fs = new StreamReader(filePath, Encoding.UTF8))
                return (YL30Communication)xs.Deserialize(fs);
        }
    }

    public class EnumDefines
    {
        public enum CommInterfaceType
        {
            [Description("USB")]
            USB,
            //[Description("RS232")]
            //RS232,
            //[Description("RS485")]
            //RS485,
            [Description("Modbus TCP/IP")]
            TCPIP,
            [Description("Socket")]
            Socket
        }
        public enum BaudRate
        {
            [Description("300")]
            _300 = 300,
            [Description("600")]
            _600 = 600,
            [Description("1200")]
            _1200 = 1200,
            [Description("2400")]
            _2400 = 2400,
            [Description("4800")]
            _4800 = 4800,
            [Description("9600")]
            _9600 = 9600,
            [Description("19200")]
            _19200 = 19200,
            [Description("38400")]
            _38400 = 38400,
            [Description("57600")]
            _57600 = 57600,
            [Description("115200")]
            _115200 = 115200
        }
        public enum DataBits
        {
            [Description("7")]
            _7 = 7,
            [Description("8")]
            _8 = 8
        }
        public enum Parity
        {
            [Description("None Parity")]
            NoneParity,
            [Description("Odd Parity")]
            OddParity,
            [Description("Even Parity")]
            EvenParity,
        }
        public enum StopBit
        {
            [Description("1 Stop Bit")]
            StopBit1,
            [Description("2 Stop Bit")]
            StopBit2,
        }
    }

   
}
