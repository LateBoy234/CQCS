using System.Runtime.InteropServices;

namespace ThreeNH.SQCX.Infrastructure.Instruments.YL30
{
    [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
    public struct ComSettingStruct
    {
        public  ushort SlaveID;

        public ushort  Rate;

        public ushort  CommInterface; 
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
    public struct SystemInfoStruct
    {
        public ushort CalbirationStatus;
        /// <summary>
        /// 仪器名称
        /// </summary>
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)]
        public string Name;
        /// <summary>
        /// 编号
        /// </summary>
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)]
        public string SN;
        /// <summary>
        /// 硬件版本
        /// </summary>
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)]
        public string HardwareVersion;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)]
        public string SoftwareVersion;

        public ushort ExpirtTimeExternal;
        public ushort ExpirtTimeInternal;
    }
}
