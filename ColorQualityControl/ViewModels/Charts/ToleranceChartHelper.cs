using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using ThreeNH.ColorAlgorithm;
using ThreeNH.ColorLibrary;
using ThreeNH.ColorQualityControl.Adapter;
using ThreeNH.ColorQualityControl.UI.Charts;
using ThreeNH.ColorQualityControl.ViewModels;
using ThreeNH.ColorSpecimen;
using ThreeNH.DataModel;
using ThreeNH.Optical;

namespace ThreeNH.ColorQualityControl.ViewModels.Charts
{
    /// <summary>
    /// 在 ContentControl 中显示容差色平图（ToleranceChartViewModel + TriaxialChartView）。
    /// </summary>
    public static class ToleranceChartHelper
    {
        public static void Show(
            ContentControl host,
            StandardIlluminant illuminant,
            StandardObserver observer,
            SpecularComponent specularComponent,
            Specimen standard,
            IEnumerable<Specimen> samples = null,
            Tolerance tolerance = null)
        {
            if (host == null)
                return;

            host.Content = CreateChartView(illuminant, observer, specularComponent, standard, samples, tolerance);
        }

        /// <summary>
        /// 创建容差色平图视图，供 Word 报告等场景嵌入 Grid 导出。
        /// </summary>
        public static TriaxialChartView CreateChartView(
            StandardIlluminant illuminant,
            StandardObserver observer,
            SpecularComponent specularComponent,
            Specimen standard,
            IEnumerable<Specimen> samples = null,
            Tolerance tolerance = null)
        {
            IColorData standardData = standard != null
                ? new ChartColorData(illuminant, observer, specularComponent, standard)
                : null;

            var sampleData = (samples ?? Enumerable.Empty<Specimen>())
                .Where(s => s != null)
                .Select(s => (IColorData)new ChartColorData(illuminant, observer, specularComponent, s))
                .ToList();

            return CreateChartView(standardData, sampleData, tolerance);
        }

        /// <summary>
        /// 基于报告适配器创建容差色平图，与界面左侧容差图使用同一套 ViewModel。
        /// </summary>
        public static TriaxialChartView CreateChartView(
            StandardSpecimenAdapter standardAdapter,
            IEnumerable<TestSpecimenAdapter> sampleAdapters,
            Tolerance tolerance = null)
        {
            IColorData standardData = standardAdapter != null
                ? new ChartColorData(
                    standardAdapter.Formula.Illuminant,
                    standardAdapter.Formula.Observer,
                    standardAdapter.SpecularComponent,
                    standardAdapter.Specimen)
                : null;

            var sampleData = (sampleAdapters ?? Enumerable.Empty<TestSpecimenAdapter>())
                .Where(s => s != null)
                .Select(s => (IColorData)new ChartColorData(
                    s.Formula.Illuminant,
                    s.Formula.Observer,
                    s.SpecularComponent,
                    s.Specimen))
                .ToList();

            return CreateChartView(standardData, sampleData, tolerance ?? standardAdapter?.Tolerance);
        }

        static TriaxialChartView CreateChartView(
            IColorData standardData,
            IList<IColorData> sampleData,
            Tolerance tolerance)
        {
            var view = new TriaxialChartView { Margin = new Thickness(6) };
            var model = ToleranceChartViewModel.CreateLab();
            model.IsAutoFitAxes = true;
            model.IsXyBackgroundVisible = false;
            model.SetData(standardData, sampleData, tolerance);
            view.DataContext = model;
            return view;
        }

        public static Specimen CreateSpecimenFromLab(
            CIELAB lab, StandardIlluminant illuminant, StandardObserver observer, SpecularComponent sc)
        {
            var specimen = new Specimen();
            var colorData = new ColorData(illuminant, observer, lab);
            AssignColorData(specimen, colorData, sc);
            return specimen;
        }

        public static Specimen CreateSpecimenFromLab(
            CIELAB lab, StandardIlluminant illuminant, StandardObserver observer, string specCom)
        {
            return CreateSpecimenFromLab(lab, illuminant, observer, ParseSpecularComponent(specCom));
        }

        public static Specimen CreateSpecimenFromColorData(
            byte[] colorDataBytes, SpecularComponent sc, string name = null)
        {
            if (colorDataBytes == null)
                return null;

            var clrs = ColorDatabase.ToColorData(colorDataBytes);
            var idx = (int)(sc == SpecularComponent.SCIAndSCE ? SpecularComponent.SCI : sc);
            var clr = idx >= 0 && idx < clrs.Length ? clrs[idx] : null;
            clr = clr ?? clrs.FirstOrDefault(c => c != null);
            if (clr == null)
                return null;

            var specimen = new Specimen { Name = name };
            AssignColorData(specimen, clr, sc);
            return specimen;
        }

        public static SpecularComponent ParseSpecularComponent(string specCom)
        {
            if (specCom == "SCE")
                return SpecularComponent.SCE;
            if (specCom == "SCI")
                return SpecularComponent.SCI;
            return SpecularComponent.Unknown;
        }

        static void AssignColorData(Specimen specimen, ColorData colorData, SpecularComponent sc)
        {
            if (sc == SpecularComponent.SCI)
                specimen.SCI = colorData;
            else if (sc == SpecularComponent.SCE)
                specimen.SCE = colorData;
            else
                specimen.Clr = colorData;
        }
    }
}
