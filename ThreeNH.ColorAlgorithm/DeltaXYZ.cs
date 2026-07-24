using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ThreeNH.ColorAlgorithm
{
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

        public float dX { get; private set; }
        public float dY { get; private set; }
        public float dZ { get; private set; }
        public float dx { get; private set; }
        public float dy { get; private set; }
        public float dz { get; private set; }
    }
}
