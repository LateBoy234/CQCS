﻿﻿﻿﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThreeNH.SQCX.Infrastructure.Instruments.YL30.Legacy;
using  param($mt) $map[$mt.Value] ;
using  param($mt) $map[$mt.Value] ;

namespace ThreeNH.SQCX.Infrastructure.Instruments.YL30.CommunicationContract
{
    public interface ISpectrometerCom
    {
        /// <summary>
        /// 获取系统容差值
        /// </summary>
        /// <returns></returns>
        Task<RequestResult<ToleranceForYL30>> GetSystemToleranceAsync();

        /// <summary>
        /// 获取校准数据
        /// </summary>
        /// <param name="mode"></param>
        /// <param name="isCorr"></param>
        /// <returns></returns>

        Task<RequestResult<ColorSpecimen.Specimen>> ExecuteCalibrationAndMeasureAsync(CaliberMeasure mode, ushort startAddress, bool isCorr);

        /// <summary>
        /// 获取仪器基本信息
        /// </summary>
        /// <returns></returns>

        Task<RequestResult<SystemInfo>> GetSystemInfoAsync();
    }
}
