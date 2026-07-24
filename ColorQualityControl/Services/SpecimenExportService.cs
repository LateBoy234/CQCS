using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Data;
using ThreeNH.ColorAlgorithm;
using ThreeNH.ColorQualityControl.Adapter;
using ThreeNH.ColorSpecimen;
using ThreeNH.DataModel;
using ThreeNH.Helper;
using ThreeNH.Optical;

namespace ThreeNH.ColorQualityControl.Services
{
    /// <summary>
    /// 标试样表格导出（行/列构建与 SCI/SCE 展开）；对话框与 Command 仍留 MainWindow（阶段4-T03）。
    /// </summary>
    internal sealed class SpecimenExportService
    {
        private readonly SpecimenDatabase _database;

        public SpecimenExportService(SpecimenDatabase database)
        {
            _database = database ?? throw new ArgumentNullException(nameof(database));
        }

        /// <summary>
        /// 判断导出时是否需要按 SCI/SCE 拆分多行（Excel 且显示字段已勾选测光方式时）。
        /// </summary>
        public static bool ShouldExpandSpecularComponents(IMyWriter writer, SpecimenAdapterHelper helper)
        {
            return writer is MyExcelWriter
                   && helper.ChromaticitySetting.DisplayFields.Contains(ChromaticityField.SpecularComponent);
        }

        /// <summary>
        /// 获取单个样品应导出的测光方式列表（有 SCI/SCE 则分别导出，仅有 Clr 时导出 Unknown）。
        /// </summary>
        public static IEnumerable<SpecularComponent> GetExportSpecularComponents(Specimen specimen)
        {
            if (specimen.SCI != null)
                yield return SpecularComponent.SCI;
            if (specimen.SCE != null)
                yield return SpecularComponent.SCE;
            if (specimen.SCI == null && specimen.SCE == null && specimen.Clr != null)
                yield return SpecularComponent.Unknown;
        }

        /// <summary>
        /// 将一个样品写入导出行；Excel 且已选测光方式显示字段时按 SCI/SCE 拆成多行。
        /// </summary>
        public void WriteSpecimenRows(
            IMyWriter myWriter,
            SpecimenAdapterHelper helper,
            Specimen specimen,
            bool withTest,
            bool isAbsolute = true,
            StandardSpecimen standardForTests = null)
        {
            if (!ShouldExpandSpecularComponents(myWriter, helper))
            {
                myWriter.WriteRow(MakeRowItems(helper, specimen, withTest, isAbsolute));
                return;
            }

            var exportModes = GetExportSpecularComponents(specimen).ToArray();
            if (exportModes.Length == 0)
            {
                myWriter.WriteRow(MakeRowItems(helper, specimen, withTest, isAbsolute));
                return;
            }

            var savedSpecularComponent = helper.SpecularComponent;
            var savedStandard = helper.Standard;
            try
            {
                foreach (var sc in exportModes)
                {
                    helper.SpecularComponent = sc;
                    if (standardForTests != null)
                    {
                        helper.Standard = helper.CreateAdapter(standardForTests);
                        if (helper.Tolerance == null)
                        {
                            helper.Tolerance = _database.GetTolerance(standardForTests,
                                new ObserverIlluminantPair(helper.Formula.Observer, helper.Formula.Illuminant));
                        }
                    }

                    var adapter = helper.CreateAdapter(specimen);
                    myWriter.WriteRow(MakeRowItems(helper, adapter, withTest, isAbsolute));
                }
            }
            finally
            {
                helper.SpecularComponent = savedSpecularComponent;
                helper.Standard = savedStandard;
            }
        }

        /// <summary>根据色度显示字段生成导出表头列名数组。</summary>
        public string[] MakeColumnLabels(SpecimenAdapterHelper helper, bool withTest)
        {
            List<string> labels = new List<string>();

            foreach (var field in helper.ChromaticitySetting.DisplayFields)
            {
                if (!withTest && field.IsNeedStandard())
                    continue;

                if (field.IsReflectance())
                {
                    labels.Add(string.Format("{0} nm", field.ToWavelength()));
                }
                else if (field == ChromaticityField.MI1 && helper.ReferenceFormula1 != null)
                {
                    labels.Add(SpecimenAdapter.MakeMIName(helper.ReferenceFormula1));
                }
                else if (field == ChromaticityField.MI2 && helper.ReferenceFormula2 != null)
                {
                    labels.Add(SpecimenAdapter.MakeMIName(helper.ReferenceFormula2));
                }
                else
                {
                    labels.Add(field.Name());
                }
            }

            return labels.ToArray();
        }

        public string[] MakeRowItems(SpecimenAdapterHelper helper, Specimen specimen, bool withTest, bool isAbsolute = true)
        {
            var adapter = helper.CreateAdapter(specimen);
            return MakeRowItems(helper, adapter, withTest, isAbsolute);
        }

