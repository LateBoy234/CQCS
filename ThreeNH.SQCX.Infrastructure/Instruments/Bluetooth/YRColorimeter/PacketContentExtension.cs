﻿using System;
using System.Text;
using ThreeNH.CommunicationBase;
using ThreeNH.ColorAlgorithm;
using System.Runtime.InteropServices;

namespace ThreeNH.SQCX.Infrastructure.Instruments.YRColorimeter
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
                info.Model = strValue;
                packetContent.Dequeue(out strValue);
                info.OpticalStruct = strValue;
                packetContent.Dequeue(out strValue);
                info.HardwareVersion = strValue;
                packetContent.Dequeue(out strValue);
                info.FirmwareVersion = strValue;
                packetContent.Dequeue(out strValue);
                info.InternalSnOfWhiteBoard = strValue;
                packetContent.Dequeue(out strValue);
                info.ExternalSnOfWhiteBoard = strValue;
                packetContent.Dequeue(out shortValue);
                info.Capacity = shortValue;
                packetContent.Dequeue(out shortValue);
                info.StandardCount = shortValue;
                packetContent.Dequeue(out shortValue);
                info.TestCount = shortValue;

                byte tmp;
                packetContent.Dequeue(out tmp);
                info.IsBlackCalibrated = tmp != 0;
                packetContent.Dequeue(out tmp);
                info.IsWhiteCalibrated = tmp != 0;

                packetContent.Dequeue(out strValue);
                info.Sn = strValue;
                packetContent.Dequeue(out strValue);
                info.CustomModel = strValue;

                info.Tolerance = new Tolerance();
                info.Tolerance.dE = packetContent.DequeueFloat();
                info.Tolerance.dL_Upper = packetContent.DequeueFloat();
                info.Tolerance.da_Upper = packetContent.DequeueFloat();
                info.Tolerance.db_Upper = packetContent.DequeueFloat();
                info.Tolerance.dL_Lower = packetContent.DequeueFloat();
                info.Tolerance.da_Lower = packetContent.DequeueFloat();
                info.Tolerance.db_Lower = packetContent.DequeueFloat();
            }
            catch(Exception)
            {
                info = null;
                throw;
            }
        }

        internal static FactoryInfo DequeueFactoryInfo(this PacketContent packetContent)
        {
            var info = new FactoryInfo();
            info.HardwareVersion = packetContent.DequeueString();
            info.SoftwareVersion = packetContent.DequeueString();
            info.Sn = packetContent.DequeueString();
            info.CustomModel = packetContent.DequeueString();
            return info;
        }

        internal static void Enqueue(this PacketContent packetContent, FactoryInfo info)
        {
            packetContent.Enqueue(info.HardwareVersion);
            packetContent.Enqueue(info.SoftwareVersion);
            packetContent.Enqueue(info.Sn);
            packetContent.Enqueue(info.CustomModel);
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
            catch (Exception)
            {
                para = null;
                throw;
            }
        }

        internal static void Dequeue(this PacketContent packetContent,
            ref Instrument.DateTime dateTime)
        {
            packetContent.Dequeue(out dateTime.Year);
            packetContent.Dequeue(out dateTime.Month);
            packetContent.Dequeue(out dateTime.Day);
            packetContent.Dequeue(out dateTime.Hour);
            packetContent.Dequeue(out dateTime.Minute);
            packetContent.Dequeue(out dateTime.Second);
        }

        internal static ColorData DequeueColorData(this PacketContent packetContent)
        {
            ColorData clr = new ColorData();
            clr.Observer = ((Instrument.StandardObserver)packetContent.DequeueByte()).Uniform();
            clr.Illuminant = ((Instrument.StandardIlluminant)packetContent.DequeueByte()).Uniform();
            for (int i = 0; i < 3; i++)
            {
                clr.Value[i] = packetContent.DequeueFloat();
            }
            return clr;
        }

        internal static Tolerance DequeueTolerance(this PacketContent packetContent)
        {
            Tolerance tolerance = new Tolerance();
            for (int i = 0; i < 7; i++)
            {
                tolerance.Data[i] = packetContent.DequeueFloat();
            }
            return tolerance;
        }

        internal static YRSamplingData DequeueRawData(this PacketContent packetContent)
        {
            YRSamplingData raw_data = new YRSamplingData();

            for (int i = 0; i < 3; i++)
            {
                raw_data.MajorLightOn[i] = packetContent.DequeueUInt16();
            }
            for (int i = 0; i < 3; i++)
            {
                raw_data.MinorLightOn[i] = packetContent.DequeueUInt16();
            }
            for (int i = 0; i < 3; i++)
            {
                raw_data.MajorLightOff[i] = packetContent.DequeueUInt16();
            }
            for (int i = 0; i < 3; i++)
            {
                raw_data.MinorLightOff[i] = packetContent.DequeueUInt16();
            }

            return raw_data;
        }

        internal static string DequeueSampleName(this PacketContent packetContent, int max_length)
        {

            byte[] name = new byte[max_length];
            for (int i = 0; i < max_length; i++)
            {
                name[i] = packetContent.DequeueByte();
            }
            int n = Array.IndexOf(name, (byte)0);
            if (n > 0)
                return Encoding.ASCII.GetString(name, 0, n);
            else
                return Encoding.ASCII.GetString(name);
        }


        internal static void Dequeue(this PacketContent packetContent, out SampleData sampleData)
        {
            sampleData = new SampleData();
            sampleData.Flags = new SampleFlags { Value = packetContent.DequeueUInt32() };
            sampleData.Index = packetContent.DequeueUInt16() + 1;
            sampleData.StdIndex = packetContent.DequeueUInt16();
            sampleData.Name = packetContent.DequeueSampleName(SampleData.NAME_LENGTH);

            Instrument.DateTime dateTime = new Instrument.DateTime();
            Dequeue(packetContent, ref dateTime);
            try
            {
                sampleData.DateTime = new DateTime(dateTime.Year, dateTime.Month, dateTime.Day,
                    dateTime.Hour, dateTime.Minute, dateTime.Second);
            }
            catch
            {
                sampleData.DateTime = DateTime.Now;
            }
            sampleData.Color[0] = packetContent.DequeueColorData();
            sampleData.Color[1] = packetContent.DequeueColorData();
            if (sampleData.Flags.HasTolerance)
            {
                sampleData.Tolerance = packetContent.DequeueTolerance();
            }
            if (sampleData.Flags.HasRawData)
            {
                sampleData.RawData = packetContent.DequeueRawData();
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
    }
}
