using System;
using System.Runtime.Serialization;

namespace ThreeNH.ColorAlgorithm
{
    [DataContract]
    public class HunterLab
    {
        public HunterLab() { }
        public HunterLab(double L, double a, double b)
        {
            this.L = L;
            this.a = a;
            this.b = b;
        }

        [DataMember]
        public double L { get; set; }
        [DataMember]
        public double a { get; set; }
        [DataMember]
        public double b { get; set; }

        public static DeltaHunterLab operator-(HunterLab trial, HunterLab standard)
        {
            return new DeltaHunterLab(trial, standard);
        }
    }

    public class DeltaHunterLab
    {
        public DeltaHunterLab(HunterLab trial, HunterLab standard)
        {
            dL = trial.L - standard.L;
            da = trial.a - standard.a;
            db = trial.b - standard.b;
            dE = Math.Sqrt(dL * dL + da * da + db * db);
        }

        public double dL { get; private set; }
        public double da { get; private set; }
        public double db { get; private set; }
        public double dE { get; private set; }
    }
}
