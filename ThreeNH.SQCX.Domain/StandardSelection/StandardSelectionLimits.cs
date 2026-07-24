using System;

namespace ThreeNH.SQCX.Domain.StandardSelection
{
    public sealed class StandardSelectionLimits
    {
        public StandardSelectionLimits(
            DifferenceLimit deltaL,
            DifferenceLimit deltaA,
            DifferenceLimit deltaB,
            DifferenceLimit deltaE)
        {
            DeltaL = deltaL ?? throw new ArgumentNullException(nameof(deltaL));
            DeltaA = deltaA ?? throw new ArgumentNullException(nameof(deltaA));
            DeltaB = deltaB ?? throw new ArgumentNullException(nameof(deltaB));
            DeltaE = deltaE ?? throw new ArgumentNullException(nameof(deltaE));
        }

        public DifferenceLimit DeltaL { get; private set; }
        public DifferenceLimit DeltaA { get; private set; }
        public DifferenceLimit DeltaB { get; private set; }
        public DifferenceLimit DeltaE { get; private set; }
    }
}
