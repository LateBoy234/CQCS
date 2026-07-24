using System;
using System.Collections.Generic;
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
using ThreeNH.DataModel;

namespace ThreeNH.ColorQualityControl.UI
{
    /// <summary>
    /// ExportSettingDialog.xaml 的交互逻辑
    /// </summary>
    public partial class ExportSettingDialog : Window
    {
        public ExportSettingDialog(MainWindow mainWindow)
        {
            Owner = mainWindow;
            InitializeComponent();
        }

        private void ExportWithSystemSettingClick(object sender, RoutedEventArgs e)
        {
            IsUseSystemSetting = true;
            DialogResult = true;
        }

        private void ExportWithStandardSetting(object sender, RoutedEventArgs e)
        {
            IsUseStandardSetting = true;
            DialogResult = true;
        }

        private void SpecifyChromaticitySetting(object sender, RoutedEventArgs e)
        {
            ChromaticitySettingDialog dlg = new ChromaticitySettingDialog((Owner as MainWindow).WavelengthRange);
            dlg.Owner = this;
            dlg.Title = S._("SpecifyFieldsToExport");
            if (dlg.ShowDialog() == true)
            {
                IsUseSpecial = true;
                ChromaticitySetting = dlg.ChromaticitySetting;
                DialogResult = true;
            }
            else
            {
                DialogResult = false;
            }
        }

        private int _settingType = 0;
        public bool IsUseSystemSetting
        {
            get { return _settingType == 0; }
            set { _settingType = 0; }
        }

        public bool IsUseStandardSetting
        {
            get { return _settingType == 1; }
            set { _settingType = 1; }
        }

        public bool IsUseSpecial
        {
            get { return _settingType == 2; }
            set { _settingType = 2; }
        }

        public ChromaticitySetting ChromaticitySetting
        {
            get; private set;
        }
            
    }
}
