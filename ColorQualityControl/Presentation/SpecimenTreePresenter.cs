using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using ThreeNH.ColorQualityControl.Adapter;
using ThreeNH.ColorQualityControl.Refactoring.Contracts;
using ThreeNH.ColorQualityControl.Services;
using ThreeNH.ColorQualityControl.Specimens.Events;
using ThreeNH.ColorSpecimen;
using ThreeNH.DataModel;

namespace ThreeNH.ColorQualityControl.Presentation
{
    /// <summary>
    /// 导航 TreeView 构建与标试样节点增删；订阅 Job 领域事件驱动刷新（阶段2-T04）。
    /// </summary>
    internal sealed class SpecimenTreePresenter
    {
        private readonly ISpecimenTreeHost _host;
        private readonly JobDocumentService _jobDocument;
        private bool _subscribed;
        private int _bulkUpdateDepth;

        public void BeginBulkUpdate() => _bulkUpdateDepth++;

        public void EndBulkUpdate()
        {
            if (_bulkUpdateDepth > 0)
            {
                _bulkUpdateDepth--;
            }
        }

        private bool IsBulkUpdate => _bulkUpdateDepth > 0;

        public SpecimenTreePresenter(ISpecimenTreeHost host, JobDocumentService jobDocument)
        {
            _host = host ?? throw new ArgumentNullException(nameof(host));
            _jobDocument = jobDocument ?? throw new ArgumentNullException(nameof(jobDocument));
        }

        public void Subscribe()
        {
            if (_subscribed)
            {
                return;
            }

            _jobDocument.SpecimenAdded += OnSpecimenAdded;
            _jobDocument.SpecimenRemoved += OnSpecimenRemoved;
            _subscribed = true;
        }

        public void Clear()
        {
            _host.StandardGroupRoot.Items.Clear();
            _host.AllTestsRoot.Items.Clear();
            _host.OrphanTestsRoot.Items.Clear();
        }

        public void LoadAll(JobSpecimensForView viewData)
        {
            Clear();
            if (viewData?.Standards == null)
            {
                return;
            }

            foreach (var entry in viewData.Standards)
            {
                var item = new SpecimenWrap(entry.Standard);
                var treeItem = CreateTreeViewItem(item);
                _host.StandardGroupRoot.Items.Add(treeItem);
                if (entry.Tests == null)
                {
                    continue;
                }

                foreach (TestSpecimenAdapter test in entry.Tests)
                {
                    var testItem = new SpecimenWrap(test);
                    var testNode = CreateTreeViewItem(testItem);
                    treeItem.Items.Add(testNode);
                    _host.AllTestsRoot.Items.Add(CreateTreeViewItem(testItem));
                }
            }

            if (viewData.OrphanTests == null)
            {
                return;
            }

            foreach (TestSpecimenAdapter test in viewData.OrphanTests)
            {
                var testItem = new SpecimenWrap(test);
                _host.OrphanTestsRoot.Items.Add(CreateTreeViewItem(testItem));
                _host.AllTestsRoot.Items.Add(CreateTreeViewItem(testItem));
            }
        }

        public SpecimenWrap FindStandard(StandardSpecimen standard)
        {
            foreach (TreeViewItem item in _host.StandardGroupRoot.Items)
            {
                if (item.Tag == standard)
                {
                    return item.DataContext as SpecimenWrap;
                }
            }

            return null;
        }

        public SpecimenWrap FindTest(TestSpecimen test)
        {
            foreach (TreeViewItem item in _host.AllTestsRoot.Items)
            {
                if (item.Tag == test)
                {
                    return item.DataContext as SpecimenWrap;
                }
            }

            return null;
        }

        public static TreeViewItem FindTreeViewItem(ItemCollection items, Specimen specimen)
        {
            if (items == null)
            {
                return null;
            }

            foreach (TreeViewItem item in items)
            {
                if (item.Tag == specimen)
                {
                    return item;
                }
            }

            return null;
        }

        public static int IndexOfTreeViewItem(ItemCollection items, Specimen specimen)
        {
            if (items == null)
            {
                return -1;
            }

            for (int i = 0; i < items.Count; i++)
            {
                if (((TreeViewItem)items[i]).Tag == specimen)
                {
                    return i;
                }
            }

            return -1;
        }

        public void ExpandStandard(StandardSpecimen standard)
        {
            var treeItem = FindTreeViewItem(_host.StandardGroupRoot.Items, standard);
            treeItem?.ExpandSubtree();
        }

        private void OnSpecimenAdded(object sender, SpecimenAddedEventArgs e)
        {
            if (IsBulkUpdate)
            {
                return;
            }

            if (e.Kind == SpecimenKind.Standard)
            {
                AddStandardNode(_host.CreateStandardWrap(e.Standard));
                return;
            }

            AddTestNode(_host.CreateTestWrap(e.Test), e.Standard);
        }

