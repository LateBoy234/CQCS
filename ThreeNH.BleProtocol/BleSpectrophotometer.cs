using System;
using System.Runtime.InteropServices;
using System.Text;
using ThreeNH.CommunicationBase;

namespace ThreeNH.BleProtocol
{
    public class BleSpectrophotometer<StandardRecord, TrialRecord> : IDisposable
    {
        private IntPtr _handle = IntPtr.Zero;


        public  BleSpectrophotometer()
        {
            if (!BleProtocolWrapper.IsSupported)
            {
                throw new PlatformNotSupportedException();
            }
        }

        public BleSpectrophotometer(IntPtr handle)
        {
            if (handle == null) throw  new ArgumentNullException();
            _handle = handle;
        }

        ~BleSpectrophotometer()
        {
            Close();
        }

        public void Dispose()
        {
            Close();
        }

        public IntPtr DetachHandle()
        {
            var handle = _handle;
            _handle = IntPtr.Zero;
            return handle;
        }

        public void AttachHandle(IntPtr handle)
        {
            if (_handle != IntPtr.Zero)
            {
                Close();
            }
            _handle = handle;
        }

        public bool IsCalibrated
        {
            get
            {
                if (_handle == IntPtr.Zero)
                {
                    throw new CommunicationException(CommunicationError.DeviceNotOpen);
                }

                var result = BleProtocolWrapper.tnhble_IsCalibrated(_handle);
                var errorCode = BleProtocolWrapper.tnhble_GetErrorCode(_handle);
                if (errorCode != BleProtocolWrapper.ErrorCode.Success)
                {
                    throw errorCode.ToCommunicationException();
                }
                return result;
            }
        }
        public void DoWhiteCalibration()
        {
            if (_handle == IntPtr.Zero)
            {
                throw new CommunicationException(CommunicationError.DeviceNotOpen);
            }

            var result = BleProtocolWrapper.tnhble_DoWhiteCalibration(_handle);
            var errorCode = BleProtocolWrapper.tnhble_GetErrorCode(_handle);
            if (errorCode != BleProtocolWrapper.ErrorCode.Success)
            {
                throw errorCode.ToCommunicationException();
            }

            if (!result)
            {
                throw new CommunicationException(CommunicationError.ExecuteCommandFailed);
            }
        }

        public void DoBlackCalibration()
        {
            if (_handle == IntPtr.Zero)
            {
                throw new CommunicationException(CommunicationError.DeviceNotOpen);
            }

            var result = BleProtocolWrapper.tnhble_DoBlackCalibration(_handle);
            var errorCode = BleProtocolWrapper.tnhble_GetErrorCode(_handle);
            if (errorCode != BleProtocolWrapper.ErrorCode.Success)
            {
                throw errorCode.ToCommunicationException();
            }

            if (!result)
            {
                throw new CommunicationException(CommunicationError.ExecuteCommandFailed);
            }
        }

