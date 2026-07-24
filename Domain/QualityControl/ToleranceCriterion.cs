using System;

namespace ThreeNH.SQCX.Domain.QualityControl
{
    public sealed class ToleranceCriterion
    {
        public ToleranceCriterion(
            string key,
            double value,
            double lower,
            double upper,
            bool useForJudgement)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("A criterion key is required.", nameof(key));
            if (lower > upper)
                throw new ArgumentException("The lower tolerance cannot exceed the upper tolerance.");

            Key = key;
            Value = value;
            Lower = lower;
            Upper = upper;
            UseForJudgement = useForJudgement;
        }

        public string Key { get; private set; }
        public double Value { get; private set; }
        public double Lower { get; private set; }
        public double Upper { get; private set; }
        public bool UseForJudgement { get; private set; }
    }
}
