using RelayCommand = ThreeNH.Helper.Plugin.RelayCommand;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using ThreeNH.ColorimeterInterface;
using ThreeNH.ColorQualityControl.Utils;

namespace ThreeNH.ColorQualityControl.UI
{
    /// <summary>
    /// WhiteCalibrationPage.xaml µÄ½»»¥Âß¼­
    /// </summary>
    public partial class WhiteCalibrationPage : Page, INotifyPropertyChanged
    {
        IInstrument _instrument;
        public WhiteCalibrationPage(IInstrument instrument)
        {
            InitializeComponent();
            _instrument = instrument;
            DataContext = this;
            _calibrationTitle = _instrument.IsColorHazeMeter() ? S._("HundredPercentCalibration") : S._("Title_WhiteCalibration");
            _WhiteCalibrationPrompt = _instrument.IsColorHazeMeter() ? S._("HundredPercentCalibrationPrompt") :
                (_instrument as ISystemSetting).SpectrumMeasurementMode == SpectrumMeasurementMode.Reflective ?
                S._("WhiteCalibrationPrompt") : S._("TransmissionWhiteCalibrationPrompt");
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void RaisePropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
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

        private string _WhiteCalibrationPrompt;
        public string WhiteCalibrationPrompt
        {
            get => _WhiteCalibrationPrompt;
            set
            {
                _WhiteCalibrationPrompt = value;
                RaisePropertyChanged(nameof(WhiteCalibrationPrompt));
            }
        }
        private RelayCommand _WhiteCalibrationCommand;
        public RelayCommand WhiteCalibrationCommand
        {
            get
            {
                if (_WhiteCalibrationCommand == null)
                    _WhiteCalibrationCommand = new RelayCommand(o =>
                    {
                        Task.Factory.StartNew(() =>
                        {
                            this.Dispatcher.Invoke(new Action(() =>
                            {
                                btnWhiteCalibration.IsEnabled = false;
                                btnClose.IsEnabled = false;
                                Cursor = Cursors.Wait;
                            }));
                            Exception exception = null;
                            try
                            {
                                (_instrument as IMeasurement)?.DoWhiteCalibration();
                            }
                            catch (Exception ex)
                            {
                                exception = ex;
                            }
                            this.Dispatcher.Invoke(new Action(() =>
                            {
                                Page page = exception != null ? CreateExceptionPage() : _instrument.IsClarityValid() ? CreateClarityCalibrationPage() : CreateSuccessPage();
                                btnWhiteCalibration.IsEnabled = true;
                                btnClose.IsEnabled = true;
                                Cursor = Cursors.AppStarting;
                                NavigationService.Navigate(page);
                            }));
                            Page CreateExceptionPage()
                            {
                                return new CalibrationResultPage(false, $"{S._("WhiteCalibrationFailed")}:{S._(exception.Message)}");
                            }
                            Page CreateSuccessPage()
                            {
                                return new CalibrationResultPage(true, $"{S._("Msg_SuccessfulCalibration")}");
                            }

                            Page CreateClarityCalibrationPage()
                            {
                                return new ClarityCalibrationPage(_instrument);
                            }
                        });
                    });
                return _WhiteCalibrationCommand;
            }
            set
            {
                _WhiteCalibrationCommand = value;
                RaisePropertyChanged(nameof(WhiteCalibrationCommand));
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
