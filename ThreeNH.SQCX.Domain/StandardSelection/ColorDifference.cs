namespace ThreeNH.SQCX.Domain.StandardSelection
{
    public sealed class ColorDifference
    {
        public ColorDifference(double deltaL, double deltaA, double deltaB, double deltaE)
        {
            DeltaL = System.Math.Abs(deltaL);
            DeltaA = System.Math.Abs(deltaA);
            DeltaB = System.Math.Abs(deltaB);
            DeltaE = System.Math.Abs(deltaE);
        }

        public double DeltaL { get; private set; }
        public double DeltaA { get; private set; }
        public double DeltaB { get; private set; }
        public double DeltaE { get; private set; }
    }
}
