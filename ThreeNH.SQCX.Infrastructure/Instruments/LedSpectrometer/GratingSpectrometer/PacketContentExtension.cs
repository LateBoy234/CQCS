using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using ThreeNH.ColorAlgorithm;
using ThreeNH.CommunicationBase;
using ThreeNH.Optical;
using ThreeNH.StructHelper;

namespace ThreeNH.GratingSpectrometer
{
    static class PacketContentExtension
    {
        private const int NameLength = 9;

        internal static Tolerance DequeueTolerance1(this PacketContent packetContent)
        {
            Tolerance tolerance = new Tolerance();

            float tmp;
            packetContent.Dequeue(out tmp);
            tolerance.dE = tmp;

            packetContent.Dequeue(out tmp);
            tolerance.dL_Upper = tmp;

            packetContent.Dequeue(out tmp);
            tolerance.da_Upper = tmp;

            packetContent.Dequeue(out tmp);
            tolerance.db_Upper = tmp;

            packetContent.Dequeue(out tmp);
            tolerance.dL_Lower = tmp;

            packetContent.Dequeue(out tmp);
            tolerance.da_Lower = tmp;

            packetContent.Dequeue(out tmp);
            tolerance.db_Lower = tmp;

            return tolerance;
        }
        internal static Tolerance DequeueTolerance2(this PacketContent packetContent)
        {
            var tolerance = new Tolerance();

            float floatValue;
            tolerance = new Tolerance();

            packetContent.Dequeue(out floatValue);
            tolerance.dE = floatValue;
            packetContent.Dequeue(out floatValue);
            tolerance.dL_Lower = floatValue;
            packetContent.Dequeue(out floatValue);
            tolerance.dL_Upper = floatValue;
            packetContent.Dequeue(out floatValue);
            tolerance.da_Lower = floatValue;
            packetContent.Dequeue(out floatValue);
            tolerance.da_Upper = floatValue;
            packetContent.Dequeue(out floatValue);
            tolerance.db_Lower = floatValue;
            packetContent.Dequeue(out floatValue);
            tolerance.db_Upper = floatValue;

            return tolerance;
        }


