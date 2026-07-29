﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using ThreeNH.ColorAlgorithm;
using ThreeNH.ColorimeterInterface;
using ThreeNH.ColorSpecimen;
using ThreeNH.CommunicationBase;
using ThreeNH.DataModel;
using ThreeNH.BleProtocol;
using ThreeNH.Instruments.DeviceInfo;

namespace ThreeNH.SQCX.Infrastructure.Instruments.BTS
{
     public class BleSpectrophotometer : IInstrument, IRecordManagement
    {
        private ThreeNH.BleProtocol.BleSpectrophotometer<StandardRecord, TrialRecord> _impl = null;

        public  BleSpectrophotometer()
        {
            _impl = new BleSpectrophotometer<StandardRecord, TrialRecord>();
        }

        public BleSpectrophotometer(BleSpectrophotometer<StandardRecord, TrialRecord> impl)
        {
            _impl = impl ?? throw new ArgumentNullException();
        }

        public BleSpectrophotometer(IntPtr handle)
        {
            _impl = new BleSpectrophotometer<StandardRecord, TrialRecord>(handle);
        }

        public bool IsCalibrated
        {
            get
            {
                if (!_impl.IsOpen)
                {
                    throw new CommunicationException(CommunicationError.DeviceNotOpen);
                }
//                由于蓝牙模块有一处Bug，会导致读取的校准状态总是为false，所以在这里总是返回true，以避免不能测量。
//                var result = BtsBleWrapper.bts_IsCalibrated(_handle);
//                var errorCode = BtsBleWrapper.bts_GetErrorCode(_handle);
//                if (errorCode != BtsBleWrapper.ErrorCode.Success)
//                {
//                    throw errorCode.ToCommunicationException();
//                }
//
//                return result;
                return true;
            }
        }

        public void DoWhiteCalibration()
        {
            if (_impl == null)
            {
                throw new CommunicationException(CommunicationError.DeviceNotOpen);
            }
            _impl.DoWhiteCalibration();
        }

        public void DoBlackCalibration()
        {
            if (_impl == null)
            {
                throw new CommunicationException(CommunicationError.DeviceNotOpen);
            }

            _impl.DoBlackCalibration();
        }

        public Specimen Measure()
        {
            if (_impl == null)
            {
                throw new CommunicationException(CommunicationError.DeviceNotOpen);
            }

            var specimen = _impl.MeasureStandard().ToSpecimen();
            specimen.Name = null;
            specimen.ID = 0;
            return specimen;
        }

        public Specimen MeasureStandard()
        {
            if (_impl == null)
            {
                throw new CommunicationException(CommunicationError.DeviceNotOpen);
            }

            var specimen = _impl.MeasureStandard().ToSpecimen();
            specimen.Name = null;
            specimen.ID = 0;
            return specimen;
        }

        public Specimen MeasureSample()
        {
            if (_impl == null)
            {
                throw new CommunicationException(CommunicationError.DeviceNotOpen);
            }

            var specimen = _impl.MeasureSample().ToSpecimen();
            specimen.Name = null;
            specimen.ID = 0;
            return specimen;
        }

        public string DeviceName
        {
            get
            {
                return _impl?.DeviceName ?? throw new CommunicationException(CommunicationError.DeviceNotOpen);
            }
        }

        public UInt64 BluetoothAddress
        {
            get
            {
                return _impl?.BluetoothAddress ?? throw new CommunicationException(CommunicationError.DeviceNotOpen);
            }
        }

        public bool IsOpen
        {
            get { return _impl?.IsOpen == true; }
        }

        public void Open(object commDevice = null)
        {
            _impl.Open(commDevice);
        }

        public void Close()
        {
            _impl.Close();
        }

        public string Model
        {
            get
            {
                return _impl?.Model ?? throw new CommunicationException(CommunicationError.DeviceNotOpen);
            }
        }

        public string SN
        {
            get { return _impl?.SN ?? throw new CommunicationException(CommunicationError.DeviceNotOpen); }
        }

        public string HardwareRevision
        {
            get
            {
                return _impl?.HardwareRevision ?? throw new CommunicationException(CommunicationError.DeviceNotOpen);
            }
        }

        public string FirmwareRevision
        {
            get
            {
                return _impl?.FirmwareRevision ?? throw new CommunicationException(CommunicationError.DeviceNotOpen);
            }
        }

        public string SoftwareRevision
        {
            get
            {
                return _impl?.SoftwareRevision ?? throw new CommunicationException(CommunicationError.DeviceNotOpen);
            }
        }

        public string Manufacturer
        {
            get
            {
                return _impl?.Manufacturer ?? throw new CommunicationException(CommunicationError.DeviceNotOpen);
            }
        }

