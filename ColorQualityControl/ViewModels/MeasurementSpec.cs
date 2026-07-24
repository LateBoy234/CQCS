using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ThreeNH.ColorQualityControl.ViewModels
{
    /// <summary>
    /// 测量条件
    /// </summary>
    public class MeasurementSpec
    {
        /// <summary>
        /// 仪器型号
        /// </summary>
        public string DeviceModel { get; set; }

        /// <summary>
        /// 光学结构
        /// </summary>
        public string OpticalStructure { get; set; }

        /// <summary>
        /// 口径
        /// </summary>
        public string Aperture { get; set; }

        /// <summary>
        /// 测量条件
        /// </summary>
        public string MeasurementCondition { get; set; }
    }
}
