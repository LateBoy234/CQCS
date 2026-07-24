using PanTongColorMatchSearch;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ThreeNH.ColorQualityControl.UI
{
    /// <summary>
    /// CalibrationResultPage.xaml 的交互逻辑
    /// </summary>
    public partial class CalibrationResultPage : Page, INotifyPropertyChanged
    {
        private bool _result;
        private string _msg;
        public CalibrationResultPage(bool result, string message)
        {
            InitializeComponent();
            DataContext = this;
            _result = result;
            _msg = message;
            _Image = _result ? "../Images/success.png" : "../Images/fail.png";
        }

        public bool BackButtonVisibility => !_result;
        private string _Image;
        public string Image
        {
            get => _Image;
            set
            {
                _Image = value;
                RaisePropertyChanged(nameof(Image));
            }
        }

        public string Msg
        {
            get => _msg;
            set
            {
                _msg = value;
                RaisePropertyChanged(nameof(Msg));
            }
        }
        public event PropertyChangedEventHandler PropertyChanged;

        public void RaisePropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private RelayCommand _BackCommand;
        public RelayCommand BackCommand
        {
            get
            {
                if (_BackCommand == null)
                    _BackCommand = new RelayCommand(o => { NavigationService.GoBack(); });
                return _BackCommand;
            }
            set
            {
                _BackCommand = value;
                RaisePropertyChanged(nameof(BackCommand));
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
                        window.DialogResult = _result;
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
