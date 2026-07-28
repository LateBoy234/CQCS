using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using ThreeNH.CommunicationBase;
using ThreeNH.Optical;
using ThreeNH.StructHelper;

namespace BTS
{
    public enum MeasurementType
    {
        Standard,   // 测量标样
        Sample,     // 测量试样
    }

    public enum InternalModelType
    {
        YS6010,
        YS6020,
        YS6060,
        YS6080,
        YS6002,
        YS6003,
        YS6002M,
        TS8296,
        TS8216
    }

    public enum SpecialTransmissionCaliber
    {
        Caliber_2mm,
        Caliber_25_4mm
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
                case Command.GetWavelengthCoef:
                case Command.GetWhiteBoardParameter:
                case Command.RtcGetDatetime:
                case Command.GetProductEquipInfo:
                case Command.AutoGetIntegralTime:
                case Command.Calibrate:
                case Command.GetStandardCount:
                case Command.GetTrialCount:
                case Command.UploadStandards:
                case Command.UploadTrials:
                case Command.GetInternalModel:
                case Command.GetCpuId:
                case Command.GetMeasurementParameter:
                case Command.GET_DEFAULT_CALIBER:
                case Command.GET_CORRECTION:
                case Command.GET_CORRECTION_ENABLE:
                    return true;

