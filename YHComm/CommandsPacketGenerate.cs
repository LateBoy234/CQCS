using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using ThreeNH.ColorimeterInterface;
using ThreeNH.CommunicationBase;
using ThreeNH.StructHelper;

namespace ThreeNH.YHComm
{

    public enum MeasurementParameter
    {
        SpectrumMeasurementMode,    // 光谱类型，反射/透射
        SpecularComponent,          // SCI/SCE设置
        LensSwitchMode,             // 透镜切换方式
        LensPosition,               // 透镜位置
        MeasurementAperture,        // 口径
        UvMode,                     // UV模式
        StandardIlluminant,         // 标准光源
        StandardObserver,           // 标准视场角
        ColorSpace,                 // 颜色空间
        ColorDiffFormula,           // 色差公式
        ColorIndex,                 // 使用的颜色指数
        CurrentStandard,            // 当前标样
        Tolerance,                  // 当前容差
        RefStandard,                // 雾度测量参考标准 
    }

    public static class Commands
    {
        /// <summary>
        /// 判断命令是否有应答数据
        /// </summary>
        /// <param name="command">要判断的命令</param>
        /// <returns>如果有返回true</returns>
        public static bool HasAckData(Command command)
        {
            switch (command)
            {
                case Command.GetDeviceInfo:
                case Command.GetInstrumentStatus:
                case Command.MeasureStandard:
                case Command.MeasureTrial:
//                case Command.GetWavelengthCoef:
                case Command.GetWhiteBoardParameter:
                case Command.RtcGetDatetime:
                case Command.GetProductEquipInfo:
                case Command.AutoGetIntegralTime:
//                case Command.Calibrate:
                case Command.GetStandardCount:
                case Command.GetTrialCount:
                case Command.UploadStandards:
                case Command.UploadTrials:
                case Command.GetInternalModel:
                case Command.GetCpuId:
                case Command.GetMeasurementParameter:
                case Command.DcMotorTimeGet:
                case Command.GetCameraImage:
                case Command.GetAdcData:
                case Command.GetSystemCfg:
                    return true;

                default: return false;
            }
        }

        /// <summary>
        /// 打包测量命令
        /// </summary>
        /// <param name="type">测量类型</param>
        /// <returns>命令包</returns>
        /// <remarks>测量不分标样试样，两个命令的执行结果是一样的</remarks>
        public static CommandPacket Measure(MeasurementType type = MeasurementType.HAZE)
        {
            Command cmd =  Command.MeasureStandard;
            return new CommandPacket(cmd, new byte[] {(byte)type});
        }

        public static CommandPacket GetProductInfo(ProductInfoField field, byte[] args=null)
        {
            if (args == null) {
                return new CommandPacket(Command.GetProductEquipInfo, new []{(byte)field});
            }
            List<byte> bytes = new List<byte>();
            bytes.Add((byte)field);
            bytes.AddRange(args);
            return new CommandPacket(Command.GetProductEquipInfo, bytes.ToArray());
        }

        public static CommandPacket SetProductInfo(ProductInfoField field, byte[] value)
        {
            List<byte> bytes = new List<byte>();
            bytes.Add((byte)field);
            bytes.AddRange(value);
            return new CommandPacket(Command.SetProductEquipInfo, bytes.ToArray());
        }

        public static CommandPacket DcMotorTimeGet()
        {
            return new CommandPacket(Command.DcMotorTimeGet);
        }

        public static CommandPacket DcMotorTimeSet(UInt16 time)
        {
            return new CommandPacket(Command.DcMotorTimeSet, BitConverter.GetBytes(time));
        }

        public static CommandPacket DcMotorReset()
        {
            return new CommandPacket(Command.DcMotorReset);
        }

        
        /// <summary>
        /// DC马达测试
        /// </summary>
        /// <param name="direction">方向</param>
        /// <param name="time">脉冲时间</param>
        public static CommandPacket DCMotorTest(bool direction, ushort time)
        {
            Packer packer = new Packer();
            packer.Enqueue((byte)(direction ? 1 : 0));
            packer.Enqueue(time);
            return new CommandPacket(Command.DcMotorTest, packer.Bytes);
        }

        /// <summary>
        /// 下载标样到仪器
        /// </summary>
        /// <param name="records"></param>
        /// <returns></returns>
        public static CommandPacket DownloadStandards(StandardRecord[] records)
        {
            Packer packer = new Packer();
            foreach (var record in records)
            {
                packer.Enqueue(StructConverter.StructToBytes(record));
            }
            return new CommandPacket(Command.DownloadStandards, packer.Bytes);
        }


