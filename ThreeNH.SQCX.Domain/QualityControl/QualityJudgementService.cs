using System;
using System.Collections.Generic;

namespace ThreeNH.SQCX.Domain.QualityControl
{
    public sealed class QualityJudgementService
    {
        public QualityJudgement Evaluate(
            IEnumerable<ToleranceCriterion> criteria,
            double? warningLevel)
        {
            if (criteria == null)
                throw new ArgumentNullException(nameof(criteria));
            if (warningLevel.HasValue &&
                (warningLevel.Value <= 0.0 || warningLevel.Value >= 1.0))
                throw new ArgumentOutOfRangeException(nameof(warningLevel));

            var results = new Dictionary<string, JudgementLevel>();
            var total = JudgementLevel.Pass;
            foreach (var criterion in criteria)
            {
                var level = EvaluateCriterion(criterion, warningLevel);
                results[criterion.Key] = level;
                if (criterion.UseForJudgement && level < total)
                    total = level;
            }

            return new QualityJudgement(results, total);
        }

        private static JudgementLevel EvaluateCriterion(
            ToleranceCriterion criterion,
            double? warningLevel)
        {
            if (!criterion.UseForJudgement || double.IsNaN(criterion.Value))
                return JudgementLevel.Invalid;

            if (criterion.Value < criterion.Lower || criterion.Value > criterion.Upper)
                return JudgementLevel.NotGood;

            if (!warningLevel.HasValue)
                return JudgementLevel.Pass;

            var warningLower = criterion.Lower * warningLevel.Value;
            var warningUpper = criterion.Upper * warningLevel.Value;
            return criterion.Value < warningLower || criterion.Value > warningUpper
                ? JudgementLevel.Warning
                : JudgementLevel.Pass;
        }
    }
}
