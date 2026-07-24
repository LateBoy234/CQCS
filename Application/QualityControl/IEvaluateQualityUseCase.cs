using ThreeNH.SQCX.Domain.QualityControl;

namespace ThreeNH.SQCX.Application.QualityControl
{
    public interface IEvaluateQualityUseCase
    {
        QualityJudgement Execute(EvaluateQualityRequest request);
    }
}
