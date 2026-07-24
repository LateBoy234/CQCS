using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using ThreeNH.ColorAlgorithm;
using ThreeNH.ColorQualityControl.Adapter;
using ThreeNH.ColorQualityControl.Refactoring.Contracts;
using ThreeNH.ColorQualityControl.UI.Charts;
using ThreeNH.ColorQualityControl.ViewModels;
using ThreeNH.ColorQualityControl.ViewModels.Charts;
using ThreeNH.ColorSpecimen;
using ThreeNH.DataModel;
using ThreeNH.Optical;

namespace ThreeNH.ColorQualityControl.Presentation
{
    /// <summary>
    /// 色品图、容差图与光谱/吸光度图刷新；从 MainWindow 迁出以便阶段2 统一协调（2-T06）。
    /// </summary>
    internal sealed class ChartCoordinator
    {
        private readonly IChartHost _host;

        public ChartCoordinator(IChartHost host)
        {
            _host = host ?? throw new ArgumentNullException(nameof(host));
        }

        public void RefreshCharts(Specimen test = null)
        {
            SetToleranceChartView(test);
            SetTriaxialChartView(test);
        }

        public void RefreshCharts(IList<Specimen> tests)
        {
            SetToleranceChartView(tests);
            SetTriaxialChartView(tests);
        }

        public void SetToleranceChartView(Specimen test = null)
        {
            SetToleranceChartView(test != null ? new List<Specimen> { test } : null);
        }

        public void SetToleranceChartView(IList<Specimen> tests)
        {
            var view = new TriaxialChartView { Margin = new Thickness(6) };

            ToleranceChartViewModel toleranceModel = ToleranceChartViewModel.CreateLab();
            toleranceModel.IsAutoFitAxes = true;
            toleranceModel.IsXyBackgroundVisible = false;

            var tolerance = _host.SingleStandardInShowing != null
                ? _host.Database.GetTolerance(
                    _host.SingleStandardInShowing,
                    new ObserverIlluminantPair(_host.Observer, _host.Illuminant))
                : null;

            if (tests == null)
            {
                toleranceModel.SetData(_host.StandardColorData, _host.SampleColorDatas, tolerance);
            }
            else
            {
                var sampleData = tests
                    .Where(t => t != null)
                    .Select(t => (IColorData)new ChartColorData(_host.Illuminant, _host.Observer, _host.SpecularComponent, t))
                    .ToList();
                toleranceModel.SetData(_host.StandardColorData, sampleData, tolerance);
            }

            view.DataContext = toleranceModel;
            _host.ToleranceChartView.Content = view;
        }

        public void SetTriaxialChartView(Specimen test = null)
        {
            SetTriaxialChartView(test != null ? new List<Specimen> { test } : null);
        }

        public void SetTriaxialChartView(IList<Specimen> tests)
        {
            var view = new TriaxialChartView { Margin = new Thickness(20) };

            ColorChartViewModel colorModel;
            switch (_host.ChromaticitySetting.ColorSpace)
            {
                case ColorSpace.CIE_LAB:
                case ColorSpace.DIN99_LAB:
                case ColorSpace.HUNTER_LAB:
                    colorModel = new LabChartViewModel();
                    break;

                case ColorSpace.CIE_XYZ:
                case ColorSpace.CIE_Yxy:
                    colorModel = new XyzChartViewModel();
                    break;

                case ColorSpace.CIE_LCH:
                case ColorSpace.DIN99_LCh:
                    colorModel = new LchChartViewModel();
                    break;

                default:
                    colorModel = new LabChartViewModel();
                    break;
            }

            if (colorModel.Observer != _host.Observer)
            {
                colorModel.Observer = _host.Observer;
            }

            colorModel.IsAutoFitAxes = false;
            colorModel.IsXyBackgroundVisible = true;

            if (tests == null)
            {
                colorModel.SetData(_host.StandardColorData, _host.SampleColorDatas);
            }
            else
            {
                var sampleData = tests
                    .Where(t => t != null)
                    .Select(t => (IColorData)new ChartColorData(_host.Illuminant, _host.Observer, _host.SpecularComponent, t))
                    .ToList();
                colorModel.SetData(_host.StandardColorData, sampleData);
            }

            view.DataContext = colorModel;
            _host.TriaxialChartView.Content = view;
        }

        public void RefreshSpectrumPlot()
        {
            _host.SpectrumPlot.Clear();
            RefreshSpectrumPlotStandards();
            RefreshSpectrumPlotTests();
        }

        public void RefreshAbsorbancePlot()
        {
            _host.AbsorbancePlot.Clear();
            RefreshAbsorbancePlotStandards();
            RefreshAbsorbancePlotTests();
        }

        private void RefreshSpectrumPlotTests()
        {
            var plot = _host.SpectrumPlot;
            plot.BeginInit();
            plot.RemoveAllTests();

            if (!_host.TestsToShow.Any())
            {
                plot.EndInit();
                return;
            }

            foreach (var test in _host.TestsToShow)
            {
                if (test.Spectrum != null)
                {
                    plot.AddTest(test.Spectrum, test.Name);
                }
            }

            plot.EndInit();
        }

        private void RefreshAbsorbancePlotTests()
        {
            var plot = _host.AbsorbancePlot;
            plot.BeginInit();
            plot.RemoveAllTests();

            if (!_host.TestsToShow.Any())
            {
                plot.EndInit();
                return;
            }

            foreach (var test in _host.TestsToShow)
            {
                if (test.Spectrum != null && test.Specimen.IsTransmission)
                {
                    plot.AddTest(test.Spectrum, test.Name);
                }
            }

            plot.EndInit();
        }

        private void RefreshAbsorbancePlotStandards()
        {
            var plot = _host.AbsorbancePlot;
            plot.BeginInit();
            plot.RemoveAllStandards();

            if (!_host.StandardsToShow.Any())
            {
                plot.EndInit();
                return;
            }

            foreach (var std in _host.StandardsToShow)
            {
                if (std.Spectrum != null && std.Specimen.IsTransmission)
                {
                    plot.AddStandard(std.Spectrum, std.Name);
                }
            }

            plot.EndInit();
        }

        private void RefreshSpectrumPlotStandards()
        {
            var plot = _host.SpectrumPlot;
            plot.BeginInit();
            plot.RemoveAllStandards();

            if (!_host.StandardsToShow.Any())
            {
                plot.EndInit();
                return;
            }

            foreach (var std in _host.StandardsToShow)
            {
                if (std.Spectrum != null)
                {
                    plot.AddStandard(std.Spectrum, std.Name);
                }
            }

            plot.EndInit();
        }
    }
}
