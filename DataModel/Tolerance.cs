using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Globalization;
using System.Runtime.Serialization;

namespace ThreeNH.DataModel
{
    [DataContract]
    public struct ToleranceItem
    {
        [DataMember]
        public double? Lower;
        [DataMember]
        public double? Upper;
        [DataMember]
        public bool UseForJudgement;

        public override string ToString()
        {
            if (Lower != null && Upper != null)
                return String.Format("[{0:F3}, {1:F3}]", Lower, Upper);
            if (Lower != null)
                return String.Format(">={0:F3}", Lower);
            if (Upper != null)
                return String.Format("{0:F3}", Upper);
            return "";
        }
    }

    [DataContract]
    public class Tolerance
    {
        [DataMember]
        Dictionary<ChromaticityField, ToleranceItem> items = 
            new Dictionary<ChromaticityField, ToleranceItem>();

        // 所有可用的容差项
        public static ChromaticityField[] AllToleranceItemKeys
        {
            get; private set;
        }

        public Tolerance()
        {
            KL94 = KC94 = KH94 = 1.0;
            KL00 = KC00 = KH00 = 1.0;
            KLcmc = 2.0;
            KCcmc = 1.0;
        }

        /// <summary>
        /// 从原有的Tolerance创建一个新的Tolerance对象
        /// </summary>
        /// <param name="source">源Tolerance对象</param>
        public Tolerance(Tolerance source)
        {
            KL94 = source.KL94;
            KC94 = source.KC94;
            KH94 = source.KH94;

            KL00 = source.KL00;
            KC00 = source.KC00;
            KH00 = source.KH00;

            KLcmc = source.KLcmc;
            KCcmc = source.KCcmc;

            foreach(var key in source.items.Keys)
            {
                items[key] = source.items[key];
            }
        }

        public Tolerance(ChromaticityField[] itemKeys)
        {
            KL94 = KC94 = KH94 = 1.0;
            KL00 = KC00 = KH00 = 1.0;
            KLcmc = 2.0;
            KCcmc = 1.0;

            ItemKeys = itemKeys;
        }

        public virtual bool LoadXmlElement(XmlElement node)
        {
            items.Clear();
            var style = NumberStyles.AllowThousands | NumberStyles.Float;
            var culture = CultureInfo.InvariantCulture;

            foreach (XmlNode child in node.ChildNodes)
            {
                if (string.Compare(child.Name, "ToleranceItem", true) == 0)
                {
                    ChromaticityField key;
                    if (child.Attributes["Key"] != null && Enum.TryParse(child.Attributes["Key"].Value, out key))
                    {
                        ToleranceItem item = new ToleranceItem();

                        double result;
                        if (child.Attributes["Lower"] != null)
                        {
                            if (double.TryParse(child.Attributes["Lower"].Value, style, culture, out result))
                                item.Lower = result;
                            else
                                item.Lower = null;
                        }
                        if (child.Attributes["Upper"] != null)
                        {
                            if (double.TryParse(child.Attributes["Upper"].Value, style, culture, out result))
                                item.Upper = result;
                            else
                                item.Upper = null;
                        }
                        if (child.Attributes["IsUsed"] != null)
                        {
                            bool bval;
                            if (bool.TryParse(child.Attributes["IsUsed"].Value, out bval))
                                item.UseForJudgement = bval;
                            else
                                item.UseForJudgement = false;
                        }

                        if (item.Lower != null || item.Upper != null)
                        {
                            items[key] = item;
                        }
                    }
                }
            }
            double value;
            if (node.Attributes["KL94"] != null && double.TryParse(node.Attributes["KL94"].Value, style, culture, out value))
            {
                KL94 = value;
            }
            if (node.Attributes["KC94"] != null && double.TryParse(node.Attributes["KC94"].Value, style, culture, out value))
            {
                KC94 = value;
            }
            if (node.Attributes["KH94"] != null && double.TryParse(node.Attributes["KH94"].Value, style, culture, out value))
            {
                KH94 = value;
            }

            if (node.Attributes["KL00"] != null && double.TryParse(node.Attributes["KL00"].Value, style, culture, out value))
            {
                KL00 = value;
            }
            if (node.Attributes["KC00"] != null && double.TryParse(node.Attributes["KC00"].Value, style, culture, out value))
            {
                KC00 = value;
            }
            if (node.Attributes["KH00"] != null && double.TryParse(node.Attributes["KH00"].Value, style, culture, out value))
            {
                KH00 = value;
            }

            if (node.Attributes["KLcmc"] != null && double.TryParse(node.Attributes["KLcmc"].Value, style, culture, out value))
            {
                KLcmc = value;
            }
            if (node.Attributes["KCcmc"] != null && double.TryParse(node.Attributes["KCcmc"].Value, style, culture, out value))
            {
                KCcmc = value;
            }

            return true;
        }

