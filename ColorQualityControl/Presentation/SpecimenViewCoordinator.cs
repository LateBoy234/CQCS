using System;
using System.Linq;
using System.Windows.Controls;
using ThreeNH.ColorQualityControl.Adapter;
using ThreeNH.ColorQualityControl.Refactoring.Contracts;
using ThreeNH.ColorSpecimen;
using ThreeNH.DataModel;
using ThreeNH.Optical;

namespace ThreeNH.ColorQualityControl.Presentation
{
    /// <summary>
    /// 标试样 adapter 刷新与显色布局更新；合并 MainWindow 中分散的 Refresh* 调用链（2-T07）。
    /// </summary>
    internal sealed class SpecimenViewCoordinator
    {
        private readonly ISpecimenViewHost _host;
        private readonly ChromaticityDisplayService _chromaticityDisplay;
        private readonly ChartCoordinator _chartCoordinator;

        public SpecimenViewCoordinator(
            ISpecimenViewHost host,
            ChromaticityDisplayService chromaticityDisplay,
            ChartCoordinator chartCoordinator)
        {
            _host = host ?? throw new ArgumentNullException(nameof(host));
            _chromaticityDisplay = chromaticityDisplay ?? throw new ArgumentNullException(nameof(chromaticityDisplay));
            _chartCoordinator = chartCoordinator ?? throw new ArgumentNullException(nameof(chartCoordinator));
        }

        /// <summary>光源/观察者/测光方式变更后刷新 adapter 与图表。</summary>
        public void OnViewContextChanged(bool refreshCharts = true)
        {
            RefreshAdapters();
            _chromaticityDisplay.RefreshToleranceTable();
            if (refreshCharts)
            {
                _chartCoordinator.RefreshCharts(_host.CurrentTest);
            }
        }

        /// <summary>显示设置或容差表结构变更后重建 DataGrid 列与统计图。</summary>
        public void RefreshDisplayLayout()
        {
            var setting = _host.ChromaticitySetting;
            _chromaticityDisplay.RefreshToleranceTable();
            _chromaticityDisplay.ResetStandardDataGrid(setting);
            _chromaticityDisplay.ResetTestDataGrid(setting);
            ResetStatisticsPlot(setting);
            _chartCoordinator.SetToleranceChartView(_host.CurrentTest);
            _host.ColorDiffTable.Refresh();
        }

        /// <summary>系统/标样显色设置对话框确认后的完整刷新。</summary>
        public void OnChromaticitySettingsChanged()
        {
            RefreshAdapters();
            RefreshDisplayLayout();
            _chartCoordinator.RefreshCharts(_host.CurrentTest);
        }

        /// <summary>系统容差设置变更。</summary>
        public void OnSystemToleranceChanged(bool toleranceSourceChanged)
        {
            if (toleranceSourceChanged)
            {
                if (_host.SingleStandardInShowing?.ChromaticitySetting != null)
                {
                    RefreshDisplayLayout();
                }

                RefreshAdapters();
                _chromaticityDisplay.RefreshToleranceTable();
                return;
            }

            _host.Database.UpdateJudgements();
            RefreshTestsAdapters();
            _chromaticityDisplay.RefreshToleranceTable();
        }

        /// <summary>单个标样的显色/容差设置变更。</summary>
        public void OnStandardDisplaySettingsChanged(StandardSpecimen standard)
        {
            RefreshAdaptersForStandard(standard, withTests: true);
            if (standard == _host.SingleStandardInShowing)
            {
                _chromaticityDisplay.RefreshToleranceTable();
                RefreshDisplayLayout();
            }

            _chartCoordinator.RefreshCharts(_host.CurrentTest);
        }

        public void RefreshAdapters()
        {
            RefreshStandardsAdapters();
            RefreshTestsAdapters();
        }

