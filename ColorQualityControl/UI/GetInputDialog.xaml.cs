using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace ThreeNH.ColorQualityControl.UI
{
    /// <summary>
    /// RenameDialog.xaml 的交互逻辑
    /// </summary>
    public partial class GetInputDialog : Window, INotifyPropertyChanged
    {
        public GetInputDialog()
        {
            InitializeComponent();
            DataContext = this;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private string _prompt = "Please input: ";
        public string Prompt
        {
            get { return _prompt; }
            set
            {
                if (_prompt != value)
                {
                    _prompt = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Prompt"));
                }
            }
        }

        private string _input;
        public string Input
        {
            get { return _input; }
            set
            {
                if (_input != value)
                {
                    _input = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Input"));
                }
            }
        }

        private void OnOkClick(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private void TextBox_Loaded(object sender, RoutedEventArgs e)
        {
            var textBox = sender as TextBox;
            if (textBox != null)
            {
                textBox.Dispatcher.Invoke(() =>
                {
                    textBox.Focus();
                    textBox.SelectAll();
                }, System.Windows.Threading.DispatcherPriority.Render);
            }
        }
    }
}
