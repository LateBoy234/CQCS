using System;
using System.Text;
using ThreeNH.CommunicationBase;
using ThreeNH.ColorAlgorithm;

namespace ThreeNH.SQCX.Infrastructure.Instruments.LedSpectrometer
{
    static class PacketContentExtension
    {
        private const int NameLength = 9;

        internal static void Dequeue(this PacketContent packetContent, out DeviceInfo info)
        {
            info = new DeviceInfo();
            try
            {
                string strValue;
                ushort shortValue;

                packetContent.Dequeue(out strValue);
                info.DeviceName = strValue;
                packetContent.Dequeue(out strValue);
                info.DeviceNumber = strValue;
                packetContent.Dequeue(out strValue);
                info.FirmwareVersion = strValue;
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

                byte tmp;

                packetContent.Dequeue(out tmp);
                info.BlackCalibration = tmp != 0;

                packetContent.Dequeue(out tmp);
                info.WhiteCalibration = tmp != 0;
            }
            catch(Exception)
            {
                info = null;
                throw;
            }
        }

        internal static void Dequeue(this PacketContent packetContent, out MeasurementParameter para)
        {
            para = new MeasurementParameter();
            try
            {
                byte tmp;

                packetContent.Dequeue(out tmp);
                para.PrimaryIlluminant = ((Instrument.StandardIlluminant)tmp).Uniform();
                packetContent.Dequeue(out tmp);
                para.SecondaryIlluminant = ((Instrument.StandardIlluminant)tmp).Uniform();
                packetContent.Dequeue(out tmp);
                para.TertiaryIlluminant = ((Instrument.StandardIlluminant)tmp).Uniform();
                packetContent.Dequeue(out tmp);
                para.MeasurementCaliber = ((Instrument.MeasurementCaliber)tmp).Uniform();
                packetContent.Dequeue(out tmp);
                para.SpecularComponent = ((Instrument.SpecularComponent)tmp).Uniform();
                packetContent.Dequeue(out tmp);
                para.Observer = ((Instrument.StandardObserver)tmp).Uniform();
            }
            catch(Exception)
            {
                para = null;
                throw;
            }
        }

        internal static void Enqueue(this PacketContent packetContent, MeasurementParameter para)
        {
            packetContent.Enqueue((byte)para.PrimaryIlluminant.ToInstrumentForm());
            packetContent.Enqueue((byte)para.SecondaryIlluminant.ToInstrumentForm());
            packetContent.Enqueue((byte)para.TertiaryIlluminant.ToInstrumentForm());
            packetContent.Enqueue((byte)para.MeasurementCaliber.ToInstrumentForm());
            packetContent.Enqueue((byte)para.SpecularComponent.ToInstrumentForm());
            packetContent.Enqueue((byte)para.Observer.ToInstrumentForm());
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

        internal static void Dequeue(this PacketContent packetContent,
            Spectrum spectrum)
        {
            // 弹出反射率数据
            float tmp;
            for(int count = 0; count < spectrum.Range.Count; ++count)
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

        internal static void Enqueue(this PacketContent packetContent, Spectrum spectrum)
        {
            for (int i = 0; i < spectrum.Range.Count; i++)
            {
                packetContent.Enqueue((float)spectrum.Data[i]);
            }
        }

        internal static void Dequeue(this PacketContent packetContent, 
            Instrument.SampleData sample)
        {
            packetContent.Dequeue(out sample.Time);

            packetContent.Dequeue(out sample.L);
            packetContent.Dequeue(out sample.a);
            packetContent.Dequeue(out sample.b);
            packetContent.Dequeue(out sample.C);
            packetContent.Dequeue(out sample.h);

            byte tmp;
            packetContent.Dequeue(out tmp);
            sample.Illuminant = ((Instrument.StandardIlluminant)tmp).Uniform();

            packetContent.Dequeue(out tmp);
            sample.Observer = ((Instrument.StandardObserver)tmp).Uniform();

            packetContent.Dequeue(out tmp);
            sample.SpecularComponent = ((Instrument.SpecularComponent)tmp).Uniform();

            packetContent.Dequeue(out sample.Index);

            byte[] name = new byte[NameLength];
            for(int i = 0; i < NameLength; i++)
            {
                packetContent.Dequeue(out name[i]);
            }
            int n = Array.IndexOf(name, (byte)0);
            if (n != -1)
                sample.Name = Encoding.ASCII.GetString(name, 0, n);
            else
                sample.Name = Encoding.ASCII.GetString(name);

            packetContent.Dequeue(out sample.StandardIndex);

            Spectrum spectrum = new Spectrum(Spectrometer.WAVELENGTHRANGE);
            packetContent.Dequeue(spectrum);
            sample.Spectrum = spectrum;

            // 由于标样的索引是从0开始，但在使用时是从1开始，所以如果是标样，就将索引加1
            if (sample.IsStandard)
            {
                sample.Index += 1;
            }
        }

        //public static int IndexOf<T>(this IEnumerable<T> enumerable, T value)
        //{
        //    int i = 0;
        //    foreach(T elem in enumerable)
        //    {
        //        if (elem.Equals(value))
        //            return i;
        //        i++;
        //    }
        //    return -1;
        //}
    }
}
