using MahApps.Metro.Controls;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using ThreeNH.ColorimeterInterface;
using ThreeNH.ColorQualityControl.Annotations;
using ThreeNH.Controls;
using ThreeNH.Helper;
using ThreeNH.Optical;

namespace ThreeNH.ColorQualityControl.UI
{
    /// <summary>
    /// MeasurementConfigsDialog.xaml 的交互逻辑
    /// </summary>
    public partial class MeasurementConfigsDialog : MetroWindow, INotifyPropertyChanged
    {
        private ISystemSetting _systemSetting;

        public MeasurementConfigsDialog(ISystemSetting setting)
        {
            _systemSetting = setting;
            InitializeComponent();
            DataContext = this;
            ApetureComboBox.BindingItemsSource(_systemSetting.OptionalMeasurementCalibers, false);
            LensPositionComboBox.BindingItemsSource(_systemSetting.OptionalLensPositions, false);
            LensSwitchModeComboBox.BindingItemsSource(_systemSetting.OptionalLensSwitchModes, false);
            SpecularComponentComboBox.BindingItemsSource(_systemSetting.OptionalSpecularComponents, false);
            SpectrumMeasurementModeComboBox.BindingItemsSource(_systemSetting.OptionalSpectrumMeasurementModes, false);
            UvFlagComboBox.BindingItemsSource(_systemSetting.OptionalUVFlags, false);
            IlluminantComboBox.BindingItemsSource(_systemSetting.OptionalStandardIlluminants, false);
            if(_systemSetting.OptionalStandardObservers.Count()<1)
            {
                ObserverComboBox.Visibility = Visibility.Collapsed;
            }
            ObserverComboBox.BindingItemsSource(_systemSetting.OptionalStandardObservers, false);
            ColorSpaceComboBox.BindingItemsSource(_systemSetting.OptionalColorSpaces, false);
            ColorDiffFormulaComboBox.BindingItemsSource(_systemSetting.OptionalColorDiffFormulas, false);
            ColorIndexComboBox.BindingItemsSource(_systemSetting.OptionalColorIndexs, false);
        }


        private MeasurementCaliber _aperture;
        private LensPosition _lensPosition;
        private LensSwitchMode _lensSwitchMode;
        private SpecularComponent _specularComponent;
        private SpectrumMeasurementMode _spectrumMeasurementMode;
        private UVFlag _uvFlag;
        private StandardIlluminant _illuminant;
        private StandardObserver _observer;
        private ColorSpace _colorSpace;
        private ColorDiffFormula _colorDiffFormula;
        private ColorIndex _colorIndex;

