using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Xml;
using ThreeNH.ColorSpecimen;

namespace ThreeNH.DataModel
{
    [DataContract(Name = "TestSpecimen", Namespace = "ThreeNH.DataModel")]
    public class TestSpecimen : Specimen
    {
        public TestSpecimen() { }
        public TestSpecimen(Specimen specimen) : base(specimen) { }

        private StandardSpecimen standard;
        /// <summary>
        /// 试样所属标样；当设置标样时，会自动将试样从其以前的标样移出并加入新的标样的试样列表中
        /// </summary>
        public StandardSpecimen StandardSpecimen
        {
            get { return standard; }
            set
            {
                if (standard == value)
                {
                    return;
                }

                if (standard != null)
                {
                    standard.TestSpecimens.Remove(this);
                }

                standard = value;
                if (standard != null)
                {
                    if (standard.TestSpecimens == null)
                    {
                        standard.TestSpecimens = new List<TestSpecimen>();
                    }
                    standard.TestSpecimens.Add(this);
                }
            }
        }
    }
}