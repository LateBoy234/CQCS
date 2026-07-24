using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using YL30.Model;

namespace YL30
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