        public virtual XmlElement ToXmlElement(XmlDocument doc, string nodeName)
        {
            var element = doc.CreateElement(nodeName);

            foreach(var item in items)
            {
                var itemNode = doc.CreateElement("ToleranceItem");
                element.AppendChild(itemNode);
                itemNode.SetAttribute("Key", item.Key.ToString());
                if (item.Value.Lower != null)
                    itemNode.SetAttribute("Lower", ((double)item.Value.Lower).ToString(CultureInfo.InvariantCulture));
                if (item.Value.Upper != null)
                    itemNode.SetAttribute("Upper", ((double)item.Value.Upper).ToString(CultureInfo.InvariantCulture));
                itemNode.SetAttribute("IsUsed", item.Value.UseForJudgement.ToString());
            }
            element.SetAttribute("KL94", KL94.ToString(CultureInfo.InvariantCulture));
            element.SetAttribute("KC94", KC94.ToString(CultureInfo.InvariantCulture));
            element.SetAttribute("KH94", KH94.ToString(CultureInfo.InvariantCulture));

            element.SetAttribute("KL00", KL00.ToString(CultureInfo.InvariantCulture));
            element.SetAttribute("KC00", KC00.ToString(CultureInfo.InvariantCulture));
            element.SetAttribute("KH00", KH00.ToString(CultureInfo.InvariantCulture));

            element.SetAttribute("KLcmc", KLcmc.ToString(CultureInfo.InvariantCulture));
            element.SetAttribute("KCcmc", KCcmc.ToString(CultureInfo.InvariantCulture));

            return element;
        }

        /// <summary>
        /// 返回指定的容差项
        /// </summary>
        /// <param name="key">色差字段或颜色指标的代号</param>
        /// <returns>返回指定色差字段的容差项</returns>
        public ToleranceItem this[ChromaticityField key]
        {
            get
            {
                if (items.ContainsKey(key))
                    return items[key];
                var item = DefaultToleranceItem(key);
                item.UseForJudgement = false;
                return item;
            }
            set
            {
                if (key.IsColorDiffField() || key.IsChromaticityIndex())
                {
                    items[key] = value;
                }
                else
                {
                    throw new ArgumentException(
                        string.Format("'{0}' is not color difference field or color index field.", key));

                }
            }
        }
        /// <summary>
        /// 返回所有容差项的键，或将容差项设为指定的项（已存在的使用原值，否则使用默认值）
        /// </summary>
        public ChromaticityField[] ItemKeys
        {
            get { return items.Keys.ToArray(); }
            internal set
            {
                if (value != null)
                {
                    // 移除不在指定的项中的原有项
                    var keys = items.Keys.ToArray();
                    foreach(var key in keys)
                    {
                        if (value.Contains(key))
                            continue;
                        items.Remove(key);
                    }
                    // 将新项添加到容差项中
                    keys = items.Keys.ToArray();
                    foreach(var key in value)
                    {
                        if (keys.Contains(key))
                            continue;
                        items[key] = DefaultToleranceItem(key);
                    }
                }
            }
        }
        /// <summary>
        /// 删除指定键的容差项
        /// </summary>
        /// <param name="key">要删除的容差项的键</param>
        public void Delete(ChromaticityField key)
        {
            if (items.ContainsKey(key))
                items.Remove(key);
        }
        /// <summary>
        /// 清容所有容差项
        /// </summary>
        public void Clear()
        {
            items.Clear();
        }

