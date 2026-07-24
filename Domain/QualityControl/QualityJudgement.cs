using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace ThreeNH.SQCX.Domain.QualityControl
{
    public sealed class QualityJudgement
    {
        public QualityJudgement(
            IDictionary<string, JudgementLevel> items,
            JudgementLevel total)
        {
            Items = new ReadOnlyDictionary<string, JudgementLevel>(
                new Dictionary<string, JudgementLevel>(items));
            Total = total;
        }

        public JudgementLevel Total { get; private set; }
        public bool Passed { get { return Total == JudgementLevel.Pass; } }
        public IReadOnlyDictionary<string, JudgementLevel> Items { get; private set; }
    }
}