        internal static void Dequeue(this PacketContent packetContent, out DeviceInfo info)
        {
            info = new DeviceInfo();

            string strValue;
            byte byteValue;
            ushort shortValue;

            packetContent.Dequeue(out strValue);
            info.InstrumentMode = strValue;
            packetContent.Dequeue(out strValue);
            info.OpticalStruct = strValue;
            packetContent.Dequeue(out strValue);
            info.HardwareVersion = strValue;
            packetContent.Dequeue(out strValue);
            info.SoftwareVersion = strValue;
            packetContent.Dequeue(out strValue);
            info.WhiteBoardInnerNumber = strValue;
            packetContent.Dequeue(out strValue);
            info.WhiteBoardOuterNumber = strValue;
            packetContent.Dequeue(out shortValue);
            info.Capacity = shortValue;
            packetContent.Dequeue(out shortValue);
            info.StandardCount = shortValue;
            packetContent.Dequeue(out shortValue);
            info.TestCount = shortValue;
            packetContent.Dequeue(out byteValue);
            info.BlackCalibration = byteValue != 0;
            packetContent.Dequeue(out byteValue);
            info.WhiteCalibration = byteValue != 0;
            packetContent.Dequeue(out strValue);
            info.SN = strValue;
            packetContent.Dequeue(out strValue);
            info.ModelName = strValue;

            info.Tolerance = packetContent.DequeueTolerance1();
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="packetContent">需要解析的数据包</param>
        /// <param name="isKSFlag">KS系列需要对口径特殊处理</param>
        /// <param name="para">返回的数据内容</param>
        internal static void Dequeue(this PacketContent packetContent,bool isKSFlag,out  MeasurementParameter para)
        {
            para = new MeasurementParameter();

            byte tmp;

            packetContent.Dequeue(out tmp);
            para.PrimaryIlluminant = ((Instrument.StandardIlluminant)tmp).Uniform();
            packetContent.Dequeue(out tmp);
            para.SecondaryIlluminant = ((Instrument.StandardIlluminant)tmp).Uniform();
            packetContent.Dequeue(out tmp);
            para.TertiaryIlluminant = ((Instrument.StandardIlluminant)tmp).Uniform();
            packetContent.Dequeue(out tmp);
            para.MeasurementCaliber = isKSFlag?((Instrument.MeasurementCaliber)tmp).UniformForKS(): ((Instrument.MeasurementCaliber)tmp).Uniform();
            packetContent.Dequeue(out tmp);
            para.SpecularComponent = ((Instrument.SpecularComponent)tmp).Uniform();
            packetContent.Dequeue(out tmp);
            para.Observer = ((Instrument.StandardObserver)tmp).Uniform();
            packetContent.Dequeue(out tmp);
//            para.UV = (UVFlag)tmp;
            para._measurementCondition = tmp;
        }

        internal static void Enqueue(this PacketContent packetContent, MeasurementParameter para, bool isKSFlag = false)
        {
            packetContent.Enqueue((byte)para.PrimaryIlluminant.ToInstrumentForm());
            packetContent.Enqueue((byte)para.SecondaryIlluminant.ToInstrumentForm());
            packetContent.Enqueue((byte)para.TertiaryIlluminant.ToInstrumentForm());
            // 与 Dequeue(isKSFlag) 对称：KS/PS 下发口径也走特殊编码
            packetContent.Enqueue((byte)(isKSFlag
                ? para.MeasurementCaliber.ToInstrumentFormForKS()
                : para.MeasurementCaliber.ToInstrumentForm()));
            packetContent.Enqueue((byte)para.SpecularComponent.ToInstrumentForm());
            packetContent.Enqueue((byte)para.Observer.ToInstrumentForm());
            packetContent.Enqueue((byte)para._measurementCondition);
        }

        internal static void Enqueue(this PacketContent packetContent, ChromaticityOperations operation)
        {
            packetContent.Enqueue((byte)operation.ColorSpace.ToInstrumentForm());
            packetContent.Enqueue((byte)operation.ColorDiffFormula.ToInstrumentForm());
            packetContent.Enqueue((float)operation.Tolerance.dL_Upper);
            packetContent.Enqueue((float)operation.Tolerance.dL_Lower);
            packetContent.Enqueue((float)operation.Tolerance.da_Upper);
            packetContent.Enqueue((float)operation.Tolerance.da_Lower);
            packetContent.Enqueue((float)operation.Tolerance.db_Upper);
            packetContent.Enqueue((float)operation.Tolerance.db_Lower);
            packetContent.Enqueue((float)operation.Tolerance.dE);
        }

        internal static void Dequeue(this PacketContent packetContent, out ChromaticityOperations operation)
        {
            operation = new ChromaticityOperations();
            try
            {
                byte byteValue;
                packetContent.Dequeue(out byteValue);
                operation.ColorSpace = ((Instrument.ColorSpace)byteValue).Uniform();
                packetContent.Dequeue(out byteValue);
                operation.ColorDiffFormula = ((Instrument.ColorDiffFormula)byteValue).Uniform();

                operation.Tolerance = packetContent.DequeueTolerance2();

            }
            catch (Exception )
            {
                operation = null;
                throw;
            }
        }

        internal static void Dequeue(this PacketContent packetContent,
            out Instrument.DateTime dateTime)
        {
            packetContent.Dequeue(out dateTime.Day);
            packetContent.Dequeue(out dateTime.Month);
            packetContent.Dequeue(out dateTime.Year);
            packetContent.Dequeue(out dateTime.Second);
            packetContent.Dequeue(out dateTime.Minute);
            packetContent.Dequeue(out dateTime.Hour);
        }

        /// <summary>
        /// 从数据包中弹出反射率数据
        /// </summary>
        /// <param name="packetContent"></param>
        /// <param name="spectrum">接数数据包中的反射率的Spectrum对象，必须在传入前指定好反射率的范围</param>
        internal static void Dequeue(this PacketContent packetContent, Spectrum spectrum)
        {
            // 弹出反射率数据
            float tmp;
            for (int count = 0; count < spectrum.Range.Count; ++count)
            {
                if (packetContent.Length < sizeof(float))
                {
                    throw new ArgumentException(string.Format("Reflectance data count not equals {0}",
                        spectrum.Range.Count));
                }
                packetContent.Dequeue(out tmp);
                spectrum.Data[count] = tmp;
            }
        }

        /// <summary>
        /// 将反射率数据压入到数据包中
        /// </summary>
        /// <param name="packetContent">数据包</param>
        /// <param name="spectrum">反射率数据</param>
        internal static void Enqueue(this PacketContent packetContent, Spectrum spectrum)
        {
            for (int i = 0; i < spectrum.Range.Count; i++)
            {
                packetContent.Enqueue((float)spectrum.Data[i]);
            }
        }

        // 丢弃颜色值
        static void DiscardColorData(this PacketContent packetContent)
        {
            packetContent.Discard(sizeof(float) * Spectrometer.WAVELENGTHRANGE.Count);
        }

        // 弹出颜色值
        static void DequeueColorValues(this PacketContent packetContent, 
            bool isXyz,
            out StandardObserver observer, 
            out StandardIlluminant illuminant,
            out CIEXYZ xyz)
        {
            var bytes = new List<byte>(packetContent.Bytes);
            observer = ((Instrument.StandardObserver)bytes[0]).Uniform();
            illuminant = ((Instrument.StandardIlluminant)bytes[1]).Uniform();

            float[] values = new float[3];
            values[0] = EndianConverter.NetworkOrderBytesToFloat(bytes.GetRange(2, 4).ToArray());
            values[1] = EndianConverter.NetworkOrderBytesToFloat(bytes.GetRange(6, 4).ToArray());
            values[2] = EndianConverter.NetworkOrderBytesToFloat(bytes.GetRange(10, 4).ToArray());

            packetContent.Discard(Spectrometer.WAVELENGTHRANGE.Count * sizeof(float));

            if (isXyz)
            {
                xyz = new CIEXYZ(values[0], values[1], values[2]);
            }
            else // is Lab
            {
                Formula formula = new Formula(illuminant, observer);
                CIELAB lab = new CIELAB(values[0], values[1], values[2]);
                xyz = formula.LabToXyz(lab);
            }
        }

        internal static void Dequeue(this PacketContent packetContent, 
            out Instrument.SampleData sample)
        {
            sample = new Instrument.SampleData();

            try
            {
                packetContent.Dequeue(out sample.flags);        // 标志
                Trace.TraceInformation("  flags=0x{0:X4}, remain={1}", sample.flags, packetContent.Length);
                packetContent.Dequeue(out sample.dateTime);     // 日期
                Trace.TraceInformation("  dateTime={0:yyyy-MM-dd HH:mm:ss}, remain={1}",
    sample.dateTime.ToSystemDateTime(), packetContent.Length);
                packetContent.Dequeue(out sample.index);        // 索引
                Trace.TraceInformation("  index=0x{0:X4}, remain={1}", sample.index, packetContent.Length);
                // 名称
                byte[] name = new byte[NameLength];
                for (int i = 0; i < NameLength; i++)
                {
                    packetContent.Dequeue(out name[i]);
                }
                int n = Array.IndexOf(name, (byte)0);

                if (n > 0)
                    sample.name = Encoding.ASCII.GetString(name, 0, n);
                else
                    sample.name = Encoding.ASCII.GetString(name);
                Trace.TraceInformation("  name=0x{0:X4}, remain={1},sample.name", string.Join(" ", name.Select(b => b.ToString("X2"))), packetContent.Length, sample.name);
                // 标样序号
                packetContent.Dequeue(out sample.standard);

                // sci 和 sce数据

                ColorSpecimen.ColorData sci = new ColorSpecimen.ColorData();
                ColorSpecimen.ColorData sce = new ColorSpecimen.ColorData();

                SampleDataFlags flags = new SampleDataFlags(sample.flags);
                if (sample.IsStandard)
                {
                    flags.HasTolerance = true;
                    //flags.SpecularComponent = SpecularComponent.SCIAndSCE;
                    sample.flags = flags.Flags;
                }

                if (flags.IsReflectance)
                {
                    if (flags.HasSCI)
                    {
                        sci.Spectrum = new Spectrum(Spectrometer.WAVELENGTHRANGE);
                        packetContent.Dequeue(sci.Spectrum);
                    }
                    else
                    {
                        packetContent.DiscardColorData();
                    }

                    if (flags.HasSCE)
                    {
                        sce.Spectrum = new Spectrum(Spectrometer.WAVELENGTHRANGE);
                        packetContent.Dequeue(sce.Spectrum);
                    }
                    else
                    {
                        packetContent.DiscardColorData();
                    }
                }
                else
                {
                    StandardIlluminant illuminant;
                    StandardObserver observer;
                    CIEXYZ xyz = null;

                    if (flags.HasSCI)
                    {
                        packetContent.DequeueColorValues(flags.IsXyz, out observer, out illuminant, out xyz);
                        sci.Set(observer, illuminant, xyz);
                    }
                    else
                    {
                        packetContent.DiscardColorData();
                    }


                    if (flags.HasSCE)
                    {
                        packetContent.DequeueColorValues(flags.IsXyz, out observer, out illuminant, out xyz);
                        sce.Set(observer, illuminant, xyz);
                    }
                    else
                    {
                        packetContent.DiscardColorData();
                    }
                }

                sample.sci = flags.HasSCI ? sci : null;
                sample.sce = flags.HasSCE ? sce : null;

                Trace.TraceInformation("  DequeueTolerance2 before={0}",  packetContent.Length);
                if (flags.HasTolerance)
                {
                    sample.tolerance = packetContent.DequeueTolerance2();
                }
                Trace.TraceInformation("  DequeueTolerance2 end={0}",  packetContent.Length);
                // 因为标样的索引在仪器中是从0开始，但在使用时又是从1开始，所以将其加1
                //if (sample.IsStandard)
                sample.index += 1;
            }
            catch(Exception)
            {
                sample = null;
                throw;
            }
        }

    }
}
