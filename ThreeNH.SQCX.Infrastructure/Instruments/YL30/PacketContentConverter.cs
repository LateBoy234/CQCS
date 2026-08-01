using System;
using System.Collections.Generic;
using System.Linq;
using ThreeNH.SQCX.Infrastructure.Instruments.YL30.Model;
using ThreeNH.SQCX.Infrastructure.Instruments.YL30.StructHelper;

namespace ThreeNH.SQCX.Infrastructure.Instruments.YL30
{
    public  static  class PacketContentConverter
    {

        
     internal static ComSetting ToComSetting(ushort[] values)
        {
            PacketContent content = new PacketContent();
            content.Enqueue(values);
            var comSetting = StructConverter.BytesToStruct<ComSettingStruct>(content.Bytes);
            return new ComSetting()
            {
                SlaveID =((ComSettingStruct)comSetting).SlaveID,
                BaudRate = (BaudRate)((ComSettingStruct)comSetting).Rate,
                CommInterfaceType = (CommInterfaceType)((ComSettingStruct)comSetting).CommInterface
            }; 
        }

        public  static SystemInfo ToSystemInfo(List<byte> values)
        {
            if (values.Count != Constants.SYSTEM_INFO_REGISTER_COUNT * 2)
            {
                throw new ArgumentException("the length of the bytes does not match");
            }
            Dictionary<string, byte[]> content = new Dictionary<string, byte[]>
            {
                ["CalStatus"] = new byte[2],
                ["Name"] = new byte[16],
                ["SN"] = new byte[16],
                ["SoftwareVersion"] = new byte[16],
                ["HardwareVersion"] = new byte[16],
                ["ExpirtTimeExternal"] = new byte[2],
                ["ExpirtTimeInternal"] = new byte[2]
            };
            if (content.Values.SelectMany(x => x.ToList()).ToList().Count != values.Count)
            {
                throw new ArgumentException("the length of the bytes does not match");
            }
            int offset = 0;
            content["CalStatus"] = EndianConverter.Invert<byte>(values.TakeRange(offset, content["CalStatus"].Length).ToArray());
            offset += content["CalStatus"].Length;
            content["Name"] = values.TakeRange(offset, content["Name"].Length).ToArray();
            offset += content["Name"].Length;
            content["SN"] = values.TakeRange(offset, content["SN"].Length).ToArray();
            offset += content["SN"].Length;
            content["SoftwareVersion"] = values.TakeRange(offset, content["SoftwareVersion"].Length).ToArray();
            offset += content["SoftwareVersion"].Length;
            content["HardwareVersion"] = values.TakeRange(offset, content["HardwareVersion"].Length).ToArray();
            offset += content["HardwareVersion"].Length;
            content["ExpirtTimeExternal"] = EndianConverter.Invert<byte>(values.TakeRange(offset, content["ExpirtTimeExternal"].Length));
            offset += content["ExpirtTimeExternal"].Length;
            content["ExpirtTimeInternal"] = EndianConverter.Invert<byte>(values.TakeRange(offset, content["ExpirtTimeInternal"].Length));

            var bytes = content.Values.SelectMany(x => x.ToList()).ToArray();
            if (bytes.Length != Constants.SYSTEM_INFO_REGISTER_COUNT * 2)
            {
                throw new ArgumentException("transfer byte errors");
            }
            var systemInfoStruct = StructConverter.BytesToStruct<SystemInfoStruct>(bytes);
            SystemInfo systemInfo = new SystemInfo()
            {
                CalibrationStatus = (CalibrationStatus)systemInfoStruct.CalbirationStatus,
                Name = systemInfoStruct.Name,
                SN = systemInfoStruct.SN,
                SoftwareVersion = systemInfoStruct.SoftwareVersion,
                HardwareVersion = systemInfoStruct.HardwareVersion,
                ExpirtTimeExternal = systemInfoStruct.ExpirtTimeExternal,
                ExpirtTimeInternal = systemInfoStruct.ExpirtTimeInternal,
            }
            ;
            return systemInfo;
        }

        public static ToleranceForYL30 ToSystemTolerance(List<byte> values)
        {
            if (values.Count != Constants.SYSTEM_TOLERANCE * 2)
            {
                throw new ArgumentException("the length of the bytes does not match");
            }
            var positions = StructConverter.BytesToStruct<SystemtToleranceStruct>(values);
            return new ToleranceForYL30()
            {
                Values=new float[7],
                dE = positions.DeltaValue,
                dL_Lower = positions.Value1Lower,
                dL_Upper = positions.Value1Upper,
                da_Lower = positions.Value2Lower,
                da_Upper = positions.Value2Upper,
                db_Lower = positions.Value3Lower,
                db_Upper = positions.Value3Upper,
            };
        }