        public string OpticalStructure
        {
            get { return "%R:d/8,%T:d/0"; }
        }

        public string WhiteBoardNumber
        {
            get { return ""; }
        }

        public DeviceType DeviceType
        {
            get { return DeviceType.Spectrophotometer; }
        }

        public ThreeNH.Instruments.DeviceInfo.WavelengthBand WavelengthRange
        {
            get {return new ThreeNH.Instruments.DeviceInfo.WavelengthBand(360, 780, 10);}
        }

        public int TotalStandards
        {
            get { return _impl.TotalStandards; }
        }

        public int TotalSamples
        {
            get
            {
                return _impl.TotalSamples;
            }
        }

        public int GetSamplesCount(int standardIndex)
        {
            return _impl.GetSamplesCount(standardIndex);

        }

        public int GetSamplesCountById(int standardId)
        {
            return _impl.GetSamplesCountById(standardId);
        }

        public StandardSpecimen UploadStandard(int standardIndex)
        {
            return _impl.UploadStandard(standardIndex).ToSpecimen();
        }

        public StandardSpecimen UploadStandardById(int standardId)
        {
            return _impl.UploadStandardById(standardId).ToSpecimen();
        }

        public StandardSpecimen[] UploadStandards(int startIndex, int count)
        {
            if (count < 0)
            {
                count = TotalStandards - startIndex;
            }

            var buf = new StandardSpecimen[count];
            for (int i = 0; i < count; i++)
            {
                buf[i] = UploadStandard(startIndex + i);
            }

            return buf;
        }

        public Specimen UploadSample(int standardIndex, int sampleIndex)
        {
            return _impl.UploadSample(standardIndex, sampleIndex).ToSpecimen();
        }

        public Specimen UploadSampleById(int standardId, int sampleIndex)
        {
            return _impl.UploadSampleById(standardId, sampleIndex).ToSpecimen();
        }

        public Specimen[] UploadSamples(int standardIndex, int startSampleIndex, int count)
        {
            if (count < 0)
            {
                count = GetSamplesCount(standardIndex) - startSampleIndex;
            }
            var buf = new Specimen[count];
            for (int i = 0; i < count; i++)
            {
                buf[i] = UploadSample(standardIndex, startSampleIndex + i);
            }

            return buf;
        }

        public Specimen[] UploadSamplesById(int standardId, int startSampleIndex, int count)
        {
            if (count < 0)
            {
                count = GetSamplesCountById(standardId) - startSampleIndex;
            }
            var buf = new Specimen[count];
            for (int i = 0; i < count; i++)
            {
                buf[i] = UploadSampleById(standardId, startSampleIndex + i);
            }

            return buf;
        }

        public void UploadStandardAsync(EventHandler<SpecimenReceivedEventArgs> handler, int @from = 0, int count = -1)
        {
            throw new NotImplementedException();
        }

        public void UploadTestAsync(EventHandler<SpecimenReceivedEventArgs> handler, int standard, int @from = 0, int count = -1)
        {
            throw new NotImplementedException();
        }

        // IRecordManagement/IInstrument 新增成员：BLE 封装暂未暴露对应能力
        public void StopUpload()
        {
            throw new NotImplementedException();
        }

        public void RefreshInstrumentInfo()
        {
            // BLE 连接后设备信息由协议层维护，此处无额外拉取动作
        }

        public void RemoveAllRecords()
        {
            _impl.RemoveAllRecords();
        }

        public void RemoveAllSamples()
        {
            _impl.RemoveAllSamples();
        }

        public void RemoveAllSamples(int standardIndex)
        {
            _impl.RemoveAllSamples(standardIndex);
        }

        public void RemoveAllSamplesById(int standardId)
        {
            _impl.RemoveAllSamplesById(standardId);
        }

        public void DeleteStandard(int standardIndex)
        {
            _impl.DeleteStandard(standardIndex);
        }

        public void DeleteStandardById(int standardId)
        {
            _impl.DeleteStandardById(standardId);
        }

        public void DeleteSample(int standardIndex, int sampleIndex)
        {
            _impl.DeleteSample(standardIndex, sampleIndex);
        }

        public void DeleteSampleById(int standardId, int sampleIndex)
        {
            _impl.DeleteSampleById(standardId, sampleIndex);
        }

        public void DownloadStandard(Specimen standard)
        {
            var record = StandardRecord.FromSpecimen(standard);
            _impl.DownloadStandard(record);
        }

        public void DownloadStandards(IEnumerable<Specimen> standards)
        {
            foreach (var standard in standards)
            {
                DownloadStandard(standard);
            }
        }

        public void Backup(StreamWriter stream)
        {
            throw new NotImplementedException();
        }

        public void Restory(StreamReader stream)
        {
            throw new NotImplementedException();
        }
    }
}