        public StandardRecord Measure()
        {
            if (_handle == IntPtr.Zero)
            {
                throw new CommunicationException(CommunicationError.DeviceNotOpen);
            }

            var bufPtr = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(StandardRecord)));
            try
            {
                BleProtocolWrapper.tnhble_MeasureStandard(_handle, bufPtr, Marshal.SizeOf(typeof(StandardRecord)));

                var errorCode = BleProtocolWrapper.tnhble_GetErrorCode(_handle);
                if (errorCode != BleProtocolWrapper.ErrorCode.Success)
                {
                    throw errorCode.ToCommunicationException();
                }

                return (StandardRecord)Marshal.PtrToStructure(bufPtr, typeof(StandardRecord));
            }
            finally
            {
                Marshal.FreeHGlobal(bufPtr);
            }
        }

        public StandardRecord MeasureStandard()
        {
            if (_handle == IntPtr.Zero)
            {
                throw new CommunicationException(CommunicationError.DeviceNotOpen);
            }

            var ptr = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(StandardRecord)));
            try
            {
                BleProtocolWrapper.tnhble_MeasureStandard(_handle, ptr, Marshal.SizeOf(typeof(StandardRecord)));
                
                var errorCode = BleProtocolWrapper.tnhble_GetErrorCode(_handle);
                if (errorCode != BleProtocolWrapper.ErrorCode.Success)
                {
                    throw errorCode.ToCommunicationException();
                }

                return (StandardRecord) Marshal.PtrToStructure(ptr, typeof(StandardRecord));
            }
            finally
            {
                Marshal.FreeHGlobal(ptr);
            }
        }

        public TrialRecord MeasureSample()
        {
            if (_handle == IntPtr.Zero)
            {
                throw new CommunicationException(CommunicationError.DeviceNotOpen);
            }

            IntPtr ptr = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(TrialRecord)));
            try
            {
                BleProtocolWrapper.tnhble_MeasureSample(_handle, ptr, Marshal.SizeOf(typeof(TrialRecord)));
                

                var errorCode = BleProtocolWrapper.tnhble_GetErrorCode(_handle);
                if (errorCode != BleProtocolWrapper.ErrorCode.Success)
                {
                    throw errorCode.ToCommunicationException();
                }

                return (TrialRecord)Marshal.PtrToStructure(ptr, typeof(TrialRecord));
            }
            finally
            {
                Marshal.FreeHGlobal(ptr);
            }

        }

        public string DeviceName
        {
            get
            {
                if (_handle == IntPtr.Zero)
                {
                    throw new CommunicationException(CommunicationError.DeviceNotOpen);
                }

                StringBuilder builder = new StringBuilder(256);
                BleProtocolWrapper.tnhble_GetDeviceName(_handle, builder, builder.Capacity);

                var errorCode = BleProtocolWrapper.tnhble_GetErrorCode(_handle);
                if (errorCode != BleProtocolWrapper.ErrorCode.Success)
                {
                    throw errorCode.ToCommunicationException();
                }

                return builder.ToString();
            }
        }

        public UInt64 BluetoothAddress
        {
            get
            {
                if (_handle == IntPtr.Zero)
                {
                    throw new CommunicationException(CommunicationError.DeviceNotOpen);
                }

                return BleProtocolWrapper.tnhble_GetDeviceAddress(_handle);
            }
        }

        public bool IsOpen
        {
            get { return _handle != IntPtr.Zero; }
        }

        public void Open(object commDevice = null)
        {
            if (commDevice != null && !(commDevice is UInt64))
            {
                throw new CommunicationException(CommunicationError.InvalidParameter, 
                    "the commDevice must be 'null' or UInt64");
            }

            if (_handle != IntPtr.Zero)
            {
                Close();
            }

            _handle = BleProtocolWrapper.tnhble_Connect((ulong?) commDevice ?? 0);

            if (_handle == IntPtr.Zero)
            {
                throw new CommunicationException(CommunicationError.DeviceOpenFailed);
            }
        }

        public void Close()
        {
            if (_handle != IntPtr.Zero)
            {
                BleProtocolWrapper.tnhble_Disconnect(_handle);
                _handle = IntPtr.Zero;
            }
        }

        public string Model
        {
            get
            {
                if (_handle == IntPtr.Zero)
                {
                    throw new CommunicationException(CommunicationError.DeviceNotOpen);
                }

                StringBuilder builder = new StringBuilder(20);
                BleProtocolWrapper.tnhble_GetDeviceModel(_handle, builder, builder.Capacity);

                var errorCode = BleProtocolWrapper.tnhble_GetErrorCode(_handle);
                if (errorCode != BleProtocolWrapper.ErrorCode.Success)
                {
                    throw errorCode.ToCommunicationException();
                }

                return builder.ToString();
            }
        }

        public string SN
        {
            get
            {
                if (_handle == IntPtr.Zero)
                {
                    throw new CommunicationException(CommunicationError.DeviceNotOpen);
                }

                StringBuilder builder = new StringBuilder(20);
                BleProtocolWrapper.tnhble_GetSerialNumber(_handle, builder, builder.Capacity);

                var errorCode = BleProtocolWrapper.tnhble_GetErrorCode(_handle);
                if (errorCode != BleProtocolWrapper.ErrorCode.Success)
                {
                    throw errorCode.ToCommunicationException();
                }

                return builder.ToString();
            }
        }

        public string HardwareRevision
        {
            get
            {
                if (_handle == IntPtr.Zero)
                {
                    throw new CommunicationException(CommunicationError.DeviceNotOpen);
                }

                StringBuilder builder = new StringBuilder(20);
                BleProtocolWrapper.tnhble_GetHardwareRevision(_handle, builder, builder.Capacity);

                var errorCode = BleProtocolWrapper.tnhble_GetErrorCode(_handle);
                if (errorCode != BleProtocolWrapper.ErrorCode.Success)
                {
                    throw errorCode.ToCommunicationException();
                }

                return builder.ToString();
            }
        }

        public string FirmwareRevision
        {
            get
            {
                if (_handle == IntPtr.Zero)
                {
                    throw new CommunicationException(CommunicationError.DeviceNotOpen);
                }

                StringBuilder builder = new StringBuilder(20);
                BleProtocolWrapper.tnhble_GetFirmwareRevision(_handle, builder, builder.Capacity);

                var errorCode = BleProtocolWrapper.tnhble_GetErrorCode(_handle);
                if (errorCode != BleProtocolWrapper.ErrorCode.Success)
                {
                    throw errorCode.ToCommunicationException();
                }

                return builder.ToString();
            }
        }

        public string SoftwareRevision
        {
            get
            {
                if (_handle == IntPtr.Zero)
                {
                    throw new CommunicationException(CommunicationError.DeviceNotOpen);
                }

                StringBuilder builder = new StringBuilder(20);
                BleProtocolWrapper.tnhble_GetSoftwareRevision(_handle, builder, builder.Capacity);

                var errorCode = BleProtocolWrapper.tnhble_GetErrorCode(_handle);
                if (errorCode != BleProtocolWrapper.ErrorCode.Success)
                {
                    throw errorCode.ToCommunicationException();
                }

                return builder.ToString();
            }
        }

        public string Manufacturer
        {
            get
            {
                if (_handle == IntPtr.Zero)
                {
                    throw new CommunicationException(CommunicationError.DeviceNotOpen);
                }

                StringBuilder builder = new StringBuilder(20);
                BleProtocolWrapper.tnhble_GetManufacturer(_handle, builder, builder.Capacity);

                var errorCode = BleProtocolWrapper.tnhble_GetErrorCode(_handle);
                if (errorCode != BleProtocolWrapper.ErrorCode.Success)
                {
                    throw errorCode.ToCommunicationException();
                }

                return builder.ToString();
            }
        }

        public string PnpId
        {
            get
            {
                if (_handle == IntPtr.Zero)
                {
                    throw new CommunicationException(CommunicationError.DeviceNotOpen);
                }

                StringBuilder builder = new StringBuilder(20);
                BleProtocolWrapper.tnhble_GetPnpId(_handle, builder, builder.Capacity);

                var errorCode = BleProtocolWrapper.tnhble_GetErrorCode(_handle);
                if (errorCode != BleProtocolWrapper.ErrorCode.Success)
                {
                    throw errorCode.ToCommunicationException();
                }

                return builder.ToString();
            }
        }

        public int TotalStandards
        {
            get
            {
                if (_handle == IntPtr.Zero)
                {
                    throw new CommunicationException(CommunicationError.DeviceNotOpen);
                }

                var result = BleProtocolWrapper.tnhble_GetTotalStandards(_handle);
                var errorCode = BleProtocolWrapper.tnhble_GetErrorCode(_handle);
                if (errorCode != BleProtocolWrapper.ErrorCode.Success)
                {
                    throw errorCode.ToCommunicationException();
                }

                return result;
            }
        }

        public int TotalSamples
        {
            get
            {
                if (_handle == IntPtr.Zero)
                {
                    throw new CommunicationException(CommunicationError.DeviceNotOpen);
                }

                var result = BleProtocolWrapper.tnhble_GetTotalSamples(_handle);
                var errorCode = BleProtocolWrapper.tnhble_GetErrorCode(_handle);
                if (errorCode != BleProtocolWrapper.ErrorCode.Success)
                {
                    throw errorCode.ToCommunicationException();
                }

                return result;
            }
        }

        public int GetSamplesCount(int standardIndex)
        {
            if (_handle == IntPtr.Zero)
            {
                throw new CommunicationException(CommunicationError.DeviceNotOpen);
            }

            var result = BleProtocolWrapper.tnhble_GetSampleCount(_handle, standardIndex);
            var errorCode = BleProtocolWrapper.tnhble_GetErrorCode(_handle);
            if (errorCode != BleProtocolWrapper.ErrorCode.Success)
            {
                throw errorCode.ToCommunicationException();
            }

            return result;

        }

        public int GetSamplesCountById(int standardId)
        {
            if (_handle == IntPtr.Zero)
            {
                throw new CommunicationException(CommunicationError.DeviceNotOpen);
            }

            var result = BleProtocolWrapper.tnhble_GetSampleCountById(_handle, standardId);
            var errorCode = BleProtocolWrapper.tnhble_GetErrorCode(_handle);
            if (errorCode != BleProtocolWrapper.ErrorCode.Success)
            {
                throw errorCode.ToCommunicationException();
            }

            return result;
        }

        public StandardRecord UploadStandard(int standardIndex)
        {
            var ptr = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(StandardRecord)));
            try
            {
                BleProtocolWrapper.tnhble_UploadStandard(_handle, standardIndex, ptr, Marshal.SizeOf(typeof(StandardRecord)));
                var errorCode = BleProtocolWrapper.tnhble_GetErrorCode(_handle);
                if (errorCode != BleProtocolWrapper.ErrorCode.Success)
                {
                    throw errorCode.ToCommunicationException();
                }

                var record = (StandardRecord) Marshal.PtrToStructure(ptr, typeof(StandardRecord));
                return record;
            }
            finally
            {
                Marshal.FreeHGlobal(ptr);
            }
        }

        public StandardRecord UploadStandardById(int standardId)
        {
            var ptr = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(StandardRecord)));
            try
            {
                BleProtocolWrapper.tnhble_UploadStandardById(_handle, standardId, ptr, Marshal.SizeOf(typeof(StandardRecord)));
                var errorCode = BleProtocolWrapper.tnhble_GetErrorCode(_handle);
                if (errorCode != BleProtocolWrapper.ErrorCode.Success)
                {
                    throw errorCode.ToCommunicationException();
                }

                return (StandardRecord) Marshal.PtrToStructure(ptr, typeof(StandardRecord));
            }
            finally
            {
                Marshal.FreeHGlobal(ptr);
            }
        }

        public StandardRecord[] UploadStandards(int startIndex, int count)
        {
            if (count < 0)
            {
                count = TotalStandards - startIndex;
            }

            var buf = new StandardRecord[count];
            for (int i = 0; i < count; i++)
            {
                buf[i] = UploadStandard(startIndex + i);
            }

            return buf;
        }

        public TrialRecord UploadSample(int standardIndex, int sampleIndex)
        {
            var ptr = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(TrialRecord)));
            try
            {
                BleProtocolWrapper.tnhble_UploadSample(_handle, standardIndex, sampleIndex, ptr, Marshal.SizeOf(typeof(TrialRecord)));
                var errorCode = BleProtocolWrapper.tnhble_GetErrorCode(_handle);
                if (errorCode != BleProtocolWrapper.ErrorCode.Success)
                {
                    throw errorCode.ToCommunicationException();
                }

                return (TrialRecord) Marshal.PtrToStructure(ptr, typeof(TrialRecord));
            }
            finally
            {
                Marshal.FreeHGlobal(ptr);
            }
        }

        public TrialRecord UploadSampleById(int standardId, int sampleIndex)
        {
            var ptr = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(TrialRecord)));
            try
            {
                BleProtocolWrapper.tnhble_UploadSampleById(_handle, standardId, sampleIndex, ptr, Marshal.SizeOf(typeof(TrialRecord)));
                var errorCode = BleProtocolWrapper.tnhble_GetErrorCode(_handle);
                if (errorCode != BleProtocolWrapper.ErrorCode.Success)
                {
                    throw errorCode.ToCommunicationException();
                }

                return (TrialRecord) Marshal.PtrToStructure(ptr, typeof(TrialRecord));
            }
            finally
            {
                Marshal.FreeHGlobal(ptr);
            }
        }

        public TrialRecord[] UploadSamples(int standardIndex, int startSampleIndex, int count)
        {
            if (count < 0)
            {
                count = GetSamplesCount(standardIndex) - startSampleIndex;
            }
            var buf = new TrialRecord[count];
            for (int i = 0; i < count; i++)
            {
                buf[i] = UploadSample(standardIndex, startSampleIndex + i);
            }

            return buf;
        }

        public TrialRecord[] UploadSamplesById(int standardId, int startSampleIndex, int count)
        {
            if (count < 0)
            {
                count = GetSamplesCountById(standardId) - startSampleIndex;
            }
            var buf = new TrialRecord[count];
            for (int i = 0; i < count; i++)
            {
                buf[i] = UploadSampleById(standardId, startSampleIndex + i);
            }

            return buf;
        }


        public void RemoveAllRecords()
        {
            BleProtocolWrapper.tnhble_DeleteStandard(_handle, -1);
            var errorCode = BleProtocolWrapper.tnhble_GetErrorCode(_handle);
            if (errorCode != BleProtocolWrapper.ErrorCode.Success)
            {
                throw errorCode.ToCommunicationException();
            }
        }

        public void RemoveAllSamples()
        {
            BleProtocolWrapper.tnhble_DeleteSample(_handle, -1, -1);
            var errorCode = BleProtocolWrapper.tnhble_GetErrorCode(_handle);
            if (errorCode != BleProtocolWrapper.ErrorCode.Success)
            {
                throw errorCode.ToCommunicationException();
            }
        }

        public void RemoveAllSamples(int standardIndex)
        {
            BleProtocolWrapper.tnhble_DeleteSample(_handle, standardIndex, -1);
            var errorCode = BleProtocolWrapper.tnhble_GetErrorCode(_handle);
            if (errorCode != BleProtocolWrapper.ErrorCode.Success)
            {
                throw errorCode.ToCommunicationException();
            }
        }

        public void RemoveAllSamplesById(int standardId)
        {
            BleProtocolWrapper.tnhble_DeleteSampleById(_handle, standardId, -1);
            var errorCode = BleProtocolWrapper.tnhble_GetErrorCode(_handle);
            if (errorCode != BleProtocolWrapper.ErrorCode.Success)
            {
                throw errorCode.ToCommunicationException();
            }
        }

        public void DeleteStandard(int standardIndex)
        {
            BleProtocolWrapper.tnhble_DeleteStandard(_handle, standardIndex);
            var errorCode = BleProtocolWrapper.tnhble_GetErrorCode(_handle);
            if (errorCode != BleProtocolWrapper.ErrorCode.Success)
            {
                throw errorCode.ToCommunicationException();
            }
        }

        public void DeleteStandardById(int standardId)
        {
            BleProtocolWrapper.tnhble_DeleteStandardById(_handle, standardId);
            var errorCode = BleProtocolWrapper.tnhble_GetErrorCode(_handle);
            if (errorCode != BleProtocolWrapper.ErrorCode.Success)
            {
                throw errorCode.ToCommunicationException();
            }
        }

        public void DeleteSample(int standardIndex, int sampleIndex)
        {
            BleProtocolWrapper.tnhble_DeleteSampleById(_handle, standardIndex, sampleIndex);
            var errorCode = BleProtocolWrapper.tnhble_GetErrorCode(_handle);
            if (errorCode != BleProtocolWrapper.ErrorCode.Success)
            {
                throw errorCode.ToCommunicationException();
            }
        }

        public void DeleteSampleById(int standardId, int sampleIndex)
        {
            BleProtocolWrapper.tnhble_DeleteSampleById(_handle, standardId, sampleIndex);
            var errorCode = BleProtocolWrapper.tnhble_GetErrorCode(_handle);
            if (errorCode != BleProtocolWrapper.ErrorCode.Success)
            {
                throw errorCode.ToCommunicationException();
            }
        }

        public void DownloadStandard(StandardRecord record)
        {
            var ptr = Marshal.AllocHGlobal(Marshal.SizeOf(record));
            try
            {
                Marshal.StructureToPtr(record, ptr, false);
                BleProtocolWrapper.tnhble_InputStandard(_handle, ptr, Marshal.SizeOf(record));
                var errorCode = BleProtocolWrapper.tnhble_GetErrorCode(_handle);
                if (errorCode != BleProtocolWrapper.ErrorCode.Success)
                {
                    throw errorCode.ToCommunicationException();
                }
            }
            finally
            {
                Marshal.FreeHGlobal(ptr);
            }
        }
    }
}