        // CIEDE94的参数因子
        [DataMember]
        public double KL94 { get; set; }
        [DataMember]
        public double KC94 { get; set; }
        [DataMember]
        public double KH94 { get; set; }
        // CIEDE00的参数因子
        [DataMember]
        public double KL00 { get; set; }
        [DataMember]
        public double KC00 { get; set; }
        [DataMember]
        public double KH00 { get; set; }
        // CMC的参数因子
        [DataMember]
        public double KLcmc { get; set; }
        [DataMember]
        public double KCcmc { get; set; }


        static Dictionary<ChromaticityField, ToleranceItem> defaultToleranceItems =
            new Dictionary<ChromaticityField, ToleranceItem>();

        static Tolerance()
        {
            List<ChromaticityField> list = new List<ChromaticityField>();

            foreach (ChromaticityField field in Enum.GetValues(typeof(ChromaticityField)))
            {
                if (!field.IsColorDiffField())
                {
                    continue;
                }

                if (field.IsEquation())
                {
                    defaultToleranceItems[field] = new ToleranceItem
                    {
                        Lower = null,
                        Upper = 1.0,
                        UseForJudgement = true
                    };
                }
                else if (field.IsReflectanceDifference())
                {
                    defaultToleranceItems[field] = new ToleranceItem
                    {
                        Lower = -0.02,
                        Upper = 0.02,
                        UseForJudgement = false
                    };
                }
                else
                {
                    if (field == ChromaticityField.dx || field == ChromaticityField.dy)
                    {
                        defaultToleranceItems[field] = new ToleranceItem
                        {
                            Lower = -0.01,
                            Upper = 0.01,
                            UseForJudgement = false
                        };
                    }
                    else
                    {
                        defaultToleranceItems[field] = new ToleranceItem
                        {
                            Lower = -0.8,
                            Upper = 0.8,
                            UseForJudgement = false
                        };
                    }
                }
                list.Add(field);
            }

            AllToleranceItemKeys = list.ToArray();
        }

        public static ToleranceItem DefaultToleranceItem(ChromaticityField field)
        {
            if (field.IsColorDiffField() || field.IsChromaticityIndex())
                return defaultToleranceItems[field];
            throw new ArgumentException(
                        string.Format("'{0}' is not color difference field or color index field.", field));
        }

        public bool IsEquals(Tolerance other)
        {
            if (this == other)
                return true;
            if (items.Count != other.items.Count)
                return false;

            if (KL00 != other.KL00 || KC00 != other.KC00 || KH00 != other.KH00 ||
                KL94 != other.KL94 || KC94 != other.KC94 || KH94 != other.KH94 ||
                KLcmc != other.KLcmc || KCcmc != other.KCcmc)
            {
                return false;
            }

            SortedList<ChromaticityField, ToleranceItem> sortedItems1 = 
                new SortedList<ChromaticityField, ToleranceItem>();
            SortedList<ChromaticityField, ToleranceItem> sortedItems2 =
                new SortedList<ChromaticityField, ToleranceItem>();

            foreach(var key in items.Keys)
            {
                sortedItems1.Add(key, items[key]);
            }
            foreach(var key in other.items.Keys)
            {
                sortedItems2.Add(key, other.items[key]);
            }

            for (int i = 0; i < items.Count; ++i)
            {
                if (sortedItems1.ElementAt(i).Key != sortedItems2.ElementAt(i).Key)
                    return false;
                if (!IsEquals(sortedItems1.ElementAt(i).Value, sortedItems2.ElementAt(i).Value))
                    return false;
            }

            return true;
        }

        public static bool IsEquals(ToleranceItem value1, ToleranceItem value2)
        {
            return value1.UseForJudgement == value2.UseForJudgement &&
                value1.Lower == value2.Lower &&
                value1.Upper == value2.Upper;
        }
    }
}