﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace ThreeNH.SQCX.Infrastructure.Instruments.YL30.Legacy.Model
{
    public class SystemSetting
    {
        public Language Language { get; set; }
        public Buzzer BuzzerStatus { get; set; }
        public ushort BackLightTime { get; set; }
        public ushort BackLightBrightness { get; set;}
        public StandardIlluminant    Illuminant { get; set;}
        public StandardObserver Observer { get; set; }
        public ColorSpace ColorSpace { get; set; }
        public ColorDiffFormula DiffFormula { get; set; }

        public List<float> ReflectanceCorrectionFactorSCI { get; set; }=new List<float>();
        public List<float> ReflectanceCorrectionFactorSCE { get; set; } = new List<float>();
        public Tolerance SystemTolerance { get; set; }
        public Tolerance StandardTolerance { get; set; }
        public ushort ExpireTime { get; set; }
        public System.DateTime SystemDate { get; set;}
        public MeasureControlMode MeasureControlMode { get; set; }
        public CalibrationMode CalibrationMode { get; set; }
        public IPAddress IPAddress { get; set; }
        public ushort Port { get; set; }
        public IPAddress ClientIPAddress { get; set; }
        public ushort StdAverageCount { get; set; }
        public ushort SampleAverageCount { get; set; }
        public ShowOKNG ShowOKNG { get; set; }
    }
}
