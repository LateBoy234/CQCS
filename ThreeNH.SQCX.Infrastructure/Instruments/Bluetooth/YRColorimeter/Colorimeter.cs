using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ThreeNH.CommunicationBase;
using ThreeNH.ColorimeterInterface;
using System.Diagnostics;
using System.Threading;
using ThreeNH.ColorSpecimen;
using ThreeNH.ColorAlgorithm;
using ThreeNH.Optical;
using ThreeNH.Instruments.DeviceInfo;

namespace ThreeNH.SQCX.Infrastructure.Instruments.YRColorimeter
{
    enum Command : byte
    {
        GetDeviceInfo = 0XA1,     // 获取设备信息
        SetMeasurementParameter,  //A2  设置测量参数
        SetChromaticitySetting,   //A3  设置颜色空间、色差公式、容差
        GetChromaticitySetting,   //A4  获取颜色空间、容差设置等信息
        Calibrate,                //A5  校正
        Measure,                  //A6  测量
        UploadSpecimen,               //A7  通知准备上传数据的条数和种类
        TransportSpecimen,            //A8  通知仪器继续上传数据
        StopTransportSpecimen,        //A9  通知仪器中断数据上传
        SpecimenPacket,               //AA  仪器在上传数据时发送过来的数据包
        DeleteSpecimen,               //AB  删除数据
        SNPasswordCheck,          //AC  废弃的命令
        WriteStandard,            //AD  写入标样
        GetMeasurementParamter,	  //AE  获取测量参数
        SetDateTime,              // AF 设置仪器时间
        GetSamplingData,          // B0 获取原始采样数据
        SetSensorLimit,         // 0XB1 设置黑白校正时的感应器限制
        GetSensorLimit,         // 0XB2 获取黑白校正时的感应器限制
        AutoMakeWhiteLimit,     // 0XB3 自动白板的限定值
        AutoMakeBlackLimit,	    // 0XB4 自动生成个黑板的限定值
        SetWhiteBoardParameter, // 0xb5 设置白板参数
        GetWhiteBoardParameter,  // 0xb6 获取白板参数
        SetWhiteBoardSn,     // 0xb7 设置白板编号
        GetWhiteBoardSn,		// 0xb8 获取白板编号
        SetLanguage = 0xB9,         // B9 设置仪器显示语言
        RestoreFactorySettings,     // BA 恢复仪器出产设置
        SetCorrectionFactors,      // 0xBB 设置修正矩阵
        GetCorrectionFactors,      // 0xBC 获取修正矩阵
        SetWhiteBoardXyz,        // 0xBD 设置白板XYZ值
        GetWhiteBoardXyz,        // 0xBE 读出白板XYZ值
        YrGetSamplingData,		// 0xBF 获取原始采样值
        SetFactoryInfo,           // 0xC0 设置序列号、仪器型号名等出厂信息
        GetFactoryInfo,           // 0Xc1 读取SET_FACTORY_INFO写入的信息
        SetInternalModel,         // 0xC2 设置仪器型号
        GetInternalModel,         // 0xC3 读取仪器型号
        SetLocationImagePosition, // 0xC4 设置相机定位图像位置
        GetLocationImagePosition, // 0xC5 获取定位图像位置
        SetIntegrationTime,	        // 0xC6 设置积分时间
        GetIntegrationTime,			// 0xC7获取积分时间
        SetGain,		            // 0xC8 设置增益
        GetGain,		            // 0xC9 获取增益
        NullCommand             // 无效命令
    }

    class NRColorimterPacketParser : PacketParser
    {
        protected override bool HasAck(byte command)
        {
            return command != (byte)Command.SpecimenPacket;
        }

        protected override bool IsValidCommand(byte command)
        {
            return command >= (byte)Command.GetDeviceInfo && command < (byte)Command.NullCommand;
        }
    }


    public class Colorimeter : InstrumentBase, IColorimeter, ISpecimenClearing, IInstrumentMaintenance
    {
        private ThreeNH.ColorAlgorithm.Formula formula = new ColorAlgorithm.Formula();
        int _stopUpload = 0;    // 停止异步上传标志
        public event EventHandler<SpecimenReceivedEventArgs> MeasureResultReceived;

        protected override byte AckCommand(byte command)
        {
            switch ((Command)command)
            {
                case Command.TransportSpecimen: return (byte)Command.SpecimenPacket;
                case Command.StopTransportSpecimen: return 0;
                default: return command;
            }
        }


        NRColorimterPacketParser parser = new NRColorimterPacketParser();
        protected override PacketParser PacketParser
        {
            get
            {
                return parser;
            }
        }

        public string SN { get; private set; }

        public string Model { get; private set; }

        public string HardwareRevision { get; private set; }
        public string FirmwareRevision { get; private set; }
        public string SoftwareRevision
        {
            get { return FirmwareRevision; }
        }
        public string Manufacturer { get;}
        public string OpticalStructure { get; private set; }
        public string WhiteBoardNumber { get; private set; }
        public DeviceType DeviceType
        {
            get { return DeviceType.Colorimeter; }
        }

