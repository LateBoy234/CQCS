using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using ThreeNH.ColorQualityControl.Adapter;
using ThreeNH.DataModel;
using ThreeNH.SQCX.Application.QualityControl;
using ThreeNH.SQCX.Domain.QualityControl;
using ThreeNH.SQCX.Infrastructure.DependencyInjection;
using DomainJudgementLevel = ThreeNH.SQCX.Domain.QualityControl.JudgementLevel;

namespace ThreeNH.ColorQualityControl
{
    public enum JudgementLevel
    {
        [Description("JudgementLevel_Invalid")]
        Invalid,
        [Description("JudgementLevel_NG")]
        NotGood,
        [Description("JudgementLevel_Warning")]
        Warning,
        [Description("JudgementLevel_Pass")]
        Pass
    }

    public class Judgement
    {
        private static readonly IEvaluateQualityUseCase EvaluateQuality =
            ApplicationServices.Create().EvaluateQuality;

        private Dictionary<ChromaticityField, JudgementLevel> _judgementFields =
            new Dictionary<ChromaticityField, JudgementLevel>();


        public static Judgement Judge(TestSpecimenAdapter test, Tolerance tolerance, JudgementSetting setting)
        {
            var criteria = new List<ToleranceCriterion>();
            foreach (var field in tolerance.ItemKeys)
            {
                double value = (double)test.GetPropertyValue(field);
                double lower = tolerance[field].Lower.GetValueOrDefault(0.0);
                double upper = tolerance[field].Upper.GetValueOrDefault(0.0);
                criteria.Add(new ToleranceCriterion(
                    field.ToString(),
                    value,
                    lower,
                    upper,
                    tolerance[field].UseForJudgement));
            }

            var result = EvaluateQuality.Execute(
                new EvaluateQualityRequest(criteria, setting.WarningLevel));
            var judgement = new Judgement
            {
                TotalJudgement = ToPresentationLevel(result.Total)
            };

            foreach (var field in tolerance.ItemKeys)
                judgement[field] = ToPresentationLevel(result.Items[field.ToString()]);

            return judgement;
        }

        private static JudgementLevel ToPresentationLevel(DomainJudgementLevel level)
        {
            return (JudgementLevel)(int)level;
        }


        /// <summary>
        /// 总评
        /// </summary>
        public JudgementLevel TotalJudgement
        {
            get; private set;
        }

        public bool Passed => TotalJudgement == JudgementLevel.Pass;
        /// <summary>
        /// 对于某项的评价
        /// </summary>
        /// <param name="diffField">色差项</param>
        /// <returns></returns>
        public JudgementLevel this[ChromaticityField diffField]
        {
            get {
                return _judgementFields.ContainsKey(diffField) ?
                    _judgementFields[diffField] : JudgementLevel.Invalid;
            }
            private set { _judgementFields[diffField] = value; }
        }
    }
}
