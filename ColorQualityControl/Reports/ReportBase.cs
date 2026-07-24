using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using ThreeNH.ColorAlgorithm;
using ThreeNH.ColorQualityControl.Adapter;
using ThreeNH.ColorQualityControl.UI;
using ThreeNH.ColorSpecimen;
using ThreeNH.DataModel;
using ThreeNH.Helper;
using ThreeNH.Optical;

namespace ThreeNH.ColorQualityControl.Reports
{
    abstract class ReportBase
    {
        protected class Item
        {
            public Formula Formula { get; set; }
            public Tolerance Tolerance { get; set; }
            public StandardSpecimenAdapter StdAdapter { get; set; }
            public TestSpecimenAdapter TestAdapter => TestAdapters.FirstOrDefault();

            public List<TestSpecimenAdapter> TestAdapters { get; } = new List<TestSpecimenAdapter>();
        }

        public ReportSetting ReportSetting { get; set; }

        public StandardSpecimen Standard { get; set; }
        public ChromaticitySetting ChromaticitySetting { get; set; }
        public Formula Formula { get; set; }
        public SpecularComponent SpecularComponent { get; set; }
        public Tolerance Tolerance { get; set; }
        public Formula ReferenceFormula1 { get; set; }
        public Formula ReferenceFormula2 { get; set; }
        public string InstrumentModel { get; set; }
        public DeltaPlotType[] VisibleDeltaPlots { get; set; }

        public ToleranceSetting ToleranceSetting { get; set; }
        public ObserverAndIlluminantSetting ObserverAndIlluminantSetting { get; set; }

        protected string GetMeasurementCondition(Specimen specimen)
        {
            List<string> conditions = new List<string>();
            if (!string.IsNullOrWhiteSpace(specimen.OpticalStruct))
            {
                conditions.Add(specimen.OpticalStruct);
            }

            if (specimen.MeasurementCaliber != MeasurementCaliber.Unknown)
            {
                if (specimen.Instrument == "MCR10")
                {
                    specimen.MeasurementCaliber= MeasurementCaliber.Phi1;
                }
                else if (specimen.Instrument == "BTS20")
                {
                    specimen.MeasurementCaliber= MeasurementCaliber.Phi50;
                }
                conditions.Add(specimen.MeasurementCaliber.Description());
            }

            if (SpecularComponent != SpecularComponent.Unknown)
            {
                conditions.Add(SpecularComponent.Description());
            }

            conditions.Add("UV " + specimen.UV.Description());

            return string.Join(", ", conditions);
        }

        protected SpecimenAdapterHelper GetAdapterHelper()
        {
            SpecimenAdapterHelper adapterHelper = new SpecimenAdapterHelper();
            adapterHelper.ChromaticitySetting = ChromaticitySetting;
            adapterHelper.Formula = Formula;
            adapterHelper.Tolerance = Tolerance;
            adapterHelper.ReferenceFormula1 = ReferenceFormula1;
            adapterHelper.ReferenceFormula2 = ReferenceFormula2;
            adapterHelper.SpecularComponent = SpecularComponent;
            return adapterHelper;
        }
        /// <summary>
        /// 打印字段属性转换
        /// </summary>
        /// <param name="field"></param>
        /// <returns></returns>
        protected string GetFieldName(ChromaticityField field)
        {
            switch (field)
            {
                case ChromaticityField.dE00:
                    return string.Format("{0}({1}:{2}:{3})", field.Description(), Tolerance.KL00, Tolerance.KC00, Tolerance.KH00);
                case ChromaticityField.dE94:
                    return string.Format("{0}({1}:{2}:{3})", field.Description(), Tolerance.KL94, Tolerance.KC94, Tolerance.KH94);
                case ChromaticityField.dEcmc:
                    return string.Format("{0}", field.Name());
                 // return string.Format("{0}({1}:{2})", field.Description(), Tolerance.KLcmc, Tolerance.KCcmc);
                default:
                    return field.Name();
            }
        }

