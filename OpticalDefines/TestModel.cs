using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThreeNH.Optical
{
    public class TestModel
    {
        public MeasurementMode StdMeasurementMode { get; set; }
        public MeasurementMode SampMeasurementMode { get; set; }

        public int StdRepeatTimes { get; set; }
        public int SampRepeatTimes { get; set; }

        public double StdDelayTimeMs { get; set; } = 1;
        public double  SampDelayTimeMs { get; set; } = 1;

    }
    public enum MeasurementMode
    {
        SingleMeasure,
        ContinuousMeasure
    }
}
