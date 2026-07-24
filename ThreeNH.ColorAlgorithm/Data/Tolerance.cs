using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using ThreeNH.ColorAlgorithm.IData;

namespace ThreeNH.ColorAlgorithm.Data
{
    [DataContract]
    public class Tolerance : ITolerance
    {
        [DataMember(Name = "Items")]
        private Dictionary<string, IToleranceItem> _items = new Dictionary<string, IToleranceItem>();

        [DataMember(Name = "Factors")]
        private Dictionary<string, double> _factors;

        public Tolerance()
        {
        }

        public Tolerance(IDictionary<string, IToleranceItem> items, IDictionary<string, double> factors = null)
        {
            _items = new Dictionary<string, IToleranceItem>(items);
            if (factors != null)
                _factors = new Dictionary<string, double>(factors);
        }

        public Tolerance(Tolerance other)
        {
            foreach (var item in other._items)
            {
                _items[item.Key] = new ToleranceItem
                {
                    Lower = item.Value.Lower,
                    Upper = item.Value.Upper,
                };
            }

            if (other._factors != null)
            {
                _factors = new Dictionary<string, double>(other._factors);
            }
        }

        #region 创建指定类型容差的静态方法及一些判断方法

        /// <summary>
        /// 创建密度容差
        /// 密度容差的判定方法与其他不同
        /// 其方法是只要给定数据项合格就合格，如果给定项不包含任何密度差项就返回null
        /// </summary>
        public static Tolerance CreateDensityTolerance()
        {
            var tolerance = new Tolerance();
            tolerance.Items.Add(nameof(SampleProperties.DensityDiff_C), new ToleranceItem(-0.1, 0.1));
            tolerance.Items.Add(nameof(SampleProperties.DensityDiff_M), new ToleranceItem(-0.1, 0.1));
            tolerance.Items.Add(nameof(SampleProperties.DensityDiff_Y), new ToleranceItem(-0.1, 0.1));
            tolerance.Items.Add(nameof(SampleProperties.DensityDiff_K), new ToleranceItem(-0.1, 0.1));
            tolerance.Items.Add(nameof(SampleProperties.DensityDiff_Spot), new ToleranceItem(-0.1, 0.1));
            tolerance.JudgeFunc = (items, values) =>
            {
                var names = tolerance.Items.Keys.Intersect(values.Keys).ToArray();
                if (names.Length == 0) return null;
                foreach (var name in names)
                {
                    if (values[name] < items[name].Lower || values[name] > items[name].Upper)
                        return false;
                }

                return true;
            };
            return tolerance;
        }

        /// <summary>
        /// 创建CIEDE1976色差公式容差
        /// </summary>
        public static Tolerance CreateDe76Tolerance()
        {
            var tolerance = new Tolerance();
            tolerance.Items.Add(nameof(SampleProperties.ColorDiffFormula_DE1976), new ToleranceItem(double.NaN, 0.1));
            return tolerance;
        }

        /// <summary>
        /// 创建CIEDE1994色差公式容差
        /// </summary>
        public static Tolerance CreateDe94Tolerance()
        {
            var tolerance = new Tolerance();
            tolerance.Items.Add(nameof(SampleProperties.ColorDiffFormula_DE1994), new ToleranceItem(double.NaN, 0.1));
            tolerance.Factors.Add(SamplePropertySettingKeys.CIEDE94FactorL, 1.0);
            tolerance.Factors.Add(SamplePropertySettingKeys.CIEDE94FactorC, 1.0);
            tolerance.Factors.Add(SamplePropertySettingKeys.CIEDE94FactorH, 1.0);
            return tolerance;
        }

        /// <summary>
        /// 创建CIEDE2000色差公式容差
        /// </summary>
        public static Tolerance CreateDe00Tolerance()
        {
            var tolerance = new Tolerance();
            tolerance.Items.Add(nameof(SampleProperties.ColorDiffFormula_DE2000), new ToleranceItem(double.NaN, 0.1));
            tolerance.Factors.Add(SamplePropertySettingKeys.CIEDE00FactorL, 1.0);
            tolerance.Factors.Add(SamplePropertySettingKeys.CIEDE00FactorC, 1.0);
            tolerance.Factors.Add(SamplePropertySettingKeys.CIEDE00FactorH, 1.0);
            return tolerance;
        }

        /// <summary>
        /// 创建CMCDE色差公式容差
        /// </summary>
        public static Tolerance CreateCmcDeTolerance()
        {
            var tolerance = new Tolerance();
            tolerance.Items.Add(nameof(SampleProperties.ColorDiffFormula_CMCDE), new ToleranceItem(double.NaN, 0.1));
            tolerance.Factors.Add(SamplePropertySettingKeys.CMCDEFactorL, 1.0);
            tolerance.Factors.Add(SamplePropertySettingKeys.CMCDEFactorC, 1.0);
            return tolerance;
        }


        /// <summary>
        /// 创建DeltaLab容差
        /// </summary>
        public static Tolerance CreateDeltaLabTolerance()
        {
            var tolerance = new Tolerance();
            tolerance.Items.Add(nameof(SampleProperties.ColorDiff_CIELAB_L), new ToleranceItem(-0.8, 0.8));
            tolerance.Items.Add(nameof(SampleProperties.ColorDiff_CIELAB_a), new ToleranceItem(-0.8, 0.8));
            tolerance.Items.Add(nameof(SampleProperties.ColorDiff_CIELAB_b), new ToleranceItem(-0.8, 0.8));
            return tolerance;
        }

