using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Xml;
using ThreeNH.ColorSpecimen;

namespace ThreeNH.DataModel
{
    [DataContract(Name = "StandardSpecimen", Namespace = "ThreeNH.DataModel")]
    public class StandardSpecimen : Specimen
    {
        public StandardSpecimen() { }
        /// <summary>
        /// 从已有的样品数据构造一个标样；会直接引用样品的颜色数据
        /// </summary>
        /// <param name="specimen"></param>
        public StandardSpecimen(Specimen specimen) : base(specimen) { }
        /// <summary>
        /// 容差设置，包括主容差和两个参考容差
        /// </summary>
        [DataMember]
        public ToleranceSetting ToleranceSetting { get; set; }
        /// <summary>
        /// 显示给用户的色品和色差属性
        /// </summary>
        public ChromaticitySetting ChromaticitySetting { get; set; }
        /// <summary>
        /// 
        /// 
        /// 关联的试样
        /// </summary>
        [DataMember]
        public ICollection<TestSpecimen> TestSpecimens { get; set; }
        /// <summary>
        /// 用户备注信息
        /// </summary>
        [DataMember]
        public string Comment { get; set; }
        /// <summary>
        /// 颜色配方
        /// </summary>
        [DataMember]
        public string ColorFormula { get; set; }



        public override XmlElement ToXmlElement(XmlDocument doc, string nodeName)
        {
            var element = base.ToXmlElement(doc, nodeName);

            if (ToleranceSetting != null)
            {
                element.AppendChild(ToleranceSetting.ToXmlElement(doc, "ToleranceSetting"));
            }
            if (ChromaticitySetting != null)
            {
                element.AppendChild(ChromaticitySetting.ToXmlElement(doc, "ChromaticitySetting"));
            }
            if (Comment != null && Comment.Length > 0)
            {
                var nodeComment = doc.CreateElement("Comment");
                element.AppendChild(nodeComment);
                nodeComment.AppendChild(doc.CreateTextNode(Comment));
            }
            if (ColorFormula != null && ColorFormula.Length > 0)
            {
                var nodeFormula = doc.CreateElement("ColorFormula");
                element.AppendChild(nodeFormula);
                nodeFormula.AppendChild(doc.CreateTextNode(Comment));
            }

            return element;
        }

        public override bool LoadXmlElement(XmlElement stdNode)
        {
            if (!base.LoadXmlElement(stdNode))
                return false;

            ToleranceSetting = null;
            ChromaticitySetting = null;
            Comment = null;
            ColorFormula = null;

            foreach(XmlElement child in stdNode.ChildNodes)
            {
                if (string.Compare(child.Name, "ToleranceSetting", true) == 0)
                {
                    ToleranceSetting tolSetting = new ToleranceSetting();
                    if (tolSetting.LoadXmlElement(child))
                        ToleranceSetting = tolSetting;
                }
                else if (string.Compare(child.Name, "ChromaticitySetting", true) == 0)
                {
                    ChromaticitySetting setting = new ChromaticitySetting();
                    if (setting.LoadXmlElement(child))
                        ChromaticitySetting = setting;
                }
                else if (string.Compare(child.Name, "Comment", true) == 0)
                {
                    Comment = child.InnerText;
                }
                else if (string.Compare(child.Name, "ColorFormula", true) == 0)
                {
                    ColorFormula = child.InnerText;
                }
            }

            return true;
        }
    }
}
