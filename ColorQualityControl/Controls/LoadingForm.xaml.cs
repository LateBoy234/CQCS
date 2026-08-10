using RelayCommand = ThreeNH.Helper.Plugin.RelayCommand;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
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
using System.Windows.Shapes;

namespace ThreeNH.Controls
{
    /// <summary>
    /// JLoadingForm.xaml µÄ½»»¥Âß¼­
    /// </summary>
    public partial class LoadingForm : Window, INotifyPropertyChanged
    {
        public LoadingForm()
        {
            InitializeComponent();
            IsProgressBarVisible = Visibility.Visible;
            IsCancelButtonVisible = Visibility.Visible;
            Caption = "Handling";
            TitlePanel.MouseLeftButtonDown += OnMouseLeftButtonDown;
            DataContext = this;
        }

        private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
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

        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
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
                if (_cancelCommand == null)
                    _cancelCommand = new RelayCommand(o => Cancel());
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

        private void StackPanel_MouseDown(object sender, MouseButtonEventArgs e)
        {

        }
    }
}
