using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YL30.Model
{
    public class SystemInfo
    {
        public CalibrationStatus CalibrationStatus { get; set; }
        public string CustomType { get; set; }
        /// <summary>
        /// 仪器型号
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// SN号
        /// </summary>
        public string SN {  get; set; }
        /// <summary>
        /// 软件版本
        /// </summary>
        public string SoftwareVersion {  get; set; }
        /// <summary>
        /// 硬件版本
        /// </summary>
        public string HardwareVersion {  get; set; }
        public ushort ExpirtTimeExternal { get; set; }
        public ushort ExpirtTimeInternal { get; set; }
    }
}
