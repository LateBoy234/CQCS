using System;
using System.Runtime.Serialization;

namespace ThreeNH.ColorAlgorithm
{
    [DataContract]
    public class CIELABCS
    {
        public CIELABCS() { }

        public CIELABCS(double L, double a, double b)
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
        public double C
        {
            get { return Math.Sqrt(a * a + b * b); }
        }
        public double h
        {
            get
            {
                double th = Math.Atan2(b, a).ToDegrees();
                return th < 0 ? th + 360.0 : th;
            }
        }
        public CIELCh ToLCh()
        {
            return new CIELCh(L, C, h);
        }

        public static DeltaLab operator-(CIELABCS trial, CIELABCS standard)
        {
            return new DeltaLab(trial, standard);
        }
    }

    [DataContract]
    public  class CIELCh
    {
    public CIELCh() { }
        public CIELCh(double L, double C, double h)
        {
            this.L = L;
            this.C = C;
            this.h = h;
        }
        [DataMember]
        public double L { get; set; }
        [DataMember]
        public double C { get; set; }
        [DataMember]
        public double h { get; set; }

        public double a
        {
            get
            {
                return C * Math.Cos(h.ToRadians());
            }
        }
        public double b
        {
            get
            {
                return C * Math.Sin(h.ToRadians());
            }
        }

        public CIELABCS ToLab()
        {
            return new CIELABCS(L, a, b);
        }

        public static DeltaLab operator -(CIELCh trial, CIELCh standard)
        {
            return new DeltaLab(trial, standard);
        }
    }

    public class CIEDE1976
    {
        public CIEDE1976(CIELABCS trial, CIELABCS standard)
        {
            dL = trial.L - standard.L;
            da = trial.a - standard.a;
            db = trial.b - standard.b;
            dC = trial.C - standard.C;
            dh = trial.h - standard.h;
            if (dh > 180.0)
                dh -= 360.0;
            else if (dh < -180.0)
                dh += 360.0;
            dH = 2.0 * Math.Sqrt(trial.C * standard.C)
                    * Math.Sin((dh / 2.0).ToRadians());
        }
        public CIEDE1976(CIELCh trial, CIELCh standard)
            : this(trial.ToLab(), standard.ToLab())
        {

        }

        public double dL { get; private set; }
        public double da { get; private set; }
        public double db { get; private set; }
        public double dC { get; private set; }
        public double dh { get; private set; }
        public double dH { get; private set; }
        public double dE
        {
            get
            {
                return Math.Sqrt(dL * dL + dC * dC + dH * dH);
            }
        }
    };

    public abstract class BetterLabDE
    {
        protected double _Sl = 1.0;
        protected double _Sc = 1.0;
        protected double _Sh = 1.0;
        protected double _Kl = 1.0;
        protected double _Kc = 1.0;
        protected double _Kh = 1.0;
        protected CIEDE1976 _dE1976 = null;

        protected BetterLabDE(CIELABCS trial, CIELABCS standard, double KL=1.0, double KC=1.0, double KH=1.0)
        {
            _dE1976 = new CIEDE1976(trial, standard);
            _Kl = KL;
            _Kc = KC;
            _Kh = KH;
        }
        protected BetterLabDE(CIEDE1976 dE1976, double KL = 1.0, double KC = 1.0, double KH = 1.0)
        {
            _dE1976 = dE1976;
            _Kl = KL;
            _Kc = KC;
            _Kh = KH;
        }

        public virtual double dE
        {
            get
            {
                return Math.Sqrt(dL * dL + dC * dC + dH * dH);
            }
        }

        public double dL
        {
            get
            {
                return _dE1976.dL / (Kl * _Sl);
            }
        }
        public double dC
        {
            get
            {
                return _dE1976.dC / (Kc * _Sc);
            }
        }
        public double dH
        {
            get
            {
                return _dE1976.dH / (Kh * _Sh);
            }
        }
        public double Kl
        {
            get { return _Kl; }
            set { _Kl = value; }
        }
        public double Kc
        {
            get { return _Kc; }
            set { _Kc = value; }
        }
        public virtual double Kh
        {
            get { return _Kh; }
            set { _Kh = value; }
        }
    }

    public class CIEDE1994 : BetterLabDE
    {
        public CIEDE1994(CIELABCS trial, CIELABCS standard, double KL = 1.0, double KC = 1.0, double KH = 1.0)
                : this(trial, standard, new CIEDE1976(trial, standard), KL, KC, KH) { }

        public CIEDE1994(CIELABCS trial, CIELABCS standard, CIEDE1976 dE1976, double KL=1.0, double KC=1.0, double KH=1.0)
                : base(dE1976, KL, KC, KH)
        {
            _Sl = 1.0;
            _Sc = 1.0 + 0.045 * standard.C;
            _Sh = 1.0 + 0.015 * standard.C;
        }

        public CIEDE1994(CIELCh trial, CIELCh standard)
            : this(trial.ToLab(), standard.ToLab())
        {

        }
    }

