using Newtonsoft.Json.Linq;
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

namespace ThreeNH.Helper.Plugin
{
    /// <summary>
    /// LoadingWindow.xaml 的交互逻辑
    /// </summary>
    public partial class LoadingWindow : Window, INotifyPropertyChanged
    {
        public LoadingWindow()
        {
            InitializeComponent();
            IsProgressBarVisible = Visibility.Visible;
            IsCancelButtonVisible = Visibility.Visible;
            TitlePanel.MouseLeftButtonDown += OnMouseLeftButtonDown;
            DataContext = this;
        }

        private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private string _caption;
        public string Caption
        {
            get => _caption;
            set
            {
                _caption = value;
                OnPropertyChanged(nameof(Caption));
            }
        }

        private string _message;
        public string Message
        {
            get => _message;
            set
            {
                _message = value;
                OnPropertyChanged(nameof(Message));
            }
        }

        private string _cancelContent = "取消";
        /// <summary>
        /// 按钮内容，默认为"Cancel"，如果设置为null或空字符串，则不显示取消按钮
        /// </summary>
        public string CancelContent
        {
            get => _cancelContent;
            set
            {
                _cancelContent = value;
                OnPropertyChanged(nameof(CancelContent));
            }
        }

        private Visibility _isCancelButtonVisible;
        public Visibility IsCancelButtonVisible
        {
            get => _isCancelButtonVisible;
            set
            {
                _isCancelButtonVisible = value;
                OnPropertyChanged(nameof(IsCancelButtonVisible));
            }
        }

        private Visibility _isProgressBarVisible;
        public Visibility IsProgressBarVisible
        {
            get => _isProgressBarVisible;
            set
            {
                _isProgressBarVisible = value;
                OnPropertyChanged(nameof(IsProgressBarVisible));
            }
        }

        private int _progress;
        public int Progress
        {
            get => _progress;
            set
            {
                _progress = value;
                OnPropertyChanged(nameof(Progress));
            }
        }

        private RelayCommand _cancelCommand;
        public RelayCommand CancelCommand
        {
            get
            {
                if(_cancelCommand == null)
                {
                    _cancelCommand = new RelayCommand(o =>
                    {
                        Cancel();
                    });
                }
                return _cancelCommand;
            }
            set
            {
                _cancelCommand = value;
                OnPropertyChanged(nameof(CancelCommand));
            }
        }

        public event Action CancelDelegate;

        private void Cancel()
        {
            CancelDelegate?.Invoke();
        }
    }
}
