using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ThreeNH.CommunicationBase;
using ThreeNH.ColorimeterInterface;
using System.Diagnostics;
using System.IO;
using System.Threading;
using ThreeNH.ColorSpecimen;
using ThreeNH.ColorAlgorithm;
using ThreeNH.DataModel;
using ThreeNH.NRColorimeter.Instrument;
using DateTime = System.DateTime;
using StandardIlluminant = ThreeNH.Optical.StandardIlluminant;
using StandardObserver = ThreeNH.Optical.StandardObserver;
using System.Reflection;

namespace ThreeNH.NRColorimeter
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
        SetWhiteBoardNumber,     // 0xb7 设置白板编号
        GetWhiteBoardNumber,		// 0xb8 获取白板编号
        SetLanguage = 0xB9,         // B9 设置仪器显示语言
        RestoreFactorySettings,     // BA 恢复仪器出产设置
        SetCorrectionMatrix,      // 0xBB 设置修正矩阵
        GetCorrectionMatrix,      // 0xBC 获取修正矩阵
        SetWhiteBoardXyz,        // 0xBD 设置白板XYZ值
        GetWhiteBoardXyz,        // 0xBE 读出白板XYZ值 //ztd:根据廖工的意思0xBE为写入SN的操作(20251030)
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
        SetWhiteBoardXyzEx,   // 0xCA 设置白板XYZ（全光源）
        GetWhiteBoardXyzEx,   // 0xCB 获取白板XYZ（全光源）
        MeasureInSensorData,      // 0xCC 单次测量读出光谱数据
        SetCompanyName = 0xD4, // 设置公司名称
        GetCompanyName, // 获取公司名称
        SetCalibrationCoefficient, // 设置标定系数
        GetCalibrationCoefficient, // 获取标定系数
        SetModel = 0xDF,            //设置型号
        GetModel = 0xE0,            //获取型号
        WriteSVD = 0xE9, //SVD算法写入仪器
        ReadSVD = 0xEA, //SVD算法读取仪器
        ReadSpectrumEight = 0xEC, //SVD算法读取仪器
        ResetSVD = 0xED,//恢复默认算法数据
        WriteFixedPoint=0xEE,
        ReadFixedPoint = 0xEF,
        ResetFixedPoint = 0xF0,
        GetInternalType=0xF1,
        BluetoothExit =0xFA,//仪器关闭,仪器蓝牙主动退出
        NullCommand,             // 无效命令
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

    public class SensorDataReceivedEventArgs : EventArgs
    {
        public SensorDataReceivedEventArgs(SensorData sensorData)
        {
            SensorData = sensorData;
        }

        public SensorData SensorData { get; }
    }

    public class Colorimeter : InstrumentBase, IColorimeter, IMetrology, ISpecimenClearing,  IRecordManagement
    {
        private ThreeNH.ColorAlgorithm.Formula formula = new ColorAlgorithm.Formula();
        int _stopUpload = 0;    // 停止异步上传标志
        public event EventHandler<SpecimenReceivedEventArgs> MeasureResultReceived;
        public event EventHandler<SensorDataReceivedEventArgs> SensorDataReceived;

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

        public string Model { get; private set; }
        public string SN
        {
            get { return null; }
        }
        public string HardwareRevision { get; private set; }
        public string FirmwareRevision { get; private set; }
        public string SoftwareRevision
        {
            get { return FirmwareRevision; }
        }
        public string Manufacturer { get; }
        public string OpticalStructure { get; private set; }

        public string WhiteBoardNumber
        {
            get
            {
                return null;
            }
        }

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

        public ThreeNH.ColorAlgorithm.WavelengthRange WavelengthRange
        {
            get
            {
                //throw new NotImplementedException();
                return new ColorAlgorithm.WavelengthRange(0, 0, 0);
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

        public string OpticalStruct
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
            if (packet.Command == (byte)Command.Measure)
            {
                try
                {
                    MeasureResultReceived?.Invoke(this, new SpecimenReceivedEventArgs(DequeueMeasureResult(packet)));
                }
                catch (Exception e)
                {
                    Trace.TraceError("Dequeue measurement result failed: {0}", ex);
                }
            }
            else if (packet.Command == (byte)Command.MeasureInSensorData)
            {
                try
                {
                    SensorDataReceived?.Invoke(this, new SensorDataReceivedEventArgs(DequeueSensorData(packet)));
                }
                catch (Exception e)
                {
                    Trace.TraceError("Dequeue sensor data failed: {0}", ex);
                }
            }
            //仪器关闭，蓝牙主动退出
            //else if (packet.Command==(byte)Command.BluetoothExit)
            //{
            //    try
            //    {
            //        SensorDataReceived?.Invoke(this,null);
            //    }
            //    catch (Exception e)
            //    {
            //        Trace.TraceError("Dequeue sensor data failed: {0}", ex);
            //    }
            //}
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

        static Instrument.MeasurementCaliber DetectCaliber(string deviceName, Optical.MeasurementCaliber caliber)
        {
            
            deviceName = deviceName.ToUpper();
            foreach (var item in _instrumentCaliberDict)
            {
                foreach (var modle in item.Value)
                {
                    if (deviceName.Contains(modle)&& deviceName != "TP110"&&deviceName!="NR100")
                    {
                        return item.Key;
                    }
                }
            }

            if (string.Compare(deviceName, "DR-18", StringComparison.CurrentCultureIgnoreCase) == 0)
            {
                if (caliber == Optical.MeasurementCaliber.Phi2)
                {
                    return Instrument.MeasurementCaliber.Phi40;
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
            catch (Exception)
            {
                Disconnect();
                throw;
            }
        }


        public ThreeNH.ColorSpecimen.Specimen Measure()
        {
            PacketContent content = new PacketContent();
            content.Enqueue((byte)1);

            var result = SendCommand((byte)Command.Measure, content);
            if (result != null && result.IsAckOk)
            {
                return DequeueMeasureResult(result);
            }
            return null;
        }

        private Specimen DequeueMeasureResult(DataPacket dataPacket)
        {
            float l, a, b, c, h;
            dataPacket.PacketContent.Dequeue(out l);
            dataPacket.PacketContent.Dequeue(out a);
            dataPacket.PacketContent.Dequeue(out b);
            dataPacket.PacketContent.Dequeue(out c);
            dataPacket.PacketContent.Dequeue(out h);

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
                return devInfo.BlackCalibration && devInfo.WhiteCalibration;
            }
        }

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
                Instrument.SampleData sample;
                result.PacketContent.Dequeue(out sample);

                ColorSpecimen.Specimen specimen = isStd ? new StandardSpecimen() : new ColorSpecimen.Specimen();

                formula.Illuminant = sample.Illuminant;

                var colorData = new ThreeNH.ColorSpecimen.ColorData();
                var xyz = formula.LabToXyz(new ColorAlgorithm.CIELAB(sample.L, sample.a, sample.b));
                colorData.Set(formula.Observer, formula.Illuminant, xyz);
                if (SpecularComponent == Optical.SpecularComponent.SCI)
                    specimen.SCI = colorData;
                else if (SpecularComponent == Optical.SpecularComponent.SCE)
                    specimen.SCE = colorData;
                else
                    specimen.Clr = colorData;

                if (isStd)
                    specimen.Name = string.Format("T{0:D3}", sample.Index);
                else
                    specimen.Name = string.Format("No.{0:D3}", sample.Index);

                try
                {
                    specimen.DateTime = sample.Time.ToSystemDateTime();
                }
                catch
                {
                    specimen.DateTime = DateTime.Now;
                }

                specimen.MeasurementCaliber = Aperture;
                specimen.Instrument = Model;
                specimen.OpticalStruct = OpticalStruct;
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


        #region IRecordManagement


        public int TotalStandards
        {
            get { return GetStandardCount(); }
        }
        public int TotalSamples
        {
            get { return GetDeviceInfo().TestCount; }
        }

        public bool YTContain400 { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        WavelengthRange IDeviceInfo.WavelengthRange => throw new NotImplementedException();

        public int GetSamplesCount(int standardIndex)
        {
            throw new NotImplementedException();
        }

        public int GetSamplesCountById(int standardId)
        {
            return GetTestCount(standardId);
        }

        public StandardSpecimen UploadStandard(int standardIndex)
        {
            return GetStandards(standardIndex, 1)[0] as StandardSpecimen;
        }

        public StandardSpecimen UploadStandardById(int standardId)
        {
            throw new NotImplementedException();
        }

        public StandardSpecimen[] UploadStandards(int startIndex, int count)
        {
            return Array.ConvertAll(GetStandards(startIndex, count), _ => (StandardSpecimen)_);
        }


        public Specimen UploadSample(int standardIndex, int sampleIndex)
        {
            throw new NotImplementedException();
        }

        public Specimen UploadSampleById(int standardId, int sampleIndex)
        {
            return GetTests(standardId, sampleIndex, 1)[0];
        }

        public Specimen[] UploadSamples(int standardIndex, int startSampleIndex, int count)
        {
            throw new NotImplementedException();
        }

        public Specimen[] UploadSamplesById(int standardId, int startSampleIndex, int count)
        {
            return GetTests(standardId, startSampleIndex, count);
        }

        public void RemoveAllRecords()
        {
            DeleteAllSpecimens();
        }

        public void RemoveAllSamples()
        {
            DeleteTests(-1);
        }

        public void RemoveAllSamples(int standardIndex)
        {
            throw new NotImplementedException();
        }

        public void RemoveAllSamplesById(int standardId)
        {
            DeleteTests(standardId);
        }

        public void DeleteStandard(int standardIndex)
        {
            throw new NotImplementedException();
        }

        public void DeleteStandardById(int standardId)
        {
            throw new NotImplementedException();
        }

        public void DeleteSample(int standardIndex, int sampleIndex)
        {
            throw new NotImplementedException();
        }

        public void DeleteSampleById(int standardIndex, int sampleId)
        {
            throw new NotImplementedException();
        }

        public void DownloadStandard(Specimen standard)
        {
            WriteStandard(standard);
        }

        public void DownloadStandards(IEnumerable<Specimen> standards)
        {
            WriteStandards(standards);
        }

        public void Backup(StreamWriter stream)
        {
            throw new NotImplementedException();
        }

        public void Restory(StreamReader stream)
        {
            throw new NotImplementedException();
        }

        #endregion


        void YRReceiveSpecimensThreadFunc(object obj)
        {
            object[] parameters = (object[])obj;
            Action<YRSampleData[], bool, Exception> receivedHandler = (Action<YRSampleData[], bool, Exception>)parameters[0];
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
                    List<YRSampleData> list = new List<YRSampleData>();

                    while (packetContent.Length > 0)
                    {
                        var sampleData = new YRSampleData();
                        Instrument.DateTime time = new Instrument.DateTime();
                        packetContent.Dequeue(ref time);
                        try
                        {
                            sampleData.Time = time.ToSystemDateTime();
                        }
                        catch
                        {
                            sampleData.Time = DateTime.Now;
                        }
                        packetContent.Discard(1);
                        sampleData.L = packetContent.DequeueFloat();
                        sampleData.a = packetContent.DequeueFloat();
                        sampleData.b = packetContent.DequeueFloat();
                        sampleData.C = packetContent.DequeueFloat();
                        sampleData.h = packetContent.DequeueFloat();
                        sampleData.Illuminant = ((Instrument.StandardIlluminant)packetContent.DequeueByte()).Uniform();
                        sampleData.Caliber = ((Instrument.MeasurementCaliber)packetContent.DequeueByte()).Uniform();
                        sampleData.SpecularComponent = ((Instrument.SpecularComponent)packetContent.DequeueByte()).Uniform();
                        packetContent.Discard(1);
                        sampleData.Index = packetContent.DequeueUInt16();
                        //packetContent.Discard(2);
                        Dequeue(packetContent, out sampleData.SamplingData);
                        packetContent.Discard(2);

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

        void YRUploadSpecimensAsync(Action<YRSampleData[], bool, Exception> handler, Instrument.SampleType type,
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

        public void YRUploadStandardAsync(Action<YRSampleData[], bool, Exception> handler, int from = 0, int count = -1)
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

        public void YRUploadTestAsync(Action<YRSampleData[], bool, Exception> handler, int standard, int from = 0, int count = -1)
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
            Console.WriteLine("获取仪器信息NR仪器信息");
            Trace.TraceInformation("获取仪器信息...");
            DeviceInfo info = GetDeviceInfo();
            Trace.TraceInformation("获取测量参数...");
            MeasurementParameter param = GetMeasurementParameter();
            Model = info.DeviceName;
            FirmwareRevision = info.FirmwareVersion;
            OpticalStruct = DetectOpticalStruct(info.DeviceName);
             var  caliber = DetectCaliber(info.DeviceName, param.MeasurementCaliber);
           
            if (caliber == Instrument.MeasurementCaliber.Phi8)
                Aperture = param.MeasurementCaliber;
            else
                Aperture = caliber.Uniform();
            formula.Illuminant = param.PrimaryIlluminant;
            _specularComponent = param.SpecularComponent;
            //GetWhiteBoardNumbers(out var internalNumber,out var externalNumber);
            Trace.TraceInformation("刷新仪器信息成功");
            Console.WriteLine("刷新仪器信息成功");
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

        public void SetDeivceInfo(DeviceInfo info)
        {
            PacketContent content = new PacketContent();
            content.Enqueue(info.FirmwareVersion);
            content.Enqueue(info.FirmwareVersion);
            content.Enqueue(info.SN);
            content.Enqueue(info.DeviceNumber);

            var result = SendCommand((byte)Command.GetWhiteBoardXyz, content);
            if (!result.IsAckOk)
            {
                {
                    throw new CommunicationException(CommunicationError.ExecuteCommandFailed,
                        "Set version info failed: " + result.Ack.ToString());
                }
            }
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

        private void Dequeue(PacketContent packetContent, out YRSamplingData samplingData)
        {
            samplingData = new YRSamplingData();

            for (int i = 0; i < 3; i++)
            {
                samplingData.MajorLightOn[i] = packetContent.DequeueUInt16();
            }
            for (int i = 0; i < 3; i++)
            {
                samplingData.MinorLightOn[i] = packetContent.DequeueUInt16();
            }
            for (int i = 0; i < 3; i++)
            {
                samplingData.MajorLightOff[i] = packetContent.DequeueUInt16();
            }
            for (int i = 0; i < 3; i++)
            {
                samplingData.MinorLightOff[i] = packetContent.DequeueUInt16();
            }
        }

        public void YRGetSamplingData(out YRSamplingData samplingData)
        {
            var result = SendCommand((byte)Command.YrGetSamplingData);
            if (result.IsAckOk)
            {
                Dequeue(result.PacketContent, out samplingData);
            }
            else
            {
                throw new CommunicationException(CommunicationError.ExecuteCommandFailed);
            }
        }

        public void GetWhiteBoardNumbers(out string internalNumber, out string externalNumber)
        {
            var result = SendCommand((byte)Command.GetWhiteBoardNumber);
            if (result.IsAckOk)
            {
                try
                {
                    internalNumber = result.PacketContent.DequeueString();
                    externalNumber = result.PacketContent.DequeueString();
                }
                catch (Exception ex)
                {
                    throw new CommunicationException(CommunicationError.ParsedBadData, innerException: ex);
                }
            }
            throw new CommunicationException(CommunicationError.ExecuteCommandFailed);
        }

        public void SetWhiteBoardNumbers(string internalNumber, string externalNumber)
        {
            PacketContent content = new PacketContent();
            content.Enqueue(internalNumber);
            content.Enqueue(externalNumber);
            var result = SendCommand((byte)Command.SetWhiteBoardNumber, content);
            if (!result.IsAckOk)
            {
                throw new CommunicationException(CommunicationError.ExecuteCommandFailed);
            }
        }

        public void GetWhiteBoardParameters(out ushort R, out ushort G, out ushort B)
        {
            var result = SendCommand((byte)Command.GetWhiteBoardParameter);
            if (result.IsAckOk)
            {
                try
                {
                    R = result.PacketContent.DequeueUInt16();
                    G = result.PacketContent.DequeueUInt16();
                    B = result.PacketContent.DequeueUInt16();
                }
                catch (Exception ex)
                {
                    throw new CommunicationException(CommunicationError.ParsedBadData, innerException: ex);
                }
            }
            throw new CommunicationException(CommunicationError.ExecuteCommandFailed);
        }

        public void SetWhiteBoardParameters(ushort R, ushort G, ushort B)
        {
            PacketContent content = new PacketContent();
            content.Enqueue(R);
            content.Enqueue(G);
            content.Enqueue(B);
            var result = SendCommand((byte)Command.SetWhiteBoardParameter, content);
            if (!result.IsAckOk)
            {
                throw new CommunicationException(CommunicationError.ExecuteCommandFailed);
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

        /// <summary>
        /// 设置积分时间
        /// </summary>
        /// <param name="integration">0～255，对应积分时间1~256</param>
        public void SetIntegrationTime(byte integration)
        {
            PacketContent content = new PacketContent();
            content.Enqueue(integration);
            var result = SendCommand((byte)Command.SetIntegrationTime, content);
            if (!result.IsAckOk)
            {
                throw new CommunicationException(CommunicationError.ExecuteCommandFailed);
            }
        }

        /// <summary>
        /// 获取积分时间
        /// </summary>
        /// <returns>0～255，对应积分时间1~256</returns>
        public byte GetIntegrationTime()
        {
            var result = SendCommand((byte)Command.GetIntegrationTime);
            if (!result.IsAckOk)
            {
                throw new CommunicationException(CommunicationError.ExecuteCommandFailed);
            }

            try
            {
                return result.PacketContent.DequeueByte();
            }
            catch (Exception exception)
            {
                throw new CommunicationException(CommunicationError.ParsedBadData, innerException: exception);
            }
        }

        /// <summary>
        /// 获取增益值
        /// </summary>
        /// <returns>
        /// 增益，0～3，分别对应1、4、16、64
        /// </returns>
        public byte GetGain()
        {
            var result = SendCommand((byte)Command.GetGain);
            if (!result.IsAckOk)
            {
                throw new CommunicationException(CommunicationError.ExecuteCommandFailed);
            }

            try
            {
                return result.PacketContent.DequeueByte();
            }
            catch (Exception exception)
            {
                throw new CommunicationException(CommunicationError.ParsedBadData, innerException: exception);
            }
        }

        /// <summary>
        /// 设置增益值
        /// </summary>
        /// <param name="gain">增益，0～3，分别对应1、4、16、64</param>
        public void SetGain(byte gain)
        {
            PacketContent content = new PacketContent();
            content.Enqueue(gain);
            var result = SendCommand((byte)Command.SetGain, content);
            if (!result.IsAckOk)
            {
                throw new CommunicationException(CommunicationError.ExecuteCommandFailed);
            }
        }

        /// <summary>
        /// 要写入的白板的所有XYZ的光源
        /// </summary>
        public static readonly StandardIlluminant[] WhiteBoardXyzIlluminants = {
            StandardIlluminant.D65,
            StandardIlluminant.D50,
            StandardIlluminant.A,
            StandardIlluminant.C,
            StandardIlluminant.D55,
            StandardIlluminant.D75,
            StandardIlluminant.F1,
            StandardIlluminant.F2,
            StandardIlluminant.F3,
            StandardIlluminant.F4,
            StandardIlluminant.F5,
            StandardIlluminant.F6,
            StandardIlluminant.F7,
            StandardIlluminant.F8,
            StandardIlluminant.F9,
            StandardIlluminant.F10,
            StandardIlluminant.F11,
            StandardIlluminant.F12,
        };

        /// <summary>
        /// 设置白板XYZ值（所有光源）
        /// </summary>
        /// <param name="spectrum">白板光谱数据</param>
        /// <param name="observer">要设置的观察者角度</param>
        public void SetWhiteBoardXyzEx(Spectrum spectrum, Optical.StandardObserver observer)
        {

            PacketContent content = new PacketContent();
            content.Enqueue((byte)(observer == StandardObserver.CIE1931 ? 1 : 0));

            foreach (var illuminant in WhiteBoardXyzIlluminants)
            {
                var formula = new Formula(illuminant, observer);
                var xyz = formula.CalculateXyz(spectrum);
                content.Enqueue((float)xyz.X);
                content.Enqueue((float)xyz.Y);
                content.Enqueue((float)xyz.Z);
            }

            var result = SendCommand((byte)Command.SetWhiteBoardXyzEx, content);
            if (!result.IsAckOk)
            {
                throw new CommunicationException(CommunicationError.ExecuteCommandFailed);
            }
        }

        /// <summary>
        /// 设置白板XYZ值（所有光源），同时写入2度和10度观察角
        /// 该函数是对void SetWhiteBoardXyzEx(Spectrum spectrum, Optical.StandardObserver observer)的封装
        /// </summary>
        /// <param name="spectrum">白板光谱数据</param>
        public void SetWhiteBoardXyzEx(Spectrum spectrum)
        {
            SetWhiteBoardXyzEx(spectrum, StandardObserver.CIE1964);
            SetWhiteBoardXyzEx(spectrum, StandardObserver.CIE1931);
        }

        /// <summary>
        /// 获取仪器的白板XYZ值设置（所有光源）
        /// </summary>
        /// <param name="observer">要获取的观察角</param>
        /// <returns>18光源的XYZ值</returns>
        public Dictionary<StandardIlluminant, CIEXYZ> GetWhiteBoardXyzEx(StandardObserver observer)
        {
            PacketContent content = new PacketContent();
            content.Enqueue((byte)(observer == StandardObserver.CIE1931 ? 1 : 0));

            var result = SendCommand((byte)Command.GetWhiteBoardXyzEx, content);
            if (!result.IsAckOk)
            {
                throw new CommunicationException(CommunicationError.ExecuteCommandFailed);
            }

            try
            {
                var data = result.PacketContent;
                byte o = data.DequeueByte();

                Dictionary<StandardIlluminant, CIEXYZ> dict = new Dictionary<StandardIlluminant, CIEXYZ>();
                foreach (var illuminant in WhiteBoardXyzIlluminants)
                {
                    var x = data.DequeueFloat();
                    var y = data.DequeueFloat();
                    var z = data.DequeueFloat();
                    dict.Add(illuminant, new CIEXYZ(x, y, z));
                }

                return dict;
            }
            catch (Exception e)
            {
                throw new CommunicationException(CommunicationError.ParsedBadData, innerException: e);
            }
        }

        /// <summary>
        /// 获取仪器的白板XYZ值设置（所有光源），同时返回2度和10度
        /// 该函数是对GetWhiteBoardXyzEx(StandardObserver observer)的封装
        /// </summary>
        /// <returns>18光源的XYZ值，第一个字典返回2度，第二个字典返回10度</returns>
        public Dictionary<StandardIlluminant, CIEXYZ>[] GetWhiteBoardXyzEx()
        {
            return new Dictionary<StandardIlluminant, CIEXYZ>[]
            {
                GetWhiteBoardXyzEx(StandardObserver.CIE1931),
                GetWhiteBoardXyzEx(StandardObserver.CIE1964)
            };
        }


        public SensorData MeasureSensorData()
        {
            var result = SendCommand((byte)Command.MeasureInSensorData);
            if (!result.IsAckOk)
            {
                throw new CommunicationException(CommunicationError.ExecuteCommandFailed);
            }

            try
            {
                return DequeueSensorData(result);
            }
            catch
            {
                throw new CommunicationException(CommunicationError.ParsedBadData);
            }
        }

        /// <summary>
        /// 设置公司名称
        /// </summary>
        /// <param name="language">要设置的语言，目前只支持英文和中文</param>
        /// <param name="name">公司名称，不能超过20个字符</param>
        public void SetCompanyName(InstrumentLanguage language, string name)
        {
            if (name.Length > 20 || (language != InstrumentLanguage.English && language != InstrumentLanguage.SimplifiedChinese))
            {
                throw new CommunicationException(CommunicationError.InvalidParameter);
            }

            var content = new PacketContent();
            content.Enqueue((byte)language);
            content.Enqueue((byte)name.Length);
            foreach (var ch in name)
            {
                content.Enqueue((ushort)ch);
            }

            var result = SendCommand((byte)Command.SetCompanyName, content);
            if (!result.IsAckOk)
            {
                throw new CommunicationException(CommunicationError.ExecuteCommandFailed);
            }
        }

        public string GetCompanyName(InstrumentLanguage language)
        {
            if (language != InstrumentLanguage.English && language != InstrumentLanguage.SimplifiedChinese)
            {
                throw new CommunicationException(CommunicationError.InvalidParameter);
            }
            var content = new PacketContent();
            content.Enqueue((byte)language);
            var result = SendCommand((byte)Command.GetCompanyName, content);
            if (!result.IsAckOk)
            {
                throw new CommunicationException(CommunicationError.ExecuteCommandFailed);
            }

            int len = result.PacketContent.DequeueByte();
            char[] arr = new char[len];
            for (int i = 0; i < len; i++)
            {
                arr[i] = (char)result.PacketContent.DequeueUInt16();
            }

            return arr.ToString();
        }

        /// <summary>
        /// 设置标定系数
        /// </summary>
        /// <param name="coefficient">要设置的标定系数，其长度必须为5</param>
        public void SetCalibrationCoefficient(float[] coefficient)
        {
            if (coefficient == null)
            {
                throw new ArgumentNullException();
            }

            if (coefficient.Length != 5)
            {
                throw new CommunicationException(CommunicationError.InvalidParameter);
            }
            var content = new PacketContent();
            content.Enqueue(coefficient);


            var result = SendCommand((byte)Command.SetCalibrationCoefficient, content);
            if (!result.IsAckOk)
            {
                throw new CommunicationException(CommunicationError.ExecuteCommandFailed);
            }
        }

        public void SetInstrumentModel(ushort model, ushort subModel)
        {
            var content = new PacketContent();
            content.Enqueue(model);
            content.Enqueue(subModel);

            var result = SendCommand((byte)Command.SetModel, content);
            if (!result.IsAckOk)
            {
                throw new CommunicationException(CommunicationError.ExecuteCommandFailed);
            }
        }

        public bool GetInstrumentModel(out short model, out short subModel)
        {
            var result = SendCommand((byte)Command.GetModel);
            if (!result.IsAckOk)
            {
                throw new CommunicationException(CommunicationError.ExecuteCommandFailed);
            }

            try
            {
                model = result.PacketContent.DequeueInt16();
                subModel = result.PacketContent.DequeueInt16();
                return true;
            }
            catch (Exception exception)
            {
                throw new CommunicationException(CommunicationError.ParsedBadData, innerException: exception);  
            }
        }
        public int GetInternalType()
        {
            PacketContent content = new PacketContent();
            var result = SendCommand((byte)Command.GetInternalType);
            if (!result.IsAckOk)
            {
                throw new CommunicationException(CommunicationError.ExecuteCommandFailed);
            }
            try
            {
               return result.PacketContent.DequeueByte();
            }
            catch (Exception exception)
            {
                throw new CommunicationException(CommunicationError.ParsedBadData, innerException: exception);
            }
        }
        public List<float> GetMeasureSpectrumEight(int numbers =3)
        {
            numbers = 3;
            PacketContent content = new PacketContent();
            var result = SendCommand((byte)Command.ReadSpectrumEight);
            List<float> data = new List<float>();
            if (result != null && result.IsAckOk)
            {
                for (int j = 0; j < numbers; j++)
                {
                    float tmp2;
                    result.PacketContent.Dequeue(out tmp2);
                    data.Add(tmp2);
                }
                return data;
            }
            return null;
        }
        //传3*9数据
        public bool SetFixedPoint(List<List<float>> datas)
        {
            if (datas == null) return false;
            var rows = datas.Count;
            var cols = datas[0].Count;
            for (int i = 0; i < rows; i++)
            {
                PacketContent content = new PacketContent();
                byte index = (byte)i;
                content.Enqueue(index);
                foreach (var data in datas[i])
                {
                    content.Enqueue(data);
                }
                var result = SendCommand((byte)Command.WriteFixedPoint, content);
                if (!result.IsAckOk)
                {
                    throw new CommunicationException(CommunicationError.ExecuteCommandFailed,
                        string.Format("FixedPointed写入失败：{0}", result.Ack));
                }
            }
            return true;
        }

        public bool ResetFixedPoint()
        {
            var result = SendCommand((byte)Command.ResetFixedPoint);
            return result.IsAckOk;
        }
        public List<List<float>> ReadFixedPointSVD(int rows = 9, int cols = 3)
        {
            float[,] arry2D = new float[rows, cols];
            for (int i = 0; i < rows; i++)
            {
                PacketContent content = new PacketContent();
                content.Enqueue((byte)i);
                var result = SendCommand((byte)Command.ReadFixedPoint, content);
                if (result != null && result.IsAckOk)
                {
                    byte tmp;
                    result.PacketContent.Dequeue(out tmp);
                    if (tmp == (byte)i)
                    {
                        for (int j = 0; j < cols; j++)
                        {
                            float tmp2;
                            result.PacketContent.Dequeue(out tmp2);
                            arry2D[i, j] = tmp2;
                        }
                    }
                }
            }
            return Enumerable.Range(0, arry2D.GetLength(0)).Select(row => Enumerable.Range(0, arry2D.GetLength(1)).Select(col => arry2D[row, col]).ToList()).ToList();
        }

        public bool SetSVD(List<List<float>> datas,MeasureType? measureType = null, byte caliber = 0)
        {
            if (datas == null) return false;
            var rows = datas.Count;
            var cols = datas[0].Count;
            for (int i = 0; i < rows; i++)
            {
                PacketContent content = new PacketContent();
                byte index = (byte)i;
                content.Enqueue(index);
                foreach (var data in datas[i])
                {
                    content.Enqueue(data);
                }
                var result = SendCommand((byte)Command.WriteSVD, content);
                if (!result.IsAckOk)
                {
                    throw new CommunicationException(CommunicationError.ExecuteCommandFailed,
                        string.Format("SVD写入失败：{0}", result.Ack));
                }
            }
            return true;
        }

        public bool ResetSVD()
        {
            var result = SendCommand((byte)Command.ResetSVD);
            return result.IsAckOk;
        }


      
        public List<List<float>> ReadSVD(MeasureType? measureType = null, int rows = 3, int cols = 12)
        {
            float[,] arry2D = new float[rows, cols];
            for (int i = 0; i < rows; i++)
            {
                PacketContent content = new PacketContent();
                content.Enqueue((byte)i);
                var result = SendCommand((byte)Command.ReadSVD, content);
                if (result != null && result.IsAckOk)
                {
                    byte tmp;
                    result.PacketContent.Dequeue(out tmp);
                    if (tmp == (byte)i)
                    {
                        for (int j = 0; j < cols; j++)
                        {
                            float tmp2;
                            result.PacketContent.Dequeue(out tmp2);
                            arry2D[i, j] = tmp2;
                        }
                    }
                }
            }
            return Enumerable.Range(0, arry2D.GetLength(0)).Select(row => Enumerable.Range(0, arry2D.GetLength(1)).Select(col => arry2D[row, col]).ToList()).ToList();
        }
        public float[] GetCalibrationCoefficient()
        {

            var result = SendCommand((byte)Command.GetCalibrationCoefficient);
            if (!result.IsAckOk)
            {
                throw new CommunicationException(CommunicationError.ExecuteCommandFailed);
            }

            var coefficient = new float[5];
            try
            {
                for (int i = 0; i < coefficient.Length; i++)
                {
                    coefficient[i] = result.PacketContent.DequeueFloat();
                }
            }
            catch
            {
                throw new CommunicationException(CommunicationError.ParsedBadData);
            }

            return coefficient;
        }

        private static SensorData DequeueSensorData(DataPacket result)
        {
            SensorData data = new SensorData();
            data.Sensor1Bright = result.PacketContent.DequeueArray(result.PacketContent.DequeueUInt16, 8);
            data.Sensor2Bright = result.PacketContent.DequeueArray(result.PacketContent.DequeueUInt16, 8);
            data.Sensor1Dark = result.PacketContent.DequeueArray(result.PacketContent.DequeueUInt16, 8);
            data.Sensor2Dark = result.PacketContent.DequeueArray(result.PacketContent.DequeueUInt16, 8);
            return data;
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
    }
}
