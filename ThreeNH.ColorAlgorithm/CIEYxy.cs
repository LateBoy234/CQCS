using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace ThreeNH.ColorAlgorithm
{
    [DataContract]
    public class CIEYxy
    {
        public CIEYxy() { }
        public CIEYxy(float Y, float x, float y)
        {
            this.Y = Y;
            this.x = x;
            this.y = y;
        }

        [DataMember]
        public float Y { get; set; }
        [DataMember]
        public float x { get; set; }
        [DataMember]
        public float y { get; set; }
        public float X
        {
            get
            {
                if (y != 0 && Y != 0)
                    return Y / y * x;
                return 0.0f;
            }
        }
        public float Z
        {
            get
            {
                if (y != 0 && Y != 0)
                    return Y / y * z;
                return 0.0f;
            }
        }
        public float z
        {
            get
            {
                return 1.0f - x - y;
            }
        }

        public CIEXYZ XYZ
        {
            get
            {
                return new CIEXYZ(X, Y, Z);
            }
        }

        public static DeltaXYZ operator -(CIEYxy trial, CIEYxy standard)
        {
            return new DeltaXYZ(trial, standard);
        }

        public static explicit operator CIEXYZ(CIEYxy Yxy)
        {
            return Yxy.XYZ;
        }
    }
}