        public void RefreshAdaptersForStandard(StandardSpecimen standard, bool withTests = true)
        {
            if (standard == null)
            {
                throw new ArgumentNullException(nameof(standard));
            }

            var stdTreeViewItem = SpecimenTreePresenter.FindTreeViewItem(
                _host.StandardGroupRoot.Items, standard);
            if (stdTreeViewItem?.DataContext is SpecimenWrap wrap)
            {
                wrap.Adapter = _host.Database.GetAdapter(
                    standard, _host.SpecularComponent, _host.Observer, _host.Illuminant);
            }

            if (withTests)
            {
                RefreshTestsAdaptersForStandard(standard);
            }
        }

        public void RefreshSingleStandardDiagram()
        {
            _host.StatisticsPlot.Refresh();
            _chartCoordinator.SetToleranceChartView(_host.CurrentTest);
            _host.ColorDiffTable.Refresh();
        }

        public void OnCurrentTestChanged()
        {
            if (_host.CurrentTest != null)
            {
                _chartCoordinator.RefreshCharts(_host.CurrentTest);
                _host.TestDataGrid.SelectedValue = _host.CurrentTest;
                if (_host.TestDataGrid.SelectedItem is SpecimenWrap item)
                {
                    _host.TestDataGrid.ScrollIntoView(item);
                    _host.ColorDiffTable.Test = item;
                    if (item.Spectrum != null && _host.SpectrumPlot.IndexOf(item.Spectrum) < 0)
                    {
                        _host.SpectrumPlot.AddTest(item.Spectrum, item.Name);
                        if (item.Specimen.IsTransmission)
                        {
                            _host.AbsorbancePlot.AddTest(item.Spectrum, item.Name);
                        }
                    }

                    _host.SpectrumPlot.Select(item.Spectrum);
                    _host.AbsorbancePlot.Select(item.Spectrum);
                }

                return;
            }

            _host.ColorDiffTable.Test = null;
            _chartCoordinator.RefreshCharts();
        }

        private void RefreshStandardsAdapters()
        {
            foreach (TreeViewItem treeViewItem in _host.StandardGroupRoot.Items)
            {
                var item = treeViewItem.DataContext as SpecimenWrap;
                if (item?.Specimen is StandardSpecimen standard)
                {
                    item.Adapter = _host.Database.GetAdapter(
                        standard, _host.SpecularComponent, _host.Observer, _host.Illuminant);
                }
            }
        }

        private void RefreshTestsAdapters()
        {
            foreach (TreeViewItem treeViewItem in _host.AllTestsRoot.Items)
            {
                var item = treeViewItem.DataContext as SpecimenWrap;
                if (item?.Specimen is TestSpecimen test)
                {
                    item.Adapter = _host.Database.GetAdapter(
                        test, _host.SpecularComponent, _host.Observer, _host.Illuminant);
                }
            }

            if (_host.IsShowSingleStandard)
            {
                RefreshSingleStandardDiagram();
            }
        }

        private void RefreshTestsAdaptersForStandard(StandardSpecimen standard)
        {
            var stdTreeViewItem = SpecimenTreePresenter.FindTreeViewItem(
                _host.StandardGroupRoot.Items, standard);
            if (stdTreeViewItem == null)
            {
                return;
            }

            foreach (TreeViewItem item in stdTreeViewItem.Items)
            {
                if (item.DataContext is SpecimenWrap testWrap
                    && testWrap.Specimen is TestSpecimen test)
                {
                    testWrap.Adapter = _host.Database.GetAdapter(
                        test, _host.SpecularComponent, _host.Observer, _host.Illuminant);
                }
            }

            RefreshSingleStandardDiagram();
        }

        private void ResetStatisticsPlot(ChromaticitySetting setting)
        {
            var titles = _host.StatisticsPlot.SeriesTitles;
            var diffFields = Array.ConvertAll(setting.ColorDiffFields, field => field.Name());
            foreach (var title in titles)
            {
                if (!diffFields.Contains(title))
                {
                    _host.StatisticsPlot.Hide(title);
                }
            }
        }
    }
}