        private void LoadedInfo()
        {
            Try(() => _systemSetting.RefreshCaches());

            try
            {
                _aperture = _systemSetting.Aperture;
            }
            catch (NotSupportedException)
            {
                ApetureComboBox.IsEnabled = false;
            }

            try
            {
                _lensPosition = _systemSetting.LensPosition;
            }
            catch (NotSupportedException)
            {
                LensPositionComboBox.IsEnabled = false;
            }

            try
            {
                _lensSwitchMode = _systemSetting.LensSwitchMode;
                ApetureComboBox.IsEnabled = _lensSwitchMode != LensSwitchMode.Auto;
                LensPositionComboBox.IsEnabled = _lensSwitchMode != LensSwitchMode.Auto && _spectrumMeasurementMode == SpectrumMeasurementMode.Transmissive;
            }
            catch (NotSupportedException)
            {
                _lensSwitchMode = LensSwitchMode.Manual;
                LensSwitchModeComboBox.IsEnabled = false;
            }

            try
            {
                _specularComponent = _systemSetting.SpecularComponent;
                SpecularComponentComboBox.IsEnabled = _systemSetting.IsSpecularComponentModifiable;
                LensSwitchModeComboBox.IsEnabled = _systemSetting.IsLensSwitchModeModifiable;
                ApetureComboBox.IsEnabled = _systemSetting.IsMeasurementCaliberModifiable;
                LensPositionComboBox.IsEnabled = _systemSetting.IsLensPositionModifiable;
            }
            catch (NotSupportedException)
            {
                SpecularComponentComboBox.IsEnabled = false;
            }

            try
            {
                _spectrumMeasurementMode = _systemSetting.SpectrumMeasurementMode;
                SpectrumMeasurementModeComboBox.IsEnabled = _systemSetting.IsSpectrumMeasurementModeModifiable;
            }
            catch (NotSupportedException)
            {
                SpectrumMeasurementModeComboBox.IsEnabled = false;
            }

            try
            {
                _uvFlag = _systemSetting.UvMode;
                UvFlagComboBox.IsEnabled = _systemSetting.IsUvModifiable;
            }
            catch (NotSupportedException)
            {
                UvFlagComboBox.IsEnabled = false;
            }

            try
            {
                _illuminant = _systemSetting.StandardIlluminant;
            }
            catch (NotSupportedException)
            {
                IlluminantComboBox.IsEnabled = false;
            }

            try
            {
                _observer = _systemSetting.StandardObserver;
            }
            catch (NotSupportedException)
            {
                ObserverComboBox.IsEnabled = false;
            }

            try
            {
                _colorSpace = _systemSetting.ColorSpace;
            }
            catch (NotSupportedException)
            {
                ColorSpaceComboBox.IsEnabled = false;
            }

            try
            {
                _colorDiffFormula = _systemSetting.ColorDiffFormula;
            }
            catch (NotSupportedException)
            {
                ColorDiffFormulaComboBox.IsEnabled = false;
            }

            try
            {
                _colorIndex = _systemSetting.ColorIndex;
            }
            catch (NotSupportedException)
            {
                ColorIndexComboBox.IsEnabled = false;
            }

        }
        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            //各型号的孔径大小是否可写入没有标准，这里统一禁用。没有取消选项
            

            // 1. 刷新缓存
            LoadedInfo();
          //  ApetureComboBox.IsEnabled = false;
            // 2. 统一读取并决定可用性
            SetEnabled(ApetureComboBox, () => _aperture = _systemSetting.Aperture);
            SetEnabled(LensPositionComboBox, () => _lensPosition = _systemSetting.LensPosition);
            SetEnabled(LensSwitchModeComboBox, () => _lensSwitchMode = _systemSetting.LensSwitchMode);
            SetEnabled(SpecularComponentComboBox, () => _specularComponent = _systemSetting.SpecularComponent);
            SetEnabled(SpectrumMeasurementModeComboBox, () => _spectrumMeasurementMode = _systemSetting.SpectrumMeasurementMode);
            SetEnabled(UvFlagComboBox, () => _uvFlag = _systemSetting.UvMode);
            SetEnabled(IlluminantComboBox, () => _illuminant = _systemSetting.StandardIlluminant);
            SetEnabled(ObserverComboBox, () => _observer = _systemSetting.StandardObserver);
            SetEnabled(ColorSpaceComboBox, () => _colorSpace = _systemSetting.ColorSpace);
            SetEnabled(ColorDiffFormulaComboBox, () => _colorDiffFormula = _systemSetting.ColorDiffFormula);
            SetEnabled(ColorIndexComboBox, () => _colorIndex = _systemSetting.ColorIndex);

            // 3. 特殊依赖逻辑
            if (LensSwitchModeComboBox.IsEnabled)
            {
                var auto = _lensSwitchMode == LensSwitchMode.Auto;
              //  ApetureComboBox.IsEnabled &= !auto;
                LensPositionComboBox.IsEnabled &= !auto;
            }

