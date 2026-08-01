using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using ThreeNH.Optical;

namespace ThreeNH.SQCX.Infrastructure.Instruments.BTS
{
    public struct RecordFlags
    {
        private static readonly BitVector32.Section OpticalSect = BitVector32.CreateSection(1);
        private static readonly BitVector32.Section ScModeSect = BitVector32.CreateSection(3, OpticalSect);
        private static readonly BitVector32.Section UvModeSect = BitVector32.CreateSection(3, ScModeSect);
        private static readonly BitVector32.Section CaliberSect = BitVector32.CreateSection(7, UvModeSect);
        private static readonly BitVector32.Section LensPosSect = BitVector32.CreateSection(7, CaliberSect);
        private static readonly BitVector32.Section DataTypeSect = BitVector32.CreateSection(3, LensPosSect);
        private static readonly BitVector32.Section LockSect = BitVector32.CreateSection(1, DataTypeSect);
        private static readonly BitVector32.Section ReservedSect = BitVector32.CreateSection(3, LockSect);
        private static readonly BitVector32.Section ObserverSect = BitVector32.CreateSection(255, ReservedSect);
        private static readonly BitVector32.Section IlluminantSect = BitVector32.CreateSection(255, ObserverSect);


        public enum RecordDataType
        {
            Spectrum = 0,   // 反射率数据
            Lab,                         // 用户输入的Lab数据
            XYZ                         // 用户输入的XYZ数据
        }

        public RecordFlags(int flags)
        {
            _flags = new BitVector32(flags);
        }


        private BitVector32 _flags;
        public int Flags
        {
            get { return _flags.Data; }
            set { _flags = new BitVector32(value); }
        }

        public ScMode ScMode
        {
            get { return (ScMode)_flags[ScModeSect]; }
            set { _flags[ScModeSect] = (int)value; }
        }

        public bool HasSCI
        {
            get { return ScMode == ScMode.SCI || ScMode == ScMode.SCIAndSCE; }
        }

        public bool HasSCE
        {
            get { return ScMode == ScMode.SCE || ScMode == ScMode.SCIAndSCE; }
        }


        public UvMode UvMode
        {
            get { return (UvMode)_flags[UvModeSect]; }
            set { _flags[UvModeSect] = (int)value; }
        }


        public RecordDataType DataType
        {
            get { return (RecordDataType)_flags[DataTypeSect]; }
            set { _flags[DataTypeSect] = (int)value; }
        }

        public bool IsXyz => DataType == RecordDataType.XYZ;

        public bool IsLab => DataType == RecordDataType.Lab;

        public bool IsSpectrum => DataType == RecordDataType.Spectrum;


        public Caliber Caliber
        {
            get { return (Caliber)_flags[CaliberSect]; }
            set { _flags[CaliberSect] = (int)value; }
        }

        public LensPos LensPos
        {
            get { return (LensPos)_flags[LensPosSect]; }
            set { _flags[LensPosSect] = (int)value; }
        }

        public bool IsTransmission
        {
            get { return _flags[OpticalSect] == 1; }
            set { _flags[OpticalSect] = value ? 1 : 0; }
        }

        public bool IsLock
        {
            get { return _flags[LockSect] == 1; }
            set { _flags[LockSect] = value ? 1 : 0; }
        }

        public byte Illuminant
        {
            get { return (byte)_flags[IlluminantSect]; }
            set { _flags[IlluminantSect] = value; }
        }
        public byte Observer
        {
            get { return (byte)_flags[ObserverSect]; }
            set { _flags[ObserverSect] = value; }
        }
    }
}
