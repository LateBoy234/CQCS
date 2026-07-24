using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ThreeNH.ColorAlgorithm;
using ThreeNH.ColorSpecimen;
using ThreeNH.DataModel;
using ThreeNH.Optical;

namespace ThreeNH.ColorQualityControl.Adapter
{
   public  class SpecimenAdapterHelper
    {
        public SpecimenAdapterHelper()
        {
            JudgementSetting = new JudgementSetting();
        }

        public SpecimenAdapterHelper(
            Formula formula,
            ChromaticitySetting setting,
            SpecularComponent sc,
            StandardSpecimenAdapter standard = null,
            Tolerance tolerance = null,
            JudgementSetting judgementSetting = null,
            Formula referenceFormula1 = null,
            Formula referenceFormula2 = null)
        {
            Formula = formula;
            ChromaticitySetting = setting;
            SpecularComponent = sc;
            Standard = standard;
            Tolerance = tolerance;
            JudgementSetting = judgementSetting == null ? new JudgementSetting() : judgementSetting;
            ReferenceFormula1 = referenceFormula1;
            ReferenceFormula2 = referenceFormula2;
        }

        public JudgementSetting JudgementSetting { get; set; }
        public StandardSpecimenAdapter Standard { get; set; }
        public Formula Formula { get; set; }
        public ChromaticitySetting ChromaticitySetting { get; set; }
        private SpecularComponent _sc;
        public SpecularComponent SpecularComponent
        {
            get { return _sc; }
            set
            {
                if (value != SpecularComponent.SCI && value != SpecularComponent.SCE && value != SpecularComponent.Unknown)
                    throw new ArgumentException("SpecularComponent must be SCI or SCE");
                _sc = value;
            }
        }
        public Tolerance Tolerance { get; set; }
        public Formula ReferenceFormula1 { get; set; }
        public Formula ReferenceFormula2 { get; set; }

        public StandardSpecimenAdapter CreateAdapter(StandardSpecimen standard)
        {
            return new StandardSpecimenAdapter(standard, Formula, ChromaticitySetting, SpecularComponent, Tolerance);
        }

        public StandardSpecimenAdapter CreateAdapter(StandardSpecimen standard,Formula formula)
        {
            return new StandardSpecimenAdapter(standard, formula, ChromaticitySetting, SpecularComponent, Tolerance);
        }

        public TestSpecimenAdapter CreateAdapter(TestSpecimen test)
        {
            var adapter = new TestSpecimenAdapter(
                            test, Formula, ChromaticitySetting,
                            SpecularComponent,
                            Standard,
                            ReferenceFormula1, 
                            ReferenceFormula2);
            if (Tolerance != null && JudgementSetting != null)
                adapter.Judge(Standard, JudgementSetting);
            return adapter;
        }

        public TestSpecimenAdapter CreateAdapter(TestSpecimen test,Formula formula)
        {
            var adapter = new TestSpecimenAdapter(
                            test, formula, ChromaticitySetting,
                            SpecularComponent,
                            Standard,
                            ReferenceFormula1,
                            ReferenceFormula2);
            if (Tolerance != null && JudgementSetting != null)
                adapter.Judge(Standard, JudgementSetting);
            return adapter;
        }

        public SpecimenAdapter CreateAdapter(Specimen specimen)
        {
            if (specimen is StandardSpecimen)
                return CreateAdapter(specimen as StandardSpecimen);
            else if (specimen is TestSpecimen)
                return CreateAdapter(specimen as TestSpecimen);
            else
                throw new ArgumentException("specimen must be StandardSpecimen or TestSpecimen");
        }
    }
}
