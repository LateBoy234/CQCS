﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using ThreeNH.ColorimeterInterface;
using ThreeNH.CommunicationBase;
using ThreeNH.StructHelper;

namespace ThreeNH.SQCX.Infrastructure.Instruments.ColorReader
{
    public enum MeasurementType
    {
        Standard,   // 测量标样
        Sample,     // 测量试样
    }

    public enum InternalModelType
    {
        CR1 = 0,
        CR2 = 1,
        CR3 = 2,

        [Description("CR P")]
        CRP = 3,

        [Description("CR S")]
        CRS = 4,

        [Description("MB 10")]
        MB10 = 5,

        [Description("MB 20")]
        MB20 = 6,

        [Description("MB 30")]
        MB30 = 7,

        [Description("CR SE")]
        CRSE = 8,

        [Description("CR PRO")]
        CRPRO = 9,

        [Description("CR Max(old)")]
        CRMAXOLD = 10,

        [Description("CR 100")]
        CR100 = 11,

        [Description("CR 200")]
        CR200 = 12,

        [Description("CR 300")]
        CR300 = 13,

        [Description("CR Max")]
        CRMAX = 14,

        [Description("CR HI")]
        CRHI = 15,

        [Description("CR Top")]
        CRTOP = 16,

        [Description("PSY 01")]
        PSY01 = 17,

        [Description("PSY 02")]
        PSY02 = 18,

        [Description("PSY 03")]
        PSY03 = 19,

        [Description("CR QS")]
        CRQS = 20,

        [Description("CR 45°0 Basic/CR 4501")]
        CR4501 = 21,

        [Description("CR 45°0 Advanced/CR 4505 ")]
        CR4505 = 22,

        [Description("CR 45°0 QS ")]
        CR450QS = 23,

        [Description("PS100")]
        PS100 = 24,

        [Description("PS200")]
        PS200 = 25,

        [Description("PS300")]
        PS300 = 26,

        [Description("PS02")]
        PS02 = 27,

        [Description("PS01")]
        PS01 = 28,

