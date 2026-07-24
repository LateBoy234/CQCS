using System;

namespace ThreeNH.SQCX.Domain.StandardSelection
{
    public sealed class DifferenceLimit
    {
        private DifferenceLimit(bool enabled, double maximum)
        {
            if (enabled && (double.IsNaN(maximum) || maximum < 0.0))
                throw new ArgumentOutOfRangeException(nameof(maximum));
            Enabled = enabled;
            Maximum = maximum;
        }

        public bool Enabled { get; private set; }
        public double Maximum { get; private set; }

        public static DifferenceLimit Disabled()
        {
            return new DifferenceLimit(false, 0.0);
        }

        public static DifferenceLimit UpTo(double maximum)
        {
            return new DifferenceLimit(true, maximum);
        }
    }
}
