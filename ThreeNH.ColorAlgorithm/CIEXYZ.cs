using System.Runtime.Serialization;

namespace ThreeNH.ColorAlgorithm
{
    [DataContract]
    public class CIEXYZ
    {
        public CIEXYZ() { }
        public CIEXYZ(double X, double Y, double Z)
        {
            this.X = X;
            this.Y = Y;
            this.Z = Z;
        }
        [DataMember]
        public double X { get; set; }
        [DataMember]
        public double Y { get; set; }
        [DataMember]
        public double Z { get; set; }
        public double x
        {
            get
            {
                double tmp = X + Y + Z;
                return tmp != 0.0 ? X / tmp : 0.0;
            }
        }
        public double y
        {
            get
            {
                double tmp = X + Y + Z;
                return tmp != 0.0 ? Y / tmp : 0.0;
            }
        }
        public double z
        {
            get
            {
                double tmp = X + Y + Z;
                return tmp != 0.0 ? Z / tmp : 0.0;
            }
        }

        public CIEYxy Yxy
        {
            get { return new CIEYxy(Y, x, y); }
            set
            {
                X = value.X;
                Y = value.y;
                Z = value.z;
            }
        }

        public static DeltaXYZ operator -(CIEXYZ trial, CIEXYZ standard)
        {
            return new DeltaXYZ(trial, standard);
        }

        public static explicit operator CIEYxy(CIEXYZ XYZ)
        {
            return XYZ.Yxy;
        }
    }

    [DataContract]
    public class CIEYxy
    {
        public CIEYxy() { }
        public CIEYxy(double Y, double x, double y)
        {
            this.Y = Y;
            this.x = x;
            this.y = y;
        }

        [DataMember]
        public double Y { get; set; }
        [DataMember]
        public double x { get; set; }
        [DataMember]
        public double y { get; set; }
        public double X
        {
            get
            {
                if (y != 0 && Y != 0)
                    return Y / y * x;
                return 0.0;
            }
        }
        public double Z
        {
            get
            {
                if (y != 0 && Y != 0)
                    return Y / y * z;
                return 0.0;
            }
        }
        public double z
        {
            get
            {
                return 1.0 - x - y;
            }
        }

        public CIEXYZ XYZ
        {
            get
            {
                return new CIEXYZ(X, Y, Z);
            }
        }

        public static DeltaXYZ operator-(CIEYxy trial, CIEYxy standard)
        {
            return new DeltaXYZ(trial, standard);
        }

        public static explicit operator CIEXYZ(CIEYxy Yxy)
        {
            return Yxy.XYZ;
        }
    }

    public class DeltaXYZ
    {
        public DeltaXYZ(CIEXYZ trial, CIEXYZ standard)
        {
            dX = trial.X - standard.X;
            dY = trial.Y - standard.Y;
            dZ = trial.Z - standard.Z;
            dx = trial.x - standard.x;
            dy = trial.y - standard.y;
            dz = trial.z - standard.z;
        }
        public DeltaXYZ(CIEYxy trial, CIEYxy standard)
        {
            dX = trial.X - standard.X;
            dY = trial.Y - standard.Y;
            dZ = trial.Z - standard.Z;
            dx = trial.x - standard.x;
            dy = trial.y - standard.y;
            dz = trial.z - standard.z;
        }

        public double dX { get; private set; }
        public double dY { get; private set; }
        public double dZ { get; private set; }
        public double dx { get; private set; }
        public double dy { get; private set; }
        public double dz { get; private set; }
    }
}
