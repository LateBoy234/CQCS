﻿﻿﻿using System;
using System.Runtime.InteropServices;
using ThreeNH.ColorAlgorithm;
using ThreeNH.ColorimeterInterface;
using ThreeNH.Optical;

namespace ThreeNH.SQCX.Infrastructure.Instruments.BTS
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 4)]
    public class ProductQeuipInfoV1
    {
        private ushort _flagUpdate;
        private uint _logoAddr;
        private uint _logoLength;
        private byte _defaultLanguage;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)]
        private string _softwareVersion;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)]
        private string _hardwareVersion;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)]
        private string _sn;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)]
        private string _blackPlate;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)]
        private string _whitePlate;


        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        private float[] _waveCoefMajor = new float[8];

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        private float[] _waveCoefMinor = new float[8];

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = Constants.VALID_WAVE_NUM)]
        private float[] _blackReflect = new float[Constants.VALID_WAVE_NUM];

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = (Constants.VALID_WAVE_NUM))]
        private float[] _whiteReflectSci = new float[Constants.VALID_WAVE_NUM];

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = (Constants.VALID_WAVE_NUM))]
        private float[] _whiteReflectSce = new float[Constants.VALID_WAVE_NUM];

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = Constants.VALID_WAVE_NUM)]
        private float[] _reflectModify = new float[Constants.VALID_WAVE_NUM];

        private ushort _lensMotorAllStep;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = (int)LensPos.Num)]
        private ushort[] _lensPosStep = new ushort[(int)LensPos.Num];

        private ushort _scMotorRunTime;
        private uint _reflectIntegralTime;
        private uint _transIntegralTime;


        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)]
        private string _majorSensorSn; // 主光谱感应器编号

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)]
        private string _minorSensorSn;  // 辅感应器编号

        public ushort FlagUpdate
        {
            get { return _flagUpdate; }
            set { _flagUpdate = value; }
        }

        /// <summary>
        /// logo文件在产品配置信息文件中的偏移地址
        /// </summary>
        public uint LogoAddr
        {
            get { return _logoAddr; }
            set { _logoAddr = value; }
        }

        /// <summary>
        /// logo文件长度
        /// </summary>
        public uint LogoLength
        {
            get { return _logoLength; }
            set { _logoLength = value; }
        }

        /// <summary>
        /// 缺省语言
        /// </summary>
        public byte DefaultLanguage
        {
            get { return _defaultLanguage; }
            set { _defaultLanguage = value; }
        }

        /// <summary>
        /// 软件版本
        /// </summary>
        public string SoftwareVersion
        {
            get { return _softwareVersion; }
            set { _softwareVersion = value; }
        }

        /// <summary>
        /// 硬件版本
        /// </summary>
        public string HardwareVersion
        {
            get { return _hardwareVersion; }
            set { _hardwareVersion = value; }
        }

        /// <summary>
        /// 序列号
        /// </summary>
        public string Sn
        {
            get { return _sn; }
            set { _sn = value; }
        }

        /// <summary>
        /// 黑筒编号
        /// </summary>
        public string BlackPlate
        {
            get { return _blackPlate; }
            set { _blackPlate = value; }
        }

        /// <summary>
        /// 白板编号
        /// </summary>
        public string WhitePlate
        {
            get { return _whitePlate; }
            set { _whitePlate = value; }
        }

        /// <summary>
        /// 主CCD波长系数
        /// </summary>
        public float[] WaveCoefMajor
        {
            get { return _waveCoefMajor; }
            set
            {
                if (value == null || value.Length != 8)
                {
                    throw new ArgumentException();
                }

                _waveCoefMajor = value;
            }
        }

        /// <summary>
        /// 辅CCD波长系数
        /// </summary>
        public float[] WaveCoefMinor
        {
            get { return _waveCoefMinor; }
            set
            {
                if (value == null || value.Length != 8)
                {
                    throw new ArgumentException();
                }

                _waveCoefMinor = value;
            }
        }

        /// <summary>
        /// 黑筒反射率，测试10次，取平均值
        /// </summary>
        public float[] BlackReflect
        {
            get { return _blackReflect; }
            set
            {
                if (value == null || value.Length != Constants.VALID_WAVE_NUM)
                {
                    throw new ArgumentException();
                }

                _blackReflect = value;
            }
        }

        /// <summary>
        /// 白板反射率(SCI)，用精度更高的仪器标定产生
        /// </summary>
        public float[] WhiteReflectSci
        {
            get { return _whiteReflectSci; }
            set
            {
                if (value == null || value.Length != Constants.VALID_WAVE_NUM)
                {
                    throw new ArgumentException();
                }
                _whiteReflectSci = value;
            }
        }

        /// <summary>
        /// 白板反射率(SCE)，用精度更高的仪器标定产生
        /// </summary>
        public float[] WhiteReflectSce
        {
            get { return _whiteReflectSce; }
            set
            {
                if (value == null || value.Length != Constants.VALID_WAVE_NUM)
                {
                    throw new ArgumentException();
                }

                _whiteReflectSce = value;
            }
        }

        /// <summary>
        /// 反射率修正系数，其他各种修正参数在这里增加
        /// </summary>
        public float[] ReflectModify
        {
            get { return _reflectModify; }
            set
            {
                if (value == null || value.Length != Constants.VALID_WAVE_NUM)
                {
                    throw new ArgumentException();
                }

                _reflectModify = value;
            }
        }

        /// <summary>
        /// 透镜从开始位置光耦到目标位置的步长
        /// </summary>
        public ushort[] LensPosStep
        {
            get { return _lensPosStep; }
            set
            {
                if (value == null || value != _lensPosStep)
                {
                    throw new ArgumentException();
                }

                _lensPosStep = value;
            }
        }

        /// <summary>
        /// sci/sce电机运行时间，
        /// </summary>
        public ushort ScMotorRunTime
        {
            get { return _scMotorRunTime; }
            set { _scMotorRunTime = value; }
        }

        /// <summary>
        /// 反射积分时间
        /// </summary>
        public uint ReflectIntegralTime
        {
            get { return _reflectIntegralTime; }
            set { _reflectIntegralTime = value; }
        }

        /// <summary>
        /// 透射积分时间
        /// </summary>
        public uint TransIntegralTime
        {
            get { return _transIntegralTime; }
            set { _transIntegralTime = value; }
        }

        /// <summary>
        /// 透镜开始位置到光耦结束位置的步长
        /// </summary>
        public ushort LensMotorAllStep
        {
            get { return _lensMotorAllStep; }

            set { _lensMotorAllStep = value; }
        }

        public string MajorSensorSn
        {
            get
            {
                return _majorSensorSn;
            }

            set
            {
                _majorSensorSn = value;
            }
        }

        public string MinorSensorSn
        {
            get
            {
                return _minorSensorSn;
            }

            set
            {
                _minorSensorSn = value;
            }
        }
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 4)]
    public class ProductQeuipInfo
    {
        private ushort _flagUpdate;
        private uint _logoAddr;
        private uint _logoLength;
        private byte _defaultLanguage;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)]
        private string _softwareVersion;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)]
        private string _hardwareVersion;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)]
        private string _sn;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)]
        private string _blackPlate;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)]
        private string _whitePlate;


        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        private float[] _waveCoefMajor = new float[8];

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        private float[] _waveCoefMinor = new float[8];

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = Constants.VALID_WAVE_NUM)]
        private float[] _blackReflect = new float[Constants.VALID_WAVE_NUM];

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = (Constants.VALID_WAVE_NUM))]
        private float[] _whiteReflectSci = new float[Constants.VALID_WAVE_NUM];

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = (Constants.VALID_WAVE_NUM))]
        private float[] _whiteReflectSce = new float[Constants.VALID_WAVE_NUM];

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = Constants.VALID_WAVE_NUM)]
        private float[] _reflectModify = new float[Constants.VALID_WAVE_NUM];

        private ushort _lensMotorAllStep;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = (int)LensPos.Num)]
        private ushort[] _lensPosStep = new ushort[(int)LensPos.Num];

        private ushort _scMotorRunTime;
        private uint _reflectIntegralTime;
        private uint _transIntegralTime;


        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)]
        private string _majorSensorSn; // 主光谱感应器编号

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)]
        private string _minorSensorSn;  // 辅感应器编号

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)]
        private string model_name;                    // 仪器型号名

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 20)]
        private string company;                       // 公司名

        public ushort FlagUpdate
        {
            get { return _flagUpdate; }
            set { _flagUpdate = value; }
        }

        /// <summary>
        /// logo文件在产品配置信息文件中的偏移地址
        /// </summary>
        public uint LogoAddr
        {
            get { return _logoAddr; }
            set { _logoAddr = value; }
        }

        /// <summary>
        /// logo文件长度
        /// </summary>
        public uint LogoLength
        {
            get { return _logoLength; }
            set { _logoLength = value; }
        }

        /// <summary>
        /// 缺省语言
        /// </summary>
        public byte DefaultLanguage
        {
            get { return _defaultLanguage; }
            set { _defaultLanguage = value; }
        }

        /// <summary>
        /// 软件版本
        /// </summary>
        public string SoftwareVersion
        {
            get { return _softwareVersion; }
            set { _softwareVersion = value; }
        }

        /// <summary>
        /// 硬件版本
        /// </summary>
        public string HardwareVersion
        {
            get { return _hardwareVersion; }
            set { _hardwareVersion = value; }
        }

        /// <summary>
        /// 序列号
        /// </summary>
        public string Sn
        {
            get { return _sn; }
            set { _sn = value; }
        }

        /// <summary>
        /// 黑筒编号
        /// </summary>
        public string BlackPlate
        {
            get { return _blackPlate; }
            set { _blackPlate = value; }
        }

        /// <summary>
        /// 白板编号
        /// </summary>
        public string WhitePlate
        {
            get { return _whitePlate; }
            set { _whitePlate = value; }
        }

        /// <summary>
        /// 主CCD波长系数
        /// </summary>
        public float[] WaveCoefMajor
        {
            get { return _waveCoefMajor; }
            set
            {
                if (value == null || value.Length != 8)
                {
                    throw new ArgumentException();
                }

                _waveCoefMajor = value;
            }
        }

        /// <summary>
        /// 辅CCD波长系数
        /// </summary>
        public float[] WaveCoefMinor
        {
            get { return _waveCoefMinor; }
            set
            {
                if (value == null || value.Length != 8)
                {
                    throw new ArgumentException();
                }

                _waveCoefMinor = value;
            }
        }

        /// <summary>
        /// 黑筒反射率，测试10次，取平均值
        /// </summary>
        public float[] BlackReflect
        {
            get { return _blackReflect; }
            set
            {
                if (value == null || value.Length != Constants.VALID_WAVE_NUM)
                {
                    throw new ArgumentException();
                }

                _blackReflect = value;
            }
        }

        /// <summary>
        /// 白板反射率(SCI)，用精度更高的仪器标定产生
        /// </summary>
        public float[] WhiteReflectSci
        {
            get { return _whiteReflectSci; }
            set
            {
                if (value == null || value.Length != Constants.VALID_WAVE_NUM)
                {
                    throw new ArgumentException();
                }

                _whiteReflectSci = value;
            }
        }

        /// <summary>
        /// 白板反射率(SCE)，用精度更高的仪器标定产生
        /// </summary>
        public float[] WhiteReflectSce
        {
            get { return _whiteReflectSce; }
            set
            {
                if (value == null || value.Length != Constants.VALID_WAVE_NUM)
                {
                    throw new ArgumentException();
                }

                _whiteReflectSce = value;
            }
        }

        /// <summary>
        /// 反射率修正系数，其他各种修正参数在这里增加
        /// </summary>
        public float[] ReflectModify
        {
            get { return _reflectModify; }
            set
            {
                if (value == null || value.Length != Constants.VALID_WAVE_NUM)
                {
                    throw new ArgumentException();
                }

                _reflectModify = value;
            }
        }

        /// <summary>
        /// 透镜从开始位置光耦到目标位置的步长
        /// </summary>
        public ushort[] LensPosStep
        {
            get { return _lensPosStep; }
            set
            {
                //if (value == null || value != _lensPosStep)
                //{
                //    throw new ArgumentException();
                //}

                _lensPosStep = value;
            }
        }

        /// <summary>
        /// sci/sce电机运行时间，
        /// </summary>
        public ushort ScMotorRunTime
        {
            get { return _scMotorRunTime; }
            set { _scMotorRunTime = value; }
        }

        /// <summary>
        /// 反射积分时间
        /// </summary>
        public uint ReflectIntegralTime
        {
            get { return _reflectIntegralTime; }
            set { _reflectIntegralTime = value; }
        }

        /// <summary>
        /// 透射积分时间
        /// </summary>
        public uint TransIntegralTime
        {
            get { return _transIntegralTime; }
            set { _transIntegralTime = value; }
        }

        /// <summary>
        /// 透镜开始位置到光耦结束位置的步长
        /// </summary>
        public ushort LensMotorAllStep
        {
            get { return _lensMotorAllStep; }

            set { _lensMotorAllStep = value; }
        }

        public string MajorSensorSn
        {
            get
            {
                return _majorSensorSn;
            }

            set
            {
                _majorSensorSn = value;
            }
        }

        public string MinorSensorSn
        {
            get
            {
                return _minorSensorSn;
            }

            set
            {
                _minorSensorSn = value;
            }
        }
        public string ModelName
        {
            get
            {
                return model_name;
            }

            set
            {
                model_name = value;
            }
        }

        public string Company
        {
            get
            {
                return company;
            }
            set
            {
                company = value;
            }
        }

        [MarshalAs(UnmanagedType.U4)]
        private UInt32 microPore;
        public UInt32 MicroPore
        {
            get => microPore;
            set => microPore = value;
        }

        public Spectrum GetWhiteboardSpectrum(bool isSCI)
        {
            var range = new WavelengthRange(Constants.VALID_WAVE_BEGIN, Constants.VALID_WAVE_END,
                        Constants.VALID_WAVE_STEP);
            return new Spectrum(range, Array.ConvertAll(isSCI ? WhiteReflectSci : WhiteReflectSce, x => (double)x));
        }

        /// <summary>
        /// 反射率除以100
        /// </summary>
        /// <param name="isSCI"></param>
        /// <returns></returns>
        public Spectrum GetWhiteboardSpectrum_1(bool isSCI)
        {
            var range = new WavelengthRange(Constants.VALID_WAVE_BEGIN, Constants.VALID_WAVE_END,
                        Constants.VALID_WAVE_STEP);
            return new Spectrum(range, Array.ConvertAll(isSCI ? WhiteReflectSci : WhiteReflectSce, x => (double)(x / 100.0)));
        }

        public static ProductQeuipInfo FromV1(ProductQeuipInfoV1 info)
        {
            return new ProductQeuipInfo
            {
                FlagUpdate = info.FlagUpdate,
                BlackPlate = info.BlackPlate,
                LensPosStep = info.LensPosStep,
                BlackReflect = info.BlackReflect,
                WhitePlate = info.WhitePlate,
                WhiteReflectSci = info.WhiteReflectSci,
                WhiteReflectSce = info.WhiteReflectSce,
                DefaultLanguage = info.DefaultLanguage,
                LogoAddr = info.LogoAddr,
                LogoLength = info.LogoLength,
                SoftwareVersion = info.SoftwareVersion,
                HardwareVersion = info.HardwareVersion,
                Sn = info.Sn,
                ReflectModify = info.ReflectModify,
                LensMotorAllStep = info.LensMotorAllStep,
                MajorSensorSn = info.MajorSensorSn,
                MinorSensorSn = info.MinorSensorSn,
                ScMotorRunTime = info.ScMotorRunTime
            };
        }

        public static ProductQeuipInfo FromV2(ProductQeuipInfoV2 info)
        {
            return new ProductQeuipInfo
            {
                FlagUpdate = info.FlagUpdate,
                BlackPlate = info.BlackPlate,
                LensPosStep = info.LensPosStep,
                BlackReflect = info.BlackReflect,
                WhitePlate = info.WhitePlate,
                WhiteReflectSci = info.WhiteReflectSci,
                WhiteReflectSce = info.WhiteReflectSce,
                DefaultLanguage = info.DefaultLanguage,
                LogoAddr = info.LogoAddr,
                LogoLength = info.LogoLength,
                SoftwareVersion = info.SoftwareVersion,
                HardwareVersion = info.HardwareVersion,
                Sn = info.Sn,
                ReflectModify = info.ReflectModify,
                LensMotorAllStep = info.LensMotorAllStep,
                MajorSensorSn = info.MajorSensorSn,
                MinorSensorSn = info.MinorSensorSn,
                WaveCoefMajor = info.WaveCoefMajor,
                WaveCoefMinor = info.WaveCoefMinor,
                Company = info.Company,
                ModelName = info.ModelName,
                ReflectIntegralTime = info.ReflectIntegralTime,
                ScMotorRunTime = info.ScMotorRunTime,
                TransIntegralTime = info.TransIntegralTime,
            };
        }
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 4)]
    public class ProductQeuipInfoV2
    {
        private ushort _flagUpdate;
        private uint _logoAddr;
        private uint _logoLength;
        private byte _defaultLanguage;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)]
        private string _softwareVersion;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)]
        private string _hardwareVersion;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)]
        private string _sn;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)]
        private string _blackPlate;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)]
        private string _whitePlate;


        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        private float[] _waveCoefMajor = new float[8];

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        private float[] _waveCoefMinor = new float[8];

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = Constants.VALID_WAVE_NUM)]
        private float[] _blackReflect = new float[Constants.VALID_WAVE_NUM];

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = (Constants.VALID_WAVE_NUM))]
        private float[] _whiteReflectSci = new float[Constants.VALID_WAVE_NUM];

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = (Constants.VALID_WAVE_NUM))]
        private float[] _whiteReflectSce = new float[Constants.VALID_WAVE_NUM];

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = Constants.VALID_WAVE_NUM)]
        private float[] _reflectModify = new float[Constants.VALID_WAVE_NUM];

        private ushort _lensMotorAllStep;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = (int)LensPos.Num)]
        private ushort[] _lensPosStep = new ushort[(int)LensPos.Num];

        private ushort _scMotorRunTime;
        private uint _reflectIntegralTime;
        private uint _transIntegralTime;


        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)]
        private string _majorSensorSn; // 主光谱感应器编号

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)]
        private string _minorSensorSn;  // 辅感应器编号

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)]
        private string model_name;                    // 仪器型号名

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 20)]
        private string company;                       // 公司名

        public ushort FlagUpdate
        {
            get { return _flagUpdate; }
            set { _flagUpdate = value; }
        }

        /// <summary>
        /// logo文件在产品配置信息文件中的偏移地址
        /// </summary>
        public uint LogoAddr
        {
            get { return _logoAddr; }
            set { _logoAddr = value; }
        }

        /// <summary>
        /// logo文件长度
        /// </summary>
        public uint LogoLength
        {
            get { return _logoLength; }
            set { _logoLength = value; }
        }

        /// <summary>
        /// 缺省语言
        /// </summary>
        public byte DefaultLanguage
        {
            get { return _defaultLanguage; }
            set { _defaultLanguage = value; }
        }

        /// <summary>
        /// 软件版本
        /// </summary>
        public string SoftwareVersion
        {
            get { return _softwareVersion; }
            set { _softwareVersion = value; }
        }

        /// <summary>
        /// 硬件版本
        /// </summary>
        public string HardwareVersion
        {
            get { return _hardwareVersion; }
            set { _hardwareVersion = value; }
        }

        /// <summary>
        /// 序列号
        /// </summary>
        public string Sn
        {
            get { return _sn; }
            set { _sn = value; }
        }

        /// <summary>
        /// 黑筒编号
        /// </summary>
        public string BlackPlate
        {
            get { return _blackPlate; }
            set { _blackPlate = value; }
        }

        /// <summary>
        /// 白板编号
        /// </summary>
        public string WhitePlate
        {
            get { return _whitePlate; }
            set { _whitePlate = value; }
        }

        /// <summary>
        /// 主CCD波长系数
        /// </summary>
        public float[] WaveCoefMajor
        {
            get { return _waveCoefMajor; }
            set
            {
                if (value == null || value.Length != 8)
                {
                    throw new ArgumentException();
                }

                _waveCoefMajor = value;
            }
        }

        /// <summary>
        /// 辅CCD波长系数
        /// </summary>
        public float[] WaveCoefMinor
        {
            get { return _waveCoefMinor; }
            set
            {
                if (value == null || value.Length != 8)
                {
                    throw new ArgumentException();
                }

                _waveCoefMinor = value;
            }
        }

        /// <summary>
        /// 黑筒反射率，测试10次，取平均值
        /// </summary>
        public float[] BlackReflect
        {
            get { return _blackReflect; }
            set
            {
                if (value == null || value.Length != Constants.VALID_WAVE_NUM)
                {
                    throw new ArgumentException();
                }

                _blackReflect = value;
            }
        }

        /// <summary>
        /// 白板反射率(SCI)，用精度更高的仪器标定产生
        /// </summary>
        public float[] WhiteReflectSci
        {
            get { return _whiteReflectSci; }
            set
            {
                if (value == null || value.Length != Constants.VALID_WAVE_NUM)
                {
                    throw new ArgumentException();
                }

                _whiteReflectSci = value;
            }
        }

        /// <summary>
        /// 白板反射率(SCE)，用精度更高的仪器标定产生
        /// </summary>
        public float[] WhiteReflectSce
        {
            get { return _whiteReflectSce; }
            set
            {
                if (value == null || value.Length != Constants.VALID_WAVE_NUM)
                {
                    throw new ArgumentException();
                }

                _whiteReflectSce = value;
            }
        }

        /// <summary>
        /// 反射率修正系数，其他各种修正参数在这里增加
        /// </summary>
        public float[] ReflectModify
        {
            get { return _reflectModify; }
            set
            {
                if (value == null || value.Length != Constants.VALID_WAVE_NUM)
                {
                    throw new ArgumentException();
                }

                _reflectModify = value;
            }
        }

        /// <summary>
        /// 透镜从开始位置光耦到目标位置的步长
        /// </summary>
        public ushort[] LensPosStep
        {
            get { return _lensPosStep; }
            set
            {
                //if (value == null || value != _lensPosStep)
                //{
                //    throw new ArgumentException();
                //}

                _lensPosStep = value;
            }
        }

        /// <summary>
        /// sci/sce电机运行时间，
        /// </summary>
        public ushort ScMotorRunTime
        {
            get { return _scMotorRunTime; }
            set { _scMotorRunTime = value; }
        }

        /// <summary>
        /// 反射积分时间
        /// </summary>
        public uint ReflectIntegralTime
        {
            get { return _reflectIntegralTime; }
            set { _reflectIntegralTime = value; }
        }

        /// <summary>
        /// 透射积分时间
        /// </summary>
        public uint TransIntegralTime
        {
            get { return _transIntegralTime; }
            set { _transIntegralTime = value; }
        }

        /// <summary>
        /// 透镜开始位置到光耦结束位置的步长
        /// </summary>
        public ushort LensMotorAllStep
        {
            get { return _lensMotorAllStep; }

            set { _lensMotorAllStep = value; }
        }

        public string MajorSensorSn
        {
            get
            {
                return _majorSensorSn;
            }

            set
            {
                _majorSensorSn = value;
            }
        }

        public string MinorSensorSn
        {
            get
            {
                return _minorSensorSn;
            }

            set
            {
                _minorSensorSn = value;
            }
        }

        public string ModelName
        {
            get
            {
                return model_name;
            }

            set
            {
                model_name = value;
            }
        }

        public string Company
        {
            get
            {
                return company;
            }

            set
            {
                company = value;
            }
        }
    }

    /// <summary>
    /// RTC时间
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct RtcDateTime
    {
        public ushort year;
        public byte month;
        public byte date;
        public byte hour;
        public byte minute;
        public byte second;
        public byte reserved;

        public DateTime ToDateTime()
        {
            try
            {
                return new DateTime(year, month, date, hour, minute, second);
            }
            catch
            {
                return DateTime.Now;
            }
        }

        public RtcDateTime(DateTime dateTime)
        {
            year = (ushort)dateTime.Year;
            month = (byte)dateTime.Month;
            date = (byte)dateTime.Day;
            hour = (byte)dateTime.Hour;
            minute = (byte)dateTime.Minute;
            second = (byte)dateTime.Second;
            reserved = 0;
        }
    }

    /// <summary>
    /// 设备信息
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
    public struct DeviceInfo
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)]
        string model; // 仪器型号

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)]
        string optical; // 光学结构

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)]
        string sn; // 序列号

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)]
        string software_version; // 软件版本号

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)]
        string hardware_version; // 硬件版本号

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)]
        string white_board_number; // 白板编号

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)]
        string black_bucket_number; // 黑桶编号

        ushort standard_capacity; // 标样容量
        ushort trial_capacity; // 试样容量
        //ushort standard_count; // 已存标样数
        //ushort trial_count; // 已存试样数
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        private byte[] reserved;

        public string Model
        {
            get { return model; }

            set { model = value; }
        }

        public string Optical
        {
            get { return optical; }

            set { optical = value; }
        }

        public string Sn
        {
            get { return sn; }

            set { sn = value; }
        }

        public string SoftwareVersion
        {
            get { return software_version; }

            set { software_version = value; }
        }

        public string HardwareVersion
        {
            get { return hardware_version; }

            set { hardware_version = value; }
        }

        public string WhiteBoardNumber
        {
            get { return white_board_number; }

            set { white_board_number = value; }
        }

        public string BlackBucketNumber
        {
            get { return black_bucket_number; }

            set { black_bucket_number = value; }
        }

        public ushort StandardCapacity
        {
            get { return standard_capacity; }

            set { standard_capacity = value; }
        }

        public ushort TrialCapacity
        {
            get { return trial_capacity; }

            set { trial_capacity = value; }
        }
    };

    [StructLayout(LayoutKind.Sequential)]
    public struct InstrumentStatus
    {
        byte caliber;           // 口径
        byte lens_pos;          // 透镜位置
        byte transmission;      // 是否是透射
        byte sc_mode;           // 镜面反射光测量方式
        byte uv_mode;           // UV模式
        byte observer;          // 观察者角度
        byte light_type;        // 当前光源设置
        byte color_space;       // 颜色空间
        byte color_diff_formula; // 色差公式
        byte color_index;        // 颜色指数
        private ushort standard_count;  // 标样数
        private ushort trial_count; // 试样数
        private byte is_calibrated; // 是否已校正
        private byte lens_switch_mode;  // 透镜切换方式
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        byte[] reserved;       // 保留以

        public MeasurementCaliber Caliber
        {
            get
            {
                return ((Caliber)caliber).Uniform();
            }

            set
            {
                caliber = (byte)value.ToInstrumentForm();
            }
        }

        public LensPosition LensPos
        {
            get
            {
                return ((LensPos)lens_pos).Uniform();
            }

            set
            {
                lens_pos = (byte)value.ToInstrumentForm();
            }
        }

        public bool IsTransmission
        {
            get
            {
                return transmission != 0;
            }

            set
            {
                transmission = (byte)(value ? 1 : 0);
            }
        }

        public SpecularComponent ScMode
        {
            get
            {
                return ((ScMode)sc_mode).Uniform();
            }

            set
            {
                sc_mode = (byte)value.ToInstrumentForm();
            }
        }

        public UVFlag UvMode
        {
            get
            {
                return ((UvMode)uv_mode).Uniform();
            }

            set
            {
                uv_mode = (byte)value.ToInstrumentForm();
            }
        }

        public StandardObserver Observer
        {
            get
            {
                return ((Observer)observer).Uniform();
            }

            set
            {
                observer = (byte)value.ToInstrumentForm();
            }
        }

        public StandardIlluminant Illuminant
        {
            get
            {
                return ((Illuminant)light_type).Uniform();
            }

            set
            {
                light_type = (byte)value.ToInstrumentForm();
            }
        }

        public Optical.ColorSpace ColorSpace
        {
            get
            {
                return ((ColorSpace)color_space).Uniform();
            }

            set
            {
                color_space = (byte)value.ToInstrumentForm();
            }
        }

        public Optical.ColorDiffFormula ColorDiffFormula
        {
            get
            {
                return ((ColorDeltaFormula)color_diff_formula).Uniform();
            }

            set
            {
                color_diff_formula = (byte)value.ToInstrumentForm();
            }
        }

        public Optical.ColorIndex ColorIndex
        {
            get
            {
                return ((ColorIndex)color_index).Uniform();
            }

            set
            {
                color_index = (byte)value.ToInstrumentForm();
            }
        }

        public ushort StandardCount
        {
            get { return standard_count; }
            set { standard_count = value; }
        }

        public ushort TrialCount
        {
            get { return trial_count; }
            set { trial_count = value; }
        }

        public bool IsCalibrated
        {
            get { return is_calibrated != 0; }
            set { is_calibrated = (byte)(value ? 1 : 0); }
        }

        public LensSwitchMode LensSwitchMode
        {
            get { return ((LensMode)lens_switch_mode).Uniform(); }
            set { lens_switch_mode = (byte)value.ToInstrumentForm(); }
        }
    };

    /// <summary>
    /// 白板参数
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct WhiteBoardParameter
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)]
        private string _sn;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = Constants.VALID_WAVE_NUM)]
        private float[] _sci;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = Constants.VALID_WAVE_NUM)]
        private float[] _sce;

        public string Sn
        {
            get
            {
                return _sn;
            }

            set
            {
                if (value?.Length >= 16)
                {
                    throw new ArgumentException("Length of value must be less than 16");
                }
                _sn = value ?? string.Empty;
            }
        }

        public float[] Sci
        {
            get
            {
                return _sci ?? (_sci = new float[Constants.VALID_WAVE_NUM]);
            }

            set
            {
                if (value?.Length != Constants.VALID_WAVE_NUM)
                {
                    throw new ArgumentException(string.Format("Length of value must be is {0}", Constants.VALID_WAVE_NUM));
                }
                _sci = value;
            }
        }

        public float[] Sce
        {
            get
            {
                return _sce ?? (_sce = new float[Constants.VALID_WAVE_NUM]);
            }

            set
            {
                if (value?.Length != Constants.VALID_WAVE_NUM)
                {
                    throw new ArgumentException(string.Format("Length of value must be is {0}", Constants.VALID_WAVE_NUM));
                }
                _sce = value;
            }
        }
    }

    /// <summary>
    /// 光栅感应器波长系数
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    struct InnerGratingWavelengthCoefficients
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)]
        private string _majorSn;    // 主感应器编号

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)]
        private string _minjorSn;   // 参考感应器编号

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        private float[] _majorCoef;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        private float[] _minorCoef;

        public string MajorSn
        {
            get
            {
                return _majorSn;
            }

            set
            {
                if (value.Length >= 16)
                {
                    throw new ArgumentException("Length of the value must be less than 16");
                }
                _majorSn = value;
            }
        }

        public string MinjorSn
        {
            get
            {
                return _minjorSn;
            }

            set
            {
                if (value.Length >= 16)
                {
                    throw new ArgumentException("Length of the value must be less than 16");
                }
                _minjorSn = value;
            }
        }

        public float[] MajorCoef
        {
            get
            {
                return _majorCoef ?? (_majorCoef = new float[8]);
            }

            set
            {
                if (value?.Length != 8)
                {
                    throw new ArgumentException("Length of the value must be less than 8");
                }
                _majorCoef = value;
            }
        }

        public float[] MinorCoef
        {
            get { return _minorCoef ?? (_minorCoef = new float[8]); }

            set
            {
                if (value?.Length != 8)
                {
                    throw new ArgumentException("Length of the value must be less than 8");
                }
                _minorCoef = value;
            }
        }
    }

    public class GratingWavelengthCoefficients
    {
        private float[] _mainSensorCoefficients = new float[6];
        private float[] _refSensorCoefficients = new float[6];

        public GratingWavelengthCoefficients()
        {

        }

        internal GratingWavelengthCoefficients(InnerGratingWavelengthCoefficients inner)
        {
            MainSensorSn = inner.MajorSn;
            RefSensorSn = inner.MinjorSn;

        }

        internal InnerGratingWavelengthCoefficients ToGratingWavelengthCoef()
        {
            InnerGratingWavelengthCoefficients tmp = new InnerGratingWavelengthCoefficients
            {
                MajorSn = MainSensorSn,
                MinjorSn = RefSensorSn,
                MajorCoef = new float[8],
                MinorCoef = new float[8]
            };

            for (int i = 0; i < 6; i++)
            {
                tmp.MajorCoef[i] = MainSensorCoefficients[i];
                tmp.MinorCoef[i] = RefSensorCoefficients[i];
            }
            return tmp;
        }

        public string MainSensorSn { get; set; }
        public string RefSensorSn { get; set; }

        public float[] MainSensorCoefficients
        {
            get { return _mainSensorCoefficients; }
            set
            {
                if (value == null)
                {
                    for (int i = 0; i < _mainSensorCoefficients.Length; i++)
                    {
                        _mainSensorCoefficients[i] = 0.0f;
                    }
                }
                else if (value.Length == _mainSensorCoefficients.Length)
                {
                    for (int i = 0; i < _mainSensorCoefficients.Length; i++)
                    {
                        _mainSensorCoefficients[i] = value[i];
                    }
                }
                else
                {
                    throw new ArgumentException($"The length must be is {_mainSensorCoefficients.Length}");
                }
            }
        }

        public float[] RefSensorCoefficients
        {
            get { return _refSensorCoefficients; }
            set
            {
                if (value == null)
                {
                    for (int i = 0; i < _refSensorCoefficients.Length; i++)
                    {
                        _refSensorCoefficients[i] = 0.0f;
                    }
                }
                else if (value.Length == _refSensorCoefficients.Length)
                {
                    for (int i = 0; i < _refSensorCoefficients.Length; i++)
                    {
                        _refSensorCoefficients[i] = value[i];
                    }
                }
                else
                {
                    throw new ArgumentException($"The length must be is {_refSensorCoefficients.Length}");
                }
            }
        }
    }

    /// <summary>
    /// 台间差修正设置，修正算法参考“YS60算法修正原理”
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public class CorrectionFactors_t
    {
        public UInt32 valid_flag;  // 有效标记，为1时有效，否则修正设置不全生效
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = Constants.VALID_WAVE_NUM)]
        public float[] r1_factors; // R1 修正系数，大小为43的数组
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
        public float[] r2_poly;   // K_Poly为5次多项式=[A5,A4,A3,A2,A1,A0]，默认设为 0, 0, 0, 0, 1, 0
        public float r2_range_lower; // r2 修正范围下限，默认为0
        public float r2_range_upper; // r2 修正范围上限，默认为2
        public float r3_k;         // 三角修正K因子，默认为0

        public CorrectionFactors_t()
        {
            valid_flag = 1;
            r1_factors = new float[Constants.VALID_WAVE_NUM];
            for (int i = 0; i < Constants.VALID_WAVE_NUM; i++)
            {
                r1_factors[i] = 1.0f;
            }
            r2_poly = new float[6] {0, 0, 0, 0, 1, 0 };
            r2_range_lower = 0.0f;
            r2_range_upper = 2.0f;
            r3_k = 0.0f;
        }
    }
}