            // 4. 受系统“可修改”标志控制的控件
            SpecularComponentComboBox.IsEnabled &= _systemSetting.IsSpecularComponentModifiable;
            LensSwitchModeComboBox.IsEnabled &= _systemSetting.IsLensSwitchModeModifiable;
           // ApetureComboBox.IsEnabled &= _systemSetting.IsMeasurementCaliberModifiable;
            LensPositionComboBox.IsEnabled &= _systemSetting.IsLensPositionModifiable;
            SpectrumMeasurementModeComboBox.IsEnabled &= _systemSetting.IsSpectrumMeasurementModeModifiable;
            UvFlagComboBox.IsEnabled &= _systemSetting.IsUvModifiable;
        }

        /* ---------- 工具方法 ---------- */

        /// <summary>
        /// 如果读取属性抛出 NotSupportedException，则把对应 ComboBox 置灰。
        /// </summary>
        private static void SetEnabled(ComboBox box, Action readProperty)
        {
            try { readProperty(); }
            catch (NotSupportedException) { box.IsEnabled = false; }
        }

        /// <summary>
        /// 无返回值的安全执行。
        /// </summary>
        private static void Try(Action action)
        {
            try { action(); }
            catch { /* 忽略所有异常 */ }
        }
        public MeasurementCaliber Aperture
        {
            get { return _aperture; }
            set
            {
                if (value == _aperture) return;

                var origValue = _aperture;
                _aperture = value;

                try
                {
                    _systemSetting.Aperture = value;
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                    Application.Current.Dispatcher.BeginInvoke(
                        new Action(() =>
                        {
                            _aperture = origValue;
                            OnPropertyChanged(nameof(Aperture));
                        }), DispatcherPriority.ContextIdle);
                }

                OnPropertyChanged();
            }
        }

        public LensPosition LensPosition
        {
            get { return _lensPosition; }
            set
            {
                if (value == _lensPosition) return;
                var origValue = _lensPosition;
                _lensPosition = value;

                try
                {
                    _systemSetting.LensPosition = value;
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                    Application.Current.Dispatcher.BeginInvoke(
                        new Action(() =>
                        {
                            _lensPosition = origValue;
                            OnPropertyChanged(nameof(LensPosition));
                        }), DispatcherPriority.ContextIdle);
                }
                OnPropertyChanged();
            }
        }


        public LensSwitchMode LensSwitchMode
        {
            get { return _lensSwitchMode; }
            set
            {
                if (value == _lensSwitchMode) return;
                var origValue = _lensSwitchMode;
                _lensSwitchMode = value;

                try
                {
                    _systemSetting.LensSwitchMode = value;
                   // ApetureComboBox.IsEnabled = _lensSwitchMode != LensSwitchMode.Auto;
                    LensPositionComboBox.IsEnabled = _lensSwitchMode != LensSwitchMode.Auto&& _spectrumMeasurementMode == SpectrumMeasurementMode.Transmissive;
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                    Dispatcher.BeginInvoke(new Action((() =>
                    {
                        _lensSwitchMode = origValue;
                        OnPropertyChanged(nameof(LensSwitchMode));
                    })), DispatcherPriority.ContextIdle);
                }

                OnPropertyChanged();
            }
        }