        /// <summary>
        /// 创建DeltaLCH容差
        /// </summary>
        public static Tolerance CreateDeltaLchTolerance()
        {
            var tolerance = new Tolerance();
            tolerance.Items.Add(nameof(SampleProperties.ColorDiff_CIELAB_L), new ToleranceItem(-0.8, 0.8));
            tolerance.Items.Add(nameof(SampleProperties.ColorDiff_CIELCH_C), new ToleranceItem(-0.8, 0.8));
            tolerance.Items.Add(nameof(SampleProperties.ColorDiff_CIELCH_H), new ToleranceItem(-0.8, 0.8));
            return tolerance;
        }

        /// <summary>
        /// 判断是否是密度容差
        /// </summary>
        /// <returns></returns>
        public static bool IsDensityTolerance(ITolerance tolerance)
        {
            return tolerance.Items.Count == 5 &&
                   tolerance.Contains(nameof(SampleProperties.DensityDiff_C)) &&
                   tolerance.Contains(nameof(SampleProperties.DensityDiff_M)) &&
                   tolerance.Contains(nameof(SampleProperties.DensityDiff_Y)) &&
                   tolerance.Contains(nameof(SampleProperties.DensityDiff_K)) &&
                   tolerance.Contains(nameof(SampleProperties.DensityDiff_Spot));
        }

        public static bool IsDe76Tolerance(ITolerance tolerance)
        {
            return tolerance.Items.Count == 1 &&
                   tolerance.Contains(nameof(SampleProperties.ColorDiffFormula_DE1976));
        }

        public static bool IsDe94Tolerance(ITolerance tolerance)
        {
            return tolerance.Items.Count == 1 &&
                   tolerance.Contains(nameof(SampleProperties.ColorDiffFormula_DE1994));
        }

        public static bool IsDe00Tolerance(ITolerance tolerance)
        {
            return tolerance.Items.Count == 1 &&
                   tolerance.Contains(nameof(SampleProperties.ColorDiffFormula_DE2000));
        }

        public static bool IsCmcDeTolerance(ITolerance tolerance)
        {
            return tolerance.Items.Count == 1 &&
                   tolerance.Contains(nameof(SampleProperties.ColorDiffFormula_CMCDE));
        }

        public static bool IsDeltaLabTolerance(ITolerance tolerance)
        {
            return tolerance.Items.Count == 3 &&
                   tolerance.Contains(nameof(SampleProperties.ColorDiff_CIELAB_L)) &&
                   tolerance.Contains(nameof(SampleProperties.ColorDiff_CIELAB_a)) &&
                   tolerance.Contains(nameof(SampleProperties.ColorDiff_CIELAB_b));
        }

        public static bool IsDeltaLchTolerance(ITolerance tolerance)
        {
            return tolerance.Items.Count == 3 &&
                   tolerance.Contains(nameof(SampleProperties.ColorDiff_CIELAB_L)) &&
                   tolerance.Contains(nameof(SampleProperties.ColorDiff_CIELCH_C)) &&
                   tolerance.Contains(nameof(SampleProperties.ColorDiff_CIELCH_H));
        }

        #endregion

        /// <summary>
        /// 判定方法，如果不为空则使用该方法判定是否合格，否则只有当所有数据容差项的值都在指定范围内时才合格
        /// </summary>
        public Func<Dictionary<string, IToleranceItem>, IDictionary<string, double>, bool?> JudgeFunc { get; set; }

        public double GetFactor(string factorName)
        {
            if (ContainsFactor(factorName)) return _factors[factorName];
            throw new ArgumentException($"Cannot find factor '{factorName}'", nameof(factorName));
        }

        public Dictionary<string, IToleranceItem> Items => _items;

        public Dictionary<string, double> Factors
        {
            get
            {
                lock (this)
                {
                    return _factors ?? (_factors = new Dictionary<string, double>());
                }
            }
        }

        public bool? IsPass(IDictionary<string, double> values)
        {
            if (JudgeFunc != null)
            {
                return JudgeFunc(Items, values);
            }

            foreach (var item in Items)
            {
                if (!values.ContainsKey(item.Key))
                    return null;

                if (double.IsNaN(item.Value.Lower))
                {
                    if (values[item.Key] > item.Value.Upper)
                        return false;
                }
                else if (double.IsNaN(item.Value.Upper))
                {
                    if (values[item.Key] < item.Value.Lower)
                        return false;
                }
                else
                {
                    if (values[item.Key] < item.Value.Lower || values[item.Key] > item.Value.Upper)
                        return false;
                }
            }

            return true;
        }

        public IToleranceCondition Condition { get; set; }

        public IToleranceItem this[string name]
        {
            get => _items[name];
            set => _items[name] = value;
        }

        #region ITolerance

        public object Clone()
        {
            return new Tolerance(this);
        }

        IToleranceItem ITolerance.this[string name]
        {
            get => _items[name];
            set
            {
                if (value is ToleranceItem item)
                {
                    _items[name] = item;
                }
                throw new ArgumentException();
            }
        }

        public bool Contains(string name)
        {
            return _items.ContainsKey(name);
        }

        public bool ContainsFactor(string factorName)
        {
            return _factors?.ContainsKey(factorName) == true;
        }

        public IToleranceItem GetItem(string name, IToleranceItem defaultValue = null)
        {
            if (_items.ContainsKey(name)) return _items[name];
            return defaultValue;
        }

        IReadOnlyList<KeyValuePair<string, IToleranceItem>> ITolerance.Items =>
            _items.ToArray();

        IReadOnlyList<KeyValuePair<string, double>> ITolerance.Factors =>
            _factors?.ToArray();

        #endregion
    }
}