                default: return false;
            }
        }

        public static CommandPacket Measure(MeasurementType type = MeasurementType.Standard, ScMode? sc = null)
        {
            Command cmd = type == MeasurementType.Standard ? Command.MeasureStandard : Command.MeasureTrial;
            return sc == null ? new CommandPacket(cmd) : new CommandPacket(cmd, new byte[] { (byte)sc });
        }

        public static CommandPacket GetProductQeuipInfo()
        {
            return new CommandPacket(Command.GetProductEquipInfo);
        }

        public static CommandPacket SetProductQeuipInfo(ProductQeuipInfo info)
        {
            return new CommandPacket(Command.SetProductEquipInfo, StructConverter.StructToBytes(info));
        }

        public static CommandPacket SetProductQeuipInfo(ProductQeuipInfoV2 info)
        {
            return new CommandPacket(Command.SetProductEquipInfo, StructConverter.StructToBytes(info));
        }

        public static CommandPacket StepperMotorRunToStart()
        {
            return new CommandPacket(Command.MotorRunToStart);
        }

        public static CommandPacket StepperMotorRunToEnd()
        {
            return new CommandPacket(Command.MotorRunToEnd);
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

        public static CommandPacket StepperMotorTestStep(uint step, uint times)
        {
            Packer packer = new Packer();
            packer.Enqueue(step);
            packer.Enqueue(times);
            return new CommandPacket(Command.MotorStepTest, packer.Bytes);
        }

        /// <summary>
        /// DC马达测试
        /// </summary>
        /// <param name="scMode">测光方式</param>
        /// <param name="plus_time">脉冲时间</param>
        /// <param name="times">往返次数</param>
        public static CommandPacket DCMotorTest(ScMode scMode, uint plus_time, uint times)
        {
            Packer packer = new Packer();
            packer.Enqueue((byte)scMode);
            packer.Enqueue(plus_time);
            packer.Enqueue(times);
            return new CommandPacket(Command.DcMotorTest, packer.Bytes);
        }

        /// <summary>
        /// 测试LED
        /// </summary>
        /// <param name="ledIndex">要测试LED的索引，如果为0xFF表示所有灯</param>
        /// <param name="on">如果为真点亮灯，否则关掉灯</param>
        public static CommandPacket LedTest(byte ledIndex, bool on)
        {
            if (ledIndex != 0xFF && ledIndex >= (int)LedIndex.Count)
            {
                throw new CommunicationException(CommunicationError.InvalidParameter);
            }
            return new CommandPacket(Command.LedTest, new byte[] { ledIndex, (byte)(on ? 1 : 0) });
        }

        /// <summary>
        /// 相机测试
        /// </summary>
        /// <param name="on">如果为true打开相机，否则关掉相机</param>
        public static CommandPacket CameraTest(bool on)
        {
            return new CommandPacket(Command.CameraTest, new byte[] { (byte)(on ? 1 : 0) });
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

        /// <summary>
        /// 自动获取积分时间
        /// </summary>
        /// <param name="isTransmission">如果为真获取透射模式的积分时间，否则获取反射</param>
        /// <param name="maxAdcValue">ADC最大采样值</param>
        /// <returns>相应的积分时间</returns>
        public static CommandPacket AutoGetIntegralTime(bool isTransmission, ushort maxAdcValue)
        {
            Packer packer = new Packer();
            packer.Enqueue((byte)(isTransmission ? 1 : 0));
            packer.Enqueue(maxAdcValue);

            return new CommandPacket(Command.AutoGetIntegralTime, packer.Bytes);
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

        public static CommandPacket Calibrate(bool isBlack)
        {
            return new CommandPacket((byte)Command.Calibrate, new byte[] { (byte)(isBlack ? 0 : 1) });
        }

        public static CommandPacket GetStandardCount()
        {
            return new CommandPacket((byte)Command.GetStandardCount);
        }

        /// <summary>
        /// 仪器校正
        /// </summary>
        public static CommandPacket ColoredBrick1Calib()
        {
            return new CommandPacket(Command.ColoredBrick1Calib);
        }
        public static CommandPacket ColoredBrick1Clear()
        {
            return new CommandPacket(Command.ColoredBrick1Clear);
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
        public static CommandPacket GenerateAdLimit(bool isTransmission, bool isWhite)
        {
            Packer packer = new Packer();
            packer.Enqueue((byte)(isTransmission ? 1 : 0));
            packer.Enqueue((byte)(isWhite ? 1 : 0));
            return new CommandPacket((byte)Command.GenerateAdLimit, packer.Bytes);
        }

        /// <summary>
        /// 存储生成的AD限制
        /// </summary>
        /// <returns></returns>
        public static CommandPacket SaveAdLimits()
        {
            return new CommandPacket((byte)Command.SaveAdLimits);
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
        public static CommandPacket SetInternalModel(InternalModelType modelType)
        {
            return new CommandPacket((byte)Command.SetInternalModel, new byte[] { (byte)modelType });
        }



        public static CommandPacket SetSpecialTransmissionCaliber(SpecialTransmissionCaliber caliber)
        {
            return new CommandPacket((byte)Command.SetSpecialTransmissionCaliber, new byte[] { (byte)caliber });
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

        public static CommandPacket StepMotorCalibration(ushort value)
        {
            return new CommandPacket((byte)Command.StepMotorCalibration, BitConverter.GetBytes(value));
        }

        public static CommandPacket GetWhiteBoardParameter()
        {
            return new CommandPacket((byte)Command.GetWhiteBoardParameter);
        }

        public static CommandPacket SetWhiteBoardParameter(WhiteBoardParameter parameter)
        {
            return new CommandPacket((byte)Command.SetWhiteBoardParameter, StructConverter.StructToBytes(parameter));
        }

        public static CommandPacket GetWavelengthCoef()
        {
            return new CommandPacket((byte)Command.GetWavelengthCoef);
        }

        public static CommandPacket SetWavelengthCoef(GratingWavelengthCoefficients coefficients)
        {
            return new CommandPacket((byte)Command.SetWavelengthCoef,
                StructConverter.StructToBytes(coefficients.ToGratingWavelengthCoef()));
        }

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

        /// <summary>
        /// 设置默认口径
        /// </summary>
        /// <param name="isTransCaliber">是否是设置透射口径</param>
        /// <param name="caliber">口径代号</param>
        /// <returns></returns>
        public static CommandPacket SetDefaultCaliber(bool isTransCaliber, byte caliber)
        {
            return new CommandPacket((byte)Command.SET_DEFAULT_CALIBER, new[] { isTransCaliber ? (byte)1 : (byte)0, caliber });
        }

        public static CommandPacket GetDefaultCaliber(bool isTransCaliber)
        {
            return new CommandPacket((byte)Command.GET_DEFAULT_CALIBER, new[] { isTransCaliber ? (byte)1 : (byte)0 });
        }

        /// <summary>
        /// 获取修正系数设置
        /// 接收会返回 CorrectionFactors_t
        /// </summary>
        /// <param name="sc">SCI或SCE</param>
        /// <param name="caliber">有效的反射口径</param>
        /// <returns></returns>
        public static CommandPacket GetCorrection(ScMode sc, Caliber caliber)
        {
            if (sc != ScMode.SCI && sc != ScMode.SCE)
            {
                throw new ArgumentException();
            }
            return new CommandPacket((byte)Command.GET_CORRECTION, new[] { (byte)sc, (byte)caliber });
        }

        /// <summary>
        /// 设置修正系数
        /// </summary>
        /// <param name="sc">SCI或SCE</param>
        /// <param name="caliber">有效的反射口径</param>
        /// <param name="factors">要设置的修正因子</param>
        /// <returns></returns>
        public static CommandPacket SetCorrection(ScMode sc, Caliber caliber, CorrectionFactors_t factors)
        {
            if (sc != ScMode.SCI && sc != ScMode.SCE)
            {
                throw new ArgumentException();
            }
            if (factors == null)
            {
                throw new ArgumentNullException();
            }
            Packer packer = new Packer();
            packer.Enqueue((byte)sc);
            packer.Enqueue((byte)caliber);
            packer.Enqueue(StructConverter.StructToBytes(factors));
            return new CommandPacket((byte)Command.SET_CORRECTION, packer.Bytes);
        }

        /// <summary>
        /// 获取当前修正开关是否开启的命令
        /// 
        /// 接收会返回一个 byte，是否为0指示是否开启
        /// </summary>
        /// <returns></returns>
        public static CommandPacket GetCorrectionEnable()
        {
            return new CommandPacket((byte)Command.GET_CORRECTION_ENABLE);
        }

        /// <summary>
        /// 设置修正开关是否开启
        /// </summary>
        /// <param name="enable"></param>
        /// <returns></returns>
        public static CommandPacket SetCorrectionEnable(bool enable)
        {
            return new CommandPacket((byte)Command.SET_CORRECTION_ENABLE, new byte[] { (byte)(enable ? 1 : 0) });
        }

        internal static CommandPacket SwitchOpticalStructure(OpticalStructure optical)
        {
            return new CommandPacket((byte)Command.SWITCH_OPTICAL_STRUCTURE, BitConverter.GetBytes((ushort)optical));
        }
    }
}
