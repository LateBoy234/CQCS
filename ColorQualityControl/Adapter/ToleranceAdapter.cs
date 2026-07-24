using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows.Data;
using ThreeNH.DataModel;
using ThreeNH.Helper;
using ThreeNH.Optical;

namespace ThreeNH.ColorQualityControl.Adapter
{
    class ToleranceConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Tolerance)
            {
                if (parameter is ObserverIlluminantPair)
                {
                    return ((ObserverIlluminantPair)parameter).ToString();
                }
                else if (parameter is ChromaticityField)
                {
                    Tolerance tolerance = value as Tolerance;
                    ChromaticityField field = (ChromaticityField)parameter;
                    if (tolerance.ItemKeys.Contains(field))
                    {
                        return tolerance[field];
                    }
                }
            }
            else if (value is ToleranceAdapter)
            {
                ToleranceAdapter adapter = value as ToleranceAdapter;
                string propertyName = parameter as string;
                if (adapter != null && propertyName != null)
                {
                    return adapter.GetAttribute(propertyName);
                }
            }
            return "--";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class ToleranceAdapter
    {
        const string KeyDE00Factors = "l:c:h (CIEDE00)";
        const string KeyDE94Factors = "l:c:h (CIEDE94)";
        const string KeyCMCFactors = "l:c (CMC)";

        Dictionary<string, string> items = new Dictionary<string, string>();


        public IDictionary<string, string> Attributes
        {
            get
            {
                return items;
            }
        }

        /// <summary>
        /// 获取容差设置有效的属性名称
        /// </summary>
        /// <param name="toleranceSetting"></param>
        /// <returns></returns>
        public static string[] GetItemNames(ToleranceSetting toleranceSetting)
        {
            List<string> list = new List<string>();
            list.Add(S._("Label_Tolerance"));
            foreach(var key in toleranceSetting.ItemKeys)
            {
                list.Add(key.Name());
                if (key == ChromaticityField.dE00)
                {
                    list.Add(KeyDE00Factors);
                }
                else if (key == ChromaticityField.dE94)
                {
                    list.Add(KeyDE94Factors);
                }
                else if (key == ChromaticityField.dEcmc)
                {
                    list.Add(KeyCMCFactors);
                }
            }

            return list.ToArray();
        }

        /// <summary>
        /// 获取容差设置的适配器
        /// </summary>
        /// <param name="observerAndIlluminantSetting">观察者角度和光源设置</param>
        /// <param name="toleranceSetting">容差设置</param>
        /// <returns>返回适配器</returns>
        public static ToleranceAdapter[] GetAdapters(
            ObserverAndIlluminantSetting observerAndIlluminantSetting,  
            ToleranceSetting toleranceSetting)
        {
            List<ToleranceAdapter> list = new List<ToleranceAdapter>();

            if (observerAndIlluminantSetting.PrimaryIlluminant != StandardIlluminant.Unknown)
            {
                list.Add(new ToleranceAdapter(
                    observerAndIlluminantSetting.Primary,
                    toleranceSetting.PrimaryTolerance));
            }
            if (observerAndIlluminantSetting.SecondaryIlluminant != StandardIlluminant.Unknown)
            {
                list.Add(new ToleranceAdapter(
                    observerAndIlluminantSetting.Secondary,
                    toleranceSetting.SecondaryTolerance));
            }
            if (observerAndIlluminantSetting.TertiaryIlluminant != StandardIlluminant.Unknown)
            {
                list.Add(new ToleranceAdapter(
                    observerAndIlluminantSetting.Tertiary,
                    toleranceSetting.TertiaryTolerance));
            }

            return list.ToArray();
        }

        public string GetAttribute(string attributeName)
        {
            return Attributes.ContainsKey(attributeName) ? Attributes[attributeName] : "--";
        }

        public ToleranceAdapter(ObserverIlluminantPair observerIlluminant, Tolerance tolerance)
        {
            items[S._("Label_Tolerance")] = observerIlluminant.ToString();
            if (tolerance == null)
                return;
            foreach(var field in tolerance.ItemKeys)
            {
                if (tolerance[field].Lower != null)
                {
                    if (field.IsReflectanceDifference())
                    {
                        items[field.Name()] = string.Format("[{0}, {1}]",
                            tolerance[field].Lower * 100.0, tolerance[field].Upper * 100.0);
                    }
                    else
                    {
                        items[field.Name()] = string.Format("[{0}, {1}]",
                            tolerance[field].Lower, tolerance[field].Upper);
                    }
                }
                else
                {
                    items[field.Name()] = string.Format("{0}", tolerance[field].Upper);
                }

                if (field == ChromaticityField.dE00)
                {
                    items[KeyDE00Factors] = string.Format("{0} : {1} : {2}",
                        tolerance.KL00, tolerance.KC00, tolerance.KH00);
                }
                else if (field == ChromaticityField.dE94)
                {
                    items[KeyDE94Factors] = string.Format("{0} : {1} : {2}",
                        tolerance.KL94, tolerance.KC94, tolerance.KH94);
                }
                else if (field == ChromaticityField.dEcmc)
                {
                    items[KeyCMCFactors] = string.Format("{0} : {1}",
                        tolerance.KLcmc, tolerance.KCcmc);
                }
            }
        }
    }
}
