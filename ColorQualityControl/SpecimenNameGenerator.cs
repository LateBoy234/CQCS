using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using ThreeNH.ColorSpecimen;

namespace ThreeNH.ColorQualityControl
{
    public class SpecimenNameGenerator
    {
        public const string INDEX = "INDEX";
        public const string INSTRUMENT = "INSTRUMENT";
        public const string MATERIAL = "MATERIAL";
        public const string SPECULARCOMPONENT = "SPECULARCOMPONENT";
        public const string UV = "UV";
        public const string DATETIME = "DATETIME";
        static readonly string[] _variables = new string[]
        {
            "Index", "Instrument", "Material", "SpecularComponent", "UV", "DateTime"
        };

        NameGenerator nameGenerator = new NameGenerator();

        public static string[] Variables
        {
            get { return _variables; }
        }

        /// <summary>
        /// 默认构造函数
        /// </summary>
        public SpecimenNameGenerator()
        {
            nameGenerator.VariableEvaluator = VariableEvaluator;
            //Index = 1;
            Rule = "S{Index}-{DateTime}";
        }
        /// <summary>
        /// 使用一个命名规则构造样品命名器
        /// </summary>
        /// <param name="rule"></param>
        public SpecimenNameGenerator(string rule)
        {
            nameGenerator.VariableEvaluator = VariableEvaluator;
            //Index = 1;
            Rule = rule;
        }

        public virtual XmlElement ToXmlElement(XmlDocument doc, string nodeName)
        {
            var node = doc.CreateElement(nodeName);
            node.SetAttribute("Rule", Rule);
            //node.SetAttribute("Index", Index.ToString());
            return node;
        }

        public virtual bool LoadXmlElement(XmlElement node)
        {
            if (node.Attributes["Rule"] != null && !string.IsNullOrWhiteSpace(node.Attributes["Rule"].Value))
            {
                Rule = node.Attributes["Rule"].Value;
            }

            return true;
        }

        /// <summary>
        /// 命名规则
        /// </summary>
        public string Rule
        {
            get { return nameGenerator.Rule; }
            set { nameGenerator.Rule = value; }
        }

        private int Index
        {
            get; set;
        }
        //public int NewIndex
        //{
        //    get
        //    {
        //        int index = Index;
        //        Index += 1;
        //        return index;
        //    }
        //}

        /// <summary>
        /// 根据命名规则为样品生成名字
        /// </summary>
        /// <param name="specimen"></param>
        /// <returns></returns>
        public string GenerateName(Specimen specimen, int index)
        {
            Index = index;
            string newName = nameGenerator.GenerateName(specimen);
            //Index += 1;

            return newName;
        }

        string VariableEvaluator(string variable, string format, object parameter)
        {
            Specimen specimen = parameter as Specimen;

            switch(variable.ToUpper())
            {
                case INDEX: return Index.ToString();
                case DATETIME:
                    if (string.IsNullOrWhiteSpace(format))
                        return specimen.DateTime.ToString("yyMMddhhmmss");
                    else
                        return specimen.DateTime.ToString(format);
                case MATERIAL: return specimen.Material;
                case INSTRUMENT: return specimen.Instrument;
                case SPECULARCOMPONENT: return specimen.SpecularComponent.ToString();
                case UV: return specimen.UV == Optical.UVFlag.On ? "UV" : "";
                default: return null;
            }
        }
    }
}
