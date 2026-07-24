using System;
using ThreeNH.SQCX.Domain.StandardSelection;

namespace ThreeNH.SQCX.Application.StandardSelection
{
    public sealed class NearestStandardCandidate<TStandard> where TStandard : class
    {
        public NearestStandardCandidate(TStandard standard, ColorDifference difference)
        {
            Standard = standard;
            Difference = difference ?? throw new ArgumentNullException(nameof(difference));
        }

        public TStandard Standard { get; private set; }
        public ColorDifference Difference { get; private set; }
    }
}