    public class CMCDE : BetterLabDE
    {
        public CMCDE(CIELABCS trial, CIELABCS standard, double KL = 1.0, double KC = 1.0, double KH = 1.0)
                : this(trial, standard, new CIEDE1976(trial, standard), KL, KC, KH) { }
        public CMCDE(CIELCh trial, CIELCh standard, double KL = 1.0, double KC = 1.0, double KH = 1.0)
            : this(trial.ToLab(), standard.ToLab(), KL, KC, KH)
        {

        }
        public CMCDE(CIELABCS trial, CIELABCS standard, CIEDE1976 dE1976, double KL = 1.0, double KC = 1.0, double KH = 1.0)
            : base(dE1976, KL, KC, KH)
        {
            _Sl = (standard.L < 16.0) ?
                0.511 : (0.040975 * standard.L / (1.0 + 0.01765 * standard.L));
            _Sc = 0.0638 * standard.C / (1.0 + 0.0131 * standard.C) + 0.638;
            double f = Math.Sqrt(Math.Pow(standard.C, 4) / (Math.Pow(standard.C, 4) + 1900.0));
            double T = (164.0 < standard.h && standard.h < 345.0) ?
                        (0.56 + Math.Abs(0.2 * Math.Cos((standard.h + 168.0).ToRadians()))) :
                        (0.36 + Math.Abs(0.4 * Math.Cos((standard.h + 35.0).ToRadians())));
            _Sh = _Sc * (T * f + 1.0 - f);
        }

        // CMC 不能设置Kh的值
        public override double Kh
        {
            get { return base.Kh; }
            set { _Kh = 1.0; }
        }
    };

    public class CIEDE2000 : BetterLabDE
    {
        double _Rt = 1.0;

        public CIEDE2000(CIELABCS trial, CIELABCS standard, double KL = 1.0, double KC = 1.0, double KH = 1.0)
            : this(trial, standard, new CIEDE1976(trial, standard), KL, KC, KH) { }
        public CIEDE2000(CIELCh trial, CIELCh standard, double KL = 1.0, double KC = 1.0, double KH = 1.0)
            : this(trial.ToLab(), standard.ToLab(), KL, KC, KH)
        {

        }

        public CIEDE2000(CIELABCS trial, CIELABCS standard, CIEDE1976 dE1976, double KL = 1.0, double KC = 1.0, double KH = 1.0)
            : base(dE1976, KL, KC, KH)
        {
            double CC = (trial.C + standard.C) / 2.0;
            double G = 0.5 * (1.0 - Math.Sqrt(Math.Pow(CC, 7) /
                                              (Math.Pow(CC, 7) + Math.Pow(25, 7))));

            CIELABCS  lab1 = new CIELABCS(trial.L, trial.a * (1.0 + G), trial.b);
            CIELABCS  lab0 = new CIELABCS(standard.L, standard.a * (1.0 + G), standard.b);
            _dE1976 = new CIEDE1976(lab1, lab0);  // 用 L',a',b' 代替原来的Lab

            double mL = (lab1.L + lab0.L) / 2.0;
            double mC = (lab1.C + lab0.C) / 2.0;
            double mh = Math.Abs(lab1.h - lab0.h) > 180 ?
                        (lab1.h + lab0.h + 360) / 2.0 : (lab1.h + lab0.h) / 2.0;

            _Sl = 1.0 + 0.015 * Math.Pow(mL - 50.0, 2) / Math.Sqrt(20.0 + Math.Pow(mL - 50.0, 2));
            _Sc = 1.0 + 0.045 * mC;
            double T = 1.0 - 0.17 * Math.Cos((mh - 30.0).ToRadians()) + 0.24 * Math.Cos((2.0 * mh).ToRadians())
                    + 0.32 * Math.Cos((3.0 * mh + 6.0).ToRadians()) - 0.20 * Math.Cos((4.0 * mh - 63.0).ToRadians());
            _Sh = 1.0 + 0.015 * mC * T;

            double Rc = 2.0 * Math.Sqrt(Math.Pow(mC, 7) / (Math.Pow(mC, 7) + Math.Pow(25.0, 7)));
            double dTheta = 30.0 * Math.Exp(-Math.Pow((mh - 275.0) / 25.0, 2));
            _Rt = -Math.Sin((2.0 * dTheta).ToRadians()) * Rc;
        }

        public override double dE
        {
            get
            {
                return Math.Sqrt(dL * dL + dC * dC + dH * dH +
                    _Rt * dC * dH);
            }
        }
    }



    // 标样减试样的色差值
    public class DeltaLab
    {
        public DeltaLab(CIELABCS trial, CIELABCS standard)
        {
            dE1976 = new CIEDE1976(trial, standard);
            dE00 = new CIEDE2000(trial, standard, dE1976);
            dE94 = new CIEDE1994(trial, standard, dE1976);
            dEcmc = new CMCDE(trial, standard, dE1976);
        }
        public DeltaLab(CIELCh trial, CIELCh standard)
            : this(trial.ToLab(), standard.ToLab())
        {

        }

        public double dL { get { return dE1976.dL; } }
        public double da { get { return dE1976.da; } }
        public double db { get { return dE1976.db; } }
        public double dC { get { return dE1976.dC; } }
        public double dh { get { return dE1976.dh; } }
        public double dH { get { return dE1976.dH; } }
        public double dE { get { return dE1976.dE; } }
        public CIEDE1976 dE1976 { get; private set;}
        public CIEDE2000 dE00 { get; private set; }
        public CIEDE1994 dE94 { get; private set; }
        public CMCDE dEcmc { get; private set; }
    }
}
