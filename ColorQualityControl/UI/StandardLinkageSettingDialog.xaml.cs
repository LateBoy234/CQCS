using MahApps.Metro.Controls;
using System.ComponentModel;
using System.Windows;
using ThreeNH.DataModel;

namespace ThreeNH.ColorQualityControl.UI
{
    /// <summary>
    /// StandardLinkageSettingDialog.xaml 的交互逻辑
    /// </summary>
    public partial class StandardLinkageSettingDialog : MetroWindow, INotifyPropertyChanged
    {
        public StandardLinkageSettingDialog()
        {
            InitializeComponent();

            DataContext = this;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void OnOkClick(object sender, RoutedEventArgs e)
        {
            if (IsLinkToStandard && SelectedStandard == null)
            {
                MessageBox.Show(S._("PleaseSelectAStandard"));
                return;
            }

            DialogResult = true;
        }

        private SpecimenWrap[] _standards;
        public SpecimenWrap[] Standards
        {
            get { return _standards; }
            set
            {
                _standards = value;
                NotifyPropertyChanged("Standards");
            }
        }


        private StandardSpecimen _selectedStandard;
        public StandardSpecimen SelectedStandard
        {
            get { return _selectedStandard; }
            set
            {
                if (_selectedStandard != value)
                {
                    _selectedStandard = value;
                    NotifyPropertyChanged("SelectedStandard");
                }
            }
        }

        private bool _isLinkToStandard = true;
        public bool IsLinkToStandard
        {
            get { return _isLinkToStandard; }
            set
            {
                if (value != _isLinkToStandard)
                {
                    _isLinkToStandard = value;
                    NotifyPropertyChanged("IsLinkToStandard");
                    NotifyPropertyChanged("DontLinkToStandard");
                }
            }
        }

        public bool DontLinkToStandard
        {
            get { return !IsLinkToStandard; }
            set
            {
                IsLinkToStandard = !value;
            }
        }
    }
}
