﻿using System;
using System.Text;
using ThreeNH.CommunicationBase;
using ThreeNH.ColorAlgorithm;
using System.Runtime.InteropServices;

namespace ThreeNH.SQCX.Infrastructure.Instruments.NRColorimeter
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

                packetContent.Dequeue(out strValue);
                info.SN = strValue;
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
            catch (Exception)
            {
                para = null;
                throw;
            }
        }

        internal static void Dequeue(this PacketContent packetContent,
            ref Instrument.DateTime dateTime)
        {
            packetContent.Dequeue(out dateTime.Day);
            packetContent.Dequeue(out dateTime.Month);
            packetContent.Dequeue(out dateTime.Year);
            packetContent.Dequeue(out dateTime.Second);
            packetContent.Dequeue(out dateTime.Minute);
            packetContent.Dequeue(out dateTime.Hour);
        }


        internal static void Dequeue(this PacketContent packetContent, out Instrument.SampleData sampleData)
        {
            sampleData = new Instrument.SampleData();
            Dequeue(packetContent, ref sampleData.Time);
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
            packetContent.Discard(2);
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
