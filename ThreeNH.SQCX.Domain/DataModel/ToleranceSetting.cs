using System;
using System.Runtime.Serialization;
using System.Xml;

namespace ThreeNH.DataModel
{
    [DataContract]
    public class ToleranceSetting
    {
        public ToleranceSetting() { }

        /// <summary>
        /// 从原有的容差设置创建一个新的
        /// </summary>
        /// <param name="source">源容差设置</param>
        public ToleranceSetting(ToleranceSetting source)
        {
            Copy(source);
        }

        public virtual XmlElement ToXmlElement(XmlDocument doc, string nodeName)
        {
            XmlElement element = doc.CreateElement(nodeName);
            if (PrimaryTolerance != null)
                element.AppendChild(PrimaryTolerance.ToXmlElement(doc, "PrimaryTolerance"));
            if (SecondaryTolerance != null)
                element.AppendChild(SecondaryTolerance.ToXmlElement(doc, "SecondaryTolerance"));
            if (TertiaryTolerance != null)
                element.AppendChild(TertiaryTolerance.ToXmlElement(doc, "TertiaryTolerance"));
            return element;
        }

        public virtual bool LoadXmlElement(XmlElement elem)
        {
            PrimaryTolerance = null;
            SecondaryTolerance = null;
            TertiaryTolerance = null;

            foreach(XmlNode node in elem.ChildNodes)
            {
                Tolerance tolerance = new Tolerance();
                if (string.Compare(node.Name, "PrimaryTolerance", true) == 0)
                {
                    if (tolerance.LoadXmlElement(node as XmlElement))
                        PrimaryTolerance = tolerance;
                }
                else if (string.Compare(node.Name, "SecondaryTolerance", true) == 0)
                {
                    if (tolerance.LoadXmlElement(node as XmlElement))
                        SecondaryTolerance = tolerance;
                }
                else if (string.Compare(node.Name, "TertiaryTolerance", true) == 0)
                {
                    if (tolerance.LoadXmlElement(node as XmlElement))
                        TertiaryTolerance = tolerance;
                }
            }
            return true;
        }

        /// <summary>
        /// 使用的容差项
        /// </summary>
        public ChromaticityField[] ItemKeys
        {
            get
            {
                if (PrimaryTolerance != null)
                    return PrimaryTolerance.ItemKeys;
                return null;
            }
            set
            {
                if (PrimaryTolerance != null)
                    PrimaryTolerance.ItemKeys = value;
                if (SecondaryTolerance != null)
                    SecondaryTolerance.ItemKeys = value;
                if (TertiaryTolerance != null)
                    TertiaryTolerance.ItemKeys = value;
            }
        }

        public virtual void Copy(ToleranceSetting source)
        {
            if (source.PrimaryTolerance != null)
                PrimaryTolerance = new Tolerance(source.PrimaryTolerance);
            if (source.SecondaryTolerance != null)
                SecondaryTolerance = new Tolerance(source.SecondaryTolerance);
            if (source.TertiaryTolerance != null)
                TertiaryTolerance = new Tolerance(source.TertiaryTolerance);
        }

        /// <summary>
        /// 获取主光源
        /// </summary>
        [DataMember]
        public Tolerance PrimaryTolerance { get; set; }
        /// <summary>
        /// 第二光源
        /// </summary>
        [DataMember]
        public Tolerance SecondaryTolerance { get; set; }
        /// <summary>
        /// 第三光源
        /// </summary>
        [DataMember]
        public Tolerance TertiaryTolerance { get; set; }
    }

    [DataContract]
    public class SystemToleranceSetting : ToleranceSetting
    {
        public SystemToleranceSetting() { }

        public SystemToleranceSetting(SystemToleranceSetting source)
            : base(source)
        {
            IsFromSystemDefault = source.IsFromSystemDefault;
        }

        /// <summary>
        /// 是否使用系统默认容差
        /// </summary>
        [DataMember]
        public bool IsFromSystemDefault { get; set; }

        public override XmlElement ToXmlElement(XmlDocument doc, string nodeName)
        {
            var element = base.ToXmlElement(doc, nodeName);
            element.SetAttribute("FromSystem", IsFromSystemDefault.ToString());
            return element;
        }

        public override bool LoadXmlElement(XmlElement element)
        {
            if (base.LoadXmlElement(element))
            {
                bool result;
                if (element.Attributes["FromSystem"] != null && bool.TryParse(element.Attributes["FromSystem"].Value, out result))
                {
                    IsFromSystemDefault = result;
                }
                return true;
            }
            return false;
        }

        public override void Copy(ToleranceSetting source)
        {
            base.Copy(source);
            SystemToleranceSetting tm = source as SystemToleranceSetting;
            if (tm != null)
            {
                IsFromSystemDefault = tm.IsFromSystemDefault;
            }
        }
    }
}