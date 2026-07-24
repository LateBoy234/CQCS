using System;
using System.Windows.Controls;
using ThreeNH.ColorQualityControl.Adapter;
using ThreeNH.ColorQualityControl.Refactoring.Contracts;
using ThreeNH.ColorQualityControl.Services;
using ThreeNH.ColorQualityControl.Specimens.Events;
using ThreeNH.ColorSpecimen;
using ThreeNH.DataModel;
using ThreeNH.Optical;

namespace ThreeNH.ColorQualityControl.Presentation
{
    /// <summary>
    /// 标试样 DataGrid 与光谱/统计图增删；订阅 Specimen 领域事件，替代 MainWindow 手动 Present/Unpresent（2-T07）。
    /// </summary>
    internal sealed class SpecimenGridPresenter
    {
        private readonly ISpecimenGridHost _host;
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

        public SpecimenGridPresenter(ISpecimenGridHost host, JobDocumentService jobDocument)
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

        private void OnSpecimenAdded(object sender, SpecimenAddedEventArgs e)
        {
            if (IsBulkUpdate)
            {
                return;
            }

            if (e.Kind == SpecimenKind.Standard)
            {
                PresentStandard(e.Standard);
                return;
            }

            PresentTest(e.Test);
        }

        private void OnSpecimenRemoved(object sender, SpecimenRemovedEventArgs e)
        {
            if (IsBulkUpdate)
            {
                return;
            }

            if (e.Kind == SpecimenKind.Standard)
            {
                UnpresentStandard(e.Standard);
                return;
            }

            UnpresentTest(e.Test);
        }

        private void PresentStandard(StandardSpecimen standard)
        {
            if (!_host.IsShowAllStandards)
            {
                SyncSpecularComponent(standard.SpecularComponent);
                return;
            }

            var item = _host.CreateStandardWrap(standard);
            _host.StandardDataGrid.Items.Add(item);
            if (item.Spectrum != null)
            {
                _host.SpectrumPlot.AddStandard(item.Spectrum, standard.Name, item.PseudoColor);
                _host.SpectrumPlot.YAxisTitle = _host.SpectrumType;
                if (item.Specimen.IsTransmission)
                {
                    _host.AbsorbancePlot.AddStandard(item.Spectrum, standard.Name, item.PseudoColor);
                }
            }

            SyncSpecularComponent(standard.SpecularComponent);
        }

        private void PresentTest(TestSpecimen test)
        {
            if (!_host.IsShowTest(test))
            {
                SyncSpecularComponent(test.SpecularComponent);
                return;
            }

            var item = _host.CreateTestWrap(test);
            AddToShow(item);
            SyncSpecularComponent(test.SpecularComponent);
        }

        private void UnpresentStandard(StandardSpecimen standard)
        {
            if (!_host.IsShowStandard(standard))
            {
                return;
            }

            var item = FindInStandardGrid(standard);
            if (item == null)
            {
                return;
            }

            if (item.Spectrum != null)
            {
                _host.SpectrumPlot.Remove(item.Spectrum);
                if (item.Specimen.IsTransmission)
                {
                    _host.AbsorbancePlot.Remove(item.Spectrum);
                }
            }

            _host.StandardDataGrid.Items.Remove(item);
        }

        private void UnpresentTest(TestSpecimen test)
        {
            // RemoveTest 后 StandardSpecimen 已被清空，不能再靠 IsShowTest 判断是否在表中
            int index = IndexOf(_host.TestDataGrid, test);
            if (index < 0 || !(_host.TestDataGrid.Items[index] is SpecimenWrap item))
            {
                return;
            }

            RemoveFromShow(item);
        }

        /// <summary>
        /// 移动试样改父标样时不触发 SpecimenAdded/Removed，须由调用方显式同步视图（2-T07）。
        /// </summary>
        internal void AddTestWrapToView(SpecimenWrap item)
        {
            if (item == null)
            {
                return;
            }

            AddToShow(item);
        }

        internal void RemoveTestWrapFromView(SpecimenWrap item)
        {
            if (item == null)
            {
                return;
            }

            RemoveFromShow(item);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="item"></param>
        private void AddToShow(SpecimenWrap item)
        {
            _host.TestDataGrid.Items.Add(item);

            if (item.Spectrum != null)
            {
                _host.SpectrumPlot.AddTest(item.Spectrum, item.Name);
                _host.SpectrumPlot.YAxisTitle = _host.SpectrumType;
                if (item.Specimen.IsTransmission)
                {
                    _host.AbsorbancePlot.AddTest(item.Spectrum, item.Name);
                }
            }

            if (_host.IsShowSingleStandard)
            {
                _host.StatisticsPlot.Add(item);
                _host.NotifySingleStandardDiagramChanged();
            }
        }

        private void RemoveFromShow(SpecimenWrap item)
        {
            _host.TestDataGrid.Items.Remove(item);
            _host.StatisticsPlot.Remove(item);
            _host.NotifySingleStandardDiagramChanged();
        }

        private static int IndexOf(DataGrid grid, Specimen specimen)
        {
            for (int i = 0; i < grid.Items.Count; i++)
            {
                if (grid.Items[i] is SpecimenWrap wrap && wrap.Specimen == specimen)
                {
                    return i;
                }
            }

            return -1;
        }

        private SpecimenWrap FindInStandardGrid(StandardSpecimen standard)
        {
            foreach (var entry in _host.StandardDataGrid.Items)
            {
                if (entry is SpecimenWrap wrap && wrap.Specimen == standard)
                {
                    return wrap;
                }
            }

            return null;
        }

        private void SyncSpecularComponent(SpecularComponent specimenComponent)
        {
            if (specimenComponent == _host.SpecularComponent)
            {
                return;
            }

            if (specimenComponent == SpecularComponent.SCIAndSCE)
            {
                if (_host.SpecularComponent == SpecularComponent.Unknown)
                {
                    _host.SpecularComponent = SpecularComponent.SCI;
                }
            }
            else
            {
                _host.SpecularComponent = specimenComponent;
            }
        }
    }
}
