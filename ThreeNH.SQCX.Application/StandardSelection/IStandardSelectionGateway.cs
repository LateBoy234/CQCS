using ThreeNH.SQCX.Domain.StandardSelection;

namespace ThreeNH.SQCX.Application.StandardSelection
{
    public interface IStandardSelectionGateway<TStandard, in TSample>
        where TStandard : class
    {
        TStandard GetManualStandard();
        NearestStandardCandidate<TStandard> FindNearest(TSample sample);
        StandardSelectionLimits GetAutoSelectionLimits();
    }
}