        protected string[] FieldsToExportNames(ChromaticityField[] fields)
        {
            List<string> list = new List<string>();
            foreach (var field in fields)
            {
                switch (field)
                {
                    case ChromaticityField.MI1:
                        list.Add(ReferenceFormula1 != null ? SpecimenAdapter.MakeMIName(ReferenceFormula1) : "MI1");
                        break;
                    case ChromaticityField.MI2:
                        list.Add(ReferenceFormula2 != null ? SpecimenAdapter.MakeMIName(ReferenceFormula2) : "MI2");
                        break;
                    default:
                        list.Add(GetFieldName(field));
                        break;
                }
            }
            return list.ToArray();
        }

        protected string[] FieldsToPropertyNames(ChromaticityField[] fields)
        {
            return fields.Select(x => x.ToString()).ToArray();
        }

        protected string GetFormulaInfo(Formula formula)
        {
            return string.Format("{0}/{1}", formula.Illuminant.Description(), formula.Observer.Description());
        }

        protected string AddFormulaInfo(string src, Formula formula)
        {
            return string.Format("{0}({1}/{2})", src, formula.Illuminant.Description(), formula.Observer.Description());
        }

        protected string AddSpecularComponent(string src, SpecularComponent sc)
        {
            return string.Format("{0}[{1}]", src, sc.Description());
        }

        protected List<Item> GetReportItems(TestSpecimen test)
        {
            return GetReportItems(new[] { test });
        }

        protected List<Item> GetReportItems(IEnumerable<TestSpecimen> tests)
        {
            List<Item> items = new List<Item>(3);

            if (ReportSetting.IsExportReferences)
            {
                Item item = new Item
                {
                    Formula = new Formula(ObserverAndIlluminantSetting.PrimaryIlluminant,
                        ObserverAndIlluminantSetting.PrimaryObserver),
                    Tolerance = ToleranceSetting.PrimaryTolerance,
                };
                item.StdAdapter = new StandardSpecimenAdapter(Standard, item.Formula, ChromaticitySetting,
                    SpecularComponent, item.Tolerance);

                items.Add(item);

                if (ToleranceSetting.SecondaryTolerance != null && ObserverAndIlluminantSetting.Secondary.IsValid)
                {
                    item = new Item
                    {
                        Formula = new Formula(ObserverAndIlluminantSetting.SecondaryIlluminant,
                            ObserverAndIlluminantSetting.SecondaryObserver),
                        Tolerance = ToleranceSetting.SecondaryTolerance,
                    };
                    item.StdAdapter = new StandardSpecimenAdapter(Standard, item.Formula, ChromaticitySetting,
                        SpecularComponent, item.Tolerance);
                    items.Add(item);
                }

                if (ToleranceSetting.TertiaryTolerance != null && ObserverAndIlluminantSetting.Tertiary.IsValid)
                {
                    item = new Item
                    {
                        Formula = new Formula(ObserverAndIlluminantSetting.TertiaryIlluminant,
                            ObserverAndIlluminantSetting.TertiaryObserver),
                        Tolerance = ToleranceSetting.TertiaryTolerance,
                    };
                    item.StdAdapter = new StandardSpecimenAdapter(Standard, item.Formula, ChromaticitySetting,
                        SpecularComponent, item.Tolerance);
                    items.Add(item);
                }
            }
            else
            {
                Item item = new Item
                {
                    Formula = Formula,
                    Tolerance = Tolerance,
                };
                item.StdAdapter = new StandardSpecimenAdapter(Standard, item.Formula, ChromaticitySetting,
                    SpecularComponent, item.Tolerance);
                items.Add(item);
            }

            foreach (var item in items)
            {
                foreach (var test in tests)
                {
                    var testAdapter = new TestSpecimenAdapter(test, item.Formula, ChromaticitySetting,
                        SpecularComponent, item.StdAdapter, ReferenceFormula1, ReferenceFormula2);
                    testAdapter.Judge(item.StdAdapter, new JudgementSetting());
                    item.TestAdapters.Add(testAdapter);
                }
            }

            return items;
        }
    }
}
