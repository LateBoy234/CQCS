using System;
using ThreeNH.ColorAlgorithm;
using ThreeNH.ColorQualityControl.Refactoring.Contracts;
using ThreeNH.ColorSpecimen;
using ThreeNH.DataModel;
using ThreeNH.Optical;
using ThreeNH.SQCX.Application.StandardSelection;
using ThreeNH.SQCX.Domain.StandardSelection;

namespace ThreeNH.ColorQualityControl.Adapter
{
    internal sealed class LegacyStandardSelectionGateway
        : IStandardSelectionGateway<StandardSpecimen, Specimen>
    {
        private readonly IMeasurementHost _host;

        public LegacyStandardSelectionGateway(IMeasurementHost host)
        {
            _host = host ?? throw new ArgumentNullException(nameof(host));
        }

        public StandardSpecimen GetManualStandard()
        {
            return _host.SingleStandardInShowing ?? _host.CurrentStandard;
        }

        public NearestStandardCandidate<StandardSpecimen> FindNearest(Specimen sample)
        {
            if (sample == null)
                return null;

            CIELAB lab = sample.GetColorData(_host.SpecularComponent)?.Lab(_host.ColorFormula);
            if (lab == null)
                return null;

            var standard = _host.Database.FindNearestStandard(
                lab, _host.SpecularComponent, _host.Observer, _host.Illuminant);
            if (standard == null)
                return null;

            CIELAB standardLab = standard
                .GetColorData(_host.SpecularComponent)
                .Lab(_host.ColorFormula);
            var delta = new CIEDE1976(lab, standardLab);
            return new NearestStandardCandidate<StandardSpecimen>(
                standard,
                new ColorDifference(delta.dL, delta.da, delta.db, delta.dE));
        }

        public StandardSelectionLimits GetAutoSelectionLimits()
        {
            Tolerance limits = _host.AutoSelectStandardLimits;
            return new StandardSelectionLimits(
                ToLimit(limits, ChromaticityField.dL),
                ToLimit(limits, ChromaticityField.da),
                ToLimit(limits, ChromaticityField.db),
                ToLimit(limits, ChromaticityField.dEab));
        }

        private static DifferenceLimit ToLimit(Tolerance tolerance, ChromaticityField field)
        {
            var item = tolerance[field];
            return item.UseForJudgement
                ? DifferenceLimit.UpTo(item.Upper.GetValueOrDefault(0.0))
                : DifferenceLimit.Disabled();
        }
    }
}
