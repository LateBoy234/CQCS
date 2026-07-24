using System;
using ThreeNH.SQCX.Domain.StandardSelection;

namespace ThreeNH.SQCX.Application.StandardSelection
{
    public sealed class SelectStandardUseCase<TStandard, TSample>
        : ISelectStandardUseCase<TStandard, TSample>
        where TStandard : class
    {
        private readonly IStandardSelectionGateway<TStandard, TSample> _gateway;
        private readonly StandardSelectionPolicy _policy;

        public SelectStandardUseCase(
            IStandardSelectionGateway<TStandard, TSample> gateway,
            StandardSelectionPolicy policy)
        {
            _gateway = gateway ?? throw new ArgumentNullException(nameof(gateway));
            _policy = policy ?? throw new ArgumentNullException(nameof(policy));
        }

        public TStandard SelectManual()
        {
            return _gateway.GetManualStandard();
        }

        public TStandard SelectForMeasurement(TSample sample, bool automatic)
        {
            return automatic
                ? FindNearest(sample) ?? _gateway.GetManualStandard()
                : _gateway.GetManualStandard();
        }

        public TStandard FindNearest(TSample sample)
        {
            var candidate = _gateway.FindNearest(sample);
            if (candidate == null)
                return default(TStandard);

            return _policy.IsAccepted(candidate.Difference, _gateway.GetAutoSelectionLimits())
                ? candidate.Standard
                : default(TStandard);
        }
    }
}