        Optical.SpecularComponent _specularComponent = Optical.SpecularComponent.Unknown;
        public ThreeNH.Optical.SpecularComponent SpecularComponent
        {
            get
            {
                return _specularComponent;
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public ThreeNH.Instruments.DeviceInfo.WavelengthBand WavelengthRange
        {
            get
            {
                //throw new NotImplementedException();
                return new ThreeNH.Instruments.DeviceInfo.WavelengthBand(0, 0, 0);
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        public ThreeNH.Optical.UVFlag UV
        {
            get
            {
                return Optical.UVFlag.None;
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        public ThreeNH.Optical.MeasurementCaliber Aperture
        {
            get;
            private set;
        }

        public bool IsUVControllable
        {
            get
            {
                return false;
            }
        }

        public bool IsSpecularComponentModifiable
        {
            get
            {
                return false;
            }
        }

        public bool IsWavelengthRangeControllable
        {
            get
            {
                return false;
            }
        }

        protected override void OnPacketArrived(DataPacket packet, Exception ex)
        {

        }

        public void Connect()
        {
            Connect(null);
        }

        // 仪器光学结构字典
        static readonly Dictionary<string, List<string>> _instrumentOpticalStructureDict =
            new Dictionary<string, List<string>>
            {
                {"8/d", new List<string> {"310", "300", "200", "100", "110", "60CP", "10QC" } },
                {"45/0", new List<string> { "145", "20XE" } },
            };
        // 由于某些仪器的口径信息不正确且其有固定口径，将其放在口径字典中
        static readonly Dictionary<Instrument.MeasurementCaliber, List<string>> _instrumentCaliberDict =
            new Dictionary<Instrument.MeasurementCaliber, List<string>>
            {
                { Instrument.MeasurementCaliber.Phi4, new List<string> { "10QC", "110",  "100"} },
                { Instrument.MeasurementCaliber.Phi20, new List<string> { "20XE" } },
            };

        static string DetectOpticalStruct(string deviceName)
        {
            deviceName = deviceName.ToUpper();
            foreach (var item in _instrumentOpticalStructureDict)
            { 
                foreach (var modle in item.Value)
                {
                    if (deviceName.Contains(modle))
                    {
                        return item.Key;
                    }
                }
            }
            return null;
        }

        static Instrument.MeasurementCaliber DetectCaliber(string deviceName)
        {
            deviceName = deviceName.ToUpper();
            foreach (var item in _instrumentCaliberDict)
            {
                foreach (var modle in item.Value)
                {
                    if (deviceName.Contains(modle))
                    {
                        return item.Key;
                    }
                }
            }
            return Instrument.MeasurementCaliber.Phi8;
        }

        public override void Connect(object device)
        {
            base.Connect(device);

            try
            {
                RefreshInstrumentInfo();
            }
            catch(Exception)
            {
                Disconnect();
                //throw;
            }
        }

        public ThreeNH.ColorSpecimen.Specimen Measure()
        {
            PacketContent content = new PacketContent();
            content.Enqueue((byte)1);

            var result = SendCommand((byte)Command.Measure, content);
            if (result != null && result.IsAckOk)
            {
                float l, a, b, c, h;
                result.PacketContent.Dequeue(out l);
                result.PacketContent.Dequeue(out a);
                result.PacketContent.Dequeue(out b);
                result.PacketContent.Dequeue(out c);
                result.PacketContent.Dequeue(out h);

                var specimen = new ThreeNH.ColorSpecimen.Specimen();
                var colorData = new ThreeNH.ColorSpecimen.ColorData();
                var xyz = formula.LabToXyz(new ColorAlgorithm.CIELAB(l, a, b));
                colorData.Set(formula.Observer, formula.Illuminant, xyz);
                if (SpecularComponent == Optical.SpecularComponent.SCI)
                    specimen.SCI = colorData;
                else if (SpecularComponent == Optical.SpecularComponent.SCE)
                    specimen.SCE = colorData;
                else
                    specimen.Clr = colorData;

                return specimen;
            }
            return null;
        }

        public Specimen MeasureStandard()
        {
            return Measure();
        }

        public Specimen MeasureSample()
        {
            return Measure();
        }

        public void DoBlackCalibration()
        {
            PacketContent content = new PacketContent();
            content.Enqueue((byte)0);
            var result = SendCommand((byte)Command.Calibrate, content);

            if (result == null || !result.IsAckOk)
            {
                throw new CommunicationException(CommunicationError.CalibrationFailed,
                    string.Format("Black calibration failed: {0}", result.Ack));
            }
        }

        public bool IsCalibrated
        {
            get
            {
                var devInfo = GetDeviceInfo();
                return devInfo.IsBlackCalibrated && devInfo.IsWhiteCalibrated;
            }
        }

        public bool YTContain400 { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        ThreeNH.Instruments.DeviceInfo.WavelengthBand IDeviceInfo.WavelengthRange => throw new NotImplementedException();

        public void DoWhiteCalibration()
        {
            PacketContent content = new PacketContent();
            content.Enqueue((byte)1);
            var result = SendCommand((byte)Command.Calibrate, content);
            if (result == null || !result.IsAckOk)
            {
                throw new CommunicationException(CommunicationError.CalibrationFailed,
                    string.Format("White calibration failed: {0}", result.Ack));
            }
        }

        /// <summary>
        /// 获取标样的个数
        /// </summary>
        /// <returns>标样个数</returns>
        public int GetStandardCount()
        {
            ushort from = 0;
            ushort count = 0xFFFF;
            GetSpecimens(Instrument.SampleType.Standard, 0, ref from, ref count);
            return count;
        }

        /// <summary>
        /// 获取给定标样下的试样个数
        /// </summary>
        /// <param name="standard">标样索引</param>
        /// <returns>返回试样个数</returns>
        public int GetTestCount(int standard)
        {
            if (standard < 0)
            {
                DeviceInfo info = GetDeviceInfo();
                return info.TestCount;
            }

            ushort from = 0;
            ushort count = 0xFFFF;
            GetSpecimens(Instrument.SampleType.Test, (ushort)standard, ref from, ref count);
            if (count == 0xFFFF)
                count = 0;
            return count;
        }

        private void DequeueSpecimens(DataPacket result, List<ColorSpecimen.Specimen> list, bool isStd)
        {
            Optical.StandardIlluminant savedIlluminant = formula.Illuminant;

            while (result.PacketContent.Length > 0)
            {
                SampleData sample;
                result.PacketContent.Dequeue(out sample);

                Specimen specimen = new Specimen();

                if (sample.Flags.SpecularComponent == Optical.SpecularComponent.SCI ||
                    sample.Flags.SpecularComponent == Optical.SpecularComponent.SCIAndSCE)
                {
                    specimen.SCI = new ColorSpecimen.ColorData();
                    specimen.SCI.Set(sample.Color[0].Observer,
                        sample.Color[0].Illuminant, sample.Color[0].XYZ);
                }

                if (sample.Flags.SpecularComponent == Optical.SpecularComponent.SCE ||
                    sample.Flags.SpecularComponent == Optical.SpecularComponent.SCIAndSCE)
                {
                    specimen.SCE = new ColorSpecimen.ColorData();
                    specimen.SCE.Set(sample.Color[1].Observer,
                        sample.Color[1].Illuminant, sample.Color[1].XYZ);
                }

                if (sample.Flags.SpecularComponent == Optical.SpecularComponent.Unknown)
                {
                    specimen.Clr = new ColorSpecimen.ColorData();
                    specimen.Clr.Set(sample.Color[0].Observer,
                        sample.Color[0].Illuminant, sample.Color[0].XYZ);
                }
                specimen.Name = sample.Name;

                //if (sample.IsStandard)
                //    specimen.Name = string.Format("T{0:D3}", sample.Index);
                //else
                //    specimen.Name = string.Format("No.{0:D3}", sample.Index);


                specimen.DateTime = sample.DateTime;

                specimen.MeasurementCaliber = sample.Flags.Aperture;
                specimen.Instrument = Model;
                specimen.OpticalStruct = OpticalStructure;
                specimen.ID = sample.Index;

                list.Add(specimen);
            }

            formula.Illuminant = savedIlluminant;
        }

        /// <summary>
        /// 上传样品数据
        /// </summary>
        /// <param name="type">要上传的样品类型</param>
        /// <param name="standard">如果上传的是试样，为标样索引，指示要上传哪个标样下的试样；否则忽略</param>
        /// <param name="from">样品的起始位置，从0开始；在返回null时返回有效的起始位置</param>
        /// <param name="count">上传的样品个数；在返回null时返回有效的样品个数</param>
        /// <returns>如果给定的索引或个数超出范围，返回空；如果成功返回上传的样品</returns>
        ColorSpecimen.Specimen[] GetSpecimens(Instrument.SampleType type, ushort standard, ref ushort from, ref ushort count)
        {
            PacketContent content = new PacketContent();
            content.Enqueue((byte)type);
            content.Enqueue(standard);
            content.Enqueue(from);
            content.Enqueue(count);
            var result = SendCommand((byte)Command.UploadSpecimen, content);

            // 如果执行失败，会返回实际的样品起始位置和数量
            if (!result.IsAckOk)
            {
                byte sampleType;
                result.PacketContent.Dequeue(out sampleType);
                result.PacketContent.Dequeue(out standard);
                result.PacketContent.Dequeue(out from);
                result.PacketContent.Dequeue(out count);
                return null;
            }

            List<ColorSpecimen.Specimen> list = new List<ColorSpecimen.Specimen>();

            // 告诉仪器上传数据
            result = SendCommand((byte)Command.TransportSpecimen);
            byte endFlag = 0;
            ushort seq = 0;
            while (endFlag == 0)
            {
                if (result == null || result.PacketContent == null)
                {
                    break;
                }

                result.PacketContent.Dequeue(out seq);
                result.PacketContent.Dequeue(out endFlag);
                DequeueSpecimens(result, list, type == Instrument.SampleType.Standard);

                if (endFlag == 0)
                {
                    // 发送应答，表示已接收到数据，请求继续传输
                    content.Clear();
                    content.Enqueue(seq);
                    result = SendCommand((byte)Command.SpecimenPacket, content, DataPacket.AckOk);
                }
            }
            return list.ToArray();
        }

        /// <summary>
        /// 上传标样
        /// </summary>
        /// <param name="from">要上传的标样的序号，从0开始</param>
        /// <param name="count">要上传的个数；如果为-1，上传from到最后一个标样</param>
        /// <returns>返回上传的标样</returns>
        public Specimen[] GetStandards(int from = 0, int count = -1)
        {
            int totalCount = GetStandardCount();
            ushort hfrom = (ushort)from;
            ushort hcount = (count > 0) ? (ushort)count : (ushort)(totalCount - from);

            if ((hfrom + hcount) > totalCount || hcount == 0)
            {
                throw new ArgumentOutOfRangeException();
            }

            var standards = GetSpecimens(Instrument.SampleType.Standard, 0, ref hfrom, ref hcount);
            if (standards == null)
            {
                throw new CommunicationException(CommunicationError.ExecuteCommandFailed);
            }
            return standards;
        }

        /// <summary>
        /// 上传试样
        /// </summary>
        /// <param name="standard">所属标样的索引</param>
        /// <param name="from">起始序号，从0开始</param>
        /// <param name="count">上传的个数</param>
        /// <returns>返回上传的数据</returns>
        public Specimen[] GetTests(int standard, int from = 0, int count = -1)
        {
            int totalCount = GetTestCount(standard);
            ushort hfrom = (ushort)from;
            ushort hcount = (count > 0) ? (ushort)count : (ushort)(totalCount - from);

            if ((hfrom + hcount) > totalCount || hcount == 0)
            {
                throw new ArgumentOutOfRangeException();
            }
            var tests = GetSpecimens(Instrument.SampleType.Test, (ushort)standard, ref hfrom, ref hcount);
            if (tests == null)
            {
                throw new CommunicationException(CommunicationError.ExecuteCommandFailed);
            }
            return tests;
        }

        void ReceiveSpecimensThreadFunc(object obj)
        {
            object[] parameters = (object[])obj;
            EventHandler<SpecimenReceivedEventArgs> specimenReceivedHandler = (EventHandler<SpecimenReceivedEventArgs>)parameters[0];
            Instrument.SampleType sampleType = (Instrument.SampleType)parameters[1];

            try
            {
                var result = SendCommand((byte)Command.TransportSpecimen);
                byte endFlag = 0;
                ushort seq = 0;
                while (endFlag == 0)
                {
                    result.PacketContent.Dequeue(out seq);
                    result.PacketContent.Dequeue(out endFlag);
                    List<Specimen> list = new List<Specimen>();
                    DequeueSpecimens(result, list, sampleType == Instrument.SampleType.Standard);
                    specimenReceivedHandler?.Invoke(this, new SpecimenReceivedEventArgs(list.ToArray(), endFlag == 0));

                    if (_stopUpload != 0)
                    {
                        SendCommand((byte)Command.StopTransportSpecimen, waitingAck: false);
                        break;
                    }

                    if (endFlag == 0)
                    {
                        // 发送应答，表示已接收到数据，请求继续传输
                        PacketContent content = new PacketContent();
                        content.Enqueue(seq);
                        result = SendCommand((byte)Command.SpecimenPacket, content, DataPacket.AckOk);
                    }
                }

            }
            catch (Exception ex)
            {
                specimenReceivedHandler?.Invoke(this, new SpecimenReceivedEventArgs(ex));
            }
        }

        void UploadSpecimensAsync(EventHandler<SpecimenReceivedEventArgs> handler, Instrument.SampleType type,
                   ushort standard, ushort from, ushort count)
        {
            PacketContent content = new PacketContent();
            content.Enqueue((byte)type);
            content.Enqueue(standard);
            content.Enqueue(from);
            content.Enqueue(count);
            var result = SendCommand((byte)Command.UploadSpecimen, content);
            if (result == null || !result.IsAckOk)
                return;

            _stopUpload = 0;
            var thread = new Thread(ReceiveSpecimensThreadFunc);
            thread.Start(new object[] { handler, type });
        }


        public void UploadStandardAsync(EventHandler<SpecimenReceivedEventArgs> handler, int from = 0, int count = -1)
        {
            int totalCount = GetStandardCount();
            if ((from + count) > totalCount || count == 0)
            {
                throw new ArgumentOutOfRangeException();
            }
            ushort hfrom = (ushort)from;
            ushort hcount = (count != -1) ? (ushort)count : (ushort)(totalCount - from);

            UploadSpecimensAsync(handler, Instrument.SampleType.Standard,
                0xFFFF, hfrom, hcount);
        }

        public void UploadTestAsync(EventHandler<SpecimenReceivedEventArgs> handler, int standard, int from = 0, int count = -1)
        {
            int totalCount = GetTestCount(standard);
            if ((from + count) > totalCount || count == 0)
            {
                throw new ArgumentOutOfRangeException();
            }
            ushort hfrom = (ushort)from;
            ushort hcount = (count != -1) ? (ushort)count : (ushort)(totalCount - from);

            UploadSpecimensAsync(handler, Instrument.SampleType.Test,
                (ushort)standard, hfrom, hcount);
        }


        void YRReceiveSpecimensThreadFunc(object obj)
        {
            object[] parameters = (object[])obj;
            Action<SampleData[], bool, Exception> receivedHandler = (Action<SampleData[], bool, Exception>)parameters[0];
            Instrument.SampleType sampleType = (Instrument.SampleType)parameters[1];

            DataPacket result;
            try
            {
                result = SendCommand((byte)Command.TransportSpecimen);
            }
            catch (CommunicationException ex)
            {
                receivedHandler?.Invoke(null, false, ex);
                return;
            }

            byte endFlag = 0;
            ushort seq = 0;
            while (endFlag == 0)
            {
                try
                {
                    var packetContent = result.PacketContent;
                    packetContent.Dequeue(out seq);
                    packetContent.Dequeue(out endFlag);
                    List<SampleData> list = new List<SampleData>();

                    while (packetContent.Length > 0)
                    {
                        SampleData sampleData;
                        packetContent.Dequeue(out sampleData);
                        list.Add(sampleData);
                    }

                    receivedHandler?.Invoke(list.ToArray(), endFlag == 0, null);

                    if (_stopUpload != 0)
                    {
                        SendCommand((byte)Command.StopTransportSpecimen, waitingAck: false);
                        break;
                    }

                    if (endFlag == 0)
                    {
                        // 发送应答，表示已接收到数据，请求继续传输
                        PacketContent content = new PacketContent();
                        content.Enqueue(seq);
                        result = SendCommand((byte)Command.SpecimenPacket, content, DataPacket.AckOk);
                    }
                }
                catch (Exception ex)
                {
                    if (endFlag == 0)
                    {
                        SendCommand((byte)Command.StopTransportSpecimen, waitingAck: false);
                    }
                    receivedHandler?.Invoke(null, false, ex);
                }
            }
        }

        void YRUploadSpecimensAsync(Action<SampleData[], bool, Exception> handler, Instrument.SampleType type,
                   ushort standard, ushort from, ushort count)
        {
            PacketContent content = new PacketContent();
            content.Enqueue((byte)type);
            content.Enqueue(standard);
            content.Enqueue(from);
            content.Enqueue(count);
            var result = SendCommand((byte)Command.UploadSpecimen, content);
            if (result == null || !result.IsAckOk)
                return;

            _stopUpload = 0;
            var thread = new Thread(YRReceiveSpecimensThreadFunc);
            thread.Start(new object[] { handler, type });
        }

        public void YRUploadStandardAsync(Action<SampleData[], bool, Exception> handler, int from = 0, int count = -1)
        {
            int totalCount = GetStandardCount();
            if ((from + count) > totalCount || count == 0)
            {
                throw new ArgumentOutOfRangeException();
            }
            ushort hfrom = (ushort)from;
            ushort hcount = (count != -1) ? (ushort)count : (ushort)(totalCount - from);

            YRUploadSpecimensAsync(handler, Instrument.SampleType.Standard,
                0xFFFF, hfrom, hcount);
        }

        public void YRUploadTestAsync(Action<SampleData[], bool, Exception> handler, int standard, int from = 0, int count = -1)
        {
            int totalCount = GetTestCount(standard);
            if ((from + count) > totalCount || count == 0)
            {
                throw new ArgumentOutOfRangeException();
            }
            ushort hfrom = (ushort)from;
            ushort hcount = (count != -1) ? (ushort)count : (ushort)(totalCount - from);

            YRUploadSpecimensAsync(handler, Instrument.SampleType.Test,
                (ushort)standard, hfrom, hcount);
        }


        public void StopUpload()
        {
            Interlocked.Exchange(ref _stopUpload, 1);
        }

        public void DeleteAllSpecimens()
        {
            DeleteSpecimens(Instrument.SampleType.Standard);
        }

        public void DeleteTests(int standard)
        {
            DeleteSpecimens(Instrument.SampleType.Test, standard);
        }

        private void DeleteSpecimens(Instrument.SampleType type, int standard = -1)
        {
            PacketContent content = new PacketContent();
            content.Enqueue((byte)type);
            if (standard < 0)
                content.Enqueue((ushort)0xFFFF);
            else
                content.Enqueue((ushort)standard);
            var result = SendCommand((byte)Command.DeleteSpecimen, content);

            if (!result.IsAckOk)
            {
                throw new CommunicationException(CommunicationError.ExecuteCommandFailed,
                    string.Format("Delete {0} failed: {1}", type, result.Ack));
            }
        }

        public void WriteStandard(ThreeNH.ColorSpecimen.Specimen standard)
        {
            CIELAB lab = null;
            Optical.SpecularComponent sc = SpecularComponent;
            switch (standard.SpecularComponent)
            {
                case Optical.SpecularComponent.SCI:
                    lab = standard.SCI.Lab(formula);
                    sc = Optical.SpecularComponent.SCI;
                    break;
                case Optical.SpecularComponent.SCE:
                    lab = standard.SCE.Lab(formula);
                    sc = Optical.SpecularComponent.SCE;
                    break;
                case Optical.SpecularComponent.Unknown:
                    lab = standard.Clr.Lab(formula);
                    break;
                case Optical.SpecularComponent.SCIAndSCE:
                    {
                        var clr = standard.GetColorData(SpecularComponent);
                        if (clr != null)
                            lab = clr.Lab(formula);
                    }
                    break;
            }
            if (lab == null)
            {
                throw new CommunicationException(CommunicationError.NotSupported);
            }

            PacketContent content = new PacketContent();
            content.Enqueue(lab.L);
            content.Enqueue(lab.a);
            content.Enqueue(lab.b);
            content.Enqueue((byte)formula.Illuminant.ToInstrumentForm());
            content.Enqueue((byte)Aperture.ToInstrumentForm());
            content.Enqueue((byte)sc.ToInstrumentForm());

            var result = SendCommand((byte)Command.WriteStandard, content);
            if (!result.IsAckOk)
            {
                throw new CommunicationException(CommunicationError.ExecuteCommandFailed);
            }
        }

        public void WriteStandards(IEnumerable<Specimen> standards)
        {
            foreach (var standard in standards)
            {
                WriteStandard(standard);
            }
        }

        public void RefreshInstrumentInfo()
        {
            Trace.TraceInformation("获取仪器信息...");
            DeviceInfo info = GetDeviceInfo();
            Trace.TraceInformation("获取测量参数...");
            MeasurementParameter param = GetMeasurementParameter();
            Model = info.CustomModel;
            FirmwareRevision = info.FirmwareVersion;
            HardwareRevision = info.HardwareVersion;
            OpticalStructure = info.OpticalStruct;
            SN = info.Sn;
            WhiteBoardNumber = info.ExternalSnOfWhiteBoard;
            var caliber = DetectCaliber(info.Model);
            if (caliber == Instrument.MeasurementCaliber.Phi8)
            {
                Aperture = param.MeasurementCaliber;
            } 
            else
            {
                Aperture = Model == "MCR10" ? MeasurementCaliber.Phi1 : (Model == "BTS20" ? MeasurementCaliber.Phi50 : caliber.Uniform());
            }
            formula.Illuminant = param.PrimaryIlluminant;
            _specularComponent = param.SpecularComponent;
            Trace.TraceInformation("刷新仪器信息成功");
        }

        public DeviceInfo GetDeviceInfo()
        {
            var result = SendCommand((byte)Command.GetDeviceInfo);
            if (result != null && result.IsAckOk)
            {
                DeviceInfo info;
                result.PacketContent.Dequeue(out info);
                return info;
            }
            throw new CommunicationException(CommunicationError.ExecuteCommandFailed);
        }

        public MeasurementParameter GetMeasurementParameter()
        {
            var result = SendCommand((byte)Command.GetMeasurementParamter);
            if (result != null && result.IsAckOk)
            {
                MeasurementParameter para;
                result.PacketContent.Dequeue(out para);
                return para;
            }
            throw new CommunicationException(CommunicationError.ExecuteCommandFailed);
        }

        public bool SetMeasurementParameter(MeasurementParameter parameter)
        {
            PacketContent content = new PacketContent();
            content.Enqueue(parameter);

            var result = SendCommand((byte)Command.SetMeasurementParameter);

            return (result != null && result.IsAckOk);
        }

        public void SetDateTime(DateTime datetime)
        {
            PacketContent content = new PacketContent();
            content.Enqueue((byte)datetime.Hour);
            content.Enqueue((byte)datetime.Minute);
            content.Enqueue((byte)(datetime.Year - 2000));
            content.Enqueue((byte)datetime.Month);
            content.Enqueue((byte)datetime.Day);

            var result = SendCommand((byte)Command.SetDateTime, content);

            if (!result.IsAckOk)
            {
                throw new CommunicationException(CommunicationError.ExecuteCommandFailed);
            }
        }

        // 获取原始采样数值
        public void GetSamplingData(out ushort[] majorRGB, out ushort[] minorRGB)
        {
            var result = SendCommand((byte)Command.GetSamplingData);
            if (result.IsAckOk)
            {
                majorRGB = new ushort[3];
                minorRGB = new ushort[3];

                for (int i = 0; i < 3; i++)
                    result.PacketContent.Dequeue(out majorRGB[i]);
                for (int i = 0; i < 3; i++)
                    result.PacketContent.Dequeue(out minorRGB[i]);
            }
            else
            {
                throw new CommunicationException(CommunicationError.ExecuteCommandFailed);
            }
        }

        // 设置黑白板上下限
        public void SetSensorLimit(ushort white_down_limit, ushort white_up_limit,
            ushort black_down_limit, ushort black_up_limit,
            ushort minior_down_limit, ushort minor_up_limit)
        {
            PacketContent content = new PacketContent();
            content.Enqueue(white_down_limit);
            content.Enqueue(white_up_limit);
            content.Enqueue(black_down_limit);
            content.Enqueue(black_up_limit);
            content.Enqueue(minior_down_limit);
            content.Enqueue(minor_up_limit);

            var result = SendCommand((byte)Command.SetSensorLimit, content);
            if (!result.IsAckOk)
            {
                throw new CommunicationException(CommunicationError.ExecuteCommandFailed);
            }
        }

        // 获取黑白板上下限
        public void GetSensorLimit(
            out ushort white_down_limit, out ushort white_up_limit, // 白板下限上限
            out ushort black_down_limit, out ushort black_up_limit, // 黑板下限上限
            out ushort minor_down_limit, out ushort minor_up_limit) // 辅助光路下限上限
        {
            var result = SendCommand((byte)Command.GetSensorLimit);
            if (result.IsAckOk)
            {
                result.PacketContent.Dequeue(out white_down_limit);
                result.PacketContent.Dequeue(out white_up_limit);
                result.PacketContent.Dequeue(out black_down_limit);
                result.PacketContent.Dequeue(out black_up_limit);
                result.PacketContent.Dequeue(out minor_down_limit);
                result.PacketContent.Dequeue(out minor_up_limit);
            }
            else
            {
                throw new CommunicationException(CommunicationError.ExecuteCommandFailed);
            }
        }

        // 生成白板的限制，并返回生成仪器生成的白板参考值及上下限
        public void AutoMakeWhiteLimit(
            out ushort[] majorRGB, out ushort[] minorRGB,     // 主辅的RGB值
            out ushort down_limit, out ushort up_limit,       // 白板的下限上限
            out ushort minor_down_limit, out ushort minor_up_limit) // 辅助光路的下限上限
        {
            var result = SendCommand((byte)Command.AutoMakeWhiteLimit);
            if (result.IsAckOk)
            {
                majorRGB = new ushort[3];
                minorRGB = new ushort[3];

                for (int i = 0; i < 3; i++)
                    result.PacketContent.Dequeue(out majorRGB[i]);
                for (int i = 0; i < 3; i++)
                    result.PacketContent.Dequeue(out minorRGB[i]);

                result.PacketContent.Dequeue(out down_limit);
                result.PacketContent.Dequeue(out up_limit);
                result.PacketContent.Dequeue(out minor_down_limit);
                result.PacketContent.Dequeue(out minor_up_limit);
            }
            else
            {
                throw new CommunicationException(CommunicationError.ExecuteCommandFailed);
            }
        }

        // 生成黑板的限制，并返回生成仪器生成的黑板参考值及上下限
        public void AutoMakeBlackLimit(
            out ushort[] majorRGB, out ushort[] minorRGB,     // 主辅的RGB值
            out ushort down_limit, out ushort up_limit,       // 白板的下限上限
            out ushort minor_down_limit, out ushort minor_up_limit) // 辅助光路的下限上限
        {
            var result = SendCommand((byte)Command.AutoMakeBlackLimit);
            if (result.IsAckOk)
            {
                majorRGB = new ushort[3];
                minorRGB = new ushort[3];

                for (int i = 0; i < 3; i++)
                    result.PacketContent.Dequeue(out majorRGB[i]);
                for (int i = 0; i < 3; i++)
                    result.PacketContent.Dequeue(out minorRGB[i]);

                result.PacketContent.Dequeue(out down_limit);
                result.PacketContent.Dequeue(out up_limit);
                result.PacketContent.Dequeue(out minor_down_limit);
                result.PacketContent.Dequeue(out minor_up_limit);
            }
            else
            {
                throw new CommunicationException(CommunicationError.ExecuteCommandFailed);
            }
        }

        public void RestoreFactorySettings()
        {
            var result = SendCommand((byte)Command.RestoreFactorySettings);
            if (!result.IsAckOk)
            {
                throw new CommunicationException(CommunicationError.ExecuteCommandFailed);
            }
        }

        public void SetInstrumentLanguage(InstrumentLanguage lang)
        {
            PacketContent content = new PacketContent();
            content.Enqueue((byte)lang);
            var result = SendCommand((byte)Command.SetLanguage, content);
            if (!result.IsAckOk)
            {
                throw new CommunicationException(CommunicationError.ExecuteCommandFailed);
            }
        }

        public void SetWhiteBoardXYZ(float X, float Y, float Z)
        {
            PacketContent content = new PacketContent();
            content.Enqueue(X);
            content.Enqueue(Y);
            content.Enqueue(Z);

            var result = SendCommand((byte)Command.SetWhiteBoardXyz, content);
            if (!result.IsAckOk)
            {
                throw new CommunicationException(CommunicationError.ExecuteCommandFailed);
            }
        }

        public void GetWhiteBoardXYZ(out float X, out float Y, out float Z)
        {
            var result = SendCommand((byte)Command.GetWhiteBoardXyz);
            if (result.IsAckOk)
            {
                result.PacketContent.Dequeue(out X);
                result.PacketContent.Dequeue(out Y);
                result.PacketContent.Dequeue(out Z);
            }
            else
            {
                throw new CommunicationException(CommunicationError.ExecuteCommandFailed);
            }
        }

        public void SetWhiteBoardSn(string internalSn, string externalSn)
        {
            PacketContent content = new PacketContent();
            content.Enqueue(internalSn);
            content.Enqueue(externalSn);
            var result = SendCommand((byte) Command.SetWhiteBoardSn, content);
            if (!result.IsAckOk)
            {
                throw new CommunicationException(CommunicationError.ExecuteCommandFailed);
            }
        }

        public void GetWhiteBoardSn(out string internalSn, out string externalSn)
        {
            var result = SendCommand((byte) Command.GetWhiteBoardSn);
            if (!result.IsAckOk)
            {
                throw new CommunicationException(CommunicationError.ExecuteCommandFailed);
            }

            internalSn = result.PacketContent.DequeueString();
            externalSn = result.PacketContent.DequeueString();
        }


        public void YRGetSamplingData(out YRSamplingData samplingData)
        {
            var result = SendCommand((byte)Command.YrGetSamplingData);
            if (result.IsAckOk)
            {
                samplingData = result.PacketContent.DequeueRawData();
            }
            else
            {
                throw new CommunicationException(CommunicationError.ExecuteCommandFailed);
            }
        }

        public FactoryInfo GetFactoryInfo()
        {
            var result = SendCommand((byte) Command.GetFactoryInfo);
            if (result.IsAckOk)
            {
                return result.PacketContent.DequeueFactoryInfo();
            }

            throw new CommunicationException(CommunicationError.ExecuteCommandFailed);
        }

        public void SetFactoryInfo(FactoryInfo factoryInfo)
        {
            PacketContent content = new PacketContent();
            content.Enqueue(factoryInfo);
            var result = SendCommand((byte) Command.SetFactoryInfo, content);
            if (!result.IsAckOk)
            {
                throw new CommunicationException(CommunicationError.ExecuteCommandFailed);
            }
        }

        public Tuple<ModelCode, ushort> GetInternalModel()
        {
            var result = SendCommand((byte) Command.GetInternalModel);
            if (result.IsAckOk)
            {
                try
                {
                    return new Tuple<ModelCode, ushort>(
                        (ModelCode) result.PacketContent.DequeueUInt16(),
                        result.PacketContent.DequeueUInt16());
                }
                catch
                {
                    throw new CommunicationException(CommunicationError.ParsedBadData);
                }
            }
            throw new CommunicationException(CommunicationError.ExecuteCommandFailed);
        }

        public void SetInternalModel(ModelCode modelCode, ushort minorModelCode=0)
        {
            PacketContent content = new PacketContent();
            content.Enqueue((ushort)modelCode);
            content.Enqueue(minorModelCode);
            var result = SendCommand((byte) Command.SetInternalModel, content);
            if (!result.IsAckOk)
            {
                throw new CommunicationException(CommunicationError.ExecuteCommandFailed);
            }
        }

        public double[] GetCorrectionFactors(byte matrixIndex)
        {
            PacketContent content = new PacketContent();
            content.Enqueue(matrixIndex);
            
            var result = SendCommand((byte) Command.GetCorrectionFactors, content);
            if (result.IsAckOk)
            {
                
                try
                {
                    double[] factors = new double[9];
                    for (int i = 0; i < factors.Length && result.PacketContent.Length > 0; i++)
                    {
                        factors[i] = result.PacketContent.DequeueFloat();
                    }
                    return factors;
                }
                catch (Exception e)
                {
                    throw new CommunicationException(CommunicationError.ParsedBadData);
                }
            }
            throw new CommunicationException(CommunicationError.ExecuteCommandFailed);
        }

        public void SetCorrectionFactors(byte matrixIndex, double[] factors)
        {
            if (factors == null)
            {
                throw new CommunicationException(CommunicationError.InvalidParameter);
            }

            PacketContent content = new PacketContent();
            content.Enqueue(matrixIndex);
            foreach (var factor in factors)
            {
                content.Enqueue((float)factor);
            }

            var result = SendCommand((byte)Command.SetCorrectionFactors, content);
            if (!result.IsAckOk)
            {
                throw new CommunicationException(CommunicationError.ExecuteCommandFailed);
            }
        }

        /// <summary>
        /// 设置相机定位图标位置
        /// </summary>
        /// <param name="aperture">口径类型</param>
        /// <param name="x">水平位置</param>
        /// <param name="y">垂直位置</param>
        public void SetLocationImagePosition(MeasurementCaliber aperture, short x, short y)
        {
            PacketContent content = new PacketContent();
            content.Enqueue((byte)aperture.ToInstrumentForm());
            content.Enqueue(x);
            content.Enqueue(y);
            var result = SendCommand((byte) Command.SetLocationImagePosition, content);
            if (!result.IsAckOk)
            {
                throw new CommunicationException(CommunicationError.ExecuteCommandFailed);
            }
        }

        /// <summary>
        /// 获取相机定位图标位置
        /// </summary>
        /// <param name="aperture">口径类型</param>
        /// <param name="x">水平位置</param>
        /// <param name="y">垂直位置</param>
        public void GetLocationImagePosition(MeasurementCaliber aperture, out short x, out short y)
        {
            PacketContent content = new PacketContent();
            content.Enqueue((byte)aperture.ToInstrumentForm());
            var result = SendCommand((byte)Command.GetLocationImagePosition, content);
            if (!result.IsAckOk)
            {
                throw new CommunicationException(CommunicationError.ExecuteCommandFailed);
            }

            try
            {
                byte tmp = result.PacketContent.DequeueByte();
                x = result.PacketContent.DequeueInt16();
                y = result.PacketContent.DequeueInt16();
            }
            catch
            {
                throw new CommunicationException(CommunicationError.ParsedBadData);
            }
        }

        public void Open(object commDevice = null)
        {
            Connect(commDevice);
        }

        public void Close()
        {
            Disconnect();
        }

        #region SVD
        /// <summary>
        /// SVD测量
        /// </summary>
        /// <returns></returns>
        public List<float> GetMeasureSpectrumEight(int numbers = 8)
        {

            //PacketContent content = new PacketContent();
            //content.Enqueue((byte)0);
            //List<float> list = new List<float>();
            //var result = SendCommand((byte)Command.ReadSpectrumEight, content);
            //if (result.IsAckOk)
            //{
            //    uint n = result.PacketContent.Length / 4;
            //    for (int i = 0; i < n; i++)
            //    {
            //        list.Add(result.PacketContent.DequeueFloat());
            //    }
            //    return list;
            //}
            return null;
        }
        /// <summary>
        /// 写入算法参数矩阵
        /// </summary>
        /// <param name="datas"></param>
        /// <returns></returns>
        public bool SetSVD(List<List<float>> datas, MeasureType? measureType = null, byte caliber = 0)
        {

            //if (datas == null) return false;
            //var rows = datas.Count;
            //var cols = datas[0].Count;
            //List<float> dataList = new List<float>();
            //byte index = 0;
            //for (int i = 0; i < rows; i++)
            //{
            //    index = (byte)i;
            //    PacketContent packet = new PacketContent();
            //    packet.Enqueue(index);
            //    for (int j = 0; j < cols; j++)
            //    {
            //        packet.Enqueue(datas[i][j]);
            //    }
            //    var result = SendCommand((byte)Command.WriteSVD, packet);
            //    if (!result.IsAckOk)
            //    {

            //        throw new CommunicationException(CommunicationError.ExecuteCommandFailed,
            //            string.Format("SVD写入失败：{0}", result.PacketContent));
            //    }
            //}
            return true;
        }

        public List<List<float>> ReadSVDs(MeasureType? measureType = null, int rows = 31, int cols = 16)
        {
            throw new NotImplementedException();
        }

        public List<float> ReadSVDList(MeasureType? measureType = null, int rows = 31, int cols = 16)
        {
            throw new NotImplementedException();
        }

        public bool SetOLSR(List<List<float>> datas)
        {
            throw new NotImplementedException();
        }

        public List<float> ReadOLSRList(MeasureType? measureType = null)
        {
            throw new NotImplementedException();
        }
        #endregion
    }
}
