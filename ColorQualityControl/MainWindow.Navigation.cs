using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ThreeNH.ColorAlgorithm;
using ThreeNH.ColorQualityControl.Adapter;
using ThreeNH.ColorQualityControl.Refactoring;
using ThreeNH.ColorQualityControl.Refactoring.Contracts;
using ThreeNH.DataModel;

namespace ThreeNH.ColorQualityControl
{
    public partial class MainWindow
    {
        // TreeView 节点锚点，仅用于选中/滚动/DataContext；展示模式由 _session.Selection.ViewMode 表达（阶段1-T12）
        private TreeViewItem displayedCategory;

        private static ItemsControl GetSelectedTreeViewItemParent(TreeViewItem item)
        {
            DependencyObject parent = VisualTreeHelper.GetParent(item);
            if (parent == null)
                return null;
            while (!(parent is TreeViewItem || parent is TreeView))
            {
                parent = VisualTreeHelper.GetParent(parent);
            }

            return parent as ItemsControl;
        }

        private bool IsOrphanTestsTree(TreeViewItem item)
        {
            return item == OrphanTestsTreeItem;
        }

        private bool IsAllTestsTree(TreeViewItem item)
        {
            return item == AllTestsTreeItem;
        }

        private bool IsStandardGroupTree(TreeViewItem item)
        {
            return item == StandardGroupTreeItem;
        }

        private bool IsTestTreeItem(TreeViewItem item)
        {
            return item != null && item.Tag is TestSpecimen;
        }

        private bool IsStandardTreeItem(TreeViewItem item)
        {
            return item != null && item.Tag is StandardSpecimen;
        }

        private StandardSpecimen SelectedStandard
        {
            get
            {
                var item = navigationTreeView.SelectedItem as TreeViewItem;
                if (item != null && item.Tag is StandardSpecimen)
                {
                    return item.Tag as StandardSpecimen;
                }
                return null;
            }
        }

        private void ClearNavigationTree()
        {
            CurrentStandardTreeItem.Items.Clear();
            CurrentStandardTreeItem.Tag = null;
            _treePresenter.Clear();
        }

        private void OnNavigationTreeViewPreviewMouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            IInputElement inputElement = navigationTreeView.InputHitTest(e.GetPosition(navigationTreeView));
            if (inputElement is DependencyObject)
            {
                DependencyObject d = inputElement as DependencyObject;
                while (d != null && !(d is TreeViewItem))
                {
                    d = VisualTreeHelper.GetParent(d);
                }
                TreeViewItem item = d as TreeViewItem;
                if (item != null && !item.IsSelected)
                {
                    item.IsSelected = true;
                }

                if (IsStandardTreeItem(item))
                {
                    item.ContextMenu = (ContextMenu)Resources["StandardContextMenu"];
                }
                else if (IsTestTreeItem(item))
                {
                    item.ContextMenu = (ContextMenu)Resources["TestContextMenu"];
                }
                else if (IsStandardGroupTree(item) || IsOrphanTestsTree(item) || IsAllTestsTree(item))
                {
                }
            }
        }

        private void NavigationTreeViewSelectionChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            TreeViewItem selectedTreeItem = e.NewValue as TreeViewItem;
            if (selectedTreeItem == null) return;
            if (IsOrphanTestsTree(selectedTreeItem))
            {
                ShowOrphanTests();
            }
            else if (IsAllTestsTree(selectedTreeItem))
            {
                ShowAllTests();
            }
            else if (IsStandardGroupTree(selectedTreeItem))
            {
                ShowAllStandards();
            }
            else if (selectedTreeItem == CurrentStandardTreeItem)
            {
                if (CurrentStandard != null)
                    ShowSingleStandard(CurrentStandard);
            }
            else
            {
                ItemsControl parent = GetSelectedTreeViewItemParent(selectedTreeItem);
                if (parent == null)
                    return;

                if (parent == AllTestsTreeItem)
                {
                    CurrentStandard = null;
                    if (_session.Selection.ViewMode != NavigationViewMode.AllTests)
                    {
                        ShowAllTests();
                    }
                    CurrentTest = selectedTreeItem.Tag as TestSpecimen;
                }
                else if (parent == OrphanTestsTreeItem)
                {
                    if (_session.Selection.ViewMode != NavigationViewMode.OrphanTests)
                    {
                        ShowOrphanTests();
                    }
                    CurrentTest = selectedTreeItem.Tag as TestSpecimen;
                }
                else if (parent == StandardGroupTreeItem)
                {
                    var standard = selectedTreeItem.Tag as StandardSpecimen;
                    CurrentStandard = standard;
                    ShowSingleStandard(standard);
                }
                //                    else if (parent == TargetLibraryTreeItem)
                //                    {
                ////                        var standard = selectedTreeItem.Tag as StandardSpecimen;
                //                        //ShowSingleStandard(standard);
                //                        if (displayedCategory != TargetLibraryTreeItem)
                //                        {
                //                            ShowTargetLibrary();
                //                        }
                //                        StandardDataGrid.SelectedItem = selectedTreeItem.DataContext as SpecimenWrap;
                //                    }
                else // 當前標樣
                {
                    var standard = (parent.Tag as StandardSpecimen);
                    if (standard == null)
                    {
                        throw new Exception("Standard is NULL");
                    }

                    if (standard != null && SingleStandardInShowing != standard)
                    {
                        CurrentStandard = standard;
                        ShowSingleStandard(standard);
                    }
                    CurrentTest = selectedTreeItem.Tag as TestSpecimen;
                    RefreshCharts(CurrentTest);
                }
            }
        }

        /// <summary>
        /// 设置StandardDataGrid中显示的数据
        /// </summary>
        /// <param name="wraps">要显示的标样的SpecimenWrap</param>
        private void SetStandardTableData(IEnumerable<SpecimenWrap> wraps)
        {
            StandardDataGrid.BeginInit();
            StandardDataGrid.Items.Clear();
            foreach (var wrap in wraps)
            {
                StandardDataGrid.Items.Add(wrap);
            }
            StandardDataGrid.EndInit();
        }

        /// <summary>
        /// 设置TestDataGrid中显示的数据
        /// </summary>
        /// <param name="wraps"></param>
        private void SetTestTableData(IEnumerable<SpecimenWrap> wraps)
        {
            TestDataGrid.BeginInit();
            TestDataGrid.Items.Clear();
            foreach (var wrap in wraps)
            {
                TestDataGrid.Items.Add(wrap);
            }
            TestDataGrid.EndInit();
        }

        //private void SetToleranceTableData(ToleranceSetting setting)
        //{
        //    //ToleranceDataGrid.BeginInit();
        //    //ToleranceDataGrid.Items.Clear();

        //    //ToleranceAdapter[] adapters = ToleranceAdapter.GetAdapters(Database.ObserverAndIlluminantSetting, setting);
        //    //foreach(var adapter in adapters)
        //    //{
        //    //    ToleranceDataGrid.Items.Add(adapter);
        //    //}

        //    //ToleranceDataGrid.EndInit();
        //}

        /// <summary>
        /// 显示所有标样
        /// </summary>
        private void ShowAllStandards()
        {
            _session.Selection.SetAllStandardsView();
            displayedCategory = StandardGroupTreeItem;

            StandardDataGrid.Visibility = Visibility.Visible;
            TestDataGrid.Visibility = Visibility.Collapsed;

            DataLayoutGrid.RowDefinitions[1].Height = new GridLength(100.0, GridUnitType.Star);
            DataLayoutGrid.RowDefinitions[2].Height = GridLength.Auto;

            var wraps = GetAllStandardWraps();
            SetStandardTableData(wraps);

            CurrentTest = null;
            colorDiffTable.Standard = null;

            // 将所有标样的反射率加入到反射率图中
            SpectrumPlot.Clear();
            //if (Database.StandardSpecimens.Count() < 200)
            //{
            int count = 0;
            foreach (var wrap in wraps)
            {
                if (wrap.Spectrum != null)
                {
                    SpectrumPlot.AddStandard(wrap.Spectrum, wrap.Name);
                    count += 1;
                }
                if (count >= 100)
                    break;
            }
            //}

            // 清空统计图
            StatisticsPlot.Clear();
            RefreshCharts();
        }

        /// <summary>
        /// 显示所有试样
        /// </summary>
        private void ShowAllTests()
        {
            _session.Selection.SetAllTestsView();
            displayedCategory = AllTestsTreeItem;

            StandardDataGrid.Visibility = Visibility.Collapsed;
            TestDataGrid.Visibility = Visibility.Visible;

            DataLayoutGrid.RowDefinitions[1].Height = GridLength.Auto;
            DataLayoutGrid.RowDefinitions[2].Height = new GridLength(100.0, GridUnitType.Star);

            var wraps = GetAllTestWraps();
            SetTestTableData(wraps);

            CurrentTest = null;
            colorDiffTable.Standard = null;

            // 将所有试样添加到反射率图中
            SpectrumPlot.BeginInit();
            SpectrumPlot.Clear();
            //if (wraps.Length < 200)
            //{
            int count = 0;
            foreach (var wrap in wraps)
            {
                if (wrap.Spectrum != null)
                {
                    SpectrumPlot.AddTest(wrap.Spectrum, wrap.Name);
                    count += 1;
                }
                if (count >= 100)
                    break;
            }
            //}
            SpectrumPlot.EndInit();

            // 清空统计图
            StatisticsPlot.Clear();
            RefreshCharts();
        }

        /// <summary>
        /// 显示单个标样及其下的所有试样
        /// </summary>
        /// <param name="standard">要显示的单个标样</param>
        private void ShowSingleStandard(StandardSpecimen standard)
        {
            if (standard == null)
                throw new ArgumentNullException();

            if (SingleStandardInShowing == standard)
            {
                RefreshCharts();
                return;
            }

            var treeItem = FindTreeViewItem(StandardGroupTreeItem.Items, standard);
            if (treeItem == null)
            {
                return;
            }

            _session.Selection.SetSingleStandardView(standard);
            displayedCategory = treeItem;
            if (!displayedCategory.IsSelected)
            {
                displayedCategory.IsSelected = true;
                displayedCategory.BringIntoView();
            }

            StandardDataGrid.Visibility = Visibility.Visible;
            TestDataGrid.Visibility = Visibility.Visible;
            DataLayoutGrid.RowDefinitions[1].Height = GridLength.Auto;
            DataLayoutGrid.RowDefinitions[2].Height = new GridLength(100.0, GridUnitType.Star);

            if (Database.IsUseIndependentTolerance)
            {
                ResetDisplayedFields();
                //SetToleranceTableData(Database.GetToleranceSetting(standard));
            }

            SpecimenWrap stdWrap = displayedCategory.DataContext as SpecimenWrap;
            SpecimenWrap[] testWraps = GetTestWraps(standard);

            SetStandardTableData(new SpecimenWrap[] { stdWrap });
            SetTestTableData(testWraps);

            CurrentTest = null;
            colorDiffTable.Standard = stdWrap;

            // 将标样和其下的所有试样的反射率添加到反射率图中
            SpectrumPlot.BeginInit();
            SpectrumPlot.Clear();
            if (stdWrap.Spectrum != null)
                SpectrumPlot.AddStandard(stdWrap.Spectrum, stdWrap.Name);
            foreach (var wrap in testWraps)
            {
                if (wrap.Spectrum != null)
                {
                    SpectrumPlot.AddTest(wrap.Spectrum, wrap.Name);
                }
            }
            SpectrumPlot.EndInit();

            //吸光度图
            AbsorbancePlot.BeginInit();
            AbsorbancePlot.Clear();
            if (stdWrap.Specimen.IsTransmission)
            {
                if (stdWrap.Spectrum != null)
                    AbsorbancePlot.AddStandard(stdWrap.Spectrum, stdWrap.Name);
                foreach (var wrap in testWraps)
                {
                    if (wrap.Spectrum != null && wrap.Specimen.IsTransmission)
                    {
                        AbsorbancePlot.AddTest(wrap.Spectrum, wrap.Name);
                    }
                }
            }
            AbsorbancePlot.EndInit();

            // 添加到统计图中；先带上当前标样容差，Show 默认 ΔE 系列时才能画正确上限线
            StatisticsPlot.RangeTolerance = Database.GetTolerance(
                standard, new ObserverIlluminantPair(Observer, Illuminant));
            StatisticsPlot.Clear();
            StatisticsPlot.AddRange(testWraps);
            //            foreach(var wrap in testWraps)
            //            {
            //                StatisticsPlot.Add(wrap);
            //            }

            //var treeViewItem = FindTreeViewItem(StandardGroupTreeItem.Items, standard);
            //if (treeViewItem != null && !treeViewItem.IsSelected)
            //{
            //    treeViewItem.IsSelected = true;
            //    treeViewItem.BringIntoView();
            //}
            //刷新色品图与容差图
            RefreshCharts();
        }

        private void ShowOrphanTests()
        {
            CurrentStandard = null;
            CurrentTest = null;
            colorDiffTable.Standard = null;

            _session.Selection.SetOrphanTestsView();
            displayedCategory = OrphanTestsTreeItem;

            StandardDataGrid.Visibility = Visibility.Collapsed;
            TestDataGrid.Visibility = Visibility.Visible;
            DataLayoutGrid.RowDefinitions[1].Height = GridLength.Auto;
            DataLayoutGrid.RowDefinitions[2].Height = new GridLength(100.0, GridUnitType.Star);

            var wraps = GetTestWraps(null);
            SetTestTableData(wraps);

            SpectrumPlot.BeginInit();
            SpectrumPlot.Clear();
            foreach (var wrap in wraps)
            {
                if (wrap.Spectrum != null)
                {
                    SpectrumPlot.AddTest(wrap.Spectrum, wrap.Name);
                }
            }
            SpectrumPlot.EndInit();

            // 清空统计图
            StatisticsPlot.Clear();

            if (displayedCategory.IsSelected != true)
                displayedCategory.IsSelected = true;
            RefreshCharts();
        }

        private bool IsShowTest(TestSpecimen test)
        {
            return IsShowAllTests || (IsShowSingleStandard && test.StandardSpecimen == SingleStandardInShowing)
                || (IsShowOrphanTests && test.StandardSpecimen == null);
        }

        private bool IsShowStandard(StandardSpecimen standard)
        {
            return IsShowAllStandards || (IsShowSingleStandard && standard == SingleStandardInShowing);
        }

        private void OnStandardDataGridSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0 && e.AddedItems[0] is SpecimenWrap)
            {
                var wrap = e.AddedItems[0] as SpecimenWrap;
                if (wrap.Spectrum != null)
                {
                    int index = SpectrumPlot.IndexOf(wrap.Spectrum);
                    if (index < 0)
                    {
                        SpectrumPlot.AddStandard(wrap.Spectrum, wrap.Name);
                    }
                }
                SpectrumPlot.Select(wrap.Spectrum);
            }
        }

        private void OnTestDataGridSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (TestDataGrid == null || TestDataGrid.SelectedItems?.Count == 0)
            {
                CurrentTest = null;
                return;
            }

            var selectedTests = TestDataGrid.SelectedItems
                .Cast<SpecimenWrap>()
                .Select(w => w.Specimen)
                .Where(s => s != null)
                .ToList();

            var treeItem = SingleStandardInShowing != null ?
                FindTreeViewItem(StandardGroupTreeItem.Items, SingleStandardInShowing) :
                OrphanTestsTreeItem;
            treeItem = IsShowAllTests ? AllTestsTreeItem : treeItem;

            if (!treeItem.IsExpanded)
                treeItem.IsExpanded = true;

            if (selectedTests.Count == 1)
            {
                CurrentTest = selectedTests[0] as TestSpecimen;
                var testTreeItem = FindTreeViewItem(treeItem.Items, selectedTests[0]);
                if (testTreeItem != null)
                {
                    testTreeItem.BringIntoView();
                    testTreeItem.IsSelected = true;
                }
            }
            else
            {
                // 多选时不走 CurrentTest（会只刷新一个试样），将全部选中项同步到色差图与色品图
                RefreshCharts(selectedTests);
            }
        }
    }
}