        public string[] MakeRowItems(SpecimenAdapterHelper helper, SpecimenAdapter adapter, bool withTest, bool isAbsolute = true)
        {
            List<string> items = new List<string>();

            IValueConverter converter;
            foreach (var field in helper.ChromaticitySetting.DisplayFields)
            {
                if (!withTest && field.IsNeedStandard())
                {
                    if (isAbsolute)
                    {
                        if (field == ChromaticityField.ColorOffset)
                        {
                            items.Add(S._("ColorOffset_None"));
                            continue;
                        }
                        items.Add("--");
                    }
                    continue;
                }

                if (adapter is TestSpecimenAdapter)
                {
                    converter = TestSpecimenAdapter.GetPropertyValueConverter(field);
                }
                else
                {
                    converter = SpecimenAdapter.GetPropertyValueConverter(field);
                }

                if (field.IsAbsorbanceField())
                {
                    if (adapter.Spectrum != null)
                    {
                        int wavelength = field.ToWavelength();
                        var multiConverter = new AbsorbanceConverter();
                        var paras = new object[] { adapter.Spectrum, adapter.Specimen.IsTransmission };
                        items.Add(multiConverter.Convert(paras, null, wavelength, CultureInfo.CurrentCulture) as string);
                    }
                }
                else if (field.IsReflectance() || field.IsAbsoluteStrengthField())
                {
                    if (adapter.Spectrum != null)
                    {
                        int wavelength = field.ToWavelength();
                        items.Add(converter.Convert(adapter.Spectrum, typeof(string), wavelength,
                            CultureInfo.CurrentCulture) as string);
                    }
                }
                else if (field == ChromaticityField.Judgement)
                {
                    var testAdapter = adapter as TestSpecimenAdapter;
                    testAdapter?.Judge(helper.Standard, helper.JudgementSetting, helper.Tolerance);
                    if (testAdapter != null && testAdapter.Judgement != null)
                    {
                        items.Add(testAdapter.Judgement.TotalJudgement.Description());
                    }
                    else
                    {
                        items.Add("--");
                    }
                }
                else
                {
                    items.Add(adapter.GetFormatedPropertyValue(field));
                }
            }

            return items.ToArray();
        }

        /// <summary>导出单个标样及其下属试样。</summary>
        public void ExportStandardWithTests(
            IMyWriter myWriter,
            StandardSpecimen standard,
            SpecimenAdapterHelper helper,
            bool withHeader)
        {
            if (withHeader)
            {
                myWriter.WriteRow(MakeColumnLabels(helper, true));
            }

            WriteSpecimenRows(myWriter, helper, standard, true);
            if (standard.TestSpecimens != null && standard.TestSpecimens.Count > 0)
            {
                helper.Tolerance = _database.GetTolerance(standard,
                    new ObserverIlluminantPair(helper.Formula.Observer, helper.Formula.Illuminant));
                foreach (var test in standard.TestSpecimens)
                {
                    WriteSpecimenRows(myWriter, helper, test, true, standardForTests: standard);
                }
            }
        }

        /// <summary>使用统一的色度设置导出数据库中全部标样、试样及孤立试样。</summary>
        public void ExportAll(ChromaticitySetting setting, IMyWriter myWriter, SpecimenAdapterHelper helper)
        {
            myWriter.WriteRow(MakeColumnLabels(helper, true));
            foreach (var std in _database.StandardSpecimens)
            {
                ExportStandardWithTests(myWriter, std, helper, false);
                myWriter.WriteRow(null);
            }

            foreach (var test in _database.OrphanTests)
            {
                WriteSpecimenRows(myWriter, helper, test, false);
            }
        }

        /// <summary>按各标样各自的色度设置分别导出全部数据。</summary>
        public void ExportAllByStandardSetting(IMyWriter myWriter, SpecimenAdapterHelper helper)
        {
            foreach (var standard in _database.StandardSpecimens)
            {
                helper.ChromaticitySetting = _database.GetChromaticitySetting(standard);
                ExportStandardWithTests(myWriter, standard, helper, true);
                myWriter.WriteRow(null);
            }

            var orphanTests = _database.OrphanTests;
            if (orphanTests.Length > 0)
            {
                helper.Standard = null;
                helper.ChromaticitySetting = _database.SystemChromaticitySetting;
                myWriter.WriteRow(MakeColumnLabels(helper, false));
                foreach (var test in _database.OrphanTests)
                {
                    WriteSpecimenRows(myWriter, helper, test, false);
                }
            }
        }

        /// <summary>如果所有的标样都使用相同的 ChromaticitySetting 返回真。</summary>
        public bool IsStandardsHasSameChromaticitySetting()
        {
            if (_database.IsUseSystemTolerance)
                return true;

            ChromaticitySetting setting = _database.SystemChromaticitySetting;
            foreach (var std in _database.StandardSpecimens)
            {
                if (!AreDisplayFieldsEqual(setting.DisplayFields, _database.GetChromaticitySetting(std).DisplayFields))
                    return false;
            }

            return true;
        }

        private static bool AreDisplayFieldsEqual(
            IEnumerable<ChromaticityField> fields1,
            IEnumerable<ChromaticityField> fields2)
        {
            HashSet<ChromaticityField> set1 = new HashSet<ChromaticityField>(fields1);
            HashSet<ChromaticityField> set2 = new HashSet<ChromaticityField>(fields2);
            return set1.SetEquals(set2);
        }
    }
}