        private void OnSpecimenRemoved(object sender, SpecimenRemovedEventArgs e)
        {
            if (IsBulkUpdate)
            {
                return;
            }

            if (e.Kind == SpecimenKind.Standard)
            {
                RemoveStandardNode(e.Standard);
                return;
            }

            // RemoveTest 后 test.StandardSpecimen 可能已被 Database 清空，须用事件参数里的父标样
            RemoveTestNode(e.Test, e.Standard);
        }

        private void AddStandardNode(SpecimenWrap item)
        {
            _host.StandardGroupRoot.Items.Add(CreateTreeViewItem(item));
        }

        private void AddTestNode(SpecimenWrap item, StandardSpecimen parentStandard)
        {
            _host.AllTestsRoot.Items.Add(CreateTreeViewItem(item));

            var test = item.Specimen as TestSpecimen;
            if (test?.StandardSpecimen != null || parentStandard != null)
            {
                var std = parentStandard ?? test?.StandardSpecimen;
                var stdItem = FindTreeViewItem(_host.StandardGroupRoot.Items, std);
                stdItem?.Items.Add(CreateTreeViewItem(item));
            }
            else
            {
                _host.OrphanTestsRoot.Items.Add(CreateTreeViewItem(item));
            }
        }

        private SpecimenWrap RemoveStandardNode(StandardSpecimen standard)
        {
            int index = IndexOfTreeViewItem(_host.StandardGroupRoot.Items, standard);
            if (index < 0)
            {
                return null;
            }

            var item = (_host.StandardGroupRoot.Items[index] as TreeViewItem)?.DataContext as SpecimenWrap;
            _host.StandardGroupRoot.Items.RemoveAt(index);

            if (standard.TestSpecimens != null)
            {
                foreach (TestSpecimen test in standard.TestSpecimens)
                {
                    RemoveTestFromAllTestsRoot(test);
                }
            }

            return item;
        }

        private void RemoveTestNode(TestSpecimen test, StandardSpecimen parentStandard)
        {
            RemoveTestFromAllTestsRoot(test);

            if (parentStandard == null)
            {
                RemoveFromOrphanRoot(test);
            }
            else
            {
                RemoveFromStandardChild(test, parentStandard);
            }
        }

        private SpecimenWrap RemoveTestFromAllTestsRoot(TestSpecimen test)
        {
            int index = IndexOfTreeViewItem(_host.AllTestsRoot.Items, test);
            if (index < 0)
            {
                return null;
            }

            var item = (_host.AllTestsRoot.Items[index] as TreeViewItem)?.DataContext as SpecimenWrap;
            _host.AllTestsRoot.Items.RemoveAt(index);
            return item;
        }

        private void RemoveFromOrphanRoot(TestSpecimen test)
        {
            int index = IndexOfTreeViewItem(_host.OrphanTestsRoot.Items, test);
            if (index >= 0)
            {
                _host.OrphanTestsRoot.Items.RemoveAt(index);
            }
        }

        private void RemoveFromStandardChild(TestSpecimen test, StandardSpecimen parentStandard)
        {
            var stdItem = FindTreeViewItem(_host.StandardGroupRoot.Items, parentStandard);
            if (stdItem == null)
            {
                return;
            }

            int index = IndexOfTreeViewItem(stdItem.Items, test);
            if (index >= 0)
            {
                stdItem.Items.RemoveAt(index);
            }
        }

        private TreeViewItem CreateTreeViewItem(SpecimenWrap item)
        {
            var treeItem = new TreeViewItem
            {
                DataContext = item,
                Tag = item.Specimen,
                Header = CreateSpecimenItemHeader(item.Specimen is StandardSpecimen)
            };

            if (item.Specimen is StandardSpecimen standard)
            {
                treeItem.MouseDoubleClick += (_, e) =>
                {
                    _host.ActivateStandard(standard);
                    e.Handled = true;
                };
            }

            return treeItem;
        }

        internal static object CreateSpecimenItemHeader(bool isStandard)
        {
            var stack = new StackPanel { Orientation = Orientation.Horizontal };

            var uri = isStandard
                ? new Uri("pack://Application:,,,/Images/reference.ico")
                : new Uri("pack://Application:,,,/Images/trial.ico");

            var image = new Image
            {
                Source = new BitmapImage(uri),
                Width = 16,
                Height = 16,
                Stretch = Stretch.Uniform
            };
            stack.Children.Add(image);

            var rect = new Rectangle { Width = 16, Height = 16 };
            rect.SetBinding(Shape.FillProperty, new Binding("PseudoColor"));
            stack.Children.Add(rect);

            var text = new TextBlock { Margin = new Thickness(2, 0, 0, 0) };
            text.SetBinding(TextBlock.TextProperty, new Binding("Name"));
            stack.Children.Add(text);

            return stack;
        }
    }
}
