using System;
using System.Runtime.Serialization;

namespace ThreeNH.ColorAlgorithm
{
    [DataContract]
    public class CIELUV1976
    {
        public CIELUV1976() { }
        public CIELUV1976(float L, float u, float v)
        {
            this.L = L;
            this.u = u;
            this.v = v;
        }

        [DataMember]
        public float L { get; set; }
        [DataMember]
        public float u { get; set; }
        [DataMember]
        public float v { get; set; }
        public float s
        {
            get
            {
                return (L != 0.0) ? (float)(C / L) : 0.0f;
            }
        }
        public float C
        {
            get { return (float)Math.Sqrt(u * u + v * v); }
        }
        public float h
        {
            get
            {
                float t = (float)Math.Atan2(v, u).ToDegrees();
                if (t < 0)
                    t += 360.0f;
                return t;
            }
        }


        public static DeltaLuv operator -(CIELUV1976 trial, CIELUV1976 standard)
        {
            return new DeltaLuv(trial, standard);
        }
    }

    public class DeltaLuv
    {
        public DeltaLuv(CIELUV1976 trial, CIELUV1976 standard)
        {
            dL = trial.L - standard.L;
            du = trial.u - standard.u;
            dv = trial.v - standard.v;
            dC = trial.C - standard.C;
            dh = trial.h - standard.h;
            if (dh > 180.0)
                dh -= 360.0f;
            else if (dh < -180.0)
                dh += 360.0f;
            dH = 2.0f * (float)Math.Sqrt(trial.C * standard.C)
                    * (float)Math.Sin((dh / 2.0f).ToRadians());
            dE = (float)Math.Sqrt(dL * dL + du * du + dv * dv);
        }

        public float dL { get; private set; }
        public float du { get; private set; }
        public float dv { get; private set; }
        public float dC { get; private set; }
        public float dh { get; private set; }
        public float dH { get; private set; }
        public float dE { get; private set; }
    }
}
