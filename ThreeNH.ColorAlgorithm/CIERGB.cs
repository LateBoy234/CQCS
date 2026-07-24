using System.Runtime.Serialization;

namespace ThreeNH.ColorAlgorithm
{
    [DataContract]
    public class CIERGB
    {
        public CIERGB(double R, double G, double B)
        {
            this.R = R;
            this.G = G;
            this.B = B;
        }
        [DataMember]
        public double R
        {
            get; set;
        }
        [DataMember]
        public double G
        {
            get; set;
        }
        [DataMember]
        public double B
        {
            get; set;
        }
        public double r
        {
            get
            {
                if (R + G + B != 0.0)
                    return R / (R + G + B);
                return 0.0;
            }
        }
        public double g
        {
            get
            {
                if (R + G + B != 0.0)
                    return G / (R + G + B);
                return 0.0;
            }
        }
        public double b
        {
            get
            {
                if (R + G + B != 0.0)
                    return B / (R + G + B);
                return 0.0;
            }
        }

        public static DeltaRGB operator -(CIERGB trial, CIERGB standard)
        {
            return new DeltaRGB(trial, standard);
        }
    }

    public class DeltaRGB
    {
        public DeltaRGB(CIERGB trial, CIERGB standard)
        {
            dR = trial.R - standard.R;
            dG = trial.G - standard.G;
            dB = trial.B - standard.B;
            dr = trial.r - standard.r;
            dg = trial.g - standard.g;
            db = trial.b - standard.b;
        }

        public double dR { get; private set; }
        public double dG { get; private set; }
        public double dB { get; private set; }
        public double dr { get; private set; }
        public double dg { get; private set; }
        public double db { get; private set; }
    }
}
