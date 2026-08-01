using PanTongColorMatchSearch;
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
using ThreeNH.SQCX.Infrastructure.Instruments.YHComm;

namespace ThreeNH.ColorQualityControl.UI
{
    /// <summary>
    /// ClarityCalibrationPage.xaml 的交互逻辑
    /// </summary>
    public partial class ClarityCalibrationPage : Page,INotifyPropertyChanged
    {
        private IInstrument _instrument;
        public ClarityCalibrationPage(IInstrument instrument)
        {
            InitializeComponent();
            DataContext = this;
            _instrument = instrument;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void RaisePropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private string _calibrationPrompt;
        public string CalibrationPrompt
        {
            get => _calibrationPrompt;
            set
            {
                _calibrationPrompt = value;
                RaisePropertyChanged(nameof(CalibrationPrompt));
            }
        }

        private RelayCommand _clarityCalibrationCommand;
        public RelayCommand ClarityCalibrationCommand
        {
            get
            {
                if (_clarityCalibrationCommand == null)
                    _clarityCalibrationCommand = new RelayCommand(o =>
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
                                if (_instrument is USBComm usbComm)
                                    usbComm.DoClarityCalibration();
                                else if (_instrument is BleComm bleComm)
                                    bleComm.DoClarityCalibration();
                            }
                            catch (Exception ex)
                            {
                                exception = ex;
                            }
                            this.Dispatcher.Invoke(new Action(() =>
                            {
                                Page page = exception != null ? CreateExceptionPage() : CreateSuccessPage();
                                btnBlackCalibration.IsEnabled = true;
                                btnClose.IsEnabled = true;
                                Cursor = Cursors.AppStarting;
                                NavigationService.Navigate(page);
                            }));
                            Page CreateExceptionPage()
                            {
                                return new CalibrationResultPage(false, $"{S._("ClarityCalibrationFailed")}");
                            }
                            Page CreateSuccessPage()
                            {
                                return new CalibrationResultPage(true, $"{S._("Msg_SuccessfulCalibration")}");
                            }
                        });
                    });
                return _clarityCalibrationCommand;
            }
            set
            {
                _clarityCalibrationCommand = value;
                RaisePropertyChanged(nameof(ClarityCalibrationCommand));
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