        /// <summary>
        /// 检测蜂鸣器
        /// </summary>
        public static CommandPacket BuzzerTest()
        {
            return new CommandPacket(Command.BuzzerTest);
        }

        /// <summary>
        /// 获取RTC时间
        /// </summary>
        /// <returns>返回获取的时间</returns>
        /// <exception cref="CommunicationException"></exception>
        public static CommandPacket GetRtcDateTime()
        {
            return new CommandPacket(Command.RtcGetDatetime);
        }

        /// <summary>
        /// 设置RTC时间
        /// </summary>
        /// <param name="dateTime">要设置的日期</param>
        public static CommandPacket SetRtcDateTime(DateTime dateTime)
        {
            return new CommandPacket(Command.RtcSetDatetime, StructConverter.StructToBytes(new RtcDateTime
            {
                year = (ushort)dateTime.Year,
                date = (byte)dateTime.Day,
                month = (byte)dateTime.Month,
                hour = (byte)dateTime.Hour,
                minute = (byte)dateTime.Minute,
                second = (byte)dateTime.Second,
            }));
        }

        /// <summary>
        /// 测试打印机
        /// </summary>
        public static CommandPacket TestPrinter()
        {
            return new CommandPacket(Command.PrinterTest);
        }

        public static CommandPacket GetDeviceInfo()
        {
            return new CommandPacket(Command.GetDeviceInfo);
        }

        public static CommandPacket GetInstrumentStatus()
        {
            return new CommandPacket(Command.GetInstrumentStatus);
        }

        public static CommandPacket Calibrate(CalibrationType calibrationType)
        {
            return new CommandPacket((byte) Command.Calibrate, new byte[] {(byte)calibrationType});
        }

        public static CommandPacket GetStandardCount()
        {
            return new CommandPacket((byte)Command.GetStandardCount);
        }

        /// <summary>
        /// 获取试样ID
        /// </summary>
        /// <param name="standardId">试样关联的标样ID，如果为-1获取所有试样数</param>
        /// <returns></returns>
        public static CommandPacket GetTrialCount(int standardId)
        {
            return new CommandPacket((byte)Command.GetTrialCount, BitConverter.GetBytes(standardId));
        }

        /// <summary>
        /// 上传标样
        /// </summary>
        /// <param name="from">起始序号</param>
        /// <param name="count">上传标样数，如果为-1上传from起的所有标样</param>
        /// <returns></returns>
        public static CommandPacket UploadStandards(int from = 0, int count = -1)
        {
            Packer packer = new Packer();
            packer.Enqueue(from);
            packer.Enqueue(count);
            return new CommandPacket((byte)Command.UploadStandards, packer.Bytes);
        }

        /// <summary>
        /// 上传试样
        /// </summary>
        /// <param name="standardId">关联的标样ID</param>
        /// <param name="from">起始序号</param>
        /// <param name="count">上传的试样数，如果为-1上传从from起的所有试样</param>
        /// <returns></returns>
        public static CommandPacket UploadTrials(int standardId, int from = 0, int count = -1)
        {
            Packer packer = new Packer();
            packer.Enqueue(standardId);
            packer.Enqueue(from);
            packer.Enqueue(count);
            return new CommandPacket((byte)Command.UploadTrials, packer.Bytes);
        }

        /// <summary>
        /// 获取内部型号
        /// </summary>
        /// <returns></returns>
        public static CommandPacket GetInternalModel()
        {
            return new CommandPacket((byte)Command.GetInternalModel);
        }

        /// <summary>
        /// 设置内部型号
        /// </summary>
        /// <param name="modelType">仪器内部型号</param>
        /// <returns></returns>
        public static CommandPacket SetInternalModel(byte modelType)
        {
            return new CommandPacket((byte)Command.SetInternalModel, new []{modelType});
        }

        /// <summary>
        /// 获取仪器的CPU ID
        /// </summary>
        /// <returns></returns>
        public static CommandPacket GetCpuId()
        {
            return new CommandPacket((byte)Command.GetCpuId);
        }

        /// <summary>
        /// 设置数据加密密钥
        /// </summary>
        /// <param name="encryption">加密密钥</param>
        /// <returns></returns>
        public static CommandPacket SetEncryption(ushort encryption)
        {
            return new CommandPacket((byte) Command.SetEncryption, BitConverter.GetBytes(encryption));
        }

