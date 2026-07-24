using System;
using ThreeNH.ColorQualityControl.Refactoring.Contracts;
using ThreeNH.ColorQualityControl.Specimens.Events;
using ThreeNH.ColorSpecimen;
using ThreeNH.DataModel;

namespace ThreeNH.ColorQualityControl.Services
{
    /// <summary>
    /// 阶段1-T11/T12：标试样选中与导航展示模式；TreeViewItem 锚点仍留 MainWindow。
    /// </summary>
    internal sealed class SpecimenSelectionState
    {
        private StandardSpecimen _currentStandard;
        private TestSpecimen _currentTest;
        private NavigationViewMode _viewMode = NavigationViewMode.None;
        private StandardSpecimen _singleStandardInShowing;

        public event EventHandler CurrentStandardChanged;

        public event EventHandler CurrentTestChanged;

        /// <summary>标样/试样选中变更的统一事件，Presenter 层优先订阅此事件（阶段2-T03）。</summary>
        public event EventHandler<SelectionChangedEventArgs> SelectionChanged;

        public NavigationViewMode ViewMode => _viewMode;

        public StandardSpecimen SingleStandardInShowing => _singleStandardInShowing;

        public bool IsShowAllStandards => _viewMode == NavigationViewMode.AllStandards;

        public bool IsShowAllTests => _viewMode == NavigationViewMode.AllTests;

        public bool IsShowOrphanTests => _viewMode == NavigationViewMode.OrphanTests;

        public bool IsShowSingleStandard => _viewMode == NavigationViewMode.SingleStandard;

        public bool IsShowCurrentStandard => _viewMode == NavigationViewMode.CurrentStandard;

        public StandardSpecimen CurrentStandard
        {
            get => _currentStandard;
            set
            {
                if (_currentStandard == value)
                {
                    return;
                }

                _currentStandard = value;
                if (_currentTest != null)
                {
                    _currentTest = null;
                    CurrentTestChanged?.Invoke(this, EventArgs.Empty);
                }

                CurrentStandardChanged?.Invoke(this, EventArgs.Empty);
                RaiseSelectionChanged();
            }
        }

        public TestSpecimen CurrentTest
        {
            get => _currentTest;
            set
            {
                if (_currentTest == value)
                {
                    return;
                }

                _currentTest = value;
                CurrentTestChanged?.Invoke(this, EventArgs.Empty);
                RaiseSelectionChanged();
            }
        }

        private void RaiseSelectionChanged()
            => SelectionChanged?.Invoke(this, new SelectionChangedEventArgs(_currentStandard, _currentTest));

        public void SetAllStandardsView() => SetViewMode(NavigationViewMode.AllStandards);

        public void SetAllTestsView() => SetViewMode(NavigationViewMode.AllTests);

        public void SetOrphanTestsView() => SetViewMode(NavigationViewMode.OrphanTests);

        public void SetCurrentStandardView() => SetViewMode(NavigationViewMode.CurrentStandard);

        public void SetSingleStandardView(StandardSpecimen standard)
        {
            _viewMode = NavigationViewMode.SingleStandard;
            _singleStandardInShowing = standard;
        }

        public void ResetView()
        {
            _viewMode = NavigationViewMode.None;
            _singleStandardInShowing = null;
        }

        public void Clear()
        {
            ResetView();
            CurrentStandard = null;
        }

        private void SetViewMode(NavigationViewMode mode)
        {
            _viewMode = mode;
            if (mode != NavigationViewMode.SingleStandard)
            {
                _singleStandardInShowing = null;
            }
        }
    }
}
