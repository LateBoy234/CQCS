using System.Collections.Generic;
using ThreeNH.SQCX.Domain.QualityControl;

namespace ThreeNH.SQCX.Application.QualityControl
{
    public sealed class EvaluateQualityRequest
    {
        public EvaluateQualityRequest(
            IEnumerable<ToleranceCriterion> criteria,
            double? warningLevel)
        {
            Criteria = criteria;
            WarningLevel = warningLevel;
        }

        public IEnumerable<ToleranceCriterion> Criteria { get; private set; }
        public double? WarningLevel { get; private set; }
    }
}
