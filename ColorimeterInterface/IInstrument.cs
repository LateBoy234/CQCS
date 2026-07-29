using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ThreeNH.Instruments.Connection;
using ThreeNH.Instruments.DeviceInfo;

namespace ThreeNH.ColorimeterInterface
{
    /// <summary>
    /// 仪器通用接口（所有仪器的基接口）。
    /// 提供连接管理（Open/Close）、设备基础信息（<see cref="IDeviceInfo"/>）与测量/校准能力（<see cref="IMeasurement"/>）。
    /// 具体仪器类型在其上扩展，例如色差仪 <see cref="IColorimeter"/>。
    /// </summary>
    public interface IInstrument : IMeasurement, IDeviceInfo, IInstrumentConnection
    {

        /// <summary>
        /// 设备名
        /// </summary>
        string DeviceName { get; }
        /// <summary>
        /// 重新从仪器拉取型号/版本等基础信息（连接后常用，故放在基接口）
        /// </summary>
        void RefreshInstrumentInfo();
    }
}
