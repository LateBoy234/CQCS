using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ThreeNH.ColorQualityControl.UI;
using ThreeNH.ColorSpecimen;
using ThreeNH.DataModel;

namespace ThreeNH.ColorQualityControl
{
    public partial class MainWindow
    {
        /// <summary>
        /// 右键菜单弹出前确保行已选中且 Grid 获焦，避免 CanExecute 因 IsFocused 为 false 需点两次。
        /// </summary>
        private void OnSpecimenDataGridRowPreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (!(sender is DataGridRow row) || row.Item == null)
            {
                return;
            }

            var dataGrid = ItemsControl.ItemsControlFromItemContainer(row) as DataGrid;
            if (dataGrid == null)
            {
                return;
            }

            if (!row.IsSelected)
            {
                dataGrid.SelectedItem = row.Item;
            }

            dataGrid.Focus();
            Keyboard.Focus(dataGrid);
        }

        private void FindExecuted(object sender, ExecutedRoutedEventArgs e)
        {
        }

        private void RenameExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            var specimens = GetSelectedSpecimens(e.Source);
            if (specimens == null)
                return;

            GetInputDialog dlg = new GetInputDialog();
            dlg.Owner = this;
            dlg.Title = _("Title_Rename");
            dlg.Prompt = _("PleaseInputNewName");
            dlg.Input = specimens[0].Name;
            if (dlg.ShowDialog().GetValueOrDefault())
            {
                string newName = dlg.Input.Trim();
                if (string.IsNullOrWhiteSpace(newName))
                {
                    MessageBox.Show(_("NameCanNotBeWhitespace"));
                    return;
                }
                if (IsActOnStandards(e.Source))
                {
                    var standard = specimens[0].Specimen as StandardSpecimen;
                    // 标样名称在样品库中全局唯一，避免重名导致查找和管理混乱
                    if (Database.StandardSpecimens.Any(s => s != standard && s.Name == newName))
                    {
                        MessageBox.Show(_("SpecimenNameDuplicated"));
                        return;
                    }
                }
                else if (IsActOnTests(e.Source))
                {
                    var test = specimens[0].Specimen as TestSpecimen;
                    var standard = test.StandardSpecimen;
                    // 试样名称在同一标样下唯一，不同标样可同名；无标样时在同组孤立试样中查重
                    IEnumerable<TestSpecimen> siblings = standard != null
                        ? (standard.TestSpecimens ?? Enumerable.Empty<TestSpecimen>())
                        : Database.OrphanTests;
                    if (siblings.Any(t => t != test && t.Name == newName))
                    {
                        MessageBox.Show(_("SpecimenNameDuplicated"));
                        return;
                    }
                }
                specimens[0].Name = newName;
            }
        }

        private void ModifyMaterialExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            var specimens = GetSelectedSpecimens(e.Source);
            if (specimens == null)
                return;

            GetInputDialog dlg = new GetInputDialog();
            dlg.Owner = this;
            dlg.Title = _("Title_ModifyMaterial");
            dlg.Prompt = _("PleaseInputMaterial");
            dlg.Input = specimens[0].Material;
            if (dlg.ShowDialog().GetValueOrDefault())
            {
                string newMaterial = dlg.Input.Trim();
                foreach (var specimen in specimens)
                {
                    specimen.Material = newMaterial;
                }
            }
        }

        private void SetRulesOfAutoNamingExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            List<KeyValuePair<string, string>> variableDescriptions =
                new List<KeyValuePair<string, string>>();

            foreach (string variable in SpecimenNameGenerator.Variables)
            {
                variableDescriptions.Add(new KeyValuePair<string, string>(variable, _(variable)));
            }

            RenameRuleDialog dlg = new RenameRuleDialog();
            dlg.Owner = this;
            dlg.Title = _("Title_AutoNamingRulesSetting");
            dlg.StandardNameRule = Database.StandardNamgeGenerator.Rule;
            dlg.StandardNameRuleVariables = variableDescriptions.ToArray();
            dlg.TestNameRule = Database.TestNameGenerator.Rule;
            dlg.TestNameRuleVariables = variableDescriptions.ToArray();
            dlg.IsShowStandardRenameRule = true;
            dlg.IsShowTestRenameRule = true;
            dlg.Material = Database.DefaultMaterial;

            if (dlg.ShowDialog() == true)
            {
                Database.StandardNamgeGenerator.Rule = dlg.StandardNameRule;
                Database.TestNameGenerator.Rule = dlg.TestNameRule;
                Database.DefaultMaterial = dlg.Material;

                SystemDefaultConfig.StandardAutoNamingRule = dlg.StandardNameRule;
                SystemDefaultConfig.TestAudotNamingRule = dlg.TestNameRule;
                SystemDefaultConfig.DefaultMaterial = dlg.Material;
            }
        }

        private void CanActOnSpecimen(object sender, CanExecuteRoutedEventArgs e)
        {
            if (TestDataGrid == null || StandardDataGrid == null || navigationTreeView == null)
            {
                e.CanExecute = false;
                return;
            }

            if (e.Source == TestDataGrid && TestDataGrid.SelectedItems.Count > 0 || TestDataGrid.IsFocused)
            {
                e.CanExecute = TestDataGrid.SelectedItems.Count > 0;
            }
            else if (e.Source == StandardDataGrid && StandardDataGrid.SelectedItems.Count > 0
                || StandardDataGrid.IsFocused)
            {
                e.CanExecute = StandardDataGrid.SelectedItems.Count > 0;
            }
            else if (navigationTreeView.SelectedItem != null)
            {
                TreeViewItem item = navigationTreeView.SelectedItem as TreeViewItem;
                if (item != null)
                {
                    e.CanExecute = item.Tag is Specimen;
                }
            }
            else
            {
                e.CanExecute = false;
            }
        }

        private void CanActOnStandard(object sender, CanExecuteRoutedEventArgs e)
        {
            if (StandardDataGrid == null || navigationTreeView == null)
            {
                e.CanExecute = false;
                return;
            }

            if (e.Source == StandardDataGrid && StandardDataGrid.SelectedItems.Count > 0
                || StandardDataGrid.IsFocused)
            {
                e.CanExecute = StandardDataGrid.SelectedItems.Count > 0;
            }
            else if (navigationTreeView.SelectedItem != null)
            {
                TreeViewItem item = navigationTreeView.SelectedItem as TreeViewItem;
                if (item != null)
                {
                    e.CanExecute = item.Tag is StandardSpecimen;
                }
            }
            else
            {
                e.CanExecute = false;
            }
        }

        private bool IsActOnStandards(object source)
        {
            if (source == TestDataGrid && TestDataGrid.SelectedItems.Count > 0
                || TestDataGrid.IsFocused)
            {
                return false;
            }
            else if (source == StandardDataGrid && StandardDataGrid.SelectedItems.Count > 0
                || StandardDataGrid.IsFocused)
            {
                return StandardDataGrid.SelectedItems.Count > 0;
            }
            else if (navigationTreeView.SelectedItem != null)
            {
                TreeViewItem item = navigationTreeView.SelectedItem as TreeViewItem;
                if (item != null)
                {
                    return item.Tag is StandardSpecimen;
                }
            }
            return false;
        }

        private bool TryGetTreeSelectedTest(out TestSpecimen test)
        {
            test = null;
            if (navigationTreeView.SelectedItem is TreeViewItem item && item.Tag is TestSpecimen selected)
            {
                test = selected;
                return true;
            }

            return false;
        }

        private bool IsActOnTests(object source)
        {
            // 树节点选中时表格可能仍持有焦点，须以命令来源为准，否则会删错试样
            if (source != TestDataGrid && TryGetTreeSelectedTest(out var test))
            {
                return true;
            }

            if ((source == TestDataGrid && TestDataGrid.SelectedItems.Count > 0)
                || TestDataGrid.IsFocused)
            {
                return TestDataGrid.SelectedItems.Count > 0;
            }

            if ((source == StandardDataGrid && StandardDataGrid.SelectedItems.Count > 0)
                || StandardDataGrid.IsFocused)
            {
                return false;
            }

            return TryGetTreeSelectedTest(out  var test1);
        }

        private StandardSpecimen[] GetSelectedStandards(object source)
        {
            if (source == StandardDataGrid || StandardDataGrid.IsFocused || navigationTreeView.SelectedItem == StandardGroupTreeItem)
            {
                return Array.ConvertAll(StandardDataGrid.SelectedItems.Cast<SpecimenWrap>().ToArray(),
                    wrap => wrap.Specimen as StandardSpecimen);
            }
            else if (navigationTreeView.SelectedItem != null &&
                navigationTreeView.SelectedItem is TreeViewItem)
            {
                TreeViewItem item = navigationTreeView.SelectedItem as TreeViewItem;
                if (item.Tag is StandardSpecimen)
                {
                    return new StandardSpecimen[] { item.Tag as StandardSpecimen };
                }
            }
            else if (StandardDataGrid.SelectedItems.Count > 0)
            {
                return StandardDataGrid.SelectedItems.OfType<SpecimenWrap>().Select(_ => _.Specimen as StandardSpecimen)
                    .ToArray();
            }
            return new StandardSpecimen[0];
        }

        private TestSpecimen[] GetSelectedTests(object source)
        {
            if (source != TestDataGrid && TryGetTreeSelectedTest(out TestSpecimen treeTest))
            {
                return new[] { treeTest };
            }

            if (source == TestDataGrid || TestDataGrid.IsFocused)
            {
                if (TestDataGrid.SelectedItems.Count == 0)
                {
                    return Array.Empty<TestSpecimen>();
                }

                return Array.ConvertAll(
                    TestDataGrid.SelectedItems.Cast<SpecimenWrap>().ToArray(),
                    wrap => wrap.Specimen as TestSpecimen);
            }

            if (TryGetTreeSelectedTest(out treeTest))
            {
                return new[] { treeTest };
            }

            return Array.Empty<TestSpecimen>();
        }

        private SpecimenWrap[] GetSelectedSpecimens(object source)
        {
            if (source == StandardDataGrid || StandardDataGrid.IsFocused)
            {
                return StandardDataGrid.SelectedItems.Cast<SpecimenWrap>().ToArray();
            }
            if (source == TestDataGrid || TestDataGrid.IsFocused)
            {
                return TestDataGrid.SelectedItems.Cast<SpecimenWrap>().ToArray();
            }
            if (navigationTreeView.SelectedItem != null &&
                navigationTreeView.SelectedItem is TreeViewItem)
            {
                TreeViewItem item = navigationTreeView.SelectedItem as TreeViewItem;
                return new SpecimenWrap[] { item.DataContext as SpecimenWrap };
            }
            return null;
        }

        private void DeleteExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            if (IsActOnTests(e.Source))
            {
                foreach (var test in GetSelectedTests(e.Source))
                {
                    RemoveTest(test);
                }
            }
            else if (IsActOnStandards(e.Source))
            {
                foreach (var std in GetSelectedStandards(e.Source))
                {
                    RemoveStandard(std);
                }
            }
        }

        private void MoveToStandardExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            if (!IsActOnTests(e.Source))
                return;

            TestSpecimen[] tests = GetSelectedTests(e.Source);
            // 先移除
            foreach (var test in tests)
            {
                RemoveTest(test);
            }

            // 再根据试样创建标样
            foreach (var test in tests)
            {
                StandardSpecimen std = new StandardSpecimen(test);
                std.ID = 0;

                AddStandard(std, isRemoveSame:false);
            }
        }

        private void CopyToUnderAStandard(object sender, ExecutedRoutedEventArgs e)
        {
            if (!IsActOnStandards(e.Source))
                return;
            var standardWraps = GetAllStandardWraps();
            StandardLinkageSettingDialog dlg = new StandardLinkageSettingDialog();
            dlg.Owner = this;
            dlg.Standards = standardWraps;
            dlg.SelectedStandard = CurrentStandard;
            if (!dlg.ShowDialog().GetValueOrDefault()) return;
            StandardSpecimen[] standards = GetSelectedStandards(e.Source);
            foreach (var std in standards)
            {
                TestSpecimen test = new TestSpecimen(std);
                test.ID = 0;

                AddTest(test, dlg.SelectedStandard);
            }
            if (dlg.SelectedStandard == null)
                OrphanTestsTreeItem.IsExpanded = true;
            else
                CurrentStandard = dlg.SelectedStandard;
        }

        private void CopyToTestExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            if (!IsActOnStandards(e.Source))
                return;

            StandardSpecimen[] standards = GetSelectedStandards(e.Source);
            // 检查选中的标样是否关联有试样，如果有的话提醒用户是否移除试样
            foreach (var std in standards)
            {
                TestSpecimen test = new TestSpecimen(std);
                test.ID = 0;

                AddTest(test, null);
            }

            foreach (TreeViewItem item in navigationTreeView.Items)
            {
                item.IsExpanded = false;
            }
            ShowOrphanTests();
            OrphanTestsTreeItem.IsExpanded = true;
        }

        private void ChangeStandardExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            StandardLinkageSettingDialog dlg = new StandardLinkageSettingDialog();
            TestSpecimen[] tests = GetSelectedTests(e.Source);

            dlg.Owner = this;
            dlg.Standards = GetAllStandardWraps();
            if (tests.Length > 0)
                dlg.SelectedStandard = tests[0].StandardSpecimen;

            if (dlg.ShowDialog() == true)
            {
                TreeViewItem newParentTreeItem = null;
                StandardSpecimen previousOldStandard = null;
                TreeViewItem oldParentTreeItem = OrphanTestsTreeItem;
                StandardSpecimen newStandard = null;

                if (dlg.IsLinkToStandard)
                {
                    newStandard = dlg.SelectedStandard;
                    newParentTreeItem = FindTreeViewItem(StandardGroupTreeItem.Items, newStandard);
                }
                else
                {
                    newParentTreeItem = OrphanTestsTreeItem;
                }

                foreach (var test in tests.OrderBy(t => t.Name))
                {
                    MoveTestToStandard(test, newStandard, newParentTreeItem,
                        ref previousOldStandard, ref oldParentTreeItem);
                }
                Database.IsModified = true;
            }
        }

        private void MoveTestToStandard(TestSpecimen test,
            StandardSpecimen newStandard, TreeViewItem newParentTreeItem,
            ref StandardSpecimen previousOldStandard, ref TreeViewItem oldParentTreeItem)
        {
            if (test.StandardSpecimen == newStandard)
                return;

            if (previousOldStandard != test.StandardSpecimen)
            {
                previousOldStandard = test.StandardSpecimen;
                oldParentTreeItem = test.StandardSpecimen != null ?
                    FindTreeViewItem(StandardGroupTreeItem.Items, test.StandardSpecimen) : OrphanTestsTreeItem;
            }
            var treeItem = FindTreeViewItem(oldParentTreeItem.Items, test);
            var testWrap = treeItem.DataContext as SpecimenWrap;

            if (!IsShowAllTests && test.StandardSpecimen == SingleStandardInShowing)
            {
                _gridPresenter?.RemoveTestWrapFromView(testWrap);
            }

            // 将试样从父节点上移除
            oldParentTreeItem.Items.Remove(treeItem);

            // 将其添加到新父节点上
            newParentTreeItem.Items.Add(treeItem);

            // 将其标样设为新标样
            test.ID = Database.GetNewTestIndex(newStandard); // 更新其ID号
            test.StandardSpecimen = newStandard;

            // 更新试样缓存
            Database.UpdateCachesOf(test);
            testWrap.Adapter = Database.GetAdapter(test, SpecularComponent, Observer, Illuminant);
            if (!IsShowAllTests && newStandard == SingleStandardInShowing)
            {
                _gridPresenter?.AddTestWrapToView(testWrap);
            }
        }

        private void AutoMatchStandardExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            Mouse.OverrideCursor = Cursors.Wait;

            StandardSpecimen previousOldStandard = null;
            TreeViewItem oldParentTreeItem = OrphanTestsTreeItem;

            int n = 0;
            var tests = GetSelectedTests(e.Source).OrderBy(t => t.Name).ToArray();
            foreach (var each in tests)
            {
                var newStandard = _session.Measurement.FindNearestStandard(each);
                if (newStandard == null)
                    continue;

                var newParentTreeItem = FindTreeViewItem(StandardGroupTreeItem.Items, newStandard);
                MoveTestToStandard(each, newStandard, newParentTreeItem,
                    ref previousOldStandard, ref oldParentTreeItem);

                n++;
            }

            Database.IsModified = n > 0;

            Mouse.OverrideCursor = Cursors.Arrow;

            // 如果只有一个试样且匹配到标样，跳转到相应标样
            if (tests.Length == 1 && n == 1)
            {
                ShowSingleStandard(tests[0].StandardSpecimen);
                //TestDataGrid.SelectedItem = tests[0];
                CurrentTest = tests[0];
            }
            else if (n == 0)
            {
                MessageBox.Show(_("NoMatchAnyStandard"));
            }
        }

        private void AutoMatchStandardCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            var tests = GetSelectedTests(e.Source);
            e.CanExecute = tests.All(t => t.StandardSpecimen == null);
        }

        private void GotoStandardExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            var tests = GetSelectedTests(e.Source);
            if (tests.Length > 0 && tests[0].StandardSpecimen != null)
            {
                if (SingleStandardInShowing != tests[0].StandardSpecimen)
                {
                    ShowSingleStandard(tests[0].StandardSpecimen);
                    StandardDataGrid.SelectedIndex = 0;
                }
            }
        }

        private void GotoStandardCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            var tests = GetSelectedTests(e.Source);
            e.CanExecute = tests.Length == 1 && tests[0].StandardSpecimen != null;
        }

        private void SwitchToStandardExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            if (!IsActOnTests(e.Source))
                return;

            TestSpecimen[] tests = GetSelectedTests(e.Source);
            if (tests.Length != 1 || tests[0].StandardSpecimen == null)
                return;

            TestSpecimen selectedTest = tests[0];
            StandardSpecimen oldStandard = selectedTest.StandardSpecimen;

            // 保存旧标样下的所有试样（除了当前选中的试样）
            List<TestSpecimen> otherTests = oldStandard.TestSpecimens
                .Where(t => t != selectedTest)
                .ToList();

            // 1. 从数据库中移除旧标样和它的所有试样
            foreach (var test in otherTests)
            {
                RemoveTest(test);
            }
            RemoveTest(selectedTest);
            RemoveStandard(oldStandard);


            // 2. 将选中的试样转换为新标样
            StandardSpecimen newStandard = new StandardSpecimen(selectedTest)
            {
                ID = 0, // 重置ID，让系统分配新ID
                Name = $"{selectedTest.Name}_{oldStandard.Name}",
                ToleranceSetting = oldStandard.ToleranceSetting != null ? new ToleranceSetting(oldStandard.ToleranceSetting) : null,
                ChromaticitySetting = oldStandard.ChromaticitySetting != null ? new ChromaticitySetting(oldStandard.ChromaticitySetting) : null,
                Comment = oldStandard.Comment
            };
            // 3. 将旧标样转换为新试样
            TestSpecimen newTest = new TestSpecimen(oldStandard);
            newTest.Name = $"{oldStandard.Name}_{selectedTest.Name}";
            // 4. 添加新标样
            AddStandard(newStandard, isRemoveSame: true);

            // 5. 将新试样（原标样）关联到新标样下
            AddTest(newTest, newStandard);

            // 6. 将旧标样下的其他试样也关联到新标样下，并重新计算色差
            foreach (var test in otherTests)
            {
                AddTest(test, newStandard);
            }

            // 7. 刷新显示
            if (IsShowSingleStandard)
            {
               // ShowSingleStandard(newStandard);
                StandardDataGrid.SelectedIndex = 0;
            }
            else
            {
                // 刷新当前视图 - 先清理导航树再重新加载，避免重复
                ClearNavigationTree();
                LoadAllSpecimens();
                if (StandardGroupTreeItem.Items.Count != 0)
                {
                    StandardGroupTreeItem.IsSelected = true;
                    StandardGroupTreeItem.IsExpanded = true;
                }
                else
                {
                    AllTestsTreeItem.IsSelected = true;
                    AllTestsTreeItem.IsExpanded = true;
                }
                RefreshTitle();
                RefreshCharts();
            }
        }

        private void SwitchToStandardCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            var tests = GetSelectedTests(e.Source);
            e.CanExecute = tests.Length == 1 && tests[0].StandardSpecimen != null;
        }

        private void ShowAssociatedTestsExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            if (StandardDataGrid.SelectedItem != null)
            {
                ShowSingleStandard(StandardDataGrid.SelectedValue as StandardSpecimen);
            }
        }

        private void ShowAssociatedTestsCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = StandardDataGrid.SelectedItems.Count == 1 && !IsShowSingleStandard;
        }
    }
}