        [Description("PS4620")]
        PS4620 = 29,
    }

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
                case Command.GetFactors:
                case Command.GetWhiteBoardFactor:
                case Command.GetKmtcMode:
                case Command.GetCIE1976Tolerance:
                case Command.GetStandardNames:
                case Command.ReadPharmaceuticalConfig:
                case Command.ReadPharmaceuticalInfo:
                    return true;
                default: return false;
            }
        }

        public static CommandPacket Measure(MeasurementType type = MeasurementType.Standard, ScMode? sc = null)
        {
            Command cmd = type == MeasurementType.Standard ? Command.MeasureStandard : Command.MeasureTrial;
            return sc == null ? new CommandPacket(cmd) : new CommandPacket(cmd, new byte[] { (byte)sc });
        }

        public static CommandPacket GetProductInfo(ProductInfoField field)
        {
            return new CommandPacket(Command.GetProductEquipInfo, new[] { (byte)field });
        }

        public static CommandPacket SetProductEquipInfo(ProductInfo info)
        {
            List<byte> bytes = new List<byte>(1 + Marshal.SizeOf(typeof(ProductInfo)));
            bytes.Add((byte)ProductInfoField.ALL);
            if (info.Flags == 0)
            {
                bytes.AddRange(StructConverter.StructToBytes(info.ToProductInfoV0()));
            }
            else
            {
                bytes.AddRange(StructConverter.StructToBytes(info));
            }

            return new CommandPacket(Command.SetProductEquipInfo, bytes.ToArray());
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
        /// <param name="scMode">测光方式</param>
        /// <param name="time">脉冲时间</param>
        public static CommandPacket DCMotorTest(ScMode scMode, ushort time)
        {
            Packer packer = new Packer();
            packer.Enqueue((byte)scMode);
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
        /// 测试LED
        /// </summary>
        /// <param name="ledIndex">要测试LED的索引</param>
        /// <param name="on">如果为真点亮灯，否则关掉灯</param>
        public static CommandPacket LedTest(LedIndex ledIndex, bool on)
        {
            return new CommandPacket(Command.LedTest, new byte[] { (byte)ledIndex, (byte)(on ? 1 : 0) });
        }

        /// <summary>
        /// 相机测试
        /// </summary>
        /// <param name="on">如果为true打开相机，否则关掉相机</param>
        public static CommandPacket CameraTest(CameraTestCode code)
        {
            return new CommandPacket(Command.CameraTest, new byte[] { (byte)code });
        }

        /// <summary>
        /// 检测Flash
        /// </summary>
        public static CommandPacket FlashTest()
        {
            return new CommandPacket(Command.FlashTest);
        }

        /// <summary>
        /// 检测外部RAM
        /// </summary>
        public static CommandPacket SdramTest()
        {
            return new CommandPacket(Command.SdramTest);
        }

        /// <summary>
        /// 检测蜂鸣器
        /// </summary>
        public static CommandPacket BuzzerTest()
        {
            return new CommandPacket(Command.BuzzerTest);
        }

        /// <summary>
        /// 测试显示屏
        /// </summary>
        public static CommandPacket LcdTest()
        {
            return new CommandPacket(Command.LcdTest);
        }

        /// <summary>
        /// 测试触屏，显示指定数格子
        /// </summary>
        /// <param name="rowCount">格子行数</param>
        /// <param name="colCount">格子列数</param>
        public static CommandPacket TouchScreenTest(ushort rowCount, ushort colCount)
        {
            Packer packer = new Packer();
            packer.Enqueue(rowCount);
            packer.Enqueue(colCount);
            return new CommandPacket(Command.TouchScreenTest, packer.Bytes);
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

        public static CommandPacket Restore()
        {
            return new CommandPacket(Command.Restore);
        }

        /// <summary>
        /// 自动获取积分时间
        /// </summary>
        /// <param name="isTransmission">如果为真获取透射模式的积分时间，否则获取反射</param>
        /// <param name="maxAdcValue">ADC最大采样值</param>
        /// <returns>相应的积分时间</returns>
        //public static CommandPacket AutoGetIntegralTime(bool isTransmission, ushort maxAdcValue)
        //{
        //    Packer packer = new Packer();
        //    packer.Enqueue((byte)(isTransmission ? 1 : 0));
        //    packer.Enqueue(maxAdcValue);

        //    return new CommandPacket(Command.AutoGetIntegralTime, packer.Bytes);
        //}

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

        public static CommandPacket Calibrate(bool isBlack)
        {
            return new CommandPacket((byte)Command.Calibrate, new byte[] { (byte)(isBlack ? 0 : 1) });
        }

        public static CommandPacket GetStandardCount()
        {
            return new CommandPacket((byte)Command.GetStandardCount);
        }

        public static CommandPacket GetWhiteBoardFactors()
        {
            return new CommandPacket((byte)Command.GetWhiteBoardFactor);
        }

        public static CommandPacket GetFactors()
        {
            return new CommandPacket((byte)Command.GetFactors);
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
        /// 生成校准的AD限制
        /// </summary>
        /// <param name="isTransmission">是否是透射模式</param>
        /// <param name="isWhite">是否是白校准</param>
        /// <returns></returns>
        public static CommandPacket GenerateAdLimit(Caliber caliber)
        {
            return new CommandPacket((byte)Command.GenerateAdLimit, new byte[] { (byte)caliber });
        }

        public static CommandPacket GetAdData()
        {
            return new CommandPacket((byte)Command.GetAdcData);
        }

        ///// <summary>
        ///// 存储生成的AD限制
        ///// </summary>
        ///// <returns></returns>
        //public static CommandPacket SaveAdLimits()
        //{
        //    return new CommandPacket((byte)Command.SaveAdLimits);
        //}

        /// <summary>
        /// 获取内部型号
        /// </summary>
        /// <returns></returns>
        public static CommandPacket GetInternalModel()
        {
            return new CommandPacket((byte)Command.GetInternalModel);
        }

        public static CommandPacket GetStandardNames()
        {
            return new CommandPacket((byte)Command.GetStandardNames);
        }

        public static CommandPacket GetCIE1976Tolerance()
        {
            return new CommandPacket((byte)Command.GetCIE1976Tolerance);
        }

        public static CommandPacket GetKmtcMode()
        {
            return new CommandPacket((byte)Command.GetKmtcMode);
        }

        public static CommandPacket ProductBlackCalibration()
        {
            return new CommandPacket((byte)Command.ProductBlackCalibration);
        }

        public static CommandPacket ProductWhiteCalibration()
        {
            return new CommandPacket((byte)Command.ProductWhiteCalibration);
        }

        public static CommandPacket SetStandardNames(StandardNames standardNames)
        {
            return new CommandPacket((byte)Command.SetStandardNames, StructConverter.StructToBytes<StandardNames>(standardNames));
        }

        /// <summary>
        /// 设置内部型号
        /// </summary>
        /// <param name="modelType">仪器内部型号</param>
        /// <returns></returns>
        public static CommandPacket SetInternalModel(byte modelType)
        {
            return new CommandPacket((byte)Command.SetInternalModel, new[] { modelType });
            //20191101 lilin change byte to 16位整形；因为ColorReader下位机为unsigned short; 之前为 new byte[] {(byte)modelType}
        }

        public static CommandPacket SetWhiteBoardFactors(Factors factors)
        {
            return new CommandPacket((byte)Command.SetWhiteBoardFactor, StructConverter.StructToBytes(factors));
            //20191101 lilin change byte to 16位整形；因为ColorReader下位机为unsigned short; 之前为 new byte[] {(byte)modelType}
        }

        public static CommandPacket SetFactors(float factor)
        {
            return new CommandPacket((byte)Command.SetFactors, BitConverter.GetBytes(factor));
            //20191101 lilin change byte to 16位整形；因为ColorReader下位机为unsigned short; 之前为 new byte[] {(byte)modelType}
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
            return new CommandPacket((byte)Command.SetEncryption, BitConverter.GetBytes(encryption));
        }

        public static CommandPacket SetKmtcMode(byte kmtcMode)
        {
            return new CommandPacket((byte)Command.SetKmtcMode, new byte[] { kmtcMode });
        }

        public static CommandPacket GetWhiteBoardParameter()
        {
            return new CommandPacket((byte)Command.GetWhiteBoardParameter);
        }

        public static CommandPacket SetWhiteBoardParameter(CalibrationBoard parameter)
        {
            return new CommandPacket((byte)Command.SetWhiteBoardParameter, StructConverter.StructToBytes(parameter));
        }

        //        public static CommandPacket GetWavelengthCoef()
        //        {
        //            return new CommandPacket((byte)Command.GetWavelengthCoef);
        //        }
        //
        //        public static CommandPacket SetWavelengthCoef(ColorReader70SpectrometerParameters coefficients)
        //        {
        //            return new CommandPacket((byte)Command.SetWavelengthCoef,
        //                StructConverter.StructToBytes(coefficients.ToSpectrometerParameters()));
        //        }

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
                new[] { (byte)parameter });
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
            return new CommandPacket((byte)Command.SetMeasurementParameter, new[] { (byte)parameter, value });
        }

        public static CommandPacket FaultCheck(FaultMode faultMode)
        {
            return new CommandPacket((byte)Command.FaultCheck, new[] { (byte)faultMode });
        }

        public static CommandPacket GetCameraImage()
        {
            return new CommandPacket((byte)Command.GetCameraImage);
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
            return new CommandPacket((byte)Command.BleSwitch, new byte[] { on ? (byte)1 : (byte)0 });
        }

        public static CommandPacket BleSendData(byte[] bytes)
        {
            return new CommandPacket((byte)Command.BleSendData, bytes);
        }

        public static CommandPacket GetSystemCfg(SystemCfgField field)
        {
            return new CommandPacket((byte)Command.GetSystemCfg, new byte[] { (byte)field });
        }

        internal static CommandPacket SetCIE1976Tolerance(ToleranceCIE1976 tolerance)
        {
            return new CommandPacket((byte)Command.SetCIE1976Tolerance, StructConverter.StructToBytes<ToleranceCIE1976>(tolerance));
        }

        internal static CommandPacket WritePharmaceuticalConfig(List<PharmaceuticalConfig> configs)
        {
            var datas = new List<byte>();
            foreach (var config in configs)
            {
                datas.AddRange(StructConverter.StructToBytes<PharmaceuticalConfig>(config));
            }
            return new CommandPacket((byte)Command.WritePharmaceuticalConfig, datas.ToArray());
        }

        public static CommandPacket ReadPharmaceuticalConfig()
        {
            return new CommandPacket((byte)Command.ReadPharmaceuticalConfig);
        }

        internal static CommandPacket WritePharmaceuticalInfo(PharmaceuticalInfo config)
        {
            return new CommandPacket((byte)Command.WritePharmaceuticalInfo, StructConverter.StructToBytes<PharmaceuticalInfo>(config));
        }

        public static CommandPacket ReadPharmaceuticalInfo()
        {
            return new CommandPacket((byte)Command.ReadPharmaceuticalInfo);
        }

        public static CommandPacket ReadSpectrumEight()
        {
            return new CommandPacket((byte)Command.ReadPharmaceuticalConfig);
        }

        public static CommandPacket ReadSVD(byte packageNum)
        {
            return new CommandPacket((byte)Command.ReaSVDParameters, new byte[] { packageNum });
        }

        public static CommandPacket WriteSVD(byte[] contentBytes)
        {
            return new CommandPacket((byte)Command.GetKmtcMode, contentBytes);
        }
    }
}