        // public static CommandPacket GetWavelengthCoef()
        // {
        //     return new CommandPacket((byte)Command.GetWavelengthCoef);
        // }
        //
        // public static CommandPacket SetWavelengthCoef(PTS70SpectrometerParameters coefficients)
        // {
        //     return new CommandPacket((byte)Command.SetWavelengthCoef, 
        //         StructConverter.StructToBytes(coefficients.ToSpectrometerParameters()));
        // }

        /// <summary>
        /// 删除指定序号的标样
        /// </summary>
        /// <param name="standardIndex">要删除的标样的序号；如果为-1删除所有样品</param>
        /// <returns>删除标样的命令包</returns>
        public static CommandPacket DeleteStandard(int standardIndex)
        {
            return new CommandPacket((byte)Command.DeleteStandard, BitConverter.GetBytes(standardIndex));
        }

        /// <summary>
        /// 删除指定试样
        /// </summary>
        /// <param name="standardId">试样关联的标样的ID；如果为-1删除所有试样</param>
        /// <param name="sampleIndex">试样的序号；如果为-1且标样ID不为-1删除相应标样关联的所有试样</param>
        /// <returns></returns>
        public static CommandPacket DeleteSample(int standardId, int sampleIndex)
        {
            Packer packer = new Packer();
            packer.Enqueue(standardId);
            packer.Enqueue(sampleIndex);
            return new CommandPacket((byte)Command.DeleteSample, packer.Bytes);
        }

        /// <summary>
        /// 获取测量参数
        /// </summary>
        /// <param name="parameter">要获取参数类型</param>
        /// <returns></returns>
        public static CommandPacket GetMeasurementParameter(MeasurementParameter parameter)
        {
            return new CommandPacket((byte)Command.GetMeasurementParameter, 
                new [] {(byte)parameter});
        }

        /// <summary>
        /// 设置测量参数
        /// </summary>
        /// <param name="parameter">设置的测量参数类型</param>
        /// <param name="value">参数的值</param>
        /// <returns></returns>
        public static CommandPacket SetMeasurementParameter(MeasurementParameter parameter, IEnumerable<byte> value)
        {
            Packer packer = new Packer();
            packer.Enqueue((byte)parameter);
            packer.Enqueue(value.ToArray());
            return new CommandPacket((byte)Command.SetMeasurementParameter,
                packer.Bytes);
        }

        /// <summary>
        /// 设置测量参数
        /// </summary>
        /// <param name="parameter">设置的测量参数类型</param>
        /// <param name="value">参数的值</param>
        /// <returns></returns>
        public static CommandPacket SetMeasurementParameter(MeasurementParameter parameter, byte value)
        {
            return new CommandPacket((byte)Command.SetMeasurementParameter, new [] {(byte)parameter, value});
        }

        /// <summary>
        /// 获取8个通道的测量值
        /// </summary>
        /// <returns></returns>
        public static CommandPacket ReadSpectrumEight(byte[] contentBytes)
        {
            return new CommandPacket((byte)Command.GetProductEquipInfo, contentBytes);
        }

        public static CommandPacket WriteSVD(byte[] contentBytes)
        {
            return new CommandPacket((byte)Command.SetProductEquipInfo, contentBytes);
        }

        public static CommandPacket ReadSVD(byte[] contentBytes)
        {
            return new CommandPacket((byte)Command.GetProductEquipInfo, contentBytes);
        }

        /// <summary>
        /// 设置蓝牙名
        /// </summary>
        /// <param name="name">蓝牙名称，转换成UTF8编码后长度不能超过Constants.BLE_NAME_LENGTH</param>
        /// <returns></returns>
        public static CommandPacket BleSetName(string name)
        {
            var bytes = Encoding.UTF8.GetBytes(name);
            if (bytes.Length > Constants.BLE_NAME_LENGTH)
            {
                throw new CommunicationException(CommunicationError.InvalidParameter,
                    $"The length of bluetooth name can't greater than {Constants.BLE_NAME_LENGTH}'");
            }
            return new CommandPacket((byte)Command.BleSetName, bytes);
        }

        /// <summary>
        /// 开关蓝牙命令
        /// </summary>
        /// <param name="on">true开启，否则关闭</param>
        /// <returns></returns>
        public static CommandPacket BleSwitch(bool on)
        {
            return new CommandPacket((byte) Command.BleSwitch, new byte[] {on ? (byte) 1 : (byte) 0});
        }

        public static CommandPacket LedTest(int lamps, bool on)
        {
            return new CommandPacket((byte) Command.LedTest, new []{on ? (byte) 1 : (byte)0});
        }
    }
}
