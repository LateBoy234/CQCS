﻿﻿﻿
using System;
using System.Linq;
using System.Runtime.InteropServices;
using ThreeNH.ColorAlgorithm;
using ThreeNH.ColorSpecimen;
using ThreeNH.DataModel;

namespace ThreeNH.SQCX.Infrastructure.Instruments.YL30.Legacy.Model
{
    [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
    public struct StandardStruct
    {
        /// <summary>
        /// 颜色空间值
        /// </summary>
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 40)]
        public string ColorSpaceValue;

        /// <summary>
        /// 白板SCI反射率系数 大端数据
        /// </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = Constants.SPECTROGRAPH_WHITEBOARDREFLECTANCE_COUNT)]
        public ushort[] SCIReflectance;

        /// <summary>
        /// 白板SCE反射率系数 大端数据
        /// </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = Constants.SPECTROGRAPH_WHITEBOARDREFLECTANCE_COUNT)]
        public ushort[] SCEReflectance;

        /// <summary>
        /// 样品ID 小端数据
        /// </summary>
        public int ID;

        /// <summary>
        /// 样品名称
        /// </summary>
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)]
        public string Name;

        /// <summary>
        /// 测量时间 大端数据
        /// </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
        public ushort[] Time;
        /// <summary>
        /// 容差设置，包括主容差和两个参考容差
        /// </summary>
        /// <param name="tolerance"></param>
        /// <returns></returns>
        public StandardSpecimen ToSpecimen(ToleranceForYL30 tolerance)
        {
            StandardSpecimen specimen = new StandardSpecimen();
            specimen.IsTransmission = false;
            specimen.DateTime = Time.ToDateTime();
            specimen.ID = ID;
            specimen.Name = Name;
            var waverange = new WavelengthRange(Constants.VALID_WAVE_BEGIN, Constants.VALID_WAVE_END, Constants.VALID_WAVE_STEP);

            specimen.SCI = new ColorData(new Spectrum(waverange, Array.ConvertAll(SCIReflectance.ToArray(), x => (float)x * 0.0001)));

            specimen.ToleranceSetting = new ToleranceSetting { PrimaryTolerance = tolerance.Uniform() };

            return specimen;
        }
    }

    /// <summary>
    /// 试样结构体（仿照 ThreeNH.SQCX.Infrastructure.Instruments.PTS.Record.TrialRecord）
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
    public struct SampleStruct
    {
        /// <summary>
        /// 颜色空间值
        /// </summary>
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 60)]
        public string ColorSpaceValue;

        /// <summary>
        /// 试样 SCI 反射率系数 大端数据
        /// </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = Constants.SPECTROGRAPH_WHITEBOARDREFLECTANCE_COUNT)]
        public ushort[] SCIReflectance;

        /// <summary>
        /// 试样 SCE 反射率系数 大端数据
        /// </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = Constants.SPECTROGRAPH_WHITEBOARDREFLECTANCE_COUNT)]
        public ushort[] SCEReflectance;

        /// <summary>
        /// 对应的标样 ID 小端数据
        /// </summary>
         public uint StandardID;
        /// <summary>
        /// 样品 ID 小端数据
        /// </summary>
        public int ID;

        /// <summary>
        /// 样品名称
        /// </summary>
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)]
        public string Name;

        /// <summary>
        /// 测量时间 大端数据
        /// </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
        public ushort[] Time;
        /// <summary>
        /// 转换为 Specimen 对象
        /// </summary>
        /// <param name="isSpecialType">是否为特殊类型</param>
        /// <returns></returns>
        public ThreeNH.ColorSpecimen.Specimen ToSpecimen(bool isSpecialType = false)
        {
            ThreeNH.ColorSpecimen.Specimen specimen = new ThreeNH.ColorSpecimen.Specimen();
           // specimen.IsTransmission = false;
            specimen.DateTime = Time.ToDateTime();
            specimen.ID = ID;
            specimen.Name = Name;
            
            var waverange = new WavelengthRange(Constants.VALID_WAVE_BEGIN, Constants.VALID_WAVE_END, Constants.VALID_WAVE_STEP);

            if (SCIReflectance != null)
            {
                var spectrum = new Spectrum(waverange, Array.ConvertAll(SCIReflectance.ToArray(), x => (float)x * 0.0001));
                if (isSpecialType)
                    spectrum = Formula.CalculateSpectrum(spectrum);
                specimen.SCI = new ColorData(spectrum);
            }

            if (SCEReflectance != null)
            {
                var spectrum = new Spectrum(waverange, Array.ConvertAll(SCEReflectance.ToArray(), x => (float)x * 0.0001));
                if (isSpecialType)
                    spectrum = Formula.CalculateSpectrum(spectrum);
                specimen.SCE = new ColorData(spectrum);
            }

            // 设置对应的标样 ID
           // specimen.Tag = (int)StandardID;

            return specimen;
        }
    }
}