        public SpecularComponent SpecularComponent
        {
            get { return _specularComponent; }
            set
            {
                if (value == _specularComponent) return;
                var origValue = _specularComponent;
                _specularComponent = value;
                try
                {
                    _systemSetting.SpecularComponent = value;
                }
                catch (Exception ex)
                {
                    MessageBox.Show(S._(ex.Message));
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        _specularComponent = origValue;
                        OnPropertyChanged(nameof(SpecularComponent));
                    }), DispatcherPriority.ContextIdle);
                }
                OnPropertyChanged();
            }
        }

        public SpectrumMeasurementMode SpectrumMeasurementMode
        {
            get { return _spectrumMeasurementMode; }
            set
            {
                if (value == _spectrumMeasurementMode) return;
                var origValue = _spectrumMeasurementMode;
                _spectrumMeasurementMode = value;
                try
                {
                    _systemSetting.SpectrumMeasurementMode = value;
                    LensSwitchModeComboBox.IsEnabled = _systemSetting.IsLensSwitchModeModifiable;
                    //ApetureComboBox.IsEnabled = _systemSetting.IsMeasurementCaliberModifiable;
                    LensPositionComboBox.IsEnabled = _systemSetting.IsLensPositionModifiable;
                }
                catch (Exception e)
                {
                    MessageBox.Show(S._(e.Message));
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        _spectrumMeasurementMode = origValue;
                        OnPropertyChanged(nameof(SpectrumMeasurementMode));
                    }), DispatcherPriority.ContextIdle);
                }
                OnPropertyChanged();
            }
        }

        public UVFlag UvFlag
        {
            get { return _uvFlag; }
            set
            {
                if (value == _uvFlag) return;
                var origValue = _uvFlag;
                _uvFlag = value;
                try
                {
                    _systemSetting.UvMode = value;
                }
                catch (Exception e)
                {
                    MessageBox.Show(S._(e.Message));
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        _uvFlag = origValue;
                        OnPropertyChanged(nameof(UvFlag));
                    }), DispatcherPriority.ContextIdle);
                }
                OnPropertyChanged();
            }
        }

        public StandardIlluminant Illuminant
        {
            get { return _illuminant; }
            set
            {
                if (value == _illuminant) return;
                var origValue = _illuminant;
                _illuminant = value;
                try
                {
                    _systemSetting.StandardIlluminant = value;
                }
                catch (Exception e)
                {
                    MessageBox.Show(S._(e.Message));
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        _illuminant = origValue;
                        OnPropertyChanged(nameof(Illuminant));
                    }), DispatcherPriority.ContextIdle);
                }
                OnPropertyChanged();
            }
        }

        public StandardObserver Observer
        {
            get { return _observer; }
            set
            {
                if (value == _observer) return;
                var origValue = _observer;
                _observer = value;
                try
                {
                    _systemSetting.StandardObserver = value;
                }
                catch (Exception e)
                {
                    MessageBox.Show(S._(e.Message));
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        _observer = origValue;
                        OnPropertyChanged(nameof(Observer));
                    }), DispatcherPriority.ContextIdle);
                }
                OnPropertyChanged();
            }
        }

        public ColorSpace ColorSpace
        {
            get { return _colorSpace; }
            set
            {
                if (value == _colorSpace) return;

                var origValue = _colorSpace;
                _colorSpace = value;
                try
                {
                    _systemSetting.ColorSpace = value;
                }
                catch (Exception e)
                {
                    MessageBox.Show(S._(e.Message));
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        _colorSpace = origValue;
                        OnPropertyChanged(nameof(ColorSpace));
                    }), DispatcherPriority.ContextIdle);
                }
                OnPropertyChanged();
            }
        }

        public ColorDiffFormula ColorDiffFormula
        {
            get { return _colorDiffFormula; }
            set
            {
                if (value == _colorDiffFormula) return;

                var origValue = _colorDiffFormula;
                _colorDiffFormula = value;
                try
                {
                    _systemSetting.ColorDiffFormula = value;
                }
                catch (Exception e)
                {
                    MessageBox.Show(S._(e.Message));
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        _colorDiffFormula = origValue;
                        OnPropertyChanged(nameof(ColorDiffFormula));
                    }), DispatcherPriority.ContextIdle);
                }
                OnPropertyChanged();
            }
        }

        public ColorIndex ColorIndex
        {
            get { return _colorIndex; }
            set
            {
                if (value == _colorIndex) return;
                var origValue = _colorIndex;
                _colorIndex = value;
                try
                {
                    _systemSetting.ColorIndex = value;
                }
                catch (Exception e)
                {
                    MessageBox.Show(S._(e.Message));
                    Dispatcher.BeginInvoke(new Action((() =>
                    {
                        _colorIndex = origValue;
                        OnPropertyChanged(nameof(ColorIndex));
                    })), DispatcherPriority.ContextIdle);
                }
                OnPropertyChanged();
            }
        }

        private void ColorIndex_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

    }
}
