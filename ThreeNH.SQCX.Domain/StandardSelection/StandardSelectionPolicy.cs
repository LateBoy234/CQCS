using System;

namespace ThreeNH.SQCX.Domain.StandardSelection
{
    public sealed class StandardSelectionPolicy
    {
        public bool IsAccepted(ColorDifference difference, StandardSelectionLimits limits)
        {
            if (difference == null)
                throw new ArgumentNullException(nameof(difference));
            if (limits == null)
                throw new ArgumentNullException(nameof(limits));

            return IsWithin(difference.DeltaL, limits.DeltaL)
                && IsWithin(difference.DeltaA, limits.DeltaA)
                && IsWithin(difference.DeltaB, limits.DeltaB)
                && IsWithin(difference.DeltaE, limits.DeltaE);
        }

        private static bool IsWithin(double value, DifferenceLimit limit)
        {
            return !limit.Enabled || value <= limit.Maximum;
        }
    }
}
