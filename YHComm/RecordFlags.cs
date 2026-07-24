using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using ThreeNH.Optical;

namespace ThreeNH.YHComm
{
    [StructLayout(LayoutKind.Sequential, Size = 4)]
    public struct RecordFlags
    {
        private static readonly BitVector32.Section ClrDataSect = BitVector32.CreateSection(1); // 1bits
        private static readonly BitVector32.Section HazeDataSect = BitVector32.CreateSection(1, ClrDataSect); // 1bits
        private static readonly BitVector32.Section TransmittanceSect = BitVector32.CreateSection(1, HazeDataSect); // 1bits
        private static readonly BitVector32.Section ClaritySect = BitVector32.CreateSection(1, TransmittanceSect); // 1bits
        private static readonly BitVector32.Section ApertureSect = BitVector32.CreateSection(15, ClaritySect); // 4bits
        private static readonly BitVector32.Section UvModeSect = BitVector32.CreateSection(7, ApertureSect); // 3bits
        private static readonly BitVector32.Section ScModeSect = BitVector32.CreateSection(3, UvModeSect); // 2bits
        private static readonly BitVector32.Section ClrTypeSect = BitVector32.CreateSection(7, ScModeSect); // 3bits
        private static readonly BitVector32.Section LockSect = BitVector32.CreateSection(1, ClrTypeSect); // 1bits
        private static readonly BitVector32.Section ReferStandardSect = BitVector32.CreateSection(15, LockSect); // 4bits

        private BitVector32 _flags;

        public enum ColorDataType
        {
            XYZ,             // 用户输入的XYZ数据
            Lab,            // 用户输入的Lab数据
            Spectrum,       // 反射率数据
            SingleValue,    // 一个单精度浮点值
        }

        public RecordFlags(int flags)
        {
            _flags = new BitVector32(flags);
        }


        public int Flags
        {
            get { return _flags.Data; }
            set { _flags = new BitVector32(value); }
        }

        /// <summary>
        /// 颜色值是否有效
        /// </summary>
        public bool IsColorVaild
        {
            get => _flags[ClrDataSect] != 0;
            set => _flags[ClrDataSect] = value ? 1 : 0;
        }

        /// <summary>
        /// 雾度值是否有效
        /// </summary>
        public bool IsHazeValid
        {
            get => _flags[HazeDataSect] != 0;
            set => _flags[HazeDataSect] = value ? 1 : 0;
        }

        /// <summary>
        /// 透过率值是否有效
        /// </summary>
        public bool IsTransmittanceValid
        {
            get => _flags[TransmittanceSect] != 0;
            set => _flags[TransmittanceSect] = value ? 1 : 0;
        }

        public bool IsClarityValid
        {
            get => _flags[ClaritySect] != 0;
            set => _flags[ClaritySect] = value ? 1 : 0;
        }

        /// <summary>
        /// 颜色值的类型
        /// </summary>
        public ColorDataType ColorType
        {
            get => (ColorDataType)_flags[ClrTypeSect];
            set => _flags[ClrTypeSect] = (int)value;
        }

        /// <summary>
        /// 测量口径
        /// </summary>
        public Aperture Aperture
        {
            get => (Aperture)_flags[ApertureSect];
            set => _flags[ApertureSect] = (int)value;
        }

        /// <summary>
        /// 标样锁定标记
        /// </summary>
        public bool IsLock
        {
            get => _flags[LockSect] == 1;
            set => _flags[LockSect] = value ? 1 : 0;
        }

        public HazeReferStandard ReferStandard
        {
            get => (HazeReferStandard)_flags[ReferStandardSect];
            set => _flags[ReferStandardSect] = (int) value;
        }
    }
}