        //internal static Standard ToStandard(List<byte> values)
        //{
        //    if (values.Count != Constants.STANDARD_COUNT * 2)
        //    {
        //        throw new ArgumentException("the length of the bytes does not match");
        //    }
        //    Dictionary<string, byte[]> content = new Dictionary<string, byte[]>
        //    {
        //        ["colorValue"] = new byte[40],
        //        ["sciSce"] = new byte[124],
        //        ["id"] = new byte[4],
        //        ["name"] = new byte[16],
        //        ["time"] = new byte[12],
        //    };
        //    if (content.Values.SelectMany(x => x.ToList()).ToList().Count != values.Count)
        //    {
        //        throw new ArgumentException("the length of the bytes does not match");
        //    }
        //    //颜色空间string值
        //    int offset = 0;
        //    content["colorValue"] = values.TakeRange(offset, content["colorValue"].Length).ToArray();
        //    offset += content["colorValue"].Length;
        //    content["sciSce"] = EndianConverter.BigEndianToLittleEndianForUShort(values.TakeRange(offset, content["sciSce"].Length).ToList()).ToArray();
        //    offset += content["sciSce"].Length;
        //    content["id"] = values.TakeRange(offset, content["id"].Length).ToArray();
        //    offset += content["id"].Length;
        //    content["name"] = values.TakeRange(offset, content["name"].Length).ToArray();
        //    offset += content["name"].Length;
        //    content["time"] = EndianConverter.BigEndianToLittleEndianForUShort(values.TakeRange(offset, content["time"].Length).ToList()).ToArray();
        //    var bytes = content.Values.SelectMany(x => x.ToList()).ToArray();
        //    var standardStruct = StructConverter.BytesToStruct<StandardStruct>(bytes);
        //    Standard standard = new Standard()
        //    {
        //        Id = Convert.ToString(standardStruct.ID),
        //        ColorFormula = standardStruct.ColorSpaceValue,
        //        DateTime = standardStruct.Time.ToDateTime(),
        //        Name = standardStruct.Name,
        //    };
        //    SpectralData data = new SpectralData(Constants.VALID_WAVE_BEGIN, Constants.VALID_WAVE_STEP, Constants.VALID_WAVE_NUM, Array.ConvertAll(standardStruct.SCIReflectance.ToArray(), x => (float)x * 0.0001));
        //    standard.SetColorData("sci", data);
        //    return standard;
        //}

        //internal static Sample ToSample(List<byte> values)
        //{
        //    if (values.Count != Constants.SAMPLE_COUNT * 2)
        //    {
        //        throw new ArgumentException("the length of the bytes does not match");
        //    }
        //    Dictionary<string, byte[]> content = new Dictionary<string, byte[]>
        //    {
        //        ["colorValue"] = new byte[60],
        //        ["sciSce"] = new byte[124],
        //        ["standardId"] = new byte[4],
        //        ["id"] = new byte[4],
        //        ["name"] = new byte[16],
        //        ["time"] = new byte[12],
        //    };
        //    if (content.Values.SelectMany(x => x.ToList()).ToList().Count != values.Count)
        //    {
        //        throw new ArgumentException("the length of the bytes does not match");
        //    }
        //    //颜色空间string值
        //    int offset = 0;
        //    content["colorValue"] = values.TakeRange(offset, content["colorValue"].Length).ToArray();
        //    offset += content["colorValue"].Length;
        //    content["sciSce"] = EndianConverter.BigEndianToLittleEndianForUShort(values.TakeRange(offset, content["sciSce"].Length).ToList()).ToArray();
        //    offset += content["sciSce"].Length;
        //    content["standardId"] = values.TakeRange(offset, content["standardId"].Length).ToArray();
        //    offset += content["standardId"].Length;
        //    content["id"] = values.TakeRange(offset, content["id"].Length).ToArray();
        //    offset += content["id"].Length;
        //    content["name"] = values.TakeRange(offset, content["name"].Length).ToArray();
        //    offset += content["name"].Length;
        //    content["time"] = EndianConverter.BigEndianToLittleEndianForUShort(values.TakeRange(offset, content["time"].Length).ToList()).ToArray();
        //    var bytes = content.Values.SelectMany(x => x.ToList()).ToArray();
        //    var sampleStruct = StructConverter.BytesToStruct<SampleStruct>(bytes);
        //    Sample sample = new Sample()
        //    {
        //        StandardId = Convert.ToString(sampleStruct.StandardID),
        //        Id = Convert.ToString(sampleStruct.SampleID),
        //        ColorFormula = sampleStruct.ColorSpaceValue,
        //        DateTime = sampleStruct.Time.ToDateTime(),
        //        Name = sampleStruct.Name,
        //    };
        //    SpectralData data = new SpectralData(Constants.VALID_WAVE_BEGIN, Constants.VALID_WAVE_STEP, Constants.VALID_WAVE_NUM, Array.ConvertAll(sampleStruct.SCIReflectance.ToArray(), x => (float)x * 0.0001));
        //    sample.SetColorData("sci", data);
        //    return sample;
        //}

    }
}
