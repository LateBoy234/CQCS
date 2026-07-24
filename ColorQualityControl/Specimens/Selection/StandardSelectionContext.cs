using ThreeNH.ColorAlgorithm;
using ThreeNH.ColorQualityControl.Refactoring.Contracts;
using ThreeNH.ColorSpecimen;
using ThreeNH.DataModel;
using ThreeNH.Optical;

namespace ThreeNH.ColorQualityControl.Specimens.Selection
{
    /// <summary>
    /// 选标样策略所需的测量/Job 上下文，由 MeasurementService 从 IMeasurementHost 组装。
    /// </summary>
    internal sealed class StandardSelectionContext
    {
        public StandardSelectionContext(IMeasurementHost host, Specimen sample)
        {
            Host = host;
            Sample = sample;
        }

        public IMeasurementHost Host { get; }

        public Specimen Sample { get; }

        public StandardSpecimen CurrentStandard => Host.CurrentStandard;

        public StandardSpecimen SingleStandardInShowing => Host.SingleStandardInShowing;

        public SpecimenDatabase Database => Host.Database;

        public SpecularComponent SpecularComponent => Host.SpecularComponent;

        public StandardObserver Observer => Host.Observer;

        public StandardIlluminant Illuminant => Host.Illuminant;

        public Formula ColorFormula => Host.ColorFormula;

        public Tolerance AutoSelectLimits => Host.AutoSelectStandardLimits;
    }
}
