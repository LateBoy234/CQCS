using RelayCommand = ThreeNH.Helper.Plugin.RelayCommand;
using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Navigation;
using ThreeNH.ColorimeterInterface;
using ThreeNH.ColorQualityControl.Utils;

namespace ThreeNH.ColorQualityControl.UI
{
    /// <summary>
    /// BlackCalibrationPage.xaml µÄ½»»¥Âß¼­
    /// </summary>
    public partial class BlackCalibrationPage : Page, INotifyPropertyChanged
    {
        private IInstrument _instrument;
        public BlackCalibrationPage(IInstrument instrument)
        {
            InitializeComponent();
            DataContext = this;
            _instrument = instrument;
            _BlackCalibrationPrompt = _instrument.IsColorHazeMeter() ? S._("ZeroPercentCalibrationPrompt") : (_instrument as ISystemSetting).SpectrumMeasurementMode == SpectrumMeasurementMode.Reflective ?
                S._("BlackCalibrationPrompt") : S._("TransmissionBlackCalibrationPrompt");
            _calibrationTitle = _instrument.IsColorHazeMeter() ? S._("ZeroPercentCalibration") : S._("Title_BlackCalibration");
        }

        private string _calibrationTitle;
        public string CalibrationTitle
        {
            get => _calibrationTitle;
            set
            {
                _calibrationTitle = value;
                RaisePropertyChanged(nameof(CalibrationTitle));
            }
        }

        private string _BlackCalibrationPrompt;
        public string BlackCalibrationPrompt
        {
            get => _BlackCalibrationPrompt;
            set
            {
                _BlackCalibrationPrompt = value;
                RaisePropertyChanged(nameof(BlackCalibrationPrompt));
            }
        }
        public event PropertyChangedEventHandler PropertyChanged;

        public void RaisePropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private RelayCommand _BlackCalibrationCommand;
        public RelayCommand BlackCalibrationCommand
        {
            get
            {
                if (_BlackCalibrationCommand == null)
                    _BlackCalibrationCommand = new RelayCommand(o =>
                    {
                        Task.Factory.StartNew(() =>
                        {
                            this.Dispatcher.Invoke(new Action(() =>
                            {
                                btnBlackCalibration.IsEnabled = false;
                                btnClose.IsEnabled = false;
                                Cursor = Cursors.Wait;
                            }));
                            Exception exception = null;
                            try
                            {
                                (_instrument as IMeasurement)?.DoBlackCalibration();
                            }
                            catch (Exception ex)
                            {
                                exception = ex;
                            }
                            this.Dispatcher.Invoke(new Action(() =>
                            {
                                Page page = exception != null ? CreateExceptionPage() : CreateWhiteCalibrationPage();
                                btnBlackCalibration.IsEnabled = true;
                                btnClose.IsEnabled = true;
                                Cursor = Cursors.AppStarting;
                                NavigationService.Navigate(page);
                            }));
                            Page CreateExceptionPage()
                            {
                                return new CalibrationResultPage(false, $"{S._("BlackCalibrationFailed")}:{exception.Message}");
                            }
                            Page CreateWhiteCalibrationPage()
                            {
                                return new WhiteCalibrationPage(_instrument);
                            }
                        });
                    });
                return _BlackCalibrationCommand;
            }
            set
            {
                _BlackCalibrationCommand = value;
                RaisePropertyChanged(nameof(BlackCalibrationCommand));
            }
        }

        private RelayCommand _CloseCommand;
        public RelayCommand CloseCommand
        {
            get
            {
                if (_CloseCommand == null)
                    _CloseCommand = new RelayCommand(o =>
                    {
                        Window window = Window.GetWindow(this);
                        window.DialogResult = false;
                        window.Close();
                    });
                return _CloseCommand;
            }
            set
            {
                _CloseCommand = value;
                RaisePropertyChanged(nameof(CloseCommand));
            }
        }

    }
}
