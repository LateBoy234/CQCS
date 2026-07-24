using System;
using ThreeNH.SQCX.Domain.QualityControl;

namespace ThreeNH.SQCX.Application.QualityControl
{
    public sealed class EvaluateQualityUseCase : IEvaluateQualityUseCase
    {
        private readonly QualityJudgementService _judgementService;

        public EvaluateQualityUseCase(QualityJudgementService judgementService)
        {
            _judgementService = judgementService ??
                throw new ArgumentNullException(nameof(judgementService));
        }

        public QualityJudgement Execute(EvaluateQualityRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            return _judgementService.Evaluate(request.Criteria, request.WarningLevel);
        }
    